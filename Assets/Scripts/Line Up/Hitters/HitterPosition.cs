using System;

public enum HitterPosition
{
    LF, CF, RF, FB, SB, SS, TB, C, DH
}

public static class HitterPositionParser
{
    public static HitterPosition Parse(string positionStr)
    {
        switch (positionStr)
        {
            case "LF":
                return HitterPosition.LF;
            case "CF":
                return HitterPosition.CF;
            case "RF":
                return HitterPosition.RF;
            case "1B":
                return HitterPosition.FB;
            case "2B":
                return HitterPosition.SB;
            case "3B":
                return HitterPosition.TB;
            case "SS":
                return HitterPosition.SS;
            case "C":
                return HitterPosition.C;
            case "DH":
                return HitterPosition.DH;
            default:
                throw new ArgumentException($"[HitterPosition]: 알 수 없는 포지션: {positionStr}");
        }
    }

    public static bool TryParse(string positionStr, out HitterPosition result)
    {
        try
        {
            result = Parse(positionStr);
            return true;
        }
        catch (ArgumentException)
        {
            result = default;
            return false;
        }
    }
}