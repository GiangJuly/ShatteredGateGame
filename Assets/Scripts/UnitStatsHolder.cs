using UnityEngine;

/// Tag trạng thái combo: một unit chỉ mang tối đa 1 Tag tại một thời điểm.
/// Vulnerable/Stunned do kỹ năng của phe khác gắn lên, tự hết hạn sau vài lượt của chính unit đó.
public enum TagType { None, Vulnerable, Stunned }

/// Một kỹ năng chiến đấu. Skill cơ bản "Strike" (apCost 0) luôn dùng được;
/// skill "setup" gắn Tag lên mục tiêu; skill "combo" ăn theo Tag đã có để gây dmg bồi.
[System.Serializable]
public class Skill
{
    public string skillName = "Strike";
    public int apCost = 0;
    public float powerMultiplier = 1f;
    public TagType appliesTag = TagType.None;
    public int tagDuration = 2;
    public TagType consumesTag = TagType.None;
    public float comboMultiplier = 1.75f;
}

/// Chỉ số chiến đấu gắn trên mỗi Prefab Hero/Enemy. Chỉnh trực tiếp trong
/// Inspector của từng prefab để cân bằng game sau này.
public class UnitStatsHolder : MonoBehaviour
{
    public string unitName = "Unit";
    public int maxHP = 20;
    public int attackPower = 5;
    public int speed = 5;
    public bool isPlayerUnit;
    public int maxAP = 3;
    public Skill[] skills = new[] { new Skill { skillName = "Strike" } };

    /// Index vào CombatManager.battlegroundSprites — chọn background combat theo tier của enemy này.
    public int battlegroundIndex = 0;
}
