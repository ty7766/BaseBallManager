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

        Refresh();
    }

    [SerializeField] private TextMeshProUGUI _guideText;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private CardView _resultCard;
    [SerializeField] private CardView[] _materialCards = new CardView[MaterialSlotCount];
    [SerializeField] private RectTransform[] _flowLines = new RectTransform[MaterialSlotCount];
    [SerializeField] private RectTransform[] _flowGlows = new RectTransform[MaterialSlotCount];
    [SerializeField] private float[] _flowLengths = new float[MaterialSlotCount];
    [SerializeField] private Button _combineButton;

    private readonly List<int> _materialIds = new List<int>(MaterialSlotCount);
    private float _flowPhase;

    public override void Refresh()
    {
        base.Refresh();

        int required = CombineManager.Instance == null ? 0 : CombineManager.Instance.GetMaterialCount();

        _guideText.text = $"같은 종류 카드 {required}장을 넣으면 1장이 나옵니다 (등급 승급 확률 적용)";

        for (int i = 0; i < MaterialSlotCount; i++)
        {
            bool used = i < required;

            _materialCards[i].Root.gameObject.SetActive(used);
            _flowLines[i].gameObject.SetActive(used);

            if (!used)
                continue;

            if (i < _materialIds.Count && CardDisplayInfo.TryCreate(_materialIds[i], out CardDisplayInfo info))
                _materialCards[i].SetCard(info);
            else
                _materialCards[i].SetEmpty($"재료 {i + 1}");
        }

        //결과는 아직 모르므로 물음표 카드로 둔다
        _resultCard.SetEmpty("?");

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

            _flowLines[i] = CreateFlowLine(i, fromX, MaterialTop, toX, ResultTop + resultHeight);
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

        _resultText = UiBuilder.CreateText(Body, "Result", 32f, UiPalette.AccentGold,
            TextAlignmentOptions.Center);
        UiBuilder.PlaceRow(_resultText.rectTransform, 700f, 46f, 8f);
        _resultText.textWrappingMode = TextWrappingModes.Normal;

        _combineButton = UiBuilder.CreateButton(Body, "CombineButton", "조합", 36f, true, Combine);
        UiBuilder.PlaceRow(_combineButton.GetComponent<RectTransform>(), 752f, 96f);
    }

    //재료 칸에서 결과 칸으로 잇는 선과, 그 위를 지나갈 광 한 점
    private RectTransform CreateFlowLine(int index, float fromX, float fromY, float toX, float toY)
    {
        float deltaX = toX - fromX;
        float deltaY = fromY - toY;
        float length = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);

        Image line = UiBuilder.CreateImage(Body, "FlowLine" + index, UiPalette.LineBorder);
        RectTransform rect = line.rectTransform;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(fromX, -fromY);
        rect.sizeDelta = new Vector2(length, LineThickness);
        rect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(deltaY, deltaX) * Mathf.Rad2Deg);
        line.raycastTarget = false;

        Image glow = UiBuilder.CreateImage(rect, "Glow", Color.white, UiSprites.IconStar);
        RectTransform glowRect = glow.rectTransform;

        glowRect.anchorMin = new Vector2(0f, 0.5f);
        glowRect.anchorMax = new Vector2(0f, 0.5f);
        glowRect.pivot = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = new Vector2(GlowSize, GlowSize);
        glow.raycastTarget = false;

        _flowGlows[index] = glowRect;
        _flowLengths[index] = length;

        return rect;
    }

    //광이 재료에서 결과 쪽으로 흐른다. 화면이 켜져 있을 때만 돈다
    private void Update()
    {
        _flowPhase += Time.unscaledDeltaTime / FlowCycleSeconds;

        if (_flowPhase >= 1f)
            _flowPhase -= 1f;

        for (int i = 0; i < MaterialSlotCount; i++)
        {
            if (_flowGlows[i] == null)
                continue;

            bool filled = i < _materialIds.Count;

            _flowGlows[i].gameObject.SetActive(filled);

            if (!filled)
                continue;

            _flowGlows[i].anchoredPosition = new Vector2(_flowLengths[i] * _flowPhase, 0f);
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

        string gradeText = result.IsUpgraded
            ? $"{GradeLabel(result.BaseGrade)} → {GradeLabel(result.Grade)} 승급!"
            : $"{GradeLabel(result.Grade)} 유지";

        if (CardDisplayInfo.TryCreate(result.InstanceId, out CardDisplayInfo info))
            _resultText.text = $"{PlayerLabelFormat.NameWithYear(info.Name, info.Year)}  ·  {gradeText}";
        else
            _resultText.text = gradeText;

        UiEffects.Pop(_resultText.transform, 0.7f, 0.34f);

        if (result.IsUpgraded)
        {
            UiEffects.Flash(_resultText, UiPalette.CardSignature, 0.7f);
            UiEffects.Burst(_resultText.rectTransform, UiEffectKind.GradeUp);
        }

        Notify(result.IsUpgraded ? "승급했습니다" : "조합했습니다", true);
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
