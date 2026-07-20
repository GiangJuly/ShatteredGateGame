using UnityEngine;

/// Kho dữ liệu tĩnh dùng chung cho mọi scene (Hero/Enemy prefab, portrait/icon,
/// 5 chặng + Final Boss, lore, background chiến đấu, khung hình Portal transition).
/// Chỉ có 1 asset duy nhất tại Assets/Data/GameDatabase.asset — mọi Controller đọc từ đây
/// thay vì mỗi scene tự khai báo riêng (tránh lặp dữ liệu, sửa 1 chỗ áp dụng toàn game).
[CreateAssetMenu(fileName = "GameDatabase", menuName = "ShatteredGate/Game Database")]
public class GameDatabase : ScriptableObject
{
    [Header("Heroes")]
    public GameObject mainHeroPrefab; // Graham
    public GameObject[] recruitOrder; // Sally, Violet, James
    public string[] recruitLoreLines;

    [Header("Map — 5 chặng + Final Boss")]
    public MapManager.StageDef[] stages;
    public GameObject finalBossPrefab;
    public string finalBossName = "Zero";
    public Sprite finalBossIcon;

    [Header("Story")]
    [TextArea(3, 6)] public string openingStoryText;
    [TextArea(3, 6)] public string finalBossIntroText;
    [TextArea(3, 6)] public string victoryEpilogueText;

    [Header("Portrait/Icon lookup (Hero + Enemy)")]
    public CombatManager.NamedSprite[] portraitLookup;

    [Header("Combat background theo Tier")]
    public Sprite[] battlegroundSprites;

    [Header("Boss Scene")]
    public Sprite bossStoryBackground; // nền riêng cho Story panel của Scene Boss (khác Story panel Scene Game)

    [Header("VFX")]
    public Sprite[] portalFrames; // dùng cho hiệu ứng LoadingFade/Portal
    public LoadingFade loadingFadePrefab; // để các Controller tự tạo LoadingFade nếu mở scene trực tiếp (bỏ qua Splash)
    public Sprite[] hitFxFrames;            // Hero — đòn thường
    public Sprite[] bigHitFxFrames;         // Hero — combo/streak/đòn kết liễu
    public Sprite[] vulnerableApplyFx;      // Hero — gắn Vulnerable (Shadow Slash)
    public Sprite[] stunApplyFx;            // gắn Stunned (Pin Down) — dùng chung 2 phe
    public Sprite[] ultimateFxFrames;       // riêng Shattered Gate
    public Sprite[] enemyHitFxFrames;       // Enemy — đòn thường
    public Sprite[] enemyBigHitFxFrames;    // Enemy — đòn to/kết liễu
    public Sprite[] enemyVulnerableApplyFx; // Enemy — gắn Vulnerable (Crushing Blow)

    [Header("SFX")]
    public AudioClip hitSfx;
    public AudioClip victorySfx;
    public AudioClip defeatSfx;
}
