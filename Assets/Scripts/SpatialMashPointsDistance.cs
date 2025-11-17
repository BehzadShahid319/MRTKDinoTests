using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpatialMashPointsDistance : MonoBehaviour
{
    [SerializeField] LineRenderer lineRendererForDisplay;
    [SerializeField] Transform trackedObject;
    [SerializeField] MeshRenderer trackedObjectMesh;
    [SerializeField] Vector3 tipOffset;
    [SerializeField] float rayLength;

    [SerializeField] Material defaultMaterial;
    [SerializeField] Material triggeredMaterial;

    [Header("=====UI=====")]
    [SerializeField] Text point1PosDIsplayUI;
    [SerializeField] Text point2PosDIsplayUI;
    [SerializeField] Text DistanceTextUI;

    Vector3 tipPosition;
    Vector3 rayTargetPoint;
    Vector3 direction;

    bool firstPointRegistered, secondPointRegestered = false;
    Vector3 firstPointPostition, secondPointPosition = Vector3.zero;
    bool distanceCalculated = false;
    float distance = 0;

    private void Start()
    {
        lineRendererForDisplay.useWorldSpace = true;
    }

    private void Update()
    {
        direction = -trackedObject.up;
        tipPosition = trackedObject.position + trackedObject.rotation * tipOffset; //Get proper offset if object is rotated
        //rayTargetPoint = -(rayLength * trackedObject.up) + tipPosition;
        rayTargetPoint = tipPosition + direction * rayLength;

        // Ray direction: stylus "down" (or forward, depending on your model)
        Vector3 rayEnd = tipPosition - trackedObject.up * rayLength;

        lineRendererForDisplay .SetPosition(0, tipPosition);
        lineRendererForDisplay.SetPosition(1, rayTargetPoint);

        detectTipCollision(direction);
    }

    private void OnDrawGizmosSelected()
    {
        tipPosition = trackedObject.position + trackedObject.rotation * tipOffset; //Get proper offset if object is rotated
        Gizmos.color = Color.red;
        Gizmos.DrawLine(tipPosition, tipPosition - trackedObject.up * rayLength);
        //Gizmos.DrawLine(trackedObject.position, -(rayLength * trackedObject.up)+trackedObject.position);
    }

    void detectTipCollision(Vector3 dir)
    {
        if (Physics.Raycast(tipPosition, dir, out RaycastHit hit, rayLength, LayerMask.GetMask("Spatial Awareness")))
        {
            if(!firstPointRegistered)
            {
                firstPointRegistered = true;
                secondPointRegestered = false;
                firstPointPostition = hit.point;
                point1PosDIsplayUI.text = firstPointPostition.ToString();
                trackedObjectMesh.material = triggeredMaterial;
                //Debug.Log("<color=yellow> Triggerred with Spatial Awareness Mesh</color>");
            }
            else if(!secondPointRegestered)
            {
                firstPointRegistered = secondPointRegestered = true;
                secondPointPosition = hit.point;
                point2PosDIsplayUI.text = secondPointPosition.ToString();
                if(firstPointRegistered && secondPointRegestered)
                {
                    calculateDistance(firstPointPostition, secondPointPosition);
                }
            }
        }
        else
        {
            if (distanceCalculated)
            {
                distanceCalculated = false;
            }
            trackedObjectMesh.material = defaultMaterial;
            //Debug.Log("<color=red> Triggerred Exit From SPatial Awareness Mesh</color>");
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            removeDistanceData();
        }
    }

    float calculateDistance( Vector3 pointA, Vector3 pointB)
    {
        distance = 0;
        distance = Vector3.Distance(pointA, pointB);
        distanceCalculated = true;
        firstPointPostition = secondPointPosition = Vector3.zero;
        firstPointRegistered = secondPointRegestered = false;
        Debug.Log("<color=yellow>Distance = " + distance + "</color>");
        DistanceTextUI.text = distance.ToString();
        return distance;
    }

    void removeDistanceData()
    {
        firstPointPostition = secondPointPosition = Vector3.zero;
        firstPointRegistered = secondPointRegestered = false;
        distance = 0;
        distanceCalculated = false;
        Debug.LogError("Distance Data Reset");
        return;
    }
}
