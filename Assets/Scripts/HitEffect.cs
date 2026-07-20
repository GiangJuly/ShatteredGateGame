using UnityEngine;
using System.Collections;

/// Hiệu ứng nổ tạm thời tại 1 vị trí world-space — phát hết N khung hình rồi tự huỷ.
/// Dùng chung cho đòn đánh thường/mạnh trong Combat và hiệu ứng riêng của Ultimate.
public class HitEffect : MonoBehaviour
{
    public static void Spawn(Sprite[] frames, Vector3 worldPos, float scale = 1.5f, float frameRate = 16f)
    {
        if (frames == null || frames.Length == 0) return;
        var go = new GameObject("HitEffect");
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 10; // vẽ đè lên nhân vật/nền
        var fx = go.AddComponent<HitEffect>();
        fx.StartCoroutine(fx.Play(sr, frames, frameRate));
    }

    IEnumerator Play(SpriteRenderer sr, Sprite[] frames, float frameRate)
    {
        float interval = 1f / frameRate;
        foreach (var frame in frames)
        {
            sr.sprite = frame;
            yield return new WaitForSeconds(interval);
        }
        Destroy(gameObject);
    }
}
