using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 투수 체력 바 (명세 ②). 50% 지점에 눈금, 구간별로 색이 바뀐다. "체력 N/M" 은 바 왼쪽 안에 쓴다 —
/// 채움이 글자를 덮을 만큼 길면 진한 글자, 짧아 빈 칸 위에 서면 흰 글자
/// </summary>
public class StaminaBarView : UiWidgetBehaviour
{
    private const float FillInset = 3f;
    private const float TextLeft = 14f;
    //글자 폭이 바의 이 비율쯤이다. 채움이 이보다 길어야 진한 글자가 채움 위에 온전히 선다
    private const float DarkTextMinRatio = 0.42f;

    [SerializeField] private Image _fill;
    [SerializeField] private TextMeshProUGUI _valueText;
    [SerializeField] private float _width;

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static StaminaBarView Bake(Transform parent, float width, float height)
    {
        StaminaBarView view = CreateNode<StaminaBarView>(parent, "StaminaBar");

        view.BuildWidget(width, height);

        return view;
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

        UiBuilder.Place(_fill.rectTransform, FillInset, FillInset, Mathf.Max(0f, (_width - FillInset * 2f) * ratio), height - FillInset * 2f);
        _fill.color = UiPalette.GetStaminaColor(ratio);
        _valueText.text = $"체력 {pitcherState.CurrentStamina}/{max}";
        _valueText.color = ratio >= DarkTextMinRatio ? UiPalette.ButtonPrimaryText : UiPalette.Text1;
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(float width, float height)
    {
        _width = width;
        UiBuilder.Place(Root, 0f, 0f, width, height);

        Image frame = UiBuilder.CreateImage(Root, "Frame", UiPalette.LineBorder,
            UiSprites.StaminaFrame, Image.Type.Sliced);
        UiBuilder.Stretch(frame.rectTransform, 0f, 0f, 0f, 0f);

        _fill = UiBuilder.CreateImage(Root, "Fill", UiPalette.StaminaHigh,
            UiSprites.StaminaFill, Image.Type.Sliced);
        UiBuilder.Place(_fill.rectTransform, FillInset, FillInset, width - FillInset * 2f, height - FillInset * 2f);

        Image tick = UiBuilder.CreateImage(Root, "Tick", UiPalette.WithAlpha(UiPalette.Text1, 0.4f),
            UiSprites.StaminaTick);
        UiBuilder.Place(tick.rectTransform, width * 0.5f - 2f, 0f, 4f, height);

        _valueText = UiBuilder.CreateText(Root, "Value", height * 0.58f, UiPalette.ButtonPrimaryText);
        UiBuilder.Place(_valueText.rectTransform, TextLeft, 0f, width - TextLeft * 2f, height);
        _valueText.fontStyle = FontStyles.Bold;
    }
}
