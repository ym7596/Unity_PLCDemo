using System;
using UnityEngine;

public class LampWork : MonoBehaviour
{
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private Material _onMaterial;
    [SerializeField] private Material _offMaterial;

    [SerializeField] private Light _pointLight;

    private void Awake()
    {
        _meshRenderer.material = _offMaterial;
    }

    public void SetLight(bool isOn)
    {
        if (isOn)
        {
            _meshRenderer.material = _onMaterial;
            _pointLight.enabled = true;
        }
        else
        {
            _meshRenderer.material = _offMaterial;
            _pointLight.enabled = false;
        }
    }
}
