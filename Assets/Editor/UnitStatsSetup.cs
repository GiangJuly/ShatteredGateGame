using UnityEngine;
using UnityEditor;
using System.IO;

/// Gán chỉ số chiến đấu mặc định (HP/ATK/SPD) lên các Prefab Hero/Enemy đã có sẵn,
/// để CombatManager có số liệu chạy thử ngay hôm nay. Chỉnh lại số trong Inspector
/// của từng prefab sau này khi cần cân bằng.
public static class UnitStatsSetup
{
    class Def
    {
        public string name;
        public string prefabPath;
        public int hp, atk, spd;
        public bool isPlayer;
    }

    static readonly Def[] Defs =
    {
        new Def{ name="Graham", prefabPath="Assets/Prefabs/Heroes/Graham.prefab", hp=40, atk=8,  spd=6,  isPlayer=true },
        new Def{ name="Sally",  prefabPath="Assets/Prefabs/Heroes/Sally.prefab",  hp=22, atk=12, spd=9,  isPlayer=true },
        new Def{ name="Violet", prefabPath="Assets/Prefabs/Heroes/Violet.prefab", hp=26, atk=10, spd=10, isPlayer=true },
        new Def{ name="James",  prefabPath="Assets/Prefabs/Heroes/James.prefab",  hp=32, atk=11, spd=7,  isPlayer=true },

        new Def{ name="Slime",          prefabPath="Assets/Prefabs/Enemies/TierA/Slime.prefab",          hp=15, atk=4, spd=5 },
        new Def{ name="Training Dummy", prefabPath="Assets/Prefabs/Enemies/TierA/Training Dummy.prefab", hp=15, atk=4, spd=5 },
        new Def{ name="Bat",            prefabPath="Assets/Prefabs/Enemies/TierA/Bat.prefab",            hp=15, atk=4, spd=5 },
        new Def{ name="Frog",           prefabPath="Assets/Prefabs/Enemies/TierA/Frog.prefab",           hp=15, atk=4, spd=5 },
        new Def{ name="Barrel",         prefabPath="Assets/Prefabs/Enemies/TierA/Barrel.prefab",         hp=15, atk=4, spd=5 },

        new Def{ name="Defencer",        prefabPath="Assets/Prefabs/Enemies/TierB/Defencer.prefab",        hp=25, atk=7, spd=6 },
        new Def{ name="Zombie",          prefabPath="Assets/Prefabs/Enemies/TierB/Zombie.prefab",          hp=25, atk=7, spd=6 },
        new Def{ name="Element Crystal", prefabPath="Assets/Prefabs/Enemies/TierB/Element Crystal.prefab", hp=25, atk=7, spd=6 },

        new Def{ name="Genie",         prefabPath="Assets/Prefabs/Enemies/TierC/Genie.prefab",         hp=30, atk=9, spd=8 },
        new Def{ name="Fairy",         prefabPath="Assets/Prefabs/Enemies/TierC/Fairy.prefab",         hp=30, atk=9, spd=8 },
        new Def{ name="Vampire Tulip", prefabPath="Assets/Prefabs/Enemies/TierC/Vampire Tulip.prefab", hp=30, atk=9, spd=8 },
        new Def{ name="Two Faced",     prefabPath="Assets/Prefabs/Enemies/TierC/Two Faced.prefab",     hp=30, atk=9, spd=8 },
        new Def{ name="Cactuar",       prefabPath="Assets/Prefabs/Enemies/TierC/Cactuar.prefab",       hp=30, atk=9, spd=8 },

        new Def{ name="Treant",      prefabPath="Assets/Prefabs/Enemies/TierD/Treant.prefab",      hp=80,  atk=14, spd=7 },
        new Def{ name="Asimole",     prefabPath="Assets/Prefabs/Enemies/TierD/Asimole.prefab",     hp=80,  atk=14, spd=7 },
        new Def{ name="Creeps",      prefabPath="Assets/Prefabs/Enemies/TierD/Creeps.prefab",      hp=80,  atk=14, spd=7 },
        new Def{ name="Demonpot",    prefabPath="Assets/Prefabs/Enemies/TierD/Demonpot.prefab",    hp=80,  atk=14, spd=7 },
        new Def{ name="Mechasphere", prefabPath="Assets/Prefabs/Enemies/TierD/Mechasphere.prefab", hp=80,  atk=14, spd=7 },
        new Def{ name="Zero",        prefabPath="Assets/Prefabs/Enemies/TierD/Zero.prefab",        hp=100, atk=16, spd=9 },
    };

    [MenuItem("ShatteredGate/Setup Unit Stats On All Prefabs")]
    static void Run()
    {
        int ok = 0, fail = 0;
        foreach (var d in Defs)
        {
            if (!File.Exists(d.prefabPath))
            {
                Debug.LogError($"[UnitStatsSetup] Không tìm thấy prefab: {d.prefabPath}");
                fail++;
                continue;
            }

            var root = PrefabUtility.LoadPrefabContents(d.prefabPath);
            var holder = root.GetComponent<UnitStatsHolder>();
            if (holder == null) holder = root.AddComponent<UnitStatsHolder>();
            holder.unitName = d.name;
            holder.maxHP = d.hp;
            holder.attackPower = d.atk;
            holder.speed = d.spd;
            holder.isPlayerUnit = d.isPlayer;

            PrefabUtility.SaveAsPrefabAsset(root, d.prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            ok++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[UnitStatsSetup] Hoàn tất: {ok} thành công, {fail} lỗi.");
    }
}
