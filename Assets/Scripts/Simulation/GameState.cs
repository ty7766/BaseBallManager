using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이닝
/// 아웃 카운트
/// 베이스 주자
/// 양 팀 득점
/// 현재 타순
/// 양 팀 투수 상태
/// 경기 종료 여부
/// 
/// 경기 시뮬레이션 중 모든 상태 변수를 담는 클래스
/// </summary>
public class GameState
{
    public int Inning { get; private set; }         //현재 이닝
    public bool IsTopInning { get; private set; }  //true = 초 원정 공격
    public int OutCount { get; private set; }        //현재 아웃 카운트
    public int HomeScore { get; private set; }      //홈 팀 점수
    public int AwayScore { get; private set; }      //원정 팀 점수

    //InstanceId로 조회 -> 스탯 확인 -> 주자가 어디까지 진루할 수 있는지 판단
    //따라서 bool을 쓰지 않고 instanceId를 담을 수 있는 int 선언
    public int FirstBase { get; private set; }     //-1이면 주자 없음
    public int SecondBase { get; private set; }
    public int ThirdBase { get; private set; }

    public int HomeBattingIndex { get; private set; }   //홈팀 타자 타순
    public int AwayBattingIndex { get; private set; }   //원정팀 타자 타순

    public PitcherState HomePitcherState { get; private set; }  //홈팀 투수 현재 체력
    public PitcherState AwayPitcherState { get; private set; }  //원정팀 투수 현재 체력

    public bool IsGameOver { get; private set; }    //경기가 종료되었는지 여부

    private HashSet<int> _homeUsedHitterInstanceIds;        //교체된 홈팀 타자 인스턴스 ID
    private HashSet<int> _awayUsedHitterInstanceIds;        //교체된 원정팀 타자 인스턴스 ID
    private HashSet<int> _homeUsedPitcherSlotIndices;       //교체된 홈팀 투수 슬롯 번호
    private HashSet<int> _awayUsedPitcherSlotIndices;       //교체된 원정팀 투수 슬롯 번호


    public GameState(SimulationContext context)
    {
        Inning = 1;
        IsTopInning = true;
        OutCount = 0;
        HomeScore = 0;
        AwayScore = 0;
        FirstBase = -1;
        SecondBase = -1;
        ThirdBase = -1;
        HomeBattingIndex = 0;
        AwayBattingIndex = 0;
        HomePitcherState = new PitcherState(context.HomePitchers[0], 0);
        AwayPitcherState = new PitcherState(context.AwayPitchers[0], 0);
        IsGameOver = false;

        _homeUsedHitterInstanceIds = new HashSet<int>();
        _awayUsedHitterInstanceIds = new HashSet<int>();
        _homeUsedPitcherSlotIndices = new HashSet<int>();
        _awayUsedPitcherSlotIndices = new HashSet<int>();
    }
    
    //아웃 카운트 증가 및 3아웃 이닝 종료 처리
    public void AddOut()
    {
        OutCount++;

        //3아웃일 때만 로직 실행
        if (OutCount < 3)
            return;

        //3아웃 되면 이닝 전환
        OutCount = 0;
        FirstBase = SecondBase = ThirdBase = -1;        //잔루 소멸

        //3아웃 되면 초 -> 말
        if (IsTopInning)
        {
            IsTopInning = false;    //초 -> 말
            HomePitcherState.ResetInningStats();
            return;
        }

        //9회가 끝난 뒤 동점이 아니거나 이닝이 12회로 넘어가면 게임 종료
        if ((Inning >= 9 && HomeScore != AwayScore) || (Inning >= 11))
        {
            IsGameOver = true;
            return;
        }

        IsTopInning = true;
        Inning++;
        AwayPitcherState.ResetInningStats();
    }

    //현재 공격 중인 팀 점수 증가
    public void AddRun()
    {
        //점수 증가
        if (IsTopInning)
            AwayScore++;
        else
        {
            HomeScore++;

            //끝내기인지 확인
            if ((Inning >= 9) && (HomeScore > AwayScore))
                IsGameOver = true;
        }
    }

    //현재 공격 중인 팀의 타순 1 증가
    public void AdvanceBatter()
    {
        if (IsTopInning)
            AwayBattingIndex = (AwayBattingIndex + 1) % 9;
        else
            HomeBattingIndex = (HomeBattingIndex + 1) % 9;
    }

    //현재 등판 중인 투수 교체
    public void SubstitutePitcher(SimulationContext context, bool isHome, int newSlotIndex)
    {
        if (isHome)
        {
            HomePitcherState = new PitcherState(context.HomePitchers[newSlotIndex], newSlotIndex);
        }
        else
        {
            AwayPitcherState = new PitcherState(context.AwayPitchers[newSlotIndex], newSlotIndex);
        }
    }

    //1루 주자 세팅
    public void SetFirstBase(int instanceId)
    {
        FirstBase = instanceId;
    }

    //2루 주자 세팅
    public void SetSecondBase(int instanceId)
    {
        SecondBase = instanceId;
    }

    //3루 주자 세팅
    public void SetThirdBase(int instanceId)
    {
        ThirdBase = instanceId;
    }

    //교체로 빠진 야수 기록
    public void MarkHitterUsed(bool isHome, int instanceId)
    {
        if (isHome)
            _homeUsedHitterInstanceIds.Add(instanceId);
        else
            _awayUsedHitterInstanceIds.Add(instanceId);
    }

    //교체로 빠진 투수 기록
    public void MarkPitcherUsed(bool isHome, int slotIndex)
    {
        if (isHome)
            _homeUsedPitcherSlotIndices.Add(slotIndex);
        else
            _awayUsedPitcherSlotIndices.Add(slotIndex);
    }

    //해당 야수가 이미 빠진 상태인지 조회
    public bool IsHitterUsed(bool isHome, int instanceId)
    {
        return isHome ? _homeUsedHitterInstanceIds.Contains(instanceId) : _awayUsedHitterInstanceIds.Contains(instanceId);
    }

    //해당 투수가 이미 빠진 상태인지 조회
    public bool IsPitcherUsed(bool isHome, int slotIndex)
    {
        return isHome ? _homeUsedPitcherSlotIndices.Contains(slotIndex) : _awayUsedPitcherSlotIndices.Contains(slotIndex);
    }
}
