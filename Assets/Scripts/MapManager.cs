using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// Gate Room: mỗi chặng hiện 3-5 cổng toả quanh 1 cổng vòm trung tâm. Người chơi có thể dọn
/// nhiều cổng (Monster/Elite/Treasure) trong CÙNG 1 chặng trước khi vào cổng Boss — cổng Boss
/// luôn mở sẵn (có thể rush thẳng, bỏ qua phần thưởng), còn cổng Key (tuyển đồng đội) có thể bị
/// khoá tới khi dọn đủ N cổng khác trong chặng. Dọn cổng Boss mới thật sự sang chặng kế.
public class MapManager : MonoBehaviour
{
    public enum GateType { Monster, Elite, Treasure, Boss, Key }

    [System.Serializable]
    public class Gate
    {
        public GateType type;
        public GameObject[] enemyPrefabs; // rỗng cho Key/Treasure; 2-3 phần tử = trận bầy (tăng độ khó)
        public string label;
        public Sprite icon;
        // -1 = CombatManager tự lấy battlegroundIndex mặc định của enemyPrefabs[0]; >=0 = ép dùng
        // chỉ số này — cần khi Gate này dùng lại đúng Prefab của 1 Gate khác trong cùng chặng (VD
        // Elite dùng lại quái của Monster) để 2 trận không hiện trùng y hệt 1 nền.
        public int battlegroundIndexOverride = -1;
    }

    [System.Serializable]
    public class StageDef
    {
        public string stageName;
        public Gate[] gates;
        // Số cổng khác (không tính Key/Boss) phải dọn xong trước khi cổng Key mở khoá. 0 = mở sẵn.
        public int keyUnlockAfterClears = 0;
    }

    public StageDef[] stages;

    [Header("UI - Gate Buttons (tối đa 5, xếp toả tròn quanh tâm)")]
    public Button[] gateButtons;
    public Image[] gateDiamonds;
    public float ringRadius = 220f;
    public float ringCenterY = -10f;

    [Header("Colors theo loại cổng")]
    public Color monsterColor = new Color(0.55f, 0.16f, 0.16f);
    public Color eliteColor = new Color(0.45f, 0.2f, 0.7f);
    public Color treasureColor = new Color(0.78f, 0.66f, 0.22f);
    public Color keyColor = new Color(0.88f, 0.86f, 0.78f);
    public Color keyLockedColor = new Color(0.28f, 0.27f, 0.3f);
    public Color bossColor = new Color(0.1f, 0.04f, 0.06f);

    int currentStage;
    bool[] cleared;
    int nonKeyClearedCount;
    Gate pendingGate;
    int pendingGateIndex;

    public System.Action<GameObject[], GateType, int> onCombatGate;
    public System.Action onKeyGate;
    public System.Action onTreasureGate;

    public bool IsAtFinalBoss => currentStage >= stages.Length;

    public void Setup()
    {
        currentStage = 0;
        InitStageState();
        ShowStage();
    }

    // Vẽ lại Gate Room cho chặng hiện tại (dùng khi quay lại sau khi dọn xong 1 cổng không phải Boss).
    public void ShowCurrentStage()
    {
        if (!IsAtFinalBoss) ShowStage();
    }

    void InitStageState()
    {
        if (IsAtFinalBoss) return;
        cleared = new bool[stages[currentStage].gates.Length];
        nonKeyClearedCount = 0;
    }

    // Lưu ý: trận Zero (Final Boss) không đi qua Gate Room này — nó nằm ở Scene "Boss" riêng,
    // chuyển sang ngay khi ResolveCurrentGate() báo dọn xong Boss của chặng 5 (xem GameSceneController).
    void ShowStage()
    {
        var stage = stages[currentStage];
        var active = new List<int>();
        for (int i = 0; i < stage.gates.Length; i++)
            if (!cleared[i]) active.Add(i);

        for (int slot = 0; slot < gateButtons.Length; slot++)
        {
            if (slot >= active.Count)
            {
                gateButtons[slot].gameObject.SetActive(false);
                continue;
            }

            int gateIndex = active[slot];
            var gate = stage.gates[gateIndex];
            bool locked = gate.type == GateType.Key && nonKeyClearedCount < stage.keyUnlockAfterClears;

            gateButtons[slot].gameObject.SetActive(true);
            PositionSlot(slot, active.Count);
            gateDiamonds[slot].color = locked ? keyLockedColor : ColorFor(gate.type);
            gateButtons[slot].interactable = !locked;
            gateButtons[slot].onClick.RemoveAllListeners();
            int capturedIndex = gateIndex;
            gateButtons[slot].onClick.AddListener(() => OnGateChosen(gate, capturedIndex));
        }
    }

    // Xếp N cổng đều quanh 1 vòng tròn, toả ra từ cổng vòm trung tâm — cổng dọn xong biến mất
    // khỏi vòng, các cổng còn lại tự dồn lại đều đặn thay vì để trống 1 khoảng.
    void PositionSlot(int slot, int total)
    {
        float angleStep = 360f / total;
        // Bắt đầu từ đỉnh (90°) và đi theo chiều kim đồng hồ cho dễ nhìn.
        float angleDeg = 90f - angleStep * slot;
        float rad = angleDeg * Mathf.Deg2Rad;
        var pos = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad) * 0.6f, 0f) * ringRadius + Vector3.up * ringCenterY;
        gateButtons[slot].transform.localPosition = pos;
    }

    Color ColorFor(GateType type) => type switch
    {
        GateType.Monster => monsterColor,
        GateType.Elite => eliteColor,
        GateType.Treasure => treasureColor,
        GateType.Key => keyColor,
        GateType.Boss => bossColor,
        _ => monsterColor,
    };

    void OnGateChosen(Gate gate, int gateIndex)
    {
        pendingGate = gate;
        pendingGateIndex = gateIndex;

        switch (gate.type)
        {
            case GateType.Key:
                onKeyGate?.Invoke();
                break;
            case GateType.Treasure:
                onTreasureGate?.Invoke();
                break;
            default: // Monster, Elite, Boss
                onCombatGate?.Invoke(gate.enemyPrefabs, gate.type, gate.battlegroundIndexOverride);
                break;
        }
    }

    // Gọi khi kết quả của cổng đang chờ (pendingGate) đã xong hẳn — thắng trận / nhận Treasure /
    // tuyển xong đồng đội. Đánh dấu cổng đó đã dọn; nếu là Boss thì coi như xong cả chặng.
    // Trả về true nếu cổng vừa dọn là Boss (gọi nơi khác biết cần chuyển chặng/scene Boss).
    public bool ResolveCurrentGate()
    {
        bool wasBoss = pendingGate.type == GateType.Boss;

        if (wasBoss)
        {
            currentStage++;
            if (!IsAtFinalBoss) InitStageState();
            return true;
        }

        if (pendingGateIndex >= 0 && pendingGateIndex < cleared.Length)
            cleared[pendingGateIndex] = true;
        if (pendingGate.type != GateType.Key) nonKeyClearedCount++;
        return false;
    }

    // Phím tắt 1-5 chọn cổng tương ứng. Vô hiệu khi đang Pause để không xuyên qua PausePanel.
    void Update()
    {
        if (Time.timeScale == 0f) return;
        for (int i = 0; i < gateButtons.Length; i++)
        {
            if (gateButtons[i].gameObject.activeInHierarchy && gateButtons[i].interactable && Input.GetKeyDown(KeyCode.Alpha1 + i))
                gateButtons[i].onClick.Invoke();
        }
    }
}
