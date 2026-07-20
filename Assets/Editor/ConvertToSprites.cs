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

        "Assets/Pixel-Art-Battlegrounds/PNG/Battleground1/Bright/Battleground1.png",
        "Assets/Pixel-Art-Battlegrounds/PNG/Battleground2/Bright/Battleground2.png",
        "Assets/Pixel-Art-Battlegrounds/PNG/Battleground3/Bright/Battleground3.png",
        "Assets/Pixel-Art-Battlegrounds/PNG/Battleground4/Bright/Battleground4.png",

        "Assets/Art/UI/MainMenu/FantasyBox/boxNormal.png",
        "Assets/Art/UI/MainMenu/FantasyBox/boxHover.png",
        "Assets/Art/UI/MainMenu/FantasyBox/boxSelect.png",
    };

    // stone_gate.png là lưới 4x4 (16 khung hoạt ảnh mở cổng), cắt bằng Sprite Mode Multiple
    // để lấy riêng 1 khung tĩnh làm hình nền Main Menu (xem SceneBuilder.cs).
    const string StoneGatePath = "Assets/Art/UI/MainMenu/StoneGate/stone_gate.png";
    const int StoneGateGridSize = 4;

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

        var gateImporter = AssetImporter.GetAtPath(StoneGatePath) as TextureImporter;
        if (gateImporter == null)
        {
            Debug.LogError($"[ConvertToSprites] Không tìm thấy file: {StoneGatePath}");
            fail++;
        }
        else
        {
            gateImporter.textureType = TextureImporterType.Sprite;
            gateImporter.spriteImportMode = SpriteImportMode.Multiple;
            gateImporter.filterMode = FilterMode.Point;
            gateImporter.textureCompression = TextureImporterCompression.Uncompressed;

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(StoneGatePath);
            int cell = tex.width / StoneGateGridSize;
            var metas = new SpriteMetaData[StoneGateGridSize * StoneGateGridSize];
            for (int row = 0; row < StoneGateGridSize; row++)
            {
                for (int col = 0; col < StoneGateGridSize; col++)
                {
                    int index = row * StoneGateGridSize + col;
                    // Toạ độ Unity tính từ đáy lên — hàng 0 (trên cùng ảnh) ứng với y cao nhất.
                    int flippedRow = StoneGateGridSize - 1 - row;
                    metas[index] = new SpriteMetaData
                    {
                        name = $"stone_gate_{index}",
                        rect = new Rect(col * cell, flippedRow * cell, cell, cell),
                    };
                }
            }
            gateImporter.spritesheet = metas;
            gateImporter.SaveAndReimport();
            ok++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ConvertToSprites] Hoàn tất: {ok} thành công, {fail} lỗi.");
    }
}
