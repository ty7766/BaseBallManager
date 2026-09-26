using UnityEngine;

public class PlayerDataManager : SingletonBehaviour<PlayerDataManager>
{
    public string PlayerTeamName { get; private set; }

    /// <summary>
    /// 해금된 가장 높은 리그 티어 (직전 리그 정규시즌 2등 이상이면 다음 티어 해금)
    /// </summary>
    public LeagueTier HighestUnlockedTier { get; private set; } = LeagueTier.Basic1;

    /// <summary>
    /// 튜토리얼 완료 여부 (완료 보상인 뽑기권 50개가 중복 지급되지 않도록 저장한다)
    /// </summary>
    public bool TutorialCompleted { get; private set; }

    /// <summary>
    /// 경기 중 교체 방식. 수동이어야 일시정지 인터럽트를 쓸 수 있다
    /// </summary>
    public SubstitutionMode CurrentSubstitutionMode { get; private set; } = SubstitutionMode.Auto;

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
    /// 해당 티어에 입장할 수 있는지
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
    /// 교체 방식 변경. 진행 중인 경기에는 적용되지 않는다
    /// </summary>
    public bool SetSubstitutionMode(SubstitutionMode mode)
    {
        if (!IsValidSubstitutionMode(mode))
        {
            Debug.LogError($"[PlayerDataManager] : 알 수 없는 교체 방식입니다 ({(int)mode})");
            return false;
        }

        CurrentSubstitutionMode = mode;
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
    public bool Restore(string teamName, LeagueTier highestUnlockedTier, bool tutorialCompleted,
        SubstitutionMode substitutionMode)
    {
        if (!SetPlayerTeam(teamName))
            return false;

        if (!LeagueTierTable.IsValidTier(highestUnlockedTier))
        {
            Debug.LogError($"[PlayerDataManager] : 알 수 없는 리그 티어입니다 ({(int)highestUnlockedTier})");
            return false;
        }

        if (!IsValidSubstitutionMode(substitutionMode))
        {
            Debug.LogError($"[PlayerDataManager] : 알 수 없는 교체 방식입니다 ({(int)substitutionMode})");
            return false;
        }

        HighestUnlockedTier = highestUnlockedTier;
        TutorialCompleted = tutorialCompleted;
        CurrentSubstitutionMode = substitutionMode;
        return true;
    }

    //enum 범위 검사. 세이브에서 읽은 값을 캐스팅한 뒤에 쓴다
    private static bool IsValidSubstitutionMode(SubstitutionMode mode)
    {
        return mode == SubstitutionMode.Auto || mode == SubstitutionMode.Manual;
    }
}
