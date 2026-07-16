using UnityEngine;
using UnityEngine.UI;

/// Bản đồ 5+1 chặng: mỗi chặng hiện tối đa 3 cổng (Quái Vật/Ác Thần/Chìa Khóa),
/// người chơi chỉ được chọn 1 cổng rồi tiến sang chặng kế tiếp.
public class MapManager : MonoBehaviour
{
    public enum GateType { Monster, Boss, Key }

    [System.Serializable]
    public class Gate
    {
        public GateType type;
        public GameObject enemyPrefab; // null cho Key
        public string label;
        public Sprite icon;
    }

    [System.Serializable]
    public class StageDef
    {
        public string stageName;
        public Gate[] gates;
    }

    public StageDef[] stages;
    public GameObject finalBossPrefab;
    public string finalBossName = "Zero";
    public Sprite finalBossIcon;

    [Header("UI - Gate Buttons (tối đa 3)")]
    public Button[] gateButtons;
    public Image[] gateDiamonds;
    public Text[] gateLabels;
    public Image[] gateIcons;
    public Text stageTitleText;

    [Header("Colors")]
    public Color monsterColor = new Color(0.55f, 0.16f, 0.16f);
    public Color bossColor = new Color(0.38f, 0.10f, 0.48f);
    public Color keyColor = new Color(0.78f, 0.66f, 0.22f);

    int currentStage;
    public System.Action<GameObject, bool> onCombatGate;
    public System.Action onKeyGate;

    public bool IsAtFinalBoss => currentStage >= stages.Length;

    public void Setup()
    {
        currentStage = 0;
        ShowStage();
    }

    void ShowStage()
    {
        if (IsAtFinalBoss)
        {
            stageTitleText.text = "FINAL BOSS";
            for (int i = 1; i < gateButtons.Length; i++) gateButtons[i].gameObject.SetActive(false);

            gateButtons[0].gameObject.SetActive(true);
            gateDiamonds[0].color = bossColor;
            gateLabels[0].text = finalBossName;
            gateIcons[0].sprite = finalBossIcon;
            gateIcons[0].enabled = finalBossIcon != null;
            gateButtons[0].onClick.RemoveAllListeners();
            gateButtons[0].onClick.AddListener(() => onCombatGate?.Invoke(finalBossPrefab, true));
            return;
        }

        var stage = stages[currentStage];
        stageTitleText.text = $"{stage.stageName}  ({currentStage + 1}/{stages.Length})";

        for (int i = 0; i < gateButtons.Length; i++)
        {
            if (i < stage.gates.Length)
            {
                var gate = stage.gates[i];
                gateButtons[i].gameObject.SetActive(true);
                gateDiamonds[i].color = gate.type switch
                {
                    GateType.Monster => monsterColor,
                    GateType.Boss => bossColor,
                    _ => keyColor
                };
                gateLabels[i].text = gate.label;
                gateIcons[i].sprite = gate.icon;
                gateIcons[i].enabled = gate.icon != null;
                gateButtons[i].onClick.RemoveAllListeners();
                int captured = i;
                gateButtons[i].onClick.AddListener(() => OnGateClicked(stage.gates[captured]));
            }
            else
            {
                gateButtons[i].gameObject.SetActive(false);
            }
        }
    }

    void OnGateClicked(Gate gate)
    {
        if (gate.type == GateType.Key)
            onKeyGate?.Invoke();
        else
            onCombatGate?.Invoke(gate.enemyPrefab, gate.type == GateType.Boss);
    }

    public void AdvanceStage()
    {
        currentStage++;
        ShowStage();
    }
}
