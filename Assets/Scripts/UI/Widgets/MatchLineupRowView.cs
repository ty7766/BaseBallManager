using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 경기 전 화면 타순 한 줄. 타순 · 포지션 · 얼굴 사진 · 이름 '연도 · 별 · OVR.
/// 높이는 부모 줄 칸을 따라가고 내용은 세로 가운데에 선다
/// </summary>
public class MatchLineupRowView : UiWidgetBehaviour
{
    private const int MaxStars = 5;
    private const float StarSize = 22f;
    private const float StarGap = 3f;
    private const float FaceWidth = 74f;
    private const float FaceHeight = 92f;
    private const float NameLeft = 194f;
    private const float OvrWidth = 92f;
    //선형 색 공간에서 흰 저알파는 생각보다 밝게 뜬다
    private const float StripeAlpha = 0.015f;
    private const string YearCloseTag = "</color></size>";

    //연도는 이름보다 작고 옅게, 굵기 없이
    private static readonly string YearOpenTag = $"<size=76%><color=#{ColorUtility.ToHtmlStringRGB(UiPalette.Text2)}> '";

    [SerializeField] private TextMeshProUGUI _orderText;
    [SerializeField] private TextMeshProUGUI _positionText;
    [SerializeField] private PlayerFaceView _face;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _ovrText;
    [SerializeField] private RectTransform _starRow;
    [SerializeField] private Image[] _stars = new Image[MaxStars];

    /// <summary>
    /// 위젯을 조립한다. 줄 위치는 호출부가 PlaceRowSlot 으로 잡는다 (프리팹 베이크 시점)
    /// </summary>
    public static MatchLineupRowView Bake(Transform parent, float width, bool striped)
    {
        MatchLineupRowView view = CreateNode<MatchLineupRowView>(parent, "LineupRow");

        view.BuildWidget(width, striped);

        return view;
    }

    /// <summary>
    /// 타자 1명. 내 카드만 별 · 카드 종류 색 OVR 을 보여 준다
    /// </summary>
    public void SetHitter(int battingOrder, HitterSnapshot snapshot, bool isPlayerCard)
    {
        _orderText.text = battingOrder.ToString();
        _positionText.text = snapshot.Position;
        _nameText.text = "<b>" + snapshot.Name + "</b>" + YearOpenTag + PlayerLabelFormat.Year(snapshot.Year) + YearCloseTag;
        _ovrText.text = snapshot.OVR.ToString();
        _ovrText.color = UiPalette.Text1;
        HideStars();

        if (!CardDisplayInfo.TryCreateForSnapshot(snapshot.InstanceId, isPlayerCard, out CardDisplayInfo info))
        {
            _face.SetEmpty();
            return;
        }

        _face.SetCard(info);

        if (isPlayerCard)
            ShowCardGrade(info);
    }

    /// <summary>
    /// 타순이 비었을 때
    /// </summary>
    public void SetEmpty()
    {
        _orderText.text = string.Empty;
        _positionText.text = string.Empty;
        _nameText.text = "—";
        _ovrText.text = string.Empty;
        _face.SetEmpty();
        HideStars();
    }

    //생성자 본문. 베이크에서 한 번만 돈다
    private void BuildWidget(float width, bool striped)
    {
        if (striped)
        {
            Image stripe = UiBuilder.CreateImage(Root, "Stripe", UiPalette.WithAlpha(Color.white, StripeAlpha));
            UiBuilder.Stretch(stripe.rectTransform, 0f, 0f, 0f, 0f);
        }

        _orderText = BroadcastPanel.CreateNumber(Root, "Order", 30f, UiPalette.Text3);
        UiBuilder.PlaceMiddle(_orderText.rectTransform, 10f, 0f, 36f, 40f);

        _positionText = UiBuilder.CreateText(Root, "Position", 26f, UiPalette.Text2, TextAlignmentOptions.Center);
        UiBuilder.PlaceMiddle(_positionText.rectTransform, 48f, 0f, 50f, 36f);

        _face = PlayerFaceView.Bake(Root, FaceWidth, FaceHeight);
        UiBuilder.PlaceMiddle(_face.Root, 106f, 0f, FaceWidth, FaceHeight);

        float nameWidth = width - NameLeft - OvrWidth - 16f;

        _nameText = UiBuilder.CreateText(Root, "Name", 34f, UiPalette.Text1);
        UiBuilder.PlaceMiddle(_nameText.rectTransform, NameLeft, 12f, nameWidth, 44f);
        _nameText.overflowMode = TextOverflowModes.Ellipsis;

        _starRow = UiBuilder.CreateRect(Root, "Stars");
        UiBuilder.PlaceMiddle(_starRow, NameLeft, -22f, MaxStars * (StarSize + StarGap), StarSize);

        for (int i = 0; i < MaxStars; i++)
        {
            _stars[i] = UiBuilder.CreateImage(_starRow, "Star", UiPalette.CardNormal, UiSprites.IconStar);
            UiBuilder.Place(_stars[i].rectTransform, i * (StarSize + StarGap), 0f, StarSize, StarSize);
            _stars[i].gameObject.SetActive(false);
        }

        _ovrText = BroadcastPanel.CreateNumber(Root, "Ovr", 46f, UiPalette.Text1, TextAlignmentOptions.Right);
        UiBuilder.PlaceMiddleRight(_ovrText.rectTransform, 18f, 0f, OvrWidth, 54f);
    }

    //카드 종류 색으로 별 개수 · OVR 을 칠한다
    private void ShowCardGrade(CardDisplayInfo info)
    {
        Color typeColor = CardVisualLookup.GetTypeColor(info.CardType);
        int starCount = (int)info.CardGrade;

        _ovrText.color = typeColor;

        for (int i = 0; i < MaxStars; i++)
        {
            bool shown = i < starCount;

            _stars[i].gameObject.SetActive(shown);

            if (shown)
                _stars[i].color = typeColor;
        }
    }

    private void HideStars()
    {
        for (int i = 0; i < MaxStars; i++)
            _stars[i].gameObject.SetActive(false);
    }
}
