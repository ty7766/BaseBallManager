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

    /// <summary>
    /// 아래 목록에서 고른 카드를 훈련 대상으로 삼는다
    /// </summary>
    protected override void OnSelectionChanged(List<int> selectedIds)
    {
        _targetInstanceId = selectedIds.Count > 0
            ? selectedIds[0]
            : InventoryManager.InvalidInstanceId;

        Refresh();
    }

    [SerializeField] private TextMeshProUGUI _targetText;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private TextMeshProUGUI _breakthroughText;
    [SerializeField] private TextMeshProUGUI[] _statTexts = new TextMeshProUGUI[StatCount];
    [SerializeField] private Button _trainButton;
    [SerializeField] private Button _breakthroughButton;
    [SerializeField] private Button _trainTabButton;
    [SerializeField] private Button _breakthroughTabButton;
    [SerializeField] private RectTransform _trainPanel;
    [SerializeField] private RectTransform _breakthroughPanel;

    private int _targetInstanceId = InventoryManager.InvalidInstanceId;
    private bool _showBreakthrough;

    public override void Refresh()
    {
        base.Refresh();

        UiTabStyle.Apply(_trainTabButton, !_showBreakthrough);
        UiTabStyle.Apply(_breakthroughTabButton, _showBreakthrough);

        _trainPanel.gameObject.SetActive(!_showBreakthrough);
        _breakthroughPanel.gameObject.SetActive(_showBreakthrough);

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

        Bind(_trainButton, Train);
        Bind(_breakthroughButton, Breakthrough);
        Bind(_trainTabButton, () => SelectTab(false));
        Bind(_breakthroughTabButton, () => SelectTab(true));
    }

    protected override void BuildBody()
    {
        _trainTabButton = UiBuilder.CreateButton(Body, "TrainTab", "훈련", 34f, true,
            () => SelectTab(false));
        UiBuilder.Place(_trainTabButton.GetComponent<RectTransform>(), 0f, 0f, 500f, 84f);

        _breakthroughTabButton = UiBuilder.CreateButton(Body, "BreakthroughTab", "돌파", 34f, false,
            () => SelectTab(true));
        UiBuilder.Place(_breakthroughTabButton.GetComponent<RectTransform>(), 516f, 0f, 500f, 84f);

        _targetText = UiBuilder.CreateText(Body, "Target", 32f, UiPalette.TextOff);
        UiBuilder.PlaceRow(_targetText.rectTransform, 104f, 48f, 8f);

        // --- 훈련 탭 ---
        _trainPanel = UiBuilder.CreateRect(Body, "TrainPanel");
        UiBuilder.Stretch(_trainPanel, 0f, 168f, 0f, 0f);

        for (int i = 0; i < StatCount; i++)
        {
            _statTexts[i] = UiBuilder.CreateText(_trainPanel, "Stat" + i, 32f, UiPalette.Text2);
            UiBuilder.PlaceRow(_statTexts[i].rectTransform, i * 52f, 48f, 8f);
        }

        _costText = UiBuilder.CreateText(_trainPanel, "Cost", 30f, UiPalette.Text3);
        UiBuilder.PlaceRow(_costText.rectTransform, 224f, 48f, 8f);

        _trainButton = UiBuilder.CreateButton(_trainPanel, "TrainButton", "훈련", 34f, true, Train);
        UiBuilder.PlaceRow(_trainButton.GetComponent<RectTransform>(), 288f, 100f);

        // --- 돌파 탭 ---
        _breakthroughPanel = UiBuilder.CreateRect(Body, "BreakthroughPanel");
        UiBuilder.Stretch(_breakthroughPanel, 0f, 168f, 0f, 0f);

        TextMeshProUGUI breakthroughTitle = UiBuilder.CreateText(_breakthroughPanel,
            "BreakthroughTitle", 34f, UiPalette.Text2);
        UiBuilder.PlaceRow(breakthroughTitle.rectTransform, 0f, 44f, 8f);
        breakthroughTitle.text = "훈련 상한을 해금합니다";

        _breakthroughText = UiBuilder.CreateText(_breakthroughPanel, "Breakthrough", 30f, UiPalette.Text3);
        UiBuilder.PlaceRow(_breakthroughText.rectTransform, 56f, 44f, 8f);

        _breakthroughButton = UiBuilder.CreateButton(_breakthroughPanel, "BreakthroughButton",
            "돌파", 34f, true, Breakthrough);
        UiBuilder.PlaceRow(_breakthroughButton.GetComponent<RectTransform>(), 120f, 100f);

        _breakthroughPanel.gameObject.SetActive(false);
    }

    //훈련과 돌파를 탭으로 나눈다
    private void SelectTab(bool breakthrough)
    {
        _showBreakthrough = breakthrough;
        Refresh();
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
