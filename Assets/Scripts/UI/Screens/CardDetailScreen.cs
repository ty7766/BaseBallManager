using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 카드 상세 보기 오버레이. 카드를 누르면 뒤집히고, 잠금을 토글할 수 있다
/// </summary>
public class CardDetailScreen : UiScreenBehaviour
{
    private const float CardWidth = 700f;

    private Action _onClose;
    private Action<string, bool> _onNotify;

    [SerializeField] private CardView _cardView;
    [SerializeField] private TextMeshProUGUI _hintText;
    [SerializeField] private Button _lockButton;
    [SerializeField] private TextMeshProUGUI _lockLabel;

    private int _instanceId = InventoryManager.InvalidInstanceId;
    private bool _showBack;

    /// <summary>
    /// 프리팹 인스턴스에 콜백을 꽂는다. 생성 직후 한 번 호출한다
    /// </summary>
    public void Initialize(Action onClose, Action<string, bool> onNotify)
    {
        _onClose = onClose;
        _onNotify = onNotify;
    }

    /// <summary>
    /// 카드 1장을 띄운다. 조회에 실패하면 아무것도 하지 않는다
    /// </summary>
    public void Open(int instanceId)
    {
        if (!CardDisplayInfo.TryCreate(instanceId, out CardDisplayInfo info))
            return;

        _instanceId = instanceId;
        _showBack = false;

        _cardView.SetFlipped(false);
        _cardView.SetCard(info);
        _cardView.PlayAppear();

        RefreshLockLabel(info.IsLocked);
    }

    protected override void BindEvents()
    {
        Bind(_lockButton, ToggleLock);
        BindButton("CloseButton", OnCloseClicked);
    }

    protected override void Build()
    {
        Image dim = UiBuilder.CreateImage(Root, "Dim", UiPalette.WithAlpha(UiPalette.BgApp, 0.94f));
        UiBuilder.Stretch(dim.rectTransform, 0f, 0f, 0f, 0f);
        dim.raycastTarget = true;

        _cardView = CardView.Bake(Root, CardWidth);
        UiBuilder.Place(_cardView.Root, (UiBuilder.DesignSize.x - CardWidth) * 0.5f, 180f,
            CardWidth, CardWidth * CardView.BaseHeight / CardView.BaseWidth);
        _cardView.SetClickHandler(Flip);

        _hintText = UiBuilder.CreateText(Root, "Hint", 30f, UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Place(_hintText.rectTransform, 40f, 110f, 1000f, 44f);
        _hintText.text = "카드를 누르면 뒷면이 보입니다";

        _lockButton = UiBuilder.CreateButton(Root, "LockButton", "잠금", 34f, false, ToggleLock, out _lockLabel);
        RectTransform lockRect = _lockButton.GetComponent<RectTransform>();

        lockRect.anchorMin = new Vector2(0f, 0f);
        lockRect.anchorMax = new Vector2(0.5f, 0f);
        lockRect.pivot = new Vector2(0f, 0f);
        lockRect.offsetMin = new Vector2(40f, 40f);
        lockRect.offsetMax = new Vector2(-12f, 140f);

        Button closeButton = UiBuilder.CreateButton(Root, "CloseButton", "닫기", 34f, true, OnCloseClicked);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();

        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(1f, 0f);
        closeRect.pivot = new Vector2(0f, 0f);
        closeRect.offsetMin = new Vector2(12f, 40f);
        closeRect.offsetMax = new Vector2(-40f, 140f);
    }

    private void Flip()
    {
        _showBack = !_showBack;
        _cardView.PlayFlip(_showBack);
        _hintText.text = _showBack ? "다시 누르면 앞면으로 돌아갑니다" : "카드를 누르면 뒷면이 보입니다";
    }

    private void ToggleLock()
    {
        if (!CardDisplayInfo.TryCreate(_instanceId, out CardDisplayInfo info))
            return;

        bool nextLocked = !info.IsLocked;

        if (InventoryManager.Instance == null || !InventoryManager.Instance.SetLocked(_instanceId, nextLocked))
        {
            _onNotify?.Invoke("잠금 상태를 바꾸지 못했습니다", false);
            return;
        }

        RefreshLockLabel(nextLocked);
        _onNotify?.Invoke(nextLocked ? "카드를 잠갔습니다" : "잠금을 풀었습니다", true);
    }

    private void RefreshLockLabel(bool locked)
    {
        _lockLabel.text = locked ? "잠금 해제" : "잠금";
        _lockLabel.color = locked ? UiPalette.AccentGold : UiPalette.Text1;
    }

    private void OnCloseClicked()
    {
        _onClose?.Invoke();
    }
}
