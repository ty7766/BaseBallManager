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
    Reveal,         //뽑기 공개 (3·4성)
    RevealRare,     //뽑기 공개 5성
    RevealSignature,    //뽑기 공개 시그니쳐
    RevealGoldenGlove,  //뽑기 공개 골든글러브
    Champion,       //우승
    Steal           //도루 성공
}
