using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 골든글러브 제작. 위는 만들어질 카드 자리(금색 칸 · ?), 아래는 제작 방식(전체 랜덤 / 팀 지정) · 팀 · 비용.
/// 카드를 재료로 쓰지 않으므로 카드 목록은 없다. 제작하면 빛이 차오른 뒤 칸에 새 카드가 나타난다
/// </summary>
public class GoldenGloveCraftScreen : UiScreenBehaviour
{
    private static readonly string[] TeamNames = { "기아", "삼성", "LG", "한화", "두산", "SSG", "KT", "NC", "키움", "롯데" };

    private const int TeamColumns = 5;
    private const int CostCount = 3;
    private const float SideMargin = 32f;
    private const float RowWidth = 1080f - SideMargin * 2f;
    //아래에서부터 쌓는다 — 버튼 · 비용 칩 · 방식별 내용 · 탭. 위 남는 칸 가운데에 카드 자리
    private const float ButtonBottom = 40f;
    private const float ButtonHeight = 120f;
    private const float CostBottom = 196f;
    private const float CostHeight = 76f;
    private const float ContentBottom = CostBottom + CostHeight + 24f;
    private const float ContentHeight = 300f;
    private const float TabBottom = ContentBottom + ContentHeight + 24f;
    private const float TabHeight = 96f;
    private const float TabInset = 8f;
    private const float StageTop = 120f;
    private const float StageBottom = TabBottom + TabHeight + 24f;
    private const float SlotWidth = 400f;
    private const float SlotHeight = SlotWidth * CardView.BaseHeight / CardView.BaseWidth;
    private const float SlotCornerScale = 0.5f;
    private const float BloomScale = 1.7f;
    private const float BloomRestAlpha = 0.35f;
    //팀 칸 5 × 2
    private const float TeamTileWidth = 188f;
    private const float TeamTileHeight = 136f;
    private const float TeamTileGap = 14f;
    private const float TeamPlateSize = 66f;
    private const float TeamLogoSize = 46f;
    private const float CostChipWidth = 320f;
    private const float CostChipGap = 18f;
    private const float CostIconSize = 52f;
    private const float ChargeSeconds = 0.6f;
    private const string TabTrackSprite = "Gacha/tab_track";
    private const string TabPillSprite = "Gacha/tab_pill";
    private const string BloomSprite = "Fx/card_bloom";
    private const string CardMaskSprite = "Fx/card_mask";
    private const string AdditiveMaterialPath = "UI/Materials/AdditiveLight";
    private const string TeamPlateSprite = "Common/press_disc";

    [SerializeField] private Image _bloom;
    [SerializeField] private RectTransform _slot;
    [SerializeField] private TextMeshProUGUI _unknownMark;
    [SerializeField] private CardView _resultCard;
    [SerializeField] private Image _slotFlash;
    [SerializeField] private TextMeshProUGUI _resultLabel;
    [SerializeField] private Button _randomTab;
    [SerializeField] private TextMeshProUGUI _randomTabLabel;
    [SerializeField] private Button _teamTab;
    [SerializeField] private TextMeshProUGUI _teamTabLabel;
    [SerializeField] private RectTransform _randomContent;
    [SerializeField] private RectTransform _teamContent;
    [SerializeField] private Button[] _teamTiles = new Button[TeamNames.Length];
    [SerializeField] private Image[] _teamFrames = new Image[TeamNames.Length];
    [SerializeField] private TextMeshProUGUI[] _teamLabels = new TextMeshProUGUI[TeamNames.Length];
    [SerializeField] private Image[] _costIcons = new Image[CostCount];
    [SerializeField] private TextMeshProUGUI[] _costAmounts = new TextMeshProUGUI[CostCount];
    [SerializeField] private TextMeshProUGUI[] _costOwned = new TextMeshProUGUI[CostCount];
    [SerializeField] private RectTransform[] _costChips = new RectTransform[CostCount];
    [SerializeField] private Button _lobbyButton;
    [SerializeField] private Button _craftButton;
    [SerializeField] private CanvasGroup _craftGroup;
    [SerializeField] private Image _inputBlocker;

    private Action _onBack;
    private Action<string, bool> _onNotify;
    private Action<int> _onShowCardDetail;
    private GoldenGloveCraftType _craftType = GoldenGloveCraftType.Random;
    private string _selectedTeam;
    private int _resultInstanceId = InventoryManager.InvalidInstanceId;
    private Coroutine _craftRoutine;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action onBack, Action<string, bool> onNotify, Action<int> onShowCardDetail)
    {
        _onBack = onBack;
        _onNotify = onNotify;
        _onShowCardDetail = onShowCardDetail;
    }

    /// <summary>
    /// 방식 · 팀 · 비용 · 결과 칸을 다시 그린다
    /// </summary>
    public void Refresh()
    {
        bool teamMode = _craftType == GoldenGloveCraftType.TeamSelect;

        SetTabSelected(_randomTab, _randomTabLabel, !teamMode);
        SetTabSelected(_teamTab, _teamTabLabel, teamMode);
        _randomContent.gameObject.SetActive(!teamMode);
        _teamContent.gameObject.SetActive(teamMode);

        for (int i = 0; i < TeamNames.Length; i++)
        {
            bool selected = TeamNames[i] == _selectedTeam;

            _teamFrames[i].color = selected ? UiPalette.AccentGold : UiPalette.LineBorder;
            _teamLabels[i].color = selected ? UiPalette.AccentGold : UiPalette.Text2;
        }

        bool affordable = RefreshCosts();
        bool ready = affordable && (!teamMode || !string.IsNullOrEmpty(_selectedTeam));

        _craftButton.interactable = ready;
        _craftGroup.alpha = ready ? 1f : 0.4f;

        RefreshResult();
    }

    /// <summary>
    /// 연출 중에는 뒤로가기를 막는다
    /// </summary>
    public override bool HandleBack()
    {
        return _craftRoutine != null;
    }

    protected override void BindEvents()
    {
        WireWidgets();
        Bind(_randomTab, SelectRandom);
        Bind(_teamTab, SelectTeamMode);
        BindEach(_teamTiles, SelectTeam);
        Bind(_lobbyButton, OnLobbyClicked);
        Bind(_craftButton, OnCraftClicked);
    }

    //제목 · 카드 자리(빛 · 금색 칸 · ? · 결과 카드 · 이름) · 탭 · 방식별 내용 · 비용 칩 · 아래 버튼 · 입력 막이
    protected override void Build()
    {
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 48f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, SideMargin, 32f, 700f, 60f);
        title.text = "골든글러브 제작";

        BuildStage();
        BuildTabs();
        BuildRandomContent();
        BuildTeamContent();
        BuildCosts();

        _lobbyButton = UiBuilder.CreateButton(Root, "BackButton", "로비로", 34f, false, OnLobbyClicked);
        PlaceBottomButton(_lobbyButton, 0f, 0.5f);

        _craftButton = UiBuilder.CreateButton(Root, "CraftButton", "제작", 36f, true, OnCraftClicked);
        _craftButton.targetGraphic.color = UiPalette.AccentGold;
        PlaceBottomButton(_craftButton, 0.5f, 1f);
        _craftGroup = _craftButton.gameObject.AddComponent<CanvasGroup>();

        _inputBlocker = UiBuilder.CreateImage(Root, "InputBlocker", UiPalette.WithAlpha(Color.black, 0f));
        UiBuilder.Stretch(_inputBlocker.rectTransform, 0f, 0f, 0f, 0f);
        _inputBlocker.raycastTarget = true;
        _inputBlocker.gameObject.SetActive(false);

        WireWidgets();
    }

    protected override void OnShown()
    {
        Refresh();
    }

    //다시 들어오면 전체 랜덤 · 팀 없음 · 빈 칸
    protected override void OnHidden()
    {
        _craftRoutine = null;
        _inputBlocker.gameObject.SetActive(false);
        _craftType = GoldenGloveCraftType.Random;
        _selectedTeam = null;
        _resultInstanceId = InventoryManager.InvalidInstanceId;
    }

    //위 남는 칸 가운데: 뒤의 금빛 번짐 · 금색 테두리 칸 · ? · 결과 카드 · 번쩍임, 칸 아래 이름
    private void BuildStage()
    {
        RectTransform stage = UiBuilder.CreateRect(Root, "Stage");
        UiBuilder.Stretch(stage, 0f, StageTop, 0f, StageBottom);

        _bloom = UiBuilder.CreateImage(stage, "Bloom", UiPalette.WithAlpha(UiPalette.AccentGold, BloomRestAlpha), BloomSprite);
        _bloom.material = Resources.Load<Material>(AdditiveMaterialPath);
        PlaceCentered(_bloom.rectTransform, SlotWidth * BloomScale, SlotHeight * BloomScale, 0f);

        Image slot = UiBuilder.CreateImage(stage, "Slot", UiPalette.BgPanel, UiSprites.ButtonPrimary, Image.Type.Sliced);
        slot.pixelsPerUnitMultiplier = SlotCornerScale;
        _slot = slot.rectTransform;
        PlaceCentered(_slot, SlotWidth, SlotHeight, 0f);

        Image frame = UiBuilder.CreateImage(_slot, "Frame", UiPalette.WithAlpha(UiPalette.AccentGold, 0.8f),
            UiSprites.ButtonNormal, Image.Type.Sliced);
        frame.pixelsPerUnitMultiplier = SlotCornerScale;
        UiBuilder.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);

        _unknownMark = UiBuilder.CreateText(_slot, "Unknown", 200f, UiPalette.WithAlpha(UiPalette.AccentGold, 0.85f),
            TextAlignmentOptions.Center);
        UiBuilder.Stretch(_unknownMark.rectTransform, 0f, 0f, 0f, 0f);
        _unknownMark.text = "?";

        _resultCard = CardView.Bake(stage, SlotWidth, false);
        PlaceCentered(_resultCard.Root, SlotWidth, SlotHeight, 0f);
        _resultCard.Root.gameObject.SetActive(false);

        _slotFlash = UiBuilder.CreateImage(stage, "Flash", UiPalette.WithAlpha(Color.white, 0f), CardMaskSprite);
        PlaceCentered(_slotFlash.rectTransform, SlotWidth, SlotHeight, 0f);

        _resultLabel = UiBuilder.CreateText(stage, "ResultLabel", 34f, UiPalette.Text3, TextAlignmentOptions.Center);
        PlaceCentered(_resultLabel.rectTransform, RowWidth, 52f, -(SlotHeight * 0.5f + 40f));
    }

    //뽑기와 같은 알약 탭 두 칸
    private void BuildTabs()
    {
        RectTransform row = CreateBottomRow("Tabs", TabBottom, TabHeight);
        Image track = UiBuilder.CreateImage(row, "TabTrack", Color.white, TabTrackSprite, Image.Type.Sliced);
        UiBuilder.Stretch(track.rectTransform, 0f, 0f, 0f, 0f);

        float half = RowWidth * 0.5f;

        _randomTab = CreateTab(row, "RandomTab", "전체 랜덤", 0f, half, SelectRandom, out _randomTabLabel);
        _teamTab = CreateTab(row, "TeamTab", "팀 지정", half, half, SelectTeamMode, out _teamTabLabel);
    }

    private static Button CreateTab(RectTransform row, string name, string label, float left, float width,
        UnityAction onClick, out TextMeshProUGUI labelText)
    {
        Button tab = UiBuilder.CreateButton(row, name, label, 34f, false, onClick, out labelText);
        Image pill = (Image)tab.targetGraphic;

        pill.sprite = UiSprites.Get(TabPillSprite);
        pill.type = Image.Type.Sliced;
        UiBuilder.Place(tab.GetComponent<RectTransform>(), left + TabInset, TabInset, width - TabInset * 2f,
            TabHeight - TabInset * 2f);

        return tab;
    }

    private void BuildRandomContent()
    {
        _randomContent = CreateBottomRow("RandomContent", ContentBottom, ContentHeight);

        TextMeshProUGUI main = UiBuilder.CreateText(_randomContent, "Main", 34f, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.Place(main.rectTransform, 0f, ContentHeight * 0.5f - 52f, RowWidth, 52f);
        main.text = "전체 골든글러브 카드 중 무작위 1장";

        TextMeshProUGUI sub = UiBuilder.CreateText(_randomContent, "Sub", 26f, UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Place(sub.rectTransform, 0f, ContentHeight * 0.5f + 8f, RowWidth, 40f);
        sub.text = "팀을 정하려면 팀 지정 (비용이 더 듭니다)";
    }

    //흰 원판 위 로고 + 이름 타일 5 × 2. 고른 팀은 금색 테두리
    private void BuildTeamContent()
    {
        _teamContent = CreateBottomRow("TeamContent", ContentBottom, ContentHeight);

        float gridWidth = TeamColumns * TeamTileWidth + (TeamColumns - 1) * TeamTileGap;
        float gridLeft = (RowWidth - gridWidth) * 0.5f;
        float gridTop = (ContentHeight - (TeamTileHeight * 2f + TeamTileGap)) * 0.5f;

        for (int i = 0; i < TeamNames.Length; i++)
        {
            int index = i;
            float left = gridLeft + i % TeamColumns * (TeamTileWidth + TeamTileGap);
            float top = gridTop + i / TeamColumns * (TeamTileHeight + TeamTileGap);
            Button tile = UiBuilder.CreateButton(_teamContent, "Team", TeamNames[i], 26f, false, () => SelectTeam(index),
                out TextMeshProUGUI label);

            UiBuilder.Place(tile.GetComponent<RectTransform>(), left, top, TeamTileWidth, TeamTileHeight);
            UiBuilder.Place(label.rectTransform, 0f, TeamTileHeight - 44f, TeamTileWidth, 36f);

            Image plate = UiBuilder.CreateImage(tile.transform, "LogoPlate", UiPalette.Text1, TeamPlateSprite);
            UiBuilder.Place(plate.rectTransform, (TeamTileWidth - TeamPlateSize) * 0.5f, 14f, TeamPlateSize, TeamPlateSize);

            Image logo = UiBuilder.CreateImage(plate.transform, "Logo", Color.white, "Team/logo_" + TeamNames[i]);
            UiBuilder.Place(logo.rectTransform, (TeamPlateSize - TeamLogoSize) * 0.5f, (TeamPlateSize - TeamLogoSize) * 0.5f,
                TeamLogoSize, TeamLogoSize);
            logo.preserveAspect = true;
            logo.gameObject.SetActive(logo.sprite != null);

            _teamTiles[i] = tile;
            _teamFrames[i] = (Image)tile.targetGraphic;
            _teamLabels[i] = label;
        }
    }

    //골글 포인트 · 포인트 · 훈련 카드 — 아이콘 · 드는 양 · 보유량
    private void BuildCosts()
    {
        RectTransform row = CreateBottomRow("Costs", CostBottom, CostHeight);
        float chipsWidth = CostCount * CostChipWidth + (CostCount - 1) * CostChipGap;
        float left = (RowWidth - chipsWidth) * 0.5f;

        for (int i = 0; i < CostCount; i++)
        {
            Image plate = UiBuilder.CreateImage(row, "CostChip", UiPalette.BgRow, UiSprites.ButtonPrimary, Image.Type.Sliced);
            _costChips[i] = plate.rectTransform;
            UiBuilder.Place(_costChips[i], left + i * (CostChipWidth + CostChipGap), 0f, CostChipWidth, CostHeight);

            _costIcons[i] = UiBuilder.CreateImage(plate.transform, "Icon", Color.white);
            UiBuilder.Place(_costIcons[i].rectTransform, 14f, (CostHeight - CostIconSize) * 0.5f, CostIconSize, CostIconSize);
            _costIcons[i].preserveAspect = true;

            _costAmounts[i] = UiBuilder.CreateText(plate.transform, "Amount", 30f, UiPalette.Text1);
            UiBuilder.Place(_costAmounts[i].rectTransform, 80f, 6f, CostChipWidth - 92f, 36f);

            _costOwned[i] = UiBuilder.CreateText(plate.transform, "Owned", 22f, UiPalette.Text3);
            UiBuilder.Place(_costOwned[i].rectTransform, 80f, 42f, CostChipWidth - 92f, 26f);
        }
    }

    //화면 폭 · 아래에서 bottom 만큼 떨어진 줄. 안쪽은 Place 로 왼쪽 위 기준
    private RectTransform CreateBottomRow(string name, float bottom, float height)
    {
        RectTransform row = UiBuilder.CreateRect(Root, name);

        row.anchorMin = new Vector2(0f, 0f);
        row.anchorMax = new Vector2(1f, 0f);
        row.pivot = new Vector2(0.5f, 0f);
        row.offsetMin = new Vector2(SideMargin, bottom);
        row.offsetMax = new Vector2(-SideMargin, bottom + height);

        return row;
    }

    private static void PlaceCentered(RectTransform rect, float width, float height, float offsetY)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, offsetY);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void PlaceBottomButton(Button button, float anchorLeft, float anchorRight)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        float leftInset = anchorLeft > 0f ? 12f : SideMargin;
        float rightInset = anchorRight < 1f ? 12f : SideMargin;

        rect.anchorMin = new Vector2(anchorLeft, 0f);
        rect.anchorMax = new Vector2(anchorRight, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(leftInset, ButtonBottom);
        rect.offsetMax = new Vector2(-rightInset, ButtonBottom + ButtonHeight);
    }

    //프리팹에 저장되지 않는 콜백
    private void WireWidgets()
    {
        _resultCard.SetLongPressHandler(OnResultLongPressed);
    }

    private static void SetTabSelected(Button tab, TextMeshProUGUI label, bool selected)
    {
        tab.targetGraphic.color = selected ? Color.white : UiPalette.WithAlpha(Color.white, 0f);
        label.color = selected ? UiPalette.ButtonPrimaryText : UiPalette.Text2;
    }

    //방식별 비용 칩. 모자란 재화는 빨간 글자. 다 있으면 true
    private bool RefreshCosts()
    {
        CurrencyCost[] costs = GoldenGloveCraftManager.Instance == null
            ? null
            : GoldenGloveCraftManager.Instance.GetCost(_craftType);
        bool affordable = costs != null && costs.Length > 0;

        for (int i = 0; i < CostCount; i++)
        {
            bool shown = costs != null && i < costs.Length;

            _costChips[i].gameObject.SetActive(shown);

            if (!shown)
                continue;

            int owned = CurrencyManager.Instance == null ? 0 : CurrencyManager.Instance.GetAmount(costs[i].Type);
            bool enough = owned >= costs[i].Amount;

            _costIcons[i].sprite = UiSprites.Get(GetCurrencyIconPath(costs[i].Type));
            _costAmounts[i].text = costs[i].Amount.ToString("N0");
            _costAmounts[i].color = enough ? UiPalette.Text1 : UiPalette.StateOut;
            _costOwned[i].text = $"보유 {owned:N0}";
            affordable &= enough;
        }

        return affordable;
    }

    //방금 만든 카드가 있으면 칸에 놓고 이름을, 없으면 ? 와 안내
    private void RefreshResult()
    {
        bool hasResult = CardDisplayInfo.TryCreate(_resultInstanceId, out CardDisplayInfo info);

        _unknownMark.gameObject.SetActive(!hasResult);
        _resultCard.Root.gameObject.SetActive(hasResult);

        if (hasResult)
        {
            _resultCard.SetCard(info);
            _resultLabel.text = $"{info.TeamName}  {PlayerLabelFormat.NameWithYear(info.Name, info.Year)}";
            _resultLabel.color = UiPalette.AccentGold;
            return;
        }

        _resultLabel.text = "골든글러브 카드 1장을 만듭니다";
        _resultLabel.color = UiPalette.Text3;
    }

    private void SelectRandom()
    {
        _craftType = GoldenGloveCraftType.Random;
        Refresh();
    }

    private void SelectTeamMode()
    {
        _craftType = GoldenGloveCraftType.TeamSelect;
        Refresh();
    }

    private void SelectTeam(int index)
    {
        _selectedTeam = TeamNames[index];
        Refresh();
        UiEffects.Pop(_teamTiles[index].transform, 0.92f, 0.2f);
    }

    private void OnCraftClicked()
    {
        if (_craftRoutine != null || GoldenGloveCraftManager.Instance == null)
            return;

        GoldenGloveCraftResult result = _craftType == GoldenGloveCraftType.Random
            ? GoldenGloveCraftManager.Instance.CraftRandom()
            : GoldenGloveCraftManager.Instance.CraftForTeam(_selectedTeam);

        if (result == null)
        {
            _onNotify?.Invoke("제작하지 못했습니다 (재화 · 골글 카드 풀 확인)", false);
            Refresh();
            return;
        }

        _craftRoutine = StartCoroutine(PlayCraft(result.InstanceId));
    }

    //금빛이 차오르며 칸이 떨리다 번쩍이고, 새 카드가 튀어나온다
    private IEnumerator PlayCraft(int instanceId)
    {
        _inputBlocker.gameObject.SetActive(true);
        _resultInstanceId = InventoryManager.InvalidInstanceId;
        RefreshResult();

        RectTransform bloom = _bloom.rectTransform;
        Vector2 slotPosition = _slot.anchoredPosition;

        UiAnimator.Play(ChargeSeconds, t =>
        {
            float scale = 1f + 0.3f * t;

            bloom.localScale = new Vector3(scale, scale, 1f);
            _bloom.color = UiPalette.WithAlpha(UiPalette.AccentGold, Mathf.Lerp(BloomRestAlpha, 1f, t));
            _slot.anchoredPosition = slotPosition + new Vector2(Mathf.Sin(t * 60f) * 6f * t, 0f);
        }, () => _slot.anchoredPosition = slotPosition, UiEase.InCubic);

        yield return new WaitForSecondsRealtime(ChargeSeconds);

        _resultInstanceId = instanceId;
        RefreshResult();
        UiEffects.Flash(_slotFlash, UiPalette.WithAlpha(Color.white, 0.9f), 0.5f);
        UiEffects.Pop(_resultCard.Root, 0.6f, 0.4f);
        UiEffects.Pop(_resultLabel.transform, 0.7f, 0.3f);
        UiEffects.Burst(_resultCard.Root, UiEffectKind.Craft);

        UiAnimator.Play(0.6f, t =>
        {
            float scale = 1.3f - 0.3f * t;

            bloom.localScale = new Vector3(scale, scale, 1f);
            _bloom.color = UiPalette.WithAlpha(UiPalette.AccentGold, Mathf.Lerp(1f, BloomRestAlpha, t));
        }, null, UiEase.OutCubic);

        _onNotify?.Invoke("골든글러브 카드를 만들었습니다", true);

        yield return new WaitForSecondsRealtime(0.6f);

        _craftRoutine = null;
        _inputBlocker.gameObject.SetActive(false);
        Refresh();
    }

    private void OnResultLongPressed()
    {
        if (_resultInstanceId != InventoryManager.InvalidInstanceId)
            _onShowCardDetail?.Invoke(_resultInstanceId);
    }

    private void OnLobbyClicked()
    {
        _onBack?.Invoke();
    }

    private static string GetCurrencyIconPath(CurrencyType type)
    {
        return type switch
        {
            CurrencyType.GoldenGlovePoint => "Currency/currency_goldenglove_point",
            CurrencyType.Point => "Currency/currency_point",
            CurrencyType.TrainCard => "Currency/currency_train_card",
            CurrencyType.Gold => "Currency/currency_gold",
            _ => null
        };
    }
}
