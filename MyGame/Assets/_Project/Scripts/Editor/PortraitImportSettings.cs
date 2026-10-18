using UnityEditor;
using UnityEngine;

namespace Hackathon.EditorTools
{
    /// <summary>
    /// 立绘导入设置：像素风素材要关闭压缩、用 Point 过滤，否则边缘会糊。
    /// 只作用于 Assets/_Project/Resources/Portraits/ 下的图片，不影响其他资源。
    /// </summary>
    public class PortraitImportSettings : AssetPostprocessor
    {
        const string PortraitFolder = "/Resources/Portraits/";

        void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (path.IndexOf(PortraitFolder, System.StringComparison.OrdinalIgnoreCase) < 0) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.spritePixelsPerUnit = 100f;
        }
    }
}
