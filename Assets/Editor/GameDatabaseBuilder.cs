using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using static UIBuilderHelpers;

/// Tạo/điền dữ liệu vào Assets/Data/GameDatabase.asset — nguồn dữ liệu DUY NHẤT cho
/// Hero/Enemy/Chặng/Lore/Background, mọi Scene đọc chung từ đây thay vì khai báo lặp lại.
public static class GameDatabaseBuilder
{
    const string AssetPath = "Assets/Data/GameDatabase.asset";

    [MenuItem("ShatteredGate/3 - Build Game Database")]
    public static void Build()
    {
        GameObject Load(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) Debug.LogError($"[GameDatabaseBuilder] Thiếu prefab: {path}");
            return go;
        }

        Sprite LoadSprite(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogWarning($"[GameDatabaseBuilder] Không tìm thấy sprite (chưa convert?): {path}");
            return s;
        }

        // Đọc N file ảnh đánh số liên tiếp (VD Explosion1.png..Explosion10.png) thành 1 mảng khung hình,
        // dùng cho các hiệu ứng nổ tách file riêng lẻ (không phải sprite-sheet 1 texture).
        Sprite[] LoadNumberedSprites(string folder, string prefix, int count)
        {
            var list = new System.Collections.Generic.List<Sprite>();
            for (int i = 1; i <= count; i++)
            {
                var s = LoadSprite($"{folder}/{prefix}{i}.png");
                if (s != null) list.Add(s);
            }
            return list.ToArray();
        }

        var graham = Load("Assets/Prefabs/Heroes/Graham.prefab");
        var sally = Load("Assets/Prefabs/Heroes/Sally.prefab");
        var violet = Load("Assets/Prefabs/Heroes/Violet.prefab");
        var james = Load("Assets/Prefabs/Heroes/James.prefab");

        var slime = Load("Assets/Prefabs/Enemies/TierA/Slime.prefab");
        var bat = Load("Assets/Prefabs/Enemies/TierA/Bat.prefab");
        var frog = Load("Assets/Prefabs/Enemies/TierA/Frog.prefab");
        var barrel = Load("Assets/Prefabs/Enemies/TierA/Barrel.prefab");
        var trainingDummy = Load("Assets/Prefabs/Enemies/TierA/Training Dummy.prefab");
        var defencer = Load("Assets/Prefabs/Enemies/TierB/Defencer.prefab");
        var zombie = Load("Assets/Prefabs/Enemies/TierB/Zombie.prefab");
        var elementCrystal = Load("Assets/Prefabs/Enemies/TierB/Element Crystal.prefab");
        var genie = Load("Assets/Prefabs/Enemies/TierC/Genie.prefab");
        var cactuar = Load("Assets/Prefabs/Enemies/TierC/Cactuar.prefab");
        var fairy = Load("Assets/Prefabs/Enemies/TierC/Fairy.prefab");
        var vampireTulip = Load("Assets/Prefabs/Enemies/TierC/Vampire Tulip.prefab");
        var twoFaced = Load("Assets/Prefabs/Enemies/TierC/Two Faced.prefab");
        var treant = Load("Assets/Prefabs/Enemies/TierD/Treant.prefab");
        var asimole = Load("Assets/Prefabs/Enemies/TierD/Asimole.prefab");
        var creeps = Load("Assets/Prefabs/Enemies/TierD/Creeps.prefab");
        var demonpot = Load("Assets/Prefabs/Enemies/TierD/Demonpot.prefab");
        var mechasphere = Load("Assets/Prefabs/Enemies/TierD/Mechasphere.prefab");
        var zero = Load("Assets/Prefabs/Enemies/TierD/Zero.prefab");

        var allPrefabs = new[] { graham, sally, violet, james, slime, bat, frog, barrel, trainingDummy,
            defencer, zombie, elementCrystal, genie, cactuar, fairy, vampireTulip, twoFaced,
            treant, asimole, creeps, demonpot, mechasphere, zero };
        if (allPrefabs.Any(p => p == null))
        {
            Debug.LogError("[GameDatabaseBuilder] Thiếu prefab ở trên — chạy đủ Setup Hero Prefabs + Tier A/B/C/D trước rồi thử lại.");
            return;
        }

        var grahamPortrait = LoadSprite("Assets/Art/Characters/Heroes/Actor Portrait/Graham 1A[portrait].png");
        var sallyPortrait = LoadSprite("Assets/Art/Characters/Heroes/Actor Portrait/Sally 1A[portrait].png");
        var violetPortrait = LoadSprite("Assets/Art/Characters/Heroes/Actor Portrait/Violet 1A[portrait].png");
        var jamesPortrait = LoadSprite("Assets/Art/Characters/Heroes/Actor Portrait/James 1A[portrait].png");

        var slimeIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Slime 1A[icon].png");
        var batIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Bat 1A[icon].png");
        var frogIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Frog 1A[icon].png");
        var barrelIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Barrel 1A[icon].png");
        var trainingDummyIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Training Dummy 1A[icon].png");
        var defencerIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Defencer 1A[icon].png");
        var zombieIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Zombie 1A[icon].png");
        var elementCrystalIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Element Crystal 1A[icon].png");
        var genieIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Genie 1A[icon].png");
        var cactuarIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Cactuar 1A[icon].png");
        var fairyIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Fairy 1A[icon].png");
        var vampireTulipIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Vampire Tulip 1A[icon].png");
        var twoFacedIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Two Faced 1A[icon].png");
        var treantIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Treant 1A[icon].png");
        var asimoleIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Asimole 1A[icon].png");
        var creepsIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Creeps 1A[icon].png");
        var demonpotIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Demonpot A[icon].png");
        var mechasphereIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Mechasphere A[icon].png");
        var zeroIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Zero 1A[icon].png");

        // 7 nền — gán riêng từng Enemy trong UnitStatsSetup.cs (không theo Tier) để tránh lặp nền
        // giữa các trận liền kề + tránh nền cùng tông màu với quái (VD quái xanh lá không đứng nền
        // rừng xanh). Index 0-3 = 4 nền Battleground gốc; 4-6 = 3 nền Postapocalypse mới tải.
        var battlegroundSprites = new[]
        {
            LoadSprite("Assets/Pixel-Art-Battlegrounds/PNG/Battleground1/Bright/Battleground1.png"),      // 0: nghĩa địa đỏ tối
            LoadSprite("Assets/Pixel-Art-Battlegrounds/PNG/Battleground2/Bright/Battleground2.png"),      // 1: giáo đường vàng ấm
            LoadSprite("Assets/Pixel-Art-Battlegrounds/PNG/Battleground3/Bright/Battleground3.png"),      // 2: rừng huyền bí tối
            LoadSprite("Assets/Pixel-Art-Battlegrounds/PNG/Battleground4/Bright/Battleground4.png"),      // 3: hầm mộ xám
            LoadSprite("Assets/postapocalypse_bg_pixel_png/PNG/Postapocalypce3/Bright/postapocalypse3.png"), // 4: sa mạc đêm tím
            LoadSprite("Assets/postapocalypse_bg_pixel_png/PNG/Postapocalypce2/Bright/postapocalypse2.png"), // 5: hoang tàn phủ xanh
            LoadSprite("Assets/postapocalypse_bg_pixel_png/PNG/Postapocalypce4/Bright/postapocalypse4.png"), // 6: nội thất xanh-lục đậm
        };

        var bossStoryBackground = LoadSprite("Assets/postapocalypse_bg_pixel_png/PNG/Postapocalypce1/Bright/postapocalypse1.png");

        var portalFrames = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/VFX/Portal/sprite-sheet.png")
            .OfType<Sprite>()
            .Select(s => new { sprite = s, index = ParseTrailingIndex(s.name) })
            .Where(x => x.index >= 0)
            .OrderBy(x => x.index)
            .Select(x => x.sprite)
            .ToArray();
        if (portalFrames.Length == 0)
            Debug.LogWarning("[GameDatabaseBuilder] Không tìm thấy frame Portal — hiệu ứng chuyển cảnh sẽ chỉ có fade đen.");

        // Hiệu ứng nổ: Hero tông ấm (vàng/cam), Enemy tông lạnh (xanh/lục) — xem CombatManager.ChooseFxFrames.
        const string ExplosionPack = "Assets/craftpix-net-270676-11-free-pixel-art-explosion-sprites/PNG";
        var hitFxFrames = LoadNumberedSprites($"{ExplosionPack}/Explosion", "Explosion", 10);
        var bigHitFxFrames = LoadNumberedSprites($"{ExplosionPack}/Nuclear_explosion", "Nuclear_explosion", 10);
        var vulnerableApplyFx = LoadNumberedSprites($"{ExplosionPack}/Circle_explosion", "Circle_explosion", 10);
        var stunApplyFx = LoadNumberedSprites($"{ExplosionPack}/Lightning", "Lightning_cycle", 6);
        var enemyHitFxFrames = LoadNumberedSprites($"{ExplosionPack}/Explosion_gas", "Explosion_gas", 10);
        var enemyBigHitFxFrames = LoadNumberedSprites($"{ExplosionPack}/Explosion_two_colors", "Explosion_two_colors", 10);
        var enemyVulnerableApplyFx = LoadNumberedSprites($"{ExplosionPack}/Explosion_blue_circle", "Explosion_blue_circle", 10);
        var ultimateFxFrames = LoadNumberedSprites(
            "Assets/craftpix-net-840730-free-animated-explosion-sprite-pack/PNG/Explosion_5", "Explosion_", 10);

        AudioClip LoadClip(string path)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (c == null) Debug.LogWarning($"[GameDatabaseBuilder] Không tìm thấy audio: {path}");
            return c;
        }
        var hitSfx = LoadClip("Assets/Audio/RPGSoundPack/RPG Sound Pack/battle/swing.wav");
        var victorySfx = LoadClip("Assets/Audio/Jingles/OGG/jingles_NES/jingles_NES00.ogg");
        var defeatSfx = LoadClip("Assets/Audio/Jingles/OGG/jingles_NES/jingles_NES16.ogg");

        Directory.CreateDirectory("Assets/Data");
        var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(AssetPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<GameDatabase>();
            AssetDatabase.CreateAsset(db, AssetPath);
        }

        db.mainHeroPrefab = graham;
        db.recruitOrder = new[] { sally, violet, james };
        db.recruitLoreLines = new[]
        {
            "Beyond a cracked archway, Graham finds Sally, a mage flung between fragments by the same Sundering that scattered his world. She recognizes the shard in his hand — and the hope it represents. She joins him.",
            "In the ruins of a fallen watchtower, Violet the Hunter has been stalking the same Portals as Graham, hunting the truth of what tore the sky apart. She sees an ally, not a stranger, and falls into step beside him.",
            "James, a mercenary stranded when his last contract's world came apart, has given up finding his own way home. He offers his blade for passage through Graham's Gates instead.",
        };

        db.finalBossPrefab = zero;
        db.finalBossName = "Zero";
        db.finalBossIcon = zeroIcon;

        db.openingStoryText =
            "The Sundering tore the world into a thousand drifting fragments. Graham woke alone in the ruins " +
            "of what was once his home, with no memory of how the sky came to break. All that remains is a shard " +
            "of the Gate itself — warm in his palm, pulling him toward every portal he finds. Somewhere beyond " +
            "them lies the truth of what happened here... and perhaps, a way back. He steps through the first Gate alone.";
        db.finalBossIntroText =
            "At the heart of the deepest fragment stands Zero — not a monster, but a wound given form: the last " +
            "splinter of whatever broke the world apart. To close the Gates, Graham must face it.";
        db.victoryEpilogueText =
            "The last Gate closes behind you. The fragments do not heal — but they stop falling. For now, that is enough.";

        // Elite dùng lại đúng prefab Monster của chặng đó — CombatManager tự bơm máu/damage cao hơn
        // lúc StartCombat(isElite: true), không cần prefab/asset riêng (xem CombatManager.EliteHpMultiplier).
        // Treasure không có enemyPrefabs — GameSceneController xử lý riêng (không combat).
        // Nhiều Monster gate là TRẬN BẦY (2 enemyPrefabs) — tăng độ khó bằng số lượng thay vì máu/damage,
        // đồng thời dùng hết roster (Frog/Barrel/Training Dummy/Element Crystal/Cactuar/Fairy/Two Faced/
        // Vampire Tulip trước đây chưa xuất hiện ở chặng nào). battlegroundIndexOverride chỉ cần khi 1
        // Gate dùng lại đúng Prefab của Gate khác trong CÙNG chặng (Elite lặp Monster) để không hiện
        // trùng y hệt 1 nền — xem MapManager.Gate.
        db.stages = new[]
        {
            new MapManager.StageDef{ stageName = "Stage 1", keyUnlockAfterClears = 0, gates = new[]{
                new MapManager.Gate{ type = MapManager.GateType.Monster, enemyPrefabs = new[]{ slime, frog }, label = "Slime & Frog", icon = slimeIcon },
                new MapManager.Gate{ type = MapManager.GateType.Key, label = "Sally" },
                new MapManager.Gate{ type = MapManager.GateType.Boss, enemyPrefabs = new[]{ treant }, label = "Treant", icon = treantIcon },
            }},
            new MapManager.StageDef{ stageName = "Stage 2", gates = new[]{
                new MapManager.Gate{ type = MapManager.GateType.Monster, enemyPrefabs = new[]{ bat, barrel }, label = "Bat & Barrel", icon = batIcon },
                new MapManager.Gate{ type = MapManager.GateType.Elite, enemyPrefabs = new[]{ bat }, label = "Elite Bat", icon = batIcon, battlegroundIndexOverride = 2 },
                new MapManager.Gate{ type = MapManager.GateType.Treasure, label = "Treasure" },
                new MapManager.Gate{ type = MapManager.GateType.Boss, enemyPrefabs = new[]{ asimole }, label = "Asimole", icon = asimoleIcon },
            }},
            new MapManager.StageDef{ stageName = "Stage 3", keyUnlockAfterClears = 2, gates = new[]{
                new MapManager.Gate{ type = MapManager.GateType.Monster, enemyPrefabs = new[]{ defencer, elementCrystal }, label = "Defencer & Element Crystal", icon = defencerIcon },
                new MapManager.Gate{ type = MapManager.GateType.Elite, enemyPrefabs = new[]{ defencer }, label = "Elite Defencer", icon = defencerIcon, battlegroundIndexOverride = 3 },
                new MapManager.Gate{ type = MapManager.GateType.Treasure, label = "Treasure" },
                new MapManager.Gate{ type = MapManager.GateType.Key, label = "Violet" },
                new MapManager.Gate{ type = MapManager.GateType.Boss, enemyPrefabs = new[]{ creeps }, label = "Creeps", icon = creepsIcon },
            }},
            new MapManager.StageDef{ stageName = "Stage 4", gates = new[]{
                new MapManager.Gate{ type = MapManager.GateType.Monster, enemyPrefabs = new[]{ zombie }, label = "Zombie", icon = zombieIcon },
                new MapManager.Gate{ type = MapManager.GateType.Monster, enemyPrefabs = new[]{ trainingDummy, twoFaced }, label = "Training Dummy & Two Faced", icon = trainingDummyIcon },
                new MapManager.Gate{ type = MapManager.GateType.Elite, enemyPrefabs = new[]{ fairy, vampireTulip }, label = "Elite Fairy & Vampire Tulip", icon = fairyIcon },
                new MapManager.Gate{ type = MapManager.GateType.Treasure, label = "Treasure" },
                new MapManager.Gate{ type = MapManager.GateType.Boss, enemyPrefabs = new[]{ demonpot }, label = "Demonpot", icon = demonpotIcon },
            }},
            new MapManager.StageDef{ stageName = "Stage 5", keyUnlockAfterClears = 2, gates = new[]{
                new MapManager.Gate{ type = MapManager.GateType.Monster, enemyPrefabs = new[]{ genie, cactuar }, label = "Genie & Cactuar", icon = genieIcon },
                new MapManager.Gate{ type = MapManager.GateType.Elite, enemyPrefabs = new[]{ genie }, label = "Elite Genie", icon = genieIcon, battlegroundIndexOverride = 0 },
                new MapManager.Gate{ type = MapManager.GateType.Treasure, label = "Treasure" },
                new MapManager.Gate{ type = MapManager.GateType.Key, label = "James" },
                new MapManager.Gate{ type = MapManager.GateType.Boss, enemyPrefabs = new[]{ mechasphere }, label = "Mechasphere", icon = mechasphereIcon },
            }},
        };

        db.portraitLookup = new[]
        {
            new CombatManager.NamedSprite{ unitName = "Graham", sprite = grahamPortrait },
            new CombatManager.NamedSprite{ unitName = "Sally", sprite = sallyPortrait },
            new CombatManager.NamedSprite{ unitName = "Violet", sprite = violetPortrait },
            new CombatManager.NamedSprite{ unitName = "James", sprite = jamesPortrait },
            new CombatManager.NamedSprite{ unitName = "Slime", sprite = slimeIcon },
            new CombatManager.NamedSprite{ unitName = "Bat", sprite = batIcon },
            new CombatManager.NamedSprite{ unitName = "Frog", sprite = frogIcon },
            new CombatManager.NamedSprite{ unitName = "Barrel", sprite = barrelIcon },
            new CombatManager.NamedSprite{ unitName = "Training Dummy", sprite = trainingDummyIcon },
            new CombatManager.NamedSprite{ unitName = "Defencer", sprite = defencerIcon },
            new CombatManager.NamedSprite{ unitName = "Zombie", sprite = zombieIcon },
            new CombatManager.NamedSprite{ unitName = "Element Crystal", sprite = elementCrystalIcon },
            new CombatManager.NamedSprite{ unitName = "Genie", sprite = genieIcon },
            new CombatManager.NamedSprite{ unitName = "Cactuar", sprite = cactuarIcon },
            new CombatManager.NamedSprite{ unitName = "Fairy", sprite = fairyIcon },
            new CombatManager.NamedSprite{ unitName = "Vampire Tulip", sprite = vampireTulipIcon },
            new CombatManager.NamedSprite{ unitName = "Two Faced", sprite = twoFacedIcon },
            new CombatManager.NamedSprite{ unitName = "Treant", sprite = treantIcon },
            new CombatManager.NamedSprite{ unitName = "Asimole", sprite = asimoleIcon },
            new CombatManager.NamedSprite{ unitName = "Creeps", sprite = creepsIcon },
            new CombatManager.NamedSprite{ unitName = "Demonpot", sprite = demonpotIcon },
            new CombatManager.NamedSprite{ unitName = "Mechasphere", sprite = mechasphereIcon },
            new CombatManager.NamedSprite{ unitName = "Zero", sprite = zeroIcon },
        };

        db.battlegroundSprites = battlegroundSprites;
        db.bossStoryBackground = bossStoryBackground;
        db.portalFrames = portalFrames;
        db.hitFxFrames = hitFxFrames;
        db.bigHitFxFrames = bigHitFxFrames;
        db.vulnerableApplyFx = vulnerableApplyFx;
        db.stunApplyFx = stunApplyFx;
        db.ultimateFxFrames = ultimateFxFrames;
        db.enemyHitFxFrames = enemyHitFxFrames;
        db.enemyBigHitFxFrames = enemyBigHitFxFrames;
        db.enemyVulnerableApplyFx = enemyVulnerableApplyFx;
        db.hitSfx = hitSfx;
        db.victorySfx = victorySfx;
        db.defeatSfx = defeatSfx;

        var loadingFadePrefabGo = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/LoadingFadePrefab.prefab");
        db.loadingFadePrefab = loadingFadePrefabGo != null ? loadingFadePrefabGo.GetComponent<LoadingFade>() : null;
        if (db.loadingFadePrefab == null)
        {
            Debug.LogWarning("[GameDatabaseBuilder] Không tìm thấy LoadingFadePrefab — chạy 'Build All UI Panel Prefabs' trước.");
        }
        else
        {
            // PanelPrefabBuilder chỉ nối overlayRoot/fadeImage/portalImage, chưa nối portalFrames
            // (mảng này chỉ có sẵn ở đây) — nối trực tiếp vào Prefab asset rồi lưu lại.
            db.loadingFadePrefab.portalFrames = portalFrames;
            EditorUtility.SetDirty(loadingFadePrefabGo);
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GameDatabaseBuilder] Xong! GameDatabase đã điền dữ liệu tại " + AssetPath);
    }
}
