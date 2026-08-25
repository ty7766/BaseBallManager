using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시작 플로우와 플레이어 세이브의 수명 관리 (기획서 5장 · 10장)
/// </summary>
/// <remarks>
/// 신규 시작 / 이어하기 / 저장의 진입점. UI는 이 클래스만 호출하면 된다.
/// 리그 진행도는 LeagueManager가 따로 저장한다 (수명이 다름 - 기획서 7.6).
/// </remarks>
public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    //선택 가능한 10팀 (기획서 5장)
    public IReadOnlyList<string> SelectableTeamNames => TeamNames;

    [Header("게임 시작 지급 (기획서 5장)")]
    [SerializeField, Tooltip("튜토리얼 완료 보상 일반 뽑기권")]
    private int _tutorialTicketReward = 150;

    [SerializeField, Tooltip("팀별 시작 지급 시그니쳐 카드 표. 비워두면 시작 카드를 지급하지 않는다")]
    private StartingSignatureTable _startingSignatureTable;

    private static readonly string[] TeamNames =
    {
        "기아", "삼성", "LG", "한화", "두산", "SSG", "KT", "NC", "키움", "롯데"
    };

    private PlayerSaveService _saveService;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            //저장소 교체 지점 - 서버 저장으로 바꿀 땐 여기만 바뀐다 (기획서 10장)
            _saveService = new PlayerSaveService(new LocalFileStorage());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //이어하기 버튼을 띄울지 판단
    public bool HasSave()
    {
        return _saveService.HasSave();
    }

    //저장된 진행 상황 불러오기
    public bool ContinueGame()
    {
        return _saveService.Load();
    }

    //현재 진행 상황 저장
    public bool SaveGame()
    {
        return _saveService.Save();
    }

    //새 게임 시작 - 팀 선택 + 기본 시그니쳐 카드 1장 지급 (기획서 5장의 1·2단계)
    public bool StartNewGame(string teamName)
    {
        if (PlayerDataManager.Instance == null || InventoryManager.Instance == null
            || CardDataManager.Instance == null)
        {
            Debug.LogError("[GameFlowManager]: 필요한 매니저가 씬에 없습니다 (Player / Inventory / CardData)");
            return false;
        }

        if (!IsSelectableTeam(teamName))
        {
            Debug.LogError($"[GameFlowManager]: 선택할 수 없는 팀입니다 - '{teamName}'");
            return false;
        }

        PlayerDataManager.Instance.SetPlayerTeam(teamName);

        GrantStartingSignatureCard(teamName);

        return true;
    }

    //튜토리얼 완료 보상 - 일반 뽑기권 지급 (기획서 5장의 4단계)
    public bool CompleteTutorial()
    {
        if (PlayerDataManager.Instance == null || CurrencyManager.Instance == null)
        {
            Debug.LogError("[GameFlowManager]: 필요한 매니저가 씬에 없습니다 (Player / Currency)");
            return false;
        }

        //이미 받은 보상을 다시 주지 않도록 완료 표시가 먼저다
        if (!PlayerDataManager.Instance.CompleteTutorial())
        {
            Debug.LogWarning("[GameFlowManager]: 튜토리얼 보상은 이미 지급되었습니다");
            return false;
        }

        CurrencyManager.Instance.Add(CurrencyType.NormalTicket, _tutorialTicketReward);
        return true;
    }

    //선택 가능한 팀인지
    public bool IsSelectableTeam(string teamName)
    {
        if (string.IsNullOrEmpty(teamName))
            return false;

        foreach (string name in TeamNames)
        {
            if (name == teamName)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 팀 선택 시 기본 시그니쳐 카드 1장 지급 (기획서 5장의 2단계)
    /// </summary>
    /// <remarks>
    /// 지급하지 못해도 팀 선택 자체는 성공으로 둔다.
    /// 시그니쳐 카드 마스터 데이터가 아직 없는 상태에서도 게임을 시작할 수 있어야 하기 때문이다.
    /// </remarks>
    private void GrantStartingSignatureCard(string teamName)
    {
        if (_startingSignatureTable == null)
        {
            Debug.LogWarning("[GameFlowManager]: 시작 시그니쳐 표가 연결되어 있지 않아 시작 카드를 지급하지 않았습니다");
            return;
        }

        //0은 "아직 정하지 않음"이라는 정상 상태 (세션 33의 빈 슬롯 센티넬)
        int startingCardId = _startingSignatureTable.GetCardId(teamName);

        if (startingCardId == 0)
        {
            Debug.LogWarning($"[GameFlowManager]: '{teamName}' 팀에 지정된 시작 시그니쳐 카드가 없습니다");
            return;
        }

        //표에 적힌 카드가 CSV에서 사라졌다면 데이터 불일치라 LogError
        if (CardDataManager.Instance.GetCardMasterData(startingCardId) == null)
        {
            Debug.LogError($"[GameFlowManager]: '{teamName}' 팀의 시작 카드(cardId {startingCardId})가 마스터 데이터에 없습니다");
            return;
        }

        InventoryManager.Instance.AddCard(startingCardId);
    }
}
