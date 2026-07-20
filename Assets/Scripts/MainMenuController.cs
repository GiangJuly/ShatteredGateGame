using UnityEngine;
using UnityEngine.UI;

/// Điều phối Scene "MainMenu": 3 panel (MainMenu/Settings/Credits) đổi qua lại bằng
/// bật/tắt — panel nhẹ, đổi liên tục trong màn Menu nên không cần tách Scene riêng.
public class MainMenuController : MonoBehaviour
{
    public GameDatabase database; // dự phòng: tự tạo GameSession/LoadingFade nếu mở thẳng scene này khi test

    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;

    public Button startButton;
    public Button settingsButton;
    public Button settingsBackButton;
    public Button creditsButton;
    public Button creditsBackButton;
    public Button quitButton;

    void Start()
    {
        SessionBootstrap.Ensure(database);

        startButton.onClick.AddListener(OnStart);
        if (settingsButton != null) settingsButton.onClick.AddListener(() => GoTo(settingsPanel));
        if (settingsBackButton != null) settingsBackButton.onClick.AddListener(() => GoTo(mainMenuPanel));
        creditsButton.onClick.AddListener(() => GoTo(creditsPanel));
        creditsBackButton.onClick.AddListener(() => GoTo(mainMenuPanel));
        quitButton.onClick.AddListener(OnQuit);

        ShowOnly(mainMenuPanel); // vừa vào Scene qua Portal rồi, khỏi lặp hiệu ứng lần nữa
    }

    void OnStart()
    {
        GameSession.Instance.StartNewRun();
        LoadingFade.Instance.LoadScene("Game");
    }

    void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void GoTo(GameObject panel)
    {
        LoadingFade.Instance.SwapPanel(() => ShowOnly(panel));
    }

    void ShowOnly(GameObject panel)
    {
        mainMenuPanel.SetActive(panel == mainMenuPanel);
        if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
        creditsPanel.SetActive(panel == creditsPanel);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
        {
            if (mainMenuPanel.activeSelf) startButton.onClick.Invoke();
            else if (creditsPanel.activeSelf) creditsBackButton.onClick.Invoke();
            else if (settingsPanel != null && settingsPanel.activeSelf) settingsBackButton.onClick.Invoke();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (creditsPanel.activeSelf) creditsBackButton.onClick.Invoke();
            else if (settingsPanel != null && settingsPanel.activeSelf) settingsBackButton.onClick.Invoke();
        }
    }
}
