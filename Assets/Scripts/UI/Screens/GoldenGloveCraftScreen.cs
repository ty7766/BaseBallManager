using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 골든글러브 제작. 전체 랜덤과 팀 지정 중 하나를 고른다 (팀 지정이 더 비싸다)
/// </summary>
public class GoldenGloveCraftScreen : CardActionScreen
{
    private static readonly string[] TeamNames =
    {
        "기아", "삼성", "LG", "한화", "두산", "SSG", "KT", "NC", "키움", "롯데"
    };

    private const int TeamColumns = 5;

    private Button _randomTabButton;
    private Button _teamTabButton;
    private RectTransform _teamGrid;
    private readonly Button[] _teamButtons = new Button[TeamNames.Length];
    private TextMeshProUGUI _costText;
    private TextMeshProUGUI _resultText;
    private Button _craftButton;

    private GoldenGloveCraftType _craftType = GoldenGloveCraftType.Random;
    private string _selectedTeamName;

    public GoldenGloveCraftScreen(Action onBack,
        Action<string, int, Func<CardDisplayInfo, bool>, Action<List<int>>> onPickCard,
        Action<string, bool> onNotify)
        : base("골든글러브 제작", onBack, onPickCard, onNotify)
    {
    }

    protected override string ScreenName => "GoldenGloveCraftScreen";

    public override void Refresh()
    {
        base.Refresh();

        SetTabSelected(_randomTabButton, _craftType == GoldenGloveCraftType.Random);
        SetTabSelected(_teamTabButton, _craftType == GoldenGloveCraftType.TeamSelect);

        bool teamMode = _craftType == GoldenGloveCraftType.TeamSelect;

        _teamGrid.gameObject.SetActive(teamMode);

        for (int i = 0; i < _teamButtons.Length; i++)
            SetTabSelected(_teamButtons[i], teamMode && TeamNames[i] == _selectedTeamName);

        if (GoldenGloveCraftManager.Instance == null)
            return;

        CurrencyCost[] costs = GoldenGloveCraftManager.Instance.GetCost(_craftType);

        _costText.text = BuildCostText(costs);
        _craftButton.interactable = CanAfford(costs) && (!teamMode || !string.IsNullOrEmpty(_selectedTeamName));
    }

    protected override void BuildBody()
    {
        _randomTabButton = UiBuilder.CreateButton(Body, "RandomTab", "전체 랜덤", 34f, true,
            () => SelectType(GoldenGloveCraftType.Random));
        UiBuilder.Place(_randomTabButton.GetComponent<RectTransform>(), 0f, 0f, 500f, 96f);

        _teamTabButton = UiBuilder.CreateButton(Body, "TeamTab", "팀 지정", 34f, false,
            () => SelectType(GoldenGloveCraftType.TeamSelect));
        UiBuilder.Place(_teamTabButton.GetComponent<RectTransform>(), 516f, 0f, 500f, 96f);

        _teamGrid = UiBuilder.CreateRect(Body, "TeamGrid");
        UiBuilder.PlaceRow(_teamGrid, 112f, 220f);

        for (int i = 0; i < TeamNames.Length; i++)
        {
            int index = i;
            int column = i % TeamColumns;
            int row = i / TeamColumns;

            _teamButtons[i] = UiBuilder.CreateButton(_teamGrid, "Team" + i, TeamNames[i], 30f, false,
                () => SelectTeam(index));
            UiBuilder.Place(_teamButtons[i].GetComponent<RectTransform>(),
                column * 204f, row * 104f, 196f, 96f);
        }

        TextMeshProUGUI costTitle = UiBuilder.CreateText(Body, "CostTitle", 34f, UiPalette.Text3);
        UiBuilder.PlaceRow(costTitle.rectTransform, 352f, 44f, 8f);
        costTitle.text = "제작 비용";

        _costText = UiBuilder.CreateText(Body, "Cost", 32f, UiPalette.Text2);
        UiBuilder.PlaceRow(_costText.rectTransform, 400f, 48f, 8f);

        _craftButton = UiBuilder.CreateButton(Body, "CraftButton", "제작", 36f, true, Craft);
        UiBuilder.PlaceRow(_craftButton.GetComponent<RectTransform>(), 464f, 110f);

        _resultText = UiBuilder.CreateText(Body, "Result", 34f, UiPalette.CardGoldenGlove);
        UiBuilder.PlaceRow(_resultText.rectTransform, 594f, 120f, 8f);
        _resultText.textWrappingMode = TextWrappingModes.Normal;
    }

    private void SelectType(GoldenGloveCraftType craftType)
    {
        _craftType = craftType;
        Refresh();
    }

    private void SelectTeam(int index)
    {
        _selectedTeamName = TeamNames[index];
        Refresh();
    }

    private void Craft()
    {
        if (GoldenGloveCraftManager.Instance == null)
            return;

        GoldenGloveCraftResult result = _craftType == GoldenGloveCraftType.Random
            ? GoldenGloveCraftManager.Instance.CraftRandom()
            : GoldenGloveCraftManager.Instance.CraftForTeam(_selectedTeamName);

        if (result == null)
        {
            Notify("제작에 실패했습니다 (재화·골글 카드 풀 확인)", false);
            Refresh();
            return;
        }

        _resultText.text = $"{result.TeamName}  {result.Name} 획득!";

        UiEffects.Pop(_resultText.transform, 0.68f, 0.36f);
        UiEffects.Flash(_resultText, UiPalette.Text1, 0.7f);
        UiEffects.Burst(_resultText.rectTransform, UiEffectKind.Craft);

        Notify($"{result.Name}을(를) 제작했습니다", true);
        Refresh();
    }

    private static string BuildCostText(CurrencyCost[] costs)
    {
        if (costs == null || costs.Length == 0)
            return "비용 정보가 없습니다";

        string text = string.Empty;

        for (int i = 0; i < costs.Length; i++)
        {
            int owned = CurrencyManager.Instance == null ? 0 : CurrencyManager.Instance.GetAmount(costs[i].Type);

            if (i > 0)
                text += "   ·   ";

            text += $"{GetCurrencyLabel(costs[i].Type)} {costs[i].Amount} (보유 {owned})";
        }

        return text;
    }

    private static bool CanAfford(CurrencyCost[] costs)
    {
        if (costs == null || costs.Length == 0 || CurrencyManager.Instance == null)
            return false;

        foreach (CurrencyCost cost in costs)
        {
            if (!CurrencyManager.Instance.CanAfford(cost.Type, cost.Amount))
                return false;
        }

        return true;
    }

    private static string GetCurrencyLabel(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.Gold => "골드",
            CurrencyType.Point => "포인트",
            CurrencyType.GoldenGlovePoint => "골글 포인트",
            CurrencyType.NormalTicket => "일반권",
            CurrencyType.SignatureTicket => "시그권",
            CurrencyType.TrainCard => "훈련 카드",
            CurrencyType.BreakthroughCard => "돌파 카드",
            _ => type.ToString()
        };
    }

    private static void SetTabSelected(Button button, bool selected)
    {
        Image background = button.GetComponent<Image>();

        background.color = selected ? UiPalette.ButtonPrimary : UiPalette.BgRow;
        background.sprite = UiSprites.Get(selected ? UiSprites.TabOn : UiSprites.ButtonNormal);

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();

        if (label != null)
            label.color = selected ? UiPalette.ButtonPrimaryText : UiPalette.Text2;
    }
}
