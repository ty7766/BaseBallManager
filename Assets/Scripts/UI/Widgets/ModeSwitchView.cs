using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 교체 모드 슬라이드 스위치. 수동(왼쪽) · 자동(오른쪽) 두 칸 위로 흰 손잡이가 미끄러진다.
/// 이미 고른 쪽을 누르면 아무 일도 없고, 반대쪽을 누를 때만 바꾸기를 요청한다
/// </summary>
public class ModeSwitchView : UiWidgetBehaviour
{
    private const float SlideDuration = 0.22f;
    private const float KnobInset = 6f;
    private const float TrackAlpha = 0.9f;

    [SerializeField] private RectTransform _knob;
    [SerializeField] private Button _manualButton;
    [SerializeField] private Button _autoButton;
    [SerializeField] private TextMeshProUGUI _manualText;
    [SerializeField] private TextMeshProUGUI _autoText;
    [SerializeField] private float _slideDistance;

    private Action _onToggleRequested;
    private UiTween _slideTween;
    private bool _isManual;
    private bool _hasState;

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static ModeSwitchView Bake(Transform parent, float width, float height, float fontSize)
    {
        ModeSwitchView view = CreateNode<ModeSwitchView>(parent, "ModeSwitch");

        view.BuildWidget(width, height, fontSize);

        return view;
    }

    /// <summary>
    /// 반대쪽 칸을 눌렀을 때 부를 동작 (모드 바꾸기)
    /// </summary>
    public void SetToggleHandler(Action onToggleRequested)
    {
        _onToggleRequested = onToggleRequested;
    }

    /// <summary>
    /// 현재 모드를 보여 준다. 처음 한 번은 바로 놓고, 그 뒤 바뀔 때는 손잡이가 미끄러진다
    /// </summary>
    public void SetMode(bool isManual)
    {
        bool animate = _hasState && _isManual != isManual;

        _isManual = isManual;
        _hasState = true;
        _manualText.color = isManual ? UiPalette.ButtonPrimaryText : UiPalette.Text2;
        _autoText.color = isManual ? UiPalette.Text2 : UiPalette.ButtonPrimaryText;

        UiAnimator.Cancel(_slideTween);
        _slideTween = null;

        float target = isManual ? 0f : _slideDistance;

        if (!animate)
        {
            _knob.anchoredPosition = new Vector2(target, 0f);
            return;
        }

        float start = _knob.anchoredPosition.x;

        _slideTween = UiAnimator.Play(SlideDuration,
            t => _knob.anchoredPosition = new Vector2(Mathf.LerpUnclamped(start, target, t), 0f),
            null, UiEase.OutBack);
    }

    protected override void BindEvents()
    {
        Bind(_manualButton, OnManualClicked);
        Bind(_autoButton, OnAutoClicked);
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(float width, float height, float fontSize)
    {
        _slideDistance = (width - KnobInset * 2f) * 0.5f;
        UiBuilder.Place(Root, 0f, 0f, width, height);

        Image track = UiBuilder.CreateImage(Root, "Track", UiPalette.WithAlpha(UiPalette.BgApp, TrackAlpha),
            UiSprites.CutFill, Image.Type.Sliced);
        UiBuilder.Stretch(track.rectTransform, 0f, 0f, 0f, 0f);

        Image frame = UiBuilder.CreateImage(Root, "Frame", UiPalette.LineBorder, UiSprites.CutOutline, Image.Type.Sliced);
        UiBuilder.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);

        RectTransform knobSlot = UiBuilder.CreateRect(Root, "KnobSlot");
        UiBuilder.Stretch(knobSlot, KnobInset, KnobInset, KnobInset, KnobInset);

        Image knob = UiBuilder.CreateImage(knobSlot, "Knob", UiPalette.ButtonPrimary, UiSprites.CutFill, Image.Type.Sliced);
        _knob = knob.rectTransform;
        _knob.anchorMin = new Vector2(0f, 0f);
        _knob.anchorMax = new Vector2(0f, 1f);
        _knob.pivot = new Vector2(0f, 0.5f);
        _knob.sizeDelta = new Vector2(_slideDistance, 0f);
        _knob.anchoredPosition = Vector2.zero;

        _manualButton = CreateHalf("Manual", "수동", 0f, fontSize, OnManualClicked, out _manualText);
        _autoButton = CreateHalf("Auto", "자동", 0.5f, fontSize, OnAutoClicked, out _autoText);
    }

    //스위치 절반 칸. 누르는 자리이자 글자 자리
    private Button CreateHalf(string name, string label, float anchorX, float fontSize, UnityEngine.Events.UnityAction onClick,
        out TextMeshProUGUI text)
    {
        Image hit = UiBuilder.CreateImage(Root, name, Color.clear);
        RectTransform rect = hit.rectTransform;

        rect.anchorMin = new Vector2(anchorX, 0f);
        rect.anchorMax = new Vector2(anchorX + 0.5f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        hit.raycastTarget = true;

        Button button = hit.gameObject.AddComponent<Button>();
        button.targetGraphic = hit;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(onClick);

        text = UiBuilder.CreateText(rect, "Label", fontSize, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
        text.text = label;
        text.fontStyle = FontStyles.Bold;

        return button;
    }

    private void OnManualClicked()
    {
        if (!_isManual)
            _onToggleRequested?.Invoke();
    }

    private void OnAutoClicked()
    {
        if (_isManual)
            _onToggleRequested?.Invoke();
    }
}
