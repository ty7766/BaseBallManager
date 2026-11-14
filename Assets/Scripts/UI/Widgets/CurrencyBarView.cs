using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 상단 재화 표시 줄. 로비와 수급·육성 화면이 공유한다.
/// 화면마다 보여 줄 재화를 고른다 (로비는 뽑기권을 빼고, 뽑기 화면은 모두)
/// </summary>
public class CurrencyBarView : UiWidgetBehaviour
{
    /// <summary>
    /// 줄 높이
    /// </summary>
    public const float BarHeight = 88f;

    /// <summary>
    /// 줄 너비 (좌우 여백 24)
    /// </summary>
    public const float BarWidth = 1032f;

    /// <summary>
    /// 모든 재화
    /// </summary>
    public static readonly CurrencyType[] AllTypes =
    {
        CurrencyType.Gold,
        CurrencyType.Point,
        CurrencyType.GoldenGlovePoint,
        CurrencyType.NormalTicket,
        CurrencyType.SignatureTicket,
        CurrencyType.TrainCard
    };

    /// <summary>
    /// 뽑기권을 뺀 재화 (뽑기권은 뽑기 화면에서만 보여 준다)
    /// </summary>
    public static readonly CurrencyType[] WithoutTickets =
    {
        CurrencyType.Gold,
        CurrencyType.Point,
        CurrencyType.GoldenGlovePoint,
        CurrencyType.TrainCard
    };

    [SerializeField] private CurrencyType[] _shownTypes;
    [SerializeField] private TextMeshProUGUI[] _amountTexts;

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점). 고른 재화만 같은 너비로 나눠 놓는다
    /// </summary>
    public static CurrencyBarView Bake(Transform parent, CurrencyType[] shownTypes)
    {
        CurrencyBarView view = CreateNode<CurrencyBarView>(parent, "CurrencyBar");

        view.BuildWidget(shownTypes);

        return view;
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(CurrencyType[] shownTypes)
    {
        _shownTypes = (CurrencyType[])shownTypes.Clone();
        _amountTexts = new TextMeshProUGUI[_shownTypes.Length];

        Image background = UiBuilder.CreateImage(Root, "Background", UiPalette.BgPanel,
            UiSprites.PanelSmall, Image.Type.Sliced);
        UiBuilder.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

        float cellWidth = BarWidth / _shownTypes.Length;

        for (int i = 0; i < _shownTypes.Length; i++)
        {
            RectTransform cell = UiBuilder.CreateRect(Root, "Cell");
            UiBuilder.Place(cell, i * cellWidth, 0f, cellWidth, BarHeight);

            Image icon = UiBuilder.CreateImage(cell, "Icon", Color.white, GetIconPath(_shownTypes[i]));
            UiBuilder.Place(icon.rectTransform, 8f, 22f, 44f, 44f);
            icon.preserveAspect = true;

            _amountTexts[i] = UiBuilder.CreateText(cell, "Amount", 28f, UiPalette.Text1);
            UiBuilder.Place(_amountTexts[i].rectTransform, 58f, 26f, cellWidth - 66f, 36f);
        }
    }

    /// <summary>
    /// 보유량을 다시 읽는다. 재화가 바뀔 수 있는 행동 뒤에 호출한다
    /// </summary>
    public void Refresh()
    {
        if (CurrencyManager.Instance == null)
            return;

        for (int i = 0; i < _shownTypes.Length; i++)
            _amountTexts[i].text = FormatAmount(CurrencyManager.Instance.GetAmount(_shownTypes[i]));
    }

    //재화 아이콘 스프라이트 경로
    private static string GetIconPath(CurrencyType type) => type switch
    {
        CurrencyType.Gold => "Currency/currency_gold",
        CurrencyType.Point => "Currency/currency_point",
        CurrencyType.GoldenGlovePoint => "Currency/currency_goldenglove_point",
        CurrencyType.NormalTicket => "Currency/currency_normal_ticket",
        CurrencyType.SignatureTicket => "Currency/currency_signature_ticket",
        CurrencyType.TrainCard => "Currency/currency_train_card",
        _ => throw new System.ArgumentException($"아이콘이 없는 재화: {type}")
    };

    //네 자리가 넘어가면 천 단위로 줄여 표기한다 (칸이 좁다)
    private static string FormatAmount(int amount)
    {
        if (amount < 10000)
            return amount.ToString();

        return (amount / 1000) + "k";
    }
}
