using TMPro;
using UnityEngine;

/// <summary>
/// 한글 글리프를 가진 TMP 폰트 자산을 런타임에 만들어 캐시한다.
/// 프로젝트에 한글 폰트 자산이 없어 OS 폰트에서 동적으로 뽑는다
/// </summary>
public static class UiFontProvider
{
    /// <summary>
    /// 에디터에서 구워 둔 폰트 자산의 Resources 경로. 있으면 이것을 먼저 쓴다
    /// </summary>
    public const string AssetResourcePath = "UI/Fonts/UiKoreanFont";

    /// <summary>
    /// 카드 앞면 폰트 자산(이사만루 · 공게임즈)의 Resources 경로. 출처 표기 조건이라 README 크레딧에 남긴다
    /// </summary>
    public const string CardAssetResourcePath = "UI/Fonts/Card/CardFont";

    private static readonly string[] CandidateFamilies =
    {
        "Pretendard", "Malgun Gothic", "맑은 고딕", "Gulim", "Batang", "Dotum"
    };

    private static TMP_FontAsset _korean;
    private static bool _resolved;
    private static TMP_FontAsset _card;

    /// <summary>
    /// 한글 본문용 폰트. 만들지 못하면 TMP 기본 폰트로 떨어진다(한글이 사각형으로 보인다)
    /// </summary>
    public static TMP_FontAsset Korean
    {
        get
        {
            if (!_resolved)
                Resolve();

            return _korean;
        }
    }

    /// <summary>
    /// 카드 앞면 OVR · 포지션 · 이름 · 강화 숫자 폰트. 자산이 없으면 본문 폰트로 떨어진다
    /// </summary>
    public static TMP_FontAsset Card
    {
        get
        {
            if (_card != null)
                return _card;

            _card = Resources.Load<TMP_FontAsset>(CardAssetResourcePath);

            if (_card == null)
                _card = Korean;

            return _card;
        }
    }

    //구워 둔 자산을 먼저 찾고, 없으면 OS 폰트 패밀리를 차례로 시도한다
    private static void Resolve()
    {
        _resolved = true;
        _korean = Resources.Load<TMP_FontAsset>(AssetResourcePath);

        if (_korean != null)
            return;

        foreach (string family in CandidateFamilies)
        {
            TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(family, "Regular", 48);

            if (created == null)
                continue;

            _korean = created;

            Debug.Log($"[UiFontProvider]: '{family}' 폰트로 UI 글자를 그립니다");
            return;
        }

        _korean = TMP_Settings.defaultFontAsset;

        Debug.LogWarning("[UiFontProvider]: 한글 폰트를 찾지 못해 TMP 기본 폰트를 씁니다 (한글이 깨질 수 있습니다)");
    }
}
