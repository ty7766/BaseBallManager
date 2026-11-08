using System;
using UnityEngine;

/// <summary>
/// 시간에 따라 진행값을 흘려주는 애니메이션 1개.
/// 뷰가 MonoBehaviour가 아니라 코루틴을 못 쓰므로 UiAnimator가 중앙에서 돌린다
/// </summary>
public class UiTween
{
    private readonly float _duration;
    private readonly float _delay;
    private readonly Func<float, float> _ease;
    private readonly Action<float> _onUpdate;
    private readonly Action _onComplete;

    private float _elapsed;

    public UiTween(float duration, Action<float> onUpdate,
        Action onComplete = null, Func<float, float> ease = null, float delay = 0f)
    {
        _duration = Mathf.Max(0.0001f, duration);
        _delay = Mathf.Max(0f, delay);
        _onUpdate = onUpdate;
        _onComplete = onComplete;
        _ease = ease ?? UiEase.OutCubic;
    }

    /// <summary>
    /// 끝났거나 취소되었는지. UiAnimator가 목록에서 걷어내는 기준
    /// </summary>
    public bool IsDone { get; private set; }

    /// <summary>
    /// 완료 콜백 없이 즉시 중단한다
    /// </summary>
    public void Cancel()
    {
        IsDone = true;
    }

    /// <summary>
    /// 한 프레임 진행. 끝나면 완료 콜백을 부른다
    /// </summary>
    public void Tick(float deltaSeconds)
    {
        if (IsDone)
            return;

        _elapsed += deltaSeconds;

        if (_elapsed < _delay)
            return;

        float progress = Mathf.Clamp01((_elapsed - _delay) / _duration);

        _onUpdate?.Invoke(_ease(progress));

        if (progress < 1f)
            return;

        IsDone = true;
        _onComplete?.Invoke();
    }
}
