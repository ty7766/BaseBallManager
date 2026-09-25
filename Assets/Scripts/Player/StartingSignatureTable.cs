using UnityEngine;

/// <summary>
/// 팀별 시작 지급 시그니쳐 카드 표 (기획서 5장의 2단계 · 12장 TBD)
/// </summary>
[CreateAssetMenu(fileName = "StartingSignatureTable", menuName = "BaseBallManager/Starting Signature Table")]
public class StartingSignatureTable : ScriptableObject
{
    [Header("팀별 지급 카드 - 팀명은 기획서 5장의 10팀과 정확히 같아야 함")]
    [SerializeField]
    private StartingSignatureEntry[] _entries = new StartingSignatureEntry[0];

    /// <summary>
    /// 해당 팀에 지급할 cardId. 표에 없거나 비어 있으면 <c>0</c>
    /// </summary>
    public int GetCardId(string teamName)
    {
        if (string.IsNullOrEmpty(teamName) || _entries == null)
            return CardMasterData.NoCardId;

        foreach (StartingSignatureEntry entry in _entries)
        {
            if (entry.TeamName == teamName)
                return entry.CardId;
        }

        return CardMasterData.NoCardId;
    }
}
