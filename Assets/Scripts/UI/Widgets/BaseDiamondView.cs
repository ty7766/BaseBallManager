using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 주자 상황 다이아몬드 (명세 1-4). 1루 우 · 2루 상 · 3루 좌 · 홈 하
/// </summary>
public class BaseDiamondView : UiWidgetBehaviour
{
    private const float BaseSize = 44f;

    [SerializeField] private Image _first;
    [SerializeField] private Image _second;
    [SerializeField] private Image _third;
    [SerializeField] private Image _runnerDot;

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static BaseDiamondView Bake(Transform parent, float size)
    {
        BaseDiamondView view = CreateNode<BaseDiamondView>(parent, "BaseDiamond");

        view.BuildWidget(parent, size);

        return view;
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(Transform parent, float size)
    {
        UiBuilder.Place(Root, 0f, 0f, size, size);

        float center = (size - BaseSize) * 0.5f;
        float edge = size - BaseSize;

        _second = CreateBase("Second", center, 0f);
        _third = CreateBase("Third", 0f, center);
        _first = CreateBase("First", edge, center);

        Image home = UiBuilder.CreateImage(Root, "Home", UiPalette.TextOff, UiSprites.BaseHome);
        UiBuilder.Place(home.rectTransform, center, edge, BaseSize, BaseSize);

        _runnerDot = UiBuilder.CreateImage(Root, "RunnerDot", UiPalette.StatePitching,
            UiSprites.BaseDiamondOccupied);
        UiBuilder.Place(_runnerDot.rectTransform, center, center, BaseSize * 0.55f, BaseSize * 0.55f);
        _runnerDot.gameObject.SetActive(false);
    }

    /// <summary>
    /// 주자 유무를 현재 상태로 갱신한다
    /// </summary>
    public void Refresh(GameState state)
    {
        if (state == null)
            return;

        SetOccupied(_first, state.FirstBase != GameState.NoRunner);
        SetOccupied(_second, state.SecondBase != GameState.NoRunner);
        SetOccupied(_third, state.ThirdBase != GameState.NoRunner);
    }

    /// <summary>
    /// 도루 연출. 주자 점이 출발 베이스에서 도착 베이스로 움직인다
    /// </summary>
    public void PlaySteal(int fromBase, bool isSuccess)
    {
        RectTransform origin = GetBaseRect(fromBase);
        RectTransform destination = GetBaseRect(fromBase + 1);

        if (origin == null || destination == null)
            return;

        Vector2 start = origin.anchoredPosition;
        Vector2 end = destination.anchoredPosition;

        _runnerDot.gameObject.SetActive(true);
        _runnerDot.color = isSuccess ? UiPalette.StatePitching : UiPalette.StateOut;
        _runnerDot.rectTransform.anchoredPosition = start;

        UiAnimator.Play(0.45f,
            t => _runnerDot.rectTransform.anchoredPosition =
                new Vector2(start.x + (end.x - start.x) * t, start.y + (end.y - start.y) * t),
            () => OnStealAnimationEnded(destination, isSuccess),
            UiEase.OutCubic);
    }

    //도착 뒤 처리. 성공이면 도착 베이스가 튀고 파티클이 터진다
    private void OnStealAnimationEnded(RectTransform destination, bool isSuccess)
    {
        _runnerDot.gameObject.SetActive(false);

        if (!isSuccess)
            return;

        UiEffects.Pop(destination, 0.7f, 0.26f);
        UiEffects.Burst(destination, UiEffectKind.Steal);
    }

    //1 = 1루 · 2 = 2루 · 3 = 3루
    private RectTransform GetBaseRect(int baseNumber)
    {
        return baseNumber switch
        {
            1 => _first.rectTransform,
            2 => _second.rectTransform,
            3 => _third.rectTransform,
            _ => null
        };
    }

    private Image CreateBase(string name, float x, float y)
    {
        Image image = UiBuilder.CreateImage(Root, name, UiPalette.LineBorder, UiSprites.BaseDiamondEmpty);

        UiBuilder.Place(image.rectTransform, x, y, BaseSize, BaseSize);

        return image;
    }

    //주자 있는 베이스는 흰색 채움 (명세 6-4)
    private static void SetOccupied(Image image, bool occupied)
    {
        image.sprite = UiSprites.Get(occupied ? UiSprites.BaseDiamondOccupied : UiSprites.BaseDiamondEmpty);
        image.color = occupied ? UiPalette.Text1 : UiPalette.LineBorder;
    }
}
