using UnityEditor;

namespace SimpleCore.Tools.EditorTools
{
    /// <summary>
    /// The art of the game (Management/tools/art.py): a picture put under <c>…/FeatureAssets/&lt;Private|Public&gt;/Art/&lt;F&gt;/&lt;id&gt;/</c> or <c>Core/Art/</c>
    /// (Simple.png by a worker, Detail.png by the human) is imported as a Sprite the first time Unity sees it. The project
    /// is in 3D mode, so a new PNG would otherwise be a plain texture. A picture already imported keeps the settings it
    /// has, so a setting changed by hand is never undone.
    /// </summary>
    internal sealed class ArtImport : AssetPostprocessor
    {
        private static bool InArt(string path) =>
            path.Contains("/FeatureAssets/Private/Art/") || path.Contains("/FeatureAssets/Public/Art/") ||
            path.StartsWith("Assets/Core/Art/");

        private void OnPreprocessTexture()
        {
            if (!InArt(assetPath) || !(assetImporter is TextureImporter importer) || !importer.importSettingsMissing)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
        }
    }
}
