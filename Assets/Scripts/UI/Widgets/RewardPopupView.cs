using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 받은 재화를 알리는 가운데 흰 창. 뒤 화면은 흐리게(ScreenBlur) 깔고, 화면 아무 곳이나 누르면 닫힌다.
/// 재화는 가로 한 줄로 나란히 — 아이콘 + "x 2,000"
/// </summary>
public class RewardPopupView : UiWidgetBehaviour
{
    private const int MaxLines = 4;
    private const float WindowWidth = 900f;
    private const float WindowCornerScale = 0.5f;
    private const float TitleTop = 36f;
    private const float TitleHeight = 48f;
    private const float RowTop = TitleTop + TitleHeight + 24f;
    private const float RowHeight = 64f;
    private const float IconSize = 56f;
    private const float IconTextGap = 10f;
    private const float ItemGap = 48f;
    private const float AmountFontSize = 34f;
    private const float HintTop = RowTop + RowHeight + 22f;
    private const float HintHeight = 32f;
    private const float WindowHeight = HintTop + HintHeight + 32f;
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

        _title = UiBuilder.CreateText(_window, "Title", 34f, UiPalette.ButtonPrimaryText, TextAlignmentOptions.Center);
        UiBuilder.Place(_title.rectTransform, 0f, TitleTop, WindowWidth, TitleHeight);

        for (int i = 0; i < MaxLines; i++)
            BuildItem();

        _hint = UiBuilder.CreateText(_window, "Hint", 22f, UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Place(_hint.rectTransform, 0f, HintTop, WindowWidth, HintHeight);
        _hint.text = "화면을 누르면 닫힙니다";

        _window.sizeDelta = new Vector2(WindowWidth, WindowHeight);
    }

    //아이콘 · x 수량 한 묶음. 가로 위치는 열 때 글자 폭을 재서 정한다
    private void BuildItem()
    {
        Image icon = UiBuilder.CreateImage(_window, "Icon", Color.white);
        TextMeshProUGUI amount = UiBuilder.CreateText(_window, "Amount", AmountFontSize, UiPalette.ButtonPrimaryText);

        icon.preserveAspect = true;
        UiBuilder.Place(icon.rectTransform, 0f, RowTop + (RowHeight - IconSize) * 0.5f, IconSize, IconSize);
        UiBuilder.Place(amount.rectTransform, 0f, RowTop, 0f, RowHeight);

        _lineIcons.Add(icon);
        _lineAmounts.Add(amount);
    }

    //받은 것만 왼쪽부터 나란히 놓고 묶음 전체를 창 가운데로
    private void LayoutLines(IReadOnlyList<RewardLine> lines)
    {
        int used = 0;

        if (lines != null)
        {
            foreach (RewardLine line in lines)
            {
                if (line.Amount <= 0 || used >= MaxLines)
                    continue;

                _lineIcons[used].sprite = UiSprites.Get(line.IconPath);
                _lineAmounts[used].text = $"x {line.Amount:N0}";
                used++;
            }
        }

        float rowWidth = 0f;

        for (int i = 0; i < used; i++)
            rowWidth += GetItemWidth(i) + (i > 0 ? ItemGap : 0f);

        float left = (WindowWidth - rowWidth) * 0.5f;

        for (int i = 0; i < MaxLines; i++)
        {
            bool shown = i < used;

            _lineIcons[i].gameObject.SetActive(shown);
            _lineAmounts[i].gameObject.SetActive(shown);

            if (!shown)
                continue;

            float textWidth = GetItemWidth(i) - IconSize - IconTextGap;
            RectTransform iconRect = _lineIcons[i].rectTransform;
            RectTransform amountRect = _lineAmounts[i].rectTransform;

            iconRect.anchoredPosition = new Vector2(left, iconRect.anchoredPosition.y);
            amountRect.anchoredPosition = new Vector2(left + IconSize + IconTextGap, amountRect.anchoredPosition.y);
            amountRect.sizeDelta = new Vector2(textWidth, RowHeight);
            left += GetItemWidth(i) + ItemGap;
        }
    }

    //아이콘 + 간격 + 글자 폭
    private float GetItemWidth(int index)
    {
        return IconSize + IconTextGap + _lineAmounts[index].GetPreferredValues(_lineAmounts[index].text).x;
    }
}
