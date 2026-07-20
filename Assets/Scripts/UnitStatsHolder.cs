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

    /// Đánh dấu đòn "đánh thường" — dùng liên tiếp (không xen skill khác) 3 lần trong trận
    /// thì đòn thứ 3 được cộng thêm dmg, rồi bộ đếm reset.
    public bool isComboStrike = false;
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

    /// Index vào CombatManager.battlegroundSprites — chọn background combat mặc định cho enemy này
    /// (có thể bị ghi đè theo từng Gate cụ thể — xem MapManager.Gate.battlegroundIndexOverride).
    public int battlegroundIndex = 0;

    /// Hệ số phóng to RIÊNG cho unit này, nhân thêm vào CombatManager.unitScale chung — cố định qua
    /// UnitStatsSetup nên không bị lệch dù sau này chỉnh unitScale chung. Cần thiết vì các sprite gốc
    /// có độ "đệm" (khoảng trong suốt quanh nhân vật) rất khác nhau dù cùng kích thước khung hình
    /// (VD Fairy/Slime chỉ chiếm ~20% khung, trong khi Treant/Asimole chiếm ~80%) — nếu không bù riêng,
    /// unit nào đệm nhiều sẽ luôn trông nhỏ/mờ hơn hẳn dù cùng 1 hệ số scale chung.
    public float displayScale = 1f;

    /// Passive: mỗi 5 đòn đánh trúng (bất kỳ skill nào) trong trận, hồi 5% HP tối đa. (Graham)
    public bool hasComboHealPassive = false;

    /// Passive Sally — Arcane Charge: mỗi lần đồng đội khác gắn Vulnerable lên địch, Sally tích 1 charge.
    /// Đủ 3 charge thì đòn ăn-Vulnerable kế tiếp của Sally tự động combo dù địch chưa (hoặc hết) mang Vulnerable.
    public bool hasArcaneChargePassive = false;

    /// Passive Violet — Marked Prey: gây thêm 30% dmg lên mục tiêu đang mang BẤT KỲ Tag nào,
    /// không chỉ Tag đúng loại kỹ năng của Violet ăn theo (khác Sally/James chỉ ăn đúng 1 loại Tag).
    public bool hasMarkedPreyPassive = false;

    /// Passive James — Contractor's Discount: mỗi lần Execute kết liễu mục tiêu, đòn kế tiếp của James giảm 1 AP.
    public bool hasContractorDiscountPassive = false;
}
