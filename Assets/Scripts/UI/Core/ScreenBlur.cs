using UnityEngine;

/// <summary>
/// 지금 화면을 한 장 찍어 흐리게 만든다 (팝업 뒤 블러). URP · 오버레이 캔버스라 그랩패스 대신
/// 화면을 찍은 뒤 절반씩 줄여 가며(쌍선형 평균) 뭉갠다. 프레임 끝(WaitForEndOfFrame)에서 불러야 UI 까지 찍힌다
/// </summary>
public static class ScreenBlur
{
    //1/2 씩 4번 = 1/16. 마지막에 한 번 키워 계단을 누그러뜨린다
    private const int DownsampleSteps = 4;

    /// <summary>
    /// 흐린 화면 텍스처. 다 쓰면 Release 로 돌려준다
    /// </summary>
    public static RenderTexture Capture()
    {
        int width = Mathf.Max(1, Screen.width);
        int height = Mathf.Max(1, Screen.height);
        RenderTexture current = Create(width, height);

        ScreenCapture.CaptureScreenshotIntoRenderTexture(current);

        for (int i = 0; i < DownsampleSteps; i++)
        {
            width = Mathf.Max(1, width / 2);
            height = Mathf.Max(1, height / 2);

            RenderTexture next = Create(width, height);

            //찍은 화면은 그래픽 API 에 따라 위아래가 뒤집혀 있다. 첫 축소에서 바로잡는다
            if (i == 0 && SystemInfo.graphicsUVStartsAtTop)
                Graphics.Blit(current, next, new Vector2(1f, -1f), new Vector2(0f, 1f));
            else
                Graphics.Blit(current, next);

            RenderTexture.ReleaseTemporary(current);
            current = next;
        }

        RenderTexture smoothed = Create(width * 2, height * 2);

        Graphics.Blit(current, smoothed);
        RenderTexture.ReleaseTemporary(current);

        return smoothed;
    }

    /// <summary>
    /// Capture 로 받은 텍스처를 돌려준다
    /// </summary>
    public static void Release(RenderTexture texture)
    {
        if (texture != null)
            RenderTexture.ReleaseTemporary(texture);
    }

    //화면 값은 감마로 들어 있으므로 sRGB 로 읽어야 선형 색 공간에서 밝기가 그대로다
    private static RenderTexture Create(int width, int height)
    {
        RenderTexture texture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.sRGB);

        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        return texture;
    }
}
