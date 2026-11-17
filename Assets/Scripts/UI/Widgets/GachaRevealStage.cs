using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 뽑기 연출 화면. 뽑기 로비 위를 통째로 덮는 차분한 남색 무대에서 빛이 가운데로 모였다 터지며 카드를 나눠 준다.
/// 바탕은 가만히 두고, 카드가 뒤집힐 때마다 카드 뒤 빛 안개가 그 카드 등급 색으로 차올랐다 가라앉는다. 다 뒤집기 전에는 닫히지 않는다
/// </summary>
public sealed class GachaRevealStage
{
    private const string BackgroundPath = "UI/Gacha/gacha_reveal_bg";
    private const string HazePath = "UI/Gacha/gacha_stage_haze";
    private const string SummonPath = "UI/Gacha/fx_gacha_summon";
    private const string AdditiveMaterialPath = "UI/Materials/AdditiveLight";
    private const string FlareMaterialPath = "UI/Materials/AdditiveFlare";
    private const float ArtWidth = 1080f;
    private const float ArtHeight = 2340f;
    private const float TitleTop = 120f;
    private const float ResultTop = 220f;
    private const float ResultBottom = 64f;
    //빛 안개: 크기 · 평소 세기 · 공개 때 더해지는 세기(등급 세기 곱) · 차오르는 비율 · 한 번 출렁이는 길이 · 처음 깔리는 시간
    private const float HazeSize = 1500f;
    private const float HazeRestAlpha = 0.1f;
    private const float HazePulseAlpha = 0.4f;
    private const float HazeRiseRatio = 0.15f;
    private const float HazePulseSeconds = 1.1f;
    private const float HazeInSeconds = 0.6f;
    //소환: 빛이 모였다 터지는 플립북 크기 · 길이 (절반에서 터진다 — GachaRevealView.DealPause 와 짝)
    private const float SummonSize = 1100f;
    private const float SummonSeconds = 0.85f;
    private const int FlipbookGrid = 4;
    private const int FlipbookLastFrame = FlipbookGrid * FlipbookGrid - 1;
    //시그 · 골글이 뒤집히면 무대 전체가 잠깐 번쩍인다
    private const float ScreenFlashAlpha = 0.35f;
    private const float ScreenFlashSeconds = 0.4f;
    private const float ScreenFlashWhiteness = 0.5f;

    private readonly RectTransform _root;
    private readonly RawImage _haze;
    private readonly RawImage _summon;
    private readonly Image _screenFlash;
    private readonly TextMeshProUGUI _title;
    private readonly GachaRevealView _reveal;

    private Color _lightColor;
    private UiTween _hazeTween;
    private UiTween _summonTween;
    private UiTween _flashTween;

    /// <summary>
    /// 열려 있는가
    /// </summary>
    public bool IsOpen => _root.gameObject.activeSelf;

    /// <summary>
    /// 뒷면이 하나도 남지 않았는가
    /// </summary>
    public bool IsAllRevealed => _reveal.IsAllRevealed;

    /// <summary>
    /// 화면 전체를 덮는 무대를 만들고 숨겨 둔다. 처음 뽑을 때 한 번
    /// </summary>
    public GachaRevealStage(RectTransform parent)
    {
        _root = UiBuilder.CreateRect(parent, "RevealStage");
        UiBuilder.Stretch(_root, 0f, 0f, 0f, 0f);

        CreateBackground();

        Material additive = Resources.Load<Material>(AdditiveMaterialPath);

        _haze = CreateCenteredRaw("Haze", HazePath, additive, HazeSize);

        _title = UiBuilder.CreateText(_root, "Title", 40f, UiPalette.Text1, TextAlignmentOptions.Center);
        UiBuilder.Place(_title.rectTransform, 0f, TitleTop, 1080f, 56f);

        RectTransform resultArea = UiBuilder.CreateRect(_root, "Result");
        UiBuilder.Stretch(resultArea, 32f, ResultTop, 32f, ResultBottom);
        _reveal = new GachaRevealView(resultArea, OnCardRevealed);

        _summon = CreateCenteredRaw("Summon", SummonPath, Resources.Load<Material>(FlareMaterialPath), SummonSize);

        _screenFlash = UiBuilder.CreateImage(_root, "ScreenFlash", Color.clear);
        UiBuilder.Stretch(_screenFlash.rectTransform, 0f, 0f, 0f, 0f);
        _screenFlash.material = additive;
        _screenFlash.gameObject.SetActive(false);

        _root.gameObject.SetActive(false);
    }

    /// <summary>
    /// 무대를 띄우고 빛을 모았다 터뜨리며 결과를 나눠 깐다. lightColor 는 이번에 나온 가장 좋은 카드 색, 다 뒤집힌 뒤 확인을 누르면 onConfirm
    /// </summary>
    public void Open(string title, List<CardDisplayInfo> cards, Color lightColor, Action<int> onOpenCard, Action onConfirm)
    {
        _root.SetAsLastSibling();
        _root.gameObject.SetActive(true);
        _title.text = title;
        _lightColor = lightColor;

        StopLights();
        _reveal.Show(cards, onOpenCard, onConfirm);
        PlayHazeIn();
        PlaySummon();
    }

    /// <summary>
    /// 결과를 걷고 무대를 숨긴다
    /// </summary>
    public void Close()
    {
        StopLights();
        _reveal.Hide();
        _root.gameObject.SetActive(false);
    }

    //1080 × 2340 바탕을 화면 비율과 상관없이 빈틈 없이 덮는다. 아래 로비를 누르지 못하게 입력도 막는다
    private void CreateBackground()
    {
        Image blocker = UiBuilder.CreateImage(_root, "InputBlocker", UiPalette.BgApp);
        UiBuilder.Stretch(blocker.rectTransform, 0f, 0f, 0f, 0f);
        blocker.raycastTarget = true;

        RectTransform art = UiBuilder.CreateRect(_root, "Background");
        art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f);
        art.sizeDelta = new Vector2(ArtWidth, ArtHeight);

        AspectRatioFitter fitter = art.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = ArtWidth / ArtHeight;

        RawImage background = art.gameObject.AddComponent<RawImage>();
        background.texture = Resources.Load<Texture2D>(BackgroundPath);
        background.raycastTarget = false;
    }

    //카드 묶음 가운데(카드를 나눠 주는 자리)에 정사각형으로 두는 가산 합성 층
    private RawImage CreateCenteredRaw(string name, string texturePath, Material material, float size)
    {
        RectTransform rect = UiBuilder.CreateRect(_root, name);
        RawImage image = rect.gameObject.AddComponent<RawImage>();

        image.texture = Resources.Load<Texture2D>(texturePath);
        image.material = material;
        image.raycastTarget = false;
        image.color = Color.clear;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, (ResultBottom - ResultTop) * 0.5f + GachaRevealView.CardsCenterRaise);
        rect.sizeDelta = new Vector2(size, size);

        return image;
    }

    //빛 안개가 이번 최고 등급 색으로 은은하게 깔린다
    private void PlayHazeIn()
    {
        _hazeTween = UiAnimator.Play(HazeInSeconds,
            t => _haze.color = UiPalette.WithAlpha(_lightColor, HazeRestAlpha * t), () => _hazeTween = null, UiEase.OutCubic);
    }

    //빛줄기가 가운데로 빨려 들어 모였다가 터진다. 터지는 순간 결과 자리가 카드를 나눠 준다
    private void PlaySummon()
    {
        _summon.color = UiPalette.WithAlpha(_lightColor, 1f);
        UiFlipbook.SetFrame(_summon, FlipbookGrid, FlipbookGrid, 0);
        _summon.gameObject.SetActive(true);

        _summonTween = UiAnimator.Play(SummonSeconds,
            t => UiFlipbook.SetFrame(_summon, FlipbookGrid, FlipbookGrid, Mathf.Min(FlipbookLastFrame, (int)(t * FlipbookLastFrame))),
            () =>
            {
                _summonTween = null;
                _summon.gameObject.SetActive(false);
            }, UiEase.Linear);
    }

    //카드가 뒤집힐 때마다 안개가 그 카드 색으로 차올랐다 가라앉는다. 시그 · 골글은 무대 전체가 번쩍인다
    private void OnCardRevealed(GachaRevealCard card)
    {
        PulseHaze(card.LightColor, card.LightStrength);

        if (card.IsSpecial)
            PlayScreenFlash(card.LightColor);
    }

    private void PulseHaze(Color color, float strength)
    {
        float peak = HazeRestAlpha + HazePulseAlpha * strength;

        UiAnimator.Cancel(_hazeTween);
        _hazeTween = UiAnimator.Play(HazePulseSeconds, t =>
        {
            float settle = t < HazeRiseRatio ? 0f : (t - HazeRiseRatio) / (1f - HazeRiseRatio);
            float alpha = t < HazeRiseRatio
                ? Mathf.Lerp(HazeRestAlpha, peak, t / HazeRiseRatio)
                : Mathf.Lerp(peak, HazeRestAlpha, settle * settle);

            _haze.color = UiPalette.WithAlpha(Color.Lerp(color, _lightColor, settle), alpha);
        }, () => _hazeTween = null, UiEase.Linear);
    }

    private void PlayScreenFlash(Color color)
    {
        Color flashColor = Color.Lerp(color, Color.white, ScreenFlashWhiteness);

        UiAnimator.Cancel(_flashTween);
        _screenFlash.gameObject.SetActive(true);
        _flashTween = UiAnimator.Play(ScreenFlashSeconds,
            t => _screenFlash.color = UiPalette.WithAlpha(flashColor, ScreenFlashAlpha * (1f - t)), () =>
            {
                _flashTween = null;
                _screenFlash.gameObject.SetActive(false);
            }, UiEase.OutCubic);
    }

    //돌던 빛을 모두 멈추고 숨긴다
    private void StopLights()
    {
        UiAnimator.Cancel(_hazeTween);
        UiAnimator.Cancel(_summonTween);
        UiAnimator.Cancel(_flashTween);
        _hazeTween = null;
        _summonTween = null;
        _flashTween = null;
        _haze.color = Color.clear;
        _summon.gameObject.SetActive(false);
        _screenFlash.gameObject.SetActive(false);
    }
}
