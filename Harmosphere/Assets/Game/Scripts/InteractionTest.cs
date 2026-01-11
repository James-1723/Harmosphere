using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionTest : MonoBehaviour
{
    public Material mat;

    void OnTriggerEnter(Collider other)
    {
        if (!(other.gameObject.name.Contains("handMesh") || 
               other.transform.root.name.Contains("OVRHand") ||
               other.CompareTag("Hand") ||
               other.gameObject.layer == LayerMask.NameToLayer("Hand")||other.gameObject.name.Contains("Hand"))) return;
               Debug.LogError($"Touched - {other.gameObject.name}");
        GetComponent<Renderer>().material = mat;
    }    
}