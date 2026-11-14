using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 로비 허브. 메뉴 칸은 화면 아래에 붙어 쌓이고(맨 아래 한 줄 = 리그 입장), 비는 위쪽에는 내 팀 입체 엠블럼이 떠 돈다.
/// 재화 줄은 뽑기권을 뺀 재화만 보여 준다 (뽑기권은 뽑기 화면에서)
/// </summary>
public class LobbyScreen : UiScreenBehaviour
{
    private const int MenuCount = 11;
    private const int LeagueIndex = 0;
    //칸 배치: 좌우 여백 · 칸 사이 · 바닥 여백
    private const float SideMargin = 32f;
    private const float Gap = 16f;
    private const float BottomMargin = 40f;
    private const float ContentWidth = 1080f - SideMargin * 2f;
    //위쪽 머리: 재화 줄 · 팀 이름 · 라인업 상태
    private const float CurrencyTop = 32f;
    private const float TeamTop = 136f;
    private const float LineupTop = 236f;
    private const float HeaderBottom = 296f;
    //입체 엠블럼: 머리와 메뉴 사이 빈 칸 가운데에 놓는 정사각형 크기
    private const float TokenSize = 660f;
    private const float TokenMargin = 12f;

    //줄마다 높이 · 글자 크기 (아래 줄부터)
    private static readonly float[] RowHeights = { 168f, 120f, 184f, 184f, 208f };
    private static readonly float[] RowFontSizes = { 46f, 32f, 34f, 34f, 40f };

    //AppFlowController 가 넘기는 동작 순서와 같다 (0 = 리그 입장)
    private static readonly MenuSlot[] Slots =
    {
        new MenuSlot("리그 입장", "league", 0, 0, 1),
        new MenuSlot("라인업", "lineup", 4, 0, 2),
        new MenuSlot("뽑기", "gacha", 4, 1, 2),
        new MenuSlot("인벤토리", "inventory", 3, 0, 3),
        new MenuSlot("강화", "enhance", 3, 1, 3),
        new MenuSlot("훈련 · 돌파", "train", 3, 2, 3),
        new MenuSlot("카드 조합", "combine", 2, 0, 3),
        new MenuSlot("카드 분해", "dismantle", 2, 1, 3),
        new MenuSlot("골든글러브 제작", "goldenglove", 2, 2, 3),
        new MenuSlot("저장", "save", 1, 0, 2),
        new MenuSlot("빠른 시작", "quickstart", 1, 1, 2)
    };

    [SerializeField] private TextMeshProUGUI _teamText;
    [SerializeField] private TextMeshProUGUI _lineupText;
    [SerializeField] private CurrencyBarView _currencyBar;
    [SerializeField] private TeamEmblemTokenView _emblemToken;
    [SerializeField] private LobbyMenuTileView _leagueTile;
    [SerializeField] private List<Button> _menuButtons = new List<Button>(MenuCount);

    private Action[] _menuActions;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action[] menuActions)
    {
        _menuActions = menuActions;
    }

    /// <summary>
    /// 팀 · 라인업 상태 · 재화 · 입체 엠블럼을 다시 읽는다
    /// </summary>
    public void Refresh()
    {
        if (PlayerDataManager.Instance != null)
        {
            _teamText.text = PlayerDataManager.Instance.PlayerTeamName;
            _emblemToken.SetTeam(PlayerDataManager.Instance.PlayerTeamName);
        }

        bool lineupReady = LineUpManager.Instance != null && LineUpManager.Instance.IsLineupComplete();

        _lineupText.text = lineupReady ? "라인업 준비 완료" : "라인업이 비어 있습니다 (리그 입장 불가)";
        _lineupText.color = lineupReady ? UiPalette.StatePitching : UiPalette.StateOut;
        _leagueTile.SetInteractable(lineupReady);

        _currencyBar.Refresh();
    }

    protected override void BindEvents()
    {
        for (int i = 0; i < _menuButtons.Count; i++)
        {
            int index = i;

            Bind(_menuButtons[i], () => OnMenuClicked(index));
        }
    }

    protected override void Build()
    {
        _currencyBar = CurrencyBarView.Bake(Root, CurrencyBarView.WithoutTickets);
        UiBuilder.Place(_currencyBar.Root, (1080f - CurrencyBarView.BarWidth) * 0.5f, CurrencyTop,
            CurrencyBarView.BarWidth, CurrencyBarView.BarHeight);

        _teamText = UiBuilder.CreateText(Root, "Team", 86f, UiPalette.Text1);
        UiBuilder.Place(_teamText.rectTransform, SideMargin, TeamTop, 700f, 100f);

        _lineupText = UiBuilder.CreateText(Root, "LineupState", 32f, UiPalette.Text2);
        UiBuilder.Place(_lineupText.rectTransform, SideMargin, LineupTop, 900f, 44f);

        BuildEmblemToken();

        for (int i = 0; i < Slots.Length; i++)
        {
            int index = i;
            LobbyMenuTileView tile = CreateTile(Slots[i], () => OnMenuClicked(index));

            _menuButtons.Add(tile.Button);

            if (i == LeagueIndex)
                _leagueTile = tile;
        }
    }

    protected override void OnShown()
    {
        Refresh();

        for (int i = 0; i < _menuButtons.Count; i++)
            UiEffects.Pop(_menuButtons[i].transform, 0.9f, 0.22f, i * 0.03f);
    }

    //머리 아래 ~ 메뉴 위 빈 칸을 늘어나는 영역으로 잡고, 그 가운데에 정사각형 토큰을 둔다
    private void BuildEmblemToken()
    {
        RectTransform area = UiBuilder.CreateRect(Root, "EmblemArea");
        UiBuilder.Stretch(area, 0f, HeaderBottom + TokenMargin, 0f, MenuTop() + TokenMargin);

        _emblemToken = TeamEmblemTokenView.Bake(area);

        RectTransform token = _emblemToken.Root;
        token.anchorMin = token.anchorMax = new Vector2(0.5f, 0.5f);
        token.pivot = new Vector2(0.5f, 0.5f);
        token.anchoredPosition = Vector2.zero;
        token.sizeDelta = new Vector2(TokenSize, TokenSize);
    }

    //칸 하나를 줄 · 열 자리에 놓는다. 화면 아래 기준이라 세로가 긴 기기에서도 바닥에 붙는다
    private LobbyMenuTileView CreateTile(MenuSlot slot, UnityAction onClick)
    {
        float width = (ContentWidth - Gap * (slot.Columns - 1)) / slot.Columns;
        float height = RowHeights[slot.Row];
        Vector2 size = new Vector2(width, height);

        LobbyMenuTileView tile = LobbyMenuTileView.Create(Root, "Menu" + slot.IconName, slot.Label, slot.IconName,
            size, RowFontSizes[slot.Row], slot.Row == 0, onClick);

        RectTransform rect = tile.Root;
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(SideMargin + slot.Column * (width + Gap) + width * 0.5f,
            RowBottom(slot.Row) + height * 0.5f);

        return tile;
    }

    //아래에서 row 번째 줄의 바닥 높이
    private static float RowBottom(int row)
    {
        float bottom = BottomMargin;

        for (int i = 0; i < row; i++)
            bottom += RowHeights[i] + Gap;

        return bottom;
    }

    //맨 위 줄의 윗변 높이 (화면 아래 기준)
    private static float MenuTop()
    {
        int topRow = RowHeights.Length - 1;

        return RowBottom(topRow) + RowHeights[topRow];
    }

    private void OnMenuClicked(int index)
    {
        if (_menuActions == null || index < 0 || index >= _menuActions.Length)
            return;

        _menuActions[index]?.Invoke();
    }

    //메뉴 칸 하나의 자리: 이름 · 아이콘 · 아래에서 몇 번째 줄 · 몇 번째 열 · 그 줄의 열 수
    private readonly struct MenuSlot
    {
        public readonly string Label;
        public readonly string IconName;
        public readonly int Row;
        public readonly int Column;
        public readonly int Columns;

        public MenuSlot(string label, string iconName, int row, int column, int columns)
        {
            Label = label;
            IconName = iconName;
            Row = row;
            Column = column;
            Columns = columns;
        }
    }
}
