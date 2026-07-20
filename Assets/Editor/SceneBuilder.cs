using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using static UIBuilderHelpers;

/// Dựng 4 Scene (Splash/MainMenu/Game/Boss), mỗi Scene ghép các Panel Prefab (đã tạo bởi
/// PanelPrefabBuilder) vào Canvas riêng và wire Controller tương ứng.
/// Thứ tự bắt buộc: Setup Hero Prefabs + Tier A/B/C/D + Convert Sprites (đã có từ trước)
/// -> "2 - Build All UI Panel Prefabs" -> "3 - Build Game Database" -> "4 - Build All Scenes".
public static class SceneBuilder
{
    const string PanelFolder = "Assets/Prefabs/UI";
    const string DbPath = "Assets/Data/GameDatabase.asset";

    [MenuItem("ShatteredGate/4 - Build All Scenes")]
    public static void BuildAllScenes()
    {
        var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(DbPath);
        if (db == null)
        {
            Debug.LogError("[SceneBuilder] Chưa có GameDatabase — chạy 'ShatteredGate/3 - Build Game Database' trước.");
            return;
        }

        Directory.CreateDirectory("Assets/Scenes");
        BuildSplashScene(db);
        BuildMainMenuScene(db);
        BuildGameScene(db);
        BuildBossScene(db);

        var scenesList = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Splash.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Game.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Boss.unity", true),
        };
        EditorBuildSettings.scenes = scenesList;

        EditorSceneManager.OpenScene("Assets/Scenes/Splash.unity");
        Debug.Log("[SceneBuilder] Xong! Đã dựng đủ 4 scene (Splash/MainMenu/Game/Boss). Mở Splash và bấm Play để test từ đầu.");
    }

    // ================= SPLASH =================
    static void BuildSplashScene(GameDatabase db)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.backgroundColor = BgColor;
        camGo.tag = "MainCamera";

        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var bg = CreateUIObject("Background", canvasGo.transform);
        StretchFull(bg.GetComponent<RectTransform>());
        bg.AddComponent<Image>().color = BgColor;

        var logo = CreateText(canvasGo.transform, "Logo", "SHATTERED GATE", 44, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 100));
        logo.color = GoldColor;
        logo.fontStyle = FontStyle.Bold;

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        var bootGo = new GameObject("Bootstrap");
        var boot = bootGo.AddComponent<Bootstrap>();
        boot.database = db;
        if (db.loadingFadePrefab == null)
            Debug.LogError("[SceneBuilder] GameDatabase thiếu loadingFadePrefab — chạy lại 'Build Game Database'.");

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Splash.unity");
    }

    // ================= MAIN MENU =================
    static void BuildMainMenuScene(GameDatabase db)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var canvasGo = SetupCameraCanvasEventSystem(BgColor);

        var menu = InstantiatePanel("MainMenuPanel", canvasGo.transform);
        var settings = InstantiatePanel("SettingsPanel", canvasGo.transform);
        var credits = InstantiatePanel("CreditsPanel", canvasGo.transform);

        var ctrlGo = new GameObject("MainMenuController");
        var ctrl = ctrlGo.AddComponent<MainMenuController>();
        ctrl.database = db;
        ctrl.mainMenuPanel = menu;
        ctrl.settingsPanel = settings;
        ctrl.creditsPanel = credits;
        ctrl.startButton = Find<Button>(menu, "StartButton");
        ctrl.settingsButton = Find<Button>(menu, "SettingsButton");
        ctrl.settingsBackButton = Find<Button>(settings, "SettingsBackButton");
        ctrl.creditsButton = Find<Button>(menu, "CreditsButton");
        ctrl.creditsBackButton = Find<Button>(credits, "CreditsBackButton");
        ctrl.quitButton = Find<Button>(menu, "QuitButton");

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
    }

    // ================= GAME =================
    static void BuildGameScene(GameDatabase db)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var (canvasGo, shake, sfxSource, combatBgRenderer, partySpawns, enemySpawns) = SetupCombatWorld();

        var story = InstantiatePanel("StoryPanel", canvasGo.transform);
        var map = InstantiatePanel("MapPanel", canvasGo.transform);
        var combat = InstantiatePanel("CombatHUDPanel", canvasGo.transform);
        var reward = InstantiatePanel("RewardPanel", canvasGo.transform);
        var inventory = InstantiatePanel("InventoryPanel", canvasGo.transform);
        var pause = InstantiatePanel("PausePanel", canvasGo.transform);
        var gameOver = InstantiatePanel("GameOverPanel", canvasGo.transform);

        var mapMgrGo = new GameObject("MapManager");
        var mapMgr = mapMgrGo.AddComponent<MapManager>();
        WireMapManager(mapMgr, map);

        var combatMgrGo = new GameObject("CombatManager");
        var combatMgr = combatMgrGo.AddComponent<CombatManager>();
        WireCombatManager(combatMgr, combat, partySpawns, enemySpawns, combatBgRenderer, shake, sfxSource);

        var ctrlGo = new GameObject("GameSceneController");
        var ctrl = ctrlGo.AddComponent<GameSceneController>();
        ctrl.database = db;
        ctrl.storyPanel = story;
        ctrl.mapPanel = map;
        ctrl.combatPanel = combat;
        ctrl.rewardPanel = reward;
        ctrl.inventoryPanel = inventory;
        ctrl.pausePanel = pause;
        ctrl.gameOverPanel = gameOver;
        ctrl.storyTitleText = Find<Text>(story, "StoryTitle");
        ctrl.storyBodyText = Find<Text>(story, "StoryBody");
        ctrl.storyContinueButton = Find<Button>(story, "StoryContinueButton");
        ctrl.rewardBodyText = Find<Text>(reward, "RewardBody");
        ctrl.rewardContinueButton = Find<Button>(reward, "RewardContinueButton");
        ctrl.pauseResumeButton = Find<Button>(pause, "ResumeButton");
        ctrl.pauseInventoryButton = Find<Button>(pause, "PauseInventoryButton");
        ctrl.pauseSettingsButton = Find<Button>(pause, "PauseSettingsButton");
        ctrl.pauseQuitButton = Find<Button>(pause, "PauseQuitButton");
        ctrl.inventoryBackButton = Find<Button>(inventory, "InventoryBackButton");
        ctrl.gameOverRestartButton = Find<Button>(gameOver, "GameOverRestartButton");
        ctrl.gameOverQuitButton = Find<Button>(gameOver, "GameOverQuitButton");
        ctrl.mapManager = mapMgr;
        ctrl.combatManager = combatMgr;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Game.unity");
    }

    // ================= BOSS =================
    static void BuildBossScene(GameDatabase db)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var (canvasGo, shake, sfxSource, combatBgRenderer, partySpawns, enemySpawns) = SetupCombatWorld();

        var story = InstantiatePanel("StoryPanel", canvasGo.transform);
        var combat = InstantiatePanel("CombatHUDPanel", canvasGo.transform);
        var pause = InstantiatePanel("PausePanel", canvasGo.transform);
        var victory = InstantiatePanel("VictoryPanel", canvasGo.transform);
        var gameOver = InstantiatePanel("GameOverPanel", canvasGo.transform);

        var combatMgrGo = new GameObject("CombatManager");
        var combatMgr = combatMgrGo.AddComponent<CombatManager>();
        WireCombatManager(combatMgr, combat, partySpawns, enemySpawns, combatBgRenderer, shake, sfxSource);

        var ctrlGo = new GameObject("BossSceneController");
        var ctrl = ctrlGo.AddComponent<BossSceneController>();
        ctrl.database = db;
        ctrl.storyPanel = story;
        ctrl.combatPanel = combat;
        ctrl.pausePanel = pause;
        ctrl.victoryPanel = victory;
        ctrl.gameOverPanel = gameOver;
        ctrl.storyTitleText = Find<Text>(story, "StoryTitle");
        ctrl.storyBodyText = Find<Text>(story, "StoryBody");
        ctrl.storyContinueButton = Find<Button>(story, "StoryContinueButton");
        ctrl.victoryBodyText = Find<Text>(victory, "VictoryBody");
        ctrl.victoryBackButton = Find<Button>(victory, "VictoryBackButton");
        ctrl.pauseResumeButton = Find<Button>(pause, "ResumeButton");
        ctrl.pauseSettingsButton = Find<Button>(pause, "PauseSettingsButton");
        ctrl.pauseQuitButton = Find<Button>(pause, "PauseQuitButton");
        ctrl.gameOverRestartButton = Find<Button>(gameOver, "GameOverRestartButton");
        ctrl.gameOverQuitButton = Find<Button>(gameOver, "GameOverQuitButton");
        ctrl.combatManager = combatMgr;

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Boss.unity");
    }

    // ================= Helpers dùng chung cho Game/Boss (world combat + camera) =================
    static (GameObject canvasGo, CameraShake shake, AudioSource sfxSource, SpriteRenderer combatBg, Transform[] partySpawns, Transform[] enemySpawns)
        SetupCombatWorld()
    {
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 4f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
        camGo.tag = "MainCamera";
        var shake = camGo.AddComponent<CameraShake>();
        var sfxSource = camGo.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;

        BuildDungeonTilemap();

        var combatBgGo = new GameObject("CombatBackground");
        combatBgGo.transform.position = new Vector3(0, 0, 3f);
        var combatBgRenderer = combatBgGo.AddComponent<SpriteRenderer>();
        combatBgRenderer.sortingOrder = -1;

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var spawnRoot = new GameObject("SpawnPoints");
        var partySpawns = new Transform[4];
        float[] xs = { -4f, -2.3f, -0.6f, 1.1f };
        for (int i = 0; i < 4; i++)
        {
            var sp = new GameObject($"PartySpawn{i}");
            sp.transform.SetParent(spawnRoot.transform);
            sp.transform.position = new Vector3(xs[i], -1f, 0);
            partySpawns[i] = sp.transform;
        }
        // Tối đa 3 Enemy/trận (bầy quái nhỏ tăng độ khó) — dàn hàng ngang, unitScale lớn hơn trước
        // nên giãn cách rộng hơn slot cũ để không đè lên nhau khi cả 3 cùng hiện.
        var enemySpawns = new Transform[3];
        float[] enemyXs = { 2.0f, 3.4f, 4.8f };
        for (int i = 0; i < 3; i++)
        {
            var sp = new GameObject($"EnemySpawn{i}");
            sp.transform.SetParent(spawnRoot.transform);
            sp.transform.position = new Vector3(enemyXs[i], 0.3f, 0);
            enemySpawns[i] = sp.transform;
        }

        return (canvasGo, shake, sfxSource, combatBgRenderer, partySpawns, enemySpawns);
    }

    static GameObject SetupCameraCanvasEventSystem(Color camBg)
    {
        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.backgroundColor = camBg;
        camGo.tag = "MainCamera";

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        return canvasGo;
    }

    static void WireMapManager(MapManager mapMgr, GameObject mapPanel)
    {
        var buttons = new Button[5];
        var diamonds = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            var gateRoot = mapPanel.transform.Find($"Gate{i}");
            buttons[i] = gateRoot.GetComponent<Button>();
            diamonds[i] = gateRoot.GetComponent<Image>();
        }
        mapMgr.gateButtons = buttons;
        mapMgr.gateDiamonds = diamonds;
    }

    static void WireCombatManager(CombatManager combatMgr, GameObject combatPanel, Transform[] partySpawns, Transform[] enemySpawns,
        SpriteRenderer combatBg, CameraShake shake, AudioSource sfxSource)
    {
        combatMgr.partySpawnPoints = partySpawns;
        combatMgr.enemySpawnPoints = enemySpawns;
        combatMgr.backgroundRenderer = combatBg;
        combatMgr.cameraShake = shake;
        combatMgr.sfxSource = sfxSource;
        combatMgr.logText = Find<Text>(combatPanel, "LogText");
        combatMgr.apText = Find<Text>(combatPanel, "APText");

        var skillButtons = new Button[4];
        for (int i = 0; i < 4; i++)
            skillButtons[i] = Find<Button>(combatPanel, $"SkillButton{i}");
        combatMgr.skillButtons = skillButtons;

        var partyNameTexts = new Text[4];
        var partyHpFills = new Image[4];
        var partyPortraits = new Image[4];
        var partyTagTexts = new Text[4];
        for (int i = 0; i < 4; i++)
        {
            var slot = combatPanel.transform.Find($"PartyHUD{i}");
            partyHpFills[i] = slot.Find("Fill").GetComponent<Image>();
            partyNameTexts[i] = slot.Find("Label").GetComponent<Text>();
            partyPortraits[i] = slot.Find("Portrait/Icon").GetComponent<Image>();
            partyTagTexts[i] = slot.Find("TagText").GetComponent<Text>();
        }
        combatMgr.partyNameText = partyNameTexts;
        combatMgr.partyHpFill = partyHpFills;
        combatMgr.partyPortrait = partyPortraits;
        combatMgr.partyTagText = partyTagTexts;

        var enemyNameTexts = new Text[3];
        var enemyHpFills = new Image[3];
        var enemyPortraits = new Image[3];
        var enemyTagTexts = new Text[3];
        var enemyTargetButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            var slot = combatPanel.transform.Find($"EnemyHUD{i}");
            enemyHpFills[i] = slot.Find("Fill").GetComponent<Image>();
            enemyNameTexts[i] = slot.Find("Label").GetComponent<Text>();
            enemyPortraits[i] = slot.Find("Portrait/Icon").GetComponent<Image>();
            enemyTagTexts[i] = slot.Find("TagText").GetComponent<Text>();
            enemyTargetButtons[i] = slot.GetComponent<Button>();
        }
        combatMgr.enemyNameText = enemyNameTexts;
        combatMgr.enemyHpFill = enemyHpFills;
        combatMgr.enemyPortrait = enemyPortraits;
        combatMgr.enemyTagText = enemyTagTexts;
        combatMgr.enemyTargetButtons = enemyTargetButtons;
    }

    static GameObject InstantiatePanel(string prefabName, Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PanelFolder}/{prefabName}.prefab");
        if (prefab == null)
        {
            Debug.LogError($"[SceneBuilder] Thiếu panel prefab: {prefabName} — chạy 'Build All UI Panel Prefabs' trước.");
            return null;
        }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        return instance;
    }

    static T Find<T>(GameObject root, string path) where T : Component
    {
        var t = root.transform.Find(path);
        if (t == null)
        {
            Debug.LogError($"[SceneBuilder] Không tìm thấy '{path}' trong '{root.name}'");
            return null;
        }
        var comp = t.GetComponent<T>();
        if (comp == null)
            Debug.LogError($"[SceneBuilder] '{path}' trong '{root.name}' không có component {typeof(T).Name}");
        return comp;
    }
}
