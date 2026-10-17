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
        //photo 열이 비면 `이름_팀` 을 키로 쓴다 (노말·시그는 같은 사진).
        //팀을 붙이면 동명이인이 저절로 갈리고, 이적한 선수는 팀별 유니폼 사진을 쓰게 된다.
        //골든글러브만 특별 사진이라 접미사를 더 붙인다.
        //특정 카드만 다른 사진을 쓸 때는 photo 열에 직접 적으면 그것이 우선한다
        PhotoKey = string.IsNullOrWhiteSpace(photoKey)
            ? BuildPhotoKey(teamName, name, cardType)
            : photoKey.Trim();
    }
}
