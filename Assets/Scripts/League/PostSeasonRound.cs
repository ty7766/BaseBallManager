/// <summary>
/// 포스트시즌 단계 (기획서 7.5 - KBO 사다리)
/// </summary>
public enum PostSeasonRound
{
    WildCard,       //와일드카드 결정전 (4위 vs 5위)
    SemiPlayOff,    //준플레이오프 (3위 vs WC 승자)
    PlayOff,        //플레이오프 (2위 vs 준PO 승자)
    KoreanSeries    //한국시리즈 (1위 vs PO 승자)
}
