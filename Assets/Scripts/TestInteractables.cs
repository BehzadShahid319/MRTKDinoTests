using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestInteractables : MonoBehaviour
{
    [SerializeField] Transform childTo;
    [SerializeField] Rigidbody[] interactables;


    public void childAndReset()
    {
        foreach (var interactable in interactables)
        {
            interactable.transform.localPosition = Vector3.zero;
            interactable.velocity = Vector3.zero;
            interactable.angularVelocity = Vector3.zero;
            interactable.isKinematic = false;
            interactable.AddForce(Vector3.up * Random.Range(-5,5), ForceMode.Impulse);
            interactable.gameObject.SetActive(true);
        }
    }
}
