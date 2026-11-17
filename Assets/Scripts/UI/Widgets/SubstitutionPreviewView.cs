using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 교체 미리보기 판 (중계 그래픽 B안). 왼쪽 나가는 선수(붉은 면) · 가운데 교체 화살표 · 오른쪽 들어오는 선수(청록 면).
/// 아직 고르지 않은 쪽은 얼굴을 비우고 안내 문구를 띄운다
/// </summary>
public class SubstitutionPreviewView : UiWidgetBehaviour
{
    private const int OutgoingSide = 0;
    private const int IncomingSide = 1;
    private const float FieldRatio = 0.42f;
    private const float FieldAlpha = 0.34f;
    private const float SideInset = 28f;
    private const float FaceWidth = 128f;
    private const float FaceHeight = 160f;
    private const float TextGap = 22f;
    private const float ArrowSize = 76f;
    private const float NameFontSize = 50f;
    private const float HintFontSize = 28f;

    [SerializeField] private PlayerFaceView[] _faces = new PlayerFaceView[2];
    [SerializeField] private TextMeshProUGUI[] _nameTexts = new TextMeshProUGUI[2];
    [SerializeField] private TextMeshProUGUI[] _detailTexts = new TextMeshProUGUI[2];

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static SubstitutionPreviewView Bake(Transform parent, float width, float height)
    {
        SubstitutionPreviewView view = CreateNode<SubstitutionPreviewView>(parent, "SubstitutionPreview");

        view.BuildWidget(width, height);

        return view;
    }

    /// <summary>
    /// 한쪽에 선수를 채운다. detail 은 이름 아래 보조 줄 (타순 · 포지션 · OVR)
    /// </summary>
    public void SetPlayer(bool isOutgoing, int snapshotId, string name, int year, string detail)
    {
        int side = isOutgoing ? OutgoingSide : IncomingSide;
        TextMeshProUGUI nameText = _nameTexts[side];

        nameText.text = PlayerLabelFormat.NameWithSmallYear(name, year);
        nameText.fontSize = NameFontSize;
        nameText.color = UiPalette.Text1;
        _detailTexts[side].text = detail;

        if (CardDisplayInfo.TryCreateForSnapshot(snapshotId, true, out CardDisplayInfo info))
            _faces[side].SetCard(info);
        else
            _faces[side].SetEmpty();
    }

    /// <summary>
    /// 한쪽을 비우고 무엇을 고를지 안내한다
    /// </summary>
    public void SetHint(bool isOutgoing, string hint)
    {
        int side = isOutgoing ? OutgoingSide : IncomingSide;
        TextMeshProUGUI nameText = _nameTexts[side];

        nameText.text = hint;
        nameText.fontSize = HintFontSize;
        nameText.color = UiPalette.Text3;
        _detailTexts[side].text = string.Empty;
        _faces[side].SetEmpty();
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(float width, float height)
    {
        UiBuilder.Place(Root, 0f, 0f, width, height);

        RectTransform panel = BroadcastPanel.Create(Root, "Panel", true).rectTransform;
        UiBuilder.Stretch(panel, 0f, 0f, 0f, 0f);

        Image outField = BroadcastPanel.CreateTeamField(panel, "OutField", true, FieldRatio);
        outField.color = UiPalette.WithAlpha(UiPalette.StateOut, FieldAlpha);

        Image inField = BroadcastPanel.CreateTeamField(panel, "InField", false, FieldRatio);
        inField.color = UiPalette.WithAlpha(UiPalette.StatePitching, FieldAlpha);

        Image arrow = UiBuilder.CreateImage(panel, "Arrow", UiPalette.AccentGold, UiSprites.IconSwap);
        UiBuilder.Place(arrow.rectTransform, (width - ArrowSize) * 0.5f, (height - ArrowSize) * 0.5f, ArrowSize, ArrowSize);
        arrow.preserveAspect = true;

        float textWidth = width * 0.5f - SideInset - FaceWidth - TextGap - ArrowSize * 0.5f - 12f;

        BuildSide(panel, OutgoingSide, "OUT · 나가는 선수", UiPalette.StateOut, height, textWidth);
        BuildSide(panel, IncomingSide, "IN · 들어오는 선수", UiPalette.StatePitching, height, textWidth);
    }

    //한쪽: 바깥 끝 얼굴, 안쪽으로 꼬리표 · 이름 · 보조 줄. 들어오는 쪽은 거울처럼 뒤집는다
    private void BuildSide(RectTransform panel, int side, string tag, Color tagColor, float height, float textWidth)
    {
        bool mirrored = side == IncomingSide;
        float textLeft = SideInset + FaceWidth + TextGap;
        TextAlignmentOptions align = mirrored ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;

        PlayerFaceView face = PlayerFaceView.Bake(panel, FaceWidth, FaceHeight);
        TextMeshProUGUI tagText = UiBuilder.CreateLabel(panel, "Tag", tag, 24f, tagColor, align);
        TextMeshProUGUI nameText = UiBuilder.CreateText(panel, "Name", NameFontSize, UiPalette.Text1, align);
        TextMeshProUGUI detailText = UiBuilder.CreateText(panel, "Detail", 30f, UiPalette.Text2, align);

        tagText.fontStyle = FontStyles.Bold;
        nameText.overflowMode = TextOverflowModes.Ellipsis;

        float faceTop = (height - FaceHeight) * 0.5f;

        if (mirrored)
        {
            UiBuilder.PlaceRight(face.Root, SideInset, faceTop, FaceWidth, FaceHeight);
            UiBuilder.PlaceRight(tagText.rectTransform, textLeft, faceTop + 6f, textWidth, 30f);
            UiBuilder.PlaceRight(nameText.rectTransform, textLeft, faceTop + 42f, textWidth, 64f);
            UiBuilder.PlaceRight(detailText.rectTransform, textLeft, faceTop + 112f, textWidth, 40f);
        }
        else
        {
            UiBuilder.Place(face.Root, SideInset, faceTop, FaceWidth, FaceHeight);
            UiBuilder.Place(tagText.rectTransform, textLeft, faceTop + 6f, textWidth, 30f);
            UiBuilder.Place(nameText.rectTransform, textLeft, faceTop + 42f, textWidth, 64f);
            UiBuilder.Place(detailText.rectTransform, textLeft, faceTop + 112f, textWidth, 40f);
        }

        _faces[side] = face;
        _nameTexts[side] = nameText;
        _detailTexts[side] = detailText;
    }
}
