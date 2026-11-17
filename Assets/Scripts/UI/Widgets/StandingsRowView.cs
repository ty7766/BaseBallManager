using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 순위표 한 줄. 순위 · 로고 · 팀 · 승 · 패 · 무 · 승률 · 게임차. 머리줄도 같은 열 배치로 여기서 만든다
/// </summary>
public class StandingsRowView : UiWidgetBehaviour
{
    private const float SidePadding = 20f;
    private const float RankWidth = 64f;
    private const float LogoLeft = 96f;
    private const float LogoSize = 52f;
    private const float TeamLeft = 162f;
    //선형 색 공간에서 흰 저알파는 생각보다 밝게 뜬다
    private const float StripeAlpha = 0.015f;
    private const float MyTeamAlpha = 0.045f;

    private static readonly string[] NumericHeaders = { "승", "패", "무", "승률", "게임차" };
    private static readonly float[] NumericWidths = { 100f, 100f, 100f, 150f, 150f };

    [SerializeField] private Image _highlight;
    [SerializeField] private TextMeshProUGUI _rankText;
    [SerializeField] private TeamEmblemView _logo;
    [SerializeField] private TextMeshProUGUI _teamText;
    [SerializeField] private TextMeshProUGUI[] _numberTexts;

    /// <summary>
    /// 줄을 만든다. 부모 높이를 rowCount 줄로 나눈 index 번째 칸에 선다 (프리팹 베이크 시점)
    /// </summary>
    public static StandingsRowView Bake(Transform parent, int index, int rowCount, float width)
    {
        StandingsRowView view = CreateNode<StandingsRowView>(parent, "Row");

        UiBuilder.PlaceRowSlot(view.Root, index, rowCount);
        view.BuildWidget(index, width);

        return view;
    }

    /// <summary>
    /// 줄과 같은 열 배치의 머리줄 글자
    /// </summary>
    public static void BuildHeader(RectTransform header)
    {
        CreateHeaderText(header, "순위", SidePadding, RankWidth, TextAlignmentOptions.Center);
        CreateHeaderText(header, "팀", TeamLeft, 200f, TextAlignmentOptions.MidlineLeft);

        float right = SidePadding;

        for (int i = NumericHeaders.Length - 1; i >= 0; i--)
        {
            TextMeshProUGUI text = UiBuilder.CreateText(header, "Header", 26f, UiPalette.Text3, TextAlignmentOptions.Center);

            UiBuilder.PlaceMiddleRight(text.rectTransform, right, 0f, NumericWidths[i], 36f);
            text.text = NumericHeaders[i];
            right += NumericWidths[i];
        }
    }

    /// <summary>
    /// 팀 1개의 순위 · 성적을 채운다
    /// </summary>
    public void Set(LeagueStandingRow standing, bool isPlayerTeam)
    {
        TeamRecord record = standing.Record;

        _rankText.text = standing.Rank.ToString();
        _teamText.text = record.TeamName;
        _teamText.color = isPlayerTeam ? UiPalette.AccentGold : UiPalette.Text1;
        _logo.SetTeam(record.TeamName);
        _highlight.enabled = isPlayerTeam;
        _numberTexts[0].text = record.Wins.ToString();
        _numberTexts[1].text = record.Losses.ToString();
        _numberTexts[2].text = record.Draws.ToString();
        _numberTexts[3].text = record.WinRate.ToString("0.000");
        _numberTexts[4].text = standing.GamesBehind.ToString("0.0");
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(int index, float width)
    {
        Image stripe = UiBuilder.CreateImage(Root, "Stripe", UiPalette.WithAlpha(Color.white, StripeAlpha));
        UiBuilder.Stretch(stripe.rectTransform, 0f, 0f, 0f, 0f);
        stripe.enabled = index % 2 == 1;

        _highlight = UiBuilder.CreateImage(Root, "MyTeam", UiPalette.WithAlpha(Color.white, MyTeamAlpha));
        UiBuilder.Stretch(_highlight.rectTransform, 0f, 0f, 0f, 0f);
        _highlight.enabled = false;

        _rankText = BroadcastPanel.CreateNumber(Root, "Rank", 34f, UiPalette.Text2);
        UiBuilder.PlaceMiddle(_rankText.rectTransform, SidePadding, 0f, RankWidth, 44f);

        _logo = TeamEmblemView.Bake(Root, LogoSize);
        UiBuilder.PlaceMiddle(_logo.Root, LogoLeft, 0f, LogoSize, LogoSize);

        float numbersWidth = 0f;

        foreach (float columnWidth in NumericWidths)
            numbersWidth += columnWidth;

        _teamText = UiBuilder.CreateText(Root, "Team", 36f, UiPalette.Text1);
        UiBuilder.PlaceMiddle(_teamText.rectTransform, TeamLeft, 0f, width - TeamLeft - numbersWidth - SidePadding, 46f);
        _teamText.fontStyle = FontStyles.Bold;

        _numberTexts = new TextMeshProUGUI[NumericWidths.Length];

        float right = SidePadding;

        for (int i = NumericWidths.Length - 1; i >= 0; i--)
        {
            _numberTexts[i] = BroadcastPanel.CreateNumber(Root, "Number", 34f, UiPalette.Text1);
            UiBuilder.PlaceMiddleRight(_numberTexts[i].rectTransform, right, 0f, NumericWidths[i], 44f);
            right += NumericWidths[i];
        }
    }

    private static void CreateHeaderText(RectTransform header, string label, float x, float width, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text = UiBuilder.CreateText(header, "Header", 26f, UiPalette.Text3, alignment);

        UiBuilder.PlaceMiddle(text.rectTransform, x, 0f, width, 36f);
        text.text = label;
    }
}
