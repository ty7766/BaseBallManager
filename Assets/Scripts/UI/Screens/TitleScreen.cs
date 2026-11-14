using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면. 키 아트 등장 연출이 끝나면 새 게임 · 이어하기 메뉴가 하나씩 튀어 오르고,
/// 새 게임 버튼 둘레가 숨 쉬듯 빛나 누를 곳을 알린다
/// </summary>
public class TitleScreen : UiScreenBehaviour
{
    private const float ButtonWidth = 600f;
    private const float ButtonHeight = 110f;
    private const float NewGameBottom = 270f;
    private const float ContinueBottom = 140f;
    private const float NoticeBottom = 40f;
    private const float NoticeHeight = 80f;
    private const string AdditiveMaterialPath = "UI/Materials/AdditiveLight";

    //메뉴 칸마다 아래에서 살짝 작게 시작해 차례로 튀어 오른다
    private const float MenuItemRise = 50f;
    private const float MenuItemStartScale = 0.9f;
    private const float MenuItemDuration = 0.42f;
    private const float MenuStagger = 0.09f;
    //새 게임 버튼 둘레 빛: 이만큼 바깥으로 번지고, 이 주기로 밝아졌다 어두워진다
    private const float HaloSpread = 14f;
    private const float HaloAlpha = 0.28f;
    private const float HaloPulseSeconds = 1.1f;

    [SerializeField] private Button _continueButton;
    [SerializeField] private TextMeshProUGUI _noticeText;
    [SerializeField] private CanvasGroup _menuGroup;

    private Action _onNewGame;
    private Action _onContinue;
    private TitleKeyArtView _keyArt;
    private MenuItem[] _menuItems;
    private Image _newGameHalo;
    private Tween _menuTween;
    private Tween _haloPulse;

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
            _keyArt.Root.SetAsFirstSibling();
        }

        CacheMenuItems();
        HideMenu();
        _keyArt.PlayIntro(RevealMenu);
    }

    protected override void OnHidden()
    {
        _keyArt?.StopAll();
        _menuTween?.Kill();
        _haloPulse?.Kill();
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

    //메뉴 칸들의 제자리를 한 번만 기억한다. 칸마다 투명도를 따로 움직이려 CanvasGroup 을 붙인다
    private void CacheMenuItems()
    {
        if (_menuItems != null)
            return;

        Transform menu = _menuGroup.transform;
        _menuItems = new MenuItem[menu.childCount];

        for (int i = 0; i < menu.childCount; i++)
        {
            RectTransform rect = (RectTransform)menu.GetChild(i);
            CanvasGroup group = rect.GetComponent<CanvasGroup>();

            if (group == null)
                group = rect.gameObject.AddComponent<CanvasGroup>();

            _menuItems[i] = new MenuItem(rect, group, rect.anchoredPosition);
        }

        _newGameHalo = CreateHalo(menu.Find("NewGameButton") as RectTransform);
    }

    //버튼과 같은 모양을 조금 크게 가산으로 겹친다. 라벨보다 뒤에 깔리도록 첫 자식으로 둔다
    private static Image CreateHalo(RectTransform button)
    {
        if (button == null)
        {
            Debug.LogError("[TitleScreen]: NewGameButton 을 찾지 못해 둘레 빛을 만들지 않습니다");
            return null;
        }

        Image source = button.GetComponent<Image>();
        Image halo = UiBuilder.CreateImage(button, "Halo", new Color(1f, 1f, 1f, 0f));

        halo.sprite = source != null ? source.sprite : null;
        halo.type = Image.Type.Sliced;
        halo.material = Resources.Load<Material>(AdditiveMaterialPath);
        halo.raycastTarget = false;
        UiBuilder.Stretch(halo.rectTransform, -HaloSpread, -HaloSpread, -HaloSpread, -HaloSpread);
        halo.rectTransform.SetAsFirstSibling();

        return halo;
    }

    //연출 중에는 메뉴를 숨기고 누를 수 없게 한다
    private void HideMenu()
    {
        _menuTween?.Kill();
        _haloPulse?.Kill();
        _menuGroup.alpha = 1f;
        _menuGroup.interactable = false;
        _menuGroup.blocksRaycasts = false;

        foreach (MenuItem item in _menuItems)
        {
            item.Group.alpha = 0f;
            item.Rect.anchoredPosition = item.Home - new Vector2(0f, MenuItemRise);
            item.Rect.localScale = Vector3.one * MenuItemStartScale;
        }

        if (_newGameHalo != null)
            _newGameHalo.color = new Color(1f, 1f, 1f, 0f);
    }

    //엠블럼이 박히고 나면 메뉴 칸이 차례로 튀어 오르고, 다 오르면 새 게임 버튼 둘레 빛이 돈다
    private void RevealMenu()
    {
        _menuGroup.interactable = true;
        _menuGroup.blocksRaycasts = true;

        Sequence sequence = DOTween.Sequence().SetUpdate(true).SetLink(gameObject);

        for (int i = 0; i < _menuItems.Length; i++)
        {
            MenuItem item = _menuItems[i];
            float at = i * MenuStagger;

            sequence.Insert(at, item.Group.DOFade(1f, MenuItemDuration * 0.6f));
            sequence.Insert(at, item.Rect.DOAnchorPos(item.Home, MenuItemDuration).SetEase(Ease.OutBack, 1.6f));
            sequence.Insert(at, item.Rect.DOScale(1f, MenuItemDuration).SetEase(Ease.OutBack, 1.6f));
        }

        _menuTween = sequence.OnComplete(StartHaloPulse);
    }

    private void StartHaloPulse()
    {
        if (_newGameHalo == null)
            return;

        _haloPulse = _newGameHalo.DOFade(HaloAlpha, HaloPulseSeconds)
            .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
    }

    private void OnNewGameClicked()
    {
        _onNewGame?.Invoke();
    }

    private void OnContinueClicked()
    {
        _onContinue?.Invoke();
    }

    //메뉴 칸 하나: 움직일 대상 · 투명도 · 제자리
    private readonly struct MenuItem
    {
        public readonly RectTransform Rect;
        public readonly CanvasGroup Group;
        public readonly Vector2 Home;

        public MenuItem(RectTransform rect, CanvasGroup group, Vector2 home)
        {
            Rect = rect;
            Group = group;
            Home = home;
        }
    }
}
