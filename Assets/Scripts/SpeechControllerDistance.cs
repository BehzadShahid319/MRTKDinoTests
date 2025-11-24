using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.HID;

public class SpeechControllerDistance : MonoBehaviour, IMixedRealitySpeechHandler
{
    public static SpeechControllerDistance Instance;

    [SerializeField] LineRenderer lineRendererForDisplay;
    [SerializeField] Transform trackedObject;
    [SerializeField] Transform tipOffset;

    [Header("VISUALS")]
    public GameObject point1Visual;
    public GameObject point2Visual;

    [Header("=====UI=====")]
    //[SerializeField] TextMeshProUGUI point1PosDIsplayUI;
    //[SerializeField] TextMeshProUGUI point2PosDIsplayUI;
    [SerializeField] TextMeshProUGUI DistanceTextUI;
    [SerializeField] RectTransform distanceUICanvas;

    bool firstPointRegistered, secondPointRegestered = false;
    Vector3 firstPointPostition, secondPointPosition, lastRegisteredPoint = Vector3.zero;

    bool distanceCalculated = false;
    float distance = 0;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnEnable()
    {
        CoreServices.InputSystem?.RegisterHandler<IMixedRealitySpeechHandler>(this);
    }

    private void OnDisable()
    {
        CoreServices.InputSystem?.UnregisterHandler<IMixedRealitySpeechHandler>(this);
    }

    public void OnSpeechKeywordRecognized(SpeechEventData eventData)
    {
        string cmd = eventData.Command.Keyword.ToLower();

        if (cmd == "record point")
        {
            GetPoint();
        }
        else if (cmd == "reset points")
        {
            resetDistanceData();
        }
    }


    [ContextMenu("GetPoint")]
    public void GetPoint()
    {
        if(trackedObject == null || !trackedObject.gameObject.activeSelf)
        {
            Debug.LogError("No Tracked Object Assigened or Detected!");
            return;
        }
        if (!firstPointRegistered)
        {
            firstPointRegistered = true;
            secondPointRegestered = false;
            firstPointPostition = tipOffset.position;
            lastRegisteredPoint = tipOffset.position;
            //point1PosDIsplayUI.text = firstPointPostition.ToString("F4");
            spawnPointRecordedVisual(point1Visual, firstPointPostition);
            //Debug.Log("<color=yellow> Triggerred with Spatial Awareness Mesh</color>");
        }
        else if (!secondPointRegestered)
        {
            firstPointRegistered = secondPointRegestered = true;
            secondPointPosition = tipOffset.position;
            lastRegisteredPoint = tipOffset.position;
            //point2PosDIsplayUI.text = secondPointPosition.ToString("F4");
            spawnPointRecordedVisual(point2Visual, secondPointPosition);
            if (firstPointRegistered && secondPointRegestered)
            {
                calculateDistance(firstPointPostition, secondPointPosition);
            }
        }
    }

    float calculateDistance(Vector3 pointA, Vector3 pointB)
    {
        lineRendererForDisplay.SetPosition(0, firstPointPostition);
        lineRendererForDisplay.SetPosition(1, secondPointPosition);
        lineRendererForDisplay.enabled = true;
        distance = Vector3.Distance(pointA, pointB);
        distance *= 100;
        DistanceTextUI.text = String.Format("{00:0000}", distance.ToString() + " cm");
        distanceCalculated = true;
        distanceUICanvas.position = (firstPointPostition + secondPointPosition) / 2;
        distanceUICanvas.position += (0.05f) * Vector3.up;
        DistanceTextUI.enabled = true;
        Debug.Log("<color=yellow>Distance = " + distance + "</color>");
        return distance;
    }

    [ContextMenu("ResetDistanceData")]
    public void resetDistanceData()
    {
        firstPointPostition = secondPointPosition = lastRegisteredPoint = Vector3.zero;
        firstPointRegistered = secondPointRegestered = false;
        distance = 0;
        distanceCalculated = false;
        DistanceTextUI.text = "0 cm";
        DistanceTextUI.enabled = false;
        lineRendererForDisplay.enabled = false;
        disableSpawnedPoints();
        //point1PosDIsplayUI.text = Vector3.zero.ToString();
        //point2PosDIsplayUI.text = Vector3.zero.ToString();
        //Debug.LogError("Distance Data Reset");
        return;
    }

    void spawnPointRecordedVisual(GameObject go, Vector3 pos)
    {
        go.transform.position = pos;
        go.SetActive(true);
    }

    void disableSpawnedPoints()
    {
        point1Visual.SetActive(false);
        point2Visual.SetActive(false);
    }
}
