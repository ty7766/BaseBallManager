using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 뽑기 결과 카드 1장. 누르거나 오른쪽으로 밀면 가운데를 축으로 뒤집히며 왼쪽으로 쏠려 앞면이 왼쪽에서 나온다.
/// 앞면이 빛난다고 보고 왼쪽 가장자리에서 등급색 빛(띠 · 윤곽 · UIParticle 알갱이, 가산 합성)이 카드 높이 전체로 샌다.
/// 뒤집히는 순간 카드 뒤에서 등급색 폭발이 터지고, 시그 · 골글은 눈부신 빛살이 돌며 카드 둘레가 반짝인다
/// </summary>
public class GachaRevealCard : UiWidgetBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const string BackSprite = "Card/card_back";
    private const string SpillSprite = "Fx/card_spill";
    private const string BloomSprite = "Fx/card_bloom";
    private const string RimSprite = "Fx/card_rimlight";
    private const string SheenSprite = "Fx/card_sheen";
    private const string LightMaterialPath = "UI/Materials/AdditiveLight";
    private const string MaskSprite = "Fx/card_mask";
    private const string ArrowSprite = "Gacha/swipe_arrow";
    private const string GlintSprite = "Fx/glint";
    private const string BurstTexturePath = "UI/Gacha/fx_reveal_burst";
    private const string GlareTexturePath = "UI/Gacha/fx_reveal_glare";
    private const string FlareMaterialPath = "UI/Materials/AdditiveFlare";
    //새는 빛 그림 높이(카드 높이 배수) · 번짐 · 윤곽 빛 그림의 카드 둘레 여백(카드 폭 비율). 그림과 짝이 맞아야 한다
    private const float SpillHeightRatio = 1.3f;
    private const float BloomMarginRatio = 0.5f;
    private const float RimMarginRatio = 0.3f;
    //새는 빛 세기 · 윤곽 빛이 가득 차는 들림 정도 · 빛 알갱이가 새는 틈 길이(카드 높이 비율)
    private const float SpillStrength = 0.7f;
    private const float RimFullLift = 0.6f;
    private const float GapHeightRatio = 0.95f;
    //들린 쪽이 눈에 가까워져 커지는 정도 · 기울 때 표면 반사광 최대 세기 · 뒤 절반에 앞면이 스스로 빛나는 세기
    private const float PerspectiveRatio = 0.06f;
    private const float SheenStrength = 0.5f;
    private const float FaceGlowStrength = 0.45f;
    //가장 센 카드(시그 · 골글)가 활짝 들렸을 때 초당 새는 알갱이 · 공개 순간 터지는 알갱이
    private const float MoteMaxRate = 34f;
    private const int MoteBurstMax = 36;
    //밀기 안내 화살표 폭(카드 폭 비율). 그림 비율 330×164 를 따른다
    private const float ArrowWidthRatio = 0.55f;
    private const float ArrowAspect = 164f / 330f;
    //카드 폭의 이 비율만큼 밀면 완전히 뒤집힌다 · 이만큼 넘게 밀고 놓으면 끝까지 넘긴다
    private const float FullDragRatio = 0.9f;
    private const float CommitProgress = 0.35f;
    //넘어가는 동안 왼쪽으로 쏠리는 거리(카드 폭 비율)와 기울기. 앞면이 빛이 새는 왼쪽에서 나온다
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
    //뒤집기 폭발 한 장 폭 = 카드 폭 × BurstScale (render_gacha_reveal_fx.py 의 BURST_SCALE 과 같아야 둘레 빛이 카드에 붙는다)
    private const float BurstScale = 2.6f;
    private const float BurstSeconds = 0.55f;
    private const float BurstMinStrength = 0.35f;
    private const int FlipbookGrid = 4;
    private const int FlipbookLastFrame = FlipbookGrid * FlipbookGrid - 1;
    //시그 · 골글 눈부심: 빛살 크기(카드 폭 배수, 1장 뽑기처럼 큰 카드는 상한까지) · 펼쳐지는 · 가득 머무는 · 잦아드는 시간 · 남는 세기 · 초당 도는 각도
    private const float GlareScale = 3.4f;
    private const float GlareMaxSize = 1300f;
    private const float GlareOpenSeconds = 0.25f;
    private const float GlareHoldSeconds = 0.75f;
    private const float GlareSettleSeconds = 0.8f;
    private const float GlareResidual = 0.3f;
    private const float GlareSpinSpeed = 14f;
    //반짝임: 개수 · 한 번 길이 · 크기(카드 폭 비율) · 공개 직후 잇따라 터지는 간격 · 그 뒤 가끔 반짝이는 간격 · 흰색 섞는 정도
    private const int GlintCount = 7;
    private const float GlintSeconds = 0.42f;
    private const float GlintMinSize = 0.3f;
    private const float GlintMaxSize = 0.55f;
    private const float GlintStagger = 0.14f;
    private const float IdleGlintMinGap = 0.9f;
    private const float IdleGlintMaxGap = 1.7f;
    private const float GlintWhiteness = 0.65f;
    //반짝임이 나오는 범위(카드 크기 비율, 가운데 기준 반폭)
    private const float GlintSpread = 0.58f;
    //시그 · 골글 공개 번쩍임은 흰색에 더 가깝고 세다
    private const float SpecialFlashWhiteness = 0.85f;

    /// <summary>
    /// 앞면이 드러났는가
    /// </summary>
    public bool IsFaceUp { get; private set; }

    /// <summary>
    /// 등급 빛 색과 세기(0~1). 무대가 공개에 맞춰 같은 색으로 물든다
    /// </summary>
    public Color LightColor => _tierColor;
    public float LightStrength => _tierStrength;

    /// <summary>
    /// 시그 · 골글처럼 공개가 눈부신 카드인가
    /// </summary>
    public bool IsSpecial => _tierShakes;

    private CanvasGroup _fade;
    private RectTransform _body;
    private RectTransform _tilt;
    private RectTransform _pivot;
    private CardView _front;
    private Image _back;
    private Image _spill;
    private Image _rim;
    private Image _sheen;
    private Image _bloom;
    private Image _flash;
    private Image _hintArrow;
    private RawImage _burst;
    private RawImage _glare;
    private Image[] _glints;
    private float[] _glintStart;
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
    private UiTween _burstTween;
    private bool _specialLit;
    private float _revealTime;
    private float _nextIdleGlint;
    private GachaLightParticles _particles;
    private float _emitRate;
    private Vector2 _gapStart;
    private Vector2 _gapEnd;
    private Vector2 _outward;

    private static Material _lightMaterial;
    private static Material _flareMaterial;

    //틈이 열려 있는 동안 빛 알갱이를 내보낸다 (움직임은 ParticleSystem 이 계산한다). 시그 · 골글은 앞면인 동안 빛살이 돌고 반짝인다
    private void Update()
    {
        if (_emitRate > 0f)
            _particles.Emit(Time.unscaledDeltaTime, _emitRate, _gapStart, _gapEnd, _outward);

        if (_specialLit)
            UpdateSpecialLight(Time.unscaledTime);
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
        UiAnimator.Cancel(_burstTween);
        _flipTween = null;
        _bloomTween = null;
        _burstTween = null;
        FinishDeal();
        StopSpecialLight();
        _burst.gameObject.SetActive(false);

        _info = info;
        _onOpen = onOpen;
        _onRevealed = onRevealed;
        _dragging = false;
        _flipping = false;
        IsFaceUp = false;
        GetTier(info, out _tierColor, out _tierStrength, out _tierShakes);
        _particles.Clear();
        _particles.SetColor(_tierColor);

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

    //뒤집힘 정도(0 뒷면 ~ 1 앞면)를 그린다. 가운데를 축으로 좁아졌다 넓어지고, 반을 넘으면 앞면이 보인다.
    //들릴수록 왼쪽으로 쏠리고 기울며 왼쪽 가장자리에서 빛이 샌다
    private void SetProgress(float progress)
    {
        _progress = progress;

        float angle = progress * Mathf.PI;
        float squash = Mathf.Abs(Mathf.Cos(angle));
        float lift = Mathf.Sin(angle);
        bool showFront = progress >= 0.5f;
        float shake = 0f;

        if (!IsFaceUp && _tierShakes)
            shake = Mathf.Sin(Time.unscaledTime * ShakeSpeed) * _width * ShakeRatio * lift;

        float tiltDegrees = lift * TiltDegrees;
        Vector2 center = new Vector2(-lift * _width * SlideRatio + shake, 0f);

        _pivot.localScale = new Vector3(Mathf.Max(0.02f, squash), 1f + PerspectiveRatio * lift, 1f);
        _tilt.anchoredPosition = center;
        _tilt.localEulerAngles = new Vector3(0f, 0f, tiltDegrees);

        _front.gameObject.SetActive(showFront);
        _back.gameObject.SetActive(!showFront);

        SetLeakLight(IsFaceUp ? 0f : lift, squash, center, tiltDegrees);
        SetSheen(squash);
        SetFaceGlow(showFront && !IsFaceUp ? lift : 0f);
    }

    //왼쪽 가장자리에서 바깥으로 새는 빛: 카드 높이 전체 띠 · 카드 윤곽 빛 · 빛 알갱이가 나오는 틈. 가장자리에서 멀어질수록 옅어진다
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
        float height = _height * (1f + PerspectiveRatio * lift);
        float edgeX = -_width * 0.5f * squash;
        RectTransform spillRect = _spill.rectTransform;

        spillRect.sizeDelta = new Vector2(reach, height * SpillHeightRatio);
        spillRect.anchoredPosition = new Vector2(edgeX, 0f);
        _spill.color = UiPalette.WithAlpha(_tierColor, light * SpillStrength);
        _rim.color = UiPalette.WithAlpha(_tierColor, _tierStrength * Mathf.SmoothStep(0f, 1f, lift / RimFullLift));

        Quaternion rotation = Quaternion.Euler(0f, 0f, tiltDegrees);
        float gapHalf = height * 0.5f * GapHeightRatio;

        _gapStart = center + (Vector2)(rotation * new Vector3(edgeX, -gapHalf, 0f));
        _gapEnd = center + (Vector2)(rotation * new Vector3(edgeX, gapHalf, 0f));
        _outward = rotation * Vector3.left;
    }

    //기울수록(눌린 폭이 좁을수록) 표면이 무대 조명을 세게 비춘다. 빛이 새는 왼쪽이 밝다
    private void SetSheen(float squash)
    {
        float tilt = 1f - squash;
        bool shown = tilt > 0.02f;

        _sheen.gameObject.SetActive(shown);

        if (shown)
            _sheen.color = UiPalette.WithAlpha(Color.white, SheenStrength * Mathf.Pow(tilt, 1.2f));
    }

    //앞면이 펼쳐지는 동안 빛나는 앞면이 직접 보인다. 공개 순간에는 번쩍임이 이어받는다
    private void SetFaceGlow(float lift)
    {
        bool shown = lift > 0.005f;

        _flash.gameObject.SetActive(shown);

        if (shown)
            _flash.color = UiPalette.WithAlpha(Color.Lerp(_tierColor, Color.white, FlashWhiteness),
                _tierStrength * FaceGlowStrength * lift);
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

    //앞면이 등급색으로 빛나고 카드 뒤에서 폭발이 터지며 둘레로 빛 알갱이가 퍼진다. 5성 이상은 번짐이 남고 카드가 튀어 오른다.
    //시그 · 골글은 눈부신 빛살과 반짝임이 더해진다
    private void PlayRevealEffect()
    {
        bool rare = _tierStrength >= RareStrength;

        PlayFlash();
        PlayBloom(rare);
        PlayBurst();
        _particles.Burst(Mathf.RoundToInt(MoteBurstMax * _tierStrength), new Vector2(_width, _height) * 0.5f);

        if (_tierShakes)
            StartSpecialLight();

        if (rare)
            UiEffects.Pop(_pivot, 0.9f, 0.32f);
    }

    private void PlayFlash()
    {
        float whiteness = _tierShakes ? SpecialFlashWhiteness : FlashWhiteness;
        float peak = _tierShakes ? 1f : 0.85f;
        Color flashColor = Color.Lerp(_tierColor, Color.white, whiteness);

        _flash.gameObject.SetActive(true);

        UiAnimator.Play(FlashSeconds, t => _flash.color = UiPalette.WithAlpha(flashColor, peak * (1f - t)),
            () => _flash.gameObject.SetActive(false), UiEase.OutCubic);
    }

    //카드 뒤에서 둘레가 번쩍이고 가시 · 파편이 곧게 뻗는 플립북을 한 번 튼다. 등급이 높을수록 진하다
    private void PlayBurst()
    {
        float strength = Mathf.Lerp(BurstMinStrength, 1f, _tierStrength);

        UiAnimator.Cancel(_burstTween);
        _burst.color = UiPalette.WithAlpha(_tierColor, strength);
        UiFlipbook.SetFrame(_burst, FlipbookGrid, FlipbookGrid, 0);
        _burst.gameObject.SetActive(true);

        _burstTween = UiAnimator.Play(BurstSeconds,
            t => UiFlipbook.SetFrame(_burst, FlipbookGrid, FlipbookGrid, Mathf.Min(FlipbookLastFrame, (int)(t * FlipbookLastFrame))),
            () =>
            {
                _burstTween = null;
                _burst.gameObject.SetActive(false);
            }, UiEase.Linear);
    }

    //빛살이 펼쳐지고 반짝임이 잇따라 터진다. 빛살이 이웃 카드 위로도 번지도록 이 카드를 맨 앞으로 올린다
    private void StartSpecialLight()
    {
        float now = Time.unscaledTime;

        _specialLit = true;
        _revealTime = now;
        _glare.gameObject.SetActive(true);
        Root.SetAsLastSibling();

        for (int i = 0; i < GlintCount; i++)
            SpawnGlint(now + i * GlintStagger + UnityEngine.Random.Range(0f, GlintStagger * 0.5f));

        _nextIdleGlint = now + GlareOpenSeconds + GlareHoldSeconds + GlareSettleSeconds;
        UpdateSpecialLight(now);
    }

    //빛살 · 반짝임을 걷는다 (카드를 새로 깔 때)
    private void StopSpecialLight()
    {
        _specialLit = false;
        _glare.gameObject.SetActive(false);

        for (int i = 0; i < _glints.Length; i++)
        {
            _glintStart[i] = -1f;
            _glints[i].gameObject.SetActive(false);
        }
    }

    //빛살은 펼쳐져 잠깐 가득 머물다 은은하게 잦아들어 계속 돈다. 그 사이 반짝임이 가끔 하나씩 터진다
    private void UpdateSpecialLight(float now)
    {
        float elapsed = now - _revealTime;
        float open = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / GlareOpenSeconds), 3f);
        float settle = Mathf.Clamp01((elapsed - GlareOpenSeconds - GlareHoldSeconds) / GlareSettleSeconds);
        float twinkle = 0.88f + 0.12f * Mathf.Sin(elapsed * 5f);
        float scale = Mathf.Lerp(0.3f, 1f, open) * Mathf.Lerp(1f, 0.85f, settle);
        RectTransform glare = _glare.rectTransform;

        glare.localScale = new Vector3(scale, scale, 1f);
        glare.localEulerAngles = new Vector3(0f, 0f, -elapsed * GlareSpinSpeed);
        _glare.color = UiPalette.WithAlpha(_tierColor, open * Mathf.Lerp(1f, GlareResidual * twinkle, settle));

        UpdateGlints(now);

        if (now < _nextIdleGlint)
            return;

        SpawnGlint(now);
        _nextIdleGlint = now + UnityEngine.Random.Range(IdleGlintMinGap, IdleGlintMaxGap);
    }

    //반짝임 하나하나가 커졌다 작아지며 살짝 돈다. 시작 전이면 숨겨 두고, 끝나면 자리를 비운다
    private void UpdateGlints(float now)
    {
        for (int i = 0; i < _glints.Length; i++)
        {
            float start = _glintStart[i];

            if (start < 0f)
                continue;

            Image glint = _glints[i];
            float u = (now - start) / GlintSeconds;

            if (u >= 1f)
            {
                _glintStart[i] = -1f;
                glint.gameObject.SetActive(false);
                continue;
            }

            bool shown = u >= 0f;

            if (glint.gameObject.activeSelf != shown)
                glint.gameObject.SetActive(shown);

            if (!shown)
                continue;

            float size = Mathf.Pow(Mathf.Sin(u * Mathf.PI), 1.5f);

            glint.rectTransform.localScale = new Vector3(size, size, 1f);
            glint.rectTransform.localEulerAngles = new Vector3(0f, 0f, u * 45f);
        }
    }

    //빈 반짝임 자리에 카드 둘레 아무 데나 하나를 startTime 에 터지도록 걸어 둔다. 빈자리가 없으면 건너뛴다
    private void SpawnGlint(float startTime)
    {
        for (int i = 0; i < _glints.Length; i++)
        {
            if (_glintStart[i] >= 0f)
                continue;

            Image glint = _glints[i];
            float size = _width * UnityEngine.Random.Range(GlintMinSize, GlintMaxSize);

            glint.rectTransform.anchoredPosition = new Vector2(UnityEngine.Random.Range(-GlintSpread, GlintSpread) * _width,
                UnityEngine.Random.Range(-GlintSpread, GlintSpread) * _height);
            glint.rectTransform.sizeDelta = new Vector2(size, size);
            glint.color = Color.Lerp(_tierColor, Color.white, GlintWhiteness);
            glint.gameObject.SetActive(false);
            _glintStart[i] = startTime;

            return;
        }
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

    //Root(제자리) > [빛 알갱이, 카드 뒤 빛(폭발 · 빛살), Body(나눠 주기) > Tilt(쏠림 · 기울기 · 떨림) > [새는 빛,
    //Pivot(눌림) > [번짐, 윤곽 빛, 앞면, 뒷면, 반사광, 번쩍임]], 반짝임]
    private void BuildCard(float width, float spillReach)
    {
        _width = width;
        _height = width * CardView.BaseHeight / CardView.BaseWidth;
        _spillReach = spillReach;

        UiBuilder.Place(Root, 0f, 0f, width, _height);
        _fade = gameObject.AddComponent<CanvasGroup>();

        Material lightMaterial = GetLightMaterial();

        _particles = new GachaLightParticles(Root, width);

        RectTransform behind = UiBuilder.CreateRect(Root, "BehindLight");
        UiBuilder.Stretch(behind, 0f, 0f, 0f, 0f);
        _glare = CreateCenteredFlare(behind, "Glare", GlareTexturePath, Mathf.Min(width * GlareScale, GlareMaxSize));
        _burst = CreateCenteredFlare(behind, "Burst", BurstTexturePath, width * BurstScale);

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

        _sheen = UiBuilder.CreateImage(_pivot, "Sheen", UiPalette.WithAlpha(Color.white, 0f), SheenSprite);
        UiBuilder.Stretch(_sheen.rectTransform, 0f, 0f, 0f, 0f);
        _sheen.material = lightMaterial;
        _sheen.gameObject.SetActive(false);

        _flash = UiBuilder.CreateImage(_pivot, "Flash", UiPalette.WithAlpha(Color.white, 0f), MaskSprite);
        UiBuilder.Stretch(_flash.rectTransform, 0f, 0f, 0f, 0f);
        _flash.material = lightMaterial;

        BuildGlints(lightMaterial);
    }

    //반짝임은 카드 앞(맨 위)에 두고 가운데 기준으로 흩뿌린다
    private void BuildGlints(Material lightMaterial)
    {
        RectTransform layer = UiBuilder.CreateRect(Root, "Glints");
        UiBuilder.Stretch(layer, 0f, 0f, 0f, 0f);

        _glints = new Image[GlintCount];
        _glintStart = new float[GlintCount];

        for (int i = 0; i < GlintCount; i++)
        {
            Image glint = UiBuilder.CreateImage(layer, "Glint", Color.white, GlintSprite);

            glint.rectTransform.anchorMin = glint.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            glint.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            glint.material = lightMaterial;
            glint.gameObject.SetActive(false);
            _glints[i] = glint;
            _glintStart[i] = -1f;
        }
    }

    //카드 가운데에 놓는 가산 합성 플립북 · 빛살 한 장 (R = 흰 심, A = 등급색 번짐)
    private static RawImage CreateCenteredFlare(RectTransform parent, string name, string texturePath, float size)
    {
        RectTransform rect = UiBuilder.CreateRect(parent, name);
        RawImage image = rect.gameObject.AddComponent<RawImage>();

        if (_flareMaterial == null)
            _flareMaterial = Resources.Load<Material>(FlareMaterialPath);

        image.texture = Resources.Load<Texture2D>(texturePath);
        image.material = _flareMaterial;
        image.raycastTarget = false;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);
        rect.gameObject.SetActive(false);

        return image;
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
