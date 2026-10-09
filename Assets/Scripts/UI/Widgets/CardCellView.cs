using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 목록·그리드용 축약 카드 타일. 사진을 올리지 않아 수백 장을 깔아도 가볍다.
/// 사진이 들어간 전체 카드는 <see cref="CardView"/>가 상세 보기에서 그린다
/// </summary>
public class CardCellView
{
    /// <summary>
    /// 타일 규격 (3열 기준)
    /// </summary>
    public const float CellWidth = 328f;
    public const float CellHeight = 240f;

    private const int MaxStars = 5;
    private const float StarSize = 26f;

    public RectTransform Root => _root;
    public int InstanceId { get; private set; }

    private readonly RectTransform _root;
    private readonly Image _background;
    private readonly Image _border;
    private readonly Image _lockIcon;
    private readonly TextMeshProUGUI _nameText;
    private readonly TextMeshProUGUI _detailText;
    private readonly TextMeshProUGUI _ovrText;
    private readonly TextMeshProUGUI _enhanceText;
    private readonly Image[] _stars = new Image[MaxStars];
    private readonly Button _button;

    private Action<int> _onClick;
    private bool _isHighlight;

    public CardCellView(Transform parent)
    {
        _root = UiBuilder.CreateRect(parent, "CardCell");
        UiBuilder.Place(_root, 0f, 0f, CellWidth, CellHeight);

        _background = UiBuilder.CreateImage(_root, "Background", UiPalette.BgRow,
            UiSprites.RowNormal, Image.Type.Sliced);
        UiBuilder.Stretch(_background.rectTransform, 0f, 0f, 0f, 0f);
        _background.raycastTarget = true;

        _button = _root.gameObject.AddComponent<Button>();
        _button.targetGraphic = _background;
        _button.transition = Selectable.Transition.None;
        _button.onClick.AddListener(OnClicked);

        _border = UiBuilder.CreateImage(_root, "Border", UiPalette.ButtonPrimary,
            UiSprites.TagOutline, Image.Type.Sliced);
        UiBuilder.Stretch(_border.rectTransform, 0f, 0f, 0f, 0f);
        _border.gameObject.SetActive(false);

        _ovrText = UiBuilder.CreateText(_root, "Ovr", 68f, UiPalette.Text1, TextAlignmentOptions.MidlineLeft);
        UiBuilder.Place(_ovrText.rectTransform, 18f, 14f, 160f, 76f);

        _enhanceText = UiBuilder.CreateText(_root, "Enhance", 32f, UiPalette.AccentGold, TextAlignmentOptions.Right);
        UiBuilder.Place(_enhanceText.rectTransform, CellWidth - 110f, 18f, 92f, 40f);

        RectTransform starRow = UiBuilder.CreateRect(_root, "Stars");
        UiBuilder.Place(starRow, 18f, 92f, MaxStars * (StarSize + 3f), StarSize + 4f);

        for (int i = 0; i < MaxStars; i++)
        {
            _stars[i] = UiBuilder.CreateImage(starRow, "Star", UiPalette.CardNormal, UiSprites.IconStar);
            UiBuilder.Place(_stars[i].rectTransform, i * (StarSize + 3f), 0f, StarSize, StarSize);
            _stars[i].gameObject.SetActive(false);
        }

        _nameText = UiBuilder.CreateText(_root, "Name", 36f, UiPalette.Text1);
        UiBuilder.Place(_nameText.rectTransform, 18f, 130f, CellWidth - 36f, 48f);

        _detailText = UiBuilder.CreateText(_root, "Detail", 28f, UiPalette.Text3);
        UiBuilder.Place(_detailText.rectTransform, 18f, 180f, CellWidth - 36f, 40f);

        _lockIcon = UiBuilder.CreateImage(_root, "Lock", UiPalette.Text2, UiSprites.IconLock);
        UiBuilder.Place(_lockIcon.rectTransform, CellWidth - 52f, CellHeight - 52f, 36f, 36f);
        _lockIcon.gameObject.SetActive(false);
    }

    /// <summary>
    /// 그리드 안의 위치 (열·행 번호로 배치)
    /// </summary>
    public void SetGridPosition(int column, int row, float columnGap, float rowGap)
    {
        UiBuilder.Place(_root,
            column * (CellWidth + columnGap),
            row * (CellHeight + rowGap),
            CellWidth, CellHeight);
    }

    /// <summary>
    /// 카드 내용을 채운다
    /// </summary>
    public void SetCard(CardDisplayInfo info)
    {
        InstanceId = info.InstanceId;

        Color typeColor = CardVisualLookup.GetTypeColor(info.CardType);

        _ovrText.text = info.FinalOvr.ToString();
        _ovrText.color = typeColor;

        _nameText.text = PlayerLabelFormat.NameWithYear(info.Name, info.Year);
        _nameText.color = UiPalette.Text1;
        _detailText.text = $"{info.Position} · {info.TeamName}";

        _enhanceText.text = info.EnhanceLevel > 0 ? "+" + info.EnhanceLevel : string.Empty;

        int starCount = (int)info.CardGrade;

        for (int i = 0; i < MaxStars; i++)
        {
            bool shown = i < starCount;

            _stars[i].gameObject.SetActive(shown);

            if (shown)
                _stars[i].color = typeColor;
        }

        _lockIcon.gameObject.SetActive(info.IsLocked);
        _isHighlight = info.CardGrade == CardGrade.Star5 || info.CardType != CardType.Normal;

        SetSelected(false);
    }

    /// <summary>
    /// 뽑기 결과가 하나씩 열리는 연출. 5성·시그·골글은 파티클이 함께 터진다
    /// </summary>
    public void PlayReveal(float delay)
    {
        UiEffects.Pop(_root, 0.5f, 0.3f, delay);

        if (!_isHighlight)
            return;

        UiAnimator.Play(0.01f, null, () =>
        {
            UiEffects.Flash(_background, UiPalette.WithAlpha(_ovrText.color, 0.5f), 0.5f);
            UiEffects.Burst(_root, UiEffectKind.Reveal);
        }, UiEase.Linear, delay);
    }

    /// <summary>
    /// 선택 테두리
    /// </summary>
    public void SetSelected(bool selected)
    {
        _border.gameObject.SetActive(selected);
        _background.color = selected ? UiPalette.BgSelected : UiPalette.BgRow;
    }

    /// <summary>
    /// 고를 수 없는 카드는 흐리게 그린다 (라인업에 이미 들어간 카드 등)
    /// </summary>
    public void SetDimmed(bool dimmed)
    {
        float alpha = dimmed ? 0.4f : 1f;

        _nameText.color = UiPalette.WithAlpha(UiPalette.Text1, alpha);
        _detailText.color = UiPalette.WithAlpha(UiPalette.Text3, alpha);
        _ovrText.color = UiPalette.WithAlpha(_ovrText.color, alpha);
    }

    public void SetClickHandler(Action<int> onClick)
    {
        _onClick = onClick;
        _button.interactable = onClick != null;
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
