using UnityEngine;
using UnityEngine.UI;

/// Điều phối Scene "Boss" (Final Boss riêng biệt): Story (lời dẫn) -> Combat HUD ->
/// Victory / Game Over. Dùng lại các Panel Prefab giống Scene Game (Story/CombatHUD/
/// Pause/GameOver) nhưng thêm Victory riêng cho kết thúc thắng cuộc.
public class BossSceneController : MonoBehaviour
{
    public GameDatabase database; // dự phòng: tự tạo GameSession/LoadingFade nếu mở thẳng scene này khi test

    [Header("Panels")]
    public GameObject storyPanel;
    public GameObject combatPanel;
    public GameObject pausePanel;
    public GameObject victoryPanel;
    public GameObject gameOverPanel;

    [Header("Story panel content")]
    public Text storyTitleText;
    public Text storyBodyText;
    public Button storyContinueButton;

    [Header("Victory panel")]
    public Text victoryBodyText;
    public Button victoryBackButton;

    [Header("Buttons")]
    public Button pauseResumeButton;
    public Button pauseSettingsButton;
    public Button pauseQuitButton;
    public Button gameOverRestartButton;
    public Button gameOverQuitButton;

    public CombatManager combatManager;

    GameSession Session => GameSession.Instance;
    GameDatabase Db => database;

    enum Stage { Story, Combat, Pause, Victory, GameOver }
    Stage current;
    Stage beforePause;

    void Start()
    {
        SessionBootstrap.Ensure(database);
        // Mở thẳng scene Boss khi test (bỏ qua Game) sẽ chưa có đội hình — tự khởi tạo run mới.
        if (Session.ActiveParty.Length == 0) Session.StartNewRun();

        storyContinueButton.onClick.AddListener(StartBossFight);
        victoryBackButton.onClick.AddListener(OnQuitToMenu);
        pauseResumeButton.onClick.AddListener(ResumeFromPause);
        pauseQuitButton.onClick.AddListener(OnQuitToMenu);
        gameOverRestartButton.onClick.AddListener(OnQuitToMenu);
        gameOverQuitButton.onClick.AddListener(OnQuitToMenu);
        pauseSettingsButton.interactable = false;

        if (Db.bossStoryBackground != null)
        {
            var bg = storyPanel.GetComponent<Image>();
            if (bg != null) bg.sprite = Db.bossStoryBackground;
        }

        storyTitleText.text = "FINAL BOSS";
        storyBodyText.text = Db.finalBossIntroText;
        ShowImmediate(Stage.Story); // vừa vào Scene qua Portal rồi, khỏi lặp hiệu ứng lần nữa
    }

    void StartBossFight()
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
            combatManager.StartCombat(Session.ActiveParty, new[] { Db.finalBossPrefab }, OnCombatEnd);
        });
    }

    void OnCombatEnd(bool won)
    {
        if (won) victoryBodyText.text = Db.victoryEpilogueText;
        GoTo(won ? Stage.Victory : Stage.GameOver);
    }

    void ResumeFromPause()
    {
        Time.timeScale = 1f;
        ShowImmediate(beforePause); // Pause là overlay nhẹ, không cần hiệu ứng Portal
    }

    void TogglePause()
    {
        if (pausePanel.activeSelf) { ResumeFromPause(); return; }
        if (current == Stage.Victory || current == Stage.GameOver) return;
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
        combatPanel.SetActive(stage == Stage.Combat);
        pausePanel.SetActive(stage == Stage.Pause);
        victoryPanel.SetActive(stage == Stage.Victory);
        gameOverPanel.SetActive(stage == Stage.GameOver);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
            return;
        }

        if (Time.timeScale == 0f) return;

        bool confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
        if (!confirm) return;

        if (current == Stage.Story) storyContinueButton.onClick.Invoke();
        else if (current == Stage.Victory) victoryBackButton.onClick.Invoke();
        else if (current == Stage.GameOver) gameOverRestartButton.onClick.Invoke();
    }
}
