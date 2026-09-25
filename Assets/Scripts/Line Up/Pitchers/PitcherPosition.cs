using System;

/// <summary>
/// 투수 보직 (선발 / 불펜 / 마무리)
/// </summary>
public enum PitcherPosition
{
    SP, RP, CP
}

/// <summary>
/// 투수 보직 문자열 파서
/// </summary>
public static class PitcherPositionParser
{
    /// <summary>
    /// 보직 문자열을 변환한다. 알 수 없는 표기면 <c>ArgumentException</c>
    /// </summary>
    public static PitcherPosition Parse(string positionStr)
    {
        if (!TryParse(positionStr, out PitcherPosition result))
            throw new ArgumentException($"[PitcherPosition]: 알 수 없는 포지션: {positionStr}");

        return result;
    }

    /// <summary>
    /// 보직 문자열 변환을 시도한다. 실패해도 예외를 던지지 않는다
    /// </summary>
    public static bool TryParse(string positionStr, out PitcherPosition result)
    {
        switch (positionStr?.Trim())
        {
            case "SP": result = PitcherPosition.SP; return true;
            case "RP": result = PitcherPosition.RP; return true;
            case "CP": result = PitcherPosition.CP; return true;
            default: result = default; return false;
        }
    }
}
