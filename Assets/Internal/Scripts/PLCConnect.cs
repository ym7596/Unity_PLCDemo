using System;
using System.Globalization;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using NModbus;

public class PLCConnect : MonoBehaviour
{
    public static PLCConnect Instance { get; private set; }

    [Header("Network Settings")]
    [SerializeField] private string ipAddress = "127.0.0.1";
    [SerializeField] private int port = 502;
    [SerializeField] private byte unitId = 1;
    [SerializeField] private int pollIntervalMs = 50; // 50ms 주기 폴링

    [Header("PLC Monitor (Read Only)")]
    public bool isEmergencyStop = false; // M0002
    public bool isConveyorRun   = false; // M0010
    public bool isDefectFlag    = false; // M0025
    public bool isPusherActive  = false; // M0030
    public ushort totalCount    = 0;     // D00010
    public ushort rejectCount   = 0;     // D00011
    public ushort passCount     = 0;     // D00012

    public bool isSpawnTrigger = false;

    private TcpClient tcpClient;
    private IModbusMaster master;
    private CancellationTokenSource cts;
    private bool isConnected = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /*private async void Start()
    {
        cts = new CancellationTokenSource();
        await ConnectAsync();

        if (isConnected)
        {
            // 실시간 폴링 루프 시작
            _ = PollingLoopAsync(cts.Token);
        }
    }*/

    public async void Connect()
    {
        if (isConnected) return;
        cts = new CancellationTokenSource();
         await ConnectAsync();

        if (isConnected)
        {
            // 실시간 폴링 루프 시작
            _ = PollingLoopAsync(cts.Token);
        }
    }

    private async Task ConnectAsync()
    {
        try
        {
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ipAddress, port);

            var factory = new ModbusFactory();
            master = factory.CreateMaster(tcpClient);

            isConnected = true;
            Debug.Log("[Modbus] PLC 통신 연결 성공!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Modbus] 연결 실패: {ex.Message}");
            isConnected = false;
        }
    }

    // 주기적으로 PLC의 코일(M)과 레지스터(D)를 읽어오는 루프
    private async Task PollingLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && isConnected)
        {
            try
            {
                // 1. 코일 읽기: 0번부터 64개 일괄 수신 (M0000 ~ M003F 커버)
                bool[] coils = await master.ReadCoilsAsync(unitId, 0, 64);

                // M 접점 번호를 그대로 넣어 값 갱신
                isEmergencyStop = coils[ConvertM(2)];   // M0002 -> 2번 인덱스
                isConveyorRun   = coils[ConvertM(10)];  // M0010 -> 16번 인덱스
                isDefectFlag    = coils[ConvertM(25)];  // M0025 -> 37번 인덱스
                isPusherActive  = coils[ConvertM(30)];  // M0030 -> 48번 인덱스
                isSpawnTrigger  = coils[ConvertM(15)];// <-- M15 추가 (1번째 워드의 5번 비트 -> 16+5=21번)
                // 2. 홀딩 레지스터 읽기: D10부터 3개 수신 (D10, D11, D12)
                ushort[] registers = await master.ReadHoldingRegistersAsync(unitId, 100, 3);

                totalCount  = registers[0]; // M0100 (총수량)
                rejectCount = registers[1]; // M0101 (불량수량)
                passCount   = registers[2]; // M0102 (양품수량)
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Modbus] 데이터 수신 에러: {ex.Message}");
            }

            await Task.Delay(pollIntervalMs, token);
        }
    }

    #region M접점 변환 함수 (XG5000 16진수 워드 -> Modbus 번지)

    /// <summary>
    /// 숫자 입력 (예: 20 입력 시 M0020의 Modbus 번지인 32 반환)
    /// </summary>
    public static ushort ConvertM(int mNumber)
    {
        int wordIndex = mNumber / 10;
        int bitIndex  = mNumber % 10;
        return (ushort)((wordIndex * 16) + bitIndex);
    }

    /// <summary>
    /// 문자열 입력 (예: "M20", "M1F" 지원)
    /// </summary>
    public static ushort ConvertM(string mAddress)
    {
        string raw = mAddress.Trim().ToUpper().TrimStart('M');
        if (raw.Length == 0) return 0;

        if (raw.Length == 1)
        {
            return ushort.Parse(raw, NumberStyles.HexNumber);
        }

        string bitHex = raw.Substring(raw.Length - 1, 1);
        int bit = int.Parse(bitHex, NumberStyles.HexNumber);

        string wordStr = raw.Substring(0, raw.Length - 1);
        int word = int.Parse(wordStr);

        return (ushort)((word * 16) + bit);
    }

    #endregion

    #region 코일 쓰기 함수

    /// <summary>
    /// M 접점에 펄스(ON 후 지정 시간 뒤 OFF) 신호 전송 (예: SendPulseM(20))
    /// </summary>
    public async Task SendPulseM(int mNumber, int pulseDurationMs = 100)
    {
        ushort address = ConvertM(mNumber);
        await WriteCoilPulseAsync(address, pulseDurationMs);
    }

    public async Task WriteCoilPulseAsync(ushort address, int pulseDurationMs = 100)
    {
        if (!isConnected || master == null) return;

        try
        {
            await master.WriteSingleCoilAsync(unitId, address, true);
            await Task.Delay(pulseDurationMs);
            await master.WriteSingleCoilAsync(unitId, address, false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Modbus] 코일 펄스 전송 실패 (Address {address}): {ex.Message}");
        }
    }

    /// <summary>
    /// M 접점에 직접 True/False 쓰기 (예: WriteM(22, true))
    /// </summary>
    public async Task WriteM(int mNumber, bool value)
    {
        ushort address = ConvertM(mNumber);
        await WriteCoilAsync(address, value);
    }

    public async Task WriteCoilAsync(ushort address, bool value)
    {
        if (!isConnected || master == null) return;

        try
        {
            await master.WriteSingleCoilAsync(unitId, address, value);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Modbus] 코일 쓰기 실패 (Address {address}): {ex.Message}");
        }
    }

    #endregion

    private void Disconnect()
    {
        cts?.Cancel();
        cts?.Dispose();
        cts = null;

        master?.Dispose();
        master = null;

        if (tcpClient != null)
        {
            tcpClient.Close();
            tcpClient.Dispose();
            tcpClient = null;
        }

        isConnected = false;
        Debug.Log("[Modbus] 연결 정상 종료.");
    }

    private void OnDestroy()
    {
        Disconnect();
    }
}