using UnityEditor;
using UnityEngine;

/// <summary>
/// 카드 아트(Resources/UI/Card 의 배경·전경·장식·메달·별)의 임포트 설정을 고정한다.
/// 카드가 150~700폭으로 줄어 그려지므로 밉맵을 켜고, 모바일은 ASTC 로 누른다
/// </summary>
public class CardArtImporter : AssetPostprocessor
{
    private const string CardFolder = "Assets/Resources/UI/Card/";
    private const int MaxSize = 2048;

    //카드 한 장을 덮는 큰 그림. 작은 메달·별은 가장자리가 뭉개지지 않게 더 촘촘한 블록을 쓴다
    private static readonly string[] LargePrefixes = { "card_base_", "card_fg_", "card_ov_" };
    private static readonly string[] SmallPrefixes = { "badge_enhance_", "card_star_" };
    private static readonly string[] MobilePlatforms = { "Android", "iPhone" };

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(CardFolder))
            return;

        string fileName = assetPath.Substring(CardFolder.Length);
        bool large = StartsWithAny(fileName, LargePrefixes);

        if (!large && !StartsWithAny(fileName, SmallPrefixes))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = MaxSize;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;

        TextureImporterSettings settings = new TextureImporterSettings();

        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(settings);

        TextureImporterFormat format = large ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4;

        foreach (string platform in MobilePlatforms)
        {
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = platform,
                overridden = true,
                maxTextureSize = MaxSize,
                format = format,
                compressionQuality = (int)TextureCompressionQuality.Best
            });
        }
    }

    private static bool StartsWithAny(string fileName, string[] prefixes)
    {
        foreach (string prefix in prefixes)
        {
            if (fileName.StartsWith(prefix))
                return true;
        }

        return false;
    }
}
