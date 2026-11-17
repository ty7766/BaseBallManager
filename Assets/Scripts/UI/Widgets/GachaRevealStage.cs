using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 뽑기 연출 화면. 뽑기 로비 위를 통째로 덮는 짙은 무대에 빛살이 천천히 돌고, 가운데에 결과 카드를 나눠 깐다.
/// 빛 색은 이번에 나온 카드 중 가장 좋은 카드의 색이다. 다 뒤집기 전에는 닫히지 않는다
/// </summary>
public sealed class GachaRevealStage
{
    private const string BackgroundPath = "UI/Gacha/gacha_reveal_bg";
    private const string RaysPath = "UI/Gacha/gacha_reveal_rays";
    private const string BloomSprite = "Fx/card_bloom";
    private const string AdditiveMaterialPath = "UI/Materials/AdditiveLight";
    private const float ArtWidth = 1080f;
    private const float ArtHeight = 2340f;
    //빛살 · 빛무리는 결과 카드 묶음 가운데 뒤에 둔다
    private const float RaysSize = 1700f;
    private const float BloomSize = 1100f;
    private const float LightCenterOffsetY = 40f;
    private const float RaysSecondsPerTurn = 48f;
    private const float RaysAlpha = 0.5f;
    private const float BloomAlpha = 0.35f;
    private const float LightInSeconds = 0.5f;
    private const float TitleTop = 120f;
    private const float ResultTop = 220f;
    private const float ResultBottom = 64f;

    private readonly RectTransform _root;
    private readonly RawImage _rays;
    private readonly Image _bloom;
    private readonly TextMeshProUGUI _title;
    private readonly GachaRevealView _reveal;

    private UiTween _spinTween;
    private UiTween _lightTween;

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

        _bloom = UiBuilder.CreateImage(_root, "Bloom", Color.clear, BloomSprite);
        _bloom.material = additive;
        PlaceAtLightCenter(_bloom.rectTransform, BloomSize);

        RectTransform raysRect = UiBuilder.CreateRect(_root, "Rays");
        _rays = raysRect.gameObject.AddComponent<RawImage>();
        _rays.texture = Resources.Load<Texture2D>(RaysPath);
        _rays.material = additive;
        _rays.raycastTarget = false;
        PlaceAtLightCenter(raysRect, RaysSize);

        _title = UiBuilder.CreateText(_root, "Title", 40f, UiPalette.Text1, TextAlignmentOptions.Center);
        UiBuilder.Place(_title.rectTransform, 0f, TitleTop, 1080f, 56f);

        RectTransform resultArea = UiBuilder.CreateRect(_root, "Result");
        UiBuilder.Stretch(resultArea, 32f, ResultTop, 32f, ResultBottom);
        _reveal = new GachaRevealView(resultArea);

        _root.gameObject.SetActive(false);
    }

    /// <summary>
    /// 무대를 띄우고 결과를 나눠 깐다. 다 뒤집힌 뒤 확인을 누르면 onConfirm
    /// </summary>
    public void Open(string title, List<CardDisplayInfo> cards, Color lightColor, Action<int> onOpenCard, Action onConfirm)
    {
        _root.SetAsLastSibling();
        _root.gameObject.SetActive(true);
        _title.text = title;
        _reveal.Show(cards, onOpenCard, onConfirm);

        StopLights();
        PlayLightsIn(lightColor);
        _spinTween = UiAnimator.Play(3600f, t => _rays.rectTransform.localEulerAngles =
            new Vector3(0f, 0f, -360f * (t * 3600f / RaysSecondsPerTurn % 1f)), null, UiEase.Linear);
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

    //결과 자리 가운데(카드 묶음 뒤)에 정사각형으로 둔다
    private void PlaceAtLightCenter(RectTransform rect, float size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, (ResultBottom - ResultTop) * 0.5f + LightCenterOffsetY);
        rect.sizeDelta = new Vector2(size, size);
    }

    //빛살 · 빛무리가 등급 색으로 차오른다
    private void PlayLightsIn(Color lightColor)
    {
        _rays.color = UiPalette.WithAlpha(lightColor, 0f);
        _bloom.color = UiPalette.WithAlpha(lightColor, 0f);

        _lightTween = UiAnimator.Play(LightInSeconds, t =>
        {
            _rays.color = UiPalette.WithAlpha(lightColor, RaysAlpha * t);
            _bloom.color = UiPalette.WithAlpha(lightColor, BloomAlpha * t);
        }, null, UiEase.OutCubic);
    }

    //돌던 빛을 멈춘다
    private void StopLights()
    {
        UiAnimator.Cancel(_spinTween);
        UiAnimator.Cancel(_lightTween);
        _spinTween = null;
        _lightTween = null;
    }
}
