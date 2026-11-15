using UnityEditor;
using UnityEngine;

/// <summary>
/// 선수 사진(Resources/Photos)의 임포트 설정을 고정한다.
/// 자동 슬라이스를 막아 768×960 틀을 그대로 쓰고, 모바일은 ASTC 로 누른다. NPOT 에 밉맵을 켜면 압축이 풀려 RGBA32 로 들어가므로
/// 360폭 넘는 카드에만 쓰는 원본은 밉맵을 끄고, 작게 줄여 그리는 썸네일만 켠다
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

        bool isThumb = assetPath.StartsWith(ThumbFolder);
        int maxSize = isThumb ? ThumbMaxSize : PhotoMaxSize;
        TextureImporter importer = (TextureImporter)assetImporter;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = isThumb;
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
