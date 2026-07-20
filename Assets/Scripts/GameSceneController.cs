using UnityEngine;
using UnityEngine.UI;

/// Điều phối Scene "Game": Story (mở đầu + lore tuyển đồng đội, dùng chung 1 panel) ->
/// Map (5 chặng) -> Combat HUD -> Reward -> quay lại Map, lặp tới khi chặng 5 xong thì
/// chuyển hẳn sang Scene "Boss" riêng (Final Boss có cảm giác "khác biệt, nặng đô" hơn).
/// Pause/Inventory là overlay nổi trên mọi panel khác.
public class GameSceneController : MonoBehaviour
{
    public GameDatabase database; // dự phòng: tự tạo GameSession/LoadingFade nếu mở thẳng scene này khi test

    [Header("Panels")]
    public GameObject storyPanel;
    public GameObject mapPanel;
    public GameObject combatPanel;
    public GameObject rewardPanel;
    public GameObject inventoryPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;

    [Header("Story panel content")]
    public Text storyTitleText;
    public Text storyBodyText;
    public Button storyContinueButton;

    [Header("Reward panel")]
    public Text rewardBodyText;
    public Button rewardContinueButton;

    [Header("Buttons")]
    public Button pauseResumeButton;
    public Button pauseInventoryButton;
    public Button pauseSettingsButton;
    public Button pauseQuitButton;
    public Button inventoryBackButton;
    public Button gameOverRestartButton;
    public Button gameOverQuitButton;

    public MapManager mapManager;
    public CombatManager combatManager;

    GameSession Session => GameSession.Instance;
    GameDatabase Db => database;

    // Recruit lore chỉ cần sống trong lúc Scene Game còn tải (không qua Scene khác) nên
    // giữ index cục bộ ở đây, không cần đẩy lên GameSession.
    int recruitIndex;

    enum Stage { Story, Map, Combat, Reward, Pause, Inventory, GameOver }
    Stage current;
    Stage beforePause;
    bool cameFromKeyGate;

    void Start()
    {
        SessionBootstrap.Ensure(database);
        // Mở thẳng scene Game khi test (bỏ qua MainMenu) sẽ chưa có đội hình — tự khởi tạo run mới.
        if (Session.ActiveParty.Length == 0) Session.StartNewRun();

        mapManager.stages = Db.stages;
        mapManager.onCombatGate = OnCombatGate;
        mapManager.onKeyGate = OnKeyGate;
        mapManager.onTreasureGate = OnTreasureGate;

        storyContinueButton.onClick.AddListener(OnStoryContinue);
        rewardContinueButton.onClick.AddListener(OnRewardContinue);
        pauseResumeButton.onClick.AddListener(ResumeFromPause);
        pauseInventoryButton.onClick.AddListener(() => ShowImmediate(Stage.Inventory));
        pauseQuitButton.onClick.AddListener(OnQuitToMenu);
        inventoryBackButton.onClick.AddListener(() => ShowImmediate(Stage.Pause));
        gameOverRestartButton.onClick.AddListener(OnQuitToMenu);
        gameOverQuitButton.onClick.AddListener(OnQuitToMenu);
        pauseSettingsButton.interactable = false;

        recruitIndex = 0;
        cameFromKeyGate = false;
        storyTitleText.text = "SHATTERED GATE";
        storyBodyText.text = Db.openingStoryText;
        ShowImmediate(Stage.Story); // vừa vào Scene qua Portal rồi, khỏi lặp hiệu ứng lần nữa
    }

    void OnStoryContinue()
    {
        if (cameFromKeyGate)
        {
            if (recruitIndex < Db.recruitOrder.Length)
            {
                Session.RecruitHero(Db.recruitOrder[recruitIndex]);
                recruitIndex++;
            }
            cameFromKeyGate = false;
            mapManager.ResolveCurrentGate(); // cổng Key không bao giờ là Boss — chỉ đánh dấu đã dọn, ở lại chặng hiện tại
            GoTo(Stage.Map, () => mapManager.ShowCurrentStage());
        }
        else
        {
            mapManager.Setup();
            GoTo(Stage.Map);
        }
    }

    // isBoss/isElite lấy thẳng từ loại cổng — Elite được CombatManager bơm máu/damage cao hơn.
    // enemyPrefabs có thể là 2-3 phần tử (trận bầy quái nhỏ) tuỳ Gate.
    void OnCombatGate(GameObject[] enemyPrefabs, MapManager.GateType gateType, int bgOverride)
    {
        GoTo(Stage.Combat, () =>
        {
            combatManager.portraitLookup = Db.portraitLookup;
            combatManager.battlegroundSprites = Db.battlegroundSprites;
            combatManager.hitFxFrames = Db.hitFxFrames;
            combatManager.bigHitFxFrames = Db.bigHitFxFrames;
            combatManager.vulnerableApplyFx = Db.vulnerableApplyFx;
            combatManager.stunApplyFx = Db.stunApplyFx;
            combatManager.ultimateFxFrames = Db.ultimateFxFrames;
            combatManager.enemyHitFxFrames = Db.enemyHitFxFrames;
            combatManager.enemyBigHitFxFrames = Db.enemyBigHitFxFrames;
            combatManager.enemyVulnerableApplyFx = Db.enemyVulnerableApplyFx;
            combatManager.hitSfx = Db.hitSfx;
            combatManager.victorySfx = Db.victorySfx;
            combatManager.defeatSfx = Db.defeatSfx;
            combatManager.StartCombat(Session.ActiveParty, enemyPrefabs, OnCombatEnd, gateType == MapManager.GateType.Elite, bgOverride);
        });
    }

    void OnCombatEnd(bool won)
    {
        if (!won) { GoTo(Stage.GameOver); return; }
        if (rewardBodyText != null) rewardBodyText.text = "You found nothing of value here... yet.";
        GoTo(Stage.Reward);
    }

    // Cổng Treasure không có chiến đấu — vào thẳng Reward panel (khung rỗng, chờ hệ thống Relic/Gold sau này).
    void OnTreasureGate()
    {
        if (rewardBodyText != null) rewardBodyText.text = "A quiet trove among the ruins. Nothing to claim yet — but the path ahead feels a little safer.";
        GoTo(Stage.Reward);
    }

    void OnRewardContinue()
    {
        bool wasBoss = mapManager.ResolveCurrentGate();
        if (wasBoss && mapManager.IsAtFinalBoss)
        {
            LoadingFade.Instance.LoadScene("Boss");
            return;
        }
        GoTo(Stage.Map, () => mapManager.ShowCurrentStage());
    }

    void OnKeyGate()
    {
        cameFromKeyGate = true;
        string lore = recruitIndex < Db.recruitLoreLines.Length
            ? Db.recruitLoreLines[recruitIndex]
            : "You find only silence and dust here.";
        GoTo(Stage.Story, () =>
        {
            storyTitleText.text = "GATE OF KEYS";
            storyBodyText.text = lore;
        });
    }

    void ResumeFromPause()
    {
        Time.timeScale = 1f;
        ShowImmediate(beforePause); // Pause là overlay nhẹ, không cần hiệu ứng Portal
    }

    void TogglePause()
    {
        if (pausePanel.activeSelf) { ResumeFromPause(); return; }
        if (current == Stage.GameOver) return;
        beforePause = current;
        Time.timeScale = 0f;
        ShowImmediate(Stage.Pause);
    }

    void OnQuitToMenu()
    {
        Time.timeScale = 1f;
        if (combatPanel.activeSelf) combatManager.AbortCombat();
        LoadingFade.Instance.LoadScene("MainMenu");
    }

    // Đổi panel kèm hiệu ứng Portal che màn hình — dùng cho các bước tiến chính của run.
    // extraAction (nếu có) chạy đúng lúc màn hình bị che kín, cùng lúc với việc đổi panel.
    void GoTo(Stage stage, System.Action extraAction = null)
    {
        LoadingFade.Instance.SwapPanel(() =>
        {
            ShowImmediate(stage);
            extraAction?.Invoke();
        });
    }

    void ShowImmediate(Stage stage)
    {
        current = stage;
        storyPanel.SetActive(stage == Stage.Story);
        mapPanel.SetActive(stage == Stage.Map);
        combatPanel.SetActive(stage == Stage.Combat);
        rewardPanel.SetActive(stage == Stage.Reward);
        inventoryPanel.SetActive(stage == Stage.Inventory);
        pausePanel.SetActive(stage == Stage.Pause);
        gameOverPanel.SetActive(stage == Stage.GameOver);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (current == Stage.Inventory) { ShowImmediate(Stage.Pause); return; }
            if (current != Stage.GameOver) TogglePause();
            return;
        }

        if (Time.timeScale == 0f) return;

        bool confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
        if (!confirm) return;

        if (current == Stage.Story) storyContinueButton.onClick.Invoke();
        else if (current == Stage.Reward) rewardContinueButton.onClick.Invoke();
    }
}
