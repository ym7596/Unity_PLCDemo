using System.Globalization;
using UnityEngine;

public static class PLCAddressHelper
{
    /// <summary>
    /// 10진수 형태의 M 번호(예: 20, 25, 30)를 Modbus Coil 주소(32, 37, 48)로 변환
    /// </summary>
    public static ushort M(int mNumber)
    {
        int wordIndex = mNumber / 10; // 앞자리: 워드 번호
        int bitIndex  = mNumber % 10; // 뒷자리: 10진수 비트 번호 (0~9)

        return (ushort)((wordIndex * 16) + bitIndex);
    }

    /// <summary>
    /// 문자열 형태의 M 접점(예: "M20", "M25", "M1F")을 Modbus Coil 주소로 변환 (A~F 지원)
    /// </summary>
    public static ushort M(string mAddress)
    {
        // "M" 또는 "m" 제거
        string raw = mAddress.Trim().ToUpper().TrimStart('M');

        if (raw.Length == 0) return 0;

        if (raw.Length == 1)
        {
            // M0 ~ M9, MA ~ MF
            return ushort.Parse(raw, NumberStyles.HexNumber);
        }

        // 마지막 1자리는 16진수 비트 번호 (0~F)
        string bitHex = raw.Substring(raw.Length - 1, 1);
        int bit = int.Parse(bitHex, NumberStyles.HexNumber);

        // 앞부분은 10진수 워드 번호
        string wordStr = raw.Substring(0, raw.Length - 1);
        int word = int.Parse(wordStr);

        return (ushort)((word * 16) + bit);
    }
}
