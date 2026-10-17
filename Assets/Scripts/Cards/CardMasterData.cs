/// <summary>
/// 카드의 정보
/// </summary>
public abstract class CardMasterData
{
    /// <summary>
    /// 골든글러브 카드 전용 사진의 파일명 접미사 (`김도영_골글.png`)
    /// </summary>
    public const string GoldenGlovePhotoSuffix = "_골글";

    /// <summary>
    /// 카드가 지정되지 않은 슬롯. cardId는 1부터라 int 기본값이 곧 빈 슬롯이다
    /// </summary>
    public const int NoCardId = 0;

    public int CardId { get; }
    public string Name { get; }
    public string TeamName { get; }
    public int Year { get; }
    public CardType CardType { get; }
    public CardGrade CardGrade { get; }
    public string Position { get; }
    public int OVR { get; }

    /// <summary>
    /// 선수 사진 파일 이름(확장자 제외). 같은 인물의 다른 카드는 같은 값을 쓴다. 비어 있으면 사진 없음
    /// </summary>
    public string PhotoKey { get; }

    protected CardMasterData(int cardId, string name, string teamName, int year,
        CardType cardType, CardGrade cardGrade, string position, int ovr, string photoKey)
    {
        CardId = cardId;
        Name = name;
        TeamName = teamName;
        Year = year;
        CardType = cardType;
        CardGrade = cardGrade;
        Position = position;
        OVR = ovr;
        //photo 열이 비면 선수 이름을 그대로 키로 쓴다 (노말·시그는 같은 사진).
        //골든글러브만 특별 사진이라 접미사를 붙여 다른 파일을 찾는다.
        //같은 이름의 다른 인물이거나 특정 카드만 다른 사진을 쓸 때만 photo 열을 채운다
        PhotoKey = string.IsNullOrWhiteSpace(photoKey)
            ? (cardType == CardType.GoldenGlove ? name + GoldenGlovePhotoSuffix : name)
            : photoKey.Trim();
    }
}
