using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 1장을 골라 무언가를 하는 화면들의 공통 틀 (강화 · 훈련 · 조합 · 분해 · 제작).
/// 제목 · 재화 줄 · 본문 영역 · 로비 버튼을 공유하고 본문만 각 화면이 채운다
/// </summary>
public abstract class CardActionScreen : UiScreenBehaviour
{
    private const float BodyTop = 212f;

    /// <summary>
    /// 화면 제목. 베이크 시점에 쓰이므로 상수 문자열로 둔다
    /// </summary>
    protected abstract string Title { get; }

    /// <summary>
    /// 본문을 올릴 영역 (제목·재화 줄 아래, 하단 버튼 위)
    /// </summary>
    protected RectTransform Body => _body;

    /// <summary>
    /// 상단 재화 줄
    /// </summary>
    protected CurrencyBarView CurrencyBar => _currencyBar;

    [SerializeField] private RectTransform _body;
    [SerializeField] private CurrencyBarView _currencyBar;

    private Action _onBack;
    private Action<string, int, Func<CardDisplayInfo, bool>, Action<List<int>>> _onPickCard;
    private Action<string, bool> _onNotify;

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

    /// <summary>
    /// 화면에 들어올 때마다 상태를 다시 읽는다
    /// </summary>
    public virtual void Refresh()
    {
        _currencyBar.Refresh();
    }

    protected sealed override void Build()
    {
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 48f, UiPalette.Text1);
        UiBuilder.Place(title.rectTransform, 32f, 32f, 700f, 60f);
        title.text = Title;

        _currencyBar = CurrencyBarView.Bake(Root);
        UiBuilder.Place(_currencyBar.Root, 24f, 104f, 1032f, CurrencyBarView.BarHeight);

        _body = UiBuilder.CreateRect(Root, "Body");
        UiBuilder.Stretch(_body, 32f, BodyTop, 32f, 200f);

        Button backButton = UiBuilder.CreateButton(Root, "BackButton", "로비로", 34f, true, OnBackClicked);
        RectTransform backRect = backButton.GetComponent<RectTransform>();

        backRect.anchorMin = new Vector2(0f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.offsetMin = new Vector2(32f, 40f);
        backRect.offsetMax = new Vector2(-32f, 160f);

        BuildBody();
    }

    protected override void BindEvents()
    {
        BindButton("BackButton", OnBackClicked);
    }

    protected override void OnShown()
    {
        Refresh();
    }

    /// <summary>
    /// 화면별 본문 구성
    /// </summary>
    protected abstract void BuildBody();

    /// <summary>
    /// 성공·실패 안내
    /// </summary>
    protected void Notify(string message, bool success)
    {
        _onNotify?.Invoke(message, success);
    }

    /// <summary>
    /// 카드 선택 모달을 띄운다
    /// </summary>
    protected void PickCard(string title, Func<CardDisplayInfo, bool> canSelect, Action<int> onPicked)
    {
        _onPickCard?.Invoke(title, 1, canSelect, ids => onPicked(ids[0]));
    }

    /// <summary>
    /// 여러 장을 고르는 선택 모달 (조합 재료)
    /// </summary>
    protected void PickCards(string title, int count, Func<CardDisplayInfo, bool> canSelect,
        Action<List<int>> onPicked)
    {
        _onPickCard?.Invoke(title, count, canSelect, onPicked);
    }

    /// <summary>
    /// 선택된 카드 요약 1줄 ("이서준 '24 · LF · OVR 73 · 강화 +2 · 훈련 Lv.5")
    /// </summary>
    protected static string BuildCardSummary(CardDisplayInfo info)
    {
        return $"{PlayerLabelFormat.NameWithYear(info.Name, info.Year)}  ·  {info.Position}  ·  " +
               $"OVR {info.FinalOvr}  ·  강화 +{info.EnhanceLevel}  ·  훈련 Lv.{info.TrainLevel}";
    }

    private void OnBackClicked()
    {
        _onBack?.Invoke();
    }
}
