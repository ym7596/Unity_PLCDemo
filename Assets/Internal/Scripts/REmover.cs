using System;
using UnityEngine;

public class REmover : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Box"))
        {
            Destroy(other.gameObject);
        }
    }
}
