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
    //야수 칸보다 작게 깔아 벤치 5장과 투수 11장이 한 화면에 들어오게 한다
    private const float SmallCardWidth = 150f;
    private const float BenchTop = 1090f;
    private const float FieldWidth = 1016f;
    private const float PitcherRowStep = 280f;

    private static readonly HitterPosition[] HitterSlots =
    {
        HitterPosition.C, HitterPosition.FB, HitterPosition.SB, HitterPosition.TB,
        HitterPosition.SS, HitterPosition.LF, HitterPosition.CF, HitterPosition.RF, HitterPosition.DH
    };

    private static readonly string[] HitterSlotLabels =
    {
        "C", "1B", "2B", "3B", "SS", "LF", "CF", "RF", "DH"
    };

    //HitterSlots 순서대로, 야구장에서 그 포지션이 서는 자리 (필드 영역 좌상단 기준)
    private static readonly Vector2[] HitterSlotPoints =
    {
        new Vector2(423f, 740f),  //C   홈플레이트
        new Vector2(816f, 560f),  //1B  오른쪽
        new Vector2(606f, 380f),  //2B  오른쪽 중간
        new Vector2(30f, 560f),   //3B  왼쪽
        new Vector2(240f, 380f),  //SS  왼쪽 중간
        new Vector2(30f, 70f),    //LF  좌익
        new Vector2(423f, 0f),    //CF  중견
        new Vector2(816f, 70f)    //RF  우익
    };

    //DH 는 수비 자리가 없어 아래 줄 맨 왼쪽에 두고 그 오른쪽으로 벤치 5장을 잇는다
    private const int DhSlotIndex = 8;

    private const float SlotCardWidth = 170f;
    private const float SlotLabelHeight = 34f;
    private const float SlotOrderHeight = 44f;
    private const float SlotGap = 4f;

    private Action _onBack;
    private Action<string, int, Func<CardDisplayInfo, bool>, Action<List<int>>> _onPickCard;
    private Action<string, bool> _onNotify;

    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private Button _hitterTabButton;
    [SerializeField] private Button _pitcherTabButton;
    [SerializeField] private RectTransform _fieldArea;
    [SerializeField] private RectTransform _pitcherArea;
    private readonly List<FieldSlot> _fieldSlots = new List<FieldSlot>(HitterSlotCount);
    private readonly List<FieldSlot> _benchSlots = new List<FieldSlot>(BenchSlotCount);
    private readonly List<FieldSlot> _pitcherSlots = new List<FieldSlot>(MaxRowCount);

    private Tab _tab = Tab.Hitter;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action onBack,
        Action<string, int, Func<CardDisplayInfo, bool>, Action<List<int>>> onPickCard,
        Action<string, bool> onNotify)
    {
        _onBack = onBack;
        _onPickCard = onPickCard;
        _onNotify = onNotify;
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

        bool complete = LineUpManager.Instance.IsLineupComplete();

        _stateText.text = complete ? "라인업 완성" : "비어 있는 슬롯이 있습니다";
        _stateText.color = complete ? UiPalette.StatePitching : UiPalette.StateOut;

        UiTabStyle.Apply(_hitterTabButton, _tab == Tab.Hitter);
        UiTabStyle.Apply(_pitcherTabButton, _tab == Tab.Pitcher);

        bool hitterTab = _tab == Tab.Hitter;

        _fieldArea.gameObject.SetActive(hitterTab);
        _pitcherArea.gameObject.SetActive(!hitterTab);

        if (hitterTab)
            RefreshHitterField();
        else
            RefreshPitcherField();
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
        UiBuilder.Stretch(_fieldArea, 32f, 316f, 32f, 200f);

        _pitcherArea = UiBuilder.CreateRect(Root, "PitcherField");
        UiBuilder.Stretch(_pitcherArea, 32f, 316f, 32f, 200f);

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

        for (int i = 0; i < DhSlotIndex; i++)
        {
            _fieldSlots.Add(new FieldSlot(_fieldArea, HitterSlotLabels[i],
                HitterSlotPoints[i].x, HitterSlotPoints[i].y, SlotCardWidth));
        }

        //아래 줄 : DH 1장 + 벤치 5장을 가로로 잇는다
        float step = SmallCardWidth + 16f;
        float left = (FieldWidth - ((BenchSlotCount + 1) * step - 16f)) * 0.5f;

        _fieldSlots.Add(new FieldSlot(_fieldArea, HitterSlotLabels[DhSlotIndex],
            left, BenchTop, SmallCardWidth));

        for (int i = 0; i < BenchSlotCount; i++)
        {
            _benchSlots.Add(new FieldSlot(_fieldArea, $"벤치 {i + 1}",
                left + (i + 1) * step, BenchTop, SmallCardWidth, false));
        }
    }

    //9포지션을 야구장 자리대로 깐다
    private void RefreshHitterField()
    {
        EnsureFieldSlots();

        for (int i = 0; i < _fieldSlots.Count; i++)
        {
            HitterPosition slot = HitterSlots[i];
            (int instanceId, int battingOrder) = LineUpManager.Instance.GetHitterSlot(slot);

            _fieldSlots[i].SetSlot(instanceId, battingOrder,
                () => PickHitter(slot), () => ClearHitter(slot), order => ChangeOrder(slot, order));
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

        AddPitcherRow("SP", 5, 0f);
        AddPitcherRow("RP", 5, PitcherRowStep);
        AddPitcherRow("CP", 1, PitcherRowStep * 2f);
    }

    //한 행을 가운데 정렬로 깐다
    private void AddPitcherRow(string label, int count, float top)
    {
        float step = SmallCardWidth + 16f;
        float left = (FieldWidth - (count * step - 16f)) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            string slotLabel = count == 1 ? label : $"{label} {i + 1}";

            _pitcherSlots.Add(new FieldSlot(_pitcherArea, slotLabel,
                left + i * step, top, SmallCardWidth, false));
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
        if (!LineUpManager.Instance.SetBattingOrder(slot, order))
            _onNotify?.Invoke($"{order}번 타순은 이미 사용 중입니다", false);

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
        private const float ClearSize = 40f;

        private readonly CardView _card;
        private readonly TextMeshProUGUI _orderText;
        private readonly Button _orderDownButton;
        private readonly Button _orderUpButton;
        private readonly Button _clearButton;

        private Action _onPick;
        private Action _onClear;
        private Action<int> _onOrderChanged;
        private int _battingOrder;

        private readonly bool _hasOrder;

        public FieldSlot(Transform parent, string label, float x, float y, float width,
            bool withOrder = true)
        {
            _hasOrder = withOrder;

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
            _clearButton = UiBuilder.CreateButton(root, "Clear", "X", 26f, false, OnClearClicked);
            UiBuilder.Place(_clearButton.GetComponent<RectTransform>(),
                width - ClearSize, cardTop, ClearSize, ClearSize);

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
            Action<int> onOrderChanged)
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

            _clearButton.gameObject.SetActive(filled);

            if (!_hasOrder)
                return;

            _orderDownButton.gameObject.SetActive(filled);
            _orderUpButton.gameObject.SetActive(filled);
            _orderText.text = filled ? $"{battingOrder}번" : string.Empty;
        }

        private void OnClearClicked()
        {
            _onClear?.Invoke();
        }

        private void DecreaseOrder()
        {
            _onOrderChanged?.Invoke(_battingOrder - 1);
        }

        private void IncreaseOrder()
        {
            _onOrderChanged?.Invoke(_battingOrder + 1);
        }
    }

}
