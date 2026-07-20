using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// Hiệu ứng chuyển cảnh dạng "xuyên Cổng Không Gian" — dùng animation Portal có sẵn
/// (Frostwindz Animated Portal) che màn hình trong lúc GameFlow đổi panel, khớp cốt truyện
/// "xuyên các cổng không gian giữa những chiều bị phân mảnh".
public class SceneTransition : MonoBehaviour
{
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

    public IEnumerator Play(System.Action swapAction)
    {
        IsPlaying = true;
        overlayRoot.SetActive(true);
        frameTimer = 0f;
        frameIndex = 0;

        yield return Phase(growing: true);
        swapAction?.Invoke();
        yield return Phase(growing: false);

        overlayRoot.SetActive(false);
        IsPlaying = false;
    }

    IEnumerator Phase(bool growing)
    {
        float t = 0f;
        while (t < PhaseDuration)
        {
            t += Time.deltaTime;
            frameTimer += Time.deltaTime;
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
