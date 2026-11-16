using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 시작 플로우와 플레이어 세이브의 수명 관리
/// </summary>
public class GameFlowManager : SingletonBehaviour<GameFlowManager>
{
    /// <summary>
    /// 선택 가능한 10팀
    /// </summary>
    public IReadOnlyList<string> SelectableTeamNames => TeamNames;

    [Header("게임 시작 지급")]
    [SerializeField, Tooltip("튜토리얼 완료 보상 일반 뽑기권")]
    private int _tutorialTicketReward = 500;

    [SerializeField, Tooltip("튜토리얼 완료 시 주는 시그니쳐 뽑기권 (테스트용)")]
    private int _tutorialSignatureTicketReward = 200;

    [SerializeField, Tooltip("뽑기권을 뺀 나머지 재화를 테스트용으로 일괄 지급한다. 0이면 안 준다")]
    private int _testCurrencyAmount = 999999;

    [SerializeField, Tooltip("팀별 시작 지급 시그니쳐 카드 표. 비워두면 시작 카드를 지급하지 않는다")]
    private StartingSignatureTable _startingSignatureTable;

    private static readonly string[] TeamNames =
    {
        "기아", "삼성", "LG", "한화", "두산", "SSG", "KT", "NC", "키움", "롯데"
    };

    private PlayerSaveService _saveService;
    //새 게임을 시작했거나 불러온 뒤에만 자동 저장한다 (빈 상태로 세이브를 덮어쓰지 않게)
    private bool _isGameActive;

    protected override void OnSingletonAwake()
    {
        _saveService = new PlayerSaveService(new LocalFileStorage());
    }

    //백그라운드로 내려갈 때 저장한다. 모바일은 이 뒤에 예고 없이 종료될 수 있다
    private void OnApplicationPause(bool paused)
    {
        if (paused)
            AutoSave();
    }

    //정상 종료 직전 저장
    private void OnApplicationQuit()
    {
        AutoSave();
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
        _isGameActive = _saveService.Load();

        return _isGameActive;
    }

    /// <summary>
    /// 현재 진행 상황 저장
    /// </summary>
    public bool SaveGame()
    {
        return _saveService.Save();
    }

    /// <summary>
    /// 플레이어 진행과 진행 중인 정규시즌을 함께 저장한다. 게임을 시작하거나 불러오기 전이면 하지 않는다
    /// </summary>
    public bool AutoSave()
    {
        if (!_isGameActive)
            return false;

        bool saved = _saveService.Save();

        if (LeagueManager.Instance != null && LeagueManager.Instance.IsSeasonRunning)
            saved &= LeagueManager.Instance.SaveLeague();

        return saved;
    }

    /// <summary>
    /// 새 게임 시작 - 팀 선택 + 기본 시그니쳐 카드 1장 지급
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
        _isGameActive = true;

        return true;
    }

    /// <summary>
    /// 튜토리얼 완료 보상 - 일반 · 시그니쳐 뽑기권 지급
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
        CurrencyManager.Instance.Add(CurrencyType.SignatureTicket, _tutorialSignatureTicketReward);
        GrantTestCurrencies();

        return true;
    }

    //테스트용 일괄 지급. 수치 균형을 잡을 때 0 으로 내리면 된다
    private void GrantTestCurrencies()
    {
        if (_testCurrencyAmount <= 0)
            return;

        foreach (CurrencyType type in System.Enum.GetValues(typeof(CurrencyType)))
        {
            if (type == CurrencyType.None
                || type == CurrencyType.NormalTicket
                || type == CurrencyType.SignatureTicket)
            {
                continue;
            }

            CurrencyManager.Instance.Add(type, _testCurrencyAmount);
        }
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
    /// 팀 선택 시 기본 시그니쳐 카드 1장 지급
    /// </summary>
    private void GrantStartingSignatureCard(string teamName)
    {
        if (_startingSignatureTable == null)
        {
            Debug.LogWarning("[GameFlowManager]: 시작 시그니쳐 표가 연결되어 있지 않아 시작 카드를 지급하지 않았습니다");
            return;
        }

        int startingCardId = _startingSignatureTable.GetCardId(teamName);

        if (startingCardId == CardMasterData.NoCardId)
        {
            Debug.LogWarning($"[GameFlowManager]: '{teamName}' 팀에 지정된 시작 시그니쳐 카드가 없습니다");
            return;
        }

        if (CardDataManager.Instance.GetCardMasterData(startingCardId) == null)
        {
            Debug.LogError($"[GameFlowManager]: '{teamName}' 팀의 시작 카드(cardId {startingCardId})가 마스터 데이터에 없습니다");
            return;
        }

        if (InventoryManager.Instance.AddCard(startingCardId) == InventoryManager.InvalidInstanceId)
            Debug.LogError($"[GameFlowManager]: '{teamName}' 팀의 시작 카드를 지급하지 못했습니다 (cardId {startingCardId})");
    }
}
