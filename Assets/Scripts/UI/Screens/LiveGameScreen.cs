using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ② 경기 중 화면. 타석 결과가 한 줄씩 흐르고 조작은 일시정지뿐이다 (명세 ② · 6-5)
/// </summary>
public class LiveGameScreen : UiScreenBehaviour
{
    private const float MatchupHeight = 196f;
    private const float FooterHeight = 120f;
    private const float ChipWidth = 420f;
    private const float DiamondSize = 160f;

    private Action _onPauseRequested;

    [SerializeField] private ScoreboardView _scoreboard;
    [SerializeField] private BaseDiamondView _diamond;
    [SerializeField] private PlayerChipView _batterChip;
    [SerializeField] private PlayerChipView _pitcherChip;
    [SerializeField] private StaminaBarView _staminaBar;
    [SerializeField] private PlayLogView _playLog;
    [SerializeField] private Button _pauseButton;
    [SerializeField] private Image _pauseIcon;
    private UiTween _pauseSpinTween;
    [SerializeField] private TextMeshProUGUI _pauseLabel;
    [SerializeField] private TextMeshProUGUI _pauseHintText;
    [SerializeField] private TextMeshProUGUI _modeText;

    private GameSession _session;
    private bool _isPlayerHome;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action onPauseRequested)
    {
        _onPauseRequested = onPauseRequested;
    }

    /// <summary>
    /// 관전할 경기를 연결하고 중계를 비운다
    /// </summary>
    public void BeginGame(GameSession session, string awayTeamName, string homeTeamName, bool isPlayerHome)
    {
        if (session == null)
        {
            Debug.LogError("[LiveGameScreen]: 관전할 경기가 없습니다");
            return;
        }

        _session = session;
        _isPlayerHome = isPlayerHome;

        _scoreboard.SetTeams(awayTeamName, homeTeamName, isPlayerHome);
        _playLog.SetTeams(awayTeamName, homeTeamName);
        _playLog.Clear();

        Refresh();
    }

    /// <summary>
    /// 스코어보드 · 주자 · 현재 대결을 현재 상태로 갱신한다
    /// </summary>
    public void Refresh()
    {
        if (_session == null)
            return;

        GameState state = _session.State;
        SimulationContext context = _session.Context;

        _scoreboard.Refresh(state);
        _diamond.Refresh(state);

        bool isAwayAttacking = state.IsTopInning;
        bool batterIsPlayerCard = isAwayAttacking != _isPlayerHome;

        HitterSnapshot[] battingLineup = isAwayAttacking ? context.AwayLineup : context.HomeLineup;
        int battingIndex = isAwayAttacking ? state.AwayBattingIndex : state.HomeBattingIndex;

        if (battingIndex >= 0 && battingIndex < battingLineup.Length)
            _batterChip.SetHitter(battingLineup[battingIndex], battingIndex + 1, batterIsPlayerCard);

        PitcherState pitcherState = isAwayAttacking ? state.HomePitcherState : state.AwayPitcherState;

        if (pitcherState != null)
            _pitcherChip.SetPitcher(pitcherState.Snapshot, !batterIsPlayerCard);

        _staminaBar.Refresh(pitcherState);
    }

    /// <summary>
    /// 타석 로그 1건을 중계에 추가한다
    /// </summary>
    public void AppendBatterLog(SimulationBatterLog log)
    {
        _playLog.AppendBatterLog(log);
    }

    /// <summary>
    /// 도루 로그 1건을 중계에 추가한다
    /// </summary>
    public void AppendStealLog(SimulationStealLog log)
    {
        _playLog.AppendStealLog(log);

        if (log != null)
            _diamond.PlaySteal(log.FromBase, log.IsSuccess);
    }

    /// <summary>
    /// 일시정지 버튼 3상태 (명세 6-4). 자동 모드면 비활성이다
    /// </summary>
    public void SetPauseState(bool isManualMode, bool isPauseRequested)
    {
        _pauseButton.interactable = isManualMode && !isPauseRequested;
        _modeText.text = isManualMode ? "수동 교체" : "자동 교체";

        UiAnimator.Cancel(_pauseSpinTween);
        _pauseSpinTween = null;
        _pauseIcon.gameObject.SetActive(isPauseRequested);

        if (isPauseRequested)
            _pauseSpinTween = UiEffects.SpinForever(_pauseIcon, "Common/spinner_");

        if (!isManualMode)
        {
            _pauseLabel.text = "일시정지";
            _pauseLabel.color = UiPalette.TextOff;
            _pauseHintText.text = "자동 교체 모드에서는 멈출 수 없습니다";
            _pauseHintText.color = UiPalette.TextOff;
            return;
        }

        if (isPauseRequested)
        {
            _pauseLabel.text = "예약됨";
            _pauseLabel.color = UiPalette.StateReserved;
            _pauseHintText.text = "진행 중인 타석이 끝나면 멈춥니다";
            _pauseHintText.color = UiPalette.StateReserved;
            return;
        }

        _pauseLabel.text = "일시정지";
        _pauseLabel.color = UiPalette.Text1;
        _pauseHintText.text = string.Empty;
    }

    protected override void BindEvents()
    {
        Bind(_pauseButton, OnPauseClicked);
    }

    protected override void Build()
    {
        _scoreboard = ScoreboardView.Bake(Root);

        RectTransform matchup = UiBuilder.CreateRect(Root, "Matchup");
        UiBuilder.Place(matchup, 0f, ScoreboardView.BoardHeight, UiBuilder.DesignSize.x, MatchupHeight);

        _batterChip = PlayerChipView.Bake(matchup, "타자", ChipWidth, false);
        UiBuilder.Place(_batterChip.Root, 24f, 8f, ChipWidth, PlayerChipView.ChipHeight);

        _pitcherChip = PlayerChipView.Bake(matchup, "투수", ChipWidth, true);
        UiBuilder.Place(_pitcherChip.Root, 636f, 8f, ChipWidth, PlayerChipView.ChipHeight);

        _diamond = BaseDiamondView.Bake(matchup, DiamondSize);
        UiBuilder.Place(_diamond.Root, 460f, 14f, DiamondSize, DiamondSize);

        _staminaBar = StaminaBarView.Bake(matchup, ChipWidth, 22f);
        UiBuilder.Place(_staminaBar.Root, 636f, 142f, ChipWidth, 22f);

        _playLog = PlayLogView.Bake(Root);
        UiBuilder.Stretch(_playLog.Root, 24f, ScoreboardView.BoardHeight + MatchupHeight + 12f, 24f, FooterHeight + 24f);

        RectTransform footer = UiBuilder.CreateRect(Root, "Footer");
        footer.anchorMin = new Vector2(0f, 0f);
        footer.anchorMax = new Vector2(1f, 0f);
        footer.pivot = new Vector2(0.5f, 0f);
        footer.offsetMin = new Vector2(24f, 16f);
        footer.offsetMax = new Vector2(-24f, 16f + FooterHeight);

        _modeText = UiBuilder.CreateText(footer, "Mode", 30f, UiPalette.Text2);
        UiBuilder.Place(_modeText.rectTransform, 8f, 10f, 300f, 40f);

        _pauseHintText = UiBuilder.CreateText(footer, "PauseHint", 26f, UiPalette.StateReserved);
        UiBuilder.Place(_pauseHintText.rectTransform, 8f, 54f, 640f, 36f);

        _pauseButton = UiBuilder.CreateButton(footer, "PauseButton", "일시정지", 38f, false,
            OnPauseClicked, out _pauseLabel);
        UiBuilder.PlaceRight(_pauseButton.GetComponent<RectTransform>(), 0f, 8f, 320f, 104f);

        _pauseIcon = UiBuilder.CreateImage(_pauseButton.transform, "PauseIcon",
            UiPalette.StateReserved, UiSprites.Spinner);
        UiBuilder.Place(_pauseIcon.rectTransform, 16f, 30f, 44f, 44f);
        _pauseIcon.gameObject.SetActive(false);
    }

    private void OnPauseClicked()
    {
        _onPauseRequested?.Invoke();
    }
}
