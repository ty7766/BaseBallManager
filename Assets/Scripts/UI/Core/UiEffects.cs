using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 자주 쓰는 연출 묶음. 모두 UiAnimator 위에서 돈다
/// </summary>
public static class UiEffects
{
    private const int SpinnerFrameCount = 8;

    /// <summary>
    /// 화면 맨 위 파티클 레이어. 부트스트랩이 한 번 넣는다
    /// </summary>
    public static UiEffectLayer Layer { get; set; }

    /// <summary>
    /// 대상 위젯 위에서 파티클을 뿌린다. 레이어가 없으면 아무 일도 하지 않는다
    /// </summary>
    public static void Burst(RectTransform target, UiEffectKind kind)
    {
        Layer?.Burst(target, kind);
    }

    /// <summary>
    /// 투명도 0 → 1
    /// </summary>
    public static UiTween FadeIn(CanvasGroup group, float duration = 0.18f, float delay = 0f)
    {
        if (group == null)
            return null;

        group.alpha = 0f;

        return UiAnimator.Play(duration, t => group.alpha = t, null, UiEase.OutCubic, delay);
    }

    /// <summary>
    /// 투명도 1 → 0. 끝나면 숨긴다
    /// </summary>
    public static UiTween FadeOutAndHide(CanvasGroup group, float duration = 0.14f)
    {
        if (group == null)
            return null;

        return UiAnimator.Play(duration, t => group.alpha = 1f - t,
            () => group.gameObject.SetActive(false), UiEase.Linear);
    }

    /// <summary>
    /// 아래에서 올라오며 나타난다 (중계 행 · 토스트)
    /// </summary>
    public static UiTween SlideUp(RectTransform rect, CanvasGroup group, float distance = 28f,
        float duration = 0.22f, float delay = 0f)
    {
        if (rect == null)
            return null;

        Vector2 target = rect.anchoredPosition;
        Vector2 start = new Vector2(target.x, target.y - distance);

        if (group != null)
            group.alpha = 0f;

        rect.anchoredPosition = start;

        return UiAnimator.Play(duration, t =>
        {
            rect.anchoredPosition = new Vector2(target.x, start.y + distance * t);

            if (group != null)
                group.alpha = t;
        }, () => rect.anchoredPosition = target, UiEase.OutCubic, delay);
    }

    /// <summary>
    /// 작게 시작해 살짝 넘겼다 제자리 (배지 · 숫자 · 결과 카드)
    /// </summary>
    public static UiTween Pop(Transform target, float from = 0.6f, float duration = 0.28f, float delay = 0f)
    {
        if (target == null)
            return null;

        target.localScale = new Vector3(from, from, 1f);

        return UiAnimator.Play(duration, t =>
        {
            float scale = from + (1f - from) * t;

            target.localScale = new Vector3(scale, scale, 1f);
        }, () => target.localScale = Vector3.one, UiEase.OutBack, delay);
    }

    /// <summary>
    /// 색이 번쩍 들어왔다 원래 색으로 (득점 · 강화 성공 · 승급)
    /// </summary>
    public static UiTween Flash(Graphic graphic, Color flashColor, float duration = 0.45f, float delay = 0f)
    {
        if (graphic == null)
            return null;

        Color original = graphic.color;

        return UiAnimator.Play(duration, t =>
        {
            float amount = UiEase.Pulse(t);

            graphic.color = new Color(
                original.r + (flashColor.r - original.r) * amount,
                original.g + (flashColor.g - original.g) * amount,
                original.b + (flashColor.b - original.b) * amount,
                original.a + (flashColor.a - original.a) * amount);
        }, () => graphic.color = original, UiEase.Linear, delay);
    }

    /// <summary>
    /// 숫자가 올라가며 바뀐다 (훈련 스탯 · 획득 재화)
    /// </summary>
    public static UiTween CountUp(TMP_Text text, int from, int to, string format = "{0}",
        float duration = 0.5f, float delay = 0f)
    {
        if (text == null)
            return null;

        return UiAnimator.Play(duration, t =>
        {
            int value = from + Mathf.RoundToInt((to - from) * t);

            text.text = string.Format(format, value);
        }, () => text.text = string.Format(format, to), UiEase.OutCubic, delay);
    }

    /// <summary>
    /// 가로로 눌렸다 펴지며 앞뒤가 바뀐다. 오버레이 캔버스에서 3D 회전 대신 쓰는 뒤집기
    /// </summary>
    public static UiTween Flip(Transform target, Action onHalfway, float duration = 0.34f)
    {
        if (target == null)
            return null;

        bool swapped = false;

        return UiAnimator.Play(duration, t =>
        {
            float squash = Mathf.Abs(1f - 2f * t);

            if (!swapped && t >= 0.5f)
            {
                swapped = true;
                onHalfway?.Invoke();
            }

            target.localScale = new Vector3(Mathf.Max(0.02f, squash), 1f, 1f);
        }, () => target.localScale = Vector3.one, UiEase.Linear);
    }

    /// <summary>
    /// 스프라이트 프레임을 돌려 회전 아이콘을 만든다 (일시정지 예약됨 표시)
    /// </summary>
    public static UiTween Spin(Image image, string framePathPrefix, float secondsPerLoop = 0.8f)
    {
        if (image == null)
            return null;

        Sprite[] frames = new Sprite[SpinnerFrameCount];
        int loaded = 0;

        for (int i = 0; i < SpinnerFrameCount; i++)
        {
            frames[i] = UiSprites.Get(framePathPrefix + i.ToString("00"));

            if (frames[i] != null)
                loaded++;
        }

        if (loaded == 0)
            return RotateZ(image.rectTransform, secondsPerLoop);

        return UiAnimator.Play(secondsPerLoop, t =>
        {
            int frame = Mathf.Clamp((int)(t * SpinnerFrameCount), 0, SpinnerFrameCount - 1);

            if (frames[frame] != null)
                image.sprite = frames[frame];
        }, null, UiEase.Linear);
    }

    /// <summary>
    /// 멈출 때까지 계속 도는 스피너. 반환한 트윈을 Cancel하면 멈춘다
    /// </summary>
    public static UiTween SpinForever(Image image, string framePathPrefix, float secondsPerLoop = 0.8f)
    {
        if (image == null)
            return null;

        const float totalSeconds = 3600f;

        Sprite[] frames = new Sprite[SpinnerFrameCount];
        int loaded = 0;

        for (int i = 0; i < SpinnerFrameCount; i++)
        {
            frames[i] = UiSprites.Get(framePathPrefix + i.ToString("00"));

            if (frames[i] != null)
                loaded++;
        }

        if (loaded == 0)
        {
            RectTransform rect = image.rectTransform;

            return UiAnimator.Play(totalSeconds, t =>
                    rect.localEulerAngles = new Vector3(0f, 0f, -360f * (t * totalSeconds / secondsPerLoop % 1f)),
                () => rect.localEulerAngles = Vector3.zero, UiEase.Linear);
        }

        return UiAnimator.Play(totalSeconds, t =>
        {
            float loopProgress = t * totalSeconds / secondsPerLoop % 1f;
            int frame = Mathf.Clamp((int)(loopProgress * SpinnerFrameCount), 0, SpinnerFrameCount - 1);

            if (frames[frame] != null)
                image.sprite = frames[frame];
        }, null, UiEase.Linear);
    }

    /// <summary>
    /// Z축 회전 1바퀴. 프레임 애셋이 없을 때의 대체
    /// </summary>
    public static UiTween RotateZ(RectTransform rect, float secondsPerLoop = 0.8f)
    {
        if (rect == null)
            return null;

        return UiAnimator.Play(secondsPerLoop,
            t => rect.localEulerAngles = new Vector3(0f, 0f, -360f * t),
            () => rect.localEulerAngles = Vector3.zero, UiEase.Linear);
    }
}
