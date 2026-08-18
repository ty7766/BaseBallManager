/// <summary>
/// 게임 내 재화 종류 (기획서 9.1)
/// </summary>
public enum CurrencyType
{
    None = 0,                //미지정 - 초기화 누락을 잡기 위한 센티넬

    Gold,                    //골드 - 카드 보유 한도 확장. 리그 종료 보상으로 획득
    Point,                   //포인트 - 훈련 · 제작. 카드 분해로 획득
    GoldenGlovePoint,        //골든글러브 포인트 - 골글 제작. 리그 보상 + 골글 분해

    NormalTicket,            //일반 뽑기권
    SignatureTicket,         //시그니쳐 뽑기권

    TrainCard,               //훈련 카드 - 훈련 · 제작 재료. 분해 시 확률적 획득
    BreakthroughCard,        //훈련돌파 카드 - 훈련 30 -> 50 해금

    EnhanceCardStar3,        //강화 전용 카드 (3성 노말)
    EnhanceCardStar4,        //강화 전용 카드 (4성 노말)
    EnhanceCardStar5,        //강화 전용 카드 (5성 노말)
    EnhanceCardSignature,    //강화 전용 카드 (시그니쳐)
    EnhanceCardGoldenGlove   //강화 전용 카드 (골든글러브) - 리그 보상으로만 획득
}
