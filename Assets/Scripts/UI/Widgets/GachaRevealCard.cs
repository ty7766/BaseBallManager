using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 뽑기 결과 카드 1장. 뒷면으로 깔렸다가 누르거나 오른쪽으로 밀면 그쪽으로 넘어가며 뒤집힌다.
/// 앞면이 빛난다고 보고, 들리는 왼쪽 틈으로 등급색 빛이 새어 나온다 — 바닥 웅덩이 · 카드 윤곽 빛 · 떠오르는 빛 알갱이.
/// 빛은 모두 가산 합성(덮지 않고 더해짐)이고, 넘길수록 · 등급이 높을수록 세다 (뒤집기 전에 기대를 걸게 한다)
/// </summary>
public class GachaRevealCard : UiWidgetBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const string BackSprite = "Card/card_back";
    private const string SpillSprite = "Fx/card_spill";
    private const string BloomSprite = "Fx/card_bloom";
    private const string RimSprite = "Fx/card_rimlight";
    private const string LightMaterialPath = "UI/Materials/AdditiveLight";
    private const string MaskSprite = "Fx/card_mask";
    private const string ArrowSprite = "Gacha/swipe_arrow";
    //새는 빛 그림의 세로 ÷ 가로(그림 그대로 키워 타원이 막대로 늘지 않게) · 번짐 그림의 카드 둘레 여백(카드 폭 비율). 그림과 짝이 맞아야 한다
    private const float SpillAspect = 2.1f / 0.9f;
    private const float BloomMarginRatio = 0.5f;
    private const float RimMarginRatio = 0.3f;
    //바닥 웅덩이 세기 · 윤곽 빛이 가득 차는 들림 정도 · 빛 알갱이가 새는 틈 길이(카드 높이 비율)
    private const float SpillStrength = 0.85f;
    private const float RimFullLift = 0.6f;
    private const float GapHeightRatio = 0.9f;
    //가장 센 카드(시그 · 골글)가 활짝 들렸을 때 초당 새는 알갱이 · 공개 순간 터지는 알갱이
    private const float MoteMaxRate = 34f;
    private const int MoteBurstMax = 36;
    //밀기 안내 화살표 폭(카드 폭 비율). 그림 비율 330×164 를 따른다
    private const float ArrowWidthRatio = 0.55f;
    private const float ArrowAspect = 164f / 330f;
    //카드 폭의 이 비율만큼 밀면 완전히 뒤집힌다 · 이만큼 넘게 밀고 놓으면 끝까지 넘긴다
    private const float FullDragRatio = 0.9f;
    private const float CommitProgress = 0.35f;
    //넘어가는 동안 미는 쪽으로 쏠리는 거리(카드 폭 비율)와 기울기
    private const float SlideRatio = 0.18f;
    private const float TiltDegrees = 7f;
    //시그 · 골글은 빛이 셀수록 카드가 떨린다 (폭 비율 · 초당 흔들림)
    private const float ShakeRatio = 0.012f;
    private const float ShakeSpeed = 70f;
    private const float FlipSeconds = 0.42f;
    private const float FlashSeconds = 0.32f;
    private const float BloomSeconds = 0.9f;
    //공개 순간 앞면 번쩍임은 등급색을 흰색 쪽으로 이만큼 섞는다
    private const float FlashWhiteness = 0.55f;
    //5성 이상은 공개 뒤에도 번짐이 은은하게 남는다. 번짐은 퍼지며 이만큼 커진다
    private const float RareStrength = 0.8f;
    private const float BloomResidual = 0.4f;
    private const float BloomGrow = 0.14f;
    //뒷면에도 앞면과 같은 광택 띠가 지나간다
    private const float BackShineStrength = 0.14f;
    //나눠 줄 때 처음 크기
    private const float DealStartScale = 0.82f;

    /// <summary>
    /// 앞면이 드러났는가
    /// </summary>
    public bool IsFaceUp { get; private set; }

    private CanvasGroup _fade;
    private RectTransform _body;
    private RectTransform _tilt;
    private RectTransform _pivot;
    private CardView _front;
    private Image _back;
    private Image _spill;
    private Image _rim;
    private Image _bloom;
    private Image _flash;
    private Image _hintArrow;
    private float _width;
    private float _height;
    private float _spillReach;
    private float _progress;
    private float _dragStartX;
    private bool _dragging;
    private bool _flipping;
    private bool _dealing;
    private CardDisplayInfo _info;
    private Color _tierColor;
    private float _tierStrength;
    private bool _tierShakes;
    private Action<int> _onOpen;
    private Action<GachaRevealCard> _onRevealed;
    private UiTween _flipTween;
    private UiTween _bloomTween;
    private UiTween _dealTween;
    private GachaLightMotes _motes;
    private float _emitRate;
    private Vector2 _gapStart;
    private Vector2 _gapEnd;
    private Vector2 _outward;

    private static Material _lightMaterial;

    //틈이 열려 있는 동안 빛 알갱이를 내보내고, 떠 있는 알갱이를 움직인다
    private void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        if (_emitRate > 0f)
            _motes.Emit(deltaTime, _emitRate, _gapStart, _gapEnd, _outward);

        _motes.Tick(deltaTime);
    }

    /// <summary>
    /// 카드 1장을 만든다. 폭은 화면에 보일 폭, spillReach 는 새는 빛이 가장 멀리 닿는 거리다
    /// </summary>
    public static GachaRevealCard Create(Transform parent, float width, float spillReach)
    {
        GachaRevealCard card = CreateNode<GachaRevealCard>(parent, "RevealCard");

        card.BuildCard(width, spillReach);

        return card;
    }

    /// <summary>
    /// 새 결과를 뒷면으로 깐다. 뒤집히면 onRevealed, 그 뒤 앞면을 누르면 onOpen(상세 보기)
    /// </summary>
    public void SetCard(CardDisplayInfo info, Action<int> onOpen, Action<GachaRevealCard> onRevealed)
    {
        UiAnimator.Cancel(_flipTween);
        UiAnimator.Cancel(_bloomTween);
        _flipTween = null;
        _bloomTween = null;
        FinishDeal();

        _info = info;
        _onOpen = onOpen;
        _onRevealed = onRevealed;
        _dragging = false;
        _flipping = false;
        IsFaceUp = false;
        GetTier(info, out _tierColor, out _tierStrength, out _tierShakes);
        _motes.Clear();
        _motes.SetColor(_tierColor);

        _front.SetDisplayWidth(_width);
        _front.SetCard(info);
        _front.SetClickHandler(null);

        _bloom.gameObject.SetActive(false);
        _flash.gameObject.SetActive(false);
        _hintArrow.color = UiPalette.WithAlpha(Color.white, 0f);

        SetProgress(0f);
    }

    /// <summary>
    /// 결과 자리가 카드를 나눠 줄 때 from(제자리 기준 출발점)에서 날아와 내려앉는다. 내려앉기 전에는 누르거나 밀 수 없다
    /// </summary>
    public void PlayDeal(Vector2 from, float delay, float duration)
    {
        UiAnimator.Cancel(_dealTween);
        _dealing = true;
        _fade.alpha = 0f;
        _body.anchoredPosition = from;
        _body.localScale = new Vector3(DealStartScale, DealStartScale, 1f);

        _dealTween = UiAnimator.Play(duration, t =>
        {
            float scale = Mathf.Lerp(DealStartScale, 1f, t);

            _fade.alpha = Mathf.Clamp01(t * 3f);
            _body.anchoredPosition = from * (1f - t);
            _body.localScale = new Vector3(scale, scale, 1f);
        }, FinishDeal, UiEase.OutCubic, delay);
    }

    /// <summary>
    /// 나눠 주기를 건너뛰고 제자리에 놓는다
    /// </summary>
    public void FinishDeal()
    {
        UiAnimator.Cancel(_dealTween);
        _dealTween = null;
        _dealing = false;
        _fade.alpha = 1f;
        _body.anchoredPosition = Vector2.zero;
        _body.localScale = Vector3.one;
    }

    /// <summary>
    /// 지금 자리에서 끝까지 넘긴다 (건너뛰기). 이미 앞면이면 아무것도 하지 않는다
    /// </summary>
    public void FlipNow()
    {
        if (IsFaceUp)
            return;

        UiAnimator.Cancel(_flipTween);
        _dragging = false;
        AnimateTo(1f);
    }

    /// <summary>
    /// 밀기 안내 화살표의 가로 위치(카드 폭 비율)와 투명도. 결과 자리가 모든 뒷면에 같은 박자로 넘긴다.
    /// 밀고 있거나 뒤집히는 중이면 숨긴다
    /// </summary>
    public void SetHint(float offset, float alpha)
    {
        bool idle = !IsFaceUp && !_dragging && !_flipping && !_dealing;

        _hintArrow.rectTransform.anchoredPosition = new Vector2(offset * _width, 0f);
        _hintArrow.color = UiPalette.WithAlpha(Color.white, idle ? alpha : 0f);
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (IsFaceUp || _flipping || _dragging || _dealing)
            return;

        AnimateTo(1f);
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        if (IsFaceUp || _flipping || _dealing)
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

    //뒤집힘 정도(0 뒷면 ~ 1 앞면)를 그린다. 많이 들릴수록 틈으로 새는 빛이 세지고, 반을 넘으면 앞면이 보인다
    private void SetProgress(float progress)
    {
        _progress = progress;

        float angle = progress * Mathf.PI;
        float squash = Mathf.Abs(Mathf.Cos(angle));
        float lift = Mathf.Sin(angle);
        float shake = 0f;

        if (!IsFaceUp && _tierShakes)
            shake = Mathf.Sin(Time.unscaledTime * ShakeSpeed) * _width * ShakeRatio * lift;

        float tiltDegrees = -lift * TiltDegrees;
        Vector2 center = new Vector2(lift * _width * SlideRatio + shake, 0f);

        _pivot.localScale = new Vector3(Mathf.Max(0.02f, squash), 1f, 1f);
        _tilt.anchoredPosition = center;
        _tilt.localEulerAngles = new Vector3(0f, 0f, tiltDegrees);

        SetLeakLight(IsFaceUp ? 0f : lift, squash, center, tiltDegrees);

        bool showFront = progress >= 0.5f;

        _front.gameObject.SetActive(showFront);
        _back.gameObject.SetActive(!showFront);
    }

    //들린 왼쪽 가장자리 밑에서 새는 빛: 바닥 웅덩이 · 카드 윤곽 빛 · 빛 알갱이가 나오는 틈. 들린 만큼, 등급이 높을수록 세다
    private void SetLeakLight(float lift, float squash, Vector2 center, float tiltDegrees)
    {
        float light = lift * _tierStrength;
        bool lit = light > 0.005f;

        _spill.gameObject.SetActive(lit);
        _rim.gameObject.SetActive(lit);
        _emitRate = lit ? MoteMaxRate * light : 0f;

        if (!lit)
            return;

        float reach = _spillReach * (0.55f + 0.45f * _tierStrength) * (0.35f + 0.65f * lift);
        float edgeX = -_width * 0.5f * squash;
        RectTransform spillRect = _spill.rectTransform;

        spillRect.sizeDelta = new Vector2(reach, reach * SpillAspect);
        spillRect.anchoredPosition = new Vector2(edgeX, 0f);
        _spill.color = UiPalette.WithAlpha(_tierColor, light * SpillStrength);
        _rim.color = UiPalette.WithAlpha(_tierColor, _tierStrength * Mathf.SmoothStep(0f, 1f, lift / RimFullLift));

        Quaternion rotation = Quaternion.Euler(0f, 0f, tiltDegrees);
        float gapHalf = _height * 0.5f * GapHeightRatio;

        _gapStart = center + (Vector2)(rotation * new Vector3(edgeX, -gapHalf, 0f));
        _gapEnd = center + (Vector2)(rotation * new Vector3(edgeX, gapHalf, 0f));
        _outward = rotation * Vector3.left;
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

    //앞면이 등급색으로 빛나고 카드에서 빛이 번지며 둘레로 빛 알갱이가 퍼진다. 5성 이상은 번짐이 남고 카드가 튀어 오른다
    private void PlayRevealEffect()
    {
        bool rare = _tierStrength >= RareStrength;

        PlayFlash();
        PlayBloom(rare);
        _motes.Burst(Mathf.RoundToInt(MoteBurstMax * _tierStrength), new Vector2(_width, _height) * 0.5f);

        if (rare)
            UiEffects.Pop(_pivot, 0.9f, 0.32f);
    }

    private void PlayFlash()
    {
        Color flashColor = Color.Lerp(_tierColor, Color.white, FlashWhiteness);

        _flash.gameObject.SetActive(true);

        UiAnimator.Play(FlashSeconds, t => _flash.color = UiPalette.WithAlpha(flashColor, 0.85f * (1f - t)),
            () => _flash.gameObject.SetActive(false), UiEase.OutCubic);
    }

    //카드에서 빛이 번지며 바깥으로 퍼졌다가 잦아든다. 5성 이상은 은은하게 남는다
    private void PlayBloom(bool rare)
    {
        float peak = _tierStrength;
        float rest = rare ? _tierStrength * BloomResidual : 0f;
        RectTransform rect = _bloom.rectTransform;

        _bloom.gameObject.SetActive(true);

        _bloomTween = UiAnimator.Play(BloomSeconds, t =>
        {
            float alpha = t < 0.15f ? peak * t / 0.15f : Mathf.Lerp(peak, rest, (t - 0.15f) / 0.85f);
            float scale = 1f + BloomGrow * Mathf.Sqrt(t);

            _bloom.color = UiPalette.WithAlpha(_tierColor, alpha);
            rect.localScale = new Vector3(scale, scale, 1f);
        }, () =>
        {
            _bloomTween = null;
            _bloom.gameObject.SetActive(rest > 0f);
        }, UiEase.Linear);
    }

    //Root(제자리) > [빛 알갱이, Body(나눠 주기) > Tilt(쏠림 · 기울기) > [바닥 웅덩이, Pivot(눌림) > [번짐, 윤곽 빛, 앞면, 뒷면, 번쩍임]]]
    private void BuildCard(float width, float spillReach)
    {
        _width = width;
        _height = width * CardView.BaseHeight / CardView.BaseWidth;
        _spillReach = spillReach;

        UiBuilder.Place(Root, 0f, 0f, width, _height);
        _fade = gameObject.AddComponent<CanvasGroup>();

        Material lightMaterial = GetLightMaterial();

        _motes = new GachaLightMotes(Root, lightMaterial, width);

        _body = UiBuilder.CreateRect(Root, "Body");
        UiBuilder.Stretch(_body, 0f, 0f, 0f, 0f);

        _tilt = UiBuilder.CreateRect(_body, "Tilt");
        UiBuilder.Stretch(_tilt, 0f, 0f, 0f, 0f);

        _spill = UiBuilder.CreateImage(_tilt, "Spill", Color.white, SpillSprite);
        _spill.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _spill.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _spill.rectTransform.pivot = new Vector2(1f, 0.5f);
        _spill.material = lightMaterial;
        _spill.gameObject.SetActive(false);

        _pivot = UiBuilder.CreateRect(_tilt, "Pivot");
        UiBuilder.Stretch(_pivot, 0f, 0f, 0f, 0f);

        float bloomMargin = width * BloomMarginRatio;

        _bloom = UiBuilder.CreateImage(_pivot, "Bloom", Color.white, BloomSprite);
        UiBuilder.Stretch(_bloom.rectTransform, -bloomMargin, -bloomMargin, -bloomMargin, -bloomMargin);
        _bloom.material = lightMaterial;
        _bloom.gameObject.SetActive(false);

        float rimMargin = width * RimMarginRatio;

        _rim = UiBuilder.CreateImage(_pivot, "RimLight", Color.white, RimSprite);
        UiBuilder.Stretch(_rim.rectTransform, -rimMargin, -rimMargin, -rimMargin, -rimMargin);
        _rim.material = lightMaterial;
        _rim.gameObject.SetActive(false);

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

        _flash = UiBuilder.CreateImage(_pivot, "Flash", UiPalette.WithAlpha(Color.white, 0f), MaskSprite);
        UiBuilder.Stretch(_flash.rectTransform, 0f, 0f, 0f, 0f);
        _flash.material = lightMaterial;
    }

    //화면 좌표를 카드 기준 가로 위치로. 카드 자체(Root)는 움직이지 않아 기준이 흔들리지 않는다
    private float ToLocalX(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(Root, eventData.position,
            eventData.pressEventCamera, out Vector2 local);

        return local.x;
    }

    //등급색과 빛 세기. 희귀할수록 강하게 새어 나오고, 시그 · 골글은 카드가 떨린다
    private static void GetTier(CardDisplayInfo info, out Color color, out float strength, out bool shakes)
    {
        color = CardStyle.GetRarityColor(info.CardType, info.CardGrade);
        shakes = info.CardType == CardType.Signature || info.CardType == CardType.GoldenGlove;
        strength = shakes ? 1f : info.CardGrade switch
        {
            CardGrade.Star5 => 0.8f,
            CardGrade.Star4 => 0.5f,
            _ => 0.32f
        };
    }

    //빛 레이어가 함께 쓰는 가산 합성 머티리얼. 색은 정점색으로만 바꾸므로 복제하지 않는다
    private static Material GetLightMaterial()
    {
        if (_lightMaterial == null)
            _lightMaterial = Resources.Load<Material>(LightMaterialPath);

        return _lightMaterial;
    }
}
