using UnityEditor;
using UnityEngine;

/// <summary>
/// UI 그림 폴더별 임포트 설정을 고정한다. 새 PNG 가 Multiple 스프라이트로 들어가 조각마다 나뉘는 것을 막는다.
/// 엠블럼 = Single 스프라이트(Image), 타이틀 키 아트 = 일반 텍스처(RawImage)
/// </summary>
public class UiArtImporter : AssetPostprocessor
{
    private const string EmblemFolder = "Assets/Resources/UI/Emblems/";
    private const int EmblemMaxSize = 512;
    private const string TitleFolder = "Assets/Resources/UI/Title/";
    private const int TitleMaxSize = 2048;

    //해당 폴더의 텍스처만 손댄다. Multiple 로 들어가면 LG 엠블럼은 TWINS 글자 조각만 잡혔다
    private void OnPreprocessTexture()
    {
        if (assetPath.StartsWith(EmblemFolder))
            ImportEmblem((TextureImporter)assetImporter);
        else if (assetPath.StartsWith(TitleFolder))
            ImportTitleArt((TextureImporter)assetImporter);
    }

    //엠블럼: 축소해 그려지므로 밉맵을 켠다
    private static void ImportEmblem(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = EmblemMaxSize;

        TextureImporterSettings settings = new TextureImporterSettings();

        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(settings);
    }

    //타이틀 층: 화면 크기 그대로 RawImage 에 깔린다. 스프라이트 · 밉맵이 필요 없다
    private static void ImportTitleArt(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = TitleMaxSize;
    }
}
