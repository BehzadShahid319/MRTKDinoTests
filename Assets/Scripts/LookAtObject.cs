using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtObject : MonoBehaviour
{
    [SerializeField] GameObject targetOBJ;

    void LateUpdate()
    {
        if (targetOBJ == null) return;

        // Option 1: Simple LookAt (most common)
        transform.LookAt(transform.position + targetOBJ.transform.rotation * Vector3.forward,
                         targetOBJ.transform.rotation * Vector3.up);

        // Optional:
        // If your canvas flips or rotates incorrectly, use:
        transform.forward = -targetOBJ.transform.forward;
    }
}
