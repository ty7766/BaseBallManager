using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 받은 재화를 알리는 가운데 흰 창. 뒤 화면은 흐리게(ScreenBlur) 깔고, 화면 아무 곳이나 누르면 닫힌다.
/// 줄마다 재화 아이콘 + "x 2,000"
/// </summary>
public class RewardPopupView : UiWidgetBehaviour
{
    private const int MaxLines = 4;
    private const float WindowWidth = 720f;
    private const float WindowCornerScale = 0.5f;
    private const float TitleTop = 44f;
    private const float TitleHeight = 60f;
    private const float LinesTop = TitleTop + TitleHeight + 28f;
    private const float LineHeight = 96f;
    private const float IconSize = 76f;
    private const float LineGroupWidth = 420f;
    private const float HintHeight = 40f;
    private const float BottomPadding = 40f;
    private const float BackdropShade = 0.7f;
    private const float DimAlpha = 0.25f;
    private const float FadeSeconds = 0.18f;

    /// <summary>
    /// 열려 있는가 (뒤로가기가 먼저 닫는다)
    /// </summary>
    public bool IsOpen => gameObject.activeSelf;

    private readonly List<Image> _lineIcons = new List<Image>(MaxLines);
    private readonly List<TextMeshProUGUI> _lineAmounts = new List<TextMeshProUGUI>(MaxLines);

    private CanvasGroup _group;
    private RawImage _backdrop;
    private RectTransform _window;
    private TextMeshProUGUI _title;
    private TextMeshProUGUI _hint;
    private RenderTexture _blurTexture;
    private Coroutine _openRoutine;

    private void OnDestroy()
    {
        ScreenBlur.Release(_blurTexture);
    }

    /// <summary>
    /// 화면 위에 덮을 팝업을 만든다. 처음엔 닫혀 있다
    /// </summary>
    public static RewardPopupView Create(Transform parent)
    {
        RewardPopupView popup = CreateNode<RewardPopupView>(parent, "RewardPopup");

        popup.BuildPopup();
        popup.SetVisible(false);

        return popup;
    }

    /// <summary>
    /// 제목과 받은 재화로 연다. 0 인 줄은 빼고 보인다
    /// </summary>
    public void Show(string title, IReadOnlyList<RewardLine> lines)
    {
        Root.SetAsLastSibling();
        SetVisible(true);
        _group.alpha = 0f;
        _group.blocksRaycasts = true;

        _title.text = title;
        LayoutLines(lines);

        if (_openRoutine != null)
            StopCoroutine(_openRoutine);

        _openRoutine = StartCoroutine(OpenAfterCapture());
    }

    /// <summary>
    /// 흐리게 사라지며 닫는다
    /// </summary>
    public void Close()
    {
        if (!IsOpen)
            return;

        _group.blocksRaycasts = false;
        UiAnimator.Play(FadeSeconds, t => _group.alpha = 1f - t, CloseImmediately, UiEase.Linear);
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

        _backdrop.texture = null;
        ScreenBlur.Release(_blurTexture);
        _blurTexture = null;
        SetVisible(false);
    }

    //이 프레임 화면에는 팝업이 투명하게 그려지므로 찍힌 그림엔 없다. 찍은 뒤 나타난다
    private IEnumerator OpenAfterCapture()
    {
        yield return new WaitForEndOfFrame();

        ScreenBlur.Release(_blurTexture);
        _blurTexture = ScreenBlur.Capture();
        _backdrop.texture = _blurTexture;
        _openRoutine = null;

        UiEffects.FadeIn(_group, FadeSeconds);
        UiEffects.Pop(_window, 0.9f, 0.24f);
    }

    //흐린 화면 → 살짝 어둡게 → 가운데 흰 창 [제목 · 재화 줄들 · 닫기 안내]. 화면 전체가 닫기 버튼
    private void BuildPopup()
    {
        UiBuilder.Stretch(Root, 0f, 0f, 0f, 0f);
        _group = gameObject.AddComponent<CanvasGroup>();

        RectTransform backdropRect = UiBuilder.CreateRect(Root, "Backdrop");
        UiBuilder.Stretch(backdropRect, 0f, 0f, 0f, 0f);
        _backdrop = backdropRect.gameObject.AddComponent<RawImage>();
        _backdrop.color = new Color(BackdropShade, BackdropShade, BackdropShade, 1f);
        _backdrop.raycastTarget = false;

        Image dim = UiBuilder.CreateImage(Root, "Dim", UiPalette.WithAlpha(Color.black, DimAlpha));
        UiBuilder.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
        dim.raycastTarget = true;

        Button closeArea = dim.gameObject.AddComponent<Button>();
        closeArea.transition = Selectable.Transition.None;
        closeArea.onClick.AddListener(Close);

        Image window = UiBuilder.CreateImage(Root, "Window", UiPalette.ButtonPrimary, UiSprites.ButtonPrimary, Image.Type.Sliced);
        window.pixelsPerUnitMultiplier = WindowCornerScale;
        _window = window.rectTransform;
        _window.anchorMin = new Vector2(0.5f, 0.5f);
        _window.anchorMax = new Vector2(0.5f, 0.5f);
        _window.pivot = new Vector2(0.5f, 0.5f);

        _title = UiBuilder.CreateText(_window, "Title", 44f, UiPalette.ButtonPrimaryText, TextAlignmentOptions.Center);
        UiBuilder.Place(_title.rectTransform, 0f, TitleTop, WindowWidth, TitleHeight);

        for (int i = 0; i < MaxLines; i++)
            BuildLine();

        _hint = UiBuilder.CreateText(_window, "Hint", 26f, UiPalette.Text3, TextAlignmentOptions.Center);
        _hint.text = "화면을 누르면 닫힙니다";
    }

    //아이콘 · x 수량. 줄 묶음을 창 가운데에 둔다
    private void BuildLine()
    {
        float left = (WindowWidth - LineGroupWidth) * 0.5f;
        Image icon = UiBuilder.CreateImage(_window, "Icon", Color.white);
        TextMeshProUGUI amount = UiBuilder.CreateText(_window, "Amount", 52f, UiPalette.ButtonPrimaryText);

        icon.preserveAspect = true;
        UiBuilder.Place(icon.rectTransform, left, 0f, IconSize, IconSize);
        UiBuilder.Place(amount.rectTransform, left + IconSize + 24f, 0f, LineGroupWidth - IconSize - 24f, IconSize);

        _lineIcons.Add(icon);
        _lineAmounts.Add(amount);
    }

    //받은 것만 위에서부터 채우고 창 높이를 맞춘다
    private void LayoutLines(IReadOnlyList<RewardLine> lines)
    {
        int used = 0;

        if (lines != null)
        {
            foreach (RewardLine line in lines)
            {
                if (line.Amount <= 0 || used >= MaxLines)
                    continue;

                float top = LinesTop + used * LineHeight + (LineHeight - IconSize) * 0.5f;

                _lineIcons[used].sprite = UiSprites.Get(line.IconPath);
                _lineIcons[used].rectTransform.anchoredPosition = new Vector2(_lineIcons[used].rectTransform.anchoredPosition.x, -top);
                _lineAmounts[used].rectTransform.anchoredPosition = new Vector2(_lineAmounts[used].rectTransform.anchoredPosition.x, -top);
                _lineAmounts[used].text = $"x {line.Amount:N0}";
                used++;
            }
        }

        for (int i = 0; i < MaxLines; i++)
        {
            _lineIcons[i].gameObject.SetActive(i < used);
            _lineAmounts[i].gameObject.SetActive(i < used);
        }

        float hintTop = LinesTop + used * LineHeight + 12f;

        UiBuilder.Place(_hint.rectTransform, 0f, hintTop, WindowWidth, HintHeight);
        _window.sizeDelta = new Vector2(WindowWidth, hintTop + HintHeight + BottomPadding);
    }
}
