using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpatialMashPointsDistance : MonoBehaviour
{
    [SerializeField] LineRenderer lineRendererForDisplay;
    [SerializeField] Transform trackedObject;
    [SerializeField] MeshRenderer trackedObjectMesh;
    [SerializeField] Vector3 tipOffset;
    [SerializeField] float rayLength;
    [SerializeField] LayerMask spatialObjectLayer;

    [SerializeField]
    [Range(0.001f, 0.05f)] float minPointDistance = 0.005f;
    [SerializeField]
    [Range(0.01f, 0.5f)] float minTime = 0.1f;
    [SerializeField]
    [Range(0.01f, 01f)] float smoothingFactor = 0.25f; 

    [SerializeField] Material defaultMaterial;
    [SerializeField] Material triggeredMaterial;

    [Header("=====UI=====")]
    [SerializeField] TextMeshProUGUI point1PosDIsplayUI;
    [SerializeField] TextMeshProUGUI point2PosDIsplayUI;
    [SerializeField] TextMeshProUGUI DistanceTextUI;

    Vector3 tipPosition;
    Vector3 previousTipPosition;
    Vector3 rayTargetPoint;
    Vector3 direction;
    Vector3 rawTipPos;

    bool tipTouchingSurface = false;
    bool firstPointRegistered, secondPointRegestered = false;
    Vector3 firstPointPostition, secondPointPosition, lastRegisteredPoint = Vector3.zero;

    float lastPointTime = 0;
    bool distanceCalculated = false;
    float distance = 0;

    private void Start()
    {
        lineRendererForDisplay.useWorldSpace = true;
        previousTipPosition = trackedObject.position + trackedObject.rotation * tipOffset;
    }

    private void Update()
    {
        direction = -trackedObject.up;
        rawTipPos = trackedObject.position + trackedObject.rotation * tipOffset;
        tipPosition = Vector3.Lerp(previousTipPosition, rawTipPos, smoothingFactor); //Get proper offset if object is rotated
        previousTipPosition = tipPosition;
        //tipPosition = trackedObject.position + trackedObject.rotation * tipOffset; //Get proper offset if object is rotated
        //rayTargetPoint = -(rayLength * trackedObject.up) + tipPosition;
        rayTargetPoint = tipPosition + direction * rayLength;

        lineRendererForDisplay .SetPosition(0, tipPosition);
        lineRendererForDisplay.SetPosition(1, rayTargetPoint);

        detectTipCollision(direction);
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            removeDistanceData();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (trackedObject == null) return;
        Vector3 gizmoTipPosition = trackedObject.position + trackedObject.rotation * tipOffset;//Get proper offset if object is rotated
        Gizmos.color = Color.red;
        Gizmos.DrawLine(gizmoTipPosition, gizmoTipPosition - trackedObject.up * rayLength);
        //Gizmos.DrawLine(trackedObject.position, -(rayLength * trackedObject.up)+trackedObject.position);
    }

    void detectTipCollision(Vector3 dir)
    {
        if (Physics.Raycast(tipPosition, dir, out RaycastHit hit, rayLength, spatialObjectLayer))
        {
            if (tipTouchingSurface)
            {
                return;
            }
            tipTouchingSurface = true;

            if(Time.time -lastPointTime < minTime)
            {
                return; 
            }
            lastPointTime = Time.time;
            if(firstPointRegistered && Vector3.Distance(hit.point, lastRegisteredPoint) < minPointDistance)
            {
                return;
            }
            if(!firstPointRegistered)
            {
                firstPointRegistered = true;
                secondPointRegestered = false;
                firstPointPostition = hit.point;
                lastRegisteredPoint = hit.point;
                point1PosDIsplayUI.text = firstPointPostition.ToString("F4");
                trackedObjectMesh.material = triggeredMaterial;
                //Debug.Log("<color=yellow> Triggerred with Spatial Awareness Mesh</color>");
            }
            else if(!secondPointRegestered)
            {
                firstPointRegistered = secondPointRegestered = true;
                secondPointPosition = hit.point;
                lastRegisteredPoint = hit.point;
                point2PosDIsplayUI.text = secondPointPosition.ToString("F4");
                if(firstPointRegistered && secondPointRegestered)
                {
                    calculateDistance(firstPointPostition, secondPointPosition);
                }
            }
        }
        else
        {
            tipTouchingSurface = false;
            trackedObjectMesh.material = defaultMaterial;
        }
    }

    float calculateDistance( Vector3 pointA, Vector3 pointB)
    {
        distance = Vector3.Distance(pointA, pointB);
        distance *= 100;
        DistanceTextUI.text = String.Format("{00:0000}" ,distance.ToString()+" cm");
        distanceCalculated = true;
        Debug.Log("<color=yellow>Distance = " + distance + "</color>");
        return distance;
    }

    void removeDistanceData()
    {
        firstPointPostition = secondPointPosition = lastRegisteredPoint = Vector3.zero;
        firstPointRegistered = secondPointRegestered = false;
        distance = 0;
        distanceCalculated = false;
        DistanceTextUI.text = "0 cm";
        point1PosDIsplayUI.text = "";
        point2PosDIsplayUI.text = "";
        trackedObjectMesh.material = defaultMaterial;
        Debug.LogError("Distance Data Reset");
        return;
    }
}
