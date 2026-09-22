using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICanvas : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _total;
    [SerializeField] private TextMeshProUGUI _defect;
    [SerializeField] private TextMeshProUGUI _good;

    [SerializeField] private Image _runImage;
    [SerializeField] private Image _alarmImage;
    [SerializeField] private Image _sensorImage;
    
    [SerializeField] private Image _pusherImage;
    [Header("Sensor State (External/Local)")]
    // 센서 감지 시 외부 스크립트에서 true/false로 갱신하거나 트리거로 변경
    public bool isSensorActive = false;

    // 램프 상태별 정의 색상
    private readonly Color _offColor     = new Color(0.25f, 0.25f, 0.25f, 1f); // 기본 어두운 회색
    private readonly Color _runColor     = new Color(0.15f, 0.85f, 0.15f, 1f); // 초록
    private readonly Color _alarmColor   = new Color(1.0f, 0.5f, 0.0f, 1f);    // 오렌지
    private readonly Color _sensorColor  = new Color(1.0f, 0.2f, 0.2f, 1f);    // 빨강
    private readonly Color _pusherColor  = new Color(1.0f, 0.9f, 0.1f, 1f);    // 노랑
    
    public void OnButtonClick_Connect()
    {
        PLCConnect.Instance.Connect();
    }
    
    private void Start()
    {
        // 시작 시 모든 램프를 기본 회색으로 초기화
        ResetAllLamps();
    }
    
    private void Update()
    {
        if (PLCConnect.Instance == null) return;

        UpdateCounterTexts();
        UpdateStatusLamps();
    }

    // 1. 수량 카운터 표시 갱신
    private void UpdateCounterTexts()
    {
        if (_total != null)
            _total.text = PLCConnect.Instance.totalCount.ToString();

        if (_defect != null)
            _defect.text = PLCConnect.Instance.rejectCount.ToString();

        if (_good != null)
            _good.text = PLCConnect.Instance.passCount.ToString();
    }
    private void UpdateStatusLamps()
    {
        // 가동 램프 (M10): 초록 / 회색
        if (_runImage != null)
        {
            _runImage.color = PLCConnect.Instance.isConveyorRun ? _runColor : _offColor;
        }

        // 알람 램프 (M2 비상정지): 오렌지 / 회색
        if (_alarmImage != null)
        {
            _alarmImage.color = PLCConnect.Instance.isEmergencyStop ? _alarmColor : _offColor;
        }

        // 푸셔 램프 (M30): 노랑 / 회색
        if (_pusherImage != null)
        {
            _pusherImage.color = PLCConnect.Instance.isPusherActive ? _pusherColor : _offColor;
        }

        // 입구 센서 램프: 빨강 / 회색
        if (_sensorImage != null)
        {
            _sensorImage.color = isSensorActive ? _sensorColor : _offColor;
        }
    }

    private void ResetAllLamps()
    {
        if (_runImage != null) _runImage.color = _offColor;
        if (_alarmImage != null) _alarmImage.color = _offColor;
        if (_sensorImage != null) _sensorImage.color = _offColor;
        if (_pusherImage != null) _pusherImage.color = _offColor;
    }
    
    // 입구 센서 트리거 시 UI 램프를 잠깐 켰다 끄는 헬퍼 메서드
    public void SetSensorState(bool active)
    {
        isSensorActive = active;
    }
}
