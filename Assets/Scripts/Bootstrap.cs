using UnityEngine;

/// Sống trong Scene "Splash" — Scene đầu tiên chạy khi mở game. Tạo các object cần
/// tồn tại xuyên suốt (GameSession, LoadingFade — Panel Prefab) rồi tự chuyển sang
/// MainMenu sau khi hiện Splash 1 chút.
public class Bootstrap : MonoBehaviour
{
    public GameDatabase database;
    public float splashDuration = 1.2f;

    void Start()
    {
        SessionBootstrap.Ensure(database);
        Invoke(nameof(GoToMainMenu), splashDuration);
    }

    void GoToMainMenu()
    {
        LoadingFade.Instance.LoadScene("MainMenu");
    }
}
