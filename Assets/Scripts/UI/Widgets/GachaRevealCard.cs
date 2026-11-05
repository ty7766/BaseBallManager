using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 뽑기 결과 카드 1장. 뒷면으로 깔렸다가 누르거나 오른쪽으로 밀면 그쪽으로 넘어가며 뒤집힌다.
/// 넘기는 만큼 카드 등급색 빛이 차올라(3성 파랑 · 4성 초록 · 5성 보라 · 시그 분홍 · 골글 금색) 뒤집기 전에 기대를 걸게 한다
/// </summary>
public class GachaRevealCard : UiWidgetBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const string BackSprite = "Card/card_back";
    private const string GlowSprite = "Fx/card_glow";
    private const string RimSprite = "Fx/card_rim";
    private const string ArrowSprite = "Gacha/swipe_arrow";
    //빛 그림의 9-슬라이스 테두리 · 테두리 빛 그림에서 카드 모서리가 놓이는 자리 (픽셀)
    private const float FxBorderPixels = 96f;
    private const float RimEdgePixels = 64f;
    //카드 뒤 빛이 번지는 폭 · 테두리 빛이 카드 밖으로 나가는 폭 (카드 폭 비율)
    private const float GlowSpreadRatio = 0.28f;
    private const float RimMarginRatio = 0.06f;
    //밀기 안내 화살표 폭(카드 폭 비율). 그림 비율 330×140 을 따른다
    private const float ArrowWidthRatio = 0.55f;
    private const float ArrowAspect = 140f / 330f;
    //카드 폭의 이 비율만큼 밀면 완전히 뒤집힌다 · 이만큼 넘게 밀고 놓으면 끝까지 넘긴다
    private const float FullDragRatio = 0.9f;
    private const float CommitProgress = 0.35f;
    //빛이 차오르기 시작하는 · 가득 차는 뒤집힘 정도 (반을 넘기 전에 다 차올라 뒷면일 때 미리 보인다)
    private const float AnticipationStart = 0.02f;
    private const float AnticipationFull = 0.45f;
    //넘어가는 동안 미는 쪽으로 쏠리는 거리(카드 폭 비율)와 기울기
    private const float SlideRatio = 0.18f;
    private const float TiltDegrees = 7f;
    //시그 · 골글은 빛이 찰수록 카드가 떨린다 (폭 비율 · 초당 흔들림)
    private const float ShakeRatio = 0.012f;
    private const float ShakeSpeed = 70f;
    private const float FlipSeconds = 0.42f;
    private const float FlashSeconds = 0.3f;
    private const float AfterglowSeconds = 0.9f;
    private const float OutlineSeconds = 0.55f;
    //5성 이상은 공개 순간 한 번 더 타오르고 은은하게 남는다
    private const float RareStrength = 0.8f;
    private const float RevealPeak = 1.25f;
    private const float RareResidual = 0.45f;
    //뒷면에도 앞면과 같은 광택 띠가 지나간다
    private const float BackShineStrength = 0.14f;

    /// <summary>
    /// 앞면이 드러났는가
    /// </summary>
    public bool IsFaceUp { get; private set; }

    private RectTransform _pivot;
    private CardView _front;
    private Image _back;
    private Image _glow;
    private Image _rim;
    private Image _outline;
    private Image _flash;
    private Image _hintArrow;
    private float _width;
    private float _progress;
    private float _dragStartX;
    private bool _dragging;
    private bool _flipping;
    private CardDisplayInfo _info;
    private Color _tierColor;
    private float _tierStrength;
    private bool _tierShakes;
    private Action<int> _onOpen;
    private Action<GachaRevealCard> _onRevealed;
    private UiTween _flipTween;
    private UiTween _afterglowTween;

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
        UiAnimator.Cancel(_afterglowTween);
        _flipTween = null;
        _afterglowTween = null;

        _info = info;
        _onOpen = onOpen;
        _onRevealed = onRevealed;
        _dragging = false;
        _flipping = false;
        IsFaceUp = false;
        GetTier(info, out _tierColor, out _tierStrength, out _tierShakes);

        _front.SetDisplayWidth(_width);
        _front.SetCard(info);
        _front.SetClickHandler(null);

        _outline.gameObject.SetActive(false);
        _flash.gameObject.SetActive(false);
        _hintArrow.color = UiPalette.WithAlpha(Color.white, 0f);

        SetProgress(0f);
    }

    /// <summary>
    /// 밀기 안내 화살표의 가로 위치(카드 폭 비율)와 투명도. 결과 자리가 모든 뒷면에 같은 박자로 넘긴다.
    /// 밀고 있거나 뒤집히는 중이면 숨긴다
    /// </summary>
    public void SetHint(float offset, float alpha)
    {
        bool idle = !IsFaceUp && !_dragging && !_flipping;

        _hintArrow.rectTransform.anchoredPosition = new Vector2(offset * _width, 0f);
        _hintArrow.color = UiPalette.WithAlpha(Color.white, idle ? alpha : 0f);
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
        _hintArrow.color = UiPalette.WithAlpha(Color.white, 0f);
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

    //뒤집힘 정도(0 뒷면 ~ 1 앞면)를 그린다. 반을 넘으면 앞면이 보이고, 그 전에 등급색 빛이 먼저 차오른다
    private void SetProgress(float progress)
    {
        _progress = progress;

        float angle = progress * Mathf.PI;
        float squash = Mathf.Abs(Mathf.Cos(angle));
        float lean = Mathf.Sin(angle);
        float shake = 0f;

        if (!IsFaceUp)
        {
            float level = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(AnticipationStart, AnticipationFull, progress));

            SetGlow(level * _tierStrength, level * _tierStrength, level);

            if (_tierShakes)
                shake = Mathf.Sin(Time.unscaledTime * ShakeSpeed) * _width * ShakeRatio * level;
        }

        _pivot.localScale = new Vector3(Mathf.Max(0.02f, squash), 1f, 1f);
        _pivot.anchoredPosition = new Vector2(lean * _width * SlideRatio + shake, 0f);
        _pivot.localEulerAngles = new Vector3(0f, 0f, -lean * TiltDegrees);

        bool showFront = progress >= 0.5f;

        _front.gameObject.SetActive(showFront);
        _back.gameObject.SetActive(!showFront);
    }

    //카드 뒤 빛 번짐과 테두리 빛. size 가 클수록 뒤 빛이 바깥으로 넓게 번진다 (0 이면 끈다)
    private void SetGlow(float glowAlpha, float rimAlpha, float size)
    {
        bool lit = glowAlpha > 0.005f || rimAlpha > 0.005f;

        _glow.gameObject.SetActive(lit);
        _rim.gameObject.SetActive(lit);

        if (!lit)
            return;

        float spread = _width * GlowSpreadRatio * (0.5f + 0.5f * size);

        UiBuilder.Stretch(_glow.rectTransform, -spread, -spread, -spread, -spread);
        _glow.pixelsPerUnitMultiplier = FxBorderPixels / spread;
        _glow.color = UiPalette.WithAlpha(_tierColor, Mathf.Clamp01(glowAlpha * 0.9f));
        _rim.color = UiPalette.WithAlpha(_tierColor, Mathf.Clamp01(rimAlpha));
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

    //모든 카드: 흰 번쩍임 · 등급색 외곽선이 퍼짐 · 파티클. 5성 이상은 빛이 한 번 더 타오르고 남으며 카드가 튀어 오른다
    private void PlayRevealEffect()
    {
        bool rare = _tierStrength >= RareStrength;

        PlayFlash();
        PlayOutline();
        PlayAfterglow(rare);
        UiEffects.Burst(Root, GetBurstKind(_info));

        if (rare)
            UiEffects.Pop(_pivot, 0.9f, 0.32f);
    }

    private void PlayFlash()
    {
        _flash.gameObject.SetActive(true);

        UiAnimator.Play(FlashSeconds, t => _flash.color = UiPalette.WithAlpha(Color.white, 0.85f * (1f - t)),
            () => _flash.gameObject.SetActive(false), UiEase.OutCubic);
    }

    //카드 모양 외곽선이 바깥으로 퍼지며 사라진다 (원형 고리 대신)
    private void PlayOutline()
    {
        RectTransform rect = _outline.rectTransform;

        _outline.gameObject.SetActive(true);

        UiAnimator.Play(OutlineSeconds, t =>
        {
            float scale = 1f + 0.18f * t;

            _outline.color = UiPalette.WithAlpha(_tierColor, 0.9f * (1f - t));
            rect.localScale = new Vector3(scale, scale, 1f);
        }, () => _outline.gameObject.SetActive(false), UiEase.OutCubic);
    }

    //차오른 빛이 공개 순간 한 번 더 타올랐다가, 5성 이상은 은은하게 남고 나머지는 꺼진다. 테두리 빛은 걷힌다
    private void PlayAfterglow(bool rare)
    {
        float start = _tierStrength;
        float peak = rare ? start * RevealPeak : start;
        float rest = rare ? start * RareResidual : 0f;

        _afterglowTween = UiAnimator.Play(AfterglowSeconds, t =>
        {
            float glow = t < 0.2f ? Mathf.Lerp(start, peak, t / 0.2f) : Mathf.Lerp(peak, rest, (t - 0.2f) / 0.8f);

            SetGlow(glow, start * (1f - t), rare ? 1f + 0.2f * (1f - t) : 1f - t);
        }, () => _afterglowTween = null, UiEase.Linear);
    }

    private void BuildCard(float width)
    {
        _width = width;

        float height = width * CardView.BaseHeight / CardView.BaseWidth;
        float rimMargin = width * RimMarginRatio;

        UiBuilder.Place(Root, 0f, 0f, width, height);

        _glow = CreateSliced(Root, "Glow", GlowSprite, 0f);

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

        float arrowWidth = width * ArrowWidthRatio;

        _hintArrow = UiBuilder.CreateImage(_back.transform, "SwipeArrow", Color.white, ArrowSprite);
        _hintArrow.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _hintArrow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _hintArrow.rectTransform.sizeDelta = new Vector2(arrowWidth, arrowWidth * ArrowAspect);

        _rim = CreateSliced(_pivot, "Rim", RimSprite, rimMargin);

        _flash = UiBuilder.CreateImage(_pivot, "Flash", UiPalette.WithAlpha(Color.white, 0f));
        UiBuilder.Stretch(_flash.rectTransform, 0f, 0f, 0f, 0f);

        _outline = CreateSliced(Root, "Outline", RimSprite, rimMargin);
    }

    //카드보다 사방 margin 만큼 큰 9-슬라이스 빛. 테두리 그림이 카드 크기에 맞게 줄도록 픽셀 배율을 맞춘다
    private Image CreateSliced(Transform parent, string name, string spritePath, float margin)
    {
        Image image = UiBuilder.CreateImage(parent, name, Color.white, spritePath, Image.Type.Sliced);

        UiBuilder.Stretch(image.rectTransform, -margin, -margin, -margin, -margin);

        if (margin > 0f)
            image.pixelsPerUnitMultiplier = RimEdgePixels / margin;

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

    //등급색과 빛 세기. 희귀할수록 강하게 차오르고, 시그 · 골글은 카드가 떨린다
    private static void GetTier(CardDisplayInfo info, out Color color, out float strength, out bool shakes)
    {
        shakes = info.CardType == CardType.Signature || info.CardType == CardType.GoldenGlove;

        if (info.CardType == CardType.Signature)
        {
            color = UiPalette.CardSignature;
            strength = 1f;
            return;
        }

        if (info.CardType == CardType.GoldenGlove)
        {
            color = UiPalette.CardGoldenGlove;
            strength = 1f;
            return;
        }

        color = CardStyle.GetStarColor(info.CardGrade);
        strength = info.CardGrade switch
        {
            CardGrade.Star5 => 0.8f,
            CardGrade.Star4 => 0.5f,
            _ => 0.32f
        };
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
