using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 버튼을 누르는 동안 살짝 작아지고 어두워졌다가, 떼면 튕기듯 돌아온다. 누를 수 없는 버튼은 반응하지 않는다.
/// UiBuilder.CreateButton 이 모든 버튼에 붙인다
/// </summary>
[DisallowMultipleComponent]
public class UiPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    private const float PressedScale = 0.94f;
    private const float PressSeconds = 0.07f;
    private const float ReleaseSeconds = 0.24f;
    //누른 동안 바탕을 이만큼 어둡게 (색에 곱한다)
    private const float PressedShade = 0.82f;

    private Selectable _selectable;
    private UiTween _tween;
    private bool _pressed;

    private void Awake()
    {
        _selectable = GetComponent<Selectable>();
    }

    //꺼질 때 눌린 채로 굳지 않게 되돌린다
    private void OnDisable()
    {
        UiAnimator.Cancel(_tween);
        _tween = null;
        _pressed = false;
        transform.localScale = Vector3.one;
        SetShade(1f, true);
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
    {
        if (_selectable != null && !_selectable.IsInteractable())
            return;

        _pressed = true;
        CenterPivot();
        SetShade(PressedShade, false);
        AnimateScale(PressedScale, PressSeconds, UiEase.OutCubic);
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    private void Release()
    {
        if (!_pressed)
            return;

        _pressed = false;
        SetShade(1f, false);
        AnimateScale(1f, ReleaseSeconds, UiEase.OutBack);
    }

    private void AnimateScale(float target, float seconds, Func<float, float> ease)
    {
        float from = transform.localScale.x;

        UiAnimator.Cancel(_tween);
        _tween = UiAnimator.Play(seconds, t =>
        {
            float scale = Mathf.LerpUnclamped(from, target, t);

            transform.localScale = new Vector3(scale, scale, 1f);
        }, () => _tween = null, ease);
    }

    //바탕 그림 색은 그대로 두고 그려지는 색만 곱한다 (버튼 색을 바꾸는 코드와 겹치지 않는다)
    private void SetShade(float shade, bool instant)
    {
        if (_selectable == null || _selectable.targetGraphic == null)
            return;

        _selectable.targetGraphic.CrossFadeColor(new Color(shade, shade, shade, 1f), instant ? 0f : PressSeconds, true, true);
    }

    //가운데로 줄어들도록 기준점을 옮기고, 그 때문에 밀리는 거리(sizeDelta × 이동량 — 늘어나는 버튼은 실제 크기가 아니다)를 되돌린다
    private void CenterPivot()
    {
        RectTransform rect = (RectTransform)transform;
        Vector2 center = new Vector2(0.5f, 0.5f);

        if (rect.pivot == center)
            return;

        Vector2 shift = Vector2.Scale(center - rect.pivot, rect.sizeDelta);

        rect.pivot = center;
        rect.anchoredPosition += shift;
    }
}
