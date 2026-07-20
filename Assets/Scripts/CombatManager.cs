using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// Combat turn-based: 1 kẻ địch / node, thứ tự lượt theo Speed. Mỗi lượt người chơi
/// chọn 1 trong các Skill của unit hiện tại (giới hạn bởi AP); Skill có thể gắn Tag
/// (Vulnerable/Stunned) lên mục tiêu hoặc ăn theo Tag đã có để gây combo dmg bồi.
/// Địch tự chọn skill (ưu tiên skill đặc biệt khi đủ AP) và mục tiêu.
public class CombatManager : MonoBehaviour
{
    [Header("Spawn points (world space)")]
    public Transform[] partySpawnPoints;
    public Transform enemySpawnPoint;
    public float unitScale = 2f;

    [Header("Background (world space, theo tier quái)")]
    public SpriteRenderer backgroundRenderer;
    public Sprite[] battlegroundSprites;

    [Header("UI")]
    public Text turnOrderText;
    public Text logText;
    public Text apText;
    public Button[] skillButtons;

    [Header("HP Bars (4 party slots)")]
    public Text[] partyNameText;
    public Image[] partyHpFill;
    public Image[] partyPortrait;
    public Text[] partyTagText;

    [Header("HP Bar (enemy)")]
    public Text enemyNameText;
    public Image enemyHpFill;
    public Image enemyPortrait;
    public Text enemyTagText;

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
        public bool IsDead => currentHP <= 0;
    }

    readonly List<Unit> party = new List<Unit>();
    Unit enemy;
    List<Unit> turnQueue = new List<Unit>();
    int turnIndex;
    bool waitingForPlayerInput;
    bool hasEnded;
    Skill chosenSkill;
    System.Action<bool> onCombatEnd;

    static readonly Skill BasicStrike = new Skill { skillName = "Strike", apCost = 0, powerMultiplier = 1f };

    public void StartCombat(GameObject[] partyPrefabs, GameObject enemyPrefab, System.Action<bool> onEnd)
    {
        onCombatEnd = onEnd;
        ClearPrevious();
        hasEnded = false;

        for (int i = 0; i < partyPrefabs.Length && i < partySpawnPoints.Length; i++)
        {
            var go = Instantiate(partyPrefabs[i], partySpawnPoints[i].position, Quaternion.identity);
            go.transform.localScale *= unitScale;
            var u = new Unit
            {
                go = go,
                animator = go.GetComponent<Animator>(),
                stats = go.GetComponent<UnitStatsHolder>()
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

        SetBackgroundForEnemy(enemyPrefab);

        var eGo = Instantiate(enemyPrefab, enemySpawnPoint.position, Quaternion.identity);
        eGo.transform.localScale *= unitScale;
        enemy = new Unit
        {
            go = eGo,
            animator = eGo.GetComponent<Animator>(),
            stats = eGo.GetComponent<UnitStatsHolder>()
        };
        ResetToIdle(enemy.animator);
        enemy.currentHP = enemy.stats.maxHP;
        var enemySprite = FindPortrait(enemy.stats.unitName);
        enemyPortrait.sprite = enemySprite;
        enemyPortrait.enabled = enemySprite != null;

        turnQueue = party.Concat(new[] { enemy }).OrderByDescending(u => u.stats.speed).ToList();
        turnIndex = 0;

        HideSkillButtons();

        UpdateStatusUI();
        StartCoroutine(RunTurns());
    }

    // Đổi background combat theo tier của enemy (battlegroundIndex đọc thẳng từ prefab, không cần instantiate trước).
    // Background là SpriteRenderer world-space (không phải UI) để nằm đúng lớp giữa nhân vật và Tilemap nền,
    // vì Canvas Screen Space Overlay luôn vẽ đè lên toàn bộ world dù đặt thứ tự thế nào trong Hierarchy.
    void SetBackgroundForEnemy(GameObject enemyPrefab)
    {
        if (backgroundRenderer == null || battlegroundSprites == null || battlegroundSprites.Length == 0) return;
        var stats = enemyPrefab.GetComponent<UnitStatsHolder>();
        int idx = stats != null ? Mathf.Clamp(stats.battlegroundIndex, 0, battlegroundSprites.Length - 1) : 0;
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
        if (enemy != null && enemy.go != null) Destroy(enemy.go);
        enemy = null;
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
            int thisIndex = turnIndex;
            turnIndex++;

            if (current.IsDead) continue;

            UpdateTurnOrderUI(thisIndex);
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
                UpdateSkillButtons(current);
                logText.text = $"{current.stats.unitName}'s turn — choose a skill!";
                waitingForPlayerInput = true;
                chosenSkill = null;
                while (waitingForPlayerInput) yield return null;
                HideSkillButtons();
                if (enemy.IsDead) continue;
                yield return StartCoroutine(DoSkill(current, enemy, chosenSkill));
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

    Skill ChooseEnemySkill(Unit enemyUnit)
    {
        var skills = enemyUnit.stats.skills;
        if (skills == null || skills.Length == 0) return BasicStrike;
        if (skills.Length > 1 && enemyUnit.currentAP >= skills[1].apCost) return skills[1];
        return skills[0];
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

        attacker.currentAP = Mathf.Max(0, attacker.currentAP - skill.apCost);

        int dmg = Mathf.Max(1, Mathf.RoundToInt(attacker.stats.attackPower * skill.powerMultiplier) + Random.Range(-2, 3));
        bool combo = skill.consumesTag != TagType.None && target.tag == skill.consumesTag;
        if (combo)
        {
            dmg = Mathf.RoundToInt(dmg * skill.comboMultiplier);
            target.tag = TagType.None;
            target.tagTurns = 0;
        }
        target.currentHP = Mathf.Max(0, target.currentHP - dmg);

        if (skill.appliesTag != TagType.None && !target.IsDead)
        {
            target.tag = skill.appliesTag;
            target.tagTurns = skill.tagDuration;
        }

        if (cameraShake != null) cameraShake.Shake();
        if (sfxSource != null && hitSfx != null) sfxSource.PlayOneShot(hitSfx);

        string comboNote = combo ? "  COMBO!" : "";
        if (target.IsDead)
        {
            target.animator.SetTrigger("Dead");
            logText.text = $"{attacker.stats.unitName} uses {skill.skillName} on {target.stats.unitName}: -{dmg} dmg.{comboNote} {target.stats.unitName} is defeated!";
        }
        else
        {
            target.animator.SetTrigger("Hurt");
            logText.text = $"{attacker.stats.unitName} uses {skill.skillName}: -{dmg} dmg.{comboNote} ({target.currentHP}/{target.stats.maxHP} HP)";
        }

        UpdateStatusUI();
        yield return new WaitForSeconds(0.35f);
    }

    void UpdateSkillButtons(Unit unit)
    {
        var skills = (unit.stats.skills != null && unit.stats.skills.Length > 0) ? unit.stats.skills : new[] { BasicStrike };
        for (int i = 0; i < skillButtons.Length; i++)
        {
            if (i < skills.Length)
            {
                var skill = skills[i];
                skillButtons[i].gameObject.SetActive(true);
                skillButtons[i].interactable = unit.currentAP >= skill.apCost;
                var label = skillButtons[i].GetComponentInChildren<Text>();
                label.text = skill.apCost > 0 ? $"{skill.skillName}\n({skill.apCost} AP)" : skill.skillName;
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
        if (enemy == null) return false;

        if (enemy.IsDead)
        {
            hasEnded = true;
            logText.text = $"Victory! {enemy.stats.unitName} has been defeated.";
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

        if (enemy != null)
        {
            enemyNameText.text = $"{enemy.stats.unitName}  {enemy.currentHP}/{enemy.stats.maxHP}";
            enemyHpFill.fillAmount = enemy.stats.maxHP > 0 ? (float)enemy.currentHP / enemy.stats.maxHP : 0f;
            enemyHpFill.color = HpColor(enemyHpFill.fillAmount);
            if (enemyTagText != null)
            {
                enemyTagText.text = TagLabel(enemy);
                enemyTagText.color = TagColor(enemy.tag);
            }
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

    void UpdateTurnOrderUI(int fromIndex)
    {
        var names = new List<string>();
        for (int i = 0; i < turnQueue.Count * 2 && names.Count < 5; i++)
        {
            var u = turnQueue[(fromIndex + i) % turnQueue.Count];
            if (!u.IsDead) names.Add(u.stats.unitName);
        }
        turnOrderText.text = "Turn order: " + string.Join(" → ", names);
    }
}
