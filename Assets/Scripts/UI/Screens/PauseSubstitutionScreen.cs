using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ④ 일시정지 화면. 공격 중이면 대타, 수비 중이면 투수를 교체한다 (명세 ④)
/// </summary>
public class PauseSubstitutionScreen : UiScreenBehaviour
{
    private const float ColumnWidth = 500f;
    private const float ListTop = 300f;
    private const float RowGap = 6f;
    private const int PitcherOptionCount = SimulationContext.PitcherSlotCount - 1;

    private Action _onApply;
    private Action _onResume;

    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _situationText;
    [SerializeField] private TextMeshProUGUI _previewText;
    [SerializeField] private TextMeshProUGUI _leftHeaderText;
    [SerializeField] private TextMeshProUGUI _rightHeaderText;
    [SerializeField] private LineupRowView[] _orderRows;
    [SerializeField] private LineupRowView[] _benchRows;
    [SerializeField] private LineupRowView[] _pitcherRows;
    [SerializeField] private Button _applyButton;

    private LiveGameController _controller;
    private GameSession _session;
    private bool _isPlayerHome;
    private bool _isPlayerAttacking;
    private int _selectedBenchIndex = -1;
    private int _selectedOrderIndex = -1;
    private int _selectedPitcherSlot = LiveGameController.NoPitcherSubstitution;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action onApply, Action onResume)
    {
        _onApply = onApply;
        _onResume = onResume;
    }

    /// <summary>
    /// 정지 시점의 경기 상태로 화면을 구성한다. 공수에 따라 보여주는 목록이 다르다
    /// </summary>
    public void Open(GameSession session, LiveGameController controller, bool isPlayerHome)
    {
        if (session == null || controller == null)
        {
            Debug.LogError("[PauseSubstitutionScreen]: 경기 또는 교체 창구가 없습니다");
            return;
        }

        _session = session;
        _controller = controller;
        _isPlayerHome = isPlayerHome;

        GameState state = session.State;

        _isPlayerAttacking = state.IsTopInning != isPlayerHome;

        _selectedBenchIndex = -1;
        _selectedOrderIndex = -1;
        _selectedPitcherSlot = LiveGameController.NoPitcherSubstitution;

        _titleText.text = _isPlayerAttacking ? "대타 교체" : "투수 교체";
        _situationText.text = BuildSituationText(state);
        _previewText.text = string.Empty;

        RefreshLists();
        UpdateApplyState();
    }

    protected override void BindEvents()
    {
        Bind(_applyButton, OnApplyClicked);
        BindButton("ResumeButton", OnResumeClicked);
    }

    protected override void Build()
    {
        Image dim = UiBuilder.CreateImage(Root, "Dim", UiPalette.WithAlpha(UiPalette.BgApp, 0.92f));
        UiBuilder.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
        dim.raycastTarget = true;

        _titleText = UiBuilder.CreateText(Root, "Title", 48f, UiPalette.Text1);
        UiBuilder.Place(_titleText.rectTransform, 32f, 40f, 600f, 56f);

        _situationText = UiBuilder.CreateText(Root, "Situation", 34f, UiPalette.Text2);
        UiBuilder.Place(_situationText.rectTransform, 32f, 110f, 1016f, 44f);

        _previewText = UiBuilder.CreateText(Root, "Preview", 36f, UiPalette.AccentGold);
        UiBuilder.Place(_previewText.rectTransform, 32f, 170f, 1016f, 48f);

        _leftHeaderText = UiBuilder.CreateText(Root, "LeftHeader", 32f, UiPalette.Text3);
        UiBuilder.Place(_leftHeaderText.rectTransform, 32f, 244f, ColumnWidth, 40f);

        _rightHeaderText = UiBuilder.CreateText(Root, "RightHeader", 32f, UiPalette.Text3);
        UiBuilder.Place(_rightHeaderText.rectTransform, 556f, 244f, ColumnWidth, 40f);

        RectTransform leftColumn = UiBuilder.CreateRect(Root, "LeftColumn");
        UiBuilder.Place(leftColumn, 32f, ListTop, ColumnWidth,
            SimulationContext.LineupSize * (LineupRowView.RowHeight + RowGap));

        RectTransform rightColumn = UiBuilder.CreateRect(Root, "RightColumn");
        UiBuilder.Place(rightColumn, 556f, ListTop, ColumnWidth,
            SimulationContext.LineupSize * (LineupRowView.RowHeight + RowGap));

        _orderRows = new LineupRowView[SimulationContext.LineupSize];

        for (int i = 0; i < _orderRows.Length; i++)
        {
            int orderIndex = i;

            _orderRows[i] = LineupRowView.Bake(leftColumn, ColumnWidth);
            _orderRows[i].SetIndex(i, RowGap);
            _orderRows[i].SetClickHandler(() => OnOrderSelected(orderIndex));
        }

        _benchRows = new LineupRowView[SimulationContext.MaxBenchSize];

        for (int i = 0; i < _benchRows.Length; i++)
        {
            int benchIndex = i;

            _benchRows[i] = LineupRowView.Bake(rightColumn, ColumnWidth);
            _benchRows[i].SetIndex(i, RowGap);
            _benchRows[i].SetClickHandler(() => OnBenchSelected(benchIndex));
        }

        _pitcherRows = new LineupRowView[PitcherOptionCount];

        for (int i = 0; i < _pitcherRows.Length; i++)
        {
            int pitcherSlot = i + 1;

            _pitcherRows[i] = LineupRowView.Bake(leftColumn, ColumnWidth);
            _pitcherRows[i].SetIndex(i, RowGap);
            _pitcherRows[i].SetClickHandler(() => OnPitcherSelected(pitcherSlot));
        }

        _applyButton = UiBuilder.CreateButton(Root, "ApplyButton", "교체 적용", 40f, true, OnApplyClicked);
        RectTransform applyRect = _applyButton.GetComponent<RectTransform>();

        applyRect.anchorMin = new Vector2(0f, 0f);
        applyRect.anchorMax = new Vector2(0.5f, 0f);
        applyRect.pivot = new Vector2(0f, 0f);
        applyRect.offsetMin = new Vector2(32f, 32f);
        applyRect.offsetMax = new Vector2(-12f, 136f);

        Button resumeButton = UiBuilder.CreateButton(Root, "ResumeButton", "그냥 재개", 40f, false, OnResumeClicked);
        RectTransform resumeRect = resumeButton.GetComponent<RectTransform>();

        resumeRect.anchorMin = new Vector2(0.5f, 0f);
        resumeRect.anchorMax = new Vector2(1f, 0f);
        resumeRect.pivot = new Vector2(0f, 0f);
        resumeRect.offsetMin = new Vector2(12f, 32f);
        resumeRect.offsetMax = new Vector2(-32f, 136f);
    }

    //공수에 따라 좌우 목록을 다시 채운다
    private void RefreshLists()
    {
        GameState state = _session.State;
        SimulationContext context = _session.Context;

        _leftHeaderText.text = _isPlayerAttacking ? "현재 타순" : "대기 투수";
        _rightHeaderText.text = _isPlayerAttacking ? "벤치" : string.Empty;

        HitterSnapshot[] lineup = _isPlayerHome ? context.HomeLineup : context.AwayLineup;
        HitterSnapshot[] bench = _isPlayerHome ? context.HomeBench : context.AwayBench;
        PitcherSnapshot[] pitchers = _isPlayerHome ? context.HomePitchers : context.AwayPitchers;

        for (int i = 0; i < _orderRows.Length; i++)
        {
            bool shown = _isPlayerAttacking && i < lineup.Length;

            _orderRows[i].SetVisible(shown);

            if (!shown)
                continue;

            _orderRows[i].SetHitter(i + 1, lineup[i], true);
            _orderRows[i].SetState(i == _selectedOrderIndex ? UiRowState.Selected : UiRowState.Normal);
        }

        for (int i = 0; i < _benchRows.Length; i++)
        {
            _benchRows[i].SetVisible(_isPlayerAttacking);

            if (!_isPlayerAttacking)
                continue;

            if (i >= bench.Length)
            {
                _benchRows[i].SetEmpty("빈 슬롯");
                continue;
            }

            _benchRows[i].SetHitter(0, bench[i], true);

            if (state.IsBenchUsed(_isPlayerHome, i))
                _benchRows[i].SetState(UiRowState.Used);
            else
                _benchRows[i].SetState(i == _selectedBenchIndex ? UiRowState.Selected : UiRowState.Normal);
        }

        PitcherState currentPitcher = _isPlayerHome ? state.HomePitcherState : state.AwayPitcherState;
        int currentSlot = currentPitcher == null ? -1 : currentPitcher.PitcherSlotIndex;

        for (int i = 0; i < _pitcherRows.Length; i++)
        {
            int slot = i + 1;
            bool shown = !_isPlayerAttacking && slot < pitchers.Length;

            _pitcherRows[i].SetVisible(shown);

            if (!shown)
                continue;

            _pitcherRows[i].SetPitcher(pitchers[slot], true);

            if (slot == currentSlot)
                _pitcherRows[i].SetState(UiRowState.Pitching);
            else if (state.IsPitcherUsed(_isPlayerHome, slot))
                _pitcherRows[i].SetState(UiRowState.Used);
            else
                _pitcherRows[i].SetState(slot == _selectedPitcherSlot ? UiRowState.Selected : UiRowState.Normal);
        }
    }

    private void OnBenchSelected(int benchIndex)
    {
        if (!_isPlayerAttacking)
            return;

        HitterSnapshot[] bench = _isPlayerHome ? _session.Context.HomeBench : _session.Context.AwayBench;

        if (benchIndex >= bench.Length || _session.State.IsBenchUsed(_isPlayerHome, benchIndex))
            return;

        _selectedBenchIndex = _selectedBenchIndex == benchIndex ? -1 : benchIndex;

        RefreshLists();
        UpdateApplyState();
    }

    private void OnOrderSelected(int orderIndex)
    {
        if (!_isPlayerAttacking)
            return;

        _selectedOrderIndex = _selectedOrderIndex == orderIndex ? -1 : orderIndex;

        RefreshLists();
        UpdateApplyState();
    }

    private void OnPitcherSelected(int pitcherSlot)
    {
        if (_isPlayerAttacking)
            return;

        GameState state = _session.State;
        PitcherState currentPitcher = _isPlayerHome ? state.HomePitcherState : state.AwayPitcherState;

        if (currentPitcher != null && currentPitcher.PitcherSlotIndex == pitcherSlot)
            return;

        if (state.IsPitcherUsed(_isPlayerHome, pitcherSlot))
            return;

        _selectedPitcherSlot = _selectedPitcherSlot == pitcherSlot
            ? LiveGameController.NoPitcherSubstitution
            : pitcherSlot;

        RefreshLists();
        UpdateApplyState();
    }

    //교체 가능 여부와 미리보기 문장을 갱신한다
    private void UpdateApplyState()
    {
        SimulationContext context = _session.Context;

        if (_isPlayerAttacking)
        {
            bool ready = _selectedBenchIndex >= 0 && _selectedOrderIndex >= 0;

            _applyButton.interactable = ready;
            _previewText.text = ready ? BuildHitterPreview(context) : "벤치와 교체할 타순을 차례로 고르세요";
            return;
        }

        bool pitcherReady = _selectedPitcherSlot != LiveGameController.NoPitcherSubstitution;

        _applyButton.interactable = pitcherReady;
        _previewText.text = pitcherReady ? BuildPitcherPreview(context) : "올릴 투수를 고르세요";
    }

    private string BuildHitterPreview(SimulationContext context)
    {
        HitterSnapshot[] lineup = _isPlayerHome ? context.HomeLineup : context.AwayLineup;
        HitterSnapshot[] bench = _isPlayerHome ? context.HomeBench : context.AwayBench;

        HitterSnapshot outgoing = lineup[_selectedOrderIndex];
        HitterSnapshot incoming = bench[_selectedBenchIndex];

        return $"{_selectedOrderIndex + 1}번 {PlayerLabelFormat.NameWithYear(outgoing.Name, outgoing.Year)}" +
               $" ⇄ {PlayerLabelFormat.NameWithYear(incoming.Name, incoming.Year)}";
    }

    private string BuildPitcherPreview(SimulationContext context)
    {
        PitcherSnapshot[] pitchers = _isPlayerHome ? context.HomePitchers : context.AwayPitchers;
        PitcherState currentPitcher = _isPlayerHome
            ? _session.State.HomePitcherState
            : _session.State.AwayPitcherState;

        PitcherSnapshot incoming = pitchers[_selectedPitcherSlot];
        string outgoingName = currentPitcher == null
            ? "—"
            : PlayerLabelFormat.NameWithYear(currentPitcher.Snapshot.Name, currentPitcher.Snapshot.Year);

        return $"{outgoingName} ⇄ {PlayerLabelFormat.NameWithYear(incoming.Name, incoming.Year)}";
    }

    private void OnApplyClicked()
    {
        if (_isPlayerAttacking)
        {
            if (!_controller.ReserveHitterSubstitution(_selectedOrderIndex, _selectedBenchIndex))
                return;
        }
        else if (!_controller.ReservePitcherSubstitution(_selectedPitcherSlot))
        {
            return;
        }

        _onApply?.Invoke();
    }

    private void OnResumeClicked()
    {
        _controller.ClearReservations();
        _onResume?.Invoke();
    }

    private static string BuildSituationText(GameState state)
    {
        string runners = BuildRunnerText(state);

        return $"{PlayLogTextProvider.GetInningChipText(state.Inning, state.IsTopInning)}" +
               $"  {state.OutCount}아웃  ·  {state.AwayScore} - {state.HomeScore}  ·  {runners}";
    }

    private static string BuildRunnerText(GameState state)
    {
        bool first = state.FirstBase != GameState.NoRunner;
        bool second = state.SecondBase != GameState.NoRunner;
        bool third = state.ThirdBase != GameState.NoRunner;

        if (!first && !second && !third)
            return "주자 없음";

        string text = string.Empty;

        if (first)
            text += "1루 ";

        if (second)
            text += "2루 ";

        if (third)
            text += "3루 ";

        return text + "주자";
    }
}
