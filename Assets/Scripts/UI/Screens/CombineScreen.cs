using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 조합. 위 칸은 재료 3칸 → 배선 → 결과(?) 칸, 아래는 보유 카드. 목록에서 누르는 순서대로 재료 칸이 찬다.
/// 같은 종류끼리만 고를 수 있고(다른 종류는 흐리게), 노말은 고른 재료로 결과 등급 확률을 미리 보여 준다
/// </summary>
public class CombineScreen : CardWorkScreen
{
    private const int MaterialSlotCount = 3;
    private const int OddsGradeCount = 3;
    //결과 칸은 위 가운데, 재료 3칸은 아래 줄. 사이를 직각 배선으로 잇는다
    private const float ScreenCenterX = 540f;
    private const float ResultWidth = 170f;
    private const float ResultHeight = ResultWidth * CardView.BaseHeight / CardView.BaseWidth;
    private const float ResultLeft = ScreenCenterX - ResultWidth * 0.5f;
    private const float ResultTop = WorkTop;
    private const float MaterialWidth = 136f;
    private const float MaterialHeight = MaterialWidth * CardView.BaseHeight / CardView.BaseWidth;
    private const float MaterialTop = WorkTop + WorkHeight - MaterialHeight;
    private const float MaterialSpacing = 300f;
    private const float SlotPlusSize = 52f;
    private const float UnknownFontSize = 110f;
    //배선 = 관(어두운 선) + 그 안을 흐르는 빛 물줄기(가산 합성 띠를 이어 붙여 밀어 올린다)
    private const float PipeThickness = 8f;
    private const float StreamThickness = 26f;
    private const float StreamTileLength = 180f;
    private const float StreamSpeed = 200f;
    private const float SurgeSpeedScale = 4f;
    private const string StreamSprite = "Fx/flow_stream";
    private const string AdditiveMaterialPath = "UI/Materials/AdditiveLight";
    //양옆 안내 칸 — 왼쪽 고른 수, 오른쪽 결과 확률 · 안내
    private const float SidePanelTop = WorkTop + 20f;
    private const float SidePanelHeight = ResultHeight - 20f;
    private const float LeftPanelWidth = ResultLeft - SideMargin - 40f;
    private const float RightPanelLeft = ResultLeft + ResultWidth + 40f;
    private const float RightPanelWidth = 1080f - SideMargin - RightPanelLeft;
    private const float OddsRowTop = SidePanelTop + 48f;
    private const float OddsRowHeight = 46f;
    private const float OddsBarWidth = 170f;
    private const float OddsBarHeight = 12f;
    private const float ConsumeSeconds = 0.36f;
    private const string PlusSprite = "Common/icon_plus";
    private const string LockedReason = "잠금";
    private const string LineUpReason = "라인업";
    private const string OtherTypeReason = "다른 종류";

    private static readonly CardGrade[] OddsGrades = { CardGrade.Star3, CardGrade.Star4, CardGrade.Star5 };
    private static readonly Color PipeEmptyColor = UiPalette.LineBorder;
    private static readonly Color PipeFilledColor = new Color(0.2f, 0.3f, 0.42f, 1f);
    private static readonly Color StreamColor = new Color(0.56f, 0.8f, 1f, 1f);

    /// <summary>
    /// 화면 제목 · 실행 버튼
    /// </summary>
    protected override string Title => "조합";
    protected override string ActionLabel => "조합";

    /// <summary>
    /// 재료를 1장이라도 골랐는가
    /// </summary>
    protected override bool HasWork => _materialIds.Count > 0;

    /// <summary>
    /// 고른 재료
    /// </summary>
    protected override IReadOnlyCollection<int> ListSelection => _materialIds;

    [SerializeField] private CardView _resultCard;
    [SerializeField] private TextMeshProUGUI _unknownMark;
    [SerializeField] private CardView[] _materialCards = new CardView[MaterialSlotCount];
    [SerializeField] private RectTransform[] _flowRoots = new RectTransform[MaterialSlotCount];
    [SerializeField] private RectTransform[] _streamRoots = new RectTransform[MaterialSlotCount];
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private TextMeshProUGUI _infoText;
    [SerializeField] private RectTransform _oddsGroup;
    [SerializeField] private Image[] _oddsFills = new Image[OddsGradeCount];
    [SerializeField] private TextMeshProUGUI[] _oddsTexts = new TextMeshProUGUI[OddsGradeCount];

    private readonly List<int> _materialIds = new List<int>(MaterialSlotCount);
    private readonly RawImage[][] _streams = new RawImage[MaterialSlotCount][];
    private readonly float[][] _streamStarts = new float[MaterialSlotCount][];
    private readonly float[][] _streamLengths = new float[MaterialSlotCount][];

    private CombineResult _lastResult;
    private float _flowDistance;
    private float _flowSpeedScale = 1f;
    private bool _surging;

    private int RequiredCount => CombineManager.Instance != null ? CombineManager.Instance.GetMaterialCount() : MaterialSlotCount;

    //빛 물줄기가 재료에서 결과 쪽으로 끝없이 흐른다. 화면이 켜져 있을 때만 돈다
    private void Update()
    {
        _flowDistance = Mathf.Repeat(_flowDistance + Time.unscaledDeltaTime * StreamSpeed * _flowSpeedScale, StreamTileLength);

        for (int i = 0; i < MaterialSlotCount; i++)
        {
            if (_streamRoots[i].gameObject.activeSelf)
                ScrollStream(i);
        }
    }

    //배선(카드 뒤) → 결과 칸 · 재료 3칸 → 양옆 안내
    protected override void BuildWork()
    {
        for (int i = 0; i < MaterialSlotCount; i++)
            _flowRoots[i] = BuildFlowLine(i);

        Image resultSlot = CreateEmptySlot(WorkArea, "ResultSlot", ResultLeft, ResultTop, ResultWidth, ResultHeight);

        _unknownMark = UiBuilder.CreateText(resultSlot.transform, "Unknown", UnknownFontSize, GetEmptySlotLineColor(),
            TextAlignmentOptions.Center);
        UiBuilder.Stretch(_unknownMark.rectTransform, 0f, 0f, 0f, 0f);
        _unknownMark.text = "?";

        _resultCard = CardView.Bake(WorkArea, ResultWidth, false);
        UiBuilder.CenterPivot(UiBuilder.Place(_resultCard.Root, ResultLeft, ResultTop, ResultWidth, ResultHeight));

        for (int i = 0; i < MaterialSlotCount; i++)
        {
            float left = GetMaterialCenterX(i) - MaterialWidth * 0.5f;
            Image slot = CreateEmptySlot(WorkArea, "MaterialSlot", left, MaterialTop, MaterialWidth, MaterialHeight);
            Image plus = UiBuilder.CreateImage(slot.transform, "Plus", GetEmptySlotLineColor(), PlusSprite);

            UiBuilder.Place(plus.rectTransform, (MaterialWidth - SlotPlusSize) * 0.5f, (MaterialHeight - SlotPlusSize) * 0.5f,
                SlotPlusSize, SlotPlusSize);

            _materialCards[i] = CardView.Bake(WorkArea, MaterialWidth, false);
            UiBuilder.CenterPivot(UiBuilder.Place(_materialCards[i].Root, left, MaterialTop, MaterialWidth, MaterialHeight));
        }

        BuildSidePanels();
    }

    protected override void WireWork()
    {
        for (int i = 0; i < MaterialSlotCount; i++)
        {
            int slot = i;

            _materialCards[i].SetClickHandler(() => RemoveMaterialAt(slot));
            _materialCards[i].SetLongPressHandler(() => ShowMaterialDetail(slot));
        }
    }

    //재료 칸 · 배선 · 고른 수 · 오른쪽 안내(확률 / 결과 / 처음 안내)
    protected override void RefreshWork(bool animate)
    {
        int required = RequiredCount;

        for (int i = 0; i < MaterialSlotCount; i++)
        {
            CardView card = _materialCards[i];
            CardDisplayInfo material = default;
            bool filled = i < _materialIds.Count && CardDisplayInfo.TryCreate(_materialIds[i], out material);

            card.Root.localScale = Vector3.one;
            card.Root.gameObject.SetActive(filled);
            TintFlowLine(i, filled);

            if (filled)
                card.SetCard(material);
        }

        CardDisplayInfo result = default;
        bool hasResult = _lastResult != null && CardDisplayInfo.TryCreate(_lastResult.InstanceId, out result);

        _resultCard.Root.gameObject.SetActive(hasResult);
        _unknownMark.gameObject.SetActive(!hasResult);

        if (hasResult)
            _resultCard.SetCard(result);

        _countText.text = $"{_materialIds.Count} / {required}";

        if (animate)
            UiEffects.Pop(_countText.transform, 0.8f, 0.22f);

        RefreshInfo();
        SetAction(_materialIds.Count == required, ActionLabel);
    }

    protected override void OnListCardClicked(int instanceId)
    {
        if (!_materialIds.Remove(instanceId))
        {
            if (_materialIds.Count >= RequiredCount)
            {
                Notify($"재료는 {RequiredCount}장까지 고를 수 있습니다", false);
                return;
            }

            _materialIds.Add(instanceId);
        }

        OnMaterialsChanged();
    }

    protected override void ClearWork()
    {
        _materialIds.Clear();
        _lastResult = null;
    }

    //잠그거나 라인업에 넣어 쓸 수 없게 된 재료는 뺀다
    protected override void ValidateWork()
    {
        for (int i = _materialIds.Count - 1; i >= 0; i--)
        {
            if (!CardDisplayInfo.TryCreate(_materialIds[i], out CardDisplayInfo info) || GetUnusableReason(info) != null)
                _materialIds.RemoveAt(i);
        }
    }

    //잠금 · 라인업 · 먼저 고른 재료와 다른 종류
    protected override string GetBlockedReason(CardDisplayInfo info)
    {
        string unusable = GetUnusableReason(info);

        if (unusable != null)
            return unusable;

        if (_materialIds.Count == 0 || _materialIds.Contains(info.InstanceId))
            return null;

        return CardDisplayInfo.TryCreate(_materialIds[0], out CardDisplayInfo first) && first.CardType != info.CardType
            ? OtherTypeReason
            : null;
    }

    protected override void OnActionClicked()
    {
        if (CombineManager.Instance == null || _materialIds.Count != RequiredCount)
            return;

        CombineResult result = CombineManager.Instance.Combine(_materialIds);

        if (result == null)
        {
            Notify("조합하지 못했습니다", false);
            Refresh();
            return;
        }

        _materialIds.Clear();
        PlaySequence(PlayCombine(result));
    }

    private static string GetUnusableReason(CardDisplayInfo info)
    {
        if (info.IsLocked)
            return LockedReason;

        return LineUpManager.Instance != null && LineUpManager.Instance.IsCardAssigned(info.InstanceId) ? LineUpReason : null;
    }

    //왼쪽: 고른 수, 오른쪽: 결과 확률 막대 3줄 또는 안내 글
    private void BuildSidePanels()
    {
        _countText = UiBuilder.CreateText(WorkArea, "Count", 72f, UiPalette.Text1, TextAlignmentOptions.Center);
        UiBuilder.CenterPivot(UiBuilder.Place(_countText.rectTransform, SideMargin, SidePanelTop, LeftPanelWidth, SidePanelHeight));

        _infoText = UiBuilder.CreateText(WorkArea, "Info", 32f, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.Place(_infoText.rectTransform, RightPanelLeft, SidePanelTop, RightPanelWidth, SidePanelHeight);

        _oddsGroup = UiBuilder.CreateRect(WorkArea, "Odds");
        UiBuilder.Place(_oddsGroup, RightPanelLeft, SidePanelTop, RightPanelWidth, SidePanelHeight);

        TextMeshProUGUI oddsTitle = UiBuilder.CreateText(_oddsGroup, "Title", 26f, UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Place(oddsTitle.rectTransform, 0f, 0f, RightPanelWidth, 40f);
        oddsTitle.text = "결과 확률";

        for (int i = 0; i < OddsGrades.Length; i++)
            BuildOddsRow(i, OddsRowTop - SidePanelTop + i * OddsRowHeight);
    }

    //등급 글자(등급 색) · 막대 · 퍼센트
    private void BuildOddsRow(int index, float top)
    {
        Color gradeColor = GetGradeColor(OddsGrades[index]);

        TextMeshProUGUI label = UiBuilder.CreateText(_oddsGroup, "Grade", 28f, gradeColor);
        UiBuilder.Place(label.rectTransform, 0f, top, 60f, OddsRowHeight);
        label.text = GetGradeLabel(OddsGrades[index]);

        Image track = UiBuilder.CreateImage(_oddsGroup, "Track", UiPalette.WithAlpha(UiPalette.Text1, 0.12f),
            UiSprites.ScrollHandle, Image.Type.Sliced);
        UiBuilder.Place(track.rectTransform, 66f, top + (OddsRowHeight - OddsBarHeight) * 0.5f, OddsBarWidth, OddsBarHeight);

        _oddsFills[index] = UiBuilder.CreateImage(track.transform, "Fill", gradeColor, UiSprites.ScrollHandle, Image.Type.Sliced);
        UiBuilder.Place(_oddsFills[index].rectTransform, 0f, 0f, 0f, OddsBarHeight);

        _oddsTexts[index] = UiBuilder.CreateText(_oddsGroup, "Percent", 28f, UiPalette.Text1, TextAlignmentOptions.MidlineRight);
        UiBuilder.Place(_oddsTexts[index].rectTransform, 66f + OddsBarWidth, top, RightPanelWidth - 66f - OddsBarWidth, OddsRowHeight);
    }

    //재료 칸 위 가운데에서 위로 → 가로 → 결과 칸 아래 가운데로 오르는 직각 경로. 관 위에 물줄기 띠를 같은 순서로 깐다
    private RectTransform BuildFlowLine(int index)
    {
        RectTransform root = UiBuilder.CreateRect(WorkArea, "FlowLine");
        Vector2[] path = GetFlowPath(index);

        UiBuilder.Place(root, 0f, 0f, 0f, 0f);

        for (int i = 0; i < path.Length - 1; i++)
            AddPipe(root, path[i], path[i + 1]);

        RectTransform streams = UiBuilder.CreateRect(root, "Streams");
        Texture streamTexture = UiSprites.Get(StreamSprite)?.texture;
        Material additive = Resources.Load<Material>(AdditiveMaterialPath);

        UiBuilder.Place(streams, 0f, 0f, 0f, 0f);

        for (int i = 0; i < path.Length - 1; i++)
            AddStream(streams, path[i], path[i + 1], streamTexture, additive);

        streams.gameObject.SetActive(false);
        _streamRoots[index] = streams;

        return root;
    }

    //가로 또는 세로 관 한 조각
    private static void AddPipe(RectTransform parent, Vector2 from, Vector2 to)
    {
        Image line = UiBuilder.CreateImage(parent, "Pipe", PipeEmptyColor);
        float left = Mathf.Min(from.x, to.x) - PipeThickness * 0.5f;
        float top = Mathf.Min(from.y, to.y) - PipeThickness * 0.5f;

        UiBuilder.Place(line.rectTransform, left, top, Mathf.Abs(to.x - from.x) + PipeThickness,
            Mathf.Abs(to.y - from.y) + PipeThickness);
    }

    //from 에서 to 쪽으로 눕힌 띠. 텍스처 가로가 흐르는 방향이 되게 돌려 놓는다
    private static void AddStream(RectTransform parent, Vector2 from, Vector2 to, Texture texture, Material material)
    {
        RectTransform rect = UiBuilder.CreateRect(parent, "Stream");
        RawImage stream = rect.gameObject.AddComponent<RawImage>();
        Vector2 delta = to - from;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(from.x, -from.y);
        rect.sizeDelta = new Vector2(delta.magnitude + PipeThickness * 0.5f, StreamThickness);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(-delta.y, delta.x) * Mathf.Rad2Deg);

        stream.texture = texture;
        stream.material = material;
        stream.color = StreamColor;
        stream.raycastTarget = false;
    }

    //배치 상수에서 나오는 값이라 베이크 때와 실행 때가 같다
    private static Vector2[] GetFlowPath(int index)
    {
        float fromX = GetMaterialCenterX(index);
        float toY = ResultTop + ResultHeight;
        float midY = (toY + MaterialTop) * 0.5f;

        if (Mathf.Approximately(fromX, ScreenCenterX))
            return new[] { new Vector2(fromX, MaterialTop), new Vector2(ScreenCenterX, toY) };

        return new[]
        {
            new Vector2(fromX, MaterialTop),
            new Vector2(fromX, midY),
            new Vector2(ScreenCenterX, midY),
            new Vector2(ScreenCenterX, toY)
        };
    }

    private static float GetMaterialCenterX(int index)
    {
        return ScreenCenterX + (index - (MaterialSlotCount - 1) * 0.5f) * MaterialSpacing;
    }

    //띠마다 경로 위 시작 거리만큼 무늬를 밀어 두어야 꺾인 곳에서도 물줄기가 이어진다. 경로는 처음 쓸 때 상수로 다시 만든다
    private void ScrollStream(int index)
    {
        if (_streams[index] == null)
            CacheStreams(index);

        RawImage[] streams = _streams[index];

        for (int i = 0; i < streams.Length; i++)
        {
            streams[i].uvRect = new Rect((_streamStarts[index][i] - _flowDistance) / StreamTileLength, 0f,
                _streamLengths[index][i] / StreamTileLength, 1f);
        }
    }

    private void CacheStreams(int index)
    {
        Vector2[] path = GetFlowPath(index);
        RawImage[] streams = _streamRoots[index].GetComponentsInChildren<RawImage>(true);
        float[] starts = new float[streams.Length];
        float[] lengths = new float[streams.Length];
        float travelled = 0f;

        for (int i = 0; i < streams.Length && i < path.Length - 1; i++)
        {
            starts[i] = travelled;
            lengths[i] = streams[i].rectTransform.sizeDelta.x;
            travelled += (path[i + 1] - path[i]).magnitude;
        }

        _streams[index] = streams;
        _streamStarts[index] = starts;
        _streamLengths[index] = lengths;
    }

    //재료가 들어간 줄은 관이 밝아지고 물줄기가 흐른다
    private void TintFlowLine(int index, bool filled)
    {
        Color color = filled ? PipeFilledColor : PipeEmptyColor;

        foreach (Transform child in _flowRoots[index])
        {
            Image pipe = child.GetComponent<Image>();

            if (pipe != null)
                pipe.color = color;
        }

        _streamRoots[index].gameObject.SetActive(filled || _surging);
    }

    //재료가 없으면 처음 안내 또는 방금 받은 결과, 노말 재료면 확률 막대, 시그 · 골글이면 같은 종류 1장
    private void RefreshInfo()
    {
        bool showOdds = false;

        if (_materialIds.Count == 0)
        {
            _infoText.text = BuildResultMessage() ?? "조합할 카드를\n선택하세요";
        }
        else if (CardDisplayInfo.TryCreate(_materialIds[0], out CardDisplayInfo first) && first.CardType != CardType.Normal)
        {
            _infoText.text = $"{GetTypeLabel(first.CardType)} 카드 중\n무작위 1장";
        }
        else
        {
            showOdds = CombineManager.Instance != null;
            _infoText.text = string.Empty;
        }

        _oddsGroup.gameObject.SetActive(showOdds);

        if (showOdds)
            RefreshOdds();
    }

    private void RefreshOdds()
    {
        (float star3, float star4, float star5) = CombineManager.Instance.GetNormalGradeOdds(_materialIds);
        float[] odds = { star3, star4, star5 };

        for (int i = 0; i < OddsGrades.Length; i++)
        {
            _oddsFills[i].rectTransform.sizeDelta = new Vector2(OddsBarWidth * odds[i], OddsBarHeight);
            _oddsFills[i].gameObject.SetActive(odds[i] > 0f);
            _oddsTexts[i].text = $"{odds[i] * 100f:0.#}%";
            _oddsTexts[i].color = odds[i] > 0f ? UiPalette.Text1 : UiPalette.TextOff;
        }
    }

    //방금 조합한 결과 — 승급이면 금색
    private string BuildResultMessage()
    {
        if (_lastResult == null || !CardDisplayInfo.TryCreate(_lastResult.InstanceId, out CardDisplayInfo result))
            return null;

        string name = PlayerLabelFormat.NameWithYear(result.Name, result.Year);

        return _lastResult.IsUpgraded
            ? $"<color=#F2C14E>{GetGradeLabel(_lastResult.BaseGrade)} → {GetGradeLabel(_lastResult.Grade)} 승급!</color>\n{name}"
            : $"{GetGradeLabel(_lastResult.Grade)} 카드\n{name}";
    }

    private void OnMaterialsChanged()
    {
        _lastResult = null;
        RefreshChrome();
        RefreshListSelection();
        RefreshWork(true);
    }

    private void RemoveMaterialAt(int slot)
    {
        if (IsPlaying || slot >= _materialIds.Count)
            return;

        _materialIds.RemoveAt(slot);
        OnMaterialsChanged();
    }

    private void ShowMaterialDetail(int slot)
    {
        if (slot < _materialIds.Count)
            ShowCardDetail(_materialIds[slot]);
    }

    //재료 3장이 한꺼번에 터지며 작아지고, 물줄기가 빨라져 결과 칸으로 몰린 뒤 새 카드가 튀어나온다. 승급이면 크게 터진다
    private IEnumerator PlayCombine(CombineResult result)
    {
        SetAction(false, ActionLabel);
        _surging = true;
        _flowSpeedScale = SurgeSpeedScale;

        for (int i = 0; i < MaterialSlotCount; i++)
        {
            _streamRoots[i].gameObject.SetActive(true);
            UiEffects.Burst(_materialCards[i].Root, UiEffectKind.Enhance);
            ShrinkAway(_materialCards[i].Root);
        }

        yield return new WaitForSecondsRealtime(ConsumeSeconds + 0.1f);

        _flowSpeedScale = 1f;
        _surging = false;

        _lastResult = result;

        if (CardDisplayInfo.TryCreate(result.InstanceId, out CardDisplayInfo info))
            _resultCard.SetCard(info);

        _unknownMark.gameObject.SetActive(false);
        _resultCard.Root.gameObject.SetActive(true);
        UiEffects.Pop(_resultCard.Root, 0.5f, 0.36f);
        UiEffects.Burst(_resultCard.Root, result.IsUpgraded ? UiEffectKind.GradeUp : UiEffectKind.Enhance);

        Notify(result.IsUpgraded
            ? $"{GetGradeLabel(result.BaseGrade)} → {GetGradeLabel(result.Grade)} 승급!"
            : $"{GetGradeLabel(result.Grade)} 카드를 받았습니다", true);

        yield return new WaitForSecondsRealtime(0.4f);

        RefreshChrome();
        RefreshList();
        RefreshWork(false);
    }

    private static void ShrinkAway(RectTransform card)
    {
        UiAnimator.Play(ConsumeSeconds, t =>
        {
            float scale = 1f - t;

            card.localScale = new Vector3(scale, scale, 1f);
        }, () => card.gameObject.SetActive(false), UiEase.InCubic);
    }

    private static string GetGradeLabel(CardGrade grade)
    {
        return grade switch
        {
            CardGrade.Star3 => "3성",
            CardGrade.Star4 => "4성",
            CardGrade.Star5 => "5성",
            _ => "-"
        };
    }

    private static Color GetGradeColor(CardGrade grade)
    {
        return grade switch
        {
            CardGrade.Star3 => UiPalette.CardStar3,
            CardGrade.Star4 => UiPalette.CardStar4,
            CardGrade.Star5 => UiPalette.CardStar5,
            _ => UiPalette.Text2
        };
    }

    private static string GetTypeLabel(CardType type)
    {
        return type switch
        {
            CardType.Signature => "시그니쳐",
            CardType.GoldenGlove => "골든글러브",
            _ => "노말"
        };
    }
}
