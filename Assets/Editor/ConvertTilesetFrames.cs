using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

/// Convert toàn bộ 370 file trong DungeonTileset/frames (tile tường modular,
/// sàn, prop, UI heart...) sang Sprite Single để dùng được trong game.
public static class ConvertTilesetFrames
{
    const string Folder = "Assets/Art/Environment/DungeonTileset/frames";

    [MenuItem("ShatteredGate/Convert DungeonTileset Frames To Sprites")]
    static void Run()
    {
        var files = Directory.GetFiles(Folder, "*.png", SearchOption.TopDirectoryOnly);
        int ok = 0, fail = 0;
        foreach (var path in files)
        {
            string assetPath = path.Replace("\\", "/");
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[ConvertTilesetFrames] Không đọc được importer: {assetPath}");
                fail++;
                continue;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spritePixelsPerUnit = 16;
            importer.SetTextureSettings(settings);

            importer.SaveAndReimport();
            ok++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ConvertTilesetFrames] Hoàn tất: {ok} thành công, {fail} lỗi (tổng {files.Length} file).");
    }
}
