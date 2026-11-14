using UnityEditor;
using UnityEngine;

/// <summary>
/// UI 그림 폴더별 임포트 설정을 고정한다. 새 PNG 가 Multiple 스프라이트로 들어가 조각마다 나뉘는 것을 막는다.
/// 엠블럼 · 로비 아이콘 = Single 스프라이트(Image), 타이틀 키 아트 · 공용 배경 · 로비 이펙트 = 일반 텍스처(RawImage)
/// </summary>
public class UiArtImporter : AssetPostprocessor
{
    private const string EmblemFolder = "Assets/Resources/UI/Emblems/";
    private const int EmblemMaxSize = 512;
    private const string LobbyIconFolder = "Assets/Resources/UI/Lobby/Icons/";
    private const int LobbyIconMaxSize = 256;
    private const string LobbyFolder = "Assets/Resources/UI/Lobby/";
    private const string TitleFolder = "Assets/Resources/UI/Title/";
    private const string BackdropFolder = "Assets/Resources/UI/Backdrop/";
    private const int FullScreenMaxSize = 2048;

    //해당 폴더의 텍스처만 손댄다. Multiple 로 들어가면 LG 엠블럼은 TWINS 글자 조각만 잡혔다
    private void OnPreprocessTexture()
    {
        if (assetPath.StartsWith(EmblemFolder))
            ImportSingleSprite((TextureImporter)assetImporter, EmblemMaxSize);
        else if (assetPath.StartsWith(LobbyIconFolder))
            ImportSingleSprite((TextureImporter)assetImporter, LobbyIconMaxSize);
        else if (assetPath.StartsWith(TitleFolder) || assetPath.StartsWith(BackdropFolder) || assetPath.StartsWith(LobbyFolder))
            ImportFullScreenArt((TextureImporter)assetImporter);
    }

    //엠블럼 · 아이콘: 축소해 그려지므로 밉맵을 켠다
    private static void ImportSingleSprite(TextureImporter importer, int maxSize)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = maxSize;

        TextureImporterSettings settings = new TextureImporterSettings();

        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(settings);
    }

    //타이틀 층 · 공용 배경: 화면 크기 그대로 RawImage 에 깔린다. 스프라이트 · 밉맵이 필요 없다
    private static void ImportFullScreenArt(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = FullScreenMaxSize;
    }
}
