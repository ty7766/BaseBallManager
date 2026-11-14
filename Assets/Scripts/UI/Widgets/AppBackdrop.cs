using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모든 화면 뒤에 한 장만 깔리는 공용 배경. 화면은 바탕 없이 그 위에 올라가므로 화면이 바뀌어도 배경은 그대로 있다.
/// 바탕 위 붓질 층은 흰 결로 구워 두고 색을 곱해 칠한다 — 팀을 고르기 전엔 붉은색, 고른 뒤엔 팀 색
/// </summary>
public class AppBackdrop
{
    private const string BaseTexturePath = "UI/Backdrop/app_backdrop";
    private const string BrushTexturePath = "UI/Backdrop/app_backdrop_brush";
    private const float ArtWidth = 1080f;
    private const float ArtHeight = 1920f;

    private readonly RawImage _brush;

    /// <summary>
    /// 캔버스 맨 뒤에 배경을 깐다. 화면 비율이 달라도 빈틈 없이 덮고 넘치는 쪽을 자른다
    /// </summary>
    public AppBackdrop(Transform canvasRoot)
    {
        RectTransform frame = UiBuilder.CreateRect(canvasRoot, "AppBackdrop");
        UiBuilder.Stretch(frame, 0f, 0f, 0f, 0f);
        frame.SetAsFirstSibling();

        RectTransform art = UiBuilder.CreateRect(frame, "Art");
        art.anchorMin = art.anchorMax = new Vector2(0.5f, 0.5f);
        art.sizeDelta = new Vector2(ArtWidth, ArtHeight);

        AspectRatioFitter fitter = art.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = ArtWidth / ArtHeight;

        RawImage background = CreateLayer(art, "Base", BaseTexturePath);

        if (background.texture == null)
            background.color = UiPalette.BgApp;

        _brush = CreateLayer(art, "Brush", BrushTexturePath);
        _brush.enabled = _brush.texture != null;
        ResetBrushColor();
    }

    /// <summary>
    /// 붓질을 팀 색으로 칠한다. 모르는 팀이면 기본 붉은색으로 둔다
    /// </summary>
    public void SetTeam(string teamName)
    {
        _brush.color = UiPalette.IsKnownTeam(teamName) ? UiPalette.GetTeamBrushColor(teamName) : UiPalette.BrushDefault;
    }

    /// <summary>
    /// 팀을 고르기 전 기본 붉은 붓질로 되돌린다
    /// </summary>
    public void ResetBrushColor()
    {
        _brush.color = UiPalette.BrushDefault;
    }

    //화면 크기 무대에 꽉 차는 텍스처 한 장. 없으면 오류를 남기고 빈 층으로 둔다
    private static RawImage CreateLayer(RectTransform art, string name, string texturePath)
    {
        RectTransform rect = UiBuilder.CreateRect(art, name);
        UiBuilder.Stretch(rect, 0f, 0f, 0f, 0f);

        RawImage image = rect.gameObject.AddComponent<RawImage>();
        image.raycastTarget = false;
        image.texture = Resources.Load<Texture2D>(texturePath);

        if (image.texture == null)
            Debug.LogError($"[AppBackdrop]: 배경 텍스처 '{texturePath}' 가 없습니다");

        return image;
    }
}
