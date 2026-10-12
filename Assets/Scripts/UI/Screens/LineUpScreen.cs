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
        new Vector2(423f, 720f),  //C   홈플레이트
        new Vector2(816f, 560f),  //1B  오른쪽
        new Vector2(606f, 350f),  //2B  오른쪽 중간
        new Vector2(30f, 560f),   //3B  왼쪽
        new Vector2(240f, 350f),  //SS  왼쪽 중간
        new Vector2(30f, 80f),    //LF  좌익
        new Vector2(423f, 0f),    //CF  중견
        new Vector2(816f, 80f),   //RF  우익
        new Vector2(30f, 900f)    //DH  수비 자리가 없어 왼쪽 아래에 뺀다
    };

    private const float SlotCardWidth = 170f;
    private const float SlotLabelHeight = 34f;
    private const float SlotOrderHeight = 44f;
    private const float SlotGap = 4f;

    private Action _onBack;
    private Action<string, int, Func<CardDisplayInfo, bool>, Action<List<int>>> _onPickCard;
    private Action<string, bool> _onNotify;

    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private Button _hitterTabButton;
    [SerializeField] private Button _benchTabButton;
    [SerializeField] private Button _pitcherTabButton;
    [SerializeField] private RectTransform _rowArea;
    [SerializeField] private RectTransform _fieldArea;
    private readonly List<SlotRow> _rows = new List<SlotRow>(MaxRowCount);
    private readonly List<FieldSlot> _fieldSlots = new List<FieldSlot>(HitterSlotCount);

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
        Bench,
        Pitcher
    }

    /// <summary>
    /// 현재 탭의 슬롯 상태를 다시 읽는다
    /// </summary>
    public void Refresh()
    {
        EnsureRows();

        if (LineUpManager.Instance == null)
            return;

        bool complete = LineUpManager.Instance.IsLineupComplete();

        _stateText.text = complete ? "라인업 완성" : "비어 있는 슬롯이 있습니다";
        _stateText.color = complete ? UiPalette.StatePitching : UiPalette.StateOut;

        UiTabStyle.Apply(_hitterTabButton, _tab == Tab.Hitter);
        UiTabStyle.Apply(_benchTabButton, _tab == Tab.Bench);
        UiTabStyle.Apply(_pitcherTabButton, _tab == Tab.Pitcher);

        bool hitterTab = _tab == Tab.Hitter;

        _fieldArea.gameObject.SetActive(hitterTab);
        _rowArea.gameObject.SetActive(!hitterTab);

        if (hitterTab)
        {
            RefreshHitterField();
            return;
        }

        if (_tab == Tab.Bench)
            RefreshBenchRows();
        else
            RefreshPitcherRows();
    }

    protected override void BindEvents()
    {
        Bind(_hitterTabButton, () => SelectTab(Tab.Hitter));
        Bind(_benchTabButton, () => SelectTab(Tab.Bench));
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

        _hitterTabButton = UiBuilder.CreateButton(Root, "HitterTab", "야수 9", 34f, true,
            () => SelectTab(Tab.Hitter));
        UiBuilder.Place(_hitterTabButton.GetComponent<RectTransform>(), 32f, 110f, 328f, 88f);

        _benchTabButton = UiBuilder.CreateButton(Root, "BenchTab", "벤치 5", 34f, false,
            () => SelectTab(Tab.Bench));
        UiBuilder.Place(_benchTabButton.GetComponent<RectTransform>(), 376f, 110f, 328f, 88f);

        _pitcherTabButton = UiBuilder.CreateButton(Root, "PitcherTab", "투수 11", 34f, false,
            () => SelectTab(Tab.Pitcher));
        UiBuilder.Place(_pitcherTabButton.GetComponent<RectTransform>(), 720f, 110f, 328f, 88f);

        Button autoFillButton = UiBuilder.CreateButton(Root, "AutoFill", "자동 편성", 34f, false, AutoFill);
        UiBuilder.Place(autoFillButton.GetComponent<RectTransform>(), 32f, 212f, 500f, 88f);

        Button clearButton = UiBuilder.CreateButton(Root, "ClearAll", "전체 비우기", 34f, false, ClearAll);
        UiBuilder.Place(clearButton.GetComponent<RectTransform>(), 548f, 212f, 500f, 88f);

        _rowArea = UiBuilder.CreateRect(Root, "Rows");
        UiBuilder.Stretch(_rowArea, 32f, 316f, 32f, 200f);

        _fieldArea = UiBuilder.CreateRect(Root, "Field");
        UiBuilder.Stretch(_fieldArea, 32f, 316f, 32f, 200f);

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

    //슬롯 행은 프리팹에 굽지 않고 런타임에 한 번만 만든다 (SlotRow는 직렬화되지 않는 평범한 클래스)
    private void EnsureRows()
    {
        if (_rows.Count > 0)
            return;

        for (int i = 0; i < MaxRowCount; i++)
            _rows.Add(new SlotRow(_rowArea, i));
    }

    //야수 칸도 프리팹에 굽지 않고 런타임에 한 번만 만든다
    private void EnsureFieldSlots()
    {
        if (_fieldSlots.Count > 0)
            return;

        for (int i = 0; i < HitterSlotCount; i++)
        {
            _fieldSlots.Add(new FieldSlot(_fieldArea, HitterSlotLabels[i],
                HitterSlotPoints[i].x, HitterSlotPoints[i].y, SlotCardWidth));
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
    }

    private void RefreshBenchRows()
    {
        int[] bench = LineUpManager.Instance.GetBenchInstanceIds();

        for (int i = 0; i < _rows.Count; i++)
        {
            if (i >= bench.Length)
            {
                _rows[i].SetVisible(false);
                continue;
            }

            int benchIndex = i;

            _rows[i].SetVisible(true);
            _rows[i].SetSimpleSlot($"벤치 {i + 1}", bench[i],
                () => PickBench(benchIndex), () => ClearBench(benchIndex));
        }
    }

    private void RefreshPitcherRows()
    {
        int[] starters = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.SP);
        int[] relievers = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.RP);
        int[] closers = LineUpManager.Instance.GetPitcherInstanceIds(PitcherPosition.CP);

        int row = 0;

        row = FillPitcherGroup(PitcherPosition.SP, "SP", starters, row);
        row = FillPitcherGroup(PitcherPosition.RP, "RP", relievers, row);
        row = FillPitcherGroup(PitcherPosition.CP, "CP", closers, row);

        for (int i = row; i < _rows.Count; i++)
            _rows[i].SetVisible(false);
    }

    private int FillPitcherGroup(PitcherPosition position, string label, int[] slots, int rowIndex)
    {
        for (int i = 0; i < slots.Length && rowIndex < _rows.Count; i++, rowIndex++)
        {
            int slotIndex = i;

            _rows[rowIndex].SetVisible(true);
            _rows[rowIndex].SetSimpleSlot($"{label} {i + 1}", slots[i],
                () => PickPitcher(position, slotIndex), () => ClearPitcher(position, slotIndex));
        }

        return rowIndex;
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
        private const float ClearSize = 46f;

        private readonly CardView _card;
        private readonly TextMeshProUGUI _orderText;
        private readonly Button _orderDownButton;
        private readonly Button _orderUpButton;
        private readonly Button _clearButton;

        private Action _onPick;
        private Action _onClear;
        private Action<int> _onOrderChanged;
        private int _battingOrder;

        public FieldSlot(Transform parent, string label, float x, float y, float width)
        {
            float cardHeight = width * CardView.BaseHeight / CardView.BaseWidth;
            float height = SlotLabelHeight + SlotGap + cardHeight + SlotGap + SlotOrderHeight;

            RectTransform root = UiBuilder.CreateRect(parent, "Slot" + label);

            UiBuilder.Place(root, x, y, width, height);

            TextMeshProUGUI positionLabel = UiBuilder.CreateText(root, "Position", 30f,
                UiPalette.Text2, TextAlignmentOptions.Center);

            UiBuilder.Place(positionLabel.rectTransform, 0f, 0f, width, SlotLabelHeight);
            positionLabel.text = label;

            float cardTop = SlotLabelHeight + SlotGap;

            _card = CardView.Bake(root, width);
            UiBuilder.Place(_card.Root, 0f, cardTop, width, cardHeight);

            _clearButton = UiBuilder.CreateButton(root, "Clear", "✕", 28f, false, OnClearClicked);
            UiBuilder.Place(_clearButton.GetComponent<RectTransform>(),
                width - ClearSize, cardTop, ClearSize, ClearSize);

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

    //슬롯 1칸. 야수 행에만 타순 조절이 붙는다
    private class SlotRow
    {
        private readonly RectTransform _root;
        private readonly TextMeshProUGUI _slotLabel;
        private readonly TextMeshProUGUI _orderText;
        private readonly Button _orderDownButton;
        private readonly Button _orderUpButton;
        private readonly Button _pickButton;
        private readonly TextMeshProUGUI _pickLabel;
        private readonly Button _clearButton;

        private Action _onPick;
        private Action _onClear;
        private Action<int> _onOrderChanged;
        private int _battingOrder;

        public SlotRow(Transform parent, int index)
        {
            _root = UiBuilder.CreateRect(parent, "SlotRow");
            UiBuilder.PlaceRow(_root, index * (RowHeight + RowGap), RowHeight);

            Image background = UiBuilder.CreateImage(_root, "Background", UiPalette.BgRow,
                UiSprites.RowNormal, Image.Type.Sliced);
            UiBuilder.Stretch(background.rectTransform, 0f, 0f, 0f, 0f);

            _slotLabel = UiBuilder.CreateText(_root, "Slot", 36f, UiPalette.Text2, TextAlignmentOptions.Center);
            UiBuilder.Place(_slotLabel.rectTransform, 8f, 28f, 120f, 48f);

            _orderDownButton = UiBuilder.CreateButton(_root, "OrderDown", "◀", 28f, false, DecreaseOrder);
            UiBuilder.Place(_orderDownButton.GetComponent<RectTransform>(), 136f, 22f, 56f, 60f);

            _orderText = UiBuilder.CreateText(_root, "Order", 34f, UiPalette.AccentGold, TextAlignmentOptions.Center);
            UiBuilder.Place(_orderText.rectTransform, 196f, 28f, 56f, 48f);

            _orderUpButton = UiBuilder.CreateButton(_root, "OrderUp", "▶", 28f, false, IncreaseOrder);
            UiBuilder.Place(_orderUpButton.GetComponent<RectTransform>(), 256f, 22f, 56f, 60f);

            _pickButton = UiBuilder.CreateButton(_root, "Pick", string.Empty, 32f, false, OnPickClicked, out _pickLabel);
            UiBuilder.Place(_pickButton.GetComponent<RectTransform>(), 324f, 14f, 540f, 76f);

            _clearButton = UiBuilder.CreateButton(_root, "Clear", "비우기", 28f, false, OnClearClicked);
            UiBuilder.PlaceRight(_clearButton.GetComponent<RectTransform>(), 12f, 14f, 140f, 76f);
        }

        public void SetVisible(bool visible)
        {
            _root.gameObject.SetActive(visible);
        }

        /// <summary>
        /// 야수 슬롯 (타순 조절 포함)
        /// </summary>
        public void SetHitterSlot(string label, int instanceId, int battingOrder,
            Action onPick, Action onClear, Action<int> onOrderChanged)
        {
            SetSlot(label, instanceId, onPick, onClear);

            _battingOrder = battingOrder;
            _onOrderChanged = onOrderChanged;

            bool filled = instanceId != LineUpManager.EmptySlot;

            _orderText.gameObject.SetActive(filled);
            _orderDownButton.gameObject.SetActive(filled);
            _orderUpButton.gameObject.SetActive(filled);
            _orderText.text = battingOrder.ToString();
        }

        /// <summary>
        /// 벤치 · 투수 슬롯 (타순 없음)
        /// </summary>
        public void SetSimpleSlot(string label, int instanceId, Action onPick, Action onClear)
        {
            SetSlot(label, instanceId, onPick, onClear);

            _onOrderChanged = null;
            _orderText.gameObject.SetActive(false);
            _orderDownButton.gameObject.SetActive(false);
            _orderUpButton.gameObject.SetActive(false);
        }

        private void SetSlot(string label, int instanceId, Action onPick, Action onClear)
        {
            _slotLabel.text = label;
            _onPick = onPick;
            _onClear = onClear;

            bool filled = false;

            if (instanceId != LineUpManager.EmptySlot
                && CardDisplayInfo.TryCreate(instanceId, out CardDisplayInfo info))
            {
                filled = true;
                _pickLabel.text = $"{PlayerLabelFormat.NameWithYear(info.Name, info.Year)}   OVR {info.FinalOvr}";
                _pickLabel.color = CardVisualLookup.GetTypeColor(info.CardType);
            }
            else
            {
                _pickLabel.text = "비어 있음 — 눌러서 배치";
                _pickLabel.color = UiPalette.TextOff;
            }

            _clearButton.gameObject.SetActive(filled);
        }

        private void DecreaseOrder()
        {
            if (_battingOrder > 1)
                _onOrderChanged?.Invoke(_battingOrder - 1);
        }

        private void IncreaseOrder()
        {
            if (_battingOrder < LineUpManager.HitterSlotCount)
                _onOrderChanged?.Invoke(_battingOrder + 1);
        }

        private void OnPickClicked()
        {
            _onPick?.Invoke();
        }

        private void OnClearClicked()
        {
            _onClear?.Invoke();
        }
    }
}
