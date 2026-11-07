using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 목록·그리드에 깔리는 카드 1장. 실제 카드 그림(<see cref="CardView"/>)을 그대로 쓰고
/// 선택 테두리 · 흐리게만 더한다 (잠금 자물쇠는 카드가 직접 그린다). 뒷면은 만들지 않아 노드 수를 절반으로 줄인다
/// </summary>
public class CardCellView
{
    /// <summary>
    /// 타일 규격 (3열 기준). 카드 비율 1398:1957 을 그대로 따른다
    /// </summary>
    public const float CellWidth = 328f;
    public const float CellHeight = CellWidth * CardView.BaseHeight / CardView.BaseWidth;

    private const float BorderThickness = 5f;
    private const float BlockedDimAlpha = 0.68f;
    //체크 방식 선택 표시 — 카드를 어둡게 덮고 가운데 흰 선 동그라미 체크
    private const float CheckDimAlpha = 0.75f;
    private const float CheckSize = 150f;
    private const string CheckSprite = "Common/check_circle";

    public RectTransform Root => _root;
    public int InstanceId { get; private set; }

    private readonly RectTransform _root;
    private readonly CardView _card;
    private readonly CanvasGroup _group;
    private readonly Image _border;
    private readonly Image _blockedDim;
    private readonly TMPro.TextMeshProUGUI _blockedText;

    private Action<int> _onClick;
    private Action<int> _onLongPress;
    private Image _checkDim;
    private Image _checkMark;
    private bool _checkStyle;
    private bool _selected;

    public CardCellView(Transform parent)
    {
        _root = UiBuilder.CreateRect(parent, "CardCell");
        UiBuilder.Place(_root, 0f, 0f, CellWidth, CellHeight);

        _group = _root.gameObject.AddComponent<CanvasGroup>();

        _card = CardView.Bake(_root, CellWidth, false);
        UiBuilder.Stretch(_card.Root, 0f, 0f, 0f, 0f);

        _border = UiBuilder.CreateImage(_root, "Border", UiPalette.ButtonPrimary,
            UiSprites.ButtonNormal, Image.Type.Sliced);
        UiBuilder.Stretch(_border.rectTransform,
            -BorderThickness, -BorderThickness, -BorderThickness, -BorderThickness);
        _border.raycastTarget = false;
        _border.gameObject.SetActive(false);

        _blockedDim = UiBuilder.CreateImage(_root, "BlockedDim",
            UiPalette.WithAlpha(Color.black, BlockedDimAlpha));
        UiBuilder.Stretch(_blockedDim.rectTransform, 0f, 0f, 0f, 0f);
        _blockedDim.raycastTarget = false;

        _blockedText = UiBuilder.CreateText(_blockedDim.transform, "BlockedText", 40f,
            UiPalette.Text1, TMPro.TextAlignmentOptions.Center);
        UiBuilder.Stretch(_blockedText.rectTransform, 8f, 0f, 8f, 0f);

        _blockedDim.gameObject.SetActive(false);
    }

    /// <summary>
    /// 고를 수 없는 카드에 반투명 필터와 사유를 덮는다. reason 이 비면 걷어낸다
    /// </summary>
    public void SetBlocked(string reason)
    {
        bool blocked = !string.IsNullOrEmpty(reason);

        _blockedDim.gameObject.SetActive(blocked);
        _blockedText.text = reason;
        _card.SetClickHandler(blocked || _onClick == null ? null : OnClicked);
    }

    /// <summary>
    /// 그리드 안의 위치 (열·행 번호로 배치)
    /// </summary>
    public void SetGridPosition(int column, int row, float columnGap, float rowGap,
        float width, float offsetX, float offsetY)
    {
        _card.SetDisplayWidth(width);
        PlaceInGrid(_root, column, row, columnGap, rowGap, width, offsetX, offsetY);
    }

    /// <summary>
    /// 타일 규격(CellWidth)으로 조립한 칸을 그리드 자리에 놓는다. 카드 속 좌표는 조립할 때 굳으므로 크기는 스케일로 바꾼다
    /// </summary>
    public static void PlaceInGrid(RectTransform root, int column, int row, float columnGap, float rowGap,
        float width, float offsetX, float offsetY)
    {
        float scale = width / CellWidth;
        float height = CellHeight * scale;

        root.localScale = new Vector3(scale, scale, 1f);

        UiBuilder.Place(root,
            offsetX + column * (width + columnGap),
            offsetY + row * (height + rowGap),
            CellWidth, CellHeight);
    }

    /// <summary>
    /// 카드 내용을 채운다
    /// </summary>
    public void SetCard(CardDisplayInfo info)
    {
        InstanceId = info.InstanceId;

        _card.SetCard(info);
        _card.SetClickHandler(_onClick == null ? null : OnClicked);
        _card.SetLongPressHandler(_onLongPress == null ? null : OnLongPressed);

        SetSelected(false);
        SetDimmed(false);
        SetBlocked(null);
    }

    /// <summary>
    /// 선택 표시. animate 면 새로 골라졌을 때 체크가 튀어나온다
    /// </summary>
    public void SetSelected(bool selected, bool animate = false)
    {
        bool turnedOn = selected && !_selected;

        _selected = selected;
        ApplySelection(animate && turnedOn);
    }

    /// <summary>
    /// 선택 표시 방식. true 면 테두리 대신 어둡게 덮고 가운데 체크 (강화 재료)
    /// </summary>
    public void SetCheckStyle(bool checkStyle)
    {
        _checkStyle = checkStyle;
        ApplySelection(false);
    }

    /// <summary>
    /// 고를 수 없는 카드는 흐리게 그린다 (라인업에 이미 들어간 카드 등)
    /// </summary>
    public void SetDimmed(bool dimmed)
    {
        _group.alpha = dimmed ? 0.4f : 1f;
    }

    public void SetClickHandler(Action<int> onClick)
    {
        _onClick = onClick;
        _card.SetClickHandler(onClick == null ? null : OnClicked);
    }

    /// <summary>
    /// 2초 길게 누르면 부를 콜백 (카드 상세 보기)
    /// </summary>
    public void SetLongPressHandler(Action<int> onLongPress)
    {
        _onLongPress = onLongPress;
        _card.SetLongPressHandler(onLongPress == null ? null : OnLongPressed);
    }

    private void OnLongPressed()
    {
        _onLongPress?.Invoke(InstanceId);
    }

    private void ApplySelection(bool animate)
    {
        bool check = _selected && _checkStyle;

        _border.gameObject.SetActive(_selected && !_checkStyle);

        if (check)
            EnsureCheckMark();

        if (_checkDim != null)
            _checkDim.gameObject.SetActive(check);

        if (check && animate)
            UiEffects.Pop(_checkMark.transform, 0.5f, 0.22f);
    }

    //체크 표시는 체크 방식 목록에서 처음 고를 때 만든다
    private void EnsureCheckMark()
    {
        if (_checkDim != null)
            return;

        _checkDim = CreateCheckOverlay(_root, out _checkMark);
    }

    /// <summary>
    /// 타일(CellWidth 규격) 위에 선택 표시를 만든다 — 어둡게 덮고 가운데 흰 선 체크.
    /// 카드 칸과 강화 전용 카드 칸이 같은 위치 · 같은 모양을 쓰도록 여기서만 만든다
    /// </summary>
    public static Image CreateCheckOverlay(RectTransform tile, out Image checkMark)
    {
        Image dim = UiBuilder.CreateImage(tile, "CheckDim", UiPalette.WithAlpha(Color.black, CheckDimAlpha));
        UiBuilder.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
        dim.raycastTarget = false;

        checkMark = UiBuilder.CreateImage(dim.transform, "Check", Color.white, CheckSprite);
        UiBuilder.CenterPivot(UiBuilder.Place(checkMark.rectTransform, (CellWidth - CheckSize) * 0.5f,
            (CellHeight - CheckSize) * 0.5f, CheckSize, CheckSize));
        checkMark.raycastTarget = false;

        return dim;
    }

    public void SetVisible(bool visible)
    {
        _root.gameObject.SetActive(visible);
    }

    private void OnClicked()
    {
        _onClick?.Invoke(InstanceId);
    }
}
