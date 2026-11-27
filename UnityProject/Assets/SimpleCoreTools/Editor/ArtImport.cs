using UnityEditor;

namespace SimpleCore.Tools.EditorTools
{
    /// <summary>
    /// The art of the game (Management/tools/art.py): a picture put under <c>…/Resources/Art/&lt;F&gt;/&lt;id&gt;/</c>
    /// (Simple.png by a worker, Detail.png by the human) is imported as a Sprite the first time Unity sees it. The project
    /// is in 3D mode, so a new PNG would otherwise be a plain texture. A picture already imported keeps the settings it
    /// has, so a setting changed by hand is never undone.
    /// </summary>
    internal sealed class ArtImport : AssetPostprocessor
    {
        private const string ArtFolder = "/Resources/Art/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.Contains(ArtFolder) || !(assetImporter is TextureImporter importer) || !importer.importSettingsMissing)
                return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
        }
    }
}
