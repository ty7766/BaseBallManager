using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 경기 전 화면 한 팀 타순 판. 팀 색이 깔린 머리줄(로고 · 팀 이름 · 원정/홈) 아래 9줄이 판 높이를 나눠 갖는다.
/// 양 팀 판을 떨어뜨려 두어 같은 타순끼리 한 줄로 붙어 읽히지 않게 한다
/// </summary>
public class MatchLineupColumnView : UiWidgetBehaviour
{
    private const float HeaderHeight = 76f;
    private const float LogoSize = 56f;
    private const float RowsBottomPadding = 6f;
    private const float RowPopDelay = 0.035f;

    [SerializeField] private Image _headerField;
    [SerializeField] private TeamEmblemView _logo;
    [SerializeField] private TextMeshProUGUI _teamText;
    [SerializeField] private MatchLineupRowView[] _rows;

    /// <summary>
    /// 판을 만든다. 위 · 아래 여백으로 세로를 잡아 긴 화면에서는 줄이 함께 늘어난다 (프리팹 베이크 시점)
    /// </summary>
    public static MatchLineupColumnView Bake(Transform parent, string name, string sideLabel,
        float x, float width, float top, float bottom)
    {
        MatchLineupColumnView view = CreateNode<MatchLineupColumnView>(parent, name);

        view.BuildWidget(sideLabel, x, width, top, bottom);

        return view;
    }

    /// <summary>
    /// 팀과 타순을 채운다
    /// </summary>
    public void Fill(string teamName, HitterSnapshot[] lineup, bool isPlayerSide)
    {
        _teamText.text = teamName;
        _logo.SetTeam(teamName);
        BroadcastPanel.PaintTeamField(_headerField, teamName);

        for (int i = 0; i < _rows.Length; i++)
        {
            if (lineup == null || i >= lineup.Length)
            {
                _rows[i].SetEmpty();
                continue;
            }

            _rows[i].SetHitter(i + 1, lineup[i], isPlayerSide);
        }
    }

    /// <summary>
    /// 줄이 위에서부터 차례로 튀어 오른다
    /// </summary>
    public void PlayEntrance(float delay)
    {
        for (int i = 0; i < _rows.Length; i++)
            UiEffects.Pop(_rows[i].Root, 0.94f, 0.22f, delay + i * RowPopDelay);
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(string sideLabel, float x, float width, float top, float bottom)
    {
        Root.anchorMin = new Vector2(0f, 0f);
        Root.anchorMax = new Vector2(0f, 1f);
        Root.pivot = new Vector2(0f, 1f);
        Root.offsetMin = new Vector2(x, bottom);
        Root.offsetMax = new Vector2(x + width, -top);

        Image panel = BroadcastPanel.Create(Root, "Panel", true);
        UiBuilder.Stretch(panel.rectTransform, 0f, 0f, 0f, 0f);

        RectTransform header = UiBuilder.PlaceRow(UiBuilder.CreateRect(panel.transform, "Header"), 0f, HeaderHeight);

        _headerField = BroadcastPanel.CreateTeamField(header, "HeaderField", true, 1f);

        _logo = TeamEmblemView.Bake(header, LogoSize);
        UiBuilder.PlaceMiddle(_logo.Root, 14f, 0f, LogoSize, LogoSize);

        _teamText = UiBuilder.CreateText(header, "Team", 32f, UiPalette.Text1);
        UiBuilder.PlaceMiddle(_teamText.rectTransform, 80f, 0f, width - 260f, 44f);
        _teamText.fontStyle = FontStyles.Bold;

        TextMeshProUGUI side = UiBuilder.CreateText(header, "Side", 24f, UiPalette.Text1, TextAlignmentOptions.Right);
        UiBuilder.PlaceMiddleRight(side.rectTransform, 22f, 0f, 160f, 36f);
        side.text = sideLabel;

        RectTransform rows = UiBuilder.CreateRect(panel.transform, "Rows");
        UiBuilder.Stretch(rows, 0f, HeaderHeight, 0f, RowsBottomPadding);

        _rows = new MatchLineupRowView[SimulationContext.LineupSize];

        for (int i = 0; i < _rows.Length; i++)
        {
            _rows[i] = MatchLineupRowView.Bake(rows, width, i % 2 == 1);
            UiBuilder.PlaceRowSlot(_rows[i].Root, i, _rows.Length);
        }
    }
}
