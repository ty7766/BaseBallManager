using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타석·도루 로그를 박스스코어로 사후 집계한다. 필요한 경기에서만 호출한다
/// </summary>
public static class BoxScoreBuilder
{
    /// <summary>
    /// 타석·도루 로그 -> 박스스코어. 로그가 없으면 null
    /// </summary>
    /// <param name="stealLogs">도루 기록. 없으면 SB/CS가 0으로 남는다</param>
    public static BoxScore Build(IReadOnlyList<SimulationBatterLog> logs,
        IReadOnlyList<SimulationStealLog> stealLogs = null)
    {
        if (logs == null)
        {
            Debug.LogError("[BoxScoreBuilder]: 집계할 타석 로그가 없습니다");
            return null;
        }

        List<InningScore> innings = new List<InningScore>(GameState.RegulationInnings);

        TeamAccumulator away = new TeamAccumulator();
        TeamAccumulator home = new TeamAccumulator();

        int awayHits = 0, homeHits = 0;
        int awayErrors = 0, homeErrors = 0;

        foreach (SimulationBatterLog log in logs)
        {
            TeamAccumulator attack = log.IsTopInning ? away : home;
            TeamAccumulator defense = log.IsTopInning ? home : away;

            InningScore inningScore = GetOrCreateInning(innings, log.Inning);

            if (log.IsTopInning)
                inningScore.AddAwayRuns(log.RunsScored);
            else
                inningScore.AddHomeRuns(log.RunsScored);

            int runsBattedIn = log.Outcome == BatterOutcome.Error ? 0 : log.RunsScored;

            attack.GetHitter(log.BatterInstanceId, log.BatterName)
                .AddPlateAppearance(log.Outcome, runsBattedIn);

            foreach (int runnerId in log.ScoredRunnerIds)
            {
                if (!attack.TryAddRun(runnerId))
                    Debug.LogError($"[BoxScoreBuilder]: 타석 기록이 없는 주자가 득점했습니다 (instanceId {runnerId})");
            }

            defense.GetPitcher(log.PitcherInstanceId, log.PitcherName)
                .AddBatterFaced(log.Outcome, log.PitchCount, log.RunsScored);

            if (IsHit(log.Outcome))
            {
                if (log.IsTopInning)
                    awayHits++;
                else
                    homeHits++;
            }

            if (log.Outcome == BatterOutcome.Error)
            {
                if (log.IsTopInning)
                    homeErrors++;
                else
                    awayErrors++;
            }
        }

        if (stealLogs != null)
        {
            foreach (SimulationStealLog stealLog in stealLogs)
            {
                TeamAccumulator attack = stealLog.IsTopInning ? away : home;

                if (!attack.TryAddStealAttempt(stealLog.RunnerInstanceId, stealLog.IsSuccess))
                    Debug.LogError($"[BoxScoreBuilder]: 타석 기록이 없는 주자가 도루했습니다 (instanceId {stealLog.RunnerInstanceId})");

                if (stealLog.IsSuccess)
                    continue;

                TeamAccumulator defense = stealLog.IsTopInning ? home : away;

                defense.GetPitcher(stealLog.PitcherInstanceId, stealLog.PitcherName).AddCaughtStealingOut();
            }
        }

        int awayRuns = 0, homeRuns = 0;

        foreach (InningScore inning in innings)
        {
            awayRuns += inning.AwayRuns;
            homeRuns += inning.HomeRuns;
        }

        return new BoxScore(innings, awayRuns, homeRuns, awayHits, homeHits, awayErrors, homeErrors,
            away.Hitters, home.Hitters, away.Pitchers, home.Pitchers);
    }

    //해당 이닝 칸을 찾거나, 없으면 그 이닝까지 칸을 만들어 채운다
    private static InningScore GetOrCreateInning(List<InningScore> innings, int inning)
    {
        while (innings.Count < inning)
            innings.Add(new InningScore(innings.Count + 1));

        return innings[inning - 1];
    }

    //안타로 집계되는 결과인지
    private static bool IsHit(BatterOutcome outcome)
    {
        return outcome == BatterOutcome.Single || outcome == BatterOutcome.Double
            || outcome == BatterOutcome.Triple || outcome == BatterOutcome.HomeRun;
    }

    //한 팀의 선수별 기록을 모으는 임시 그릇. List는 등장 순서(타순·등판 순서) 보존용이다
    private class TeamAccumulator
    {
        public List<HitterGameStats> Hitters { get; } = new List<HitterGameStats>(SimulationContext.LineupSize);
        public List<PitcherGameStats> Pitchers { get; } = new List<PitcherGameStats>(4);

        private readonly Dictionary<int, HitterGameStats> _hitterLookup = new Dictionary<int, HitterGameStats>();
        private readonly Dictionary<int, PitcherGameStats> _pitcherLookup = new Dictionary<int, PitcherGameStats>();

        public HitterGameStats GetHitter(int instanceId, string name)
        {
            if (_hitterLookup.TryGetValue(instanceId, out HitterGameStats stats))
                return stats;

            stats = new HitterGameStats(instanceId, name);

            _hitterLookup.Add(instanceId, stats);
            Hitters.Add(stats);

            return stats;
        }

        /// <summary>
        /// 이미 타석에 선 적 있는 선수에게 득점을 붙인다. 모르는 주자면 false
        /// </summary>
        public bool TryAddRun(int instanceId)
        {
            if (!_hitterLookup.TryGetValue(instanceId, out HitterGameStats stats))
                return false;

            stats.AddRun();
            return true;
        }

        /// <summary>
        /// 이미 타석에 선 적 있는 선수에게 도루 기록을 붙인다. 모르는 주자면 false
        /// </summary>
        public bool TryAddStealAttempt(int instanceId, bool isSuccess)
        {
            if (!_hitterLookup.TryGetValue(instanceId, out HitterGameStats stats))
                return false;

            stats.AddStealAttempt(isSuccess);
            return true;
        }

        public PitcherGameStats GetPitcher(int instanceId, string name)
        {
            if (_pitcherLookup.TryGetValue(instanceId, out PitcherGameStats stats))
                return stats;

            stats = new PitcherGameStats(instanceId, name);

            _pitcherLookup.Add(instanceId, stats);
            Pitchers.Add(stats);

            return stats;
        }
    }
}
