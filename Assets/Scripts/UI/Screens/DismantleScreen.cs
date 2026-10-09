using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 분해. 포인트·훈련 카드·골글 포인트로 돌려받는다
/// </summary>
public class DismantleScreen : CardActionScreen
{
    private TextMeshProUGUI _targetText;
    private TextMeshProUGUI _previewText;
    private TextMeshProUGUI _noticeText;
    private Button _dismantleButton;

    private int _targetInstanceId = InventoryManager.InvalidInstanceId;

    public DismantleScreen(Action onBack,
        Action<string, int, Func<CardDisplayInfo, bool>, Action<List<int>>> onPickCard,
        Action<string, bool> onNotify)
        : base("카드 분해", onBack, onPickCard, onNotify)
    {
    }

    protected override string ScreenName => "DismantleScreen";

    public override void Refresh()
    {
        base.Refresh();

        if (!CardDisplayInfo.TryCreate(_targetInstanceId, out CardDisplayInfo info))
        {
            _targetInstanceId = InventoryManager.InvalidInstanceId;
            _targetText.text = "분해할 카드를 고르세요";
            _targetText.color = UiPalette.TextOff;
            _previewText.text = string.Empty;
            _dismantleButton.interactable = false;
            return;
        }

        _targetText.text = BuildCardSummary(info);
        _targetText.color = UiPalette.Text1;

        if (DismantleManager.Instance == null)
            return;

        if (!DismantleManager.Instance.CanDismantle(_targetInstanceId))
        {
            _previewText.text = info.IsLocked
                ? "잠긴 카드는 분해할 수 없습니다"
                : "이 카드는 분해할 수 없습니다 (라인업 배치 확인)";
            _previewText.color = UiPalette.StateOut;
            _dismantleButton.interactable = false;
            return;
        }

        DismantleResult preview = DismantleManager.Instance.GetPreview(_targetInstanceId);

        if (preview == null)
        {
            _previewText.text = "보상을 계산하지 못했습니다";
            _previewText.color = UiPalette.StateOut;
            _dismantleButton.interactable = false;
            return;
        }

        _previewText.text = $"포인트 {preview.Point}" +
                            $"   ·   훈련 카드 {preview.TrainCard} (확률)" +
                            $"   ·   골글 포인트 {preview.GoldenGlovePoint}";
        _previewText.color = UiPalette.Text2;
        _dismantleButton.interactable = true;
    }

    protected override void BuildBody()
    {
        Button pickButton = UiBuilder.CreateButton(Body, "PickTarget", "분해할 카드 선택", 34f, false, PickTarget);
        UiBuilder.PlaceRow(pickButton.GetComponent<RectTransform>(), 0f, 96f);

        _targetText = UiBuilder.CreateText(Body, "Target", 32f, UiPalette.TextOff);
        UiBuilder.PlaceRow(_targetText.rectTransform, 112f, 48f, 8f);

        TextMeshProUGUI previewTitle = UiBuilder.CreateText(Body, "PreviewTitle", 34f, UiPalette.Text3);
        UiBuilder.PlaceRow(previewTitle.rectTransform, 190f, 44f, 8f);
        previewTitle.text = "돌려받는 것";

        _previewText = UiBuilder.CreateText(Body, "Preview", 32f, UiPalette.Text2);
        UiBuilder.PlaceRow(_previewText.rectTransform, 240f, 48f, 8f);

        _dismantleButton = UiBuilder.CreateButton(Body, "DismantleButton", "분해", 36f, true, Dismantle);
        UiBuilder.PlaceRow(_dismantleButton.GetComponent<RectTransform>(), 310f, 110f);

        _noticeText = UiBuilder.CreateText(Body, "Notice", 28f, UiPalette.TextOff);
        UiBuilder.PlaceRow(_noticeText.rectTransform, 436f, 44f, 8f);
        _noticeText.text = "잠긴 카드와 라인업에 들어간 카드는 분해되지 않습니다";
    }

    private void PickTarget()
    {
        PickCard("분해할 카드", info => !info.IsLocked, instanceId =>
        {
            _targetInstanceId = instanceId;
            Refresh();
        });
    }

    private void Dismantle()
    {
        if (DismantleManager.Instance == null)
            return;

        DismantleResult result = DismantleManager.Instance.Dismantle(_targetInstanceId);

        if (result == null)
        {
            Notify("분해에 실패했습니다", false);
            Refresh();
            return;
        }

        _targetInstanceId = InventoryManager.InvalidInstanceId;

        Notify($"분해 완료 — 포인트 {result.Point} · 훈련카드 {result.TrainCard}", true);
        Refresh();
    }
}
