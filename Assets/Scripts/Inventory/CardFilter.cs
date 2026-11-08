using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인벤토리 필터 조건. 한 항목 안에서 여러 개를 고르면 하나라도 맞으면 통과(합집합)하고, 항목끼리는 모두 맞아야 통과(교집합)한다.
/// 예) 3성 + KT · 삼성 + 시그 → 3성이면서 KT 나 삼성이면서 시그인 카드. 아무것도 고르지 않은 항목은 검사하지 않는다
/// </summary>
public class CardFilter
{
    /// <summary>
    /// 고른 선수 타입 (HitterOnly · PitcherOnly)
    /// </summary>
    public HashSet<PlayerTypeFilter> PlayerTypes { get; } = new HashSet<PlayerTypeFilter>();
    public HashSet<CardGrade> Grades { get; } = new HashSet<CardGrade>();
    public HashSet<CardType> Types { get; } = new HashSet<CardType>();
    public HashSet<string> TeamNames { get; } = new HashSet<string>();

    /// <summary>
    /// 고른 조건 수 (필터 버튼 배지)
    /// </summary>
    public int SelectedCount => PlayerTypes.Count + Grades.Count + Types.Count + TeamNames.Count;

    /// <summary>
    /// 이 카드가 모든 항목을 통과하는지 판정
    /// </summary>
    public bool Matches(CardMasterData cardMasterData)
    {
        if (PlayerTypes.Count > 0 && !PlayerTypes.Contains(GetPlayerType(cardMasterData)))
            return false;

        if (Grades.Count > 0 && !Grades.Contains(cardMasterData.CardGrade))
            return false;

        if (Types.Count > 0 && !Types.Contains(cardMasterData.CardType))
            return false;

        if (TeamNames.Count > 0 && !TeamNames.Contains(cardMasterData.TeamName))
            return false;

        return true;
    }

    /// <summary>
    /// 모든 조건을 비운다
    /// </summary>
    public void Clear()
    {
        PlayerTypes.Clear();
        Grades.Clear();
        Types.Clear();
        TeamNames.Clear();
    }

    /// <summary>
    /// 다른 필터의 조건을 그대로 옮겨 담는다 (필터 창의 고르는 중 사본 ↔ 적용된 필터)
    /// </summary>
    public void CopyFrom(CardFilter other)
    {
        if (other == null)
        {
            Debug.LogWarning("[CardFilter] : 복사할 필터가 null 입니다");
            return;
        }

        Clear();
        PlayerTypes.UnionWith(other.PlayerTypes);
        Grades.UnionWith(other.Grades);
        Types.UnionWith(other.Types);
        TeamNames.UnionWith(other.TeamNames);
    }

    //타자 · 투수는 마스터 데이터의 실제 타입으로 판정한다
    private static PlayerTypeFilter GetPlayerType(CardMasterData cardMasterData)
    {
        return cardMasterData is PitcherMasterData ? PlayerTypeFilter.PitcherOnly : PlayerTypeFilter.HitterOnly;
    }
}

//TODO : 선수 이름 검색 기능 추가 예정
