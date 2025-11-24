using Microsoft.MixedReality.Toolkit.Experimental.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RecordedPoint : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI positionText;

    private void OnEnable()
    {
        positionText.text = transform.position.ToString("F4");
    }
}
