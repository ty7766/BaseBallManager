using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 경기 중 화면 최상단 스코어보드 (명세 1-3 · 6-4). 높이 264
/// </summary>
public class ScoreboardView : UiWidgetBehaviour
{
    /// <summary>
    /// 스코어보드 높이 (명세 6-5)
    /// </summary>
    public const float BoardHeight = 264f;

    private const int OutDotCount = 2;

    [SerializeField] private TextMeshProUGUI _awayNameText;
    [SerializeField] private TextMeshProUGUI _homeNameText;
    [SerializeField] private TextMeshProUGUI _awayScoreText;
    [SerializeField] private TextMeshProUGUI _homeScoreText;
    [SerializeField] private TextMeshProUGUI _inningText;
    [SerializeField] private TextMeshProUGUI _awayAttackTag;
    [SerializeField] private TextMeshProUGUI _homeAttackTag;
    [SerializeField] private TextMeshProUGUI _awayMyTeamTag;
    [SerializeField] private TextMeshProUGUI _homeMyTeamTag;
    [SerializeField] private Image[] _outDots = new Image[OutDotCount];

    private int _lastAwayScore = -1;
    private int _lastHomeScore = -1;

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static ScoreboardView Bake(Transform parent)
    {
        ScoreboardView view = CreateNode<ScoreboardView>(parent, "Scoreboard");

        view.BuildWidget(parent);

        return view;
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(Transform parent)
    {
        UiBuilder.Place(Root, 0f, 0f, UiBuilder.DesignSize.x, BoardHeight);

        Image background = UiBuilder.CreateImage(Root, "Background", UiPalette.BgPanel,
            UiSprites.PanelLarge, Image.Type.Sliced);
        UiBuilder.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

        _awayNameText = UiBuilder.CreateText(Root, "AwayName", 44f, UiPalette.Text1, TextAlignmentOptions.MidlineLeft);
        UiBuilder.Place(_awayNameText.rectTransform, 28f, 22f, 250f, 50f);

        _homeNameText = UiBuilder.CreateText(Root, "HomeName", 44f, UiPalette.Text1, TextAlignmentOptions.Right);
        UiBuilder.Place(_homeNameText.rectTransform, 802f, 22f, 250f, 50f);

        TextMeshProUGUI awayTag = UiBuilder.CreateLabel(Root, "AwayTag", "AWAY", 26f, UiPalette.Text3);
        UiBuilder.Place(awayTag.rectTransform, 28f, 74f, 120f, 30f);

        TextMeshProUGUI homeTag = UiBuilder.CreateLabel(Root, "HomeTag", "HOME", 26f, UiPalette.Text3,
            TextAlignmentOptions.Right);
        UiBuilder.Place(homeTag.rectTransform, 932f, 74f, 120f, 30f);

        _awayMyTeamTag = UiBuilder.CreateLabel(Root, "AwayMyTeam", "MY TEAM", 24f, UiPalette.AccentGold);
        UiBuilder.Place(_awayMyTeamTag.rectTransform, 150f, 74f, 180f, 30f);
        _awayMyTeamTag.gameObject.SetActive(false);

        _homeMyTeamTag = UiBuilder.CreateLabel(Root, "HomeMyTeam", "MY TEAM", 24f, UiPalette.AccentGold,
            TextAlignmentOptions.Right);
        UiBuilder.Place(_homeMyTeamTag.rectTransform, 750f, 74f, 180f, 30f);
        _homeMyTeamTag.gameObject.SetActive(false);

        _awayScoreText = UiBuilder.CreateText(Root, "AwayScore", 150f, UiPalette.Text1, TextAlignmentOptions.Right);
        UiBuilder.Place(_awayScoreText.rectTransform, 300f, 24f, 190f, 160f);

        TextMeshProUGUI dash = UiBuilder.CreateText(Root, "Dash", 70f, UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Place(dash.rectTransform, 496f, 60f, 88f, 90f);
        dash.text = "-";

        _homeScoreText = UiBuilder.CreateText(Root, "HomeScore", 150f, UiPalette.Text1, TextAlignmentOptions.MidlineLeft);
        UiBuilder.Place(_homeScoreText.rectTransform, 590f, 24f, 190f, 160f);

        _inningText = UiBuilder.CreateText(Root, "Inning", 92f, UiPalette.Text1, TextAlignmentOptions.Center);
        UiBuilder.Place(_inningText.rectTransform, 390f, 158f, 300f, 96f);

        _awayAttackTag = UiBuilder.CreateText(Root, "AwayAttack", 28f, UiPalette.AccentGold, TextAlignmentOptions.MidlineLeft);
        UiBuilder.Place(_awayAttackTag.rectTransform, 28f, 180f, 120f, 36f);
        _awayAttackTag.text = "공격";
        _awayAttackTag.gameObject.SetActive(false);

        _homeAttackTag = UiBuilder.CreateText(Root, "HomeAttack", 28f, UiPalette.AccentGold, TextAlignmentOptions.Right);
        UiBuilder.Place(_homeAttackTag.rectTransform, 932f, 180f, 120f, 36f);
        _homeAttackTag.text = "공격";
        _homeAttackTag.gameObject.SetActive(false);

        RectTransform outRow = UiBuilder.CreateRect(Root, "Outs");
        UiBuilder.Place(outRow, 760f, 190f, 160f, 40f);

        TextMeshProUGUI outLabel = UiBuilder.CreateLabel(outRow, "OutLabel", "OUT", 24f, UiPalette.Text3);
        UiBuilder.Place(outLabel.rectTransform, 0f, 6f, 70f, 28f);

        for (int i = 0; i < OutDotCount; i++)
        {
            _outDots[i] = UiBuilder.CreateImage(outRow, "OutDot", UiPalette.TextOff, UiSprites.OutDotEmpty);
            UiBuilder.Place(_outDots[i].rectTransform, 74f + i * 38f, 4f, 30f, 30f);
        }
    }

    /// <summary>
    /// 양 팀 이름과 내 팀 표식. 경기 시작 시 한 번만 호출한다
    /// </summary>
    public void SetTeams(string awayTeamName, string homeTeamName, bool isPlayerHome)
    {
        _awayNameText.text = awayTeamName;
        _homeNameText.text = homeTeamName;

        _awayMyTeamTag.gameObject.SetActive(!isPlayerHome);
        _homeMyTeamTag.gameObject.SetActive(isPlayerHome);

        _lastAwayScore = -1;
        _lastHomeScore = -1;
    }

    //점수가 오른 쪽만 튀어오르고 금색으로 번쩍인다
    private static void PlayScoreEffect(TextMeshProUGUI scoreText)
    {
        UiEffects.Pop(scoreText.transform, 0.68f, 0.34f);
        UiEffects.Flash(scoreText, UiPalette.AccentGold, 0.5f);
        UiEffects.Burst(scoreText.rectTransform, UiEffectKind.Score);
    }

    /// <summary>
    /// 점수·이닝·아웃을 현재 상태로 갱신한다
    /// </summary>
    public void Refresh(GameState state)
    {
        if (state == null)
            return;

        if (_lastAwayScore >= 0 && state.AwayScore > _lastAwayScore)
            PlayScoreEffect(_awayScoreText);

        if (_lastHomeScore >= 0 && state.HomeScore > _lastHomeScore)
            PlayScoreEffect(_homeScoreText);

        _lastAwayScore = state.AwayScore;
        _lastHomeScore = state.HomeScore;

        _awayScoreText.text = state.AwayScore.ToString();
        _homeScoreText.text = state.HomeScore.ToString();

        _awayScoreText.color = state.AwayScore >= state.HomeScore ? UiPalette.Text1 : UiPalette.Text3;
        _homeScoreText.color = state.HomeScore >= state.AwayScore ? UiPalette.Text1 : UiPalette.Text3;

        _inningText.text = PlayLogTextProvider.GetInningChipText(state.Inning, state.IsTopInning);

        _awayAttackTag.gameObject.SetActive(state.IsTopInning);
        _homeAttackTag.gameObject.SetActive(!state.IsTopInning);

        for (int i = 0; i < OutDotCount; i++)
        {
            bool filled = i < state.OutCount;

            _outDots[i].sprite = UiSprites.Get(filled ? UiSprites.OutDotFilled : UiSprites.OutDotEmpty);
            _outDots[i].color = filled ? UiPalette.StateOut : UiPalette.TextOff;
        }
    }
}
