using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인벤토리·카드 선택 공용 필터 줄. 선수 타입 · 등급 · 종류 · 팀을 고른다
/// </summary>
public class CardFilterBarView
{
    /// <summary>
    /// 필터 줄 높이 (2단)
    /// </summary>
    public const float BarHeight = 168f;

    private static readonly PlayerTypeFilter[] TypeOptions =
    {
        PlayerTypeFilter.All, PlayerTypeFilter.HitterOnly, PlayerTypeFilter.PitcherOnly
    };

    private static readonly string[] TypeLabels = { "전체", "타자", "투수" };

    private static readonly CardGrade[] GradeOptions =
    {
        CardGrade.None, CardGrade.Star3, CardGrade.Star4, CardGrade.Star5
    };

    private static readonly string[] GradeLabels = { "등급", "3성", "4성", "5성" };

    private static readonly CardType[] KindOptions =
    {
        CardType.None, CardType.Normal, CardType.Signature, CardType.GoldenGlove
    };

    private static readonly string[] KindLabels = { "종류", "노말", "시그", "골글" };

    private static readonly string[] TeamOptions =
    {
        null, "기아", "삼성", "LG", "한화", "두산", "SSG", "KT", "NC", "키움", "롯데"
    };

    public RectTransform Root => _root;

    /// <summary>
    /// 현재 필터 조건. 인벤토리 조회에 그대로 넘긴다
    /// </summary>
    public CardFilter CurrentFilter { get; } = new CardFilter();

    private readonly RectTransform _root;
    private readonly Button[] _typeButtons = new Button[TypeOptions.Length];
    private readonly Button[] _gradeButtons = new Button[GradeOptions.Length];
    private readonly Button[] _kindButtons = new Button[KindOptions.Length];
    private readonly TextMeshProUGUI _teamLabel;

    private Action _onChanged;
    private int _teamIndex;

    public CardFilterBarView(Transform parent)
    {
        _root = UiBuilder.CreateRect(parent, "CardFilterBar");

        CurrentFilter.PlayerType = PlayerTypeFilter.All;
        CurrentFilter.Grade = CardGrade.None;
        CurrentFilter.Type = CardType.None;
        CurrentFilter.TeamName = null;

        for (int i = 0; i < TypeOptions.Length; i++)
        {
            int index = i;

            _typeButtons[i] = UiBuilder.CreateButton(_root, "Type", TypeLabels[i], 30f, false,
                () => SelectPlayerType(index));
            UiBuilder.Place(_typeButtons[i].GetComponent<RectTransform>(), i * 124f, 0f, 116f, 72f);
        }

        for (int i = 0; i < GradeOptions.Length; i++)
        {
            int index = i;

            _gradeButtons[i] = UiBuilder.CreateButton(_root, "Grade", GradeLabels[i], 28f, false,
                () => SelectGrade(index));
            UiBuilder.Place(_gradeButtons[i].GetComponent<RectTransform>(), 400f + i * 100f, 0f, 92f, 72f);
        }

        for (int i = 0; i < KindOptions.Length; i++)
        {
            int index = i;

            _kindButtons[i] = UiBuilder.CreateButton(_root, "Kind", KindLabels[i], 28f, false,
                () => SelectKind(index));
            UiBuilder.Place(_kindButtons[i].GetComponent<RectTransform>(), i * 124f, 84f, 116f, 72f);
        }

        Button teamButton = UiBuilder.CreateButton(_root, "Team", string.Empty, 28f, false, CycleTeam);
        UiBuilder.Place(teamButton.GetComponent<RectTransform>(), 520f, 84f, 280f, 72f);
        _teamLabel = teamButton.GetComponentInChildren<TextMeshProUGUI>();

        RefreshButtons();
    }

    /// <summary>
    /// 필터가 바뀔 때 호출할 동작
    /// </summary>
    public void SetChangedHandler(Action onChanged)
    {
        _onChanged = onChanged;
    }

    private void SelectPlayerType(int index)
    {
        CurrentFilter.PlayerType = TypeOptions[index];
        RefreshButtons();
        _onChanged?.Invoke();
    }

    private void SelectGrade(int index)
    {
        CurrentFilter.Grade = GradeOptions[index];
        RefreshButtons();
        _onChanged?.Invoke();
    }

    private void SelectKind(int index)
    {
        CurrentFilter.Type = KindOptions[index];
        RefreshButtons();
        _onChanged?.Invoke();
    }

    private void CycleTeam()
    {
        _teamIndex = (_teamIndex + 1) % TeamOptions.Length;
        CurrentFilter.TeamName = TeamOptions[_teamIndex];
        RefreshButtons();
        _onChanged?.Invoke();
    }

    //선택된 버튼만 흰 채움으로 둔다
    private void RefreshButtons()
    {
        for (int i = 0; i < _typeButtons.Length; i++)
            SetButtonSelected(_typeButtons[i], CurrentFilter.PlayerType == TypeOptions[i]);

        for (int i = 0; i < _gradeButtons.Length; i++)
            SetButtonSelected(_gradeButtons[i], CurrentFilter.Grade == GradeOptions[i]);

        for (int i = 0; i < _kindButtons.Length; i++)
            SetButtonSelected(_kindButtons[i], CurrentFilter.Type == KindOptions[i]);

        _teamLabel.text = CurrentFilter.TeamName ?? "팀 전체";
    }

    private static void SetButtonSelected(Button button, bool selected)
    {
        Image background = button.GetComponent<Image>();

        background.color = selected ? UiPalette.ButtonPrimary : UiPalette.BgRow;
        background.sprite = UiSprites.Get(selected ? UiSprites.ButtonPrimary : UiSprites.ButtonNormal);

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();

        if (label != null)
            label.color = selected ? UiPalette.ButtonPrimaryText : UiPalette.Text2;
    }
}
