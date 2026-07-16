using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public float duration = 0.15f;
    public float magnitude = 0.15f;

    public void Shake()
    {
        StopAllCoroutines();
        StartCoroutine(DoShake());
    }

    IEnumerator DoShake()
    {
        Vector3 original = transform.localPosition;
        float t = 0f;
        while (t < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            transform.localPosition = original + new Vector3(x, y, 0);
            t += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = original;
    }
}
