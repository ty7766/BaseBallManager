using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 탭 버튼의 선택/비선택 모양을 한 곳에서 칠한다.
/// 화면마다 같은 코드를 복사하던 것을 모았다
/// </summary>
public static class UiTabStyle
{
    /// <summary>
    /// 탭 버튼에 선택 여부를 반영한다
    /// </summary>
    public static void Apply(Button button, bool selected)
    {
        if (button == null)
            return;

        Image background = button.GetComponent<Image>();

        if (background != null)
        {
            background.sprite = UiSprites.Get(selected ? UiSprites.TabOn : UiSprites.ButtonNormal);

            //tab_on 은 어두운 채움 + 아래쪽 강조선을 그림에 이미 담고 있어 흰색으로 그대로 통과시킨다
            background.color = selected ? Color.white : UiPalette.BgRow;
        }

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();

        if (label != null)
            label.color = selected ? UiPalette.Text1 : UiPalette.Text2;
    }
}
