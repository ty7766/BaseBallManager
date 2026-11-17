using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 경기 중 화면 최상단 스코어보드 (명세 1-3 · 6-4 · 중계 그래픽 B안).
/// 양 끝 팀 색 면 · 반쯤 걸친 엠블럼 위에 로고, 팀 이름은 아래 귀퉁이, 가운데 이사만루 점수 아래 회색 이닝 칩 · 아웃
/// </summary>
public class ScoreboardView : UiWidgetBehaviour
{
    /// <summary>
    /// 스코어보드 크기 (화면 좌우 여백 24 안쪽)
    /// </summary>
    public const float BoardWidth = 1032f;
    public const float BoardHeight = 270f;

    private const int OutDotCount = 2;
    private const float LogoSize = 96f;
    private const float EdgeInset = 30f;
    private const float FieldRatio = 0.5f;
    private const float InningChipWidth = 150f;
    private const float ScoreFontSize = 150f;
    private const float ScoreTop = 14f;
    private const float ScoreHeight = 160f;

    [SerializeField] private Image _awayField;
    [SerializeField] private Image _homeField;
    [SerializeField] private Image _awayEdgeEmblem;
    [SerializeField] private Image _homeEdgeEmblem;
    [SerializeField] private TeamEmblemView _awayLogo;
    [SerializeField] private TeamEmblemView _homeLogo;
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

        view.BuildWidget();

        return view;
    }

    /// <summary>
    /// 양 팀 이름 · 로고 · 팀 색과 내 팀 표식. 경기 시작 시 한 번만 호출한다
    /// </summary>
    public void SetTeams(string awayTeamName, string homeTeamName, bool isPlayerHome)
    {
        _awayNameText.text = awayTeamName;
        _homeNameText.text = homeTeamName;
        _awayLogo.SetTeam(awayTeamName);
        _homeLogo.SetTeam(homeTeamName);
        BroadcastPanel.PaintTeamField(_awayField, awayTeamName);
        BroadcastPanel.PaintTeamField(_homeField, homeTeamName);
        BroadcastPanel.PlaceEdgeEmblem(_awayEdgeEmblem, awayTeamName, true);
        BroadcastPanel.PlaceEdgeEmblem(_homeEdgeEmblem, homeTeamName, false);
        _awayMyTeamTag.gameObject.SetActive(!isPlayerHome);
        _homeMyTeamTag.gameObject.SetActive(isPlayerHome);

        _lastAwayScore = -1;
        _lastHomeScore = -1;
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

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget()
    {
        UiBuilder.Place(Root, 0f, 0f, BoardWidth, BoardHeight);

        Image panel = BroadcastPanel.Create(Root, "Panel", true);
        RectTransform rect = panel.rectTransform;

        UiBuilder.Stretch(rect, 0f, 0f, 0f, 0f);

        _awayField = BroadcastPanel.CreateTeamField(rect, "AwayField", true, FieldRatio);
        _homeField = BroadcastPanel.CreateTeamField(rect, "HomeField", false, FieldRatio);
        _awayEdgeEmblem = BroadcastPanel.CreateEdgeEmblem(rect, "AwayEdgeEmblem");
        _homeEdgeEmblem = BroadcastPanel.CreateEdgeEmblem(rect, "HomeEdgeEmblem");

        BuildTeamSide(rect, true);
        BuildTeamSide(rect, false);

        float center = BoardWidth * 0.5f;

        _awayScoreText = BroadcastPanel.CreateNumber(rect, "AwayScore", ScoreFontSize, UiPalette.Text1, TextAlignmentOptions.MidlineRight);
        UiBuilder.Place(_awayScoreText.rectTransform, center - 220f, ScoreTop, 190f, ScoreHeight);

        TextMeshProUGUI colon = BroadcastPanel.CreateNumber(rect, "Colon", 70f, UiPalette.Text3, TextAlignmentOptions.Midline);
        UiBuilder.Place(colon.rectTransform, center - 30f, ScoreTop, 60f, ScoreHeight);
        colon.text = ":";

        _homeScoreText = BroadcastPanel.CreateNumber(rect, "HomeScore", ScoreFontSize, UiPalette.Text1, TextAlignmentOptions.MidlineLeft);
        UiBuilder.Place(_homeScoreText.rectTransform, center + 30f, ScoreTop, 190f, ScoreHeight);

        Image inningChip = UiBuilder.CreateImage(rect, "InningChip", UiPalette.LineBorder, UiSprites.CutFillSmall, Image.Type.Sliced);
        UiBuilder.Place(inningChip.rectTransform, center - InningChipWidth * 0.5f, 186f, InningChipWidth, 58f);

        _inningText = UiBuilder.CreateText(inningChip.transform, "Inning", 36f, UiPalette.Text1, TextAlignmentOptions.Center);
        UiBuilder.Stretch(_inningText.rectTransform, 0f, 0f, 0f, 0f);

        RectTransform outRow = UiBuilder.CreateRect(rect, "Outs");
        UiBuilder.Place(outRow, center + InningChipWidth * 0.5f + 22f, 196f, 160f, 40f);

        TextMeshProUGUI outLabel = UiBuilder.CreateLabel(outRow, "OutLabel", "OUT", 22f, UiPalette.Text2);
        UiBuilder.Place(outLabel.rectTransform, 0f, 6f, 64f, 28f);

        for (int i = 0; i < OutDotCount; i++)
        {
            _outDots[i] = UiBuilder.CreateImage(outRow, "OutDot", UiPalette.TextOff, UiSprites.OutDotEmpty);
            UiBuilder.Place(_outDots[i].rectTransform, 68f + i * 38f, 4f, 30f, 30f);
        }
    }

    //한 팀 쪽: 위 로고 + 공격 표시, 아래 귀퉁이 AWAY/HOME · MY TEAM · 팀 이름
    private void BuildTeamSide(RectTransform panel, bool isAway)
    {
        TextAlignmentOptions align = isAway ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.MidlineRight;

        TeamEmblemView logo = TeamEmblemView.Bake(panel, LogoSize);
        TextMeshProUGUI attack = UiBuilder.CreateText(panel, isAway ? "AwayAttack" : "HomeAttack", 28f, UiPalette.AccentGold, align);
        TextMeshProUGUI tag = UiBuilder.CreateLabel(panel, isAway ? "AwayTag" : "HomeTag", isAway ? "AWAY" : "HOME", 22f, UiPalette.Text2, align);
        TextMeshProUGUI myTeam = UiBuilder.CreateLabel(panel, isAway ? "AwayMyTeam" : "HomeMyTeam", "MY TEAM", 22f, UiPalette.AccentGold, align);
        TextMeshProUGUI teamName = UiBuilder.CreateText(panel, isAway ? "AwayName" : "HomeName", 46f, UiPalette.Text1, align);

        attack.text = "공격";
        attack.fontStyle = FontStyles.Bold;
        attack.gameObject.SetActive(false);
        myTeam.gameObject.SetActive(false);
        teamName.fontStyle = FontStyles.Bold;

        if (isAway)
        {
            UiBuilder.Place(logo.Root, EdgeInset, 22f, LogoSize, LogoSize);
            UiBuilder.Place(attack.rectTransform, EdgeInset + LogoSize + 14f, 50f, 100f, 40f);
            UiBuilder.Place(tag.rectTransform, EdgeInset + 2f, 140f, 96f, 28f);
            UiBuilder.Place(myTeam.rectTransform, EdgeInset + 98f, 140f, 160f, 28f);
            UiBuilder.Place(teamName.rectTransform, EdgeInset, 172f, 250f, 60f);
        }
        else
        {
            UiBuilder.PlaceRight(logo.Root, EdgeInset, 22f, LogoSize, LogoSize);
            UiBuilder.PlaceRight(attack.rectTransform, EdgeInset + LogoSize + 14f, 50f, 100f, 40f);
            UiBuilder.PlaceRight(tag.rectTransform, EdgeInset + 2f, 140f, 96f, 28f);
            UiBuilder.PlaceRight(myTeam.rectTransform, EdgeInset + 98f, 140f, 160f, 28f);
            UiBuilder.PlaceRight(teamName.rectTransform, EdgeInset, 172f, 250f, 60f);
        }

        if (isAway)
        {
            _awayLogo = logo;
            _awayAttackTag = attack;
            _awayMyTeamTag = myTeam;
            _awayNameText = teamName;
            return;
        }

        _homeLogo = logo;
        _homeAttackTag = attack;
        _homeMyTeamTag = myTeam;
        _homeNameText = teamName;
    }

    //점수가 오른 쪽만 튀어오르고 금색으로 번쩍이며 빛이 터진다
    private static void PlayScoreEffect(TextMeshProUGUI scoreText)
    {
        UiEffects.Pop(scoreText.transform, 0.84f, 0.26f);
        UiEffects.Flash(scoreText, UiPalette.AccentGold, 0.4f);
        UiEffects.Burst(scoreText.rectTransform, UiEffectKind.Score);
    }
}
