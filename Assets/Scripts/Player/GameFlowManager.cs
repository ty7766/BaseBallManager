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
    private int _tutorialTicketReward = 50;

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

        //TODO: 팀별 지급 시그니쳐 카드가 확정되면(기획서 12장 TBD) 표에서 읽도록 교체.
        //현재는 해당 팀 시그니쳐 카드 중 무작위 1장
        int startingCardId = PickStartingSignatureCardId(teamName);

        if (startingCardId == -1)
        {
            //시그니쳐 카드가 아직 CSV에 없는 상태. 팀 선택 자체는 성공으로 둔다
            Debug.LogWarning($"[GameFlowManager]: '{teamName}' 팀의 시그니쳐 카드가 없어 시작 카드를 지급하지 못했습니다");
        }
        else
        {
            InventoryManager.Instance.AddCard(startingCardId);
        }

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

    //해당 팀 시그니쳐 카드 중 무작위 1장. 없으면 -1
    private static int PickStartingSignatureCardId(string teamName)
    {
        List<int> pool = new List<int>();

        foreach (HitterMasterData hitter in CardDataManager.Instance.GetAllHitters())
        {
            if (hitter.TeamName == teamName && hitter.CardType == CardType.Signature)
                pool.Add(hitter.CardId);
        }

        foreach (PitcherMasterData pitcher in CardDataManager.Instance.GetAllPitchers())
        {
            if (pitcher.TeamName == teamName && pitcher.CardType == CardType.Signature)
                pool.Add(pitcher.CardId);
        }

        if (pool.Count == 0)
            return -1;

        return pool[Random.Range(0, pool.Count)];
    }
}
