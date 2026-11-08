using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 팀 심볼. 로고 애셋이 있으면 그것을, 없으면 팀 컬러 + 이니셜로 그린다 (명세 3-3)
/// </summary>
public class TeamEmblemView : UiWidgetBehaviour
{
    private const string LogoResourcePrefix = "Team/logo_";

    [SerializeField] private Image _plate;
    [SerializeField] private Image _logo;
    [SerializeField] private TextMeshProUGUI _initialText;

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static TeamEmblemView Bake(Transform parent, float size)
    {
        TeamEmblemView view = CreateNode<TeamEmblemView>(parent, "TeamEmblem");

        UiBuilder.Place(view.Root, 0f, 0f, size, size);

        view._plate = UiBuilder.CreateImage(view.Root, "Plate", UiPalette.BgRow,
            UiSprites.PanelSmall, Image.Type.Sliced);
        UiBuilder.Stretch(view._plate.rectTransform, 0f, 0f, 0f, 0f);

        view._logo = UiBuilder.CreateImage(view.Root, "Logo", Color.white);
        UiBuilder.Stretch(view._logo.rectTransform, size * 0.12f, size * 0.12f, size * 0.12f, size * 0.12f);
        view._logo.preserveAspect = true;
        view._logo.gameObject.SetActive(false);

        view._initialText = UiBuilder.CreateText(view.Root, "Initial", size * 0.44f, UiPalette.Text1,
            TextAlignmentOptions.Center);
        UiBuilder.Stretch(view._initialText.rectTransform, 0f, 0f, 0f, 0f);

        return view;
    }

    /// <summary>
    /// 팀을 지정한다. Resources/UI/Team에 로고가 있으면 그것을 쓰고 없으면 이니셜로 그린다
    /// </summary>
    public void SetTeam(string teamName)
    {
        Sprite logo = UiSprites.Get(LogoResourcePrefix + teamName);

        if (logo != null)
        {
            //로고 PNG 가 투명 배경이므로 판을 비워 로고만 보이게 한다
            _plate.sprite = null;
            _plate.color = UiPalette.WithAlpha(UiPalette.BgApp, 0f);
            _logo.sprite = logo;
            _logo.gameObject.SetActive(true);
            _initialText.gameObject.SetActive(false);
            return;
        }

        _plate.sprite = UiSprites.Get(UiSprites.PanelSmall);
        _plate.color = UiPalette.WithAlpha(UiPalette.GetTeamColor(teamName), 0.85f);
        _logo.gameObject.SetActive(false);
        _initialText.gameObject.SetActive(true);
        _initialText.text = GetInitial(teamName);
    }

    //아래에 팀명이 따로 찍히므로 엠블럼은 한 글자만 쓴다 (영문 약칭은 두 글자까지)
    private static string GetInitial(string teamName)
    {
        if (string.IsNullOrEmpty(teamName))
            return "?";

        bool isAscii = teamName[0] < 128;

        if (!isAscii)
            return teamName.Substring(0, 1);

        return teamName.Length <= 2 ? teamName : teamName.Substring(0, 2);
    }
}
