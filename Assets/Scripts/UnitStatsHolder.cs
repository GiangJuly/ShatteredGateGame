using UnityEngine;

/// Chỉ số chiến đấu gắn trên mỗi Prefab Hero/Enemy. Chỉnh trực tiếp trong
/// Inspector của từng prefab để cân bằng game sau này.
public class UnitStatsHolder : MonoBehaviour
{
    public string unitName = "Unit";
    public int maxHP = 20;
    public int attackPower = 5;
    public int speed = 5;
    public bool isPlayerUnit;
}
