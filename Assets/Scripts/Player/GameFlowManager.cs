using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시작 플로우와 플레이어 세이브의 수명 관리 (기획서 5장 · 10장)
/// </summary>
public class GameFlowManager : SingletonBehaviour<GameFlowManager>
{
    /// <summary>
    /// 선택 가능한 10팀 (기획서 5장)
    /// </summary>
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

    protected override void OnSingletonAwake()
    {
        _saveService = new PlayerSaveService(new LocalFileStorage());
    }

    /// <summary>
    /// 이어하기 버튼을 띄울지 판단
    /// </summary>
    public bool HasSave()
    {
        return _saveService.HasSave();
    }

    /// <summary>
    /// 저장된 진행 상황 불러오기
    /// </summary>
    public bool ContinueGame()
    {
        return _saveService.Load();
    }

    /// <summary>
    /// 현재 진행 상황 저장
    /// </summary>
    public bool SaveGame()
    {
        return _saveService.Save();
    }

    /// <summary>
    /// 새 게임 시작 - 팀 선택 + 기본 시그니쳐 카드 1장 지급 (기획서 5장의 1·2단계)
    /// </summary>
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

        if (!PlayerDataManager.Instance.SetPlayerTeam(teamName))
            return false;

        GrantStartingSignatureCard(teamName);

        return true;
    }

    /// <summary>
    /// 튜토리얼 완료 보상 - 일반 뽑기권 지급 (기획서 5장의 4단계)
    /// </summary>
    public bool CompleteTutorial()
    {
        if (PlayerDataManager.Instance == null || CurrencyManager.Instance == null)
        {
            Debug.LogError("[GameFlowManager]: 필요한 매니저가 씬에 없습니다 (Player / Currency)");
            return false;
        }

        if (!PlayerDataManager.Instance.CompleteTutorial())
        {
            Debug.LogWarning("[GameFlowManager]: 튜토리얼 보상은 이미 지급되었습니다");
            return false;
        }

        CurrencyManager.Instance.Add(CurrencyType.NormalTicket, _tutorialTicketReward);
        return true;
    }

    /// <summary>
    /// 선택 가능한 팀인지
    /// </summary>
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
    private void GrantStartingSignatureCard(string teamName)
    {
        if (_startingSignatureTable == null)
        {
            Debug.LogWarning("[GameFlowManager]: 시작 시그니쳐 표가 연결되어 있지 않아 시작 카드를 지급하지 않았습니다");
            return;
        }

        int startingCardId = _startingSignatureTable.GetCardId(teamName);

        if (startingCardId == 0)
        {
            Debug.LogWarning($"[GameFlowManager]: '{teamName}' 팀에 지정된 시작 시그니쳐 카드가 없습니다");
            return;
        }

        if (CardDataManager.Instance.GetCardMasterData(startingCardId) == null)
        {
            Debug.LogError($"[GameFlowManager]: '{teamName}' 팀의 시작 카드(cardId {startingCardId})가 마스터 데이터에 없습니다");
            return;
        }

        InventoryManager.Instance.AddCard(startingCardId);
    }
}
