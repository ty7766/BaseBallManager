using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 투수 체력 바 (명세 ②). 50% 지점에 눈금, 구간별로 색이 바뀐다
/// </summary>
public class StaminaBarView : UiWidgetBehaviour
{
    [SerializeField] private Image _fill;
    [SerializeField] private TextMeshProUGUI _valueText;
    [SerializeField] private float _width;

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static StaminaBarView Bake(Transform parent, float width, float height)
    {
        StaminaBarView view = CreateNode<StaminaBarView>(parent, "StaminaBar");

        view.BuildWidget(parent, width, height);

        return view;
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(Transform parent, float width, float height)
    {
        _width = width;

        UiBuilder.Place(Root, 0f, 0f, width, height);

        Image frame = UiBuilder.CreateImage(Root, "Frame", UiPalette.LineBorder,
            UiSprites.StaminaFrame, Image.Type.Sliced);
        UiBuilder.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);

        _fill = UiBuilder.CreateImage(Root, "Fill", UiPalette.StaminaHigh,
            UiSprites.StaminaFill, Image.Type.Sliced);
        UiBuilder.Place(_fill.rectTransform, 3f, 3f, width - 6f, height - 6f);

        Image tick = UiBuilder.CreateImage(Root, "Tick", UiPalette.WithAlpha(UiPalette.Text1, 0.4f),
            UiSprites.StaminaTick);
        UiBuilder.Place(tick.rectTransform, width * 0.5f - 2f, 0f, 4f, height);

        _valueText = UiBuilder.CreateText(Root, "Value", 22f, UiPalette.Text2, TextAlignmentOptions.Right);
        UiBuilder.Place(_valueText.rectTransform, width - 160f, height + 4f, 160f, 26f);
    }

    /// <summary>
    /// 남은 체력 비율을 반영한다. 투수가 없으면 숨긴다
    /// </summary>
    public void Refresh(PitcherState pitcherState)
    {
        if (pitcherState == null)
        {
            Root.gameObject.SetActive(false);
            return;
        }

        Root.gameObject.SetActive(true);

        int max = Mathf.Max(1, pitcherState.Snapshot.Stamina);
        float ratio = Mathf.Clamp01(pitcherState.GetFatigueRatio());
        float height = Root.sizeDelta.y;

        UiBuilder.Place(_fill.rectTransform, 3f, 3f, Mathf.Max(0f, (_width - 6f) * ratio), height - 6f);
        _fill.color = UiPalette.GetStaminaColor(ratio);

        _valueText.text = $"체력 {pitcherState.CurrentStamina} / {max}";
    }
}
