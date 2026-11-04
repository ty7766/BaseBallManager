using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 뽑기 결과 카드 1장. 뒷면으로 깔렸다가 누르거나 오른쪽으로 밀면 그쪽으로 넘어가며 뒤집힌다.
/// 뒤집힌 순간 등급·종류에 맞는 번쩍임 · 빛 번짐 · 고리 · 파티클이 터진다
/// </summary>
public class GachaRevealCard : UiWidgetBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const string BackSprite = "Card/card_back";
    private const string GlowSprite = "Fx/glow_soft";
    private const string RingSprite = "Fx/ring";
    //카드 폭의 이 비율만큼 밀면 완전히 뒤집힌다 · 이만큼 넘게 밀고 놓으면 끝까지 넘긴다
    private const float FullDragRatio = 0.9f;
    private const float CommitProgress = 0.35f;
    //넘어가는 동안 미는 쪽으로 쏠리는 거리(카드 폭 비율)와 기울기
    private const float SlideRatio = 0.18f;
    private const float TiltDegrees = 7f;
    private const float FlipSeconds = 0.42f;
    private const float FlashSeconds = 0.3f;
    private const float GlowSeconds = 0.9f;
    private const float RingSeconds = 0.55f;
    //뒷면에도 앞면과 같은 광택 띠가 지나간다
    private const float BackShineStrength = 0.14f;
    //공개 뒤에도 남는 빛 번짐 (희귀할수록 오래 빛나 보인다)
    private const float GlowResidualAlpha = 0.32f;

    /// <summary>
    /// 앞면이 드러났는가
    /// </summary>
    public bool IsFaceUp { get; private set; }

    private RectTransform _pivot;
    private CardView _front;
    private Image _back;
    private Image _glow;
    private Image _ring;
    private Image _flash;
    private float _width;
    private float _progress;
    private float _dragStartX;
    private bool _dragging;
    private bool _flipping;
    private CardDisplayInfo _info;
    private Action<int> _onOpen;
    private Action<GachaRevealCard> _onRevealed;
    private UiTween _flipTween;

    /// <summary>
    /// 카드 1장을 만든다. 폭은 화면에 보일 폭 그대로다
    /// </summary>
    public static GachaRevealCard Create(Transform parent, float width)
    {
        GachaRevealCard card = CreateNode<GachaRevealCard>(parent, "RevealCard");

        card.BuildCard(width);

        return card;
    }

    /// <summary>
    /// 새 결과를 뒷면으로 깐다. 뒤집히면 onRevealed, 그 뒤 앞면을 누르면 onOpen(상세 보기)
    /// </summary>
    public void SetCard(CardDisplayInfo info, Action<int> onOpen, Action<GachaRevealCard> onRevealed)
    {
        UiAnimator.Cancel(_flipTween);
        _flipTween = null;

        _info = info;
        _onOpen = onOpen;
        _onRevealed = onRevealed;
        _dragging = false;
        _flipping = false;
        IsFaceUp = false;

        _front.SetDisplayWidth(_width);
        _front.SetCard(info);
        _front.SetClickHandler(null);

        _glow.gameObject.SetActive(false);
        _ring.gameObject.SetActive(false);
        _flash.gameObject.SetActive(false);

        SetProgress(0f);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (IsFaceUp || _flipping || _dragging)
            return;

        AnimateTo(1f);
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        if (IsFaceUp || _flipping)
            return;

        _dragging = true;
        _dragStartX = ToLocalX(eventData);
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;

        float pushed = ToLocalX(eventData) - _dragStartX;

        SetProgress(Mathf.Clamp01(pushed / (_width * FullDragRatio)));
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
            return;

        _dragging = false;
        AnimateTo(_progress >= CommitProgress ? 1f : 0f);
    }

    //눌러 둔 채 끄는 동안 뒤집힘 정도(0 뒷면 ~ 1 앞면)를 그린다. 반을 넘으면 앞면이 보인다
    private void SetProgress(float progress)
    {
        _progress = progress;

        float angle = progress * Mathf.PI;
        float squash = Mathf.Abs(Mathf.Cos(angle));
        float lean = Mathf.Sin(angle);

        _pivot.localScale = new Vector3(Mathf.Max(0.02f, squash), 1f, 1f);
        _pivot.anchoredPosition = new Vector2(lean * _width * SlideRatio, 0f);
        _pivot.localEulerAngles = new Vector3(0f, 0f, -lean * TiltDegrees);

        bool showFront = progress >= 0.5f;

        _front.gameObject.SetActive(showFront);
        _back.gameObject.SetActive(!showFront);
    }

    //손을 뗀 자리에서 끝까지 넘기거나 원래대로 돌아간다. 남은 거리만큼만 시간을 쓴다
    private void AnimateTo(float target)
    {
        float from = _progress;
        float duration = Mathf.Max(0.08f, FlipSeconds * Mathf.Abs(target - from));

        _flipping = true;
        _flipTween = UiAnimator.Play(duration, t => SetProgress(Mathf.Lerp(from, target, t)), () =>
        {
            _flipTween = null;
            _flipping = false;
            SetProgress(target);

            if (target >= 1f)
                Reveal();
        }, UiEase.OutCubic);
    }

    private void Reveal()
    {
        IsFaceUp = true;

        int instanceId = _info.InstanceId;

        _front.SetClickHandler(() => _onOpen?.Invoke(instanceId));
        PlayRevealEffect();
        _onRevealed?.Invoke(this);
    }

    //모든 카드: 흰 번쩍임 + 파티클. 5성 · 시그 · 골글은 등급색 빛 번짐 · 고리 · 튀어 오름이 더해진다
    private void PlayRevealEffect()
    {
        PlayFlash();

        Color tierColor;
        bool rare = TryGetTierColor(_info, out tierColor);

        UiEffects.Burst(Root, GetBurstKind(_info));

        if (!rare)
            return;

        PlayGlow(tierColor);
        PlayRing(tierColor);
        UiEffects.Pop(_pivot, 0.9f, 0.32f);
    }

    private void PlayFlash()
    {
        _flash.gameObject.SetActive(true);

        UiAnimator.Play(FlashSeconds, t => _flash.color = UiPalette.WithAlpha(Color.white, 0.85f * (1f - t)),
            () => _flash.gameObject.SetActive(false), UiEase.OutCubic);
    }

    //빠르게 차올랐다가 은은하게 남는다
    private void PlayGlow(Color color)
    {
        RectTransform rect = _glow.rectTransform;

        _glow.gameObject.SetActive(true);

        UiAnimator.Play(GlowSeconds, t =>
        {
            float rise = Mathf.Clamp01(t / 0.25f);
            float alpha = t < 0.25f ? rise * 0.95f : Mathf.Lerp(0.95f, GlowResidualAlpha, (t - 0.25f) / 0.75f);
            float scale = 0.8f + 0.3f * rise;

            _glow.color = UiPalette.WithAlpha(color, alpha);
            rect.localScale = new Vector3(scale, scale, 1f);
        }, null, UiEase.Linear);
    }

    private void PlayRing(Color color)
    {
        RectTransform rect = _ring.rectTransform;

        _ring.gameObject.SetActive(true);

        UiAnimator.Play(RingSeconds, t =>
        {
            float scale = 0.5f + 1.4f * t;

            _ring.color = UiPalette.WithAlpha(color, 0.9f * (1f - t));
            rect.localScale = new Vector3(scale, scale, 1f);
        }, () => _ring.gameObject.SetActive(false), UiEase.OutCubic);
    }

    private void BuildCard(float width)
    {
        _width = width;

        float height = width * CardView.BaseHeight / CardView.BaseWidth;

        UiBuilder.Place(Root, 0f, 0f, width, height);

        _glow = CreateCentered(Root, "Glow", GlowSprite, width * 1.9f, height * 1.45f);
        _ring = CreateCentered(Root, "Ring", RingSprite, width * 1.25f, width * 1.25f);

        _pivot = UiBuilder.CreateRect(Root, "Pivot");
        UiBuilder.Stretch(_pivot, 0f, 0f, 0f, 0f);

        _front = CardView.Bake(_pivot, width, false);
        UiBuilder.Stretch(_front.Root, 0f, 0f, 0f, 0f);

        _back = UiBuilder.CreateImage(_pivot, "Back", Color.white, BackSprite);
        UiBuilder.Stretch(_back.rectTransform, 0f, 0f, 0f, 0f);
        _back.raycastTarget = true;

        Image backShine = UiBuilder.CreateImage(_back.transform, "Shine", UiPalette.WithAlpha(Color.white, BackShineStrength));
        UiBuilder.Stretch(backShine.rectTransform, 0f, 0f, 0f, 0f);
        backShine.material = CardMaterials.Shine;

        _flash = UiBuilder.CreateImage(_pivot, "Flash", UiPalette.WithAlpha(Color.white, 0f));
        UiBuilder.Stretch(_flash.rectTransform, 0f, 0f, 0f, 0f);
    }

    //카드 가운데를 중심으로 놓이는 효과 이미지. 처음엔 꺼 둔다
    private static Image CreateCentered(Transform parent, string name, string spritePath, float width, float height)
    {
        Image image = UiBuilder.CreateImage(parent, name, Color.white, spritePath);
        RectTransform rect = image.rectTransform;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
        image.gameObject.SetActive(false);

        return image;
    }

    //화면 좌표를 카드 기준 가로 위치로. 카드 자체(Root)는 움직이지 않아 기준이 흔들리지 않는다
    private float ToLocalX(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, eventData.position,
            eventData.pressEventCamera, out Vector2 local);

        return local.x;
    }

    private static bool TryGetTierColor(CardDisplayInfo info, out Color color)
    {
        color = Color.white;

        if (info.CardType == CardType.Signature)
            color = UiPalette.CardSignature;
        else if (info.CardType == CardType.GoldenGlove)
            color = UiPalette.CardGoldenGlove;
        else if (info.CardGrade == CardGrade.Star5)
            color = UiPalette.CardStar5;
        else
            return false;

        return true;
    }

    private static UiEffectKind GetBurstKind(CardDisplayInfo info)
    {
        return info.CardType switch
        {
            CardType.Signature => UiEffectKind.RevealSignature,
            CardType.GoldenGlove => UiEffectKind.RevealGoldenGlove,
            _ => info.CardGrade == CardGrade.Star5 ? UiEffectKind.RevealRare : UiEffectKind.Reveal
        };
    }
}
