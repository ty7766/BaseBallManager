using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모든 화면 뒤에 한 장만 깔리는 공용 배경. 화면은 바탕 없이 그 위에 올라가므로
/// 화면이 바뀌어도 배경은 그대로 있고 본문만 바뀐다
/// </summary>
public static class AppBackdrop
{
    private const string TexturePath = "UI/Backdrop/app_backdrop";
    private const float ArtWidth = 1080f;
    private const float ArtHeight = 1920f;

    /// <summary>
    /// 캔버스 맨 뒤에 배경을 깐다. 화면 비율이 달라도 빈틈 없이 덮고 넘치는 쪽을 자른다
    /// </summary>
    public static RectTransform Create(Transform canvasRoot)
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

        RawImage image = art.gameObject.AddComponent<RawImage>();
        image.raycastTarget = false;
        image.texture = Resources.Load<Texture2D>(TexturePath);

        if (image.texture == null)
        {
            Debug.LogError($"[AppBackdrop]: 배경 텍스처 '{TexturePath}' 가 없어 단색으로 깝니다");
            image.color = UiPalette.BgApp;
        }

        return frame;
    }
}
