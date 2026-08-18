using UnityEngine;

/// <summary>
/// 팀별 시작 지급 시그니쳐 카드 표 (기획서 5장의 2단계 · 12장 TBD)
/// </summary>
/// <remarks>
/// 어느 선수를 줄지는 밸런스 판단이라 인스펙터에서 직접 고른다.
/// cardId 필드에 <see cref="CardIdAttribute"/>가 붙어 있어 팀별 폴더 + 검색 드롭다운으로 선택할 수 있다
/// (AI 로스터 편성과 같은 도구).
/// </remarks>
[CreateAssetMenu(fileName = "StartingSignatureTable", menuName = "BaseBallManager/Starting Signature Table")]
public class StartingSignatureTable : ScriptableObject
{
    [Header("팀별 지급 카드 - 팀명은 기획서 5장의 10팀과 정확히 같아야 함")]
    [SerializeField]
    private StartingSignatureEntry[] _entries = new StartingSignatureEntry[0];

    /// <summary>
    /// 해당 팀에 지급할 cardId. 표에 없거나 비어 있으면 <c>0</c>
    /// </summary>
    /// <remarks>
    /// 0을 반환하는 것은 <b>정상 상태</b>다(아직 시그니쳐 카드를 CSV에 넣지 않음).
    /// 호출자는 이 경우 카드를 지급하지 않고 넘어간다.
    /// </remarks>
    public int GetCardId(string teamName)
    {
        if (string.IsNullOrEmpty(teamName) || _entries == null)
            return 0;

        foreach (StartingSignatureEntry entry in _entries)
        {
            if (entry.TeamName == teamName)
                return entry.CardId;
        }

        return 0;
    }
}
