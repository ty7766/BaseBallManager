using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ④ 일시정지 화면 (명세 ④ · 중계 그래픽 B안). 공격 중이면 대타, 수비 중이면 투수를 교체한다.
/// 위 판은 상황, 그 아래 나가는 선수 → 들어오는 선수 미리보기, 아래는 얼굴 사진이 든 선수 목록.
/// 투수 교체 때는 대기 투수 아래에 상대 다음 타자 셋을 함께 보여 준다
/// </summary>
public class PauseSubstitutionScreen : UiScreenBehaviour
{
    private const float SideMargin = 24f;
    private const float PanelWidth = 1032f;
    private const float HeaderTop = 24f;
    private const float HeaderHeight = 132f;
    private const float PreviewTop = 172f;
    private const float PreviewHeight = 250f;
    private const float ListTop = 438f;
    private const float ButtonHeight = 120f;
    private const float ButtonBottom = 32f;
    private const float ListBottom = ButtonBottom + ButtonHeight + 20f;
    private const float ColumnGap = 16f;
    private const float ListHeaderHeight = 60f;
    private const float ListPadding = 12f;
    private const float ListBottomPadding = 10f;
    private const int PitcherOptionCount = SimulationContext.PitcherSlotCount - 1;
    private const int UpcomingBatterCount = 3;

    private Action _onApply;
    private Action _onResume;

    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _situationText;
    [SerializeField] private SubstitutionPreviewView _preview;
    [SerializeField] private RectTransform _hitterGroup;
    [SerializeField] private RectTransform _pitcherGroup;
    [SerializeField] private LineupRowView[] _orderRows;
    [SerializeField] private LineupRowView[] _benchRows;
    [SerializeField] private LineupRowView[] _pitcherRows;
    [SerializeField] private LineupRowView[] _upcomingRows;
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

        RefreshLists();
        UpdateApplyState();
    }

    protected override void BindEvents()
    {
        for (int i = 0; i < _orderRows.Length; i++)
        {
            int orderIndex = i;

            _orderRows[i].SetClickHandler(() => OnOrderSelected(orderIndex));
        }

        for (int i = 0; i < _benchRows.Length; i++)
        {
            int benchIndex = i;

            _benchRows[i].SetClickHandler(() => OnBenchSelected(benchIndex));
        }

        for (int i = 0; i < _pitcherRows.Length; i++)
        {
            int pitcherSlot = i + 1;

            _pitcherRows[i].SetClickHandler(() => OnPitcherSelected(pitcherSlot));
        }

        Bind(_applyButton, OnApplyClicked);
        BindButton("ResumeButton", OnResumeClicked);
    }

    protected override void Build()
    {
        Image dim = UiBuilder.CreateImage(Root, "Dim", UiPalette.WithAlpha(UiPalette.BgApp, 0.9f));
        UiBuilder.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
        dim.raycastTarget = true;

        RectTransform header = BroadcastPanel.Create(Root, "Header").rectTransform;
        UiBuilder.Place(header, SideMargin, HeaderTop, PanelWidth, HeaderHeight);

        _titleText = UiBuilder.CreateText(header, "Title", 50f, UiPalette.Text1);
        UiBuilder.Place(_titleText.rectTransform, 32f, 16f, 600f, 60f);
        _titleText.fontStyle = FontStyles.Bold;

        _situationText = UiBuilder.CreateText(header, "Situation", 30f, UiPalette.Text2);
        UiBuilder.Place(_situationText.rectTransform, 32f, 80f, PanelWidth - 64f, 40f);

        _preview = SubstitutionPreviewView.Bake(Root, PanelWidth, PreviewHeight);
        UiBuilder.Place(_preview.Root, SideMargin, PreviewTop, PanelWidth, PreviewHeight);

        RectTransform listArea = UiBuilder.CreateRect(Root, "ListArea");
        UiBuilder.Stretch(listArea, SideMargin, ListTop, SideMargin, ListBottom);

        BuildHitterLists(listArea);
        BuildPitcherList(listArea);

        _applyButton = UiBuilder.CreateButton(Root, "ApplyButton", "교체 적용", 42f, true, OnApplyClicked);
        BroadcastPanel.ApplyButtonStyle(_applyButton, true);
        PlaceBottomButton(_applyButton, 0f, 0.5f, SideMargin, -ColumnGap * 0.5f);

        Button resumeButton = UiBuilder.CreateButton(Root, "ResumeButton", "재개", 42f, false, OnResumeClicked);
        BroadcastPanel.ApplyButtonStyle(resumeButton, false);
        PlaceBottomButton(resumeButton, 0.5f, 1f, ColumnGap * 0.5f, -SideMargin);
    }

    //대타: 왼쪽 현재 타순 · 오른쪽 벤치. 두 판 모두 타순 9칸 높이로 줄을 나눈다
    private void BuildHitterLists(RectTransform listArea)
    {
        _hitterGroup = UiBuilder.CreateRect(listArea, "HitterGroup");
        UiBuilder.Stretch(_hitterGroup, 0f, 0f, 0f, 0f);

        float rowWidth = (PanelWidth - ColumnGap) * 0.5f - ListPadding * 2f;

        RectTransform orderPanel = CreateListPanel(_hitterGroup, "OrderPanel", "현재 타순", out RectTransform orderRowArea);
        orderPanel.anchorMin = new Vector2(0f, 0f);
        orderPanel.anchorMax = new Vector2(0.5f, 1f);
        orderPanel.offsetMin = Vector2.zero;
        orderPanel.offsetMax = new Vector2(-ColumnGap * 0.5f, 0f);

        RectTransform benchPanel = CreateListPanel(_hitterGroup, "BenchPanel", "벤치", out RectTransform benchRowArea);
        benchPanel.anchorMin = new Vector2(0.5f, 0f);
        benchPanel.anchorMax = new Vector2(1f, 1f);
        benchPanel.offsetMin = new Vector2(ColumnGap * 0.5f, 0f);
        benchPanel.offsetMax = Vector2.zero;

        _orderRows = new LineupRowView[SimulationContext.LineupSize];

        for (int i = 0; i < _orderRows.Length; i++)
        {
            int orderIndex = i;

            _orderRows[i] = LineupRowView.Bake(orderRowArea, i, SimulationContext.LineupSize, rowWidth);
            _orderRows[i].SetClickHandler(() => OnOrderSelected(orderIndex));
        }

        _benchRows = new LineupRowView[SimulationContext.MaxBenchSize];

        for (int i = 0; i < _benchRows.Length; i++)
        {
            int benchIndex = i;

            _benchRows[i] = LineupRowView.Bake(benchRowArea, i, SimulationContext.LineupSize, rowWidth);
            _benchRows[i].SetClickHandler(() => OnBenchSelected(benchIndex));
        }
    }

    //투수: 위 판은 대기 투수(줄 높이를 대타 목록과 맞추려고 판 높이를 6/9로), 남은 아래 판은 상대 다음 타자
    private void BuildPitcherList(RectTransform listArea)
    {
        _pitcherGroup = UiBuilder.CreateRect(listArea, "PitcherGroup");
        UiBuilder.Stretch(_pitcherGroup, 0f, 0f, 0f, 0f);

        CreatePanelForPitchers(out RectTransform rowArea);
        CreatePanelForUpcoming(out RectTransform upcomingArea);

        _upcomingRows = new LineupRowView[UpcomingBatterCount];

        for (int i = 0; i < _upcomingRows.Length; i++)
            _upcomingRows[i] = LineupRowView.Bake(upcomingArea, i, UpcomingBatterCount, PanelWidth - ListPadding * 2f);

        _pitcherRows = new LineupRowView[PitcherOptionCount];

        for (int i = 0; i < _pitcherRows.Length; i++)
        {
            int pitcherSlot = i + 1;

            _pitcherRows[i] = LineupRowView.Bake(rowArea, i, PitcherOptionCount, PanelWidth - ListPadding * 2f);
            _pitcherRows[i].SetClickHandler(() => OnPitcherSelected(pitcherSlot));
        }
    }

    //대기 투수 판. 위에서부터 타순 목록 6칸 몫의 높이만 차지한다
    private RectTransform CreatePanelForPitchers(out RectTransform rowArea)
    {
        RectTransform panel = CreateListPanel(_pitcherGroup, "PitcherPanel", "대기 투수", out rowArea);
        float shownRatio = (float)PitcherOptionCount / SimulationContext.LineupSize;
        float fixedHeight = ListHeaderHeight + ListBottomPadding;

        panel.anchorMin = new Vector2(0f, 1f - shownRatio);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.offsetMin = new Vector2(0f, -fixedHeight * (1f - shownRatio));
        panel.offsetMax = Vector2.zero;

        return panel;
    }

    //상대 다음 타자 판. 대기 투수 판 아래 남은 자리를 채운다
    private void CreatePanelForUpcoming(out RectTransform rowArea)
    {
        RectTransform panel = CreateListPanel(_pitcherGroup, "UpcomingPanel", "상대 다음 타자", out rowArea);
        float shownRatio = (float)PitcherOptionCount / SimulationContext.LineupSize;
        float fixedHeight = ListHeaderHeight + ListBottomPadding;

        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(1f, 1f - shownRatio);
        panel.offsetMin = Vector2.zero;
        panel.offsetMax = new Vector2(0f, -fixedHeight * (1f - shownRatio) - ColumnGap);
    }

    //제목 줄이 있는 목록 판. 줄은 rowArea 안에 깐다
    private static RectTransform CreateListPanel(RectTransform parent, string name, string title, out RectTransform rowArea)
    {
        RectTransform panel = BroadcastPanel.Create(parent, name).rectTransform;

        TextMeshProUGUI header = UiBuilder.CreateLabel(panel, "Header", title, 26f, UiPalette.Text2);
        UiBuilder.Place(header.rectTransform, ListPadding + 12f, 14f, 400f, 36f);
        header.fontStyle = FontStyles.Bold;

        rowArea = UiBuilder.CreateRect(panel, "Rows");
        UiBuilder.Stretch(rowArea, ListPadding, ListHeaderHeight, ListPadding, ListBottomPadding);

        return panel;
    }

    //화면 아래 가로 절반 버튼
    private static void PlaceBottomButton(Button button, float anchorLeft, float anchorRight, float left, float right)
    {
        RectTransform rect = button.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(anchorLeft, 0f);
        rect.anchorMax = new Vector2(anchorRight, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.offsetMin = new Vector2(left, ButtonBottom);
        rect.offsetMax = new Vector2(right, ButtonBottom + ButtonHeight);
    }

    //공수에 따라 좌우 목록을 다시 채운다
    private void RefreshLists()
    {
        GameState state = _session.State;
        SimulationContext context = _session.Context;

        _hitterGroup.gameObject.SetActive(_isPlayerAttacking);
        _pitcherGroup.gameObject.SetActive(!_isPlayerAttacking);

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

        if (!_isPlayerAttacking)
            FillUpcomingBatters(state, context);
    }

    //수비 중일 때 상대 타순에서 지금 타자부터 셋
    private void FillUpcomingBatters(GameState state, SimulationContext context)
    {
        HitterSnapshot[] opponentLineup = _isPlayerHome ? context.AwayLineup : context.HomeLineup;
        int battingIndex = _isPlayerHome ? state.AwayBattingIndex : state.HomeBattingIndex;

        for (int i = 0; i < _upcomingRows.Length; i++)
        {
            int order = (Mathf.Max(0, battingIndex) + i) % opponentLineup.Length;

            _upcomingRows[i].SetHitter(order + 1, opponentLineup[order], false);
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

    //교체 가능 여부와 나가는 선수 → 들어오는 선수 미리보기를 갱신한다
    private void UpdateApplyState()
    {
        SimulationContext context = _session.Context;

        if (_isPlayerAttacking)
        {
            _applyButton.interactable = _selectedBenchIndex >= 0 && _selectedOrderIndex >= 0;
            ShowHitterPreview(context);
            return;
        }

        _applyButton.interactable = _selectedPitcherSlot != LiveGameController.NoPitcherSubstitution;
        ShowPitcherPreview(context);
    }

    private void ShowHitterPreview(SimulationContext context)
    {
        HitterSnapshot[] lineup = _isPlayerHome ? context.HomeLineup : context.AwayLineup;
        HitterSnapshot[] bench = _isPlayerHome ? context.HomeBench : context.AwayBench;

        if (_selectedOrderIndex >= 0)
        {
            HitterSnapshot outgoing = lineup[_selectedOrderIndex];

            _preview.SetPlayer(true, outgoing.InstanceId, outgoing.Name, outgoing.Year,
                $"{_selectedOrderIndex + 1}번 · {outgoing.Position} · OVR {outgoing.OVR}");
        }
        else
        {
            _preview.SetHint(true, "타순을 고르세요");
        }

        if (_selectedBenchIndex >= 0)
        {
            HitterSnapshot incoming = bench[_selectedBenchIndex];

            _preview.SetPlayer(false, incoming.InstanceId, incoming.Name, incoming.Year,
                $"{incoming.Position} · OVR {incoming.OVR}");
            return;
        }

        _preview.SetHint(false, "벤치에서 고르세요");
    }

    private void ShowPitcherPreview(SimulationContext context)
    {
        PitcherSnapshot[] pitchers = _isPlayerHome ? context.HomePitchers : context.AwayPitchers;
        PitcherState currentPitcher = _isPlayerHome
            ? _session.State.HomePitcherState
            : _session.State.AwayPitcherState;

        if (currentPitcher != null)
        {
            PitcherSnapshot outgoing = currentPitcher.Snapshot;

            _preview.SetPlayer(true, outgoing.InstanceId, outgoing.Name, outgoing.Year,
                $"{outgoing.Position} · OVR {outgoing.OVR}");
        }
        else
        {
            _preview.SetHint(true, "등판 중인 투수 없음");
        }

        if (_selectedPitcherSlot != LiveGameController.NoPitcherSubstitution)
        {
            PitcherSnapshot incoming = pitchers[_selectedPitcherSlot];

            _preview.SetPlayer(false, incoming.InstanceId, incoming.Name, incoming.Year,
                $"{incoming.Position} · OVR {incoming.OVR}");
            return;
        }

        _preview.SetHint(false, "올릴 투수를 고르세요");
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
