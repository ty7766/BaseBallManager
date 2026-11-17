using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 확인 · 취소를 묻는 가운데 어두운 판. 뒤 화면은 흐리게(ScreenBlur) 깔고, 확인을 눌렀을 때만 넘겨받은 동작을 부른다.
/// 취소 · 판 밖 · 뒤로가기는 아무 일 없이 닫는다. 프리팹에 굽지 않고 런타임에만 만든다
/// </summary>
public class ConfirmPopupView : UiWidgetBehaviour
{
    private const float WindowWidth = 840f;
    private const float Padding = 48f;
    private const float ContentWidth = WindowWidth - Padding * 2f;
    private const float TitleTop = 56f;
    private const float TitleHeight = 60f;
    private const float MessageTop = TitleTop + TitleHeight + 28f;
    private const float MessageHeight = 104f;
    private const float NoteTop = MessageTop + MessageHeight + 12f;
    private const float NoteHeight = 36f;
    private const float ButtonTop = NoteTop + NoteHeight + 44f;
    private const float ButtonHeight = 112f;
    private const float ButtonGap = 24f;
    private const float ButtonWidth = (ContentWidth - ButtonGap) * 0.5f;
    private const float ButtonFontSize = 38f;
    private const float WindowHeight = ButtonTop + ButtonHeight + Padding;
    private const float BackdropShade = 0.6f;
    private const float DimAlpha = 0.4f;
    private const float FadeSeconds = 0.18f;

    /// <summary>
    /// 열려 있는가 (뒤로가기가 먼저 닫는다). 닫히는 중이면 false
    /// </summary>
    public bool IsOpen => gameObject.activeSelf && _group.blocksRaycasts;

    private CanvasGroup _group;
    private RawImage _backdrop;
    private RectTransform _window;
    private TextMeshProUGUI _title;
    private TextMeshProUGUI _message;
    private TextMeshProUGUI _note;
    private RenderTexture _blurTexture;
    private Coroutine _openRoutine;
    private UiTween _fadeTween;
    private Action _onConfirm;

    private void OnDestroy()
    {
        ScreenBlur.Release(_blurTexture);
    }

    /// <summary>
    /// 화면 위에 덮을 확인 창을 만든다. 처음엔 닫혀 있다
    /// </summary>
    public static ConfirmPopupView Create(Transform parent)
    {
        ConfirmPopupView popup = CreateNode<ConfirmPopupView>(parent, "ConfirmPopup");

        popup.BuildPopup();
        popup.SetVisible(false);

        return popup;
    }

    /// <summary>
    /// 질문 · 내용(여러 줄 가능) · 작은 안내(없으면 null)로 연다. 확인을 누르면 onConfirm 을 부른다
    /// </summary>
    public void Show(string title, string message, string note, Action onConfirm)
    {
        UiAnimator.Cancel(_fadeTween);
        _fadeTween = null;
        _onConfirm = onConfirm;

        Root.SetAsLastSibling();
        SetVisible(true);
        _group.alpha = 0f;
        _group.blocksRaycasts = true;

        _title.text = title;
        _message.text = message;
        _note.text = note ?? string.Empty;

        if (_openRoutine != null)
            StopCoroutine(_openRoutine);

        _openRoutine = StartCoroutine(OpenAfterCapture());
    }

    /// <summary>
    /// 아무것도 하지 않고 흐리게 사라지며 닫는다 (취소 · 판 밖 · 뒤로가기)
    /// </summary>
    public void Close()
    {
        if (!IsOpen)
            return;

        _onConfirm = null;
        _group.blocksRaycasts = false;

        if (_openRoutine != null)
        {
            CloseImmediately();
            return;
        }

        UiAnimator.Cancel(_fadeTween);
        _fadeTween = UiAnimator.Play(FadeSeconds, t => _group.alpha = 1f - t, CloseImmediately, UiEase.Linear);
    }

    /// <summary>
    /// 바로 닫고 흐린 화면 텍스처를 돌려준다 (화면을 떠날 때)
    /// </summary>
    public void CloseImmediately()
    {
        if (_openRoutine != null)
        {
            StopCoroutine(_openRoutine);
            _openRoutine = null;
        }

        UiAnimator.Cancel(_fadeTween);
        _fadeTween = null;
        _onConfirm = null;
        _backdrop.texture = null;
        ScreenBlur.Release(_blurTexture);
        _blurTexture = null;
        SetVisible(false);
    }

    //넘겨받은 동작은 창을 닫은 뒤 부른다. 닫으면서 비워지므로 먼저 잡아 둔다
    private void OnConfirmClicked()
    {
        Action onConfirm = _onConfirm;

        Close();
        onConfirm?.Invoke();
    }

    //이 프레임 화면에는 창이 투명하게 그려지므로 찍힌 그림엔 없다. 찍은 뒤 나타난다
    private IEnumerator OpenAfterCapture()
    {
        yield return new WaitForEndOfFrame();

        ScreenBlur.Release(_blurTexture);
        _blurTexture = ScreenBlur.Capture();
        _backdrop.texture = _blurTexture;
        _openRoutine = null;

        _fadeTween = UiEffects.FadeIn(_group, FadeSeconds);
        UiEffects.Pop(_window, 0.9f, 0.24f);
    }

    //흐린 화면 → 어둡게 → 가운데 깎인 판 [질문 · 내용 · 안내 · 취소 | 확인]. 판 밖을 누르면 취소
    private void BuildPopup()
    {
        UiBuilder.Stretch(Root, 0f, 0f, 0f, 0f);
        _group = gameObject.AddComponent<CanvasGroup>();

        RectTransform backdropRect = UiBuilder.CreateRect(Root, "Backdrop");
        UiBuilder.Stretch(backdropRect, 0f, 0f, 0f, 0f);
        _backdrop = backdropRect.gameObject.AddComponent<RawImage>();
        _backdrop.color = new Color(BackdropShade, BackdropShade, BackdropShade, 1f);
        _backdrop.raycastTarget = false;

        Image dim = UiBuilder.CreateImage(Root, "Dim", UiPalette.WithAlpha(UiPalette.BgApp, DimAlpha));
        UiBuilder.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
        dim.raycastTarget = true;

        Button closeArea = dim.gameObject.AddComponent<Button>();
        closeArea.transition = Selectable.Transition.None;
        closeArea.onClick.AddListener(Close);

        Image window = BroadcastPanel.Create(Root, "Window");
        window.raycastTarget = true;
        _window = window.rectTransform;
        _window.anchorMin = new Vector2(0.5f, 0.5f);
        _window.anchorMax = new Vector2(0.5f, 0.5f);
        _window.pivot = new Vector2(0.5f, 0.5f);
        _window.sizeDelta = new Vector2(WindowWidth, WindowHeight);

        Image frame = UiBuilder.CreateImage(_window, "Frame", UiPalette.LineBorder, UiSprites.CutOutline, Image.Type.Sliced);
        UiBuilder.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);

        _title = UiBuilder.CreateText(_window, "Title", 44f, UiPalette.Text1, TextAlignmentOptions.Center);
        UiBuilder.Place(_title.rectTransform, Padding, TitleTop, ContentWidth, TitleHeight);
        _title.fontStyle = FontStyles.Bold;

        _message = UiBuilder.CreateText(_window, "Message", 36f, UiPalette.Text1, TextAlignmentOptions.Center);
        UiBuilder.Place(_message.rectTransform, Padding, MessageTop, ContentWidth, MessageHeight);

        _note = UiBuilder.CreateText(_window, "Note", 28f, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.Place(_note.rectTransform, Padding, NoteTop, ContentWidth, NoteHeight);

        Button cancelButton = UiBuilder.CreateButton(_window, "CancelButton", "취소", ButtonFontSize, false, Close);
        BroadcastPanel.ApplyButtonStyle(cancelButton, false);
        UiBuilder.Place((RectTransform)cancelButton.transform, Padding, ButtonTop, ButtonWidth, ButtonHeight);

        Button confirmButton = UiBuilder.CreateButton(_window, "ConfirmButton", "확인", ButtonFontSize, true, OnConfirmClicked);
        BroadcastPanel.ApplyButtonStyle(confirmButton, true);
        UiBuilder.Place((RectTransform)confirmButton.transform, Padding + ButtonWidth + ButtonGap, ButtonTop, ButtonWidth,
            ButtonHeight);
    }
}
