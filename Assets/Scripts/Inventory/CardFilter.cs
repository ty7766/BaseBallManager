using System;
/// <summary>
/// 인벤토리 필터 조건을 담는 데이터 클래스
/// </summary>
public class CardFilter
{
    public PlayerTypeFilter PlayerType  { get; set; }
    public CardGrade        Grade       { get; set; }
    public CardType         Type        { get; set; }
    public string           TeamName    { get; set; }

    /// <summary>
    /// 이 카드가 모든 조건을 통과하는지 판정. 비어 있는 조건(None / null)은 검사하지 않는다
    /// </summary>
    public bool Matches(CardMasterData cardMasterData)
    {
        if (!MatchesPlayerType(cardMasterData))
            return false;

        if (Grade != CardGrade.None && cardMasterData.CardGrade != Grade)
            return false;

        if (Type != CardType.None && cardMasterData.CardType != Type)
            return false;

        if (!string.IsNullOrEmpty(TeamName) && cardMasterData.TeamName != TeamName)
            return false;

        return true;
    }

    //타자·투수는 마스터 데이터의 실제 타입으로 판정한다
    private bool MatchesPlayerType(CardMasterData cardMasterData)
    {
        return PlayerType switch
        {
            PlayerTypeFilter.All => true,
            PlayerTypeFilter.HitterOnly => cardMasterData is HitterMasterData,
            PlayerTypeFilter.PitcherOnly => cardMasterData is PitcherMasterData,
            _ => throw new ArgumentException($"알 수 없는 선수 타입 필터: {PlayerType}")
        };
    }
}

//TODO : 선수 이름 검색 기능 추가 예정
