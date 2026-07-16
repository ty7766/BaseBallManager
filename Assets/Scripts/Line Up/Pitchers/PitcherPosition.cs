using System;

public enum PitcherPosition
{
    SP, RP, CP
}

public static class PitcherPositionParser
{
    public static PitcherPosition Parse(string positionStr)
    {
        switch (positionStr)
        {
            case "SP":
                return PitcherPosition.SP;
            case "RP":
                return PitcherPosition.RP;
            case "CP":
                return PitcherPosition.CP;
            default:
                throw new ArgumentException($"[PitcherPosition]: 알 수 없는 포지션: {positionStr}");
        }
    }

    public static bool TryParse(string positionStr, out PitcherPosition result)
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