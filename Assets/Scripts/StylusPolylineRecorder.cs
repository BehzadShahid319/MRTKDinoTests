using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using TMPro; // optional, remove if not using

[RequireComponent(typeof(LineRenderer))]
public class StylusPolylineRecorder : MonoBehaviour
{
    public static StylusPolylineRecorder Instance;

    [Header("Line Mode")]
    [Tooltip("If true, draws straight segments between points. If false, uses Catmull-Rom smoothing.")]
    public bool useStraightLines = true;

    [Header("Tracking (required)")]
    [Tooltip("Transform that represents the tracked marker/root from DINO.")]
    public Transform trackedObject;

    [Tooltip("Local offset from trackedObject to physical tip (calibrated).")]
    public Vector3 tipOffset = Vector3.zero;

    [Header("Recording Settings")]
    [Tooltip("Minimum world-space distance (meters) between two registered points.")]
    public float minPointDistance = 0.005f; // 5 mm

    [Tooltip("Minimum time between registrations (seconds).")]
    public float minTimeBetweenPoints = 0.12f;

    [Tooltip("Low-pass smoothing factor for raw tip (0..1). 0=no smoothing, 1=very slow).")]
    [Range(0f, 1f)]
    public float tipSmoothing = 0.25f;

    [Header("Line / Visuals")]
    public LineRenderer lineRenderer;
    [Tooltip("Marker prefab for each recorded point (small sphere).")]
    public GameObject markerPrefab;
    [Tooltip("Distance Text prefab for each recorded point group.")]
    public GameObject distanceTextPrefab;
    [Tooltip("Container transform for spawned markers (keeps hierarchy clean).")]
    public Transform worldMarkersParent;
    [Tooltip("Material for the smoothed polyline.")]
    public Material lineMaterial;
    [Tooltip("Line base width (meters).")]
    public float lineWidth = 0.004f;

    [Header("Smoothing / Interpolation")]
    [Tooltip("Catmull-Rom subdivisions per segment (higher = smoother, heavier).")]
    [Range(1, 24)]
    public int subdivisionsPerSegment = 6;

    [Header("Optional UI")]
    public LookAtObject PolyLineCanvasLookAt;
    public TMP_Text totalDistanceText; // optional
    public TMP_Text pointsCountText;   // optional

    [Header("Logging")]
    public bool saveLogToFile = false;
    public string logFileName = "PolylinePointsLog.txt";

    // runtime
    private List<Vector3> points = new List<Vector3>();
    private List<GameObject> markerPool = new List<GameObject>();
    private List<GameObject> distanceTextPool = new List<GameObject>();
    private Vector3 smoothedTipPos;
    private float lastPointTime = -999f;
    private float totalDistance = 0f;
    private bool pointsDirty = false; // mark when we need to rebuild line

    // pooling config
    private int poolGrowStep = 8;

    private void Reset()
    {
        // defaults if not set in inspector
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.widthMultiplier = lineWidth;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        lineRenderer.material = lineMaterial;
        lineRenderer.widthMultiplier = lineWidth;

        if (worldMarkersParent == null)
        {
            // create a persistent root so markers are static in world space
            GameObject root = new GameObject("PolylineMarkersRoot");
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            worldMarkersParent = root.transform;
        }

        // initial smoothed tip pos
        if (trackedObject != null)
            smoothedTipPos = trackedObject.position + trackedObject.rotation * tipOffset;
        else
            smoothedTipPos = Vector3.zero;
    }

    private void Update()
    {
        if (trackedObject == null) return;

        // Smooth the raw tip position to reduce jitter (low-pass filter)
        Vector3 rawTip = trackedObject.position + trackedObject.rotation * tipOffset;
        smoothedTipPos = Vector3.Lerp(smoothedTipPos, rawTip, 1f - Mathf.Exp(-tipSmoothing * 60f * Time.deltaTime));
        // Note: above uses an exponential smoothing approach (stable across frame rates)

        // If points changed, rebuild polyline (only when dirty), once per frame
        if (pointsDirty)
        {
            RebuildPolyline();
            pointsDirty = false;
        }

        // Optional: update UI
        if (totalDistanceText != null && points.Count>1)
        {
            float mm = totalDistance * 1000f;
            totalDistanceText.text = $"Total: {mm:F2} mm";
            totalDistanceText.enabled = true;
            totalDistanceText.transform.position = 0.02f * Vector3.up + ((points[0] + points[points.Count - 1]) / 2); //set total distance text position in center of line
        }
        if (pointsCountText != null && points.Count > 1)
        {
            pointsCountText.text = $"Pts: {points.Count}";
            pointsCountText.enabled = true;
            pointsCountText.transform.position = 0.04f * Vector3.up + ((points[0] + points[points.Count - 1]) / 2); //set total distance text position in center of line
        }
        if(((totalDistanceText != null && points.Count > 1) || (pointsCountText != null && points.Count > 1)) && PolyLineCanvasLookAt != null)
        {
            PolyLineCanvasLookAt.updateLookAt();
        }
    }

    // Public API for external callers (e.g., voice handlers, button events)
    public bool TryRecordPoint(bool forceRecord = false)
    {
        // Use smoothed tip position
        Vector3 pos = smoothedTipPos;
        if (forceRecord)
        {
            pos = trackedObject.position + trackedObject.rotation * tipOffset;
        }
        // simple validation
        if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z)) return false;

        // time debounce
        if (!forceRecord && Time.time - lastPointTime < minTimeBetweenPoints) return false;

        // distance debounce against last registered point
        if (!forceRecord && points.Count > 0)
        {
            if (Vector3.Distance(points[points.Count - 1], pos) < minPointDistance) return false;
        }

        // register
        AddPointInternal(pos);
        lastPointTime = Time.time;
        return true;
    }

    // internal add - will create marker and mark dirty
    private void AddPointInternal(Vector3 worldPos)
    {
        points.Add(worldPos);
        SpawnMarker(worldPos, points.Count - 1);
        if (points.Count > 2)
        {
            float dist = Vector3.Distance(points[points.Count - 2], points[points.Count - 1]) * 1000f; // mm
            Vector3 midPoint = (points[points.Count - 2] + points[points.Count - 1]) / 2f;
            SpawnDistanceText(midPoint, points.Count - 2, dist);
            //spawn distance text for 1st 2 Points
            dist = Vector3.Distance(points[1], points[0]) * 1000f; // mm
            midPoint = (points[1] + points[0]) / 2f;
            SpawnDistanceText(midPoint, 0, dist);
        }
        UpdateTotalDistance();
        pointsDirty = true;
        if (saveLogToFile) AppendLog($"Point[{points.Count - 1}] = {worldPos.ToString("F6")}");
    }

    // Undo last point
    public bool UndoLastPoint()
    {
        if (points.Count == 0) return false;
        int lastIndex = points.Count - 1;
        points.RemoveAt(lastIndex);
        DespawnMarkerAt(lastIndex);
        UpdateTotalDistance();
        pointsDirty = true;
        if (saveLogToFile) AppendLog($"Undo point {lastIndex}");
        return true;
    }

    public void ClearAll()
    {
        points.Clear();
        RecycleAllMarkers();
        RecycleAllDistanceTexts();
        totalDistance = 0f;
        pointsDirty = true;
        pointsCountText.enabled = false;
        totalDistanceText.enabled = false;
        if (saveLogToFile) AppendLog("Cleared all points");
    }

    // Rebuild the LineRenderer with smoothed Catmull-Rom points
    private void RebuildPolyline()
    {
        if (points.Count == 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        // STRAIGHT LINE MODE
        if (useStraightLines || points.Count < 3)
        {
            lineRenderer.positionCount = points.Count;
            for (int i = 0; i < points.Count; ++i)
            {
                lineRenderer.SetPosition(i, points[i]);
            }
            return;
        }

        // CATMULL-ROM CURVE MODE
        List<Vector3> interp = CatmullRomSpline(points, subdivisionsPerSegment);

        lineRenderer.positionCount = interp.Count;
        for (int i = 0; i < interp.Count; ++i)
        {
            lineRenderer.SetPosition(i, interp[i]);
        }
    }

    // calculate total polyline length (sum of segments between points — not the interpolated length)
    private void UpdateTotalDistance()
    {
        float sum = 0f;
        for (int i = 1; i < points.Count; ++i)
        {
            sum += Vector3.Distance(points[i - 1], points[i]);
        }
        totalDistance = sum;
    }

    public void toggleLineStyle()
    {
        useStraightLines = !useStraightLines;
        RebuildPolyline();
    }

    #region Marker Pooling
    private void EnsurePoolSize(int size)
    {
        while (markerPool.Count < size)
        {
            GameObject go = null;
            if (markerPrefab != null)
            {
                go = Instantiate(markerPrefab, worldMarkersParent);
            }
            else
            {
                // generate a small default sphere if no prefab
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.SetParent(worldMarkersParent, false);
                go.transform.localScale = Vector3.one * 0.012f; // 12 mm sphere default
                DestroyImmediate(go.GetComponent<Collider>()); // remove collider for performance
            }
            go.SetActive(false);
            markerPool.Add(go);
        }
    }

    private void SpawnMarker(Vector3 pos, int index)
    {
        EnsurePoolSize(index + 1);
        GameObject go = markerPool[index];
        go.transform.position = pos;
        go.transform.rotation = Quaternion.identity;
        go.SetActive(true);
    }

    private void DespawnMarkerAt(int index)
    {
        if (index < 0 || index >= markerPool.Count) return;
        GameObject go = markerPool[index];
        if (go != null) go.SetActive(false);
    }

    private void RecycleAllMarkers()
    {
        for (int i = 0; i < markerPool.Count; ++i)
        {
            if (markerPool[i] != null) markerPool[i].SetActive(false);
        }
    }
    #endregion

    #region Distance Text Pooling
    private void EnsureDistanceTextPoolSize(int size)
    {
        while (distanceTextPool.Count < size)
        {
            GameObject go = null;
            if (distanceTextPrefab != null)
            {
                go = Instantiate(distanceTextPrefab, Vector3.zero, Quaternion.identity);
            }
            else
            {
                return;
            }
            go.SetActive(false);
            distanceTextPool.Add(go);
        }
    }

    private void SpawnDistanceText(Vector3 pos, int index, float distance)
    {
        EnsureDistanceTextPoolSize(index + 1);
        GameObject go = distanceTextPool[index];
        go.GetComponentInChildren<TMP_Text>().text = distance.ToString("F2") + " mm";
        go.transform.position = pos;
        //go.transform.rotation = Quaternion.identity;
        go.SetActive(true);
    }

    private void DespawnDistanceTextAt(int index)
    {
        if (index < 0 || index >= distanceTextPool.Count) return;
        GameObject go = distanceTextPool[index];
        if (go != null) go.SetActive(false);
    }

    private void RecycleAllDistanceTexts()
    {
        for (int i = 0; i < distanceTextPool.Count; ++i)
        {
            if (distanceTextPool[i] != null) distanceTextPool[i].SetActive(false);
        }
    }
    #endregion

    #region Catmull-Rom Spline
    // returns interpolated positions including endpoints
    private List<Vector3> CatmullRomSpline(List<Vector3> pts, int subdivisions)
    {
        List<Vector3> output = new List<Vector3>();

        // For endpoints, we add "virtual" control points by reflecting ends
        // so curve passes through first and last actual points
        for (int i = 0; i < pts.Count; i++)
        {
            // Determine control points p0,p1,p2,p3 for segment around p1-p2
            Vector3 p0 = i == 0 ? pts[i] + (pts[i] - pts[i + 1]) : pts[i - 1];
            Vector3 p1 = pts[i];
            Vector3 p2 = (i + 1 < pts.Count) ? pts[i + 1] : pts[pts.Count - 1];
            Vector3 p3 = (i + 2 < pts.Count) ? pts[i + 2] : p2 + (p2 - p1);

            if (i == pts.Count - 1)
            {
                // last point just add endpoint
                output.Add(pts[pts.Count - 1]);
                break;
            }

            // Add subdivided points between p1 and p2
            for (int s = 0; s <= subdivisions; s++)
            {
                float t = s / (float)subdivisions;
                Vector3 point = CatmullRom(p0, p1, p2, p3, t);
                output.Add(point);
            }
        }
        return output;
    }

    // Catmull-Rom interpolation formula (centripetal-like)
    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // Standard Catmull-Rom (can be tuned via tension if needed)
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * ((2f * p1) +
               (-p0 + p2) * t +
               (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
               (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }
    #endregion

    #region Logging
    private void AppendLog(string line)
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, logFileName);
            string entry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {line}\n";
            File.AppendAllText(path, entry);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"PolylineRecorder: Failed to write log: {e.Message}");
        }
    }
    #endregion
}