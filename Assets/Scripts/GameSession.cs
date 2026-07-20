using UnityEngine;
using System.Collections.Generic;

/// Trạng thái sống xuyên suốt các lần đổi Scene thật (DontDestroyOnLoad) — chỉ giữ đúng
/// những gì cần tồn tại QUA lúc chuyển Scene (Game -> Boss -> MainMenu): đội hình đã
/// tuyển. Tiến trình Map/chặng KHÔNG cần ở đây vì MapManager sống suốt trong 1 lần
/// Scene "Game" còn tải (không bị huỷ giữa các chặng).
public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public GameDatabase database;

    readonly List<GameObject> activeParty = new List<GameObject>();

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

    public void StartNewRun()
    {
        activeParty.Clear();
        activeParty.Add(database.mainHeroPrefab);
    }

    public void RecruitHero(GameObject heroPrefab) => activeParty.Add(heroPrefab);

    public GameObject[] ActiveParty => activeParty.ToArray();
}
