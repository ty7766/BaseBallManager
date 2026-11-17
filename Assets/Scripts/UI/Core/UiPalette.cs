using UnityEngine;

/// <summary>
/// 경기_UI_명세 6-2 색 토큰. 화면 코드는 색 리터럴을 쓰지 않고 여기만 참조한다
/// </summary>
public static class UiPalette
{
    public static readonly Color BgApp = Hex("#1E2738");           //화면 바탕 · 덮개 (짙은 남회색, 검정은 쓰지 않는다)
    public static readonly Color BgPanel = Hex("#263147");         //패널
    public static readonly Color BgRow = Hex("#3A4866");           //행 · 칩 · 버튼 (바탕보다 한 단계 밝게 떠 보이게)
    public static readonly Color BgSelected = Hex("#4A5A7E");      //선택된 행 바탕
    public static readonly Color BgLight = Hex("#ECEEF1");         //밝은 바탕 (재화 줄 · 글자는 ButtonPrimaryText)

    public static readonly Color LineHair = Hex("#36435E");        //구분선
    public static readonly Color LineBorder = Hex("#5A6B8E");      //테두리

    public static readonly Color Text1 = Hex("#F3F6FA");           //주 글자
    public static readonly Color Text2 = Hex("#A9B4C5");           //보조 글자
    public static readonly Color Text3 = Hex("#8693A9");           //약한 글자
    public static readonly Color TextOff = Hex("#5D6A83");         //비활성 글자

    public static readonly Color ButtonPrimary = Hex("#F3F6FA");   //주 버튼 채움 · 선택 테두리
    public static readonly Color ButtonPrimaryText = Hex("#0A111D");

    public static readonly Color StateReserved = Hex("#8EC5FF");   //일시정지 예약됨
    public static readonly Color StatePitching = Hex("#4FD1C5");   //등판 중
    public static readonly Color StateOut = Hex("#FF4D5A");        //아웃 점 · 체력 위험

    public static readonly Color StaminaHigh = Hex("#4FD1A5");     //50% 초과
    public static readonly Color StaminaMid = Hex("#FFB23F");      //50% 이하
    public static readonly Color StaminaLow = Hex("#FF4D5A");      //20% 이하

    public static readonly Color CardNormal = Hex("#93C1F5");      //노말 OVR · 별
    public static readonly Color CardSignature = Hex("#FF6FA0");   //시그니쳐
    public static readonly Color CardGoldenGlove = Hex("#F2C14E"); //골든글러브

    //카드 앞면 별 (명세 6-6). 등급마다 색이 다르다
    public static readonly Color CardStar3 = Hex("#3D8BFF");       //3성 파랑
    public static readonly Color CardStar4 = Hex("#2FD06A");       //4성 초록
    public static readonly Color CardStar5 = Hex("#A85CFF");       //5성 보라

    public static readonly Color LogNormal = Hex("#DCE4EF");       //중계 평범
    public static readonly Color LogOffense = Hex("#FFA24C");      //중계 공격
    public static readonly Color LogDefense = Hex("#5EC8F2");      //중계 수비
    public static readonly Color LogHomeRun = Hex("#F7D57A");      //중계 홈런

    public static readonly Color AccentGold = Hex("#F2C14E");      //+N점 · R · MY TEAM
    public static readonly Color BrushDefault = Hex("#0E6B32");    //배경 붓질 (팀을 고르기 전 · 타이틀과 같은 짙은 초록)

    private static readonly Color[] TeamColors =
    {
        Hex("#C8102E"), //기아
        Hex("#0C2340"), //삼성
        Hex("#C30452"), //LG
        Hex("#FF6600"), //한화
        Hex("#131230"), //두산
        Hex("#CE0E2D"), //SSG
        Hex("#000000"), //KT
        Hex("#005E7E"), //NC (짙은 청록 · 마린블루)
        Hex("#570514"), //키움
        Hex("#041E42")  //롯데
    };

    //어두운 바탕에 깔 팀 색. 남색 · 검정처럼 바탕에 묻히는 색만 밝게 올렸다 (두산 · NC 는 짙게 따로 맞춤)
    private static readonly Color[] TeamColorsOnDark =
    {
        Hex("#C8102E"), //기아
        Hex("#1D5499"), //삼성
        Hex("#C30452"), //LG
        Hex("#FF6600"), //한화
        Hex("#213078"), //두산 (짙은 군청)
        Hex("#CE0E2D"), //SSG
        Hex("#999999"), //KT
        Hex("#00546F"), //NC
        Hex("#990923"), //키움
        Hex("#094699")  //롯데
    };

    //배경 붓질에 곱할 팀 색. 가는 결이라 칸 바탕보다 밝아야 묻히지 않는다
    private static readonly Color[] TeamBrushColors =
    {
        Hex("#CC102F"), //기아
        Hex("#2670CC"), //삼성
        Hex("#CC0456"), //LG
        Hex("#FF6600"), //한화
        Hex("#2C3C9C"), //두산 (짙은 군청)
        Hex("#CE0E2D"), //SSG
        Hex("#CCCCCC"), //KT
        Hex("#0098CC"), //NC
        Hex("#CC0C2F"), //키움
        Hex("#0C5DCC")  //롯데
    };

    //불투명 칸 바탕에 깔 아주 짙은 팀 색의 밝기 (HSV 의 V)
    private const float TeamDeepValue = 0.16f;

    private static readonly string[] TeamOrder =
    {
        "기아", "삼성", "LG", "한화", "두산", "SSG", "KT", "NC", "키움", "롯데"
    };

    /// <summary>
    /// 팀 고유색. 모르는 팀명이면 패널색을 돌려준다
    /// </summary>
    public static Color GetTeamColor(string teamName)
    {
        int index = IndexOfTeam(teamName);

        return index >= 0 ? TeamColors[index] : BgPanel;
    }

    /// <summary>
    /// 팀 색 목록에 있는 팀인지
    /// </summary>
    public static bool IsKnownTeam(string teamName)
    {
        return IndexOfTeam(teamName) >= 0;
    }

    /// <summary>
    /// 어두운 바탕에 깔 팀 색. 모르는 팀명이면 패널색을 돌려준다
    /// </summary>
    public static Color GetTeamColorOnDark(string teamName)
    {
        int index = IndexOfTeam(teamName);

        return index >= 0 ? TeamColorsOnDark[index] : BgPanel;
    }

    /// <summary>
    /// 배경 붓질에 곱할 팀 색. 모르는 팀명이면 기본 붓질 색을 돌려준다
    /// </summary>
    public static Color GetTeamBrushColor(string teamName)
    {
        int index = IndexOfTeam(teamName);

        return index >= 0 ? TeamBrushColors[index] : BrushDefault;
    }

    /// <summary>
    /// 팀 색 칸을 불투명하게 받칠 아주 짙은 팀 색. 색상 · 채도는 두고 밝기만 낮춘다
    /// </summary>
    public static Color GetTeamColorDeep(string teamName)
    {
        Color.RGBToHSV(GetTeamColorOnDark(teamName), out float hue, out float saturation, out float value);

        return Color.HSVToRGB(hue, saturation, TeamDeepValue);
    }

    /// <summary>
    /// 체력 비율(0~1)에 대응하는 색. 6-2의 3단계
    /// </summary>
    public static Color GetStaminaColor(float ratio)
    {
        if (ratio <= 0.2f)
            return StaminaLow;

        return ratio <= 0.5f ? StaminaMid : StaminaHigh;
    }

    /// <summary>
    /// 알파만 바꾼 사본
    /// </summary>
    public static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    //명세의 16진 토큰을 그대로 옮겨 적기 위한 변환
    //팀 순서표에서의 자리. 없으면 -1
    private static int IndexOfTeam(string teamName)
    {
        for (int i = 0; i < TeamOrder.Length; i++)
        {
            if (TeamOrder[i] == teamName)
                return i;
        }

        return -1;
    }

    private static Color Hex(string html)
    {
        return ColorUtility.TryParseHtmlString(html, out Color parsed) ? parsed : Color.white;
    }
}
