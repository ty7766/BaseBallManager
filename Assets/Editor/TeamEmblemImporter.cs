using UnityEditor;
using UnityEngine;

/// <summary>
/// 구단 엠블럼(Resources/UI/Emblems)의 임포트 설정을 고정한다.
/// 자동 슬라이스를 막아 떨어진 글자 · 테가 조각 스프라이트로 나뉘지 않게 한다
/// </summary>
public class TeamEmblemImporter : AssetPostprocessor
{
    private const string EmblemFolder = "Assets/Resources/UI/Emblems/";
    private const int EmblemMaxSize = 512;

    //엠블럼 폴더의 텍스처만 손댄다. Multiple 로 들어가면 첫 조각(LG 는 TWINS 글자)만 버튼에 깔린다
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(EmblemFolder))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;

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
}
