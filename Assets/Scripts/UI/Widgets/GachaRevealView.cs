using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 뽑기 결과 자리. 1장은 가운데 크게, 10장은 4 · 4 · 2 줄로 뒷면을 나눠 깔고 카드 한가운데에 밀기 화살표를 띄운다.
/// 아래 건너뛰기를 누르면 남은 뒷면을 한꺼번에 뒤집고, 다 뒤집히면 같은 자리 버튼이 확인으로 바뀐다.
/// 카드는 처음 쓸 때 만들어 두고 돌려쓴다 (1장용 1 + 10장용 10)
/// </summary>
public sealed class GachaRevealView
{
    /// <summary>
    /// 뒷면이 하나도 남지 않았는가
    /// </summary>
    public bool IsAllRevealed => _faceDownCount == 0;

    /// <summary>
    /// 결과 자리 가운데보다 카드 묶음 가운데가 이만큼 위에 있다 (아래 버튼 칸은 비우고, 안내 줄은 카드 아래에 붙는다)
    /// </summary>
    public const float CardsCenterRaise = (SkipHeight + SkipGap + HintGap + HintHeight) * 0.5f;

    private const int MaxCards = 10;
    private const float SingleWidth = 540f;
    //10장은 열 사이를 넓게 벌려 카드를 옆으로 밀어도 이웃 카드에 닿지 않게 한다
    private const float MultiWidth = 196f;
    private const float MultiColumnGap = 64f;
    private const float MultiRowGap = 44f;
    //새는 빛이 가장 멀리 닿는 거리. 10장은 이웃 카드를 덮지 않도록 열 간격 남짓으로 줄인다
    private const float SingleSpillReach = SingleWidth * 0.75f;
    private const float MultiSpillReach = MultiColumnGap + MultiWidth * 0.35f;
    private const float HintGap = 30f;
    private const float HintHeight = 44f;
    //건너뛰기 · 확인 버튼은 결과 자리 맨 아래에 붙인다
    private const string SkipLabel = "건너뛰기";
    private const string ConfirmLabel = "확인";
    private const float SkipWidth = 320f;
    private const float SkipHeight = 96f;
    private const float SkipGap = 24f;
    //화살표가 왼쪽에서 나타나 오른쪽으로 흐르며 사라지는 한 번의 길이와 이동 거리(카드 폭 비율)
    private const float HintCycleSeconds = 1.6f;
    private const float HintTravel = 0.22f;
    //무대의 소환 빛이 모였다 터지는 순간까지 기다렸다가 카드를 한 장씩 나눠 준다 (GachaRevealStage 의 소환 플립북 절반)
    private const float DealPause = 0.42f;
    private const float DealSeconds = 0.36f;
    private const float DealStagger = 0.07f;
    private const float SingleDealSeconds = 0.5f;
    //1장은 카드 높이의 이 비율만큼 아래에서 떠오른다
    private const float SingleDealRise = 0.3f;

    private static readonly int[] MultiRowCounts = { 4, 4, 2 };

    private readonly RectTransform _group;
    private readonly RectTransform _hint;
    private readonly Button _skipButton;
    private readonly TextMeshProUGUI _skipLabel;
    private readonly List<GachaRevealCard> _multiCards = new List<GachaRevealCard>(MaxCards);
    private readonly List<GachaRevealCard> _shownCards = new List<GachaRevealCard>(MaxCards);

    private readonly Action<GachaRevealCard> _onCardRevealed;

    private Action _onConfirm;
    private GachaRevealCard _singleCard;
    private int _faceDownCount;
    private UiTween _hintTween;
    private UiTween _dealTween;

    /// <summary>
    /// 결과 자리를 만든다. 카드가 한 장 뒤집힐 때마다 onCardRevealed(무대가 같은 색으로 반응한다)
    /// </summary>
    public GachaRevealView(RectTransform area, Action<GachaRevealCard> onCardRevealed)
    {
        _onCardRevealed = onCardRevealed;

        RectTransform cardsArea = UiBuilder.CreateRect(area, "RevealArea");
        UiBuilder.Stretch(cardsArea, 0f, 0f, 0f, SkipHeight + SkipGap);

        _group = UiBuilder.CreateRect(cardsArea, "Reveal");
        _group.anchorMin = new Vector2(0.5f, 0.5f);
        _group.anchorMax = new Vector2(0.5f, 0.5f);
        _group.pivot = new Vector2(0.5f, 0.5f);

        _hint = UiBuilder.CreateRect(_group, "SwipeHint");

        TextMeshProUGUI label = UiBuilder.CreateText(_hint, "Label", 30f, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.Stretch(label.rectTransform, 0f, 0f, 0f, 0f);
        label.text = "카드를 누르거나 오른쪽으로 밀어서 뒤집기";

        _skipButton = UiBuilder.CreateButton(area, "SkipButton", SkipLabel, 34f, false, OnBottomClicked, out _skipLabel);
        RectTransform skipRect = _skipButton.GetComponent<RectTransform>();

        skipRect.anchorMin = new Vector2(0.5f, 0f);
        skipRect.anchorMax = new Vector2(0.5f, 0f);
        skipRect.pivot = new Vector2(0.5f, 0f);
        skipRect.anchoredPosition = Vector2.zero;
        skipRect.sizeDelta = new Vector2(SkipWidth, SkipHeight);

        _group.gameObject.SetActive(false);
        _skipButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 결과를 뒷면으로 나눠 깐다. 다 뒤집힌 뒤 카드를 누르면 onOpen(instanceId), 확인을 누르면 onConfirm
    /// </summary>
    public void Show(List<CardDisplayInfo> cards, Action<int> onOpen, Action onConfirm)
    {
        UiAnimator.Cancel(_dealTween);
        _dealTween = null;
        _onConfirm = onConfirm;
        _group.gameObject.SetActive(cards.Count > 0);
        _faceDownCount = cards.Count;
        SetHintVisible(false);
        _skipLabel.text = cards.Count > 0 ? SkipLabel : ConfirmLabel;
        _skipButton.gameObject.SetActive(true);

        if (cards.Count == 0)
            return;

        _shownCards.Clear();

        if (cards.Count == 1)
            LayoutSingle(cards[0], onOpen);
        else
            LayoutMulti(cards, onOpen);
    }

    /// <summary>
    /// 결과를 걷어 낸다 (화면을 떠날 때)
    /// </summary>
    public void Hide()
    {
        UiAnimator.Cancel(_dealTween);
        _dealTween = null;
        _faceDownCount = 0;
        SetHintVisible(false);
        _shownCards.Clear();
        _group.gameObject.SetActive(false);
        _skipButton.gameObject.SetActive(false);
    }

    //1장은 가운데 크게. 아래에서 떠오르며 놓인다
    private void LayoutSingle(CardDisplayInfo info, Action<int> onOpen)
    {
        if (_singleCard == null)
            _singleCard = GachaRevealCard.Create(_group, SingleWidth, SingleSpillReach);

        foreach (GachaRevealCard card in _multiCards)
            card.SetVisible(false);

        float height = CardHeight(SingleWidth);

        SetGroupSize(SingleWidth, height);
        UiBuilder.Place(_singleCard.Root, 0f, 0f, SingleWidth, height);
        _singleCard.SetVisible(true);
        _singleCard.SetCard(info, onOpen, OnCardRevealed);
        _shownCards.Add(_singleCard);

        _singleCard.PlayDeal(new Vector2(0f, -height * SingleDealRise), DealPause, SingleDealSeconds);
        WaitForDeal(DealPause + SingleDealSeconds);
    }

    //4 · 4 · 2 줄. 줄마다 가운데로 모으고, 카드 묶음 한가운데에서 한 장씩 제자리로 날려 보낸다
    private void LayoutMulti(List<CardDisplayInfo> cards, Action<int> onOpen)
    {
        while (_multiCards.Count < MaxCards)
            _multiCards.Add(GachaRevealCard.Create(_group, MultiWidth, MultiSpillReach));

        if (_singleCard != null)
            _singleCard.SetVisible(false);

        float height = CardHeight(MultiWidth);
        float groupWidth = MultiRowCounts[0] * MultiWidth + (MultiRowCounts[0] - 1) * MultiColumnGap;
        float cardsHeight = MultiRowCounts.Length * height + (MultiRowCounts.Length - 1) * MultiRowGap;
        Vector2 dealCenter = new Vector2(groupWidth * 0.5f, cardsHeight * 0.5f);
        int index = 0;

        SetGroupSize(groupWidth, cardsHeight);

        for (int row = 0; row < MultiRowCounts.Length; row++)
        {
            int count = MultiRowCounts[row];
            float rowWidth = count * MultiWidth + (count - 1) * MultiColumnGap;
            float left = (groupWidth - rowWidth) * 0.5f;

            for (int column = 0; column < count; column++, index++)
            {
                GachaRevealCard card = _multiCards[index];
                bool used = index < cards.Count;

                card.SetVisible(used);

                if (!used)
                    continue;

                float x = left + column * (MultiWidth + MultiColumnGap);
                float y = row * (height + MultiRowGap);
                Vector2 slotCenter = new Vector2(x + MultiWidth * 0.5f, y + height * 0.5f);
                Vector2 from = dealCenter - slotCenter;

                UiBuilder.Place(card.Root, x, y, MultiWidth, height);
                card.SetCard(cards[index], onOpen, OnCardRevealed);
                card.PlayDeal(new Vector2(from.x, -from.y), DealPause + index * DealStagger, DealSeconds);
                _shownCards.Add(card);
            }
        }

        WaitForDeal(DealPause + (_shownCards.Count - 1) * DealStagger + DealSeconds);
    }

    //다 놓이면 안내를 띄운다
    private void WaitForDeal(float seconds)
    {
        _dealTween = UiAnimator.Play(seconds, null, () =>
        {
            _dealTween = null;

            if (_hint != null && _faceDownCount > 0)
                SetHintVisible(true);
        }, UiEase.Linear);
    }

    //카드 묶음 크기에 안내 줄을 더해 영역 가운데에 둔다
    private void SetGroupSize(float width, float cardsHeight)
    {
        _group.sizeDelta = new Vector2(width, cardsHeight + HintGap + HintHeight);
        _group.anchoredPosition = Vector2.zero;
        UiBuilder.Place(_hint, 0f, cardsHeight + HintGap, width, HintHeight);
    }

    //뒷면이 남았으면 건너뛰기, 다 뒤집혔으면 확인
    private void OnBottomClicked()
    {
        if (_faceDownCount > 0)
        {
            SkipAll();
            return;
        }

        _onConfirm?.Invoke();
    }

    //나눠 주던 중이면 마저 놓고, 남은 뒷면을 한꺼번에 뒤집는다. 다 넘어가면 버튼이 확인으로 바뀐다
    private void SkipAll()
    {
        UiAnimator.Cancel(_dealTween);
        _dealTween = null;
        SetHintVisible(false);
        _skipButton.gameObject.SetActive(false);

        foreach (GachaRevealCard card in _shownCards)
        {
            card.FinishDeal();
            card.FlipNow();
        }
    }

    private void OnCardRevealed(GachaRevealCard card)
    {
        _faceDownCount = Mathf.Max(0, _faceDownCount - 1);
        _onCardRevealed?.Invoke(card);

        if (_faceDownCount > 0)
            return;

        SetHintVisible(false);
        _skipLabel.text = ConfirmLabel;
        _skipButton.gameObject.SetActive(true);
        UiEffects.Pop(_skipButton.transform, 0.7f, 0.24f);
    }

    private void SetHintVisible(bool visible)
    {
        _hint.gameObject.SetActive(visible);
        UiAnimator.Cancel(_hintTween);
        _hintTween = null;

        if (visible)
        {
            PlayHintCycle();
            return;
        }

        foreach (GachaRevealCard card in _shownCards)
            card.SetHint(0f, 0f);
    }

    //왼쪽에서 나타나 오른쪽으로 흐르며 사라진다. 뒷면이 남아 있고 화면에 보이는 동안만 되풀이한다 (화면이 지워졌으면 멈춘다)
    private void PlayHintCycle()
    {
        _hintTween = UiAnimator.Play(HintCycleSeconds, t =>
        {
            if (_hint == null)
                return;

            float offset = (t - 0.5f) * HintTravel;
            float alpha = t < 0.3f ? t / 0.3f : t > 0.7f ? (1f - t) / 0.3f : 1f;

            foreach (GachaRevealCard card in _shownCards)
                card.SetHint(offset, alpha);
        }, () =>
        {
            _hintTween = null;

            if (_hint != null && _faceDownCount > 0 && _hint.gameObject.activeInHierarchy)
                PlayHintCycle();
        }, UiEase.Linear);
    }

    private static float CardHeight(float width)
    {
        return width * CardView.BaseHeight / CardView.BaseWidth;
    }
}
