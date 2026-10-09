using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 상단 재화 표시 줄. 로비와 수급·육성 화면이 공유한다
/// </summary>
public class CurrencyBarView
{
    /// <summary>
    /// 줄 높이
    /// </summary>
    public const float BarHeight = 88f;

    private static readonly CurrencyType[] ShownTypes =
    {
        CurrencyType.Gold,
        CurrencyType.Point,
        CurrencyType.GoldenGlovePoint,
        CurrencyType.NormalTicket,
        CurrencyType.SignatureTicket,
        CurrencyType.TrainCard
    };

    private static readonly string[] IconPaths =
    {
        "Currency/currency_gold",
        "Currency/currency_point",
        "Currency/currency_goldenglove_point",
        "Currency/currency_normal_ticket",
        "Currency/currency_signature_ticket",
        "Currency/currency_train_card"
    };

    public RectTransform Root => _root;

    private readonly RectTransform _root;
    private readonly TextMeshProUGUI[] _amountTexts = new TextMeshProUGUI[ShownTypes.Length];

    public CurrencyBarView(Transform parent)
    {
        _root = UiBuilder.CreateRect(parent, "CurrencyBar");

        Image background = UiBuilder.CreateImage(_root, "Background", UiPalette.BgPanel,
            UiSprites.PanelSmall, Image.Type.Sliced);
        UiBuilder.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

        float cellWidth = 1032f / ShownTypes.Length;

        for (int i = 0; i < ShownTypes.Length; i++)
        {
            RectTransform cell = UiBuilder.CreateRect(_root, "Cell");
            UiBuilder.Place(cell, i * cellWidth, 0f, cellWidth, BarHeight);

            Image icon = UiBuilder.CreateImage(cell, "Icon", Color.white, IconPaths[i]);
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

        for (int i = 0; i < ShownTypes.Length; i++)
            _amountTexts[i].text = FormatAmount(CurrencyManager.Instance.GetAmount(ShownTypes[i]));
    }

    //네 자리가 넘어가면 천 단위로 줄여 표기한다 (칸이 좁다)
    private static string FormatAmount(int amount)
    {
        if (amount < 10000)
            return amount.ToString();

        return (amount / 1000) + "k";
    }
}
