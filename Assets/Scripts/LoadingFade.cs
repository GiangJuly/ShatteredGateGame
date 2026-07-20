using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// Panel "Loading/Fade" — sống xuyên suốt (DontDestroyOnLoad) vì phải che màn hình
/// trong lúc Scene mới đang load ở nền. Dùng animation Portal có sẵn làm hiệu ứng
/// "xuyên Cổng Không Gian" khi đổi Scene, khớp cốt truyện.
public class LoadingFade : MonoBehaviour
{
    public static LoadingFade Instance { get; private set; }

    public GameObject overlayRoot;
    public Image fadeImage;
    public Image portalImage;
    public Sprite[] portalFrames;

    const float PhaseDuration = 0.32f;
    const float PortalFrameInterval = 1f / 12f;
    const float MaxPortalScale = 16f;

    public bool IsPlaying { get; private set; }

    float frameTimer;
    int frameIndex;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// Đổi Scene thật (Splash/MainMenu/Game/Boss) qua hiệu ứng Portal che kín màn hình.
    public void LoadScene(string sceneName)
    {
        if (IsPlaying) return;
        StartCoroutine(LoadRoutine(sceneName));
    }

    /// Đổi panel trong cùng 1 Scene (VD Map -> Combat trong scene Game) — swapAction chạy
    /// đúng lúc màn hình bị Portal che kín.
    /// Tự StartCoroutine trên chính LoadingFade (object sống xuyên suốt) thay vì bắt Controller
    /// gọi ngoài — nếu không, coroutine bị gắn vào Controller của Scene đang hoạt động, và nếu
    /// Controller đó bị huỷ giữa chừng thì cờ IsPlaying kẹt ở true mãi mãi, chặn luôn mọi hiệu
    /// ứng Portal về sau.
    public void SwapPanel(System.Action swapAction)
    {
        if (IsPlaying) return;
        StartCoroutine(SwapPanelRoutine(swapAction));
    }

    IEnumerator SwapPanelRoutine(System.Action swapAction)
    {
        IsPlaying = true;
        overlayRoot.SetActive(true);
        frameTimer = 0f; frameIndex = 0;

        yield return Phase(growing: true);
        swapAction?.Invoke();
        yield return Phase(growing: false);

        overlayRoot.SetActive(false);
        IsPlaying = false;
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        IsPlaying = true;
        overlayRoot.SetActive(true);
        frameTimer = 0f; frameIndex = 0;

        yield return Phase(growing: true);

        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;

        yield return Phase(growing: false);

        overlayRoot.SetActive(false);
        IsPlaying = false;
    }

    IEnumerator Phase(bool growing)
    {
        float t = 0f;
        while (t < PhaseDuration)
        {
            t += Time.unscaledDeltaTime;
            frameTimer += Time.unscaledDeltaTime;
            if (portalFrames != null && portalFrames.Length > 0 && frameTimer >= PortalFrameInterval)
            {
                frameTimer = 0f;
                frameIndex = (frameIndex + 1) % portalFrames.Length;
                portalImage.sprite = portalFrames[frameIndex];
            }

            float p = growing ? (t / PhaseDuration) : (1f - t / PhaseDuration);
            fadeImage.color = new Color(0f, 0f, 0f, p);
            portalImage.color = new Color(1f, 1f, 1f, p);
            portalImage.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, MaxPortalScale, p * p);
            yield return null;
        }
    }
}
