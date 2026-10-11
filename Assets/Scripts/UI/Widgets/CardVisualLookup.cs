using UnityEngine;

/// <summary>
/// 보유 카드의 등급·종류를 instanceId로 역추적한다.
/// AI 팀 스냅샷은 보유 카드가 아니므로 조회하지 않는다
/// </summary>
public static class CardVisualLookup
{
    /// <summary>
    /// 보유 카드의 종류·등급. 인벤토리에 없으면 false (별·종류색을 그리지 않는다)
    /// </summary>
    public static bool TryGet(int instanceId, out CardType cardType, out CardGrade cardGrade)
    {
        cardType = CardType.None;
        cardGrade = CardGrade.None;

        if (InventoryManager.Instance == null || CardDataManager.Instance == null)
            return false;

        CardInstance card = InventoryManager.Instance.GetCard(instanceId);

        if (card == null)
            return false;

        CardMasterData master = CardDataManager.Instance.GetCardMasterData(card.CardId);

        if (master == null)
            return false;

        cardType = master.CardType;
        cardGrade = master.CardGrade;

        return true;
    }

    /// <summary>
    /// 카드 종류별 강조색 (OVR 숫자 · 별)
    /// </summary>
    public static Color GetTypeColor(CardType cardType)
    {
        return cardType switch
        {
            CardType.Signature => UiPalette.CardSignature,
            CardType.GoldenGlove => UiPalette.CardGoldenGlove,
            CardType.Normal => UiPalette.CardNormal,
            _ => UiPalette.Text1
        };
    }

    /// <summary>
    /// 카드 앞면에 새기는 진한 색 (명세 6-6). 밝은 베이스 위에서도 읽힌다
    /// </summary>
    public static Color GetInkColor(CardType cardType)
    {
        return cardType switch
        {
            CardType.Signature => UiPalette.CardSignatureInk,
            CardType.GoldenGlove => UiPalette.CardGoldenGloveInk,
            CardType.Normal => UiPalette.CardNormalInk,
            _ => UiPalette.CardNormalInk
        };
    }
}
