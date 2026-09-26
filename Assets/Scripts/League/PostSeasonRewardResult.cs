/// <summary>
/// 포스트시즌 종료 보상 수령 결과 (기획서 7.5)
/// </summary>
public class PostSeasonRewardResult
{
    public PostSeasonRound ReachedRound { get; }    //플레이어 팀이 오른 가장 높은 단계
    public bool IsChampion { get; }                 //한국시리즈 우승 여부

    public int Gold { get; }
    public int GoldenGlovePoint { get; }
    public int SignatureTicket { get; }
    public int GoldenGloveEnhanceCard { get; }

    public PostSeasonRewardResult(PostSeasonRound reachedRound, bool isChampion,
        int gold, int goldenGlovePoint, int signatureTicket, int goldenGloveEnhanceCard)
    {
        ReachedRound = reachedRound;
        IsChampion = isChampion;
        Gold = gold;
        GoldenGlovePoint = goldenGlovePoint;
        SignatureTicket = signatureTicket;
        GoldenGloveEnhanceCard = goldenGloveEnhanceCard;
    }
}
