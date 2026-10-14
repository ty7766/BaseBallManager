using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 분해. 여러 장을 담아 한 번에 분해하고 포인트·훈련 카드·골글 포인트로 돌려받는다
/// </summary>
public class DismantleScreen : CardActionScreen
{
    private const int MaxBatchCount = 30;
    private const float PickedCardWidth = 150f;
    private const float PickedGap = 12f;
    private const float RewardIconSize = 56f;

    private static readonly string[] RewardIconPaths =
    {
        "Currency/currency_point",
        "Currency/currency_train_card",
        "Currency/currency_goldenglove_point"
    };

    /// <summary>
    /// 화면 제목
    /// </summary>
    protected override string Title => "분해";

    /// <summary>
    /// 한 번에 여러 장을 담는다
    /// </summary>
    protected override int RequiredSelectionCount => MaxBatchCount;

    [SerializeField] private RectTransform _pickedContent;
    [SerializeField] private TextMeshProUGUI _pickedEmptyText;
    [SerializeField] private TextMeshProUGUI[] _rewardTexts = new TextMeshProUGUI[3];
    [SerializeField] private TextMeshProUGUI _noticeText;
    [SerializeField] private Button _dismantleButton;

    private readonly List<int> _pickedIds = new List<int>(MaxBatchCount);
    private readonly List<CardView> _pickedCards = new List<CardView>(MaxBatchCount);

    /// <summary>
    /// 분해할 수 없는 카드도 목록에 두되 이유를 덮어 보여준다
    /// </summary>
    protected override string GetBlockedReason(CardDisplayInfo info)
    {
        if (info.IsLocked)
            return "잠금 카드";

        if (LineUpManager.Instance != null && LineUpManager.Instance.IsCardAssigned(info.InstanceId))
            return "라인업 카드";

        return null;
    }

    /// <summary>
    /// 담은 카드가 바뀌었다
    /// </summary>
    protected override void OnSelectionChanged(List<int> selectedIds)
    {
        _pickedIds.Clear();
        _pickedIds.AddRange(selectedIds);

        Refresh();
    }

    public override void Refresh()
    {
        base.Refresh();

        RefreshPickedRow();
        RefreshRewards();
    }

    protected override void BindEvents()
    {
        base.BindEvents();

        Bind(_dismantleButton, Dismantle);
    }

    protected override void BuildBody()
    {
        float pickedHeight = PickedCardWidth * CardView.BaseHeight / CardView.BaseWidth;

        TextMeshProUGUI pickedTitle = UiBuilder.CreateText(Body, "PickedTitle", 32f, UiPalette.Text3);
        UiBuilder.PlaceRow(pickedTitle.rectTransform, 0f, 44f, 8f);
        pickedTitle.text = "분해할 카드";

        ScrollRect pickedScroll = UiBuilder.CreateTableScroll(Body, "PickedScroll", out _pickedContent);
        UiBuilder.PlaceRow(pickedScroll.GetComponent<RectTransform>(), 52f, pickedHeight + 16f);
        pickedScroll.vertical = false;

        _pickedEmptyText = UiBuilder.CreateText(Body, "PickedEmpty", 30f, UiPalette.TextOff,
            TextAlignmentOptions.Center);
        UiBuilder.PlaceRow(_pickedEmptyText.rectTransform, 52f + pickedHeight * 0.4f, 44f, 8f);
        _pickedEmptyText.text = "아래에서 분해할 카드를 고르세요";

        float rewardTop = 52f + pickedHeight + 60f;

        TextMeshProUGUI rewardTitle = UiBuilder.CreateText(Body, "RewardTitle", 34f, UiPalette.Text3);
        UiBuilder.PlaceRow(rewardTitle.rectTransform, rewardTop, 44f, 8f);
        rewardTitle.text = "반환 재화";

        //재화는 아이콘만 두고 글자는 수량만 적는다
        for (int i = 0; i < RewardIconPaths.Length; i++)
        {
            float left = i * 300f;

            Image icon = UiBuilder.CreateImage(Body, "RewardIcon" + i, Color.white, RewardIconPaths[i]);
            UiBuilder.Place(icon.rectTransform, left, rewardTop + 52f, RewardIconSize, RewardIconSize);

            _rewardTexts[i] = UiBuilder.CreateText(Body, "RewardAmount" + i, 34f, UiPalette.Text1);
            UiBuilder.Place(_rewardTexts[i].rectTransform,
                left + RewardIconSize + 12f, rewardTop + 56f, 200f, 48f);
        }

        _dismantleButton = UiBuilder.CreateButton(Body, "DismantleButton", "분해", 36f, true, Dismantle);
        UiBuilder.PlaceRow(_dismantleButton.GetComponent<RectTransform>(), rewardTop + 140f, 110f);

        _noticeText = UiBuilder.CreateText(Body, "Notice", 28f, UiPalette.TextOff);
        UiBuilder.PlaceRow(_noticeText.rectTransform, rewardTop + 266f, 44f, 8f);
        _noticeText.text = "잠긴 카드와 라인업에 들어간 카드는 분해되지 않습니다";
    }

    //담은 카드를 가로 한 줄로 깐다. 칸은 필요한 만큼만 만들고 재사용한다
    private void RefreshPickedRow()
    {
        for (int i = 0; i < _pickedIds.Count; i++)
        {
            CardView card = EnsurePickedCard(i);

            card.Root.gameObject.SetActive(true);

            if (CardDisplayInfo.TryCreate(_pickedIds[i], out CardDisplayInfo info))
                card.SetCard(info);
        }

        for (int i = _pickedIds.Count; i < _pickedCards.Count; i++)
            _pickedCards[i].Root.gameObject.SetActive(false);

        _pickedEmptyText.gameObject.SetActive(_pickedIds.Count == 0);
        _pickedContent.sizeDelta = new Vector2(_pickedIds.Count * (PickedCardWidth + PickedGap), 0f);
    }

    private CardView EnsurePickedCard(int index)
    {
        while (_pickedCards.Count <= index)
        {
            int slot = _pickedCards.Count;
            CardView created = CardView.Bake(_pickedContent, PickedCardWidth, false);

            UiBuilder.Place(created.Root, slot * (PickedCardWidth + PickedGap), 0f,
                PickedCardWidth, PickedCardWidth * CardView.BaseHeight / CardView.BaseWidth);

            _pickedCards.Add(created);
        }

        return _pickedCards[index];
    }

    //담은 카드 전부의 보상을 더해 아이콘 옆 수량만 바꾼다
    private void RefreshRewards()
    {
        int point = 0;
        int trainCard = 0;
        int goldenGlovePoint = 0;
        int usable = 0;

        if (DismantleManager.Instance != null)
        {
            foreach (int instanceId in _pickedIds)
            {
                if (!DismantleManager.Instance.CanDismantle(instanceId))
                    continue;

                DismantleResult preview = DismantleManager.Instance.GetPreview(instanceId);

                if (preview == null)
                    continue;

                point += preview.Point;
                trainCard += preview.TrainCard;
                goldenGlovePoint += preview.GoldenGlovePoint;
                usable++;
            }
        }

        _rewardTexts[0].text = point.ToString();
        _rewardTexts[1].text = trainCard.ToString();
        _rewardTexts[2].text = goldenGlovePoint.ToString();

        _dismantleButton.interactable = usable > 0;
    }

    //담은 카드를 한 장씩 분해하고 합계를 알린다
    private void Dismantle()
    {
        if (DismantleManager.Instance == null)
            return;

        int point = 0;
        int trainCard = 0;
        int goldenGlovePoint = 0;
        int done = 0;

        foreach (int instanceId in _pickedIds)
        {
            DismantleResult result = DismantleManager.Instance.Dismantle(instanceId);

            if (result == null)
                continue;

            point += result.Point;
            trainCard += result.TrainCard;
            goldenGlovePoint += result.GoldenGlovePoint;
            done++;
        }

        if (done == 0)
        {
            Notify("분해에 실패했습니다", false);
            Refresh();
            return;
        }

        ClearSelection();
        Notify($"{done}장 분해 — 포인트 {point} · 훈련카드 {trainCard} · 골글 {goldenGlovePoint}", true);
    }
}
