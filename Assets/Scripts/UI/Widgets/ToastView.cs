using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 성공·실패를 알리는 짧은 안내. 매니저가 bool만 돌려주므로 결과를 화면에 알릴 창구가 필요하다
/// </summary>
public class ToastView
{
    private const float ShowSeconds = 2.2f;

    public RectTransform Root => _root;

    private readonly RectTransform _root;
    private readonly CanvasGroup _group;
    private readonly Image _background;
    private readonly TextMeshProUGUI _messageText;

    private float _remainingSeconds;
    private bool _hiding;

    public ToastView(Transform parent)
    {
        _root = UiBuilder.CreateRect(parent, "Toast");

        _root.anchorMin = new Vector2(0.5f, 0f);
        _root.anchorMax = new Vector2(0.5f, 0f);
        _root.pivot = new Vector2(0.5f, 0f);
        _root.anchoredPosition = new Vector2(0f, 300f);
        _root.sizeDelta = new Vector2(900f, 96f);

        _group = _root.gameObject.AddComponent<CanvasGroup>();

        _background = UiBuilder.CreateImage(_root, "Background", UiPalette.BgSelected,
            UiSprites.PanelSmall, Image.Type.Sliced);
        UiBuilder.Stretch(_background.rectTransform, 0f, 0f, 0f, 0f);

        _messageText = UiBuilder.CreateText(_root, "Message", 32f, UiPalette.Text1, TextAlignmentOptions.Center);
        UiBuilder.Stretch(_messageText.rectTransform, 24f, 0f, 24f, 0f);

        _root.gameObject.SetActive(false);
    }

    /// <summary>
    /// 안내를 띄운다. 실패는 붉은 테두리색으로 구분한다
    /// </summary>
    public void Show(string message, bool success = true)
    {
        if (string.IsNullOrEmpty(message))
            return;

        _messageText.text = message;
        _messageText.color = success ? UiPalette.Text1 : UiPalette.StateOut;
        _background.color = success ? UiPalette.BgSelected : UiPalette.WithAlpha(UiPalette.StateOut, 0.18f);

        _remainingSeconds = ShowSeconds;
        _hiding = false;
        _root.gameObject.SetActive(true);
        _root.SetAsLastSibling();

        UiEffects.SlideUp(_root, _group, 40f, 0.24f);
    }

    /// <summary>
    /// 표시 시간을 깎는다. 흐름 제어기의 Update에서 매 프레임 호출한다
    /// </summary>
    public void Tick(float deltaSeconds)
    {
        if (_remainingSeconds <= 0f || _hiding)
            return;

        _remainingSeconds -= deltaSeconds;

        if (_remainingSeconds > 0f)
            return;

        _hiding = true;
        UiEffects.FadeOutAndHide(_group);
    }
}
