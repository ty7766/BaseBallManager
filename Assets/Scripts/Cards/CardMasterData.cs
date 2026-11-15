/// <summary>
/// 카드의 정보
/// </summary>
public abstract class CardMasterData
{
    /// <summary>
    /// 골든글러브 카드 전용 사진의 파일명 접미사 (`김도영_기아_골글.png`)
    /// </summary>
    public const string GoldenGlovePhotoSuffix = "_골글";

    /// <summary>
    /// photo 열이 비었을 때 쓰는 기본 파일명. `이름_팀` 에 골글이면 접미사를 더한다
    /// </summary>
    public static string BuildPhotoKey(string teamName, string name, CardType cardType)
    {
        string key = name + "_" + teamName;

        return cardType == CardType.GoldenGlove ? key + GoldenGlovePhotoSuffix : key;
    }

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
        PhotoKey = string.IsNullOrWhiteSpace(photoKey)
            ? BuildPhotoKey(teamName, name, cardType)
            : photoKey.Trim();
    }
}
