using System.Collections.Generic;

/// <summary>
/// 경기 진행 중 바뀌는 모든 상태 (이닝·아웃·주자·점수·타순·투수)
/// </summary>
public class GameState
{
    /// <summary>
    /// 주자 없음 센티넬
    /// </summary>
    public const int NoRunner = -1;

    /// <summary>
    /// 이닝을 끝내는 아웃 수
    /// </summary>
    public const int OutsPerInning = 3;

    /// <summary>
    /// 정규 이닝 수
    /// </summary>
    public const int RegulationInnings = 9;

    /// <summary>
    /// 연장 포함 최대 이닝. 넘기면 동점이어도 무승부로 끝낸다
    /// </summary>
    public const int MaxInnings = 11;

    public int Inning { get; private set; }         //현재 이닝
    public bool IsTopInning { get; private set; }   //true = 초 원정 공격
    public int OutCount { get; private set; }       //현재 아웃 카운트
    public int HomeScore { get; private set; }      //홈 팀 점수
    public int AwayScore { get; private set; }      //원정 팀 점수

    public int FirstBase { get; private set; }      //주자의 instanceId. NoRunner면 비어 있음
    public int SecondBase { get; private set; }
    public int ThirdBase { get; private set; }

    public int HomeBattingIndex { get; private set; }   //홈팀 타자 타순
    public int AwayBattingIndex { get; private set; }   //원정팀 타자 타순

    public PitcherState HomePitcherState { get; private set; }  //홈팀 등판 투수
    public PitcherState AwayPitcherState { get; private set; }  //원정팀 등판 투수

    public bool IsGameOver { get; private set; }

    private readonly HashSet<int> _homeUsedHitterInstanceIds = new HashSet<int>();
    private readonly HashSet<int> _awayUsedHitterInstanceIds = new HashSet<int>();
    private readonly HashSet<int> _homeUsedPitcherSlotIndices = new HashSet<int>();
    private readonly HashSet<int> _awayUsedPitcherSlotIndices = new HashSet<int>();

    public GameState(SimulationContext context)
    {
        Inning = 1;
        IsTopInning = true;
        OutCount = 0;
        HomeScore = 0;
        AwayScore = 0;
        FirstBase = NoRunner;
        SecondBase = NoRunner;
        ThirdBase = NoRunner;
        HomeBattingIndex = 0;
        AwayBattingIndex = 0;
        HomePitcherState = new PitcherState(context.HomePitchers[SimulationContext.StartingPitcherSlot],
            SimulationContext.StartingPitcherSlot);
        AwayPitcherState = new PitcherState(context.AwayPitchers[SimulationContext.StartingPitcherSlot],
            SimulationContext.StartingPitcherSlot);
        IsGameOver = false;
    }

    /// <summary>
    /// 아웃 1 증가. 3아웃이면 잔루를 지우고 공수를 바꾸거나 경기를 끝낸다
    /// </summary>
    public void AddOut()
    {
        OutCount++;

        if (OutCount < OutsPerInning)
            return;

        OutCount = 0;
        FirstBase = SecondBase = ThirdBase = NoRunner;

        if (IsTopInning)
        {
            if (Inning >= RegulationInnings && HomeScore > AwayScore)
            {
                IsGameOver = true;
                return;
            }

            IsTopInning = false;
            HomePitcherState.ResetInningStats();
            return;
        }

        if ((Inning >= RegulationInnings && HomeScore != AwayScore) || Inning >= MaxInnings)
        {
            IsGameOver = true;
            return;
        }

        IsTopInning = true;
        Inning++;
        AwayPitcherState.ResetInningStats();
    }

    /// <summary>
    /// 공격 중인 팀의 득점 1 증가. 홈팀이 9회 이후 역전하면 끝내기로 종료한다
    /// </summary>
    public void AddRun()
    {
        if (IsTopInning)
        {
            AwayScore++;
            return;
        }

        HomeScore++;

        if (Inning >= RegulationInnings && HomeScore > AwayScore)
            IsGameOver = true;
    }

    /// <summary>
    /// 공격 중인 팀의 타순을 1 전진시킨다
    /// </summary>
    public void AdvanceBatter()
    {
        if (IsTopInning)
            AwayBattingIndex = (AwayBattingIndex + 1) % SimulationContext.LineupSize;
        else
            HomeBattingIndex = (HomeBattingIndex + 1) % SimulationContext.LineupSize;
    }

    /// <summary>
    /// 등판 투수를 해당 슬롯의 투수로 바꾼다
    /// </summary>
    public void SubstitutePitcher(SimulationContext context, bool isHome, int newSlotIndex)
    {
        if (isHome)
            HomePitcherState = new PitcherState(context.HomePitchers[newSlotIndex], newSlotIndex);
        else
            AwayPitcherState = new PitcherState(context.AwayPitchers[newSlotIndex], newSlotIndex);
    }

    /// <summary>
    /// 1루 주자 설정
    /// </summary>
    public void SetFirstBase(int instanceId)
    {
        FirstBase = instanceId;
    }

    /// <summary>
    /// 2루 주자 설정
    /// </summary>
    public void SetSecondBase(int instanceId)
    {
        SecondBase = instanceId;
    }

    /// <summary>
    /// 3루 주자 설정
    /// </summary>
    public void SetThirdBase(int instanceId)
    {
        ThirdBase = instanceId;
    }

    /// <summary>
    /// 교체로 빠진 야수를 기록한다 (다시 내보낼 수 없게)
    /// </summary>
    public void MarkHitterUsed(bool isHome, int instanceId)
    {
        if (isHome)
            _homeUsedHitterInstanceIds.Add(instanceId);
        else
            _awayUsedHitterInstanceIds.Add(instanceId);
    }

    /// <summary>
    /// 강판된 투수 슬롯을 기록한다 (다시 올릴 수 없게)
    /// </summary>
    public void MarkPitcherUsed(bool isHome, int slotIndex)
    {
        if (isHome)
            _homeUsedPitcherSlotIndices.Add(slotIndex);
        else
            _awayUsedPitcherSlotIndices.Add(slotIndex);
    }

    /// <summary>
    /// 해당 야수가 이미 교체로 빠졌는지 확인한다
    /// </summary>
    public bool IsHitterUsed(bool isHome, int instanceId)
    {
        return isHome
            ? _homeUsedHitterInstanceIds.Contains(instanceId)
            : _awayUsedHitterInstanceIds.Contains(instanceId);
    }

    /// <summary>
    /// 해당 투수 슬롯이 이미 강판됐는지 확인한다
    /// </summary>
    public bool IsPitcherUsed(bool isHome, int slotIndex)
    {
        return isHome
            ? _homeUsedPitcherSlotIndices.Contains(slotIndex)
            : _awayUsedPitcherSlotIndices.Contains(slotIndex);
    }
}
