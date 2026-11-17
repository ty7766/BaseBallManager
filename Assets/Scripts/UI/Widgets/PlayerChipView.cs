using TMPro;
using UnityEngine;

/// <summary>
/// 경기 중 현재 타자 · 투수 칩 (명세 1-1 B 밀도 · 중계 그래픽 B안). 얼굴 사진 바로 옆에 OVR · 역할, 그 아래 이름 · 보조 줄.
/// 바탕은 놓인 판이 맡는다. 오른쪽 칩은 사진 · 글자를 거울처럼 뒤집어 둔다
/// </summary>
public class PlayerChipView : UiWidgetBehaviour
{
    /// <summary>
    /// 칩 높이
    /// </summary>
    public const float ChipHeight = 150f;

    private const float FaceWidth = 112f;
    private const float FaceHeight = 140f;
    private const float TextGap = 18f;
    private const float OvrWidth = 72f;
    private const float OvrGap = 8f;

    [SerializeField] private PlayerFaceView _face;
    [SerializeField] private TextMeshProUGUI _roleText;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _detailText;
    [SerializeField] private TextMeshProUGUI _ovrText;

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static PlayerChipView Bake(Transform parent, string roleLabel, float width, bool alignRight)
    {
        PlayerChipView view = CreateNode<PlayerChipView>(parent, "PlayerChip");

        view.BuildWidget(roleLabel, width, alignRight);

        return view;
    }

    /// <summary>
    /// 타자 칩. 타순을 보조 줄에 함께 표기한다
    /// </summary>
    public void SetHitter(HitterSnapshot snapshot, int battingOrder, bool isPlayerCard)
    {
        _nameText.text = snapshot.Name;
        _detailText.text = $"'{PlayerLabelFormat.Year(snapshot.Year)} · {snapshot.Position} · {battingOrder}번";
        ApplyCard(snapshot.InstanceId, snapshot.OVR, isPlayerCard);
    }

    /// <summary>
    /// 투수 칩. 보직만 보조 줄에 표기한다
    /// </summary>
    public void SetPitcher(PitcherSnapshot snapshot, bool isPlayerCard)
    {
        _nameText.text = snapshot.Name;
        _detailText.text = $"'{PlayerLabelFormat.Year(snapshot.Year)} · {snapshot.Position}";
        ApplyCard(snapshot.InstanceId, snapshot.OVR, isPlayerCard);
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(string roleLabel, float width, bool alignRight)
    {
        UiBuilder.Place(Root, 0f, 0f, width, ChipHeight);

        _face = PlayerFaceView.Bake(Root, FaceWidth, FaceHeight);

        float textLeft = FaceWidth + TextGap;
        float textWidth = width - textLeft;
        float roleLeft = textLeft + OvrWidth + OvrGap;
        TextAlignmentOptions align = alignRight ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;

        _ovrText = BroadcastPanel.CreateNumber(Root, "Ovr", 44f, UiPalette.Text1, align);
        _roleText = UiBuilder.CreateLabel(Root, "Role", roleLabel, 24f, UiPalette.Text3, align);
        _nameText = UiBuilder.CreateText(Root, "Name", 40f, UiPalette.Text1, align);
        _nameText.fontStyle = FontStyles.Bold;
        _nameText.overflowMode = TextOverflowModes.Ellipsis;
        _detailText = UiBuilder.CreateText(Root, "Detail", 27f, UiPalette.Text2, align);

        if (alignRight)
        {
            UiBuilder.PlaceRight(_face.Root, 0f, (ChipHeight - FaceHeight) * 0.5f, FaceWidth, FaceHeight);
            UiBuilder.PlaceRight(_ovrText.rectTransform, textLeft, 2f, OvrWidth, 52f);
            UiBuilder.PlaceRight(_roleText.rectTransform, roleLeft, 14f, textWidth - OvrWidth - OvrGap, 30f);
            UiBuilder.PlaceRight(_nameText.rectTransform, textLeft, 54f, textWidth, 50f);
            UiBuilder.PlaceRight(_detailText.rectTransform, textLeft, 104f, textWidth, 36f);
            return;
        }

        UiBuilder.Place(_face.Root, 0f, (ChipHeight - FaceHeight) * 0.5f, FaceWidth, FaceHeight);
        UiBuilder.Place(_ovrText.rectTransform, textLeft, 2f, OvrWidth, 52f);
        UiBuilder.Place(_roleText.rectTransform, roleLeft, 14f, textWidth - OvrWidth - OvrGap, 30f);
        UiBuilder.Place(_nameText.rectTransform, textLeft, 54f, textWidth, 50f);
        UiBuilder.Place(_detailText.rectTransform, textLeft, 104f, textWidth, 36f);
    }

    //얼굴 사진 · OVR 색. 내 카드만 카드 종류 색으로 OVR 을 칠한다
    private void ApplyCard(int snapshotId, int ovr, bool isPlayerCard)
    {
        _ovrText.text = ovr.ToString();
        _ovrText.color = UiPalette.Text1;

        if (!CardDisplayInfo.TryCreateForSnapshot(snapshotId, isPlayerCard, out CardDisplayInfo info))
        {
            _face.SetEmpty();
            return;
        }

        _face.SetCard(info);

        if (isPlayerCard)
            _ovrText.color = CardVisualLookup.GetTypeColor(info.CardType);
    }
}
