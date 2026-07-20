using UnityEngine;

/// Đảm bảo GameSession + LoadingFade tồn tại trước khi dùng — gọi đầu Start() của mọi
/// Controller. Cần thiết vì lúc test riêng 1 Scene (mở thẳng MainMenu/Game/Boss trong
/// Editor, bỏ qua Splash) thì Bootstrap chưa từng chạy, GameSession.Instance sẽ null.
public static class SessionBootstrap
{
    public static void Ensure(GameDatabase database)
    {
        if (GameSession.Instance == null)
        {
            var session = new GameObject("GameSession").AddComponent<GameSession>();
            session.database = database;
        }
        if (LoadingFade.Instance == null && database.loadingFadePrefab != null)
        {
            Object.Instantiate(database.loadingFadePrefab);
        }
    }
}
