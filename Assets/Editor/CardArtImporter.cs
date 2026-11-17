using UnityEditor;
using UnityEngine;

/// <summary>
/// 카드 아트(Resources/UI/Card 의 배경·전경·장식·뒷면·메달·별)의 임포트 설정을 고정한다.
/// 카드가 150~700폭으로 줄어 그려지므로 밉맵을 켠다. 모바일 압축 형식은 MobileTextureFormatImporter 가 정한다
/// </summary>
public class CardArtImporter : AssetPostprocessor
{
    private const string CardFolder = "Assets/Resources/UI/Card/";
    private const int MaxSize = 2048;

    //카드 한 장을 덮는 큰 그림과 작은 메달 · 별. 둘 다 줄여 그린다
    private static readonly string[] ArtPrefixes = { "card_base_", "card_fg_", "card_ov_", "card_back", "badge_enhance_", "card_star_" };

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(CardFolder))
            return;

        if (!StartsWithAny(assetPath.Substring(CardFolder.Length), ArtPrefixes))
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
