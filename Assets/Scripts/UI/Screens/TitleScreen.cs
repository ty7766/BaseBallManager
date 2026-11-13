using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면. 키 아트 등장 연출이 끝나면 새 게임 · 이어하기 메뉴가 아래에서 떠오른다
/// </summary>
public class TitleScreen : UiScreenBehaviour
{
    private const float ButtonWidth = 600f;
    private const float ButtonHeight = 110f;
    private const float NewGameBottom = 270f;
    private const float ContinueBottom = 140f;
    private const float NoticeBottom = 40f;
    private const float NoticeHeight = 80f;
    private const float MenuRise = 30f;
    private const float MenuFadeDuration = 0.35f;

    [SerializeField] private Button _continueButton;
    [SerializeField] private TextMeshProUGUI _noticeText;
    [SerializeField] private CanvasGroup _menuGroup;

    private Action _onNewGame;
    private Action _onContinue;
    private TitleKeyArtView _keyArt;
    private Tween _menuTween;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action onNewGame, Action onContinue)
    {
        _onNewGame = onNewGame;
        _onContinue = onContinue;
    }

    /// <summary>
    /// 이어하기 버튼 노출 여부와 안내 문구
    /// </summary>
    public void SetSaveAvailable(bool hasSave, string notice)
    {
        _continueButton.gameObject.SetActive(hasSave);
        _noticeText.text = notice;
    }

    protected override void BindEvents()
    {
        Bind(_continueButton, OnContinueClicked);
        BindButton("NewGameButton", OnNewGameClicked);
    }

    protected override void OnShown()
    {
        if (_keyArt == null)
        {
            _keyArt = TitleKeyArtView.Create(Root);
            _keyArt.Root.SetSiblingIndex(1);
        }

        HideMenu();
        _keyArt.PlayIntro(RevealMenu);
    }

    protected override void OnHidden()
    {
        _keyArt?.StopAll();
        _menuTween?.Kill();
    }

    protected override void Build()
    {
        RectTransform menu = UiBuilder.CreateRect(Root, "Menu");
        UiBuilder.Stretch(menu, 0f, 0f, 0f, 0f);
        _menuGroup = menu.gameObject.AddComponent<CanvasGroup>();

        Button newGameButton = UiBuilder.CreateButton(menu, "NewGameButton", "새 게임", 44f, true, OnNewGameClicked);
        PlaceFromBottom(newGameButton.GetComponent<RectTransform>(), NewGameBottom, ButtonWidth, ButtonHeight);

        _continueButton = UiBuilder.CreateButton(menu, "ContinueButton", "이어하기", 44f, false, OnContinueClicked);
        PlaceFromBottom(_continueButton.GetComponent<RectTransform>(), ContinueBottom, ButtonWidth, ButtonHeight);

        _noticeText = UiBuilder.CreateText(menu, "Notice", 30f, UiPalette.Text3, TextAlignmentOptions.Center);
        PlaceFromBottom(_noticeText.rectTransform, NoticeBottom, UiBuilder.DesignSize.x - 80f, NoticeHeight);
    }

    //아래 가운데 기준. 세로가 긴 기기에서도 메뉴가 화면 바닥에 붙는다
    private static void PlaceFromBottom(RectTransform rect, float bottom, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottom);
        rect.sizeDelta = new Vector2(width, height);
    }

    //연출 중에는 메뉴를 숨기고 누를 수 없게 한다
    private void HideMenu()
    {
        _menuTween?.Kill();
        _menuGroup.alpha = 0f;
        _menuGroup.interactable = false;
        _menuGroup.blocksRaycasts = false;
        ((RectTransform)_menuGroup.transform).anchoredPosition = new Vector2(0f, -MenuRise);
    }

    //로고가 내려앉으면 메뉴가 아래에서 떠오른다
    private void RevealMenu()
    {
        RectTransform menu = (RectTransform)_menuGroup.transform;

        _menuGroup.interactable = true;
        _menuGroup.blocksRaycasts = true;
        _menuTween = DOTween.Sequence().SetUpdate(true).SetLink(gameObject)
            .Join(_menuGroup.DOFade(1f, MenuFadeDuration))
            .Join(menu.DOAnchorPosY(0f, MenuFadeDuration).SetEase(Ease.OutCubic));
    }

    private void OnNewGameClicked()
    {
        _onNewGame?.Invoke();
    }

    private void OnContinueClicked()
    {
        _onContinue?.Invoke();
    }
}
