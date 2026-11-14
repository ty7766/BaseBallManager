using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 격자로 깐 연속 그림(플립북)을 RawImage 의 uvRect 를 넘겨 가며 튼다.
/// 칸 순서는 왼쪽 위부터 가로로 (Design/ui_assets/render_impact_fx.py 출력 규칙)
/// </summary>
public static class UiFlipbook
{
    /// <summary>
    /// 첫 칸부터 마지막 칸까지 duration 동안 넘기는 트윈. 시퀀스에 끼워 쓴다
    /// </summary>
    public static Tween Play(RawImage image, int columns, int rows, float duration)
    {
        int frameCount = columns * rows;
        float frame = 0f;

        return DOTween.To(() => frame, value =>
        {
            frame = value;
            SetFrame(image, columns, rows, Mathf.Min(frameCount - 1, (int)value));
        }, frameCount, duration).SetEase(Ease.Linear);
    }

    /// <summary>
    /// 칸 하나를 보여 준다. 0 = 왼쪽 위
    /// </summary>
    public static void SetFrame(RawImage image, int columns, int rows, int frame)
    {
        float width = 1f / columns;
        float height = 1f / rows;
        int column = frame % columns;
        int row = frame / columns;

        image.uvRect = new Rect(column * width, 1f - (row + 1) * height, width, height);
    }
}
