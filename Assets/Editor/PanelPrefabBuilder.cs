using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;
using System.Linq;
using static UIBuilderHelpers;

/// Dựng 12 Panel Prefab riêng biệt (thay vì để thẳng trong scene) — mỗi Panel double-click
/// mở được ở chế độ cô lập (Prefab Isolation Mode) để chỉnh nền/nút mà không bị panel khác
/// chen vào, trong khi runtime vẫn nhẹ (không cần load Scene cho mỗi lần đổi màn hình).
public static class PanelPrefabBuilder
{
    const string Folder = "Assets/Prefabs/UI";

    [MenuItem("ShatteredGate/2 - Build All UI Panel Prefabs")]
    public static void BuildAll()
    {
        Directory.CreateDirectory(Folder);

        BuildMainMenuPanel();
        BuildSettingsPanel();
        BuildCreditsPanel();
        BuildStoryPanel();
        BuildMapPanel();
        BuildCombatHudPanel();
        BuildRewardPanel();
        BuildInventoryPanel();
        BuildPausePanel();
        BuildVictoryPanel();
        BuildGameOverPanel();
        BuildLoadingFadePanel();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PanelPrefabBuilder] Xong! Đã tạo 12 Panel Prefab tại " + Folder);
    }

    static GameObject SavePanel(GameObject root, string fileName)
    {
        string path = $"{Folder}/{fileName}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    static GameObject NewPanelRoot(string name, Color bg)
    {
        var root = new GameObject(name, typeof(RectTransform));
        StretchFull(root.GetComponent<RectTransform>());
        root.AddComponent<Image>().color = bg;
        return root;
    }

    // Gán ảnh nền thật (Battleground/Tileset) lên Image sẵn có của root, kèm lớp phủ tối
    // để chữ/nút vẫn đọc rõ. overlayAlpha: 0-1, càng cao càng tối.
    static void AddBackground(GameObject root, string spritePath, float overlayAlpha)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        var bgImage = root.GetComponent<Image>();
        if (sprite != null)
        {
            bgImage.sprite = sprite;
            bgImage.color = Color.white;
        }
        else
        {
            Debug.LogWarning($"[PanelPrefabBuilder] Không tìm thấy sprite nền: {spritePath}");
        }

        var overlay = CreateUIObject("DarkOverlay", root.transform);
        StretchFull(overlay.GetComponent<RectTransform>());
        overlay.AddComponent<Image>().color = new Color(0f, 0f, 0f, overlayAlpha);
    }

    // ---------------- 1. Main Menu ----------------
    static void BuildMainMenuPanel()
    {
        var root = NewPanelRoot("MainMenuPanel", Color.white);
        AddBackground(root, "Assets/Pixel-Art-Battlegrounds/PNG/Battleground1/Bright/Battleground1.png", 0.5f);

        var title = CreateText(root.transform, "Title", "SHATTERED GATE", 56, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(900, 100));
        title.color = GoldColor;
        title.fontStyle = FontStyle.Bold;

        var subtitle = CreateText(root.transform, "Subtitle", "A Node-Based Roguelite", 20, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(700, 40));
        subtitle.color = new Color(0.7f, 0.7f, 0.75f);

        CreateButton(root.transform, "StartButton", "Start Game",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(220, 46));

        var continueBtn = CreateButton(root.transform, "ContinueButton", "Continue",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -114), new Vector2(220, 46));
        continueBtn.interactable = false;

        var settingsBtn = CreateButton(root.transform, "SettingsButton", "Settings",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -168), new Vector2(220, 46));

        CreateButton(root.transform, "CreditsButton", "Credits",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -222), new Vector2(220, 46));

        CreateButton(root.transform, "QuitButton", "Exit Game",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -276), new Vector2(220, 46));

        SavePanel(root, "MainMenuPanel");
    }

    // ---------------- 2. Settings (khung rỗng) ----------------
    static void BuildSettingsPanel()
    {
        var root = NewPanelRoot("SettingsPanel", new Color(0.03f, 0.02f, 0.05f, 1f));

        var title = CreateText(root.transform, "SettingsTitle", "SETTINGS", 30, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(700, 50));
        title.color = GoldColor;

        CreateText(root.transform, "SettingsBody", "Audio / control options coming soon.", 18, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(600, 100));

        CreateButton(root.transform, "SettingsBackButton", "Back",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(200, 50));

        SavePanel(root, "SettingsPanel");
    }

    // ---------------- 3. Credits ----------------
    static void BuildCreditsPanel()
    {
        var root = NewPanelRoot("CreditsPanel", new Color(0.03f, 0.02f, 0.05f, 1f));

        var title = CreateText(root.transform, "CreditsTitle", "CREDITS", 30, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(700, 50));
        title.color = GoldColor;

        CreateText(root.transform, "CreditsBody",
            "DungeonTileset II — 0x72 (CC0)\n" +
            "Pixel-Art Battlegrounds — CraftPix.net (Free License)\n" +
            "RPG Sound Pack — artisticdude (CC0)\n" +
            "Short Music Jingles — Kenney.nl (CC0)\n" +
            "Fantasy-UI, Portal VFX — xem license kèm theo trong Assets/",
            17, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(760, 220));

        CreateButton(root.transform, "CreditsBackButton", "Back",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(200, 50));

        SavePanel(root, "CreditsPanel");
    }

    // ---------------- 4. Story (dùng chung: mở đầu / lore tuyển đồng đội / lời dẫn Final Boss) ----------------
    static void BuildStoryPanel()
    {
        var root = NewPanelRoot("StoryPanel", Color.white);
        AddBackground(root, "Assets/Pixel-Art-Battlegrounds/PNG/Battleground3/Bright/Battleground3.png", 0.6f);

        var title = CreateText(root.transform, "StoryTitle", "SHATTERED GATE", 30, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(700, 50));
        title.color = GoldColor;

        CreateText(root.transform, "StoryBody", "", 19, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(760, 180));

        CreateButton(root.transform, "StoryContinueButton", "Continue",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(220, 56));

        SavePanel(root, "StoryPanel");
    }

    // ---------------- 5. Gate Room (Map) ----------------
    // Phòng hình tròn lơ lửng giữa các mảnh vỡ: sàn dungeon mờ dưới lớp phủ tối gần như void,
    // 1 cổng vòm (từ chính sprite-sheet Portal đang dùng cho hiệu ứng chuyển cảnh) làm tâm điểm,
    // tối đa 5 cổng toả quanh tâm — vị trí thật sự do MapManager tính lại theo số cổng còn lại
    // của từng chặng (xem MapManager.PositionSlot), ở đây chỉ đặt vị trí mặc định cho dễ nhìn
    // khi mở Prefab Isolation Mode.
    static void BuildMapPanel()
    {
        var root = NewPanelRoot("MapPanel", BgColor);

        var floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/DungeonTileset/atlas_floor-16x16.png")
            ?? AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Environment/DungeonTileset/atlas_floor-16x16.png").OfType<Sprite>().FirstOrDefault();
        var mapBg = root.GetComponent<Image>();
        if (floorSprite != null)
        {
            mapBg.sprite = floorSprite;
            mapBg.type = Image.Type.Tiled;
            mapBg.pixelsPerUnitMultiplier = 0.5f;
            mapBg.color = new Color(0.35f, 0.32f, 0.4f, 1f);
        }
        var mapOverlay = CreateUIObject("DarkOverlay", root.transform);
        StretchFull(mapOverlay.GetComponent<RectTransform>());
        mapOverlay.AddComponent<Image>().color = new Color(0.02f, 0.015f, 0.04f, 0.85f); // gần như void giữa các mảnh vỡ

        var portalFrame = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/VFX/Portal/sprite-sheet.png")
            .OfType<Sprite>()
            .Select(s => new { sprite = s, index = ParseTrailingIndex(s.name) })
            .Where(x => x.index >= 0)
            .OrderBy(x => x.index)
            .Select(x => x.sprite)
            .FirstOrDefault();
        if (portalFrame != null)
        {
            var archway = CreateUIObject("GateArchway", root.transform);
            var arRt = archway.GetComponent<RectTransform>();
            arRt.anchorMin = new Vector2(0.5f, 0.5f);
            arRt.anchorMax = new Vector2(0.5f, 0.5f);
            arRt.anchoredPosition = new Vector2(0, -10);
            arRt.sizeDelta = new Vector2(240, 240);
            var arImg = archway.AddComponent<Image>();
            arImg.sprite = portalFrame;
            arImg.preserveAspect = true;
            arImg.color = new Color(1f, 1f, 1f, 0.4f); // mờ — chỉ làm tâm điểm khí quyển, không che cổng
        }

        const int maxGates = 5;
        for (int i = 0; i < maxGates; i++)
        {
            float angleDeg = 90f - (360f / maxGates) * i;
            float rad = angleDeg * Mathf.Deg2Rad;
            var pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad) * 0.6f) * 220f + new Vector2(0, -10f);
            var (btn, _) = CreateGateDiamond(root.transform, pos);
            btn.gameObject.name = $"Gate{i}"; // tên riêng biệt để tìm lại sau khi Prefab hoá
        }

        SavePanel(root, "MapPanel");
    }

    // ---------------- 6. Combat HUD ----------------
    static void BuildCombatHudPanel()
    {
        // Không nền riêng — để lộ world phía sau (background + nhân vật world-space).
        var root = new GameObject("CombatPanel", typeof(RectTransform));
        StretchFull(root.GetComponent<RectTransform>());

        for (int i = 0; i < 4; i++)
            CreateHealthBarSlot(root.transform, $"PartyHUD{i}",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(160, 40 + i * 46), new Vector2(220, 38));

        // Cố định ở góc trên-phải màn hình, giống nhau cho mọi trận (không phụ thuộc vị trí world của
        // sprite) — tối đa 3 slot cho trận bầy nhiều Enemy, xếp dọc xuống dưới. Mỗi slot có Button để
        // người chơi click chọn mục tiêu cho đòn tiếp theo (mặc định nhắm Enemy còn sống đầu tiên).
        for (int i = 0; i < 3; i++)
        {
            var (fill, _, _, _) = CreateHealthBarSlot(root.transform, $"EnemyHUD{i}",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150, -50 - i * 46), new Vector2(260, 38));
            fill.transform.parent.gameObject.AddComponent<Button>().targetGraphic = fill.transform.parent.GetComponent<Image>();
        }

        CreateText(root.transform, "LogText", "", 20, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 118), new Vector2(900, 50));

        var apText = CreateText(root.transform, "APText", "", 18, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 72), new Vector2(300, 20));
        apText.color = GoldColor;

        float[] skillX = { -300f, -100f, 100f, 300f };
        for (int i = 0; i < 4; i++)
            CreateButton(root.transform, $"SkillButton{i}", "Skill",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(skillX[i], 35), new Vector2(180, 56));

        SavePanel(root, "CombatHUDPanel");
    }

    // ---------------- 7. Reward (khung rỗng) ----------------
    static void BuildRewardPanel()
    {
        var root = NewPanelRoot("RewardPanel", new Color(0.03f, 0.02f, 0.05f, 1f));

        var title = CreateText(root.transform, "RewardTitle", "SPOILS", 30, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(700, 50));
        title.color = GoldColor;

        CreateText(root.transform, "RewardBody", "You found nothing of value here... yet.", 18, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(600, 80));

        CreateButton(root.transform, "RewardContinueButton", "Continue",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -100), new Vector2(220, 56));

        SavePanel(root, "RewardPanel");
    }

    // ---------------- 8. Inventory (khung rỗng) ----------------
    static void BuildInventoryPanel()
    {
        var root = NewPanelRoot("InventoryPanel", new Color(0.03f, 0.02f, 0.05f, 1f));

        var title = CreateText(root.transform, "InventoryTitle", "INVENTORY", 30, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(700, 50));
        title.color = GoldColor;

        CreateText(root.transform, "InventoryBody", "Empty.", 18, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(600, 80));

        CreateButton(root.transform, "InventoryBackButton", "Back",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(200, 50));

        SavePanel(root, "InventoryPanel");
    }

    // ---------------- 9. Pause ----------------
    static void BuildPausePanel()
    {
        var root = NewPanelRoot("PausePanel", new Color(0.02f, 0.015f, 0.03f, 0.9f));

        var title = CreateText(root.transform, "PauseTitle", "PAUSED", 34, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(500, 50));
        title.color = GoldColor;
        title.fontStyle = FontStyle.Bold;

        CreateButton(root.transform, "ResumeButton", "Resume",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(220, 50));

        CreateButton(root.transform, "PauseInventoryButton", "Inventory",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(220, 50));

        var settingsBtn = CreateButton(root.transform, "PauseSettingsButton", "Settings",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(220, 50));
        settingsBtn.interactable = false;

        CreateButton(root.transform, "PauseQuitButton", "Quit to Menu",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -100), new Vector2(220, 50));

        SavePanel(root, "PausePanel");
    }

    // ---------------- 10. Victory ----------------
    static void BuildVictoryPanel()
    {
        var root = NewPanelRoot("VictoryPanel", Color.white);
        AddBackground(root, "Assets/Pixel-Art-Battlegrounds/PNG/Battleground2/Bright/Battleground2.png", 0.45f);

        var title = CreateText(root.transform, "VictoryTitle", "VICTORY!", 46, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(800, 70));
        title.color = GoldColor;
        title.fontStyle = FontStyle.Bold;

        CreateText(root.transform, "VictoryBody", "", 18, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(700, 100));

        CreateButton(root.transform, "VictoryBackButton", "Back to Menu",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(240, 56));

        SavePanel(root, "VictoryPanel");
    }

    // ---------------- 11. Game Over ----------------
    static void BuildGameOverPanel()
    {
        var root = NewPanelRoot("GameOverPanel", Color.white);
        AddBackground(root, "Assets/Pixel-Art-Battlegrounds/PNG/Battleground4/Bright/Battleground4.png", 0.55f);

        // Lớp nhiễu glitch mờ phủ lên trên nền — gợi "thực tại vỡ vụn" lúc thất bại.
        var glitchSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/ROBOXEL-Glitchy_Backgrounds/ROBOXEL-Glitchy_Backgrounds/PNG/Glitchy_Backgrounds-042.png");
        if (glitchSprite != null)
        {
            var glitch = CreateUIObject("GlitchOverlay", root.transform);
            StretchFull(glitch.GetComponent<RectTransform>());
            var glitchImg = glitch.AddComponent<Image>();
            glitchImg.sprite = glitchSprite;
            glitchImg.color = new Color(1f, 1f, 1f, 0.22f);
        }

        var title = CreateText(root.transform, "GameOverTitle", "DEFEAT", 46, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(800, 70));
        title.color = GoldColor;
        title.fontStyle = FontStyle.Bold;

        CreateText(root.transform, "GameOverBody", "The Shattered Gate claims another wanderer.", 18, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(700, 60));

        CreateButton(root.transform, "GameOverRestartButton", "Back to Menu",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-125, -100), new Vector2(220, 56));

        CreateButton(root.transform, "GameOverQuitButton", "Exit Game",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(125, -100), new Vector2(220, 56));

        SavePanel(root, "GameOverPanel");
    }

    // ---------------- 12. Loading/Fade (persistent, có Canvas riêng) ----------------
    static void BuildLoadingFadePanel()
    {
        var root = new GameObject("LoadingFade");

        var canvasGo = new GameObject("Canvas");
        canvasGo.transform.SetParent(root.transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // luôn vẽ đè lên mọi Canvas khác trong scene
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var overlayRoot = CreateUIObject("Overlay", canvasGo.transform);
        StretchFull(overlayRoot.GetComponent<RectTransform>());
        var fadeImg = overlayRoot.AddComponent<Image>();
        fadeImg.color = new Color(0f, 0f, 0f, 0f);

        var portalGo = CreateUIObject("PortalImage", overlayRoot.transform);
        var portalRt = portalGo.GetComponent<RectTransform>();
        portalRt.anchorMin = new Vector2(0.5f, 0.5f);
        portalRt.anchorMax = new Vector2(0.5f, 0.5f);
        portalRt.anchoredPosition = Vector2.zero;
        portalRt.sizeDelta = new Vector2(150, 150);
        var portalImg = portalGo.AddComponent<Image>();
        portalImg.preserveAspect = true;
        portalImg.raycastTarget = false;
        portalImg.color = new Color(1f, 1f, 1f, 0f);

        overlayRoot.SetActive(false);

        var fade = root.AddComponent<LoadingFade>();
        fade.overlayRoot = overlayRoot;
        fade.fadeImage = fadeImg;
        fade.portalImage = portalImg;

        SavePanel(root, "LoadingFadePrefab");
    }
}
