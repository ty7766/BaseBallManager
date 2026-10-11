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
    private const int MaxMaterialRows = 5;

    /// <summary>
    /// 화면 제목
    /// </summary>
    protected override string Title => "조합";

    [SerializeField] private TextMeshProUGUI _guideText;
    [SerializeField] private TextMeshProUGUI _resultText;
    [SerializeField] private TextMeshProUGUI[] _materialTexts = new TextMeshProUGUI[MaxMaterialRows];
    [SerializeField] private Button _combineButton;

    private readonly List<int> _materialIds = new List<int>(MaxMaterialRows);

    public override void Refresh()
    {
        base.Refresh();

        int required = CombineManager.Instance == null ? 0 : CombineManager.Instance.GetMaterialCount();

        _guideText.text = $"같은 종류 카드 {required}장을 넣으면 1장이 나옵니다 (등급 승급 확률 적용)";

        for (int i = 0; i < MaxMaterialRows; i++)
        {
            bool used = i < required;

            _materialTexts[i].gameObject.SetActive(used);

            if (!used)
                continue;

            if (i < _materialIds.Count && CardDisplayInfo.TryCreate(_materialIds[i], out CardDisplayInfo info))
            {
                _materialTexts[i].text = $"재료 {i + 1}   {BuildCardSummary(info)}";
                _materialTexts[i].color = UiPalette.Text1;
                continue;
            }

            _materialTexts[i].text = $"재료 {i + 1}   비어 있음";
            _materialTexts[i].color = UiPalette.TextOff;
        }

        _combineButton.interactable = required > 0
            && _materialIds.Count == required
            && CombineManager.Instance.CanCombine(_materialIds);
    }

    protected override void BindEvents()
    {
        base.BindEvents();

        BindButton("PickMaterials", PickMaterials);
        Bind(_combineButton, Combine);
    }

    protected override void BuildBody()
    {
        _guideText = UiBuilder.CreateText(Body, "Guide", 32f, UiPalette.Text3);
        UiBuilder.PlaceRow(_guideText.rectTransform, 0f, 48f, 8f);

        Button pickButton = UiBuilder.CreateButton(Body, "PickMaterials", "재료 선택", 34f, false, PickMaterials);
        UiBuilder.PlaceRow(pickButton.GetComponent<RectTransform>(), 60f, 96f);

        for (int i = 0; i < MaxMaterialRows; i++)
        {
            _materialTexts[i] = UiBuilder.CreateText(Body, "Material" + i, 30f, UiPalette.TextOff);
            UiBuilder.PlaceRow(_materialTexts[i].rectTransform, 176f + i * 52f, 48f, 8f);
        }

        _combineButton = UiBuilder.CreateButton(Body, "CombineButton", "조합", 36f, true, Combine);
        UiBuilder.PlaceRow(_combineButton.GetComponent<RectTransform>(), 460f, 110f);

        _resultText = UiBuilder.CreateText(Body, "Result", 34f, UiPalette.AccentGold);
        UiBuilder.PlaceRow(_resultText.rectTransform, 590f, 120f, 8f);
        _resultText.textWrappingMode = TextWrappingModes.Normal;
    }

    private void PickMaterials()
    {
        int required = CombineManager.Instance == null ? 0 : CombineManager.Instance.GetMaterialCount();

        if (required <= 0)
        {
            Notify("조합 재료 수가 설정되지 않았습니다", false);
            return;
        }

        PickCards($"조합 재료 {required}장", required, info => true, ids =>
        {
            _materialIds.Clear();
            _materialIds.AddRange(ids);
            Refresh();
        });
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
