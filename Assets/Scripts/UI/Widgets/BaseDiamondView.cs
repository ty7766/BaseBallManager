using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 주자 상황 다이아몬드 (명세 1-4). 1루 우 · 2루 상 · 3루 좌 · 홈 하
/// </summary>
public class BaseDiamondView
{
    private const float BaseSize = 44f;

    public RectTransform Root => _root;

    private readonly RectTransform _root;
    private readonly Image _first;
    private readonly Image _second;
    private readonly Image _third;

    public BaseDiamondView(Transform parent, float size)
    {
        _root = UiBuilder.CreateRect(parent, "BaseDiamond");
        UiBuilder.Place(_root, 0f, 0f, size, size);

        float center = (size - BaseSize) * 0.5f;
        float edge = size - BaseSize;

        _second = CreateBase("Second", center, 0f);
        _third = CreateBase("Third", 0f, center);
        _first = CreateBase("First", edge, center);

        Image home = UiBuilder.CreateImage(_root, "Home", UiPalette.TextOff, UiSprites.BaseHome);
        UiBuilder.Place(home.rectTransform, center, edge, BaseSize, BaseSize);
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

    private Image CreateBase(string name, float x, float y)
    {
        Image image = UiBuilder.CreateImage(_root, name, UiPalette.LineBorder, UiSprites.BaseDiamondEmpty);

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
