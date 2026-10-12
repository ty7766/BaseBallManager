using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 목록·그리드에 깔리는 카드 1장. 실제 카드 그림(<see cref="CardView"/>)을 그대로 쓰고
/// 선택 테두리 · 잠금 · 흐리게만 더한다. 뒷면은 만들지 않아 노드 수를 절반으로 줄인다
/// </summary>
public class CardCellView
{
    /// <summary>
    /// 타일 규격 (3열 기준). 카드 비율 1398:1957 을 그대로 따른다
    /// </summary>
    public const float CellWidth = 328f;
    public const float CellHeight = CellWidth * CardView.BaseHeight / CardView.BaseWidth;

    private const float BorderThickness = 5f;
    private const float LockSize = 52f;

    public RectTransform Root => _root;
    public int InstanceId { get; private set; }

    private readonly RectTransform _root;
    private readonly CardView _card;
    private readonly CanvasGroup _group;
    private readonly Image _border;
    private readonly Image _lockIcon;

    private Action<int> _onClick;
    private bool _isHighlight;

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

        _lockIcon = UiBuilder.CreateImage(_root, "Lock", UiPalette.Text1, UiSprites.IconLock);
        UiBuilder.PlaceRight(_lockIcon.rectTransform, 10f, 10f, LockSize, LockSize);
        _lockIcon.raycastTarget = false;
        _lockIcon.gameObject.SetActive(false);
    }

    /// <summary>
    /// 그리드 안의 위치 (열·행 번호로 배치)
    /// </summary>
    public void SetGridPosition(int column, int row, float columnGap, float rowGap,
        float width, float offsetX, float offsetY)
    {
        //카드 속 좌표는 조립할 때 굳으므로 크기는 스케일로 바꾼다
        float scale = width / CellWidth;
        float height = CellHeight * scale;

        _root.localScale = new Vector3(scale, scale, 1f);

        UiBuilder.Place(_root,
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

        _lockIcon.gameObject.SetActive(info.IsLocked);
        _isHighlight = info.CardGrade == CardGrade.Star5 || info.CardType != CardType.Normal;

        SetSelected(false);
        SetDimmed(false);
    }

    /// <summary>
    /// 뽑기 결과가 하나씩 열리는 연출. 5성·시그·골글은 파티클이 함께 터진다
    /// </summary>
    public void PlayReveal(float delay)
    {
        _card.PlayAppear(delay);

        if (!_isHighlight)
            return;

        UiAnimator.Play(0.01f, null, () => UiEffects.Burst(_root, UiEffectKind.Reveal),
            UiEase.Linear, delay);
    }

    /// <summary>
    /// 선택 테두리
    /// </summary>
    public void SetSelected(bool selected)
    {
        _border.gameObject.SetActive(selected);
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

    public void SetVisible(bool visible)
    {
        _root.gameObject.SetActive(visible);
    }

    private void OnClicked()
    {
        _onClick?.Invoke(InstanceId);
    }
}
