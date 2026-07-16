using UnityEngine;
using UnityEditor;

/// Chuyển các file portrait/icon (đang là Texture thường) sang Sprite (Single)
/// để dùng làm UI Image — các file này chưa từng qua bước cắt Sprite Editor.
public static class ConvertToSprites
{
    static readonly string[] Paths =
    {
        "Assets/Art/Characters/Heroes/Actor Portrait/Graham 1A[portrait].png",
        "Assets/Art/Characters/Heroes/Actor Portrait/Sally 1A[portrait].png",
        "Assets/Art/Characters/Heroes/Actor Portrait/Violet 1A[portrait].png",
        // James không có file portrait riêng trong bộ asset gốc (dùng chung class với Ulric).

        "Assets/Art/Characters/Enemies/Icon 32x32/Slime 1A[icon].png",
        "Assets/Art/Characters/Enemies/Icon 32x32/Bat 1A[icon].png",
        "Assets/Art/Characters/Enemies/Icon 32x32/Defencer 1A[icon].png",
        "Assets/Art/Characters/Enemies/Icon 32x32/Zombie 1A[icon].png",
        "Assets/Art/Characters/Enemies/Icon 32x32/Genie 1A[icon].png",
        "Assets/Art/Characters/Enemies/Icon 32x32/Treant 1A[icon].png",
        "Assets/Art/Characters/Enemies/Icon 32x32/Asimole 1A[icon].png",
        "Assets/Art/Characters/Enemies/Icon 32x32/Creeps 1A[icon].png",
        "Assets/Art/Characters/Enemies/Icon 32x32/Demonpot A[icon].png",
        "Assets/Art/Characters/Enemies/Icon 32x32/Mechasphere A[icon].png",
        "Assets/Art/Characters/Enemies/Icon 32x32/Zero 1A[icon].png",
    };

    [MenuItem("ShatteredGate/Convert Portraits & Icons To Sprites")]
    static void Run()
    {
        int ok = 0, fail = 0;
        foreach (var path in Paths)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[ConvertToSprites] Không tìm thấy file: {path}");
                fail++;
                continue;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            ok++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ConvertToSprites] Hoàn tất: {ok} thành công, {fail} lỗi.");
    }
}
