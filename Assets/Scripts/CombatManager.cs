using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// Combat turn-based: 1-3 kẻ địch / node (chọn Elite/quái nhỏ theo bầy tăng độ khó), thứ tự lượt
/// theo Speed. Mỗi lượt người chơi chọn 1 trong các Skill của unit hiện tại (giới hạn bởi AP);
/// Skill có thể gắn Tag (Vulnerable/Stunned) lên mục tiêu hoặc ăn theo Tag đã có để gây combo dmg
/// bồi. Người chơi có thể click vào HUD 1 Enemy để chọn mục tiêu cho đòn tiếp theo (mặc định nhắm
/// địch còn sống đầu tiên nếu chưa chọn). Địch tự chọn skill (ưu tiên skill đặc biệt khi đủ AP) và
/// mục tiêu trong party.
public class CombatManager : MonoBehaviour
{
    [Header("Spawn points (world space, tối đa 3 Enemy)")]
    public Transform[] partySpawnPoints;
    public Transform[] enemySpawnPoints;
    public float unitScale = 2.6f;

    [Header("Background (world space, theo Enemy đầu tiên trong nhóm)")]
    public SpriteRenderer backgroundRenderer;
    public Sprite[] battlegroundSprites;

    [Header("UI")]
    public Text logText;
    public Text apText;
    public Button[] skillButtons;

    [Header("HP Bars (4 party slots)")]
    public Text[] partyNameText;
    public Image[] partyHpFill;
    public Image[] partyPortrait;
    public Text[] partyTagText;

    [Header("HP Bars (tối đa 3 Enemy slot)")]
    public Text[] enemyNameText;
    public Image[] enemyHpFill;
    public Image[] enemyPortrait;
    public Text[] enemyTagText;
    public Button[] enemyTargetButtons; // click để chọn mục tiêu cho đòn tiếp theo của người chơi

    [System.Serializable]
    public class NamedSprite { public string unitName; public Sprite sprite; }
    [Header("Portrait/Icon lookup")]
    public NamedSprite[] portraitLookup;

    Sprite FindPortrait(string unitName)
    {
        if (portraitLookup == null) return null;
        foreach (var p in portraitLookup)
            if (p.unitName == unitName) return p.sprite;
        return null;
    }

    // Hero dùng tông ấm (vàng/cam), Enemy dùng tông lạnh (xanh/lục) — để 2 phe luôn phân biệt được
    // bằng mắt bất kể dùng skill nào. Việc gắn Tag (Vulnerable/Stunned) có hiệu ứng riêng, ưu tiên
    // thấp hơn đòn to/combo/kết liễu (xem ChooseFxFrames).
    [Header("Hit VFX — Hero (tông ấm)")]
    public Sprite[] hitFxFrames;        // đòn thường
    public Sprite[] bigHitFxFrames;     // combo/streak/đòn kết liễu
    public Sprite[] vulnerableApplyFx;  // gắn Vulnerable (Shadow Slash)
    public Sprite[] stunApplyFx;        // gắn Stunned (Pin Down) — dùng chung cho cả 2 phe, sét luôn là ngôn ngữ "choáng"
    public Sprite[] ultimateFxFrames;   // riêng Shattered Gate

    [Header("Hit VFX — Enemy (tông lạnh)")]
    public Sprite[] enemyHitFxFrames;       // đòn thường
    public Sprite[] enemyBigHitFxFrames;    // đòn to/kết liễu
    public Sprite[] enemyVulnerableApplyFx; // gắn Vulnerable (Crushing Blow)

    [Header("Optional")]
    public CameraShake cameraShake;
    public AudioSource sfxSource;
    public AudioClip hitSfx;
    public AudioClip victorySfx;
    public AudioClip defeatSfx;

    class Unit
    {
        public GameObject go;
        public Animator animator;
        public UnitStatsHolder stats;
        public int currentHP;
        public int currentAP;
        public TagType tag;
        public int tagTurns;
        public int comboStrikeStreak;
        public int totalHitsLanded;
        public int maxHPOverride; // >0: dùng thay stats.maxHP (Elite được bơm máu cao hơn prefab gốc)
        public float atkMultiplier = 1f;
        public int actionCount;          // tổng số lượt unit này đã ra tay — dùng cho AI Zero luân phiên skill
        public int lastAnnouncedPhase;   // Zero: phase đã bark log gần nhất, tránh bark lặp lại
        public int arcaneCharge;         // Passive Sally — Arcane Charge
        public int nextSkillApDiscount;  // Passive James — Contractor's Discount, tiêu thụ ở đòn kế tiếp
        public bool IsDead => currentHP <= 0;
    }

    const float EliteHpMultiplier = 1.4f;
    const float EliteAtkMultiplier = 1.25f;

    static int MaxHPOf(Unit u) => u.maxHPOverride > 0 ? u.maxHPOverride : u.stats.maxHP;

    readonly List<Unit> party = new List<Unit>();
    readonly List<Unit> enemies = new List<Unit>();
    int targetEnemyIndex; // slot trong `enemies` mà đòn kế tiếp của người chơi sẽ nhắm tới
    List<Unit> turnQueue = new List<Unit>();
    int turnIndex;
    bool waitingForPlayerInput;
    bool hasEnded;
    Skill chosenSkill;
    System.Action<bool> onCombatEnd;

    static readonly Skill BasicStrike = new Skill { skillName = "Strike", apCost = 0, powerMultiplier = 1f };

    // backgroundIndexOverride: -1 = dùng battlegroundIndex mặc định của Enemy đầu tiên trong nhóm;
    // >=0 = ép dùng chỉ số này — cần khi 1 Gate dùng lại đúng Prefab của Gate khác trong cùng chặng
    // (VD Elite dùng lại quái Monster) để 2 trận không hiện trùng y hệt 1 nền.
    public void StartCombat(GameObject[] partyPrefabs, GameObject[] enemyPrefabs, System.Action<bool> onEnd,
        bool isElite = false, int backgroundIndexOverride = -1)
    {
        onCombatEnd = onEnd;
        ClearPrevious();
        hasEnded = false;
        targetEnemyIndex = 0;

        for (int i = 0; i < partyPrefabs.Length && i < partySpawnPoints.Length; i++)
        {
            var go = Instantiate(partyPrefabs[i], partySpawnPoints[i].position, Quaternion.identity);
            var partyStats = go.GetComponent<UnitStatsHolder>();
            go.transform.localScale *= unitScale * partyStats.displayScale;
            var partySprite = go.GetComponent<SpriteRenderer>();
            if (partySprite != null) partySprite.flipX = true; // quay mặt sang phải, đối diện Enemy
            go.AddComponent<CombatBob>();
            var u = new Unit
            {
                go = go,
                animator = go.GetComponent<Animator>(),
                stats = partyStats
            };
            ResetToIdle(u.animator);
            u.currentHP = u.stats.maxHP;
            party.Add(u);

            if (i < partyPortrait.Length)
            {
                var sprite = FindPortrait(u.stats.unitName);
                partyPortrait[i].sprite = sprite;
                partyPortrait[i].enabled = sprite != null;
            }
        }
        for (int i = party.Count; i < partyPortrait.Length; i++)
            partyPortrait[i].enabled = false;

        SetBackgroundForEnemyGroup(enemyPrefabs, backgroundIndexOverride);

        for (int i = 0; i < enemyPrefabs.Length && i < enemySpawnPoints.Length; i++)
        {
            var eGo = Instantiate(enemyPrefabs[i], enemySpawnPoints[i].position, Quaternion.identity);
            var eStats = eGo.GetComponent<UnitStatsHolder>();
            eGo.transform.localScale *= unitScale * eStats.displayScale;
            eGo.AddComponent<CombatBob>();
            var eu = new Unit
            {
                go = eGo,
                animator = eGo.GetComponent<Animator>(),
                stats = eStats
            };
            ResetToIdle(eu.animator);
            var enemySprite = FindPortrait(eu.stats.unitName); // tra icon TRƯỚC khi đổi tên hiển thị Elite
            if (isElite)
            {
                eu.maxHPOverride = Mathf.RoundToInt(eu.stats.maxHP * EliteHpMultiplier);
                eu.atkMultiplier = EliteAtkMultiplier;
                eu.stats.unitName += " (Elite)"; // an toàn: Instantiate đã tách bản sao component riêng, không đụng prefab gốc
            }
            eu.currentHP = MaxHPOf(eu);
            enemies.Add(eu);

            if (i < enemyPortrait.Length)
            {
                enemyPortrait[i].sprite = enemySprite;
                enemyPortrait[i].enabled = enemySprite != null;
            }
        }

        turnQueue = party.Concat(enemies).OrderByDescending(u => u.stats.speed).ToList();
        turnIndex = 0;

        WireEnemyTargetButtons();
        HideSkillButtons();

        UpdateStatusUI();
        StartCoroutine(RunTurns());
    }

    void WireEnemyTargetButtons()
    {
        if (enemyTargetButtons == null) return;
        for (int i = 0; i < enemyTargetButtons.Length; i++)
        {
            if (enemyTargetButtons[i] == null) continue;
            int captured = i;
            enemyTargetButtons[i].onClick.RemoveAllListeners();
            enemyTargetButtons[i].onClick.AddListener(() =>
            {
                if (captured < enemies.Count && !enemies[captured].IsDead) targetEnemyIndex = captured;
            });
        }
    }

    // Đổi background combat theo Enemy ĐẦU TIÊN trong nhóm (battlegroundIndex đọc thẳng từ prefab,
    // không cần instantiate trước), trừ khi Gate ép 1 chỉ số riêng (backgroundIndexOverride >= 0).
    // Background là SpriteRenderer world-space (không phải UI) để nằm đúng lớp giữa nhân vật và
    // Tilemap nền, vì Canvas Screen Space Overlay luôn vẽ đè lên toàn bộ world dù đặt thứ tự thế
    // nào trong Hierarchy.
    void SetBackgroundForEnemyGroup(GameObject[] enemyPrefabs, int backgroundIndexOverride)
    {
        if (backgroundRenderer == null || battlegroundSprites == null || battlegroundSprites.Length == 0) return;
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        int idx;
        if (backgroundIndexOverride >= 0)
        {
            idx = backgroundIndexOverride;
        }
        else
        {
            var stats = enemyPrefabs[0].GetComponent<UnitStatsHolder>();
            idx = stats != null ? stats.battlegroundIndex : 0;
        }
        idx = Mathf.Clamp(idx, 0, battlegroundSprites.Length - 1);

        var sprite = battlegroundSprites[idx];
        if (sprite == null) return;
        backgroundRenderer.sprite = sprite;

        var cam = Camera.main;
        if (cam != null && cam.orthographic)
        {
            float worldHeight = cam.orthographicSize * 2f;
            float worldWidth = worldHeight * cam.aspect;
            Vector2 spriteSize = sprite.bounds.size;
            backgroundRenderer.transform.localScale = new Vector3(worldWidth / spriteSize.x, worldHeight / spriteSize.y, 1f);
        }
    }

    static void ResetToIdle(Animator anim)
    {
        if (anim == null) return;
        anim.Rebind();
        anim.ResetTrigger("Attack");
        anim.ResetTrigger("Hurt");
        anim.ResetTrigger("Dead");
        anim.Update(0f);
    }

    void ClearPrevious()
    {
        StopAllCoroutines();
        foreach (var u in party) if (u.go != null) Destroy(u.go);
        party.Clear();
        foreach (var u in enemies) if (u.go != null) Destroy(u.go);
        enemies.Clear();
        turnQueue.Clear();
    }

    // Bỏ dở trận đấu (VD người chơi thoát ra Main Menu từ Pause) — dừng coroutine, dọn unit,
    // không gọi onCombatEnd vì đây không phải thắng/thua thật.
    public void AbortCombat()
    {
        ClearPrevious();
        hasEnded = true;
    }

    IEnumerator RunTurns()
    {
        while (!CheckEnd())
        {
            var current = turnQueue[turnIndex % turnQueue.Count];
            turnIndex++;

            if (current.IsDead) continue;

            current.currentAP = Mathf.Min(current.stats.maxAP, current.currentAP + 1);

            if (current.tag == TagType.Stunned)
            {
                logText.text = $"{current.stats.unitName} is stunned and skips the turn!";
                current.tag = TagType.None;
                current.tagTurns = 0;
                UpdateStatusUI();
                yield return new WaitForSeconds(0.6f);
                continue;
            }

            if (party.Contains(current))
            {
                var target = SelectedEnemyOrFirstAlive();
                if (target == null) continue; // toàn bộ Enemy đã gục — CheckEnd() sẽ kết thúc trận ở vòng lặp kế

                UpdateSkillButtons(current);
                waitingForPlayerInput = true;
                chosenSkill = null;
                while (waitingForPlayerInput) yield return null;
                HideSkillButtons();

                target = SelectedEnemyOrFirstAlive();
                if (target == null) continue;
                yield return StartCoroutine(DoSkill(current, target, chosenSkill));
            }
            else
            {
                var aliveParty = party.Where(p => !p.IsDead).ToList();
                if (aliveParty.Count == 0) continue;
                var skill = ChooseEnemySkill(current);
                var target = ChooseEnemyTarget(skill, aliveParty);
                yield return new WaitForSeconds(0.5f);
                yield return StartCoroutine(DoSkill(current, target, skill));
            }

            if (current.tag != TagType.None)
            {
                current.tagTurns--;
                if (current.tagTurns <= 0) { current.tag = TagType.None; current.tagTurns = 0; }
            }
        }
    }

    // Mục tiêu người chơi đã click chọn (nếu còn sống); nếu chưa chọn/đã chết thì tự nhắm Enemy
    // còn sống đầu tiên trong danh sách — luôn có 1 mục tiêu hợp lệ để bấm Skill mà không cần thao
    // tác thêm, click vào HUD Enemy chỉ để ĐỔI mục tiêu khi muốn tập trung hoả lực.
    Unit SelectedEnemyOrFirstAlive()
    {
        if (targetEnemyIndex >= 0 && targetEnemyIndex < enemies.Count && !enemies[targetEnemyIndex].IsDead)
            return enemies[targetEnemyIndex];

        for (int i = 0; i < enemies.Count; i++)
        {
            if (!enemies[i].IsDead) { targetEnemyIndex = i; return enemies[i]; }
        }
        return null;
    }

    Skill ChooseEnemySkill(Unit enemyUnit)
    {
        var skills = enemyUnit.stats.skills;
        if (skills == null || skills.Length == 0) return BasicStrike;

        // Zero (Final Boss, 4 skill: Strike/Fracture/Paralyze/Unravel) đổi hành vi theo % HP còn lại
        // thay vì AI 2-skill đơn giản dùng chung cho các Boss chặng thường.
        if (skills.Length >= 4) return ChooseZeroSkill(enemyUnit, skills);

        if (skills.Length > 1 && enemyUnit.currentAP >= skills[1].apCost) return skills[1];
        return skills[0];
    }

    // Phase 1 (>66% HP): chỉ Strike, cho người chơi làm quen arena.
    // Phase 2 (33-66% HP): luân phiên Fracture (gắn Vulnerable) / Paralyze (gắn Stunned) lên party —
    // quay ngược đúng 2 loại Tag mà bộ kỹ năng của party đang dùng.
    // Phase 3 (<33% HP): ưu tiên Unravel — ăn theo Vulnerable nó vừa gắn ở Phase 2, tự chơi luật combo-Tag
    // giống hệt party. Bark 1 dòng log khi vừa đổi phase.
    Skill ChooseZeroSkill(Unit zero, Skill[] skills)
    {
        var strike = skills[0];
        var fracture = skills[1];
        var paralyze = skills[2];
        var unravel = skills[3];

        int maxHp = MaxHPOf(zero);
        float hpRatio = maxHp > 0 ? (float)zero.currentHP / maxHp : 0f;
        int phase = hpRatio > 0.66f ? 1 : hpRatio > 0.33f ? 2 : 3;

        if (phase != zero.lastAnnouncedPhase && phase > 1)
        {
            zero.lastAnnouncedPhase = phase;
            logText.text = phase == 2 ? "Zero: -- IT STIRS --" : "Zero: -- IT UNRAVELS --";
        }

        if (phase == 1) return strike;

        var alternate = (zero.actionCount % 2 == 0) ? fracture : paralyze;

        if (phase == 3 && zero.currentAP >= unravel.apCost) return unravel;

        return zero.currentAP >= alternate.apCost ? alternate : strike;
    }

    Unit ChooseEnemyTarget(Skill skill, List<Unit> aliveParty)
    {
        if (skill.appliesTag != TagType.None)
        {
            var untagged = aliveParty.Where(p => p.tag != skill.appliesTag).ToList();
            if (untagged.Count > 0) return untagged[Random.Range(0, untagged.Count)];
        }
        return aliveParty[Random.Range(0, aliveParty.Count)];
    }

    IEnumerator DoSkill(Unit attacker, Unit target, Skill skill)
    {
        skill ??= BasicStrike;
        attacker.animator.SetTrigger("Attack");
        yield return new WaitForSeconds(0.35f);

        attacker.actionCount++;

        // Passive James — Contractor's Discount: tiêu thụ ngay đòn này, giảm AP cần trả.
        int apCost = Mathf.Max(0, skill.apCost - attacker.nextSkillApDiscount);
        attacker.nextSkillApDiscount = 0;
        attacker.currentAP = Mathf.Max(0, attacker.currentAP - apCost);

        // Combo đánh thường: dùng liên tiếp (không xen skill khác) 3 lần thì đòn thứ 3 mạnh hơn.
        bool streakBonus = false;
        if (skill.isComboStrike)
        {
            attacker.comboStrikeStreak++;
            if (attacker.comboStrikeStreak >= 3)
            {
                streakBonus = true;
                attacker.comboStrikeStreak = 0;
            }
        }
        else
        {
            attacker.comboStrikeStreak = 0;
        }

        int dmg = Mathf.Max(1, Mathf.RoundToInt(attacker.stats.attackPower * skill.powerMultiplier * attacker.atkMultiplier) + Random.Range(-2, 3));
        if (streakBonus) dmg = Mathf.RoundToInt(dmg * 1.5f);

        // Passive Violet — Marked Prey: +30% dmg lên mục tiêu đang mang BẤT KỲ Tag nào (không chỉ Tag
        // đúng loại cô ăn theo) — khác combo thường vì không tiêu thụ Tag, chỉ cộng thêm sát thương.
        if (attacker.stats.hasMarkedPreyPassive && target.tag != TagType.None)
            dmg = Mathf.RoundToInt(dmg * 1.3f);

        // Passive Sally — Arcane Charge: đủ 3 charge thì đòn ăn-Vulnerable tự combo dù địch chưa mang Tag.
        bool passiveArcaneTrigger = false;
        if (!(skill.consumesTag != TagType.None && target.tag == skill.consumesTag)
            && skill.consumesTag == TagType.Vulnerable && attacker.stats.hasArcaneChargePassive && attacker.arcaneCharge >= 3)
        {
            passiveArcaneTrigger = true;
            attacker.arcaneCharge = 0;
        }

        bool combo = (skill.consumesTag != TagType.None && target.tag == skill.consumesTag) || passiveArcaneTrigger;
        if (combo)
        {
            dmg = Mathf.RoundToInt(dmg * skill.comboMultiplier);
            target.tag = TagType.None;
            target.tagTurns = 0;
        }
        target.currentHP = Mathf.Max(0, target.currentHP - dmg);

        // Passive James — Contractor's Discount: Execute (skill ăn Tag) kết liễu mục tiêu -> đòn kế tiếp giảm 1 AP.
        if (attacker.stats.hasContractorDiscountPassive && combo && target.IsDead)
            attacker.nextSkillApDiscount = 1;

        if (skill.appliesTag != TagType.None && !target.IsDead)
        {
            target.tag = skill.appliesTag;
            target.tagTurns = skill.tagDuration;

            // Passive Sally — mỗi lần ĐỒNG ĐỘI KHÁC gắn Vulnerable lên địch, Sally tích 1 charge.
            if (skill.appliesTag == TagType.Vulnerable)
            {
                foreach (var p in party)
                    if (p != attacker && p.stats.hasArcaneChargePassive)
                        p.arcaneCharge++;
            }
        }

        // Passive: mỗi 5 đòn trúng (bất kỳ skill) hồi 5% HP tối đa.
        string healNote = "";
        if (attacker.stats.hasComboHealPassive)
        {
            attacker.totalHitsLanded++;
            if (attacker.totalHitsLanded % 5 == 0)
            {
                int healAmount = Mathf.Max(1, Mathf.RoundToInt(attacker.stats.maxHP * 0.05f));
                attacker.currentHP = Mathf.Min(attacker.stats.maxHP, attacker.currentHP + healAmount);
                healNote = $"  {attacker.stats.unitName} hồi {healAmount} HP!";
            }
        }
        if (passiveArcaneTrigger) healNote += "  Arcane Charge fires!";
        if (attacker.stats.hasContractorDiscountPassive && combo && target.IsDead) healNote += "  Contractor's Discount!";

        if (cameraShake != null) cameraShake.Shake();
        if (sfxSource != null && hitSfx != null) sfxSource.PlayOneShot(hitSfx);

        bool isUltimate = skill.skillName == "Shattered Gate";
        bool bigHit = streakBonus || combo || target.IsDead;
        Sprite[] fxFrames = ChooseFxFrames(attacker, skill, isUltimate, bigHit);
        float fxScale = isUltimate ? 4f : bigHit ? 2.5f : 1.5f;
        if (target.go != null) HitEffect.Spawn(fxFrames, target.go.transform.position, fxScale);

        string comboNote = streakBonus ? "  COMBO x3!" : (combo ? "  COMBO!" : "");
        if (target.IsDead)
        {
            target.animator.SetTrigger("Dead");
            logText.text = $"{attacker.stats.unitName} uses {skill.skillName} on {target.stats.unitName}: -{dmg} dmg.{comboNote} {target.stats.unitName} is defeated!{healNote}";
        }
        else
        {
            target.animator.SetTrigger("Hurt");
            logText.text = $"{attacker.stats.unitName} uses {skill.skillName}: -{dmg} dmg.{comboNote} ({target.currentHP}/{MaxHPOf(target)} HP){healNote}";
        }

        UpdateStatusUI();
        yield return new WaitForSeconds(0.35f);
    }

    // Ultimate > đòn to/combo/kết liễu (theo phe) > gắn Tag (flavor riêng) > đòn thường (theo phe).
    Sprite[] ChooseFxFrames(Unit attacker, Skill skill, bool isUltimate, bool bigHit)
    {
        if (isUltimate) return ultimateFxFrames;

        bool isHero = party.Contains(attacker);
        if (bigHit) return isHero ? bigHitFxFrames : enemyBigHitFxFrames;

        if (skill.appliesTag == TagType.Stunned) return stunApplyFx;
        if (skill.appliesTag == TagType.Vulnerable) return isHero ? vulnerableApplyFx : enemyVulnerableApplyFx;

        return isHero ? hitFxFrames : enemyHitFxFrames;
    }

    void UpdateSkillButtons(Unit unit)
    {
        var skills = (unit.stats.skills != null && unit.stats.skills.Length > 0) ? unit.stats.skills : new[] { BasicStrike };
        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (i < skills.Length)
            {
                var skill = skills[i];
                // Passive James — Contractor's Discount: đòn kế tiếp giảm AP, phản ánh luôn lên nút/label.
                int effectiveCost = Mathf.Max(0, skill.apCost - unit.nextSkillApDiscount);
                skillButtons[i].gameObject.SetActive(true);
                skillButtons[i].interactable = unit.currentAP >= effectiveCost;
                var label = skillButtons[i].GetComponentInChildren<Text>();
                label.text = effectiveCost > 0 ? $"{skill.skillName}\n({effectiveCost} AP)" : skill.skillName;
                skillButtons[i].onClick.RemoveAllListeners();
                var captured = skill;
                skillButtons[i].onClick.AddListener(() => { chosenSkill = captured; waitingForPlayerInput = false; });
            }
            else
            {
                skillButtons[i].gameObject.SetActive(false);
            }
        }
        if (apText != null) apText.text = $"AP: {unit.currentAP}/{unit.stats.maxAP}";
    }

    void HideSkillButtons()
    {
        foreach (var b in skillButtons) b.gameObject.SetActive(false);
        if (apText != null) apText.text = "";
    }

    // Phím tắt 1/2/3 chọn skill tương ứng khi nút đang hiện và dùng được. Vô hiệu khi đang Pause
    // (Time.timeScale == 0) — nút vẫn active trong hierarchy dù bị PausePanel che, chặn ở đây
    // để phím tắt không xuyên qua overlay.
    void Update()
    {
        if (Time.timeScale == 0f) return;
        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (skillButtons[i].gameObject.activeInHierarchy && skillButtons[i].interactable && Input.GetKeyDown(KeyCode.Alpha1 + i))
                skillButtons[i].onClick.Invoke();
        }
    }

    bool CheckEnd()
    {
        if (hasEnded) return true;
        if (enemies.Count == 0) return false;

        if (enemies.All(e => e.IsDead))
        {
            hasEnded = true;
            logText.text = enemies.Count > 1 ? "Victory! All enemies have been defeated." : $"Victory! {enemies[0].stats.unitName} has been defeated.";
            if (sfxSource != null && victorySfx != null) sfxSource.PlayOneShot(victorySfx);
            StartCoroutine(EndAfterDelay(true));
            return true;
        }
        if (party.Count > 0 && party.All(p => p.IsDead))
        {
            hasEnded = true;
            logText.text = "Your party has fallen. Defeat.";
            if (sfxSource != null && defeatSfx != null) sfxSource.PlayOneShot(defeatSfx);
            StartCoroutine(EndAfterDelay(false));
            return true;
        }
        return false;
    }

    IEnumerator EndAfterDelay(bool won)
    {
        yield return new WaitForSeconds(1.2f);
        onCombatEnd?.Invoke(won);
    }

    void UpdateStatusUI()
    {
        for (int i = 0; i < partyHpFill.Length; i++)
        {
            // Ẩn hẳn cả khối HUD (bar+portrait+text) của slot chưa có hero, thay vì chỉ set rỗng
            var slotRoot = partyHpFill[i].transform.parent.gameObject;
            bool active = i < party.Count;
            if (slotRoot.activeSelf != active) slotRoot.SetActive(active);

            if (active)
            {
                var p = party[i];
                partyNameText[i].text = $"{p.stats.unitName}  {p.currentHP}/{p.stats.maxHP}";
                partyHpFill[i].fillAmount = p.stats.maxHP > 0 ? (float)p.currentHP / p.stats.maxHP : 0f;
                partyHpFill[i].color = HpColor(partyHpFill[i].fillAmount);
                if (i < partyTagText.Length)
                {
                    partyTagText[i].text = TagLabel(p);
                    partyTagText[i].color = TagColor(p.tag);
                }
            }
            else if (i < partyTagText.Length)
            {
                partyTagText[i].text = "";
            }
        }

        for (int i = 0; i < enemyHpFill.Length; i++)
        {
            var slotRoot = enemyHpFill[i].transform.parent.gameObject;
            bool active = i < enemies.Count;
            if (slotRoot.activeSelf != active) slotRoot.SetActive(active);
            if (!active)
            {
                if (i < enemyTagText.Length) enemyTagText[i].text = "";
                continue;
            }

            var e = enemies[i];
            int eMax = MaxHPOf(e);
            enemyNameText[i].text = $"{e.stats.unitName}  {e.currentHP}/{eMax}";
            enemyHpFill[i].fillAmount = eMax > 0 ? (float)e.currentHP / eMax : 0f;
            enemyHpFill[i].color = e.IsDead ? new Color(0.3f, 0.3f, 0.3f) : HpColor(enemyHpFill[i].fillAmount);
            if (i < enemyTagText.Length)
            {
                enemyTagText[i].text = TagLabel(e);
                enemyTagText[i].color = TagColor(e.tag);
            }
            if (enemyTargetButtons != null && i < enemyTargetButtons.Length && enemyTargetButtons[i] != null)
                enemyTargetButtons[i].interactable = !e.IsDead;
        }
    }

    static Color HpColor(float ratio)
    {
        if (ratio > 0.5f) return new Color(0.35f, 0.75f, 0.35f);
        if (ratio > 0.25f) return new Color(0.85f, 0.75f, 0.25f);
        return new Color(0.8f, 0.2f, 0.2f);
    }

    static string TagLabel(Unit u) => u.tag switch
    {
        TagType.Vulnerable => $"VULNERABLE ({u.tagTurns})",
        TagType.Stunned => $"STUNNED ({u.tagTurns})",
        _ => ""
    };

    static Color TagColor(TagType t) => t switch
    {
        TagType.Vulnerable => new Color(0.85f, 0.3f, 0.85f),
        TagType.Stunned => new Color(0.95f, 0.85f, 0.2f),
        _ => Color.white
    };

}
