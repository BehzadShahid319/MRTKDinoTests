using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtObject : MonoBehaviour
{
    [SerializeField] bool updateOnAwakeOnly = false;
    [SerializeField] GameObject targetOBJ;

    private void Awake()
    {
        if (targetOBJ == null)
        {
            targetOBJ = Camera.main.gameObject;
        }
        if (updateOnAwakeOnly)
            updateLookAt();
    }

    void LateUpdate()
    {
        if (updateOnAwakeOnly) return;
        updateLookAt();
    }

    public void updateLookAt()
    {
        if (targetOBJ == null) return;

        // Option 1: Simple LookAt (most common)
        transform.LookAt(transform.position + targetOBJ.transform.rotation * Vector3.forward,
                         targetOBJ.transform.rotation * Vector3.up);

        // Optional:
        // If canvas flips or rotates incorrectly, use:
        //transform.forward = -targetOBJ.transform.forward;
    }
}
