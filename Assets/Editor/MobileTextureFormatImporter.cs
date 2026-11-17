using UnityEditor;

/// <summary>
/// Resources 아래 모든 텍스처의 모바일(Android · iPhone) 압축 형식을 한곳에서 정한다. 에디터와 기기 화면이 같아야 하므로
/// 가장자리가 또렷하거나 작게 줄여 그리는 그림은 무손실 RGBA32, 크고 부드러운 그림만 ASTC 4×4(원본 대비 50dB 이상 실측)로 누른다
/// </summary>
public class MobileTextureFormatImporter : AssetPostprocessor
{
    private const string ResourcesFolder = "Assets/Resources/";
    private const string ThumbFolder = "Assets/Resources/Photos/Thumb/";
    private const string LobbyIconFolder = "Assets/Resources/UI/Lobby/Icons/";
    //폴더 전용 임포터(사진 · 카드 · UI)가 크기 · 밉맵을 먼저 정한 뒤에 돈다
    private const int RunAfterFolderImporters = 100;

    //사진 · 배경 · 연출처럼 크고 부드러운 그림. ASTC 6×6 은 가장자리 오차가 최대 141 까지 나서 쓰지 않는다
    private static readonly string[] NearLosslessFolders =
    {
        "Assets/Resources/Photos/",
        "Assets/Resources/UI/Backdrop/",
        "Assets/Resources/UI/Gacha/",
        "Assets/Resources/UI/Lineup/",
        "Assets/Resources/UI/Lobby/",
        "Assets/Resources/UI/MatchFx/",
        "Assets/Resources/UI/Title/"
    };
    private static readonly string[] MobilePlatforms = { "Android", "iPhone" };

    //NPOT 에 밉맵이 켜진 그림은 ASTC 를 골라도 RGBA32 로 풀리므로 카드 아트 · 썸네일은 처음부터 무손실로 둔다
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ResourcesFolder))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        TextureImporterFormat format = IsNearLossless(assetPath) ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.RGBA32;

        foreach (string platform in MobilePlatforms)
        {
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = platform,
                overridden = true,
                maxTextureSize = importer.maxTextureSize,
                format = format,
                compressionQuality = (int)TextureCompressionQuality.Best
            });
        }
    }

    /// <summary>
    /// 폴더 전용 임포터보다 늦게 돌아 그쪽이 정한 최대 크기를 그대로 받는다
    /// </summary>
    public override int GetPostprocessOrder()
    {
        return RunAfterFolderImporters;
    }

    //썸네일 · 로비 아이콘은 누르는 폴더 줄기 안에 있어도 무손실로 둔다
    private static bool IsNearLossless(string path)
    {
        if (path.StartsWith(ThumbFolder) || path.StartsWith(LobbyIconFolder))
            return false;

        foreach (string folder in NearLosslessFolders)
        {
            if (path.StartsWith(folder))
                return true;
        }

        return false;
    }
}
