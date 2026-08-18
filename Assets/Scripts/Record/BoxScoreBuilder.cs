using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타석 로그를 박스스코어로 집계 (기획서 8.5)
/// </summary>
/// <remarks>
/// 시뮬 코어에 집계 책임을 두지 않고 사후 변환으로 분리했다.
/// 일괄 시뮬은 경기마다 박스스코어가 필요하지 않으므로, 필요한 경기에서만 호출해 낭비를 없앤다.
/// </remarks>
public static class BoxScoreBuilder
{
    /// <summary>
    /// 타석·도루 로그 -> 박스스코어. 로그가 없으면 null
    /// </summary>
    /// <param name="stealLogs">도루 기록 (기획서 8.3.1). 없으면 SB/CS가 0으로 남는다</param>
    public static BoxScore Build(IReadOnlyList<SimulationBatterLog> logs,
        IReadOnlyList<SimulationStealLog> stealLogs = null)
    {
        if (logs == null)
        {
            Debug.LogError("[BoxScoreBuilder]: 집계할 타석 로그가 없습니다");
            return null;
        }

        List<InningScore> innings = new List<InningScore>(9);

        //Dictionary는 조회용, List는 표시 순서용 (등장 순서를 유지해야 타순·등판 순서가 보존됨)
        TeamAccumulator away = new TeamAccumulator();
        TeamAccumulator home = new TeamAccumulator();

        int awayHits = 0, homeHits = 0;
        int awayErrors = 0, homeErrors = 0;

        foreach (SimulationBatterLog log in logs)
        {
            //초 = 원정 공격 / 홈 수비
            TeamAccumulator attack = log.IsTopInning ? away : home;
            TeamAccumulator defense = log.IsTopInning ? home : away;

            //① 라인스코어
            InningScore inningScore = GetOrCreateInning(innings, log.Inning);

            if (log.IsTopInning)
                inningScore.AddAwayRuns(log.RunsScored);
            else
                inningScore.AddHomeRuns(log.RunsScored);

            //② 타자 기록. 실책으로 난 득점은 타점으로 인정하지 않는다 (야구 기록 규칙)
            int runsBattedIn = log.Outcome == BatterOutcome.Error ? 0 : log.RunsScored;

            attack.GetHitter(log.BatterInstanceId, log.BatterName)
                .AddPlateAppearance(log.Outcome, runsBattedIn);

            //③ 득점한 주자들 (타자 본인이 홈런으로 밟은 경우도 여기 포함됨).
            //득점하려면 먼저 타석에 섰어야 하므로 이미 등록돼 있는 것이 정상이다
            foreach (int runnerId in log.ScoredRunnerIds)
            {
                if (!attack.TryAddRun(runnerId))
                    Debug.LogError($"[BoxScoreBuilder]: 타석 기록이 없는 주자가 득점했습니다 (instanceId {runnerId})");
            }

            //④ 투수 기록
            defense.GetPitcher(log.PitcherInstanceId, log.PitcherName)
                .AddBatterFaced(log.Outcome, log.PitchCount, log.RunsScored);

            //⑤ 팀 합계 (안타는 공격팀, 실책은 수비팀에 붙는다)
            if (IsHit(log.Outcome))
            {
                if (log.IsTopInning) awayHits++;
                else homeHits++;
            }

            if (log.Outcome == BatterOutcome.Error)
            {
                if (log.IsTopInning) homeErrors++;
                else awayErrors++;
            }
        }

        //⑥ 도루 (기획서 8.3.1). 타석 로그를 다 돌린 뒤에 처리한다 - 주자는 반드시 그 전에 타석에 섰으므로
        //이 시점이면 선수 항목이 이미 만들어져 있다
        if (stealLogs != null)
        {
            foreach (SimulationStealLog stealLog in stealLogs)
            {
                TeamAccumulator attack = stealLog.IsTopInning ? away : home;

                if (!attack.TryAddStealAttempt(stealLog.RunnerInstanceId, stealLog.IsSuccess))
                    Debug.LogError($"[BoxScoreBuilder]: 타석 기록이 없는 주자가 도루했습니다 (instanceId {stealLog.RunnerInstanceId})");
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
        //경기는 1회부터 순서대로 진행되므로 대개 마지막 칸이 정답이다
        while (innings.Count < inning)
        {
            innings.Add(new InningScore(innings.Count + 1));
        }

        return innings[inning - 1];
    }

    //안타로 집계되는 결과인지
    private static bool IsHit(BatterOutcome outcome)
    {
        return outcome == BatterOutcome.Single || outcome == BatterOutcome.Double
            || outcome == BatterOutcome.Triple || outcome == BatterOutcome.HomeRun;
    }

    /// <summary>
    /// 한 팀의 선수별 기록을 모으는 임시 그릇
    /// </summary>
    private class TeamAccumulator
    {
        public List<HitterGameStats> Hitters { get; } = new List<HitterGameStats>(9);
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

        //이미 타석에 선 적 있는 선수에게 득점을 붙인다. 모르는 주자면 false
        public bool TryAddRun(int instanceId)
        {
            if (!_hitterLookup.TryGetValue(instanceId, out HitterGameStats stats))
                return false;

            stats.AddRun();
            return true;
        }

        //이미 타석에 선 적 있는 선수에게 도루 기록을 붙인다. 모르는 주자면 false
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
