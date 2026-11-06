using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 명세 1-1 A 밀도 라인업 행 (96px). 타순·포지션·이름·연도·별·OVR을 담는다
/// </summary>
public class LineupRowView : UiWidgetBehaviour
{
    /// <summary>
    /// 행 높이 (명세 1-1)
    /// </summary>
    public const float RowHeight = 96f;

    private const int MaxStars = 5;
    private const float StarSize = 23f;

    [SerializeField] private Image _background;
    [SerializeField] private Image _border;
    [SerializeField] private TextMeshProUGUI _slotText;
    [SerializeField] private TextMeshProUGUI _positionText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _ovrText;
    [SerializeField] private TextMeshProUGUI _stateTagText;
    [SerializeField] private Image[] _stars = new Image[MaxStars];
    [SerializeField] private Button _button;

    private Action _onClick;

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static LineupRowView Bake(Transform parent, float width)
    {
        LineupRowView view = CreateNode<LineupRowView>(parent, "LineupRow");

        view.BuildWidget(parent, width);

        return view;
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    protected override void BindEvents()
    {
        Bind(_button, OnClicked);
    }

    private void BuildWidget(Transform parent, float width)
    {
        UiBuilder.Place(Root, 0f, 0f, width, RowHeight);

        _background = UiBuilder.CreateImage(Root, "Background", UiPalette.BgRow,
            UiSprites.RowNormal, Image.Type.Sliced);
        UiBuilder.Stretch(_background.rectTransform, 0f, 0f, 0f, 0f);
        _background.raycastTarget = true;

        _button = Root.gameObject.AddComponent<Button>();
        _button.targetGraphic = _background;
        _button.transition = Selectable.Transition.None;
        _button.interactable = false;
        _button.onClick.AddListener(OnClicked);
        Root.gameObject.AddComponent<UiPressFeedback>();

        _border = UiBuilder.CreateImage(Root, "Border", UiPalette.ButtonPrimary,
            UiSprites.TagOutline, Image.Type.Sliced);
        UiBuilder.Stretch(_border.rectTransform, 0f, 0f, 0f, 0f);
        _border.gameObject.SetActive(false);

        _slotText = UiBuilder.CreateText(Root, "Slot", 38f, UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Place(_slotText.rectTransform, 8f, 24f, 56f, 48f);

        _positionText = UiBuilder.CreateText(Root, "Position", 31f, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.Place(_positionText.rectTransform, 68f, 24f, 72f, 48f);

        _nameText = UiBuilder.CreateText(Root, "Name", 34f, UiPalette.Text1);
        UiBuilder.Place(_nameText.rectTransform, 150f, 10f, width - 260f, 46f);

        RectTransform starRow = UiBuilder.CreateRect(Root, "Stars");
        UiBuilder.Place(starRow, 150f, 54f, MaxStars * (StarSize + 4f), StarSize + 6f);

        for (int i = 0; i < MaxStars; i++)
        {
            _stars[i] = UiBuilder.CreateImage(starRow, "Star", UiPalette.CardNormal, UiSprites.IconStar);
            UiBuilder.Place(_stars[i].rectTransform, i * (StarSize + 4f), 0f, StarSize, StarSize);
            _stars[i].gameObject.SetActive(false);
        }

        _ovrText = UiBuilder.CreateText(Root, "Ovr", 46f, UiPalette.Text1, TextAlignmentOptions.Right);
        UiBuilder.Place(_ovrText.rectTransform, width - 112f, 24f, 96f, 48f);

        _stateTagText = UiBuilder.CreateText(Root, "StateTag", 26f, UiPalette.TextOff, TextAlignmentOptions.Right);
        UiBuilder.Place(_stateTagText.rectTransform, width - 112f, 60f, 96f, 30f);
        _stateTagText.gameObject.SetActive(false);
    }

    /// <summary>
    /// 행을 눌렀을 때의 동작. null이면 누를 수 없는 행이 된다
    /// </summary>
    public void SetClickHandler(Action onClick)
    {
        _onClick = onClick;
        _button.interactable = onClick != null;
    }

    /// <summary>
    /// 행의 y 위치를 목록 안에서 지정한다
    /// </summary>
    public void SetIndex(int index, float gap = 6f)
    {
        Vector2 size = Root.sizeDelta;

        UiBuilder.Place(Root, 0f, index * (RowHeight + gap), size.x, RowHeight);
    }

    /// <summary>
    /// 타자 1명. battingOrder가 0 이하면 타순 칸을 비운다
    /// </summary>
    public void SetHitter(int battingOrder, HitterSnapshot snapshot, bool isPlayerCard)
    {
        _slotText.text = battingOrder > 0 ? battingOrder.ToString() : string.Empty;
        _positionText.text = snapshot.Position;
        _nameText.text = PlayerLabelFormat.NameWithYear(snapshot.Name, snapshot.Year);

        ApplyCardVisual(snapshot.InstanceId, snapshot.OVR, isPlayerCard);
        SetState(UiRowState.Normal);
    }

    /// <summary>
    /// 투수 1명. 보직은 포지션 칸에 들어간다
    /// </summary>
    public void SetPitcher(PitcherSnapshot snapshot, bool isPlayerCard)
    {
        _slotText.text = string.Empty;
        _positionText.text = snapshot.Position;
        _nameText.text = PlayerLabelFormat.NameWithYear(snapshot.Name, snapshot.Year);

        ApplyCardVisual(snapshot.InstanceId, snapshot.OVR, isPlayerCard);
        SetState(UiRowState.Normal);
    }

    /// <summary>
    /// 빈 슬롯 표시 (벤치가 비어 있을 수 있다)
    /// </summary>
    public void SetEmpty(string label)
    {
        _slotText.text = string.Empty;
        _positionText.text = string.Empty;
        _nameText.text = label;
        _nameText.color = UiPalette.TextOff;
        _ovrText.text = string.Empty;

        HideStars();
        SetState(UiRowState.Empty);
    }

    /// <summary>
    /// 행 상태별 테두리·흐림 처리
    /// </summary>
    public void SetState(UiRowState state)
    {
        _background.sprite = UiSprites.Get(GetStateSprite(state));
        _background.color = state == UiRowState.Selected ? UiPalette.BgSelected : UiPalette.BgRow;

        _border.gameObject.SetActive(state == UiRowState.Selected || state == UiRowState.Pitching);
        _border.color = state == UiRowState.Pitching ? UiPalette.StatePitching : UiPalette.ButtonPrimary;

        float alpha = state == UiRowState.Used ? 0.5f : 1f;

        _nameText.color = UiPalette.WithAlpha(state == UiRowState.Empty ? UiPalette.TextOff : UiPalette.Text1, alpha);
        _ovrText.color = UiPalette.WithAlpha(_ovrText.color, alpha);
        _positionText.color = UiPalette.WithAlpha(UiPalette.Text2, alpha);

        _stateTagText.gameObject.SetActive(state == UiRowState.Used || state == UiRowState.Pitching);
        _stateTagText.text = state == UiRowState.Pitching ? "등판 중" : "교체 완료";
        _stateTagText.color = state == UiRowState.Pitching ? UiPalette.StatePitching : UiPalette.TextOff;
    }

    //OVR 숫자색과 별 개수를 보유 카드 정보에서 채운다
    private void ApplyCardVisual(int instanceId, int ovr, bool isPlayerCard)
    {
        _ovrText.text = ovr.ToString();

        if (!isPlayerCard || !CardVisualLookup.TryGet(instanceId, out CardType cardType, out CardGrade cardGrade))
        {
            _ovrText.color = UiPalette.Text1;
            HideStars();
            return;
        }

        Color typeColor = CardVisualLookup.GetTypeColor(cardType);

        _ovrText.color = typeColor;

        int starCount = (int)cardGrade;

        for (int i = 0; i < MaxStars; i++)
        {
            bool shown = i < starCount;

            _stars[i].gameObject.SetActive(shown);

            if (shown)
                _stars[i].color = typeColor;
        }
    }

    private void HideStars()
    {
        for (int i = 0; i < MaxStars; i++)
            _stars[i].gameObject.SetActive(false);
    }

    private void OnClicked()
    {
        _onClick?.Invoke();
    }

    private static string GetStateSprite(UiRowState state)
    {
        return state switch
        {
            UiRowState.Selected => UiSprites.RowSelected,
            UiRowState.Used => UiSprites.RowUsed,
            UiRowState.Pitching => UiSprites.RowPitching,
            UiRowState.Empty => UiSprites.RowEmpty,
            _ => UiSprites.RowNormal
        };
    }
}
