using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 뽑기 결과 자리. 1장은 가운데 크게, 10장은 4 · 4 · 2 줄로 뒷면을 깔고 카드 한가운데에 밀기 화살표를 띄운다.
/// 카드는 처음 쓸 때 한 번 만들어 두고 돌려쓴다 (1장용 1 + 10장용 10)
/// </summary>
public sealed class GachaRevealView
{
    private const int MaxCards = 10;
    private const float SingleWidth = 540f;
    //10장은 열 사이를 넓게 벌려 카드를 옆으로 밀어도 이웃 카드에 닿지 않게 한다
    private const float MultiWidth = 196f;
    private const float MultiColumnGap = 64f;
    private const float MultiRowGap = 44f;
    private const float HintGap = 30f;
    private const float HintHeight = 44f;
    //화살표가 왼쪽에서 나타나 오른쪽으로 흐르며 사라지는 한 번의 길이와 이동 거리(카드 폭 비율)
    private const float HintCycleSeconds = 1.6f;
    private const float HintTravel = 0.22f;

    private static readonly int[] MultiRowCounts = { 4, 4, 2 };

    private readonly RectTransform _group;
    private readonly RectTransform _hint;
    private readonly List<GachaRevealCard> _multiCards = new List<GachaRevealCard>(MaxCards);
    private readonly List<GachaRevealCard> _shownCards = new List<GachaRevealCard>(MaxCards);

    private GachaRevealCard _singleCard;
    private int _faceDownCount;
    private UiTween _hintTween;

    public GachaRevealView(RectTransform area)
    {
        _group = UiBuilder.CreateRect(area, "Reveal");
        _group.anchorMin = new Vector2(0.5f, 0.5f);
        _group.anchorMax = new Vector2(0.5f, 0.5f);
        _group.pivot = new Vector2(0.5f, 0.5f);

        _hint = UiBuilder.CreateRect(_group, "SwipeHint");

        TextMeshProUGUI label = UiBuilder.CreateText(_hint, "Label", 30f, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.Stretch(label.rectTransform, 0f, 0f, 0f, 0f);
        label.text = "카드를 누르거나 오른쪽으로 밀어서 뒤집기";

        _group.gameObject.SetActive(false);
    }

    /// <summary>
    /// 결과를 뒷면으로 깐다. 다 뒤집힌 뒤 카드를 누르면 onOpen(instanceId)
    /// </summary>
    public void Show(List<CardDisplayInfo> cards, Action<int> onOpen)
    {
        _group.gameObject.SetActive(cards.Count > 0);
        _faceDownCount = cards.Count;

        if (cards.Count == 0)
        {
            SetHintVisible(false);
            return;
        }

        _shownCards.Clear();

        if (cards.Count == 1)
            LayoutSingle(cards[0], onOpen);
        else
            LayoutMulti(cards, onOpen);

        SetHintVisible(true);
    }

    /// <summary>
    /// 화면에 다시 들어왔을 때 남은 뒷면이 있으면 안내 화살표를 다시 돌린다
    /// </summary>
    public void ResumeHint()
    {
        if (_faceDownCount > 0 && _hintTween == null && _group.gameObject.activeInHierarchy)
            PlayHintCycle();
    }

    private void LayoutSingle(CardDisplayInfo info, Action<int> onOpen)
    {
        if (_singleCard == null)
            _singleCard = GachaRevealCard.Create(_group, SingleWidth);

        foreach (GachaRevealCard card in _multiCards)
            card.SetVisible(false);

        float height = CardHeight(SingleWidth);

        SetGroupSize(SingleWidth, height);
        UiBuilder.Place(_singleCard.Root, 0f, 0f, SingleWidth, height);
        _singleCard.SetVisible(true);
        _singleCard.SetCard(info, onOpen, OnCardRevealed);
        _shownCards.Add(_singleCard);
    }

    //4 · 4 · 2 줄. 줄마다 가운데로 모은다
    private void LayoutMulti(List<CardDisplayInfo> cards, Action<int> onOpen)
    {
        while (_multiCards.Count < MaxCards)
            _multiCards.Add(GachaRevealCard.Create(_group, MultiWidth));

        if (_singleCard != null)
            _singleCard.SetVisible(false);

        float height = CardHeight(MultiWidth);
        float groupWidth = MultiRowCounts[0] * MultiWidth + (MultiRowCounts[0] - 1) * MultiColumnGap;
        float cardsHeight = MultiRowCounts.Length * height + (MultiRowCounts.Length - 1) * MultiRowGap;
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

                UiBuilder.Place(card.Root, left + column * (MultiWidth + MultiColumnGap),
                    row * (height + MultiRowGap), MultiWidth, height);
                card.SetCard(cards[index], onOpen, OnCardRevealed);
                _shownCards.Add(card);
            }
        }
    }

    //카드 묶음 크기에 안내 줄을 더해 영역 가운데에 둔다
    private void SetGroupSize(float width, float cardsHeight)
    {
        _group.sizeDelta = new Vector2(width, cardsHeight + HintGap + HintHeight);
        _group.anchoredPosition = Vector2.zero;
        UiBuilder.Place(_hint, 0f, cardsHeight + HintGap, width, HintHeight);
    }

    private void OnCardRevealed(GachaRevealCard card)
    {
        _faceDownCount = Mathf.Max(0, _faceDownCount - 1);

        if (_faceDownCount == 0)
            SetHintVisible(false);
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

    //왼쪽에서 나타나 오른쪽으로 흐르며 사라진다. 뒷면이 남아 있고 화면에 보이는 동안만 되풀이한다
    private void PlayHintCycle()
    {
        _hintTween = UiAnimator.Play(HintCycleSeconds, t =>
        {
            float offset = (t - 0.5f) * HintTravel;
            float alpha = t < 0.3f ? t / 0.3f : t > 0.7f ? (1f - t) / 0.3f : 1f;

            foreach (GachaRevealCard card in _shownCards)
                card.SetHint(offset, alpha);
        }, () =>
        {
            _hintTween = null;

            if (_faceDownCount > 0 && _hint.gameObject.activeInHierarchy)
                PlayHintCycle();
        }, UiEase.Linear);
    }

    private static float CardHeight(float width)
    {
        return width * CardView.BaseHeight / CardView.BaseWidth;
    }
}
