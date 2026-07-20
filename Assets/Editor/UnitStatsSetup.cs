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
        public int maxAP = 3;
        public Skill[] skills;
        public int battlegroundIndex;
        public float displayScale = 1f;
        public bool hasComboHealPassive;
        public bool hasArcaneChargePassive;
        public bool hasMarkedPreyPassive;
        public bool hasContractorDiscountPassive;
    }

    // Kỹ năng cơ bản dùng chung — luôn 0 AP, không tag.
    static Skill Strike() => new Skill { skillName = "Strike", apCost = 0, powerMultiplier = 1f };

    // 2 cặp combo xuyên hero: Graham setup Vulnerable -> Sally ăn combo;
    // Violet setup Stunned -> James ăn combo (execute).
    // Bộ chiêu Graham (nhân vật chính, cầm mảnh Gate): đánh thường có combo 3 đòn tự cộng dmg,
    // Shadow Slash gắn Vulnerable, Dark Nova/Shattered Gate ăn theo Vulnerable đó — Graham có thể
    // tự tạo combo 1 mình (Q -> E/R) chứ không bắt buộc phải chờ đồng đội khác.
    static readonly Skill[] GrahamKit =
    {
        new Skill{ skillName="Strike", apCost=0, powerMultiplier=1f, isComboStrike=true },
        new Skill{ skillName="Shadow Slash", apCost=2, powerMultiplier=0.7f, appliesTag=TagType.Vulnerable, tagDuration=2 },
        new Skill{ skillName="Dark Nova", apCost=3, powerMultiplier=1.3f, consumesTag=TagType.Vulnerable, comboMultiplier=1.6f },
        new Skill{ skillName="Shattered Gate", apCost=5, powerMultiplier=1.8f, consumesTag=TagType.Vulnerable, comboMultiplier=2.2f },
    };
    static readonly Skill[] SallyKit =
    {
        Strike(),
        new Skill{ skillName="Arcane Burst", apCost=2, powerMultiplier=1f, consumesTag=TagType.Vulnerable, comboMultiplier=1.75f },
    };
    static readonly Skill[] VioletKit =
    {
        Strike(),
        new Skill{ skillName="Pin Down", apCost=2, powerMultiplier=0.6f, appliesTag=TagType.Stunned, tagDuration=1 },
    };
    static readonly Skill[] JamesKit =
    {
        Strike(),
        new Skill{ skillName="Execute", apCost=2, powerMultiplier=1f, consumesTag=TagType.Stunned, comboMultiplier=2f },
    };

    // Boss Tier D thường: thêm 1 đòn đặc biệt gắn Vulnerable lên hero — buộc người chơi
    // phải phản ứng (đổi mục tiêu/ưu tiên combo) thay vì chỉ bấm 1 skill lặp lại.
    static readonly Skill[] BossKit =
    {
        Strike(),
        new Skill{ skillName="Crushing Blow", apCost=2, powerMultiplier=0.8f, appliesTag=TagType.Vulnerable, tagDuration=2 },
    };

    // Final Boss Zero — 4 skill, đổi hành vi theo % HP còn lại (xem CombatManager.ChooseZeroSkill):
    // Phase 1 (>66%) chỉ Strike; Phase 2 (33-66%) luân phiên Fracture/Paralyze gắn cả 2 loại Tag lên
    // party; Phase 3 (<33%) tung Unravel ăn theo chính Tag nó vừa gắn — Zero chơi đúng luật combo-Tag
    // mà người chơi đang dùng, quay ngược lại chính hệ thống đó.
    static readonly Skill[] ZeroKit =
    {
        new Skill{ skillName="Strike", apCost=0, powerMultiplier=1f },
        new Skill{ skillName="Fracture", apCost=2, powerMultiplier=0.7f, appliesTag=TagType.Vulnerable, tagDuration=2 },
        new Skill{ skillName="Paralyze", apCost=2, powerMultiplier=0.6f, appliesTag=TagType.Stunned, tagDuration=1 },
        new Skill{ skillName="Unravel", apCost=3, powerMultiplier=1.6f, consumesTag=TagType.Vulnerable, comboMultiplier=1.8f },
    };

    static readonly Def[] Defs =
    {
        new Def{ name="Graham", prefabPath="Assets/Prefabs/Heroes/Graham.prefab", hp=40, atk=8,  spd=6,  isPlayer=true, maxAP=5, skills=GrahamKit, hasComboHealPassive=true },
        new Def{ name="Sally",  prefabPath="Assets/Prefabs/Heroes/Sally.prefab",  hp=22, atk=12, spd=9,  isPlayer=true, skills=SallyKit, hasArcaneChargePassive=true },
        new Def{ name="Violet", prefabPath="Assets/Prefabs/Heroes/Violet.prefab", hp=26, atk=10, spd=10, isPlayer=true, skills=VioletKit, hasMarkedPreyPassive=true },
        new Def{ name="James",  prefabPath="Assets/Prefabs/Heroes/James.prefab",  hp=32, atk=11, spd=7,  isPlayer=true, skills=JamesKit, hasContractorDiscountPassive=true },

        // battlegroundIndex: 0=Battleground1(nghĩa địa đỏ tối) 1=Battleground2(giáo đường vàng) 2=Battleground3(rừng
        // tối) 3=Battleground4(hầm mộ xám) 4=Postapocalypse3(sa mạc đêm tím) 5=Postapocalypse2(hoang tàn xanh)
        // 6=Postapocalypse4(nội thất xanh-lục đậm). Gán riêng từng con (không theo Tier như trước) để tránh lặp
        // nền + tránh cùng tông màu với nền (VD quái xanh lá không đứng nền rừng xanh) — xem CombatManager
        // để biết displayScale bù riêng cho sprite bị "đệm" trống nhiều (nhỏ/mờ hơn hẳn dù cùng khung hình).
        new Def{ name="Slime",          prefabPath="Assets/Prefabs/Enemies/TierA/Slime.prefab",          hp=22, atk=5, spd=5, skills=new[]{ Strike() }, battlegroundIndex=0, displayScale=1.6f },
        new Def{ name="Training Dummy", prefabPath="Assets/Prefabs/Enemies/TierA/Training Dummy.prefab", hp=22, atk=5, spd=5, skills=new[]{ Strike() }, battlegroundIndex=2, displayScale=1.3f },
        new Def{ name="Bat",            prefabPath="Assets/Prefabs/Enemies/TierA/Bat.prefab",            hp=22, atk=5, spd=5, skills=new[]{ Strike() }, battlegroundIndex=1, displayScale=1.7f },
        new Def{ name="Frog",           prefabPath="Assets/Prefabs/Enemies/TierA/Frog.prefab",           hp=22, atk=5, spd=5, skills=new[]{ Strike() }, battlegroundIndex=3, displayScale=1.6f },
        new Def{ name="Barrel",         prefabPath="Assets/Prefabs/Enemies/TierA/Barrel.prefab",         hp=22, atk=5, spd=5, skills=new[]{ Strike() }, battlegroundIndex=5, displayScale=1.3f },

        new Def{ name="Defencer",        prefabPath="Assets/Prefabs/Enemies/TierB/Defencer.prefab",        hp=34, atk=8, spd=6, skills=new[]{ Strike() }, battlegroundIndex=4, displayScale=1.3f },
        new Def{ name="Zombie",          prefabPath="Assets/Prefabs/Enemies/TierB/Zombie.prefab",          hp=34, atk=8, spd=6, skills=new[]{ Strike() }, battlegroundIndex=0, displayScale=1.0f },
        new Def{ name="Element Crystal", prefabPath="Assets/Prefabs/Enemies/TierB/Element Crystal.prefab", hp=34, atk=8, spd=6, skills=new[]{ Strike() }, battlegroundIndex=3, displayScale=1.4f },

        new Def{ name="Genie",         prefabPath="Assets/Prefabs/Enemies/TierC/Genie.prefab",         hp=42, atk=10, spd=8, skills=new[]{ Strike() }, battlegroundIndex=6, displayScale=1.0f },
        new Def{ name="Fairy",         prefabPath="Assets/Prefabs/Enemies/TierC/Fairy.prefab",         hp=42, atk=10, spd=8, skills=new[]{ Strike() }, battlegroundIndex=5, displayScale=1.8f },
        new Def{ name="Vampire Tulip", prefabPath="Assets/Prefabs/Enemies/TierC/Vampire Tulip.prefab", hp=42, atk=10, spd=8, skills=new[]{ Strike() }, battlegroundIndex=2, displayScale=1.3f },
        new Def{ name="Two Faced",     prefabPath="Assets/Prefabs/Enemies/TierC/Two Faced.prefab",     hp=42, atk=10, spd=8, skills=new[]{ Strike() }, battlegroundIndex=6, displayScale=1.8f },
        new Def{ name="Cactuar",       prefabPath="Assets/Prefabs/Enemies/TierC/Cactuar.prefab",       hp=42, atk=10, spd=8, skills=new[]{ Strike() }, battlegroundIndex=4, displayScale=1.25f },

        new Def{ name="Treant",      prefabPath="Assets/Prefabs/Enemies/TierD/Treant.prefab",      hp=95,  atk=15, spd=7, skills=BossKit, battlegroundIndex=6, displayScale=1.0f },
        new Def{ name="Asimole",     prefabPath="Assets/Prefabs/Enemies/TierD/Asimole.prefab",     hp=95,  atk=15, spd=7, skills=BossKit, battlegroundIndex=0, displayScale=1.0f },
        new Def{ name="Creeps",      prefabPath="Assets/Prefabs/Enemies/TierD/Creeps.prefab",      hp=95,  atk=15, spd=7, skills=BossKit, battlegroundIndex=1, displayScale=1.3f },
        new Def{ name="Demonpot",    prefabPath="Assets/Prefabs/Enemies/TierD/Demonpot.prefab",    hp=95,  atk=15, spd=7, skills=BossKit, battlegroundIndex=6, displayScale=1.0f },
        new Def{ name="Mechasphere", prefabPath="Assets/Prefabs/Enemies/TierD/Mechasphere.prefab", hp=95,  atk=15, spd=7, skills=BossKit, battlegroundIndex=3, displayScale=1.0f },
        new Def{ name="Zero",        prefabPath="Assets/Prefabs/Enemies/TierD/Zero.prefab",        hp=130, atk=18, spd=9, skills=ZeroKit, battlegroundIndex=4, displayScale=1.05f },
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
            holder.maxAP = d.maxAP;
            holder.skills = d.skills ?? new[] { Strike() };
            holder.battlegroundIndex = d.battlegroundIndex;
            holder.displayScale = d.displayScale;
            holder.hasComboHealPassive = d.hasComboHealPassive;
            holder.hasArcaneChargePassive = d.hasArcaneChargePassive;
            holder.hasMarkedPreyPassive = d.hasMarkedPreyPassive;
            holder.hasContractorDiscountPassive = d.hasContractorDiscountPassive;

            PrefabUtility.SaveAsPrefabAsset(root, d.prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            ok++;
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[UnitStatsSetup] Hoàn tất: {ok} thành công, {fail} lỗi.");
    }
}
