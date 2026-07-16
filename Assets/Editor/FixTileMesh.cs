using UnityEditor;
using UnityEngine;

/// Đổi Sprite Mesh Type sang Full Rect cho các tile dùng SpriteDrawMode.Tiled,
/// tránh lỗi lặp bị vỡ vệt sọc do Tight Mesh cắt sát viền alpha.
public static class FixTileMesh
{
    static readonly string[] Paths =
    {
        "Assets/Art/Environment/DungeonTileset/atlas_floor-16x16.png",
        "Assets/Art/Environment/DungeonTileset/atlas_walls_high-16x32.png",
    };

    [MenuItem("ShatteredGate/Fix Tile Sprite Mesh (Full Rect)")]
    static void Run()
    {
        int ok = 0, fail = 0;
        foreach (var path in Paths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[FixTileMesh] Không tìm thấy file: {path}");
                fail++;
                continue;
            }
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
            ok++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FixTileMesh] Hoàn tất: {ok} thành công, {fail} lỗi.");
    }
}
