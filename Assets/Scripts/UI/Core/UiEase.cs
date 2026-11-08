using UnityEngine;

/// <summary>
/// 트윈 이징. 0~1 진행값을 받아 0~1 보간값을 돌려준다
/// </summary>
public static class UiEase
{
    /// <summary>
    /// 선형
    /// </summary>
    public static float Linear(float t)
    {
        return t;
    }

    /// <summary>
    /// 빠르게 시작해 부드럽게 멈춤 (등장·이동의 기본)
    /// </summary>
    public static float OutCubic(float t)
    {
        float inverse = 1f - t;

        return 1f - inverse * inverse * inverse;
    }

    /// <summary>
    /// 천천히 시작
    /// </summary>
    public static float InCubic(float t)
    {
        return t * t * t;
    }

    /// <summary>
    /// 목표를 살짝 넘어갔다 돌아옴 (배지·숫자 팝)
    /// </summary>
    public static float OutBack(float t)
    {
        const float overshoot = 1.9f;
        float inverse = t - 1f;

        return 1f + inverse * inverse * ((overshoot + 1f) * inverse + overshoot);
    }

    /// <summary>
    /// 0 → 1 → 0 (플래시·펄스)
    /// </summary>
    public static float Pulse(float t)
    {
        return Mathf.Sin(t * Mathf.PI);
    }
}
