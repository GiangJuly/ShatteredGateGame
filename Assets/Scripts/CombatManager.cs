using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// Combat turn-based đơn giản: 1 kẻ địch / node, thứ tự lượt theo Speed,
/// người chơi bấm Attack khi tới lượt, địch tự chọn ngẫu nhiên 1 hero còn sống.
public class CombatManager : MonoBehaviour
{
    [Header("Spawn points (world space)")]
    public Transform[] partySpawnPoints;
    public Transform enemySpawnPoint;

    [Header("UI")]
    public Text turnOrderText;
    public Text logText;
    public Button attackButton;

    [Header("HP Bars (4 party slots)")]
    public Text[] partyNameText;
    public Image[] partyHpFill;
    public Image[] partyPortrait;

    [Header("HP Bar (enemy)")]
    public Text enemyNameText;
    public Image enemyHpFill;
    public Image enemyPortrait;

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
        public bool IsDead => currentHP <= 0;
    }

    readonly List<Unit> party = new List<Unit>();
    Unit enemy;
    List<Unit> turnQueue = new List<Unit>();
    int turnIndex;
    bool waitingForPlayerInput;
    bool hasEnded;
    System.Action<bool> onCombatEnd;

    public void StartCombat(GameObject[] partyPrefabs, GameObject enemyPrefab, System.Action<bool> onEnd)
    {
        onCombatEnd = onEnd;
        ClearPrevious();
        hasEnded = false;

        for (int i = 0; i < partyPrefabs.Length && i < partySpawnPoints.Length; i++)
        {
            var go = Instantiate(partyPrefabs[i], partySpawnPoints[i].position, Quaternion.identity);
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

        var eGo = Instantiate(enemyPrefab, enemySpawnPoint.position, Quaternion.identity);
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

        attackButton.onClick.RemoveAllListeners();
        attackButton.onClick.AddListener(() => waitingForPlayerInput = false);
        attackButton.gameObject.SetActive(false);

        UpdateStatusUI();
        StartCoroutine(RunTurns());
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

    IEnumerator RunTurns()
    {
        while (!CheckEnd())
        {
            var current = turnQueue[turnIndex % turnQueue.Count];
            int thisIndex = turnIndex;
            turnIndex++;

            if (current.IsDead) continue;

            UpdateTurnOrderUI(thisIndex);

            if (party.Contains(current))
            {
                logText.text = $"{current.stats.unitName}'s turn — press Attack!";
                attackButton.gameObject.SetActive(true);
                waitingForPlayerInput = true;
                while (waitingForPlayerInput) yield return null;
                attackButton.gameObject.SetActive(false);
                if (enemy.IsDead) continue;
                yield return StartCoroutine(DoAttack(current, enemy));
            }
            else
            {
                var aliveParty = party.Where(p => !p.IsDead).ToList();
                if (aliveParty.Count == 0) continue;
                var target = aliveParty[Random.Range(0, aliveParty.Count)];
                yield return new WaitForSeconds(0.5f);
                yield return StartCoroutine(DoAttack(current, target));
            }
        }
    }

    IEnumerator DoAttack(Unit attacker, Unit target)
    {
        attacker.animator.SetTrigger("Attack");
        yield return new WaitForSeconds(0.35f);

        int dmg = Mathf.Max(1, attacker.stats.attackPower + Random.Range(-2, 3));
        target.currentHP = Mathf.Max(0, target.currentHP - dmg);

        if (cameraShake != null) cameraShake.Shake();
        if (sfxSource != null && hitSfx != null) sfxSource.PlayOneShot(hitSfx);

        if (target.IsDead)
        {
            target.animator.SetTrigger("Dead");
            logText.text = $"{attacker.stats.unitName} hits {target.stats.unitName}: -{dmg} dmg. {target.stats.unitName} is defeated!";
        }
        else
        {
            target.animator.SetTrigger("Hurt");
            logText.text = $"{attacker.stats.unitName} hits {target.stats.unitName}: -{dmg} dmg ({target.currentHP}/{target.stats.maxHP} HP)";
        }

        UpdateStatusUI();
        yield return new WaitForSeconds(0.35f);
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
            }
        }

        if (enemy != null)
        {
            enemyNameText.text = $"{enemy.stats.unitName}  {enemy.currentHP}/{enemy.stats.maxHP}";
            enemyHpFill.fillAmount = enemy.stats.maxHP > 0 ? (float)enemy.currentHP / enemy.stats.maxHP : 0f;
            enemyHpFill.color = HpColor(enemyHpFill.fillAmount);
        }
    }

    static Color HpColor(float ratio)
    {
        if (ratio > 0.5f) return new Color(0.35f, 0.75f, 0.35f);
        if (ratio > 0.25f) return new Color(0.85f, 0.75f, 0.25f);
        return new Color(0.8f, 0.2f, 0.2f);
    }

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
