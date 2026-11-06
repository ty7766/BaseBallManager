using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 라인업 편성. 야수 9포지션 · 벤치 5 · 투수 SP5/RP5/CP1을 채운다.
/// 포지션·타순·중복 검사는 LineUpManager가 하고 화면은 결과만 보여준다
/// </summary>
public class LineUpScreen : UiScreenBehaviour
{
    private const float RowHeight = 104f;
    private const float RowGap = 8f;
    private const int MaxRowCount = 11;
    private const int HitterSlotCount = 9;
    private const int BenchSlotCount = 5;
    //벤치는 DH 와 함께 6장이 한 줄에 들어가야 해 야수 칸보다 작다
    private const float SmallCardWidth = 150f;
    //투수는 한 행 5장. 열 사이를 넓히려 야수 칸보다 조금 작다
    private const float PitcherCardWidth = 160f;
    private const float PitcherColumnGap = 44f;
    private const float PitcherRowGap = 50f;
    private const float FieldWidth = 1016f;
    //야수 영역 = 버튼 줄 아래 ~ 로비 버튼 위. 수비 배치 + 벤치 묶음을 이 안의 세로 가운데에 둔다
    private const float FieldTop = 320f;
    private const float FieldBottom = 180f;
    //수비 배치 칸 높이(C·DH 타순 버튼 아래 끝) · 벤치와의 간격 · 벤치 카드 사이
    private const float FormationHeight = 1070f;
    private const float BenchGap = 110f;
    private const float BenchCardGap = 30f;
    private const string FieldLinesSprite = "Lineup/lineup_field";
    //투수 배경 선 그림. 3행 묶음보다 위아래로 크게 깔아 세로가 긴 기기의 빈 곳을 채운다
    private const string PitcherLinesSprite = "Lineup/pitcher_field";
    private const float PitcherLinesHeight = 1300f;

    private static readonly HitterPosition[] HitterSlots =
    {
        HitterPosition.C, HitterPosition.FB, HitterPosition.SB, HitterPosition.TB,
        HitterPosition.SS, HitterPosition.LF, HitterPosition.CF, HitterPosition.RF, HitterPosition.DH
    };

    private static readonly string[] HitterSlotLabels =
    {
        "C", "1B", "2B", "3B", "SS", "LF", "CF", "RF", "DH"
    };

    //HitterSlots 순서대로 야구장 자리 (수비 배치 칸 좌상단 기준). CF 를 축으로 좌우대칭 5열 — 바깥 ±403 · 안쪽 ±183
    private static readonly Vector2[] HitterSlotPoints =
    {
        new Vector2(240f, 740f),  //C   홈 왼쪽 (SS 아래)
        new Vector2(826f, 545f),  //1B  오른쪽
        new Vector2(606f, 370f),  //2B  오른쪽 중간
        new Vector2(20f, 545f),   //3B  왼쪽
        new Vector2(240f, 370f),  //SS  왼쪽 중간
        new Vector2(20f, 70f),    //LF  좌익
        new Vector2(423f, 0f),    //CF  중견
        new Vector2(826f, 70f),   //RF  우익
        new Vector2(606f, 740f)   //DH  수비 자리가 없어 C 와 대칭인 홈 오른쪽 (2B 아래)
    };

    private const int DhSlotIndex = 8;

    private const float SlotCardWidth = 170f;
    private const float SlotLabelHeight = 34f;
    private const float SlotOrderHeight = 44f;
    private const float SlotGap = 4f;

    private Action _onBack;
    private Action<string, int, Func<CardDisplayInfo, bool>, Action<List<int>>> _onPickCard;
    private Action<string, bool> _onNotify;
    private Action<int> _onOpenDetail;

    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private Button _hitterTabButton;
    [SerializeField] private Button _pitcherTabButton;
    [SerializeField] private RectTransform _fieldArea;
    [SerializeField] private RectTransform _formation;
    [SerializeField] private RectTransform _pitcherArea;
    [SerializeField] private RectTransform _pitcherFormation;
    private readonly List<FieldSlot> _fieldSlots = new List<FieldSlot>(HitterSlotCount);
    private readonly List<FieldSlot> _benchSlots = new List<FieldSlot>(BenchSlotCount);
    private readonly List<FieldSlot> _pitcherSlots = new List<FieldSlot>(MaxRowCount);

    private Tab _tab = Tab.Hitter;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action onBack,
        Action<string, int, Func<CardDisplayInfo, bool>, Action<List<int>>> onPickCard,
        Action<string, bool> onNotify, Action<int> onOpenDetail)
    {
        _onBack = onBack;
        _onPickCard = onPickCard;
        _onNotify = onNotify;
        _onOpenDetail = onOpenDetail;
    }

    private enum Tab
    {
        Hitter,
        Pitcher
    }

    /// <summary>
    /// 현재 탭의 슬롯 상태를 다시 읽는다
    /// </summary>
    public void Refresh()
    {
        if (LineUpManager.Instance == null)
            return;

        List<int> duplicated = LineUpManager.Instance.GetDuplicatedBattingOrders();
        bool complete = LineUpManager.Instance.IsLineupComplete();

        if (duplicated.Count > 0)
        {
            _stateText.text = $"타순 {string.Join(" · ", duplicated)}번이 겹칩니다";
            _stateText.color = UiPalette.StateOut;
        }
        else
        {
            _stateText.text = complete ? "라인업 완성" : "비어 있는 슬롯이 있습니다";
            _stateText.color = complete ? UiPalette.StatePitching : UiPalette.StateOut;
        }

        UiTabStyle.Apply(_hitterTabButton, _tab == Tab.Hitter);
        UiTabStyle.Apply(_pitcherTabButton, _tab == Tab.Pitcher);

        bool hitterTab = _tab == Tab.Hitter;

        _fieldArea.gameObject.SetActive(hitterTab);
        _pitcherArea.gameObject.SetActive(!hitterTab);

        if (hitterTab)
            RefreshHitterField();
        else
            RefreshPitcherField();

        FitFormation();
    }

    protected override void BindEvents()
    {
        Bind(_hitterTabButton, () => SelectTab(Tab.Hitter));
        Bind(_pitcherTabButton, () => SelectTab(Tab.Pitcher));
        BindButton("AutoFill", AutoFill);
        BindButton("ClearAll", ClearAll);
        BindButton("BackButton", OnBackClicked);
    }

    protected override void Build()
    {
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 48f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, 32f, 32f, 500f, 60f);
        title.text = "라인업";

        _stateText = UiBuilder.CreateText(Root, "State", 32f, UiPalette.Text2, TextAlignmentOptions.Right);
        UiBuilder.PlaceRight(_stateText.rectTransform, 32f, 42f, 520f, 48f);

        _hitterTabButton = UiBuilder.CreateButton(Root, "HitterTab", "야수 9 · 벤치 5", 34f, true,
            () => SelectTab(Tab.Hitter));
        UiBuilder.Place(_hitterTabButton.GetComponent<RectTransform>(), 32f, 110f, 500f, 88f);

        _pitcherTabButton = UiBuilder.CreateButton(Root, "PitcherTab", "투수 11", 34f, false,
            () => SelectTab(Tab.Pitcher));
        UiBuilder.Place(_pitcherTabButton.GetComponent<RectTransform>(), 548f, 110f, 500f, 88f);

        Button autoFillButton = UiBuilder.CreateButton(Root, "AutoFill", "자동 편성", 34f, false, AutoFill);
        UiBuilder.Place(autoFillButton.GetComponent<RectTransform>(), 32f, 212f, 500f, 88f);

        Button clearButton = UiBuilder.CreateButton(Root, "ClearAll", "전체 비우기", 34f, false, ClearAll);
        UiBuilder.Place(clearButton.GetComponent<RectTransform>(), 548f, 212f, 500f, 88f);

        _fieldArea = UiBuilder.CreateRect(Root, "Field");
        UiBuilder.Stretch(_fieldArea, 32f, FieldTop, 32f, FieldBottom);

        float benchHeight = SlotLabelHeight + SlotGap + SmallCardWidth * CardView.BaseHeight / CardView.BaseWidth;

        _formation = CreateCenteredGroup(_fieldArea, "Formation", FormationHeight + BenchGap + benchHeight);

        Image fieldLines = UiBuilder.CreateImage(_formation, "FieldLines", Color.white, FieldLinesSprite);
        UiBuilder.Place(fieldLines.rectTransform, 0f, 0f, FieldWidth, FormationHeight);
        fieldLines.gameObject.SetActive(fieldLines.sprite != null);

        _pitcherArea = UiBuilder.CreateRect(Root, "PitcherField");
        UiBuilder.Stretch(_pitcherArea, 32f, FieldTop, 32f, FieldBottom);

        float pitcherSlotHeight = SlotLabelHeight + SlotGap + PitcherCardWidth * CardView.BaseHeight / CardView.BaseWidth;

        _pitcherFormation = CreateCenteredGroup(_pitcherArea, "PitcherFormation",
            pitcherSlotHeight * 3f + PitcherRowGap * 2f);

        Image pitcherLines = UiBuilder.CreateImage(_pitcherFormation, "PitcherLines", Color.white, PitcherLinesSprite);
        pitcherLines.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        pitcherLines.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        pitcherLines.rectTransform.sizeDelta = new Vector2(FieldWidth, PitcherLinesHeight);
        pitcherLines.rectTransform.anchoredPosition = Vector2.zero;
        pitcherLines.gameObject.SetActive(pitcherLines.sprite != null);

        Button backButton = UiBuilder.CreateButton(Root, "BackButton", "로비로", 34f, true, OnBackClicked);
        RectTransform backRect = backButton.GetComponent<RectTransform>();

        backRect.anchorMin = new Vector2(0f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.offsetMin = new Vector2(32f, 40f);
        backRect.offsetMax = new Vector2(-32f, 160f);
    }

    protected override void OnShown()
    {
        Refresh();
    }


    //야수 칸도 프리팹에 굽지 않고 런타임에 한 번만 만든다
    private void EnsureFieldSlots()
    {
        if (_fieldSlots.Count > 0)
            return;

        //DH 까지 포함해 9자리를 야구장 좌표대로 깐다
        for (int i = 0; i <= DhSlotIndex; i++)
        {
            _fieldSlots.Add(new FieldSlot(_formation, HitterSlotLabels[i],
                HitterSlotPoints[i].x, HitterSlotPoints[i].y, SlotCardWidth, OpenDetail));
        }

        //벤치 5장은 야구장 아래에 간격을 두고 가로 한 줄로
        float step = SmallCardWidth + BenchCardGap;
        float left = (FieldWidth - (BenchSlotCount * step - BenchCardGap)) * 0.5f;

        for (int i = 0; i < BenchSlotCount; i++)
        {
            _benchSlots.Add(new FieldSlot(_formation, $"벤치 {i + 1}",
                left + i * step, FormationHeight + BenchGap, SmallCardWidth, OpenDetail, false));
        }
    }

    //영역 세로 가운데에 고정하는 묶음. 기기 비율이 달라도 위아래 여백이 같다
    private static RectTransform CreateCenteredGroup(RectTransform parent, string name, float height)
    {
        RectTransform group = UiBuilder.CreateRect(parent, name);

        group.anchorMin = new Vector2(0.5f, 0.5f);
        group.anchorMax = new Vector2(0.5f, 0.5f);
        group.pivot = new Vector2(0.5f, 0.5f);
        group.anchoredPosition = Vector2.zero;
        group.sizeDelta = new Vector2(FieldWidth, height);

        return group;
    }

    //16:9 처럼 세로가 짧은 기기에서는 수비 배치 묶음을 영역 높이에 맞춰 통째로 줄인다 (긴 기기는 그대로)
    private void FitFormation()
    {
        float available = _fieldArea.rect.height;
        float needed = _formation.sizeDelta.y;
        float scale = available > 0f && needed > available ? available / needed : 1f;

        _formation.localScale = new Vector3(scale, scale, 1f);
    }

    //9포지션을 야구장 자리대로 깐다
    private void RefreshHitterField()
    {
        EnsureFieldSlots();

        List<int> duplicatedOrders = LineUpManager.Instance.GetDuplicatedBattingOrders();

        for (int i = 0; i < _fieldSlots.Count; i++)
        {
            HitterPosition slot = HitterSlots[i];
            (int instanceId, int battingOrder) = LineUpManager.Instance.GetHitterSlot(slot);

            _fieldSlots[i].SetSlot(instanceId, battingOrder,
                () => PickHitter(slot), () => ClearHitter(slot), order => ChangeOrder(slot, order),
                duplicatedOrders.Contains(battingOrder));
        }

        int[] bench = LineUpManager.Instance.GetBenchInstanceIds();

        for (int i = 0; i < _benchSlots.Count; i++)
        {
            int benchIndex = i;
            int instanceId = i < bench.Length ? bench[i] : InventoryManager.InvalidInstanceId;

            _benchSlots[i].SetSlot(instanceId, 0,
                () => PickBench(benchIndex), () => ClearBench(benchIndex), null);
        }
    }

    //투수는 1행 선발 5 · 2행 중계 5 · 3행 마무리 1(가운데)
    private void EnsurePitcherSlots()
    {
        if (_pitcherSlots.Count > 0)
            return;

        float rowStep = SlotLabelHeight + SlotGap + PitcherCardWidth * CardView.BaseHeight / CardView.BaseWidth
            + PitcherRowGap;

        AddPitcherRow("SP", 5, 0f);
        AddPitcherRow("RP", 5, rowStep);
        AddPitcherRow("CP", 1, rowStep * 2f);
    }

    //한 행을 가운데 정렬로 깐다
    private void AddPitcherRow(string label, int count, float top)
    {
        float step = PitcherCardWidth + PitcherColumnGap;
        float left = (FieldWidth - (count * step - PitcherColumnGap)) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            string slotLabel = count == 1 ? label : $"{label} {i + 1}";

            _pitcherSlots.Add(new FieldSlot(_pitcherFormation, slotLabel,
                left + i * step, top, PitcherCardWidth, OpenDetail, false));
        }
    }

    private void RefreshPitcherField()
    {
        EnsurePitcherSlots();

        int[] starters = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.SP);
        int[] relievers = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.RP);
        int[] closers = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.CP);

        int index = 0;

        index = FillPitcherRow(PitcherPosition.SP, starters, 5, index);
        index = FillPitcherRow(PitcherPosition.RP, relievers, 5, index);
        FillPitcherRow(PitcherPosition.CP, closers, 1, index);
    }

    private int FillPitcherRow(PitcherPosition position, int[] slots, int count, int index)
    {
        for (int i = 0; i < count && index < _pitcherSlots.Count; i++, index++)
        {
            int slotIndex = i;
            int instanceId = i < slots.Length ? slots[i] : InventoryManager.InvalidInstanceId;

            _pitcherSlots[index].SetSlot(instanceId, 0,
                () => PickPitcher(position, slotIndex), () => ClearPitcher(position, slotIndex), null);
        }

        return index;
    }




    private void PickHitter(HitterPosition slot)
    {
        _onPickCard?.Invoke($"{slot} 자리에 넣을 야수", 1,
            info => IsHitterAllowed(info, slot),
            ids => AssignHitter(slot, ids[0]));
    }

    private void PickBench(int benchIndex)
    {
        _onPickCard?.Invoke($"벤치 {benchIndex + 1}에 넣을 야수", 1,
            info => info.IsHitter && !LineUpManager.Instance.IsCardAssigned(info.InstanceId),
            ids => AssignBench(benchIndex, ids[0]));
    }

    private void PickPitcher(PitcherPosition position, int slotIndex)
    {
        _onPickCard?.Invoke($"{position} {slotIndex + 1}에 넣을 투수", 1,
            info => IsPitcherAllowed(info, position),
            ids => AssignPitcher(position, slotIndex, ids[0]));
    }

    //DH는 아무 야수나 가능하고 나머지는 포지션이 같아야 한다
    private static bool IsHitterAllowed(CardDisplayInfo info, HitterPosition slot)
    {
        if (!info.IsHitter || LineUpManager.Instance.IsCardAssigned(info.InstanceId))
            return false;

        if (slot == HitterPosition.DH)
            return true;

        return HitterPositionParser.TryParse(info.Position, out HitterPosition cardSlot) && cardSlot == slot;
    }

    private static bool IsPitcherAllowed(CardDisplayInfo info, PitcherPosition position)
    {
        if (info.IsHitter || LineUpManager.Instance.IsCardAssigned(info.InstanceId))
            return false;

        return PitcherPositionParser.TryParse(info.Position, out PitcherPosition cardPosition)
            && cardPosition == position;
    }

    private void AssignHitter(HitterPosition slot, int instanceId)
    {
        (int occupied, int _) = LineUpManager.Instance.GetHitterSlot(slot);

        if (occupied != LineUpManager.EmptySlot)
            LineUpManager.Instance.RemoveHitter(slot);

        int order = FindFreeBattingOrder();

        if (order == LineUpManager.EmptySlot)
        {
            _onNotify?.Invoke("빈 타순이 없습니다", false);
            Refresh();
            return;
        }

        if (!LineUpManager.Instance.AssignHitter(slot, instanceId, order))
            _onNotify?.Invoke("배치하지 못했습니다 (포지션·타순 확인)", false);

        Refresh();
    }

    private void AssignBench(int benchIndex, int instanceId)
    {
        if (LineUpManager.Instance.GetBenchInstanceIds()[benchIndex] != LineUpManager.EmptySlot)
            LineUpManager.Instance.RemoveBench(benchIndex);

        if (!LineUpManager.Instance.AssignBench(benchIndex, instanceId))
            _onNotify?.Invoke("벤치에 배치하지 못했습니다", false);

        Refresh();
    }

    private void AssignPitcher(PitcherPosition position, int slotIndex, int instanceId)
    {
        if (LineUpManager.Instance.GetPitcherInstanceIds(position)[slotIndex] != LineUpManager.EmptySlot)
            LineUpManager.Instance.RemovePitcher(position, slotIndex);

        if (!LineUpManager.Instance.AssignPitcher(position, slotIndex, instanceId))
            _onNotify?.Invoke("보직이 맞지 않습니다", false);

        Refresh();
    }

    //카드를 꾹 누르면 상세 보기 (뽑기 결과의 상세 보기와 같은 화면)
    private void OpenDetail(int instanceId)
    {
        _onOpenDetail?.Invoke(instanceId);
    }

    private void ClearHitter(HitterPosition slot)
    {
        LineUpManager.Instance.RemoveHitter(slot);
        Refresh();
    }

    private void ClearBench(int benchIndex)
    {
        LineUpManager.Instance.RemoveBench(benchIndex);
        Refresh();
    }

    private void ClearPitcher(PitcherPosition position, int slotIndex)
    {
        LineUpManager.Instance.RemovePitcher(position, slotIndex);
        Refresh();
    }

    private void ChangeOrder(HitterPosition slot, int order)
    {
        LineUpManager.Instance.SetBattingOrder(slot, order);
        Refresh();
    }

    //아직 아무도 쓰지 않은 타순 중 가장 작은 번호
    private static int FindFreeBattingOrder()
    {
        bool[] taken = new bool[LineUpManager.HitterSlotCount + 1];

        foreach (HitterPosition slot in HitterSlots)
        {
            (int instanceId, int order) = LineUpManager.Instance.GetHitterSlot(slot);

            if (instanceId != LineUpManager.EmptySlot && order >= 1 && order <= LineUpManager.HitterSlotCount)
                taken[order] = true;
        }

        for (int order = 1; order <= LineUpManager.HitterSlotCount; order++)
        {
            if (!taken[order])
                return order;
        }

        return LineUpManager.EmptySlot;
    }

    private void AutoFill()
    {
        if (LineUpAutoFill.Fill())
        {
            _onNotify?.Invoke("라인업을 자동으로 채웠습니다", true);
            Refresh();
            return;
        }

        _onNotify?.Invoke("보유 카드가 부족해 다 채우지 못했습니다", false);
        Refresh();
    }

    private void ClearAll()
    {
        LineUpManager.Instance.ClearAll();
        Refresh();
    }

    private void SelectTab(Tab tab)
    {
        _tab = tab;
        Refresh();
    }

    private void OnBackClicked()
    {
        _onBack?.Invoke();
    }

    //야구장 자리에 놓이는 야수 1칸. 카드 비율 그림 + 포지션 + 타순 조절
    private sealed class FieldSlot
    {
        //카드 오른쪽 아래 모서리에 겹치므로 이름을 덜 가리게 작게
        private const float ClearSize = 30f;
        private const float ClearFontSize = 20f;

        private readonly CardView _card;
        private readonly TextMeshProUGUI _orderText;
        private readonly Button _orderDownButton;
        private readonly Button _orderUpButton;
        private readonly Button _clearButton;

        private readonly Action<int> _onOpenDetail;

        private Action _onPick;
        private Action _onClear;
        private Action<int> _onOrderChanged;
        private int _battingOrder;

        private readonly bool _hasOrder;

        public FieldSlot(Transform parent, string label, float x, float y, float width,
            Action<int> onOpenDetail, bool withOrder = true)
        {
            _hasOrder = withOrder;
            _onOpenDetail = onOpenDetail;

            float cardHeight = width * CardView.BaseHeight / CardView.BaseWidth;
            float height = SlotLabelHeight + SlotGap + cardHeight
                + (withOrder ? SlotGap + SlotOrderHeight : 0f);

            RectTransform root = UiBuilder.CreateRect(parent, "Slot" + label);

            UiBuilder.Place(root, x, y, width, height);

            TextMeshProUGUI positionLabel = UiBuilder.CreateText(root, "Position", 30f,
                UiPalette.Text2, TextAlignmentOptions.Center);

            UiBuilder.Place(positionLabel.rectTransform, 0f, 0f, width, SlotLabelHeight);
            positionLabel.text = label;

            float cardTop = SlotLabelHeight + SlotGap;

            _card = CardView.Bake(root, width);
            UiBuilder.Place(_card.Root, 0f, cardTop, width, cardHeight);

            //한글 폰트에 ✕(U+2715) 글리프가 없어 네모로 깨진다. 기본 라틴 X 를 쓴다
            _clearButton = UiBuilder.CreateButton(root, "Clear", "X", ClearFontSize, false, OnClearClicked);
            UiBuilder.Place(_clearButton.GetComponent<RectTransform>(),
                width - ClearSize, cardTop + cardHeight - ClearSize, ClearSize, ClearSize);

            if (!withOrder)
                return;

            float orderTop = cardTop + cardHeight + SlotGap;

            _orderDownButton = UiBuilder.CreateButton(root, "OrderDown", "◀", 26f, false, DecreaseOrder);
            UiBuilder.Place(_orderDownButton.GetComponent<RectTransform>(), 0f, orderTop, 44f, SlotOrderHeight);

            _orderText = UiBuilder.CreateText(root, "Order", 30f, UiPalette.AccentGold,
                TextAlignmentOptions.Center);
            UiBuilder.Place(_orderText.rectTransform, 44f, orderTop, width - 88f, SlotOrderHeight);

            _orderUpButton = UiBuilder.CreateButton(root, "OrderUp", "▶", 26f, false, IncreaseOrder);
            UiBuilder.Place(_orderUpButton.GetComponent<RectTransform>(),
                width - 44f, orderTop, 44f, SlotOrderHeight);
        }

        /// <summary>
        /// 이 자리에 놓인 카드와 타순을 다시 그린다
        /// </summary>
        public void SetSlot(int instanceId, int battingOrder, Action onPick, Action onClear,
            Action<int> onOrderChanged, bool duplicated = false)
        {
            _onPick = onPick;
            _onClear = onClear;
            _onOrderChanged = onOrderChanged;
            _battingOrder = battingOrder;

            bool filled = CardDisplayInfo.TryCreate(instanceId, out CardDisplayInfo info);

            if (filled)
                _card.SetCard(info);
            else
                _card.SetEmpty("비어 있음");

            _card.SetClickHandler(onPick);
            _card.SetLongPressHandler(filled ? () => _onOpenDetail?.Invoke(instanceId) : null);

            _clearButton.gameObject.SetActive(filled);

            if (!_hasOrder)
                return;

            _orderDownButton.gameObject.SetActive(filled);
            _orderUpButton.gameObject.SetActive(filled);
            _orderText.text = filled ? $"{battingOrder}번" : string.Empty;
            _orderText.color = duplicated ? UiPalette.StateOut : UiPalette.AccentGold;
        }

        private void OnClearClicked()
        {
            _onClear?.Invoke();
        }

        //1번 아래와 9번 위는 없는 타순이라 아무 일도 하지 않는다
        private void DecreaseOrder()
        {
            if (_battingOrder <= 1)
                return;

            _onOrderChanged?.Invoke(_battingOrder - 1);
        }

        private void IncreaseOrder()
        {
            if (_battingOrder >= LineUpManager.HitterSlotCount)
                return;

            _onOrderChanged?.Invoke(_battingOrder + 1);
        }
    }

}
