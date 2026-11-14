using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 선수 얼굴 칸. 누끼 사진의 머리 · 어깨만 깎인 칸 안에 잘라 보이고, 칸 바탕은 카드 등급 색으로 옅게 물든다
/// </summary>
public class PlayerFaceView : UiWidgetBehaviour
{
    //사진(768×960)에서 칸에 들어올 부분: 가로 가운데 60%, 위쪽 4% 아래부터
    private const float CropWidthRatio = 0.6f;
    private const float CropTopRatio = 0.04f;
    private const float PhotoAspect = 1.25f;
    private const float BackgroundAlpha = 0.42f;

    [SerializeField] private Image _background;
    [SerializeField] private Image _photo;

    /// <summary>
    /// 위젯을 조립한다 (프리팹 베이크 시점)
    /// </summary>
    public static PlayerFaceView Bake(Transform parent, float width, float height)
    {
        PlayerFaceView view = CreateNode<PlayerFaceView>(parent, "PlayerFace");

        view.BuildWidget(width, height);

        return view;
    }

    /// <summary>
    /// 카드 사진과 등급 색을 채운다
    /// </summary>
    public void SetCard(CardDisplayInfo info)
    {
        CardStyle style = CardStyle.Get(info.CardType, info.CardGrade);
        Sprite photo = PlayerPhotoProvider.Get(info.PhotoKey, true);

        _background.color = UiPalette.WithAlpha(style.GradeColor, BackgroundAlpha);
        _photo.sprite = photo;
        _photo.enabled = photo != null;
    }

    /// <summary>
    /// 카드 정보를 못 찾았을 때 빈 칸
    /// </summary>
    public void SetEmpty()
    {
        _background.color = UiPalette.BgRow;
        _photo.enabled = false;
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(float width, float height)
    {
        UiBuilder.Place(Root, 0f, 0f, width, height);

        _background = UiBuilder.CreateImage(Root, "Background", UiPalette.BgRow, UiSprites.CutFillSmall, Image.Type.Sliced);
        UiBuilder.Stretch(_background.rectTransform, 0f, 0f, 0f, 0f);
        _background.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        float photoWidth = width / CropWidthRatio;
        float photoHeight = photoWidth * PhotoAspect;

        _photo = UiBuilder.CreateImage(_background.transform, "Photo", Color.white);
        UiBuilder.Place(_photo.rectTransform, -(photoWidth - width) * 0.5f, -CropTopRatio * photoHeight, photoWidth, photoHeight);
        _photo.enabled = false;
    }
}
