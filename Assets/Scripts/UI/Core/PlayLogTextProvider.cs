using System;
using UnityEngine;

/// <summary>
/// 타석·도루 로그를 중계 문장과 강조색으로 바꾼다 (명세 1-5 · 6-4)
/// </summary>
public static class PlayLogTextProvider
{
    /// <summary>
    /// 결과 11종의 한국어 표기
    /// </summary>
    public static string GetOutcomeText(BatterOutcome outcome)
    {
        return outcome switch
        {
            BatterOutcome.StrikeOut => "삼진",
            BatterOutcome.Walk => "볼넷",
            BatterOutcome.HomeRun => "홈런",
            BatterOutcome.Single => "안타",
            BatterOutcome.Double => "2루타",
            BatterOutcome.Triple => "3루타",
            BatterOutcome.Error => "실책 출루",
            BatterOutcome.FlyOut => "뜬공 아웃",
            BatterOutcome.GroundOut => "땅볼 아웃",
            BatterOutcome.DoublePlay => "병살타",
            BatterOutcome.SacrificeFly => "희생플라이",
            _ => throw new ArgumentException($"알 수 없는 타석 결과: {outcome}")
        };
    }

    /// <summary>
    /// 결과 강조 4단계 색 (평범 · 공격 · 수비 · 홈런)
    /// </summary>
    public static Color GetOutcomeColor(BatterOutcome outcome)
    {
        return outcome switch
        {
            BatterOutcome.HomeRun => UiPalette.LogHomeRun,
            BatterOutcome.Walk => UiPalette.LogOffense,
            BatterOutcome.Single => UiPalette.LogOffense,
            BatterOutcome.Double => UiPalette.LogOffense,
            BatterOutcome.Triple => UiPalette.LogOffense,
            BatterOutcome.StrikeOut => UiPalette.LogDefense,
            BatterOutcome.FlyOut => UiPalette.LogDefense,
            BatterOutcome.GroundOut => UiPalette.LogDefense,
            BatterOutcome.DoublePlay => UiPalette.LogDefense,
            BatterOutcome.Error => UiPalette.LogNormal,
            BatterOutcome.SacrificeFly => UiPalette.LogNormal,
            _ => UiPalette.LogNormal
        };
    }

    /// <summary>
    /// 2루타·3루타·병살은 한 단계 굵게 그린다
    /// </summary>
    public static bool IsBoldOutcome(BatterOutcome outcome)
    {
        return outcome == BatterOutcome.Double
            || outcome == BatterOutcome.Triple
            || outcome == BatterOutcome.DoublePlay
            || outcome == BatterOutcome.HomeRun;
    }

    /// <summary>
    /// 타석 중계 문장 (타자 이름 + 결과)
    /// </summary>
    public static string BuildBatterSentence(SimulationBatterLog log)
    {
        return log == null ? string.Empty : $"{log.BatterName} {GetOutcomeText(log.Outcome)}";
    }

    /// <summary>
    /// 도루 중계 문장 (명세 1-5의 4종)
    /// </summary>
    public static string BuildStealSentence(SimulationStealLog log)
    {
        if (log == null)
            return string.Empty;

        int targetBase = log.FromBase + 1;
        string result = log.IsSuccess ? "성공" : "실패";

        return $"{log.RunnerName} {targetBase}루 도루 {result}";
    }

    /// <summary>
    /// 도루 로그 색. 성공은 공격 강조, 실패는 수비 강조
    /// </summary>
    public static Color GetStealColor(SimulationStealLog log)
    {
        if (log == null)
            return UiPalette.LogNormal;

        return log.IsSuccess ? UiPalette.LogOffense : UiPalette.LogDefense;
    }

    /// <summary>
    /// 이닝 칩 표기 ("6▼"). 초는 ▲, 말은 ▼
    /// </summary>
    public static string GetInningChipText(int inning, bool isTopInning)
    {
        return inning + (isTopInning ? "▲" : "▼");
    }

    /// <summary>
    /// 이닝 구분 헤더 문장 ("3 ▲ 기아 공격")
    /// </summary>
    public static string BuildInningHeader(int inning, bool isTopInning, string attackTeamName)
    {
        string mark = isTopInning ? "▲" : "▼";

        return $"{inning} {mark}  {attackTeamName} 공격";
    }
}
