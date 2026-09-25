using UnityEngine;

public class PlayerDataManager : SingletonBehaviour<PlayerDataManager>
{
    public string PlayerTeamName { get; private set; }

    /// <summary>
    /// 해금된 가장 높은 리그 티어 (기획서 7.1 - 직전 리그 정규시즌 2등 이상이면 다음 티어 해금)
    /// </summary>
    public LeagueTier HighestUnlockedTier { get; private set; } = LeagueTier.Basic1;

    /// <summary>
    /// 튜토리얼 완료 여부 (기획서 5장 - 완료 보상인 뽑기권 50개가 중복 지급되지 않도록 저장한다)
    /// </summary>
    public bool TutorialCompleted { get; private set; }

    public bool SetPlayerTeam(string teamName)
    {
        if (string.IsNullOrEmpty(teamName))
        {
            Debug.LogWarning("[PlayerDataManager] : 팀 이름이 유효하지 않습니다");
            return false;
        }
        PlayerTeamName = teamName;
        return true;
    }

    /// <summary>
    /// 해당 티어에 입장할 수 있는지 (기획서 7.1)
    /// </summary>
    public bool IsTierUnlocked(LeagueTier tier)
    {
        return tier <= HighestUnlockedTier;
    }

    /// <summary>
    /// 티어 해금. 이미 더 높은 티어가 열려 있으면 되돌리지 않는다
    /// </summary>
    public bool UnlockTier(LeagueTier tier)
    {
        if (tier <= HighestUnlockedTier)
            return false;

        HighestUnlockedTier = tier;
        Debug.Log($"[PlayerDataManager] : {tier} 리그가 해금되었습니다");

        return true;
    }

    /// <summary>
    /// 튜토리얼 완료 표시. 이미 완료된 상태면 false (보상 중복 지급 차단)
    /// </summary>
    public bool CompleteTutorial()
    {
        if (TutorialCompleted)
            return false;

        TutorialCompleted = true;
        return true;
    }

    /// <summary>
    /// 세이브 복원용 일괄 주입
    /// </summary>
    public bool Restore(string teamName, LeagueTier highestUnlockedTier, bool tutorialCompleted)
    {
        if (!SetPlayerTeam(teamName))
            return false;

        if (!LeagueTierTable.IsValidTier(highestUnlockedTier))
        {
            Debug.LogError($"[PlayerDataManager] : 알 수 없는 리그 티어입니다 ({(int)highestUnlockedTier})");
            return false;
        }

        HighestUnlockedTier = highestUnlockedTier;
        TutorialCompleted = tutorialCompleted;
        return true;
    }
}
