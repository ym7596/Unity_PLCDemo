using System;
using UnityEngine;

public class BoxChecker : MonoBehaviour
{
    [SerializeField] private GameObject _light;
    [SerializeField] private UICanvas _uiCanvas;
    [SerializeField] private LampWork _lampWork;
    private void Start()
    {
        _light.SetActive(false);
    }

    private async void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Box"))
        {
            
        
            // 1. PLC의 M20(Coil 20) 번지로 0.1초 펄스 신호 전송 -> 총 수량(D10) 증가
            await PLCConnect.Instance.WriteCoilPulseAsync(32);
            _uiCanvas.SetSensorState(true);
            _lampWork.SetLight(true);
            // 2. 그 순간 PLC의 M25(불량 플래그)가 ON인지 체크
            if (PLCConnect.Instance.isDefectFlag)
            {
                // 상자 머티리얼을 빨간색으로 변경
                other.GetComponent<MeshRenderer>().material.color = Color.red;
                
               // other.gameObject.layer = LayerMask.NameToLayer($"DefectBox");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Box"))
        {
           // _light.SetActive(false);
           _lampWork.SetLight(false);
           _uiCanvas.SetSensorState(false);
        }
    }
}
