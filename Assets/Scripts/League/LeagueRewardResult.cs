/// <summary>
/// 리그 종료 보상 수령 결과 (기획서 7.1 · 7.9)
/// </summary>
public class LeagueRewardResult
{
    public int Rank { get; }                        //플레이어 팀 최종 순위
    public int Gold { get; }                        //획득 골드
    public int GoldenGloveEnhanceCard { get; }      //획득 골카 전용 카드
    public int GoldenGlovePoint { get; }            //획득 골든글러브 포인트 (기획서 9.1)
    public int SignatureTicket { get; }             //획득 시그니쳐 뽑기권

    public bool TierUnlocked { get; }               //다음 티어가 새로 열렸는지
    public LeagueTier UnlockedTier { get; }         //열린 티어 (TierUnlocked가 false면 의미 없음)

    public LeagueRewardResult(int rank, int gold, int goldenGloveEnhanceCard,
        int goldenGlovePoint, int signatureTicket, bool tierUnlocked, LeagueTier unlockedTier)
    {
        Rank = rank;
        Gold = gold;
        GoldenGloveEnhanceCard = goldenGloveEnhanceCard;
        GoldenGlovePoint = goldenGlovePoint;
        SignatureTicket = signatureTicket;
        TierUnlocked = tierUnlocked;
        UnlockedTier = unlockedTier;
    }
}
