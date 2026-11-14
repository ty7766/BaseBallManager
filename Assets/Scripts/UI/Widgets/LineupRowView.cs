using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 교체 화면 선수 한 줄 (중계 그래픽 B안). 타순 · 포지션 · 얼굴 사진 · 이름 '연도 · 별 · OVR · 상태 꼬리표.
/// 높이는 부모 줄 칸을 따라가고 내용은 세로 가운데에 선다. 고른 줄은 흰 테, 등판 중은 청록 테, 쓴 줄은 흐리게
/// </summary>
public class LineupRowView : UiWidgetBehaviour
{
    private const int MaxStars = 5;
    private const float StarSize = 22f;
    private const float StarGap = 3f;
    private const float RowInset = 4f;
    private const float FaceWidth = 72f;
    private const float FaceHeight = 90f;
    private const float NameLeft = 184f;
    private const float OvrWidth = 80f;
    private const float TagWidth = 130f;
    //선형 색 공간에서 흰 저알파는 생각보다 밝게 뜬다
    private const float NormalFillAlpha = 0.012f;
    private const float SelectedFillAlpha = 0.04f;
    private const float UsedContentAlpha = 0.4f;

    [SerializeField] private Image _background;
    [SerializeField] private Image _border;
    [SerializeField] private CanvasGroup _content;
    [SerializeField] private TextMeshProUGUI _slotText;
    [SerializeField] private TextMeshProUGUI _positionText;
    [SerializeField] private PlayerFaceView _face;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _ovrText;
    [SerializeField] private TextMeshProUGUI _stateTagText;
    [SerializeField] private Image[] _stars = new Image[MaxStars];
    [SerializeField] private Button _button;

    private Action _onClick;

    /// <summary>
    /// 위젯을 조립한다. 부모 높이를 slotCount 칸으로 나눈 index 번째 칸에 선다 (프리팹 베이크 시점)
    /// </summary>
    public static LineupRowView Bake(Transform parent, int index, int slotCount, float width)
    {
        LineupRowView view = CreateNode<LineupRowView>(parent, "LineupRow");

        UiBuilder.PlaceRowSlot(view.Root, index, slotCount);
        view.BuildWidget(width);

        return view;
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
    /// 타자 1명. battingOrder가 0 이하면 타순 칸을 비운다
    /// </summary>
    public void SetHitter(int battingOrder, HitterSnapshot snapshot, bool isPlayerCard)
    {
        _slotText.text = battingOrder > 0 ? battingOrder.ToString() : string.Empty;
        ApplyPlayer(snapshot.Position, snapshot.Name, snapshot.Year, snapshot.InstanceId, snapshot.OVR, isPlayerCard);
    }

    /// <summary>
    /// 투수 1명. 보직은 포지션 칸에 들어간다
    /// </summary>
    public void SetPitcher(PitcherSnapshot snapshot, bool isPlayerCard)
    {
        _slotText.text = string.Empty;
        ApplyPlayer(snapshot.Position, snapshot.Name, snapshot.Year, snapshot.InstanceId, snapshot.OVR, isPlayerCard);
    }

    /// <summary>
    /// 빈 슬롯 표시 (벤치가 비어 있을 수 있다)
    /// </summary>
    public void SetEmpty(string label)
    {
        _slotText.text = string.Empty;
        _positionText.text = string.Empty;
        _nameText.text = label;
        _ovrText.text = string.Empty;
        _face.SetEmpty();

        HideStars();
        SetState(UiRowState.Empty);
    }

    /// <summary>
    /// 행 상태별 테두리 · 채움 · 흐림 처리
    /// </summary>
    public void SetState(UiRowState state)
    {
        bool selected = state == UiRowState.Selected;
        bool pitching = state == UiRowState.Pitching;

        _background.color = UiPalette.WithAlpha(Color.white, selected ? SelectedFillAlpha : NormalFillAlpha);
        _border.gameObject.SetActive(selected || pitching);
        _border.color = pitching ? UiPalette.StatePitching : UiPalette.ButtonPrimary;
        _content.alpha = state == UiRowState.Used ? UsedContentAlpha : 1f;
        _nameText.color = state == UiRowState.Empty ? UiPalette.TextOff : UiPalette.Text1;

        _stateTagText.gameObject.SetActive(state == UiRowState.Used || pitching);
        _stateTagText.text = pitching ? "등판 중" : "교체 완료";
        _stateTagText.color = pitching ? UiPalette.StatePitching : UiPalette.Text3;
    }

    protected override void BindEvents()
    {
        Bind(_button, OnClicked);
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(float width)
    {
        _background = UiBuilder.CreateImage(Root, "Background", UiPalette.WithAlpha(Color.white, NormalFillAlpha),
            UiSprites.CutFillSmall, Image.Type.Sliced);
        UiBuilder.Stretch(_background.rectTransform, 0f, RowInset, 0f, RowInset);
        _background.raycastTarget = true;

        _button = Root.gameObject.AddComponent<Button>();
        _button.targetGraphic = _background;
        _button.transition = Selectable.Transition.None;
        _button.interactable = false;
        _button.onClick.AddListener(OnClicked);
        Root.gameObject.AddComponent<UiPressFeedback>();

        _border = UiBuilder.CreateImage(Root, "Border", UiPalette.ButtonPrimary, UiSprites.CutOutline, Image.Type.Sliced);
        UiBuilder.Stretch(_border.rectTransform, 0f, RowInset, 0f, RowInset);
        _border.gameObject.SetActive(false);

        RectTransform content = UiBuilder.CreateRect(Root, "Content");
        UiBuilder.Stretch(content, 0f, 0f, 0f, 0f);
        _content = content.gameObject.AddComponent<CanvasGroup>();
        _content.blocksRaycasts = false;

        _slotText = BroadcastPanel.CreateNumber(content, "Slot", 30f, UiPalette.Text3);
        UiBuilder.PlaceMiddle(_slotText.rectTransform, 6f, 0f, 36f, 44f);

        _positionText = UiBuilder.CreateText(content, "Position", 26f, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.PlaceMiddle(_positionText.rectTransform, 42f, 0f, 54f, 40f);

        _face = PlayerFaceView.Bake(content, FaceWidth, FaceHeight);
        UiBuilder.PlaceMiddle(_face.Root, 100f, 0f, FaceWidth, FaceHeight);

        float nameWidth = width - NameLeft - OvrWidth - 26f;

        _nameText = UiBuilder.CreateText(content, "Name", 34f, UiPalette.Text1);
        UiBuilder.PlaceMiddle(_nameText.rectTransform, NameLeft, 14f, nameWidth, 46f);
        _nameText.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform starRow = UiBuilder.CreateRect(content, "Stars");
        UiBuilder.PlaceMiddle(starRow, NameLeft, -22f, MaxStars * (StarSize + StarGap), StarSize);

        for (int i = 0; i < MaxStars; i++)
        {
            _stars[i] = UiBuilder.CreateImage(starRow, "Star", UiPalette.CardNormal, UiSprites.IconStar);
            UiBuilder.Place(_stars[i].rectTransform, i * (StarSize + StarGap), 0f, StarSize, StarSize);
            _stars[i].gameObject.SetActive(false);
        }

        _ovrText = BroadcastPanel.CreateNumber(content, "Ovr", 42f, UiPalette.Text1, TextAlignmentOptions.MidlineRight);
        UiBuilder.PlaceMiddleRight(_ovrText.rectTransform, 18f, 8f, OvrWidth, 50f);

        _stateTagText = UiBuilder.CreateText(Root, "StateTag", 24f, UiPalette.Text3, TextAlignmentOptions.MidlineRight);
        UiBuilder.PlaceMiddleRight(_stateTagText.rectTransform, 18f, -30f, TagWidth, 30f);
        _stateTagText.gameObject.SetActive(false);
    }

    //이름 · 얼굴 · OVR 을 채우고 기본 상태로 되돌린다. 내 카드만 별 · 카드 종류 색 OVR
    private void ApplyPlayer(string position, string name, int year, int snapshotId, int ovr, bool isPlayerCard)
    {
        _positionText.text = position;
        _nameText.text = PlayerLabelFormat.NameWithSmallYear(name, year);
        _ovrText.text = ovr.ToString();
        _ovrText.color = UiPalette.Text1;
        HideStars();

        if (CardDisplayInfo.TryCreateForSnapshot(snapshotId, isPlayerCard, out CardDisplayInfo info))
        {
            _face.SetCard(info);

            if (isPlayerCard)
                ShowCardGrade(info);
        }
        else
        {
            _face.SetEmpty();
        }

        SetState(UiRowState.Normal);
    }

    //카드 종류 색으로 별 개수 · OVR 을 칠한다
    private void ShowCardGrade(CardDisplayInfo info)
    {
        Color typeColor = CardVisualLookup.GetTypeColor(info.CardType);
        int starCount = (int)info.CardGrade;

        _ovrText.color = typeColor;

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
}
