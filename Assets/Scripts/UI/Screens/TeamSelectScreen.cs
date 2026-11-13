using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 팀 선택 화면. 선택 가능한 10팀 중 하나를 고르고 새 게임을 시작한다
/// </summary>
public class TeamSelectScreen : UiScreenBehaviour
{
    private const int ColumnCount = 2;
    //정사각형에 가까운 칸. 10팀이 2열 5행으로 들어간다
    private const float ButtonWidth = 360f;
    private const float ButtonHeight = 276f;
    private const float ColumnGap = 40f;
    private const float RowGap = 14f;
    private const float GridTop = 200f;
    private const float LabelFontSize = 52f;

    [SerializeField] private TextMeshProUGUI _selectionText;
    [SerializeField] private Button _confirmButton;

    private Action<string> _onConfirm;
    private readonly List<TeamSelectButtonView> _teamButtons = new List<TeamSelectButtonView>(10);
    private readonly List<string> _teamNames = new List<string>(10);
    private string _selectedTeamName;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action<string> onConfirm)
    {
        _onConfirm = onConfirm;
    }

    /// <summary>
    /// 선택 가능한 팀 목록을 채운다. 화면을 띄우기 전에 한 번 호출한다
    /// </summary>
    public void SetTeams(IReadOnlyList<string> teamNames)
    {
        if (teamNames == null || teamNames.Count == 0)
        {
            Debug.LogError("[TeamSelectScreen]: 선택할 팀 목록이 비어 있습니다");
            return;
        }

        _teamNames.Clear();
        _teamNames.AddRange(teamNames);
        _selectedTeamName = null;

        for (int i = 0; i < _teamButtons.Count; i++)
            _teamButtons[i].SetVisible(false);

        for (int i = 0; i < _teamNames.Count; i++)
        {
            TeamSelectButtonView button = EnsureButton(i);

            button.SetVisible(true);
            button.SetTeam(_teamNames[i]);
        }

        RefreshSelection();
    }

    protected override void BindEvents()
    {
        Bind(_confirmButton, OnConfirmClicked);
    }

    protected override void Build()
    {
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 56f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, 40f, 60f, 800f, 70f);
        title.text = "팀 선택";

        TextMeshProUGUI guide = UiBuilder.CreateText(Root, "Guide", 32f, UiPalette.Text3);
        UiBuilder.Place(guide.rectTransform, 40f, 140f, 1000f, 44f);
        guide.text = "선택한 팀의 시작 시그니쳐 카드를 받습니다";

        _selectionText = UiBuilder.CreateText(Root, "Selection", 38f, UiPalette.AccentGold,
            TextAlignmentOptions.Center);
        RectTransform selectionRect = _selectionText.rectTransform;

        selectionRect.anchorMin = new Vector2(0f, 0f);
        selectionRect.anchorMax = new Vector2(1f, 0f);
        selectionRect.pivot = new Vector2(0.5f, 0f);
        selectionRect.offsetMin = new Vector2(40f, 196f);
        selectionRect.offsetMax = new Vector2(-40f, 252f);

        _confirmButton = UiBuilder.CreateButton(Root, "ConfirmButton", "이 팀으로 시작", 42f, true, OnConfirmClicked);
        RectTransform confirmRect = _confirmButton.GetComponent<RectTransform>();

        confirmRect.anchorMin = new Vector2(0f, 0f);
        confirmRect.anchorMax = new Vector2(1f, 0f);
        confirmRect.pivot = new Vector2(0.5f, 0f);
        confirmRect.offsetMin = new Vector2(40f, 60f);
        confirmRect.offsetMax = new Vector2(-40f, 188f);
    }

    //팀 버튼은 목록 길이에 맞춰 필요한 만큼만 만든다
    private TeamSelectButtonView EnsureButton(int index)
    {
        while (_teamButtons.Count <= index)
        {
            int teamIndex = _teamButtons.Count;
            int row = teamIndex / ColumnCount;
            int column = teamIndex % ColumnCount;

            float gridWidth = ColumnCount * ButtonWidth + (ColumnCount - 1) * ColumnGap;
            float gridLeft = (UiBuilder.DesignSize.x - gridWidth) * 0.5f;

            TeamSelectButtonView button = TeamSelectButtonView.Create(Root, ButtonHeight, LabelFontSize,
                () => OnTeamClicked(teamIndex));

            UiBuilder.Place(button.Root,
                gridLeft + column * (ButtonWidth + ColumnGap),
                GridTop + row * (ButtonHeight + RowGap),
                ButtonWidth, ButtonHeight);

            _teamButtons.Add(button);
        }

        return _teamButtons[index];
    }

    private void OnTeamClicked(int index)
    {
        if (index < 0 || index >= _teamNames.Count)
            return;

        _selectedTeamName = _teamNames[index];
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        for (int i = 0; i < _teamNames.Count && i < _teamButtons.Count; i++)
            _teamButtons[i].SetSelected(_teamNames[i] == _selectedTeamName);

        bool hasSelection = !string.IsNullOrEmpty(_selectedTeamName);

        _confirmButton.interactable = hasSelection;
        _selectionText.text = hasSelection ? $"{_selectedTeamName} 선택" : "팀을 고르세요";
    }

    private void OnConfirmClicked()
    {
        if (string.IsNullOrEmpty(_selectedTeamName))
            return;

        _onConfirm?.Invoke(_selectedTeamName);
    }
}
