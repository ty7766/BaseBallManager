using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 조합. 같은 종류 재료 여러 장을 넣어 1장을 받고, 확률로 등급이 오른다
/// </summary>
public class CombineScreen : CardActionScreen
{
    //재료는 좌 · 중 · 우 3칸으로 고정해 보여준다
    private const int MaterialSlotCount = 3;

    private const float ResultCardWidth = 180f;
    private const float MaterialCardWidth = 160f;
    private const float ResultTop = 20f;
    private const float MaterialTop = 420f;
    private const float LineThickness = 5f;
    private const float GlowSize = 26f;
    //전기가 흐르는 듯한 광이 선을 타고 지나가는 주기(초)
    private const float FlowCycleSeconds = 1.6f;

    /// <summary>
    /// 화면 제목
    /// </summary>
    protected override string Title => "조합";

    /// <summary>
    /// 조합은 필요한 장수만큼 고른다
    /// </summary>
    protected override int RequiredSelectionCount =>
        CombineManager.Instance != null ? CombineManager.Instance.GetMaterialCount() : 1;

    /// <summary>
    /// 아래 목록에서 고른 카드를 재료로 삼는다
    /// </summary>
    protected override void OnSelectionChanged(List<int> selectedIds)
    {
        _materialIds.Clear();
        _materialIds.AddRange(selectedIds);

        //재료를 다시 고르기 시작하면 지난 결과는 치운다
        if (selectedIds.Count == 0)
            _lastResultInstanceId = InventoryManager.InvalidInstanceId;

        Refresh();
    }

    [SerializeField] private TextMeshProUGUI _guideText;
    [SerializeField] private CardView _resultCard;
    [SerializeField] private CardView[] _materialCards = new CardView[MaterialSlotCount];
    [SerializeField] private RectTransform[] _flowRoots = new RectTransform[MaterialSlotCount];
    [SerializeField] private RectTransform[] _flowGlows = new RectTransform[MaterialSlotCount];
    //각 줄은 꺾인 경로다. 광이 지날 좌표를 미리 적어 둔다
    private readonly List<Vector2[]> _flowPaths = new List<Vector2[]>(MaterialSlotCount);
    private readonly List<float> _flowTotals = new List<float>(MaterialSlotCount);
    [SerializeField] private Button _combineButton;

    private readonly List<int> _materialIds = new List<int>(MaterialSlotCount);
    private float _flowPhase;
    private int _lastResultInstanceId = InventoryManager.InvalidInstanceId;

    public override void Refresh()
    {
        base.Refresh();

        int required = CombineManager.Instance == null ? 0 : CombineManager.Instance.GetMaterialCount();

        _guideText.text = $"같은 종류 카드 {required}장을 넣으면 1장이 나옵니다 (등급 승급 확률 적용)";

        for (int i = 0; i < MaterialSlotCount; i++)
        {
            bool used = i < required;

            _materialCards[i].Root.gameObject.SetActive(used);
            _flowRoots[i].gameObject.SetActive(used);

            if (!used)
                continue;

            if (i < _materialIds.Count && CardDisplayInfo.TryCreate(_materialIds[i], out CardDisplayInfo info))
                _materialCards[i].SetCard(info);
            else
                _materialCards[i].SetEmpty($"재료 {i + 1}");
        }

        //방금 조합한 결과가 있으면 그 카드를, 없으면 물음표를 보인다
        if (CardDisplayInfo.TryCreate(_lastResultInstanceId, out CardDisplayInfo resultInfo))
            _resultCard.SetCard(resultInfo);
        else
            _resultCard.SetUnknown();

        _combineButton.interactable = required > 0
            && _materialIds.Count == required
            && CombineManager.Instance.CanCombine(_materialIds);
    }

    protected override void BindEvents()
    {
        base.BindEvents();

        Bind(_combineButton, Combine);
    }

    protected override void BuildBody()
    {
        float bodyWidth = UiBuilder.DesignSize.x - 64f;
        float resultHeight = ResultCardWidth * CardView.BaseHeight / CardView.BaseWidth;
        float resultLeft = (bodyWidth - ResultCardWidth) * 0.5f;

        //재료 3칸을 좌 · 중 · 우로 벌려 놓는다
        float[] materialLeft =
        {
            60f,
            (bodyWidth - MaterialCardWidth) * 0.5f,
            bodyWidth - MaterialCardWidth - 60f
        };

        //선은 카드보다 뒤에 깔려야 하므로 먼저 만든다
        for (int i = 0; i < MaterialSlotCount; i++)
        {
            float fromX = materialLeft[i] + MaterialCardWidth * 0.5f;
            float toX = resultLeft + ResultCardWidth * 0.5f;

            _flowRoots[i] = CreateFlowLine(i, fromX, MaterialTop, toX, ResultTop + resultHeight);
        }

        _resultCard = CardView.Bake(Body, ResultCardWidth, false);
        UiBuilder.Place(_resultCard.Root, resultLeft, ResultTop, ResultCardWidth, resultHeight);

        for (int i = 0; i < MaterialSlotCount; i++)
        {
            _materialCards[i] = CardView.Bake(Body, MaterialCardWidth, false);
            UiBuilder.Place(_materialCards[i].Root, materialLeft[i], MaterialTop,
                MaterialCardWidth, MaterialCardWidth * CardView.BaseHeight / CardView.BaseWidth);
        }

        _guideText = UiBuilder.CreateText(Body, "Guide", 30f, UiPalette.Text3,
            TextAlignmentOptions.Center);
        UiBuilder.PlaceRow(_guideText.rectTransform, 656f, 44f, 8f);

        _combineButton = UiBuilder.CreateButton(Body, "CombineButton", "조합", 36f, true, Combine);
        UiBuilder.PlaceRow(_combineButton.GetComponent<RectTransform>(), 752f, 96f);
    }

    //재료 칸에서 결과 칸으로 ㄱ자(세로 → 가로 → 세로)로 잇는다. 대각선은 쓰지 않는다
    private RectTransform CreateFlowLine(int index, float fromX, float fromY, float toX, float toY)
    {
        RectTransform root = UiBuilder.CreateRect(Body, "FlowLine" + index);

        UiBuilder.Place(root, 0f, 0f, 0f, 0f);

        float midY = (fromY + toY) * 0.5f;

        //가운데 줄은 수직 한 조각이면 된다
        Vector2[] path = Mathf.Approximately(fromX, toX)
            ? new[] { new Vector2(fromX, fromY), new Vector2(toX, toY) }
            : new[]
            {
                new Vector2(fromX, fromY),
                new Vector2(fromX, midY),
                new Vector2(toX, midY),
                new Vector2(toX, toY)
            };

        float total = 0f;

        for (int i = 0; i < path.Length - 1; i++)
        {
            AddSegment(root, path[i], path[i + 1]);
            total += (path[i + 1] - path[i]).magnitude;
        }

        Image glow = UiBuilder.CreateImage(root, "Glow", Color.white, UiSprites.IconStar);
        RectTransform glowRect = glow.rectTransform;

        glowRect.anchorMin = new Vector2(0f, 1f);
        glowRect.anchorMax = new Vector2(0f, 1f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = new Vector2(GlowSize, GlowSize);
        glow.raycastTarget = false;

        _flowGlows[index] = glowRect;
        _flowPaths.Add(path);
        _flowTotals.Add(total);

        return root;
    }

    //꺾인 경로의 한 조각. 가로 또는 세로로만 긋는다
    private static void AddSegment(RectTransform parent, Vector2 from, Vector2 to)
    {
        Image line = UiBuilder.CreateImage(parent, "Segment", UiPalette.LineBorder);
        RectTransform rect = line.rectTransform;

        float left = Mathf.Min(from.x, to.x) - LineThickness * 0.5f;
        float top = Mathf.Min(from.y, to.y) - LineThickness * 0.5f;
        float width = Mathf.Abs(to.x - from.x) + LineThickness;
        float height = Mathf.Abs(to.y - from.y) + LineThickness;

        UiBuilder.Place(rect, left, top, width, height);
        line.raycastTarget = false;
    }

    //경로 위 비율 t 에 해당하는 점
    private Vector2 GetPointOnPath(int index, float t)
    {
        Vector2[] path = _flowPaths[index];
        float target = _flowTotals[index] * t;

        for (int i = 0; i < path.Length - 1; i++)
        {
            float length = (path[i + 1] - path[i]).magnitude;

            if (target <= length || i == path.Length - 2)
                return Vector2.Lerp(path[i], path[i + 1], length <= 0f ? 0f : target / length);

            target -= length;
        }

        return path[path.Length - 1];
    }

    //광이 재료에서 결과 쪽으로 흐른다. 화면이 켜져 있을 때만 돈다
    private void Update()
    {
        _flowPhase += Time.unscaledDeltaTime / FlowCycleSeconds;

        if (_flowPhase >= 1f)
            _flowPhase -= 1f;

        for (int i = 0; i < MaterialSlotCount; i++)
        {
            if (_flowGlows[i] == null || i >= _flowPaths.Count)
                continue;

            bool filled = i < _materialIds.Count;

            _flowGlows[i].gameObject.SetActive(filled);

            if (!filled)
                continue;

            Vector2 point = GetPointOnPath(i, _flowPhase);

            _flowGlows[i].anchoredPosition = new Vector2(point.x, -point.y);
        }
    }

    private void Combine()
    {
        if (CombineManager.Instance == null)
            return;

        CombineResult result = CombineManager.Instance.Combine(_materialIds);

        if (result == null)
        {
            Notify("조합에 실패했습니다 (같은 종류·등급인지 확인)", false);
            Refresh();
            return;
        }

        _materialIds.Clear();

        _lastResultInstanceId = result.InstanceId;

        if (CardDisplayInfo.TryCreate(result.InstanceId, out CardDisplayInfo resultInfo))
            _resultCard.SetCard(resultInfo);

        UiEffects.Pop(_resultCard.Root, 0.6f, 0.36f);

        if (result.IsUpgraded)
            UiEffects.Burst(_resultCard.Root, UiEffectKind.GradeUp);

        Notify(result.IsUpgraded
            ? $"{GradeLabel(result.BaseGrade)} → {GradeLabel(result.Grade)} 승급!"
            : $"{GradeLabel(result.Grade)} 카드를 받았습니다", true);
        Refresh();
    }

    private static string GradeLabel(CardGrade grade)
    {
        return grade switch
        {
            CardGrade.Star3 => "3성",
            CardGrade.Star4 => "4성",
            CardGrade.Star5 => "5성",
            _ => "-"
        };
    }
}
