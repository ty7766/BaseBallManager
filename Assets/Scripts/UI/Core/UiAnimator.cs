using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 돌고 있는 트윈을 한곳에서 갱신한다. 흐름 제어기의 Update가 Tick을 불러준다.
/// 뷰가 MonoBehaviour가 아니어도 애니메이션을 쓸 수 있게 하는 축
/// </summary>
public static class UiAnimator
{
    private static readonly List<UiTween> Active = new List<UiTween>(64);
    private static readonly List<UiTween> Pending = new List<UiTween>(16);

    private static bool _ticking;

    /// <summary>
    /// 트윈을 시작한다
    /// </summary>
    public static UiTween Play(float duration, Action<float> onUpdate,
        Action onComplete = null, Func<float, float> ease = null, float delay = 0f)
    {
        UiTween tween = new UiTween(duration, onUpdate, onComplete, ease, delay);

        //Tick 도중에 추가되면 목록이 흔들리므로 다음 프레임에 합친다
        if (_ticking)
            Pending.Add(tween);
        else
            Active.Add(tween);

        return tween;
    }

    /// <summary>
    /// 지정한 트윈을 중단한다 (null도 허용)
    /// </summary>
    public static void Cancel(UiTween tween)
    {
        tween?.Cancel();
    }

    /// <summary>
    /// 모든 트윈을 중단한다 (화면을 갈아엎을 때)
    /// </summary>
    public static void CancelAll()
    {
        foreach (UiTween tween in Active)
            tween.Cancel();

        Pending.Clear();
    }

    /// <summary>
    /// 한 프레임 진행. Time.unscaledDeltaTime을 넘긴다
    /// </summary>
    public static void Tick(float deltaSeconds)
    {
        if (Active.Count == 0 && Pending.Count == 0)
            return;

        _ticking = true;

        for (int i = 0; i < Active.Count; i++)
            Active[i].Tick(deltaSeconds);

        _ticking = false;

        for (int i = Active.Count - 1; i >= 0; i--)
        {
            if (Active[i].IsDone)
                Active.RemoveAt(i);
        }

        if (Pending.Count == 0)
            return;

        Active.AddRange(Pending);
        Pending.Clear();
    }
}
