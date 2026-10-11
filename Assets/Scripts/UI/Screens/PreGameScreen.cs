using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ① 경기 전 화면. 대진 · 상대전적 · 선발 · 양 팀 타순을 보여주고 경기를 시작한다 (명세 ①)
/// </summary>
public class PreGameScreen : UiScreen
{
    private const float LineupColumnWidth = 520f;
    private const float LineupTop = 540f;
    private const float RowGap = 6f;

    private readonly Action _onStart;

    private TextMeshProUGUI _tierText;
    private TextMeshProUGUI _progressText;
    private TextMeshProUGUI _awayNameText;
    private TextMeshProUGUI _homeNameText;
    private TextMeshProUGUI _headToHeadText;
    private TextMeshProUGUI _awayMyTeamTag;
    private TextMeshProUGUI _homeMyTeamTag;
    private TeamEmblemView _awayEmblem;
    private TeamEmblemView _homeEmblem;
    private PlayerChipView _awayStarterChip;
    private PlayerChipView _homeStarterChip;
    private LineupRowView[] _awayRows;
    private LineupRowView[] _homeRows;

    public PreGameScreen(Action onStart)
    {
        _onStart = onStart;
    }

    protected override string ScreenName => "PreGameScreen";

    /// <summary>
    /// 오늘 경기 정보를 채운다. 경기 시작 직전에 호출한다
    /// </summary>
    public void SetGame(LeagueSeason season, LeagueGame game, SimulationContext context, string playerTeamName)
    {
        if (season == null || context == null)
        {
            Debug.LogError("[PreGameScreen]: 시즌 또는 시뮬 입력이 없어 화면을 채울 수 없습니다");
            return;
        }

        _tierText.text = $"{LeagueTierNames.Get(season.Tier)} 리그";
        _progressText.text = $"{season.CurrentDayIndex + 1} / {season.TotalDayCount}";

        _awayNameText.text = game.AwayTeamName;
        _homeNameText.text = game.HomeTeamName;
        _awayEmblem.SetTeam(game.AwayTeamName);
        _homeEmblem.SetTeam(game.HomeTeamName);

        bool isPlayerHome = game.HomeTeamName == playerTeamName;

        _awayMyTeamTag.gameObject.SetActive(!isPlayerHome);
        _homeMyTeamTag.gameObject.SetActive(isPlayerHome);

        _headToHeadText.text = BuildHeadToHeadText(season, game, playerTeamName);

        _awayStarterChip.SetPitcher(context.AwayPitchers[SimulationContext.StartingPitcherSlot], !isPlayerHome);
        _homeStarterChip.SetPitcher(context.HomePitchers[SimulationContext.StartingPitcherSlot], isPlayerHome);

        FillLineup(_awayRows, context.AwayLineup, !isPlayerHome);
        FillLineup(_homeRows, context.HomeLineup, isPlayerHome);

        PlayEntranceEffect();
    }

    //양 팀 심볼이 튀어오르고 라인업이 위에서부터 차례로 뜬다
    private void PlayEntranceEffect()
    {
        UiEffects.Pop(_awayEmblem.Root, 0.6f, 0.34f);
        UiEffects.Pop(_homeEmblem.Root, 0.6f, 0.34f, 0.08f);

        for (int i = 0; i < _awayRows.Length; i++)
        {
            float delay = i * 0.035f;

            UiEffects.Pop(_awayRows[i].Root, 0.94f, 0.22f, delay);
            UiEffects.Pop(_homeRows[i].Root, 0.94f, 0.22f, delay);
        }
    }

    protected override void Build()
    {
        _tierText = UiBuilder.CreateText(Root, "Tier", 48f, UiPalette.Text1);
        UiBuilder.Place(_tierText.rectTransform, 32f, 36f, 600f, 56f);

        _progressText = UiBuilder.CreateText(Root, "Progress", 36f, UiPalette.Text2, TextAlignmentOptions.Right);
        UiBuilder.PlaceRight(_progressText.rectTransform, 32f, 44f, 300f, 44f);

        Image matchPanel = UiBuilder.CreateImage(Root, "MatchPanel", UiPalette.BgPanel,
            UiSprites.PanelLarge, Image.Type.Sliced);
        UiBuilder.Place(matchPanel.rectTransform, 24f, 116f, 1032f, 230f);

        _awayEmblem = new TeamEmblemView(matchPanel.transform, 120f);
        UiBuilder.Place(_awayEmblem.Root, 40f, 28f, 120f, 120f);

        _homeEmblem = new TeamEmblemView(matchPanel.transform, 120f);
        UiBuilder.Place(_homeEmblem.Root, 872f, 28f, 120f, 120f);

        _awayNameText = UiBuilder.CreateText(matchPanel.transform, "AwayName", 44f, UiPalette.Text1,
            TextAlignmentOptions.Center);
        UiBuilder.Place(_awayNameText.rectTransform, 20f, 152f, 160f, 50f);

        _homeNameText = UiBuilder.CreateText(matchPanel.transform, "HomeName", 44f, UiPalette.Text1,
            TextAlignmentOptions.Center);
        UiBuilder.Place(_homeNameText.rectTransform, 852f, 152f, 160f, 50f);

        TextMeshProUGUI awayTag = UiBuilder.CreateLabel(matchPanel.transform, "AwayTag", "AWAY", 26f,
            UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Place(awayTag.rectTransform, 20f, 2f, 160f, 30f);

        TextMeshProUGUI homeTag = UiBuilder.CreateLabel(matchPanel.transform, "HomeTag", "HOME", 26f,
            UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Place(homeTag.rectTransform, 852f, 2f, 160f, 30f);

        _awayMyTeamTag = UiBuilder.CreateLabel(matchPanel.transform, "AwayMyTeam", "MY TEAM", 24f,
            UiPalette.AccentGold, TextAlignmentOptions.Center);
        UiBuilder.Place(_awayMyTeamTag.rectTransform, 20f, 206f, 160f, 26f);

        _homeMyTeamTag = UiBuilder.CreateLabel(matchPanel.transform, "HomeMyTeam", "MY TEAM", 24f,
            UiPalette.AccentGold, TextAlignmentOptions.Center);
        UiBuilder.Place(_homeMyTeamTag.rectTransform, 852f, 206f, 160f, 26f);

        TextMeshProUGUI versus = UiBuilder.CreateLabel(matchPanel.transform, "Versus", "VS", 60f,
            UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Place(versus.rectTransform, 416f, 40f, 200f, 70f);

        _headToHeadText = UiBuilder.CreateText(matchPanel.transform, "HeadToHead", 34f, UiPalette.Text2,
            TextAlignmentOptions.Center);
        UiBuilder.Place(_headToHeadText.rectTransform, 316f, 128f, 400f, 44f);

        _awayStarterChip = new PlayerChipView(Root, "선발", LineupColumnWidth, false);
        UiBuilder.Place(_awayStarterChip.Root, 24f, 376f, LineupColumnWidth, PlayerChipView.ChipHeight);

        _homeStarterChip = new PlayerChipView(Root, "선발", LineupColumnWidth, true);
        UiBuilder.Place(_homeStarterChip.Root, 536f, 376f, LineupColumnWidth, PlayerChipView.ChipHeight);

        RectTransform awayColumn = UiBuilder.CreateRect(Root, "AwayLineup");
        UiBuilder.Place(awayColumn, 24f, LineupTop, LineupColumnWidth, SimulationContext.LineupSize * (LineupRowView.RowHeight + RowGap));

        RectTransform homeColumn = UiBuilder.CreateRect(Root, "HomeLineup");
        UiBuilder.Place(homeColumn, 536f, LineupTop, LineupColumnWidth, SimulationContext.LineupSize * (LineupRowView.RowHeight + RowGap));

        _awayRows = CreateLineupRows(awayColumn);
        _homeRows = CreateLineupRows(homeColumn);

        Button startButton = UiBuilder.CreateButton(Root, "StartButton", "경기 시작", 44f, true, OnStartClicked);
        RectTransform startRect = startButton.GetComponent<RectTransform>();

        startRect.anchorMin = new Vector2(0f, 0f);
        startRect.anchorMax = new Vector2(1f, 0f);
        startRect.pivot = new Vector2(0.5f, 0f);
        startRect.offsetMin = new Vector2(24f, 32f);
        startRect.offsetMax = new Vector2(-24f, 152f);
    }

    private LineupRowView[] CreateLineupRows(RectTransform column)
    {
        LineupRowView[] rows = new LineupRowView[SimulationContext.LineupSize];

        for (int i = 0; i < rows.Length; i++)
        {
            rows[i] = new LineupRowView(column, LineupColumnWidth);
            rows[i].SetIndex(i, RowGap);
        }

        return rows;
    }

    private static void FillLineup(LineupRowView[] rows, HitterSnapshot[] lineup, bool isPlayerSide)
    {
        for (int i = 0; i < rows.Length; i++)
        {
            if (i >= lineup.Length)
            {
                rows[i].SetEmpty("—");
                continue;
            }

            rows[i].SetHitter(i + 1, lineup[i], isPlayerSide);
        }
    }

    //이번 리그에서 이 상대에게 거둔 전적
    private static string BuildHeadToHeadText(LeagueSeason season, LeagueGame game, string playerTeamName)
    {
        TeamRecord record = season.Standings.GetRecord(playerTeamName);

        if (record == null)
            return string.Empty;

        string opponent = game.GetOpponent(playerTeamName);

        if (string.IsNullOrEmpty(opponent))
            return string.Empty;

        return $"상대전적 {record.GetWinsAgainst(opponent)}승 {record.GetLossesAgainst(opponent)}패";
    }

    private void OnStartClicked()
    {
        _onStart?.Invoke();
    }
}
