using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 강화. 같은 카드를 재료로 쓰거나 강화 전용 카드를 쓴다
/// </summary>
public class EnhanceScreen : CardActionScreen
{
    private TextMeshProUGUI _targetText;
    private TextMeshProUGUI _identicalText;
    private TextMeshProUGUI _enhanceCardText;
    private Button _identicalButton;
    private Button _enhanceCardButton;

    private int _targetInstanceId = InventoryManager.InvalidInstanceId;

    public EnhanceScreen(Action onBack,
        Action<string, int, Func<CardDisplayInfo, bool>, Action<List<int>>> onPickCard,
        Action<string, bool> onNotify)
        : base("강화", onBack, onPickCard, onNotify)
    {
    }

    protected override string ScreenName => "EnhanceScreen";

    public override void Refresh()
    {
        base.Refresh();

        if (!CardDisplayInfo.TryCreate(_targetInstanceId, out CardDisplayInfo info))
        {
            _targetInstanceId = InventoryManager.InvalidInstanceId;
            _targetText.text = "강화할 카드를 고르세요";
            _targetText.color = UiPalette.TextOff;
            _identicalText.text = string.Empty;
            _enhanceCardText.text = string.Empty;
            _identicalButton.interactable = false;
            _enhanceCardButton.interactable = false;
            return;
        }

        _targetText.text = BuildCardSummary(info);
        _targetText.color = UiPalette.Text1;

        bool canEnhance = EnhanceManager.Instance != null && EnhanceManager.Instance.CanEnhance(_targetInstanceId);

        List<CardInstance> identical = EnhanceManager.Instance == null
            ? new List<CardInstance>()
            : EnhanceManager.Instance.GetIdenticalCards(_targetInstanceId);

        _identicalText.text = $"같은 카드 {identical.Count}장 보유  ·  1장을 재료로 써서 +1";
        _identicalButton.interactable = canEnhance && identical.Count > 0;

        (CurrencyType enhanceCardType, int count) required = EnhanceManager.Instance == null
            ? (CurrencyType.None, 0)
            : EnhanceManager.Instance.GetRequiredEnhanceCard(_targetInstanceId);

        int owned = CurrencyManager.Instance == null || required.enhanceCardType == CurrencyType.None
            ? 0
            : CurrencyManager.Instance.GetAmount(required.enhanceCardType);

        _enhanceCardText.text = required.enhanceCardType == CurrencyType.None
            ? "이 카드에 맞는 전용 카드가 없습니다"
            : $"전용 카드 {GetEnhanceCardLabel(required.enhanceCardType)}  필요 {required.count}장 / 보유 {owned}장";

        _enhanceCardButton.interactable = canEnhance && required.count > 0 && owned >= required.count;

        if (!canEnhance)
            _identicalText.text = "이 카드는 더 강화할 수 없습니다";
    }

    protected override void BuildBody()
    {
        Button pickButton = UiBuilder.CreateButton(Body, "PickTarget", "강화할 카드 선택", 34f, false, PickTarget);
        UiBuilder.PlaceRow(pickButton.GetComponent<RectTransform>(), 0f, 96f);

        _targetText = UiBuilder.CreateText(Body, "Target", 32f, UiPalette.TextOff);
        UiBuilder.PlaceRow(_targetText.rectTransform, 112f, 48f, 8f);

        UiBuilder.CreateDivider(Body, 0f, 180f, 1016f);

        TextMeshProUGUI identicalTitle = UiBuilder.CreateText(Body, "IdenticalTitle", 34f, UiPalette.Text2);
        UiBuilder.PlaceRow(identicalTitle.rectTransform, 200f, 44f, 8f);
        identicalTitle.text = "① 같은 카드로 강화";

        _identicalText = UiBuilder.CreateText(Body, "Identical", 30f, UiPalette.Text3);
        UiBuilder.PlaceRow(_identicalText.rectTransform, 248f, 44f, 8f);

        _identicalButton = UiBuilder.CreateButton(Body, "IdenticalButton", "같은 카드로 강화", 32f, true,
            PickIdenticalMaterial);
        UiBuilder.PlaceRow(_identicalButton.GetComponent<RectTransform>(), 300f, 96f);

        UiBuilder.CreateDivider(Body, 0f, 420f, 1016f);

        TextMeshProUGUI enhanceCardTitle = UiBuilder.CreateText(Body, "EnhanceCardTitle", 34f, UiPalette.Text2);
        UiBuilder.PlaceRow(enhanceCardTitle.rectTransform, 440f, 44f, 8f);
        enhanceCardTitle.text = "② 강화 전용 카드로 강화";

        _enhanceCardText = UiBuilder.CreateText(Body, "EnhanceCard", 30f, UiPalette.Text3);
        UiBuilder.PlaceRow(_enhanceCardText.rectTransform, 488f, 44f, 8f);

        _enhanceCardButton = UiBuilder.CreateButton(Body, "EnhanceCardButton", "전용 카드로 강화", 32f, true,
            EnhanceWithCards);
        UiBuilder.PlaceRow(_enhanceCardButton.GetComponent<RectTransform>(), 540f, 96f);
    }

    private void PickTarget()
    {
        PickCard("강화할 카드", info => true, instanceId =>
        {
            _targetInstanceId = instanceId;
            Refresh();
        });
    }

    //재료는 같은 카드 목록에 있는 것만 고를 수 있다
    private void PickIdenticalMaterial()
    {
        if (EnhanceManager.Instance == null)
            return;

        List<CardInstance> identical = EnhanceManager.Instance.GetIdenticalCards(_targetInstanceId);
        HashSet<int> allowed = new HashSet<int>();

        foreach (CardInstance card in identical)
            allowed.Add(card.InstanceId);

        if (allowed.Count == 0)
        {
            Notify("재료로 쓸 같은 카드가 없습니다", false);
            return;
        }

        PickCard("재료로 쓸 같은 카드", info => allowed.Contains(info.InstanceId), materialId =>
        {
            if (!EnhanceManager.Instance.EnhanceWithIdenticalCard(_targetInstanceId, materialId))
            {
                Notify("강화에 실패했습니다", false);
                Refresh();
                return;
            }

            Notify("강화했습니다", true);
            Refresh();
        });
    }

    private void EnhanceWithCards()
    {
        if (EnhanceManager.Instance == null)
            return;

        if (!EnhanceManager.Instance.EnhanceWithEnhanceCards(_targetInstanceId))
        {
            Notify("전용 카드가 부족합니다", false);
            Refresh();
            return;
        }

        Notify("강화했습니다", true);
        Refresh();
    }

    private static string GetEnhanceCardLabel(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.EnhanceCardStar3 => "3성",
            CurrencyType.EnhanceCardStar4 => "4성",
            CurrencyType.EnhanceCardStar5 => "5성",
            CurrencyType.EnhanceCardSignature => "시그니쳐",
            CurrencyType.EnhanceCardGoldenGlove => "골든글러브",
            _ => "-"
        };
    }
}
