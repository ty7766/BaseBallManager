using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 팀 심볼. 로고 애셋이 아직 없어 팀 컬러 + 이니셜로 대체한다 (명세 ① U5)
/// </summary>
public class TeamEmblemView
{
    private const string LogoResourcePrefix = "Team/logo_";

    public RectTransform Root => _root;

    private readonly RectTransform _root;
    private readonly Image _plate;
    private readonly Image _logo;
    private readonly TextMeshProUGUI _initialText;

    public TeamEmblemView(Transform parent, float size)
    {
        _root = UiBuilder.CreateRect(parent, "TeamEmblem");
        UiBuilder.Place(_root, 0f, 0f, size, size);

        _plate = UiBuilder.CreateImage(_root, "Plate", UiPalette.BgRow, UiSprites.PanelSmall, Image.Type.Sliced);
        UiBuilder.Stretch(_plate.rectTransform, 0f, 0f, 0f, 0f);

        _logo = UiBuilder.CreateImage(_root, "Logo", Color.white);
        UiBuilder.Stretch(_logo.rectTransform, size * 0.12f, size * 0.12f, size * 0.12f, size * 0.12f);
        _logo.preserveAspect = true;
        _logo.gameObject.SetActive(false);

        _initialText = UiBuilder.CreateText(_root, "Initial", size * 0.44f, UiPalette.Text1,
            TextAlignmentOptions.Center);
        UiBuilder.Stretch(_initialText.rectTransform, 0f, 0f, 0f, 0f);
    }

    /// <summary>
    /// 팀을 지정한다. Resources/UI/Team에 로고가 있으면 그것을 쓰고 없으면 이니셜로 그린다
    /// </summary>
    public void SetTeam(string teamName)
    {
        Sprite logo = UiSprites.Get(LogoResourcePrefix + teamName);

        if (logo != null)
        {
            //실제 로고는 자체 색을 가지므로 판을 중립색으로 두어야 로고가 읽힌다
            _plate.color = UiPalette.WithAlpha(UiPalette.BgRow, 0.95f);
            _logo.sprite = logo;
            _logo.gameObject.SetActive(true);
            _initialText.gameObject.SetActive(false);
            return;
        }

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
