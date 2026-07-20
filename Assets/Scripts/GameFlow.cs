using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// Điều phối 5 màn hình (Menu/Map/Combat/KeyGate/End) trong 1 scene, quản lý
/// đội hình: bắt đầu chỉ có Main, tuyển thêm tối đa 3 đồng đội qua Cổng Chìa Khóa.
public class GameFlow : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject mapPanel;
    public GameObject combatPanel;
    public GameObject keyGatePanel;
    public GameObject endPanel;
    public GameObject creditsPanel;
    public GameObject pausePanel;

    public Button startButton;
    public Button creditsButton;
    public Button creditsBackButton;
    public Button quitButton;
    public Button restartButton;
    public Button keyGateContinueButton;
    public Button resumeButton;
    public Button pauseSettingsButton;
    public Button pauseQuitButton;
    public Text keyGateLoreText;
    public Text endTitleText;

    public MapManager mapManager;
    public CombatManager combatManager;
    public SceneTransition transition;

    public GameObject mainHeroPrefab;
    public GameObject[] recruitOrder;
    public string[] recruitLoreLines;

    readonly List<GameObject> activeParty = new List<GameObject>();
    int recruitIndex;

    void Start()
    {
        startButton.onClick.AddListener(OnStart);
        creditsButton.onClick.AddListener(() => GoTo(creditsPanel));
        creditsBackButton.onClick.AddListener(() => GoTo(mainMenuPanel));
        quitButton.onClick.AddListener(OnQuit);
        restartButton.onClick.AddListener(OnRestart);
        keyGateContinueButton.onClick.AddListener(OnKeyGateContinue);
        resumeButton.onClick.AddListener(Resume);
        pauseQuitButton.onClick.AddListener(OnPauseQuit);

        mapManager.onCombatGate = OnCombatGate;
        mapManager.onKeyGate = OnKeyGate;

        ShowOnly(mainMenuPanel);
    }

    void OnStart()
    {
        activeParty.Clear();
        activeParty.Add(mainHeroPrefab);
        recruitIndex = 0;
        mapManager.Setup();
        GoTo(mapPanel);
    }

    void OnCombatGate(GameObject enemyPrefab, bool isBoss)
    {
        GoTo(combatPanel, () => combatManager.StartCombat(activeParty.ToArray(), enemyPrefab, OnCombatEnd));
    }

    void OnCombatEnd(bool won)
    {
        if (!won)
        {
            GoToEnd(false);
            return;
        }
        if (mapManager.IsAtFinalBoss)
        {
            GoToEnd(true);
            return;
        }
        mapManager.AdvanceStage();
        GoTo(mapPanel);
    }

    void OnKeyGate()
    {
        string lore = recruitIndex < recruitLoreLines.Length
            ? recruitLoreLines[recruitIndex]
            : "You find only silence and dust here.";
        GoTo(keyGatePanel, () => keyGateLoreText.text = lore);
    }

    void OnKeyGateContinue()
    {
        if (recruitIndex < recruitOrder.Length)
        {
            activeParty.Add(recruitOrder[recruitIndex]);
            recruitIndex++;
        }
        mapManager.AdvanceStage();
        GoTo(mapPanel);
    }

    void GoToEnd(bool victory)
    {
        GoTo(endPanel, () => endTitleText.text = victory ? "VICTORY!\nYou have escaped the Shattered Gate." : "DEFEAT");
    }

    void OnRestart()
    {
        GoTo(mainMenuPanel);
    }

    void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Chỉ cho Pause khi đang thực sự trong 1 lượt chơi (Map/Combat/KeyGate) — không áp
    // dụng cho Menu/Credits/End vì các màn đó không có gì để "tạm dừng".
    bool CanPause => mapPanel.activeSelf || combatPanel.activeSelf || keyGatePanel.activeSelf;

    void TogglePause()
    {
        if (transition.IsPlaying) return;
        if (pausePanel.activeSelf) Resume();
        else if (CanPause)
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    void Resume()
    {
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void OnPauseQuit()
    {
        if (combatPanel.activeSelf) combatManager.AbortCombat();
        Resume();
        GoTo(mainMenuPanel);
    }

    // Chuyển panel qua hiệu ứng Portal — swapAction chạy đúng lúc màn hình bị che kín.
    void GoTo(GameObject panel, System.Action swapAction = null)
    {
        if (transition.IsPlaying) return;
        StartCoroutine(transition.Play(() =>
        {
            ShowOnly(panel);
            swapAction?.Invoke();
        }));
    }

    void ShowOnly(GameObject panel)
    {
        mainMenuPanel.SetActive(panel == mainMenuPanel);
        mapPanel.SetActive(panel == mapPanel);
        combatPanel.SetActive(panel == combatPanel);
        keyGatePanel.SetActive(panel == keyGatePanel);
        endPanel.SetActive(panel == endPanel);
        creditsPanel.SetActive(panel == creditsPanel);
    }

    // Enter/Space bấm nút chính của màn hình chỉ có 1 lựa chọn (Start/Continue/Restart). Esc bật/tắt Pause.
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (Time.timeScale == 0f) return;

        bool confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
        if (!confirm) return;

        if (mainMenuPanel.activeSelf) startButton.onClick.Invoke();
        else if (keyGatePanel.activeSelf) keyGateContinueButton.onClick.Invoke();
        else if (endPanel.activeSelf) restartButton.onClick.Invoke();
        else if (creditsPanel.activeSelf) creditsBackButton.onClick.Invoke();
    }
}
