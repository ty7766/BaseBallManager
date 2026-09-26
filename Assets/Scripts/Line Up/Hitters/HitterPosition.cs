using System;

/// <summary>
/// 야수 포지션. CSV 표기 1B/2B/3B는 숫자로 시작할 수 없어 FB/SB/TB로 둔다
/// </summary>
public enum HitterPosition
{
    LF, CF, RF, FB, SB, SS, TB, C, DH
}

/// <summary>
/// 야수 포지션 문자열 파서
/// </summary>
public static class HitterPositionParser
{
    /// <summary>
    /// 포지션 문자열을 변환한다. 알 수 없는 표기면 <c>ArgumentException</c>
    /// </summary>
    public static HitterPosition Parse(string positionStr)
    {
        if (!TryParse(positionStr, out HitterPosition result))
            throw new ArgumentException($"[HitterPosition]: 알 수 없는 포지션: {positionStr}");

        return result;
    }

    /// <summary>
    /// 포지션 문자열 변환을 시도한다. 실패해도 예외를 던지지 않는다
    /// </summary>
    public static bool TryParse(string positionStr, out HitterPosition result)
    {
        switch (positionStr?.Trim())
        {
            case "LF": result = HitterPosition.LF; return true;
            case "CF": result = HitterPosition.CF; return true;
            case "RF": result = HitterPosition.RF; return true;
            case "1B": result = HitterPosition.FB; return true;
            case "2B": result = HitterPosition.SB; return true;
            case "3B": result = HitterPosition.TB; return true;
            case "SS": result = HitterPosition.SS; return true;
            case "C": result = HitterPosition.C; return true;
            case "DH": result = HitterPosition.DH; return true;
            default: result = default; return false;
        }
    }
}
