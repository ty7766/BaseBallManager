using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면. 새 게임과 이어하기 진입점
/// </summary>
public class TitleScreen : UiScreenBehaviour
{
    private Action _onNewGame;
    private Action _onContinue;

    [SerializeField] private Button _continueButton;
    [SerializeField] private TextMeshProUGUI _noticeText;

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

    protected override void Build()
    {
        TextMeshProUGUI title = UiBuilder.CreateText(Root, "Title", 96f, UiPalette.Text1, TextAlignmentOptions.Center);
        UiBuilder.Place(title.rectTransform, 40f, 420f, 1000f, 120f);
        title.text = "베이스볼 매니저";

        TextMeshProUGUI subtitle = UiBuilder.CreateLabel(Root, "Subtitle", "BASEBALL MANAGER", 36f,
            UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Place(subtitle.rectTransform, 40f, 548f, 1000f, 50f);

        Button newGameButton = UiBuilder.CreateButton(Root, "NewGameButton", "새 게임", 44f, true, OnNewGameClicked);
        UiBuilder.Place(newGameButton.GetComponent<RectTransform>(), 240f, 900f, 600f, 128f);

        _continueButton = UiBuilder.CreateButton(Root, "ContinueButton", "이어하기", 44f, false, OnContinueClicked);
        UiBuilder.Place(_continueButton.GetComponent<RectTransform>(), 240f, 1060f, 600f, 128f);

        _noticeText = UiBuilder.CreateText(Root, "Notice", 30f, UiPalette.Text3, TextAlignmentOptions.Center);
        UiBuilder.Place(_noticeText.rectTransform, 40f, 1220f, 1000f, 120f);
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
