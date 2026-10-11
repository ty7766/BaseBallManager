using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 훈련과 훈련돌파. 훈련은 4개 스탯에 무작위로 분배되고, 돌파는 상한을 늘린다
/// </summary>
public class TrainScreen : CardActionScreen
{
    private const int StatCount = 4;

    /// <summary>
    /// 화면 제목
    /// </summary>
    protected override string Title => "훈련";

    [SerializeField] private TextMeshProUGUI _targetText;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private TextMeshProUGUI _breakthroughText;
    [SerializeField] private TextMeshProUGUI[] _statTexts = new TextMeshProUGUI[StatCount];
    [SerializeField] private Button _trainButton;
    [SerializeField] private Button _breakthroughButton;

    private int _targetInstanceId = InventoryManager.InvalidInstanceId;

    public override void Refresh()
    {
        base.Refresh();

        if (!CardDisplayInfo.TryCreate(_targetInstanceId, out CardDisplayInfo info))
        {
            _targetInstanceId = InventoryManager.InvalidInstanceId;
            _targetText.text = "훈련할 카드를 고르세요";
            _targetText.color = UiPalette.TextOff;
            _costText.text = string.Empty;
            _breakthroughText.text = string.Empty;
            _trainButton.interactable = false;
            _breakthroughButton.interactable = false;

            for (int i = 0; i < StatCount; i++)
                _statTexts[i].text = string.Empty;

            return;
        }

        _targetText.text = BuildCardSummary(info);
        _targetText.color = UiPalette.Text1;

        string[] labels = info.StatLabels;

        for (int i = 0; i < StatCount; i++)
        {
            int trainDelta = info.GetTrainDelta(i);

            _statTexts[i].text = $"{labels[i]}  {info.GetBaseStat(i)}" +
                                 (trainDelta > 0 ? $"  +{trainDelta}" : string.Empty) +
                                 $"  →  {info.GetFinalStat(i)}";
        }

        if (TrainManager.Instance == null)
            return;

        (int pointCost, int trainCardCost) cost = TrainManager.Instance.GetTrainCost(info.TrainLevel);
        int point = CurrencyManager.Instance == null ? 0 : CurrencyManager.Instance.GetAmount(CurrencyType.Point);
        int trainCard = CurrencyManager.Instance == null ? 0 : CurrencyManager.Instance.GetAmount(CurrencyType.TrainCard);

        _costText.text = $"훈련 Lv.{info.TrainLevel} / {info.GetMaxTrainLevel()}" +
                         $"   ·   포인트 {cost.pointCost} (보유 {point})" +
                         $"   ·   훈련카드 {cost.trainCardCost} (보유 {trainCard})";

        bool canTrain = TrainManager.Instance.CanTrain(_targetInstanceId)
                        && point >= cost.pointCost && trainCard >= cost.trainCardCost;

        _trainButton.interactable = canTrain;

        if (BreakthroughManager.Instance == null)
            return;

        bool canBreakthrough = BreakthroughManager.Instance.CanBreakthrough(_targetInstanceId);
        int breakthroughCost = BreakthroughManager.Instance.GetBreakthroughCost(_targetInstanceId);

        if (info.BreakthroughUsed)
        {
            _breakthroughText.text = $"돌파 완료 — 훈련 상한 {info.GetMaxTrainLevel()}";
            _breakthroughText.color = UiPalette.StatePitching;
            _breakthroughButton.interactable = false;
            return;
        }

        int ownedBreakthroughCard = CurrencyManager.Instance == null
            ? 0
            : CurrencyManager.Instance.GetAmount(CurrencyType.BreakthroughCard);

        _breakthroughText.text = breakthroughCost == BreakthroughManager.InvalidCost
            ? "이 카드는 돌파할 수 없습니다"
            : $"돌파 카드 {breakthroughCost}장 필요 (보유 {ownedBreakthroughCard})";
        _breakthroughText.color = UiPalette.Text3;

        _breakthroughButton.interactable = canBreakthrough
            && breakthroughCost != BreakthroughManager.InvalidCost
            && ownedBreakthroughCard >= breakthroughCost;
    }

    protected override void BindEvents()
    {
        base.BindEvents();

        BindButton("PickTarget", PickTarget);
        Bind(_trainButton, Train);
        Bind(_breakthroughButton, Breakthrough);
    }

    protected override void BuildBody()
    {
        Button pickButton = UiBuilder.CreateButton(Body, "PickTarget", "훈련할 카드 선택", 34f, false, PickTarget);
        UiBuilder.PlaceRow(pickButton.GetComponent<RectTransform>(), 0f, 96f);

        _targetText = UiBuilder.CreateText(Body, "Target", 32f, UiPalette.TextOff);
        UiBuilder.PlaceRow(_targetText.rectTransform, 112f, 48f, 8f);

        for (int i = 0; i < StatCount; i++)
        {
            _statTexts[i] = UiBuilder.CreateText(Body, "Stat" + i, 32f, UiPalette.Text2);
            UiBuilder.PlaceRow(_statTexts[i].rectTransform, 176f + i * 52f, 48f, 8f);
        }

        _costText = UiBuilder.CreateText(Body, "Cost", 30f, UiPalette.Text3);
        UiBuilder.PlaceRow(_costText.rectTransform, 400f, 48f, 8f);

        _trainButton = UiBuilder.CreateButton(Body, "TrainButton", "훈련", 34f, true, Train);
        UiBuilder.PlaceRow(_trainButton.GetComponent<RectTransform>(), 456f, 100f);

        UiBuilder.CreateDivider(Body, 0f, 580f, 1016f);

        TextMeshProUGUI breakthroughTitle = UiBuilder.CreateText(Body, "BreakthroughTitle", 34f, UiPalette.Text2);
        UiBuilder.PlaceRow(breakthroughTitle.rectTransform, 600f, 44f, 8f);
        breakthroughTitle.text = "훈련 돌파 (상한 해금)";

        _breakthroughText = UiBuilder.CreateText(Body, "Breakthrough", 30f, UiPalette.Text3);
        UiBuilder.PlaceRow(_breakthroughText.rectTransform, 648f, 44f, 8f);

        _breakthroughButton = UiBuilder.CreateButton(Body, "BreakthroughButton", "돌파", 34f, false, Breakthrough);
        UiBuilder.PlaceRow(_breakthroughButton.GetComponent<RectTransform>(), 700f, 100f);
    }

    private void PickTarget()
    {
        PickCard("훈련할 카드", info => true, instanceId =>
        {
            _targetInstanceId = instanceId;
            Refresh();
        });
    }

    private void Train()
    {
        if (TrainManager.Instance == null)
            return;

        if (!TrainManager.Instance.Train(_targetInstanceId))
        {
            Notify("훈련에 실패했습니다 (비용·상한 확인)", false);
            Refresh();
            return;
        }

        Refresh();

        for (int i = 0; i < StatCount; i++)
        {
            UiEffects.Flash(_statTexts[i], UiPalette.StaminaHigh, 0.5f, i * 0.06f);
            UiEffects.Pop(_statTexts[i].transform, 0.94f, 0.26f, i * 0.06f);
        }

        UiEffects.Burst(_targetText.rectTransform, UiEffectKind.Enhance);
        Notify("훈련했습니다", true);
    }

    private void Breakthrough()
    {
        if (BreakthroughManager.Instance == null)
            return;

        if (!BreakthroughManager.Instance.Breakthrough(_targetInstanceId))
        {
            Notify("돌파에 실패했습니다 (돌파 카드 확인)", false);
            Refresh();
            return;
        }

        UiEffects.Flash(_breakthroughText, UiPalette.AccentGold, 0.7f);
        UiEffects.Burst(_breakthroughText.rectTransform, UiEffectKind.GradeUp);

        Notify("훈련 상한을 늘렸습니다", true);
        Refresh();
    }
}
