using UnityEditor;
using UnityEngine;

/// <summary>
/// 선수 사진(Resources/Photos)의 임포트 설정을 고정한다.
/// 자동 슬라이스를 막아 768×960 틀을 그대로 쓰고, 모바일 압축·밉맵을 맞춘다
/// </summary>
public class PlayerPhotoImporter : AssetPostprocessor
{
    private const string PhotoFolder = "Assets/Resources/Photos/";
    private const string ThumbFolder = PhotoFolder + "Thumb/";
    private const int PhotoMaxSize = 1024;
    private const int ThumbMaxSize = 512;

    private static readonly string[] MobilePlatforms = { "Android", "iPhone" };

    //사진 폴더의 텍스처만 손댄다. Multiple로 들어가면 선수 윤곽만 잘린 조각이 카드에 들어간다
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(PhotoFolder))
            return;

        int maxSize = assetPath.StartsWith(ThumbFolder) ? ThumbMaxSize : PhotoMaxSize;
        TextureImporter importer = (TextureImporter)assetImporter;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = maxSize;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;

        TextureImporterSettings settings = new TextureImporterSettings();

        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(settings);

        foreach (string platform in MobilePlatforms)
        {
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = platform,
                overridden = true,
                maxTextureSize = maxSize,
                format = TextureImporterFormat.ASTC_6x6,
                compressionQuality = (int)TextureCompressionQuality.Best
            });
        }
    }
}
