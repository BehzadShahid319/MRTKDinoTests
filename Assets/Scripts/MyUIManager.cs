using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyUIManager : MonoBehaviour
{
    [SerializeField] ResearchModeController researchModeController;
    [SerializeField] RMVisualiserUnity RMVisualiserUnity;
    [SerializeField] bool invokeVisualizersOnInit = true;

    private void Awake()
    {
        if(researchModeController == null)
        {
            researchModeController = FindAnyObjectByType<ResearchModeController>();
        }
        if(RMVisualiserUnity == null)
        {
            RMVisualiserUnity = FindAnyObjectByType<RMVisualiserUnity>();
        }
    }

    private void Start()
    {
        if (invokeVisualizersOnInit)
        {
            RMVisualiserUnity.ResetToFaceUser();
        }
    }

    public void toggleLineStyle()
    {
        if (StylusPolylineRecorder.Instance)
        {
            StylusPolylineRecorder.Instance.toggleLineStyle();
        }
    }

    public void QuitApp()
    {
        Application.Quit();
    }
}
