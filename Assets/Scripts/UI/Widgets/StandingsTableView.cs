using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 리그 순위표 판. 판 폭 전체에 열을 펴고, 줄은 판 높이를 나눠 가져 긴 화면에서 함께 늘어난다
/// </summary>
public class StandingsTableView : UiWidgetBehaviour
{
    private const int RowCount = 10;
    private const float HeaderHeight = 64f;
    private const float DividerInset = 20f;
    private const float RowsBottomPadding = 8f;

    [SerializeField] private StandingsRowView[] _rows;

    /// <summary>
    /// 위젯을 조립한다. 판 크기 · 위치는 호출부가 잡는다 (프리팹 베이크 시점)
    /// </summary>
    public static StandingsTableView Bake(Transform parent, float width)
    {
        StandingsTableView view = CreateNode<StandingsTableView>(parent, "Standings");

        view.BuildWidget(width);

        return view;
    }

    /// <summary>
    /// 순위 줄을 채운다. 팀이 줄 수보다 적으면 남는 줄을 숨긴다
    /// </summary>
    public void Fill(LeagueStandingRow[] ranking, string playerTeamName)
    {
        int count = ranking == null ? 0 : ranking.Length;

        for (int i = 0; i < _rows.Length; i++)
        {
            bool used = i < count;

            _rows[i].SetVisible(used);

            if (used)
                _rows[i].Set(ranking[i], ranking[i].Record.TeamName == playerTeamName);
        }
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(float width)
    {
        Image panel = BroadcastPanel.Create(Root, "Panel");
        UiBuilder.Stretch(panel.rectTransform, 0f, 0f, 0f, 0f);

        RectTransform header = UiBuilder.PlaceRow(UiBuilder.CreateRect(panel.transform, "Header"), 0f, HeaderHeight);
        StandingsRowView.BuildHeader(header);

        Image divider = UiBuilder.CreateImage(header, "Divider", UiPalette.LineBorder);
        UiBuilder.PlaceRow(divider.rectTransform, HeaderHeight - 2f, 2f, DividerInset);

        RectTransform rows = UiBuilder.CreateRect(panel.transform, "Rows");
        UiBuilder.Stretch(rows, 0f, HeaderHeight, 0f, RowsBottomPadding);

        _rows = new StandingsRowView[RowCount];

        for (int i = 0; i < RowCount; i++)
            _rows[i] = StandingsRowView.Bake(rows, i, RowCount, width);
    }
}
