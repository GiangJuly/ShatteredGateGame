using UnityEngine;

/// Nhấp nhô nhẹ lên xuống liên tục cho Hero/Enemy trong Combat — chỉ để tạo cảm giác
/// sống động, không ảnh hưởng logic chiến đấu. Mỗi unit lệch pha ngẫu nhiên để không
/// nhấp nhô đồng loạt trông cứng.
public class CombatBob : MonoBehaviour
{
    public float amplitude = 0.08f;
    public float speed = 2f;

    Vector3 basePos;
    float phase;

    void Start()
    {
        basePos = transform.localPosition;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * speed + phase) * amplitude;
    }
}
