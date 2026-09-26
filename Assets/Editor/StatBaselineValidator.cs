using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// StatBaseline 상수가 현재 카드 CSV의 평균과 맞는지 측정한다 (PROGRESS 2-1).
/// 게임 동작은 건드리지 않고 콘솔에 수치만 찍는다
/// </summary>
public static class StatBaselineValidator
{
    //이만큼 벗어나면 경고. 0.5점 = 편차 0.025 = 확률 약 0.25%p
    private const float AllowedDrift = 0.5f;

    //확률 실측 타석 수. 상수를 복제하지 않으려고 실제 계산기를 돌린다
    private const int ProbabilityTrials = 200000;

    private const int MeasureSeed = 20260926;

    //PitcherPower는 (구위 + 구속) / 2라, 두 상수를 동시에 만족하는 구속을 역산해야 편차가 0이 된다
    private const float BaselineVelocity = StatBaseline.PitcherPower * 2f - StatBaseline.PitcherStuff;

    //메뉴 진입점 - CSV를 읽어 상수와 대조하고 확률 영향까지 실측한다
    [MenuItem("Tools/BaseBallManager/StatBaseline 드리프트 측정")]
    public static void Measure()
    {
        CardCSVLoader loader = new CardCSVLoader();
        List<HitterMasterData> hitters = loader.LoadHitters();
        List<PitcherMasterData> pitchers = loader.LoadPitchers();

        if (hitters.Count == 0 || pitchers.Count == 0)
        {
            Debug.LogError($"[StatBaselineValidator] 카드 CSV를 읽지 못했습니다 (타자 {hitters.Count} / 투수 {pitchers.Count})");
            return;
        }

        PoolAverage pool = new PoolAverage(hitters, pitchers);
        List<StatLine> lines = BuildStatLines(pool);

        StringBuilder report = new StringBuilder(2048);
        report.AppendLine($"[StatBaselineValidator] 타자 {hitters.Count}장 / 투수 {pitchers.Count}장 기준");
        report.AppendLine();

        AppendStatLines(report, lines);
        AppendProbabilityComparison(report, pool);
        AppendNeededCardCount(report, lines);

        float worstDrift = 0f;

        foreach (StatLine line in lines)
        {
            if (Mathf.Abs(line.Drift) > Mathf.Abs(worstDrift))
                worstDrift = line.Drift;
        }

        if (Mathf.Abs(worstDrift) > AllowedDrift)
        {
            report.AppendLine($"판정: 어긋남 (최대 {worstDrift:+0.00;-0.00}점 / 허용 ±{AllowedDrift})");
            Debug.LogWarning(report.ToString());
            return;
        }

        report.AppendLine($"판정: 통과 (최대 {worstDrift:+0.00;-0.00}점 / 허용 ±{AllowedDrift})");
        Debug.Log(report.ToString());
    }

    //상수 7개를 현재 평균·5성 평균과 묶는다
    private static List<StatLine> BuildStatLines(PoolAverage pool)
    {
        return new List<StatLine>(7)
        {
            new StatLine("HitterPower", StatBaseline.HitterPower, pool.HitterPower, pool.FiveStarHitterPower, pool.HitterCount),
            new StatLine("HitterContact", StatBaseline.HitterContact, pool.HitterContact, pool.FiveStarHitterContact, pool.HitterCount),
            new StatLine("HitterRun", StatBaseline.HitterRun, pool.HitterRun, pool.FiveStarHitterRun, pool.HitterCount),
            new StatLine("HitterDefense", StatBaseline.HitterDefense, pool.HitterDefense, pool.FiveStarHitterDefense, pool.HitterCount),
            new StatLine("PitcherPower", StatBaseline.PitcherPower, pool.PitcherPower, pool.FiveStarPitcherPower, pool.PitcherCount),
            new StatLine("PitcherStuff", StatBaseline.PitcherStuff, pool.PitcherStuff, pool.FiveStarPitcherStuff, pool.PitcherCount),
            new StatLine("PitcherControl", StatBaseline.PitcherControl, pool.PitcherControl, pool.FiveStarPitcherControl, pool.PitcherCount),
        };
    }

    //상수 대 실제 평균 표
    private static void AppendStatLines(StringBuilder report, List<StatLine> lines)
    {
        report.AppendLine("── 상수 vs 현재 카드풀 평균 ──");

        foreach (StatLine line in lines)
        {
            string mark = Mathf.Abs(line.Drift) > AllowedDrift ? "!" : " ";

            report.AppendLine($" {mark} {line.Name,-15} 상수 {line.Constant,5:0.#}   실제 {line.Actual,6:0.00}"
                + $"   차이 {line.Drift,6:+0.00;-0.00}   평균 카드의 편차 {line.Edge,6:+0.000;-0.000}");
        }

        report.AppendLine();
    }

    //같은 대결을 두 번 돌려 드리프트가 확률을 얼마나 밀어내는지 본다
    private static void AppendProbabilityComparison(StringBuilder report, PoolAverage pool)
    {
        Tally current = Simulate(
            new HitterSnapshot(1, "평균", Round(pool.HitterPower), Round(pool.HitterContact), Round(pool.HitterRun), Round(pool.HitterDefense)),
            new PitcherSnapshot(2, "평균", Round(pool.PitcherVelocity), Round(pool.PitcherStuff), Round(pool.PitcherControl), 100),
            pool.HitterDefense / 100f);

        Tally baseline = Simulate(
            new HitterSnapshot(1, "기준", Round(StatBaseline.HitterPower), Round(StatBaseline.HitterContact), Round(StatBaseline.HitterRun), Round(StatBaseline.HitterDefense)),
            new PitcherSnapshot(2, "기준", Round(BaselineVelocity), Round(StatBaseline.PitcherStuff), Round(StatBaseline.PitcherControl), 100),
            StatBaseline.HitterDefense / 100f);

        report.AppendLine($"── 평균 카드끼리 붙였을 때 ({ProbabilityTrials:N0}타석 실측) ──");
        report.AppendLine("   (기준 = 편차가 정확히 0인 가상의 카드. 둘이 같아야 튜닝 상수가 제 값을 낸다)");
        report.AppendLine($"   {"",-14}{"현재 카드풀",12}{"기준",10}{"차이",12}");

        AppendRateRow(report, "삼진", current.StrikeOutRate, baseline.StrikeOutRate);
        AppendRateRow(report, "볼넷", current.WalkRate, baseline.WalkRate);
        AppendRateRow(report, "홈런", current.HomeRunRate, baseline.HomeRunRate);
        AppendRateRow(report, "인플레이 안타", current.HitRate, baseline.HitRate);
        AppendRateRow(report, "장타 비중", current.ExtraBaseShare, baseline.ExtraBaseShare);

        report.AppendLine();
    }

    //비율 한 줄. 각 단계는 앞 단계를 통과한 타석만 분모로 쓴다 (코드 분기 순서와 같다)
    private static void AppendRateRow(StringBuilder report, string label, float current, float baseline)
    {
        report.AppendLine($"   {label,-14}{current,11:P2}{baseline,11:P2}{(current - baseline),11:+0.00%;-0.00%}");
    }

    //상수에 도달하려면 5성급이 몇 장 더 필요한지
    private static void AppendNeededCardCount(StringBuilder report, List<StatLine> lines)
    {
        report.AppendLine("── 상수에 맞추려면 5성급이 몇 장 더 필요한가 ──");
        report.AppendLine("   (시그·골글은 5성 고정이라 현재 5성 평균으로 들어온다고 가정)");

        foreach (StatLine line in lines)
        {
            if (line.Drift >= 0f)
            {
                report.AppendLine($"   {line.Name,-15} 이미 상수 이상 (추가 불필요)");
                continue;
            }

            if (line.FiveStarAverage <= line.Constant)
            {
                report.AppendLine($"   {line.Name,-15} 5성 평균({line.FiveStarAverage:0.00})이 상수보다 낮아 추가로는 도달 불가");
                continue;
            }

            int needed = Mathf.CeilToInt(-line.Drift * line.PoolCount / (line.FiveStarAverage - line.Constant));

            report.AppendLine($"   {line.Name,-15} 5성 평균 {line.FiveStarAverage,6:0.00}   →   {needed,4}장");
        }

        report.AppendLine();
    }

    //평균 카드끼리 타석을 반복해 단계별 발생 비율을 센다
    private static Tally Simulate(HitterSnapshot hitter, PitcherSnapshot pitcher, float averageDefense)
    {
        Random.State previousState = Random.state;
        Random.InitState(MeasureSeed);

        GameState state = new GameState(BuildContext(hitter, pitcher));
        Tally tally = new Tally();
        BatterOutcomeCalculator calculator = new BatterOutcomeCalculator();

        for (int i = 0; i < ProbabilityTrials; i++)
            tally.Add(calculator.Calculate(state, hitter, pitcher, averageDefense));

        Random.state = previousState;

        return tally;
    }

    //주자 없는 0아웃 상황을 만든다. 병살·희생플라이를 빼고 기본 확률만 재기 위함
    private static SimulationContext BuildContext(HitterSnapshot hitter, PitcherSnapshot pitcher)
    {
        HitterSnapshot[] lineup = new HitterSnapshot[SimulationContext.LineupSize];
        HitterSnapshot[] opponentLineup = new HitterSnapshot[SimulationContext.LineupSize];

        for (int i = 0; i < SimulationContext.LineupSize; i++)
        {
            lineup[i] = hitter;
            opponentLineup[i] = hitter;
        }

        PitcherSnapshot[] staff = new PitcherSnapshot[SimulationContext.PitcherSlotCount];
        PitcherSnapshot[] opponentStaff = new PitcherSnapshot[SimulationContext.PitcherSlotCount];

        for (int i = 0; i < SimulationContext.PitcherSlotCount; i++)
        {
            staff[i] = pitcher;
            opponentStaff[i] = pitcher;
        }

        return new SimulationContext(true, lineup, opponentLineup,
            System.Array.Empty<HitterSnapshot>(), System.Array.Empty<HitterSnapshot>(), staff, opponentStaff);
    }

    private static int Round(float value)
    {
        return Mathf.RoundToInt(value);
    }

    /// <summary>
    /// 상수 1개의 측정 결과
    /// </summary>
    private readonly struct StatLine
    {
        public string Name { get; }
        public float Constant { get; }
        public float Actual { get; }
        public float FiveStarAverage { get; }
        public int PoolCount { get; }

        public float Drift => Actual - Constant;
        public float Edge => StatBaseline.GetEdge(Actual, Constant);

        public StatLine(string name, float constant, float actual, float fiveStarAverage, int poolCount)
        {
            Name = name;
            Constant = constant;
            Actual = actual;
            FiveStarAverage = fiveStarAverage;
            PoolCount = poolCount;
        }
    }

    /// <summary>
    /// 타석 결과 집계. 각 단계는 앞 단계를 통과한 타석만 분모로 쓴다
    /// </summary>
    private struct Tally
    {
        private int _plateAppearances;
        private int _strikeOuts;
        private int _walks;
        private int _homeRuns;
        private int _errors;
        private int _hits;
        private int _extraBaseHits;

        public float StrikeOutRate => Share(_strikeOuts, _plateAppearances);
        public float WalkRate => Share(_walks, _plateAppearances - _strikeOuts);
        public float HomeRunRate => Share(_homeRuns, _plateAppearances - _strikeOuts - _walks);
        public float HitRate => Share(_hits, _plateAppearances - _strikeOuts - _walks - _homeRuns - _errors);
        public float ExtraBaseShare => Share(_extraBaseHits, _hits);

        public void Add(BatterOutcome outcome)
        {
            _plateAppearances++;

            switch (outcome)
            {
                case BatterOutcome.StrikeOut:
                    _strikeOuts++;
                    break;
                case BatterOutcome.Walk:
                    _walks++;
                    break;
                case BatterOutcome.HomeRun:
                    _homeRuns++;
                    break;
                case BatterOutcome.Error:
                    _errors++;
                    break;
                case BatterOutcome.Single:
                    _hits++;
                    break;
                case BatterOutcome.Double:
                case BatterOutcome.Triple:
                    _hits++;
                    _extraBaseHits++;
                    break;
            }
        }

        private static float Share(int part, int total)
        {
            return total <= 0 ? 0f : (float)part / total;
        }
    }

    /// <summary>
    /// 카드풀 전체·5성 평균 스탯
    /// </summary>
    private readonly struct PoolAverage
    {
        public int HitterCount { get; }
        public int PitcherCount { get; }

        public float HitterPower { get; }
        public float HitterContact { get; }
        public float HitterRun { get; }
        public float HitterDefense { get; }

        public float PitcherVelocity { get; }
        public float PitcherStuff { get; }
        public float PitcherControl { get; }
        public float PitcherPower { get; }

        public float FiveStarHitterPower { get; }
        public float FiveStarHitterContact { get; }
        public float FiveStarHitterRun { get; }
        public float FiveStarHitterDefense { get; }

        public float FiveStarPitcherStuff { get; }
        public float FiveStarPitcherControl { get; }
        public float FiveStarPitcherPower { get; }

        public PoolAverage(List<HitterMasterData> hitters, List<PitcherMasterData> pitchers)
        {
            HitterCount = hitters.Count;
            PitcherCount = pitchers.Count;

            long power = 0, contact = 0, run = 0, defense = 0;
            long fivePower = 0, fiveContact = 0, fiveRun = 0, fiveDefense = 0;
            int fiveHitterCount = 0;

            foreach (HitterMasterData hitter in hitters)
            {
                power += hitter.Power;
                contact += hitter.Contact;
                run += hitter.Run;
                defense += hitter.Defense;

                if (hitter.CardGrade != CardGrade.Star5)
                    continue;

                fivePower += hitter.Power;
                fiveContact += hitter.Contact;
                fiveRun += hitter.Run;
                fiveDefense += hitter.Defense;
                fiveHitterCount++;
            }

            long velocity = 0, stuff = 0, control = 0;
            long fiveVelocity = 0, fiveStuff = 0, fiveControl = 0;
            int fivePitcherCount = 0;

            foreach (PitcherMasterData pitcher in pitchers)
            {
                velocity += pitcher.Velocity;
                stuff += pitcher.Stuff;
                control += pitcher.Control;

                if (pitcher.CardGrade != CardGrade.Star5)
                    continue;

                fiveVelocity += pitcher.Velocity;
                fiveStuff += pitcher.Stuff;
                fiveControl += pitcher.Control;
                fivePitcherCount++;
            }

            HitterPower = Mean(power, HitterCount);
            HitterContact = Mean(contact, HitterCount);
            HitterRun = Mean(run, HitterCount);
            HitterDefense = Mean(defense, HitterCount);

            PitcherVelocity = Mean(velocity, PitcherCount);
            PitcherStuff = Mean(stuff, PitcherCount);
            PitcherControl = Mean(control, PitcherCount);
            PitcherPower = (PitcherVelocity + PitcherStuff) * 0.5f;

            FiveStarHitterPower = Mean(fivePower, fiveHitterCount);
            FiveStarHitterContact = Mean(fiveContact, fiveHitterCount);
            FiveStarHitterRun = Mean(fiveRun, fiveHitterCount);
            FiveStarHitterDefense = Mean(fiveDefense, fiveHitterCount);

            FiveStarPitcherStuff = Mean(fiveStuff, fivePitcherCount);
            FiveStarPitcherControl = Mean(fiveControl, fivePitcherCount);
            FiveStarPitcherPower = (Mean(fiveVelocity, fivePitcherCount) + FiveStarPitcherStuff) * 0.5f;
        }

        private static float Mean(long sum, int count)
        {
            return count <= 0 ? 0f : (float)sum / count;
        }
    }
}
