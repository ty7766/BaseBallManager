/// <summary>
/// 파티클 연출 종류. 상황별 색·개수·속도 프리셋을 고르는 키다
/// </summary>
public enum UiEffectKind
{
    Score,          //득점
    HomeRun,        //홈런
    GradeUp,        //조합 승급
    Enhance,        //강화 성공
    Craft,          //골든글러브 제작
    Champion,       //우승
    Steal,          //도루 성공
    CombineAbsorb,  //조합 재료가 빛으로 풀려 오름
    CombineForge,   //조합 결과가 맺힘
    CombineGradeUp  //조합 결과가 맺힘 (승급)
}
