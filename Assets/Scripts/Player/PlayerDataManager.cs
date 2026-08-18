using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    public string PlayerTeamName { get; private set; }

    //해금된 가장 높은 리그 티어 (기획서 7.1 - 직전 리그 정규시즌 2등 이상이면 다음 티어 해금)
    public LeagueTier HighestUnlockedTier { get; private set; } = LeagueTier.Basic1;

    //튜토리얼 완료 여부 (기획서 5장 - 완료 보상인 뽑기권 50개가 중복 지급되지 않도록 저장한다)
    public bool TutorialCompleted { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetPlayerTeam(string teamName)
    {
        if (string.IsNullOrEmpty(teamName))
        {
            Debug.LogWarning("[PlayerDataManager] : 팀 이름이 유효하지 않습니다");
            return;
        }
        PlayerTeamName = teamName;
    }

    //해당 티어에 입장할 수 있는지 (기획서 7.1)
    public bool IsTierUnlocked(LeagueTier tier)
    {
        return (int)tier <= (int)HighestUnlockedTier;
    }

    //티어 해금. 이미 더 높은 티어가 열려 있으면 되돌리지 않는다
    public bool UnlockTier(LeagueTier tier)
    {
        if ((int)tier <= (int)HighestUnlockedTier)
            return false;

        HighestUnlockedTier = tier;
        Debug.Log($"[PlayerDataManager] : {tier} 리그가 해금되었습니다");

        return true;
    }

    //튜토리얼 완료 표시. 이미 완료된 상태면 false (보상 중복 지급 차단)
    public bool CompleteTutorial()
    {
        if (TutorialCompleted)
            return false;

        TutorialCompleted = true;
        return true;
    }

    //세이브 복원용 일괄 주입
    public void Restore(string teamName, LeagueTier highestUnlockedTier, bool tutorialCompleted)
    {
        SetPlayerTeam(teamName);

        //복원은 되돌리기가 아니라 저장 당시 값 그대로 반영이므로 UnlockTier를 쓰지 않는다
        HighestUnlockedTier = highestUnlockedTier;
        TutorialCompleted = tutorialCompleted;
    }
}
