using TMPro;
using UnityEngine;

/// <summary>
/// 한글 글리프를 가진 TMP 폰트 자산을 런타임에 만들어 캐시한다.
/// 프로젝트에 한글 폰트 자산이 없어 OS 폰트에서 동적으로 뽑는다
/// </summary>
public static class UiFontProvider
{
    private static readonly string[] CandidateFamilies =
    {
        "Pretendard", "Malgun Gothic", "맑은 고딕", "Gulim", "Batang", "Dotum"
    };

    private static TMP_FontAsset _korean;
    private static bool _resolved;

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

    //OS 폰트 패밀리를 차례로 시도한다. 성공하면 동적 아틀라스로 글리프를 그때그때 굽는다
    private static void Resolve()
    {
        _resolved = true;

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
