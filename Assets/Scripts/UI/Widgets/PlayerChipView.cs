using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 명세 1-1 B 밀도 현재 타석 칩 (120px). 2줄 표기 + 작은 OVR 라벨
/// </summary>
public class PlayerChipView : UiWidgetBehaviour
{
    /// <summary>
    /// 칩 높이 (명세 1-1)
    /// </summary>
    public const float ChipHeight = 120f;

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

        view.BuildWidget(parent, roleLabel, width, alignRight);

        return view;
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(Transform parent, string roleLabel, float width, bool alignRight)
    {
        UiBuilder.Place(Root, 0f, 0f, width, ChipHeight);

        Image background = UiBuilder.CreateImage(Root, "Background", UiPalette.BgRow,
            UiSprites.PanelSmall, Image.Type.Sliced);
        UiBuilder.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

        TextAlignmentOptions align = alignRight ? TextAlignmentOptions.Right : TextAlignmentOptions.MidlineLeft;
        float textX = alignRight ? 16f : 120f;
        float textWidth = width - 136f;

        _roleText = UiBuilder.CreateLabel(Root, "Role", roleLabel, 26f, UiPalette.Text3,
            alignRight ? TextAlignmentOptions.Right : TextAlignmentOptions.MidlineLeft);
        UiBuilder.Place(_roleText.rectTransform, textX, 8f, textWidth, 30f);

        _nameText = UiBuilder.CreateText(Root, "Name", 42f, UiPalette.Text1, align);
        UiBuilder.Place(_nameText.rectTransform, textX, 36f, textWidth, 48f);

        _detailText = UiBuilder.CreateText(Root, "Detail", 31f, UiPalette.Text2, align);
        UiBuilder.Place(_detailText.rectTransform, textX, 82f, textWidth, 32f);

        _ovrText = UiBuilder.CreateText(Root, "Ovr", 54f, UiPalette.Text1,
            alignRight ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Right);
        UiBuilder.Place(_ovrText.rectTransform, alignRight ? width - 112f : 8f, 34f, 104f, 54f);
    }

    /// <summary>
    /// 타자 칩. 타순을 보조 줄에 함께 표기한다
    /// </summary>
    public void SetHitter(HitterSnapshot snapshot, int battingOrder, bool isPlayerCard)
    {
        _nameText.text = snapshot.Name;
        _detailText.text = $"'{PlayerLabelFormat.Year(snapshot.Year)} · {snapshot.Position} · {battingOrder}번";

        ApplyOvr(snapshot.InstanceId, snapshot.OVR, isPlayerCard);
    }

    /// <summary>
    /// 투수 칩. 보직만 보조 줄에 표기한다
    /// </summary>
    public void SetPitcher(PitcherSnapshot snapshot, bool isPlayerCard)
    {
        _nameText.text = snapshot.Name;
        _detailText.text = $"'{PlayerLabelFormat.Year(snapshot.Year)} · {snapshot.Position}";

        ApplyOvr(snapshot.InstanceId, snapshot.OVR, isPlayerCard);
    }

    private void ApplyOvr(int instanceId, int ovr, bool isPlayerCard)
    {
        _ovrText.text = ovr.ToString();

        if (isPlayerCard && CardVisualLookup.TryGet(instanceId, out CardType cardType, out CardGrade _))
        {
            _ovrText.color = CardVisualLookup.GetTypeColor(cardType);
            return;
        }

        _ovrText.color = UiPalette.Text1;
    }
}
