using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 로비 허브. 재화를 보여주고 리그 · 라인업 · 수급 · 육성 화면으로 보낸다
/// </summary>
public class LobbyScreen : UiScreenBehaviour
{
    private Action[] _menuActions;

    private static readonly string[] MenuLabels =
    {
        "리그 입장", "라인업", "뽑기", "인벤토리",
        "강화", "훈련 · 돌파", "카드 조합", "카드 분해",
        "골든글러브 제작", "저장", "빠른 시작"
    };

    private const int MenuLabelCount = 11;
    private const int MenuColumns = 3;
    //1080 에서 좌우 여백 32 · 열 간격 16 을 빼고 3등분한 값
    private const float MenuWidth = 328f;
    private const float MenuHeight = 148f;
    private const float MenuGap = 16f;
    private const float MenuTop = 420f;
    //3열이라 칸이 좁아져 '골든글러브 제작' 이 넘치지 않도록 줄였다
    private const float MenuFontSize = 32f;

    [SerializeField] private TextMeshProUGUI _teamText;
    [SerializeField] private TextMeshProUGUI _tierText;
    [SerializeField] private TextMeshProUGUI _lineupText;
    [SerializeField] private CurrencyBarView _currencyBar;
    [SerializeField] private Button _leagueButton;
    [SerializeField] private List<Button> _menuButtons = new List<Button>(MenuLabelCount);

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action[] menuActions)
    {
        _menuActions = menuActions;
    }

    /// <summary>
    /// 팀 · 티어 · 라인업 상태와 재화를 다시 읽는다
    /// </summary>
    public void Refresh()
    {
        if (PlayerDataManager.Instance != null)
        {
            _teamText.text = PlayerDataManager.Instance.PlayerTeamName;
            _tierText.text = $"해금 티어  {LeagueTierNames.Get(PlayerDataManager.Instance.HighestUnlockedTier)}" +
                             $"   ·   {(PlayerDataManager.Instance.CurrentSubstitutionMode == SubstitutionMode.Manual ? "수동 교체" : "자동 교체")}";
        }

        bool lineupReady = LineUpManager.Instance != null && LineUpManager.Instance.IsLineupComplete();

        _lineupText.text = lineupReady ? "라인업 준비 완료" : "라인업이 비어 있습니다 (리그 입장 불가)";
        _lineupText.color = lineupReady ? UiPalette.StatePitching : UiPalette.StateOut;
        _leagueButton.interactable = lineupReady;

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
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 40f, UiPalette.Text3);
        UiBuilder.Place(title.rectTransform, 32f, 40f, 600f, 52f);
        title.text = "베이스볼 매니저";

        _teamText = UiBuilder.CreateText(Root, "Team", 86f, UiPalette.Text1);
        UiBuilder.Place(_teamText.rectTransform, 32f, 96f, 700f, 100f);

        _tierText = UiBuilder.CreateText(Root, "Tier", 32f, UiPalette.Text2);
        UiBuilder.Place(_tierText.rectTransform, 32f, 200f, 900f, 44f);

        _lineupText = UiBuilder.CreateText(Root, "LineupState", 32f, UiPalette.Text2);
        UiBuilder.Place(_lineupText.rectTransform, 32f, 248f, 900f, 44f);

        _currencyBar = CurrencyBarView.Bake(Root);
        UiBuilder.Place(_currencyBar.Root, 24f, 310f, 1032f, CurrencyBarView.BarHeight);

        for (int i = 0; i < MenuLabels.Length; i++)
        {
            int index = i;
            int column = i % MenuColumns;
            int row = i / MenuColumns;

            Button button = UiBuilder.CreateButton(Root, "Menu" + i, MenuLabels[i], MenuFontSize,
                i == 0, () => OnMenuClicked(index));

            UiBuilder.Place(button.GetComponent<RectTransform>(),
                32f + column * (MenuWidth + MenuGap),
                MenuTop + row * (MenuHeight + MenuGap),
                MenuWidth, MenuHeight);

            _menuButtons.Add(button);

            if (i == 0)
                _leagueButton = button;
        }
    }

    protected override void OnShown()
    {
        Refresh();

        for (int i = 0; i < _menuButtons.Count; i++)
            UiEffects.Pop(_menuButtons[i].transform, 0.9f, 0.22f, i * 0.03f);
    }

    private void OnMenuClicked(int index)
    {
        if (_menuActions == null || index < 0 || index >= _menuActions.Length)
            return;

        _menuActions[index]?.Invoke();
    }
}
