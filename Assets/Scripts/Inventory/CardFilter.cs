/// <summary>
/// 인벤토리 필터 조건을 담는 데이터 클래스
/// </summary>
public class CardFilter
{
    public PlayerTypeFilter PlayerType  { get; set; }
    public CardGrade        Grade       { get; set; }
    public CardType         Type        { get; set; }
    public string           TeamName    { get; set; }
}

//TODO : 선수 이름 검색 기능 추가 예정
