using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 필터 창. 선수 · 등급 · 종류 · 팀을 체크박스로 골라 조합한다 (팀은 로고 + 이름 타일).
/// 고르는 동안은 사본을 바꾸고, 적용을 눌러야 대상 필터에 옮긴다. 바깥을 누르거나 닫으면 바꾼 것을 버린다
/// </summary>
public class CardFilterPanel : UiWidgetBehaviour
{
    private const float WindowWidth = 960f;
    private const float Padding = 40f;
    private const float ContentWidth = WindowWidth - Padding * 2f;
    private const float TitleTop = 36f;
    private const float TitleHeight = 64f;
    private const float HeaderHeight = 44f;
    private const float HeaderGap = 4f;
    private const float SectionGap = 28f;
    private const float ChipHeight = 84f;
    private const float ChipGap = 16f;
    private const int ChipColumns = 3;
    private const float CheckSize = 40f;
    private const float TeamTileHeight = 132f;
    private const int TeamColumns = 5;
    //로고 중에는 검은 그림(KT 등)이 있어 어두운 타일에서 사라진다. 흰 원판 위에 얹는다
    private const float TeamLogoSize = 50f;
    private const float TeamPlateSize = 70f;
    private const string TeamPlateSprite = "Common/press_disc";
    private const float FooterHeight = 100f;
    private const string CheckOffSprite = "Common/checkbox_off";
    private const string CheckOnSprite = "Common/checkbox_on";

    private static readonly string[] TeamNames = { "기아", "삼성", "LG", "한화", "두산", "SSG", "KT", "NC", "키움", "롯데" };

    //체크박스 하나. 켜졌는지 묻고 · 바꾸는 동작을 함께 들고 있다
    private sealed class FilterOption
    {
        public Image Frame;
        public Image Check;
        public TextMeshProUGUI Label;
        public Func<bool> IsOn;
    }

    private readonly CardFilter _working = new CardFilter();
    private readonly List<FilterOption> _options = new List<FilterOption>(24);

    private CanvasGroup _fade;
    private RectTransform _window;
    private CardFilter _target;
    private Action _onApplied;

    /// <summary>
    /// 화면 위에 덮을 필터 창을 만든다. 처음엔 닫혀 있다
    /// </summary>
    public static CardFilterPanel Create(Transform parent)
    {
        CardFilterPanel panel = CreateNode<CardFilterPanel>(parent, "CardFilterPanel");

        panel.BuildPanel();
        panel.SetVisible(false);

        return panel;
    }

    /// <summary>
    /// 지금 필터를 사본으로 받아 연다. 적용하면 target 에 옮기고 onApplied 를 부른다
    /// </summary>
    public void Open(CardFilter target, Action onApplied)
    {
        _target = target;
        _onApplied = onApplied;
        _working.CopyFrom(target);

        Root.SetAsLastSibling();
        SetVisible(true);
        RefreshOptions();

        UiEffects.FadeIn(_fade, 0.16f);
        UiEffects.Pop(_window, 0.94f, 0.22f);
    }

    private void Apply()
    {
        _target.CopyFrom(_working);
        Close();
        _onApplied?.Invoke();
    }

    private void ResetAll()
    {
        _working.Clear();
        RefreshOptions();
    }

    private void Close()
    {
        SetVisible(false);
    }

    //켜진 칸은 흰 테두리 + 체크 + 밝은 글자, 꺼진 칸은 흐린 테두리
    private void RefreshOptions()
    {
        foreach (FilterOption option in _options)
        {
            bool on = option.IsOn();

            option.Check.sprite = UiSprites.Get(on ? CheckOnSprite : CheckOffSprite);
            option.Check.color = on ? Color.white : UiPalette.Text3;
            option.Frame.color = on ? UiPalette.Text1 : UiPalette.LineBorder;
            option.Label.color = on ? UiPalette.Text1 : UiPalette.Text2;
        }
    }

    //Dim(누르면 닫힘) > Window > [제목 · 닫기, 선수 · 등급 · 종류 체크박스 줄, 팀 타일 2줄, 초기화 · 적용]
    private void BuildPanel()
    {
        UiBuilder.Stretch(Root, 0f, 0f, 0f, 0f);
        _fade = gameObject.AddComponent<CanvasGroup>();

        Image dim = UiBuilder.CreateImage(Root, "Dim", UiPalette.WithAlpha(UiPalette.BgApp, 0.86f));
        UiBuilder.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
        dim.raycastTarget = true;

        Button dimButton = dim.gameObject.AddComponent<Button>();
        dimButton.transition = Selectable.Transition.None;
        dimButton.onClick.AddListener(Close);

        Image window = UiBuilder.CreateImage(Root, "Window", UiPalette.BgPanel, UiSprites.PanelLarge, Image.Type.Sliced);
        _window = window.rectTransform;
        _window.anchorMin = new Vector2(0.5f, 0.5f);
        _window.anchorMax = new Vector2(0.5f, 0.5f);
        _window.pivot = new Vector2(0.5f, 0.5f);
        window.raycastTarget = true;

        TextMeshProUGUI title = UiBuilder.CreateText(_window, "Title", 44f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, Padding, TitleTop, 500f, TitleHeight);
        title.text = "필터";

        Button closeButton = UiBuilder.CreateButton(_window, "CloseButton", "닫기", 30f, false, Close);
        UiBuilder.PlaceRight(closeButton.GetComponent<RectTransform>(), Padding, TitleTop, 140f, 72f);

        float top = TitleTop + TitleHeight + 12f;

        top = AddChipSection("선수", top, new[] { "타자", "투수" },
            new[] { PlayerTypeFilter.HitterOnly, PlayerTypeFilter.PitcherOnly }, _working.PlayerTypes);
        top = AddChipSection("등급", top, new[] { "3성", "4성", "5성" },
            new[] { CardGrade.Star3, CardGrade.Star4, CardGrade.Star5 }, _working.Grades);
        top = AddChipSection("종류", top, new[] { "노말", "시그니쳐", "골든글러브" },
            new[] { CardType.Normal, CardType.Signature, CardType.GoldenGlove }, _working.Types);
        top = AddTeamSection(top);

        float footerWidth = (ContentWidth - ChipGap) * 0.5f;

        Button resetButton = UiBuilder.CreateButton(_window, "ResetButton", "초기화", 34f, false, ResetAll);
        UiBuilder.Place(resetButton.GetComponent<RectTransform>(), Padding, top, footerWidth, FooterHeight);

        Button applyButton = UiBuilder.CreateButton(_window, "ApplyButton", "적용", 34f, true, Apply);
        UiBuilder.Place(applyButton.GetComponent<RectTransform>(), Padding + footerWidth + ChipGap, top,
            footerWidth, FooterHeight);

        _window.sizeDelta = new Vector2(WindowWidth, top + FooterHeight + Padding);
    }

    //제목 + 체크박스 칸 한 줄. 누르면 그 값을 넣었다 뺐다 한다. 다음 항목이 시작할 높이를 돌려준다
    private float AddChipSection<T>(string title, float top, string[] labels, T[] values, HashSet<T> selected)
    {
        float chipTop = AddHeader(title, top);
        float chipWidth = (ContentWidth - ChipGap * (ChipColumns - 1)) / ChipColumns;

        for (int i = 0; i < values.Length; i++)
        {
            T value = values[i];
            Button chip = UiBuilder.CreateButton(_window, "Option", labels[i], 30f, false,
                () => Toggle(selected, value), out TextMeshProUGUI chipLabel);
            RectTransform chipRect = chip.GetComponent<RectTransform>();

            UiBuilder.Place(chipRect, Padding + i * (chipWidth + ChipGap), chipTop, chipWidth, ChipHeight);

            FilterOption option = CreateOption(chip, chipLabel, () => selected.Contains(value));
            RectTransform check = option.Check.rectTransform;

            UiBuilder.Place(check, 20f, (ChipHeight - CheckSize) * 0.5f, CheckSize, CheckSize);
            UiBuilder.Place(option.Label.rectTransform, 20f + CheckSize + 12f, 0f, chipWidth - CheckSize - 40f, ChipHeight);
            option.Label.alignment = TextAlignmentOptions.MidlineLeft;
        }

        return chipTop + ChipHeight + SectionGap;
    }

    //팀은 흰 원판 위 로고 + 이름 타일 5열 2줄. 왼쪽 위 체크박스
    private float AddTeamSection(float top)
    {
        float tileTop = AddHeader("팀", top);
        float tileWidth = (ContentWidth - ChipGap * (TeamColumns - 1)) / TeamColumns;

        for (int i = 0; i < TeamNames.Length; i++)
        {
            string team = TeamNames[i];
            float x = Padding + i % TeamColumns * (tileWidth + ChipGap);
            float y = tileTop + i / TeamColumns * (TeamTileHeight + ChipGap);
            Button tile = UiBuilder.CreateButton(_window, "Team", team, 26f, false,
                () => Toggle(_working.TeamNames, team), out TextMeshProUGUI teamLabel);

            UiBuilder.Place(tile.GetComponent<RectTransform>(), x, y, tileWidth, TeamTileHeight);

            FilterOption option = CreateOption(tile, teamLabel, () => _working.TeamNames.Contains(team));

            UiBuilder.Place(option.Check.rectTransform, 10f, 10f, 32f, 32f);
            UiBuilder.Place(option.Label.rectTransform, 0f, TeamTileHeight - 46f, tileWidth, 38f);

            Image plate = UiBuilder.CreateImage(tile.transform, "LogoPlate", UiPalette.Text1, TeamPlateSprite);
            UiBuilder.Place(plate.rectTransform, (tileWidth - TeamPlateSize) * 0.5f, 14f, TeamPlateSize, TeamPlateSize);

            Image logo = UiBuilder.CreateImage(plate.transform, "Logo", Color.white, "Team/logo_" + team);
            UiBuilder.Place(logo.rectTransform, (TeamPlateSize - TeamLogoSize) * 0.5f, (TeamPlateSize - TeamLogoSize) * 0.5f,
                TeamLogoSize, TeamLogoSize);
            logo.preserveAspect = true;
            logo.gameObject.SetActive(logo.sprite != null);
        }

        int rows = (TeamNames.Length + TeamColumns - 1) / TeamColumns;

        return tileTop + rows * (TeamTileHeight + ChipGap) - ChipGap + SectionGap;
    }

    private float AddHeader(string title, float top)
    {
        TextMeshProUGUI header = UiBuilder.CreateText(_window, "Header", 28f, UiPalette.Text3);
        UiBuilder.Place(header.rectTransform, Padding, top, ContentWidth, HeaderHeight);
        header.text = title;

        return top + HeaderHeight + HeaderGap;
    }

    //버튼 바탕(테두리) · 체크 그림 · 글자를 한 칸으로 묶는다. 바탕은 체크 상태에 따라 테두리 색만 바뀐다
    private FilterOption CreateOption(Button button, TextMeshProUGUI label, Func<bool> isOn)
    {
        Image frame = (Image)button.targetGraphic;
        Image check = UiBuilder.CreateImage(button.transform, "Check", Color.white, CheckOffSprite);

        FilterOption option = new FilterOption { Frame = frame, Check = check, Label = label, IsOn = isOn };

        _options.Add(option);

        return option;
    }

    private void Toggle<T>(HashSet<T> selected, T value)
    {
        if (!selected.Remove(value))
            selected.Add(value);

        RefreshOptions();
    }
}
