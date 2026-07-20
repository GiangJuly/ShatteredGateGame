using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// Dựng toàn bộ scene "Main" (Menu -> Map 5+1 chặng -> Combat -> KeyGate -> End) bằng code.
/// Yêu cầu đã chạy "Setup Hero Prefabs" và các Tier A/B/C/D trước đó.
public static class SceneBuilder
{
    static readonly Color BgColor = new Color(0.05f, 0.035f, 0.07f, 1f);
    static readonly Color ButtonColor = new Color(0.22f, 0.16f, 0.30f, 0.95f);
    static readonly Color GoldColor = new Color(0.85f, 0.72f, 0.42f, 1f);

    [MenuItem("ShatteredGate/Build Vertical Slice Scene")]
    static void BuildScene()
    {
        GameObject Load(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) Debug.LogError($"[SceneBuilder] Thiếu prefab: {path}");
            return go;
        }

        var graham = Load("Assets/Prefabs/Heroes/Graham.prefab");
        var sally = Load("Assets/Prefabs/Heroes/Sally.prefab");
        var violet = Load("Assets/Prefabs/Heroes/Violet.prefab");
        var james = Load("Assets/Prefabs/Heroes/James.prefab");

        var slime = Load("Assets/Prefabs/Enemies/TierA/Slime.prefab");
        var bat = Load("Assets/Prefabs/Enemies/TierA/Bat.prefab");
        var defencer = Load("Assets/Prefabs/Enemies/TierB/Defencer.prefab");
        var zombie = Load("Assets/Prefabs/Enemies/TierB/Zombie.prefab");
        var genie = Load("Assets/Prefabs/Enemies/TierC/Genie.prefab");
        var treant = Load("Assets/Prefabs/Enemies/TierD/Treant.prefab");
        var asimole = Load("Assets/Prefabs/Enemies/TierD/Asimole.prefab");
        var creeps = Load("Assets/Prefabs/Enemies/TierD/Creeps.prefab");
        var demonpot = Load("Assets/Prefabs/Enemies/TierD/Demonpot.prefab");
        var mechasphere = Load("Assets/Prefabs/Enemies/TierD/Mechasphere.prefab");
        var zero = Load("Assets/Prefabs/Enemies/TierD/Zero.prefab");

        var floorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/DungeonTileset/atlas_floor-16x16.png")
            ?? AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Environment/DungeonTileset/atlas_floor-16x16.png").OfType<Sprite>().FirstOrDefault();

        const string FantasyUIPath = "Assets/Art/UI/Fantasy-UI.png";
        Sprite LoadNamedSubSprite(string spriteName)
        {
            var s = AssetDatabase.LoadAllAssetsAtPath(FantasyUIPath).OfType<Sprite>().FirstOrDefault(sp => sp.name == spriteName);
            if (s == null) Debug.LogWarning($"[SceneBuilder] Không tìm thấy sprite con: {spriteName}");
            return s;
        }
        var ornateFrame = LoadNamedSubSprite("Fantasy-UI_1");

        Sprite LoadSprite(string path)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogWarning($"[SceneBuilder] Không tìm thấy sprite (chưa convert?): {path}");
            return s;
        }

        var grahamPortrait = LoadSprite("Assets/Art/Characters/Heroes/Actor Portrait/Graham 1A[portrait].png");
        var sallyPortrait = LoadSprite("Assets/Art/Characters/Heroes/Actor Portrait/Sally 1A[portrait].png");
        var violetPortrait = LoadSprite("Assets/Art/Characters/Heroes/Actor Portrait/Violet 1A[portrait].png");
        var jamesPortrait = LoadSprite("Assets/Art/Characters/Heroes/Actor Portrait/James 1A[portrait].png");

        var slimeIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Slime 1A[icon].png");
        var batIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Bat 1A[icon].png");
        var defencerIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Defencer 1A[icon].png");
        var zombieIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Zombie 1A[icon].png");
        var genieIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Genie 1A[icon].png");
        var treantIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Treant 1A[icon].png");
        var asimoleIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Asimole 1A[icon].png");
        var creepsIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Creeps 1A[icon].png");
        var demonpotIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Demonpot A[icon].png");
        var mechasphereIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Mechasphere A[icon].png");
        var zeroIcon = LoadSprite("Assets/Art/Characters/Enemies/Icon 32x32/Zero 1A[icon].png");

        var allPrefabs = new[] { graham, sally, violet, james, slime, bat, defencer, zombie, genie, treant, asimole, creeps, demonpot, mechasphere, zero };
        if (allPrefabs.Any(p => p == null))
        {
            Debug.LogError("[SceneBuilder] Thiếu prefab ở trên — chạy đủ Setup Hero Prefabs + Tier A/B/C/D trước rồi thử lại.");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- Camera ----
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

        // ---- World-space dungeon Tilemap (nhiều biến thể sàn/tường thật, không lặp 1 tile) ----
        BuildDungeonTilemap();

        // ---- Combat background (world-space, đổi theo tier quái) ----
        // Đặt Z=3: giữa nhân vật (Z=0, gần camera hơn nên vẽ đè lên) và DungeonGrid (Z=5, xa nhất).
        var combatBgGo = new GameObject("CombatBackground");
        combatBgGo.transform.position = new Vector3(0, 0, 3f);
        var combatBgRenderer = combatBgGo.AddComponent<SpriteRenderer>();
        combatBgRenderer.sortingOrder = -1;

        var battlegroundSprites = new[]
        {
            LoadSprite("Assets/Pixel-Art-Battlegrounds/PNG/Battleground1/Bright/Battleground1.png"),
            LoadSprite("Assets/Pixel-Art-Battlegrounds/PNG/Battleground2/Bright/Battleground2.png"),
            LoadSprite("Assets/Pixel-Art-Battlegrounds/PNG/Battleground3/Bright/Battleground3.png"),
            LoadSprite("Assets/Pixel-Art-Battlegrounds/PNG/Battleground4/Bright/Battleground4.png"),
        };
        combatBgRenderer.sprite = battlegroundSprites[0];

        // ---- EventSystem ----
        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<StandaloneInputModule>();

        // ---- Canvas ----
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // ---- Main Menu Panel ----
        var menuPanel = CreateUIObject("MainMenuPanel", canvasGo.transform);
        StretchFull(menuPanel.GetComponent<RectTransform>());
        menuPanel.AddComponent<Image>().color = BgColor;

        var titleText = CreateText(menuPanel.transform, "Title", "SHATTERED GATE", 56, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(900, 100));
        titleText.color = GoldColor;
        titleText.fontStyle = FontStyle.Bold;

        if (ornateFrame != null)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                var ornGo = CreateUIObject("TitleOrnament", menuPanel.transform);
                var ornRt = ornGo.GetComponent<RectTransform>();
                ornRt.anchorMin = new Vector2(0.5f, 0.5f);
                ornRt.anchorMax = new Vector2(0.5f, 0.5f);
                ornRt.anchoredPosition = new Vector2(side * 340, 100);
                ornRt.sizeDelta = new Vector2(44, 44);
                var ornImg = ornGo.AddComponent<Image>();
                ornImg.sprite = ornateFrame;
                ornImg.preserveAspect = true;
            }
        }

        var subtitleText = CreateText(menuPanel.transform, "Subtitle", "A Node-Based Roguelite", 20, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(700, 40));
        subtitleText.color = new Color(0.7f, 0.7f, 0.75f);

        var startBtn = CreateButton(menuPanel.transform, "StartButton", "Start Game",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(220, 46));

        // Continue/Options: chưa có hệ thống save/settings đứng sau — hiện nút nhưng khoá lại
        // thay vì giả vờ hoạt động, để bố cục đủ 5 nút như bản mẫu mà không có UI "chết".
        var continueBtn = CreateButton(menuPanel.transform, "ContinueButton", "Continue",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -114), new Vector2(220, 46));
        continueBtn.interactable = false;

        var optionsBtn = CreateButton(menuPanel.transform, "OptionsButton", "Options",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -168), new Vector2(220, 46));
        optionsBtn.interactable = false;

        var creditsBtn = CreateButton(menuPanel.transform, "CreditsButton", "Credits",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -222), new Vector2(220, 46));

        var quitBtn = CreateButton(menuPanel.transform, "QuitButton", "Exit Game",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -276), new Vector2(220, 46));

        // ---- Credits Panel ----
        var creditsPanel = CreateUIObject("CreditsPanel", canvasGo.transform);
        StretchFull(creditsPanel.GetComponent<RectTransform>());
        creditsPanel.AddComponent<Image>().color = new Color(0.03f, 0.02f, 0.05f, 1f);

        var creditsTitle = CreateText(creditsPanel.transform, "CreditsTitle", "CREDITS", 30, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(700, 50));
        creditsTitle.color = GoldColor;

        var creditsBody = CreateText(creditsPanel.transform,
            "CreditsBody",
            "DungeonTileset II — 0x72 (CC0)\n" +
            "Pixel-Art Battlegrounds — CraftPix.net (Free License)\n" +
            "RPG Sound Pack — artisticdude (CC0)\n" +
            "Short Music Jingles — Kenney.nl (CC0)\n" +
            "Fantasy-UI, Portal VFX — xem license kèm theo trong Assets/",
            17, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(760, 220));

        var creditsBackBtn = CreateButton(creditsPanel.transform, "CreditsBackButton", "Back",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -160), new Vector2(200, 50));

        // ---- Map Panel (5+1 chặng, cổng dạng thoi) ----
        var mapPanel = CreateUIObject("MapPanel", canvasGo.transform);
        StretchFull(mapPanel.GetComponent<RectTransform>());
        var mapBg = mapPanel.AddComponent<Image>();
        mapBg.color = BgColor;
        if (floorSprite != null)
        {
            mapBg.sprite = floorSprite;
            mapBg.type = Image.Type.Tiled;
            mapBg.pixelsPerUnitMultiplier = 0.5f;
            mapBg.color = new Color(0.35f, 0.32f, 0.4f, 1f);
        }
        var mapOverlay = CreateUIObject("DarkOverlay", mapPanel.transform);
        StretchFull(mapOverlay.GetComponent<RectTransform>());
        mapOverlay.AddComponent<Image>().color = new Color(0.03f, 0.02f, 0.05f, 0.72f);

        var stageTitle = CreateText(mapPanel.transform, "StageTitle", "Stage", 30, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(700, 60));
        stageTitle.color = GoldColor;

        var partyText = CreateText(mapPanel.transform, "PartyText", "", 16, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140, -30), new Vector2(400, 30));

        var gateButtons = new Button[3];
        var gateDiamonds = new Image[3];
        var gateLabels = new Text[3];
        var gateIcons = new Image[3];
        float[] gateX = { -260f, 0f, 260f };
        for (int i = 0; i < 3; i++)
        {
            var (btn, diamond, label, icon) = CreateGateDiamond(mapPanel.transform, new Vector2(gateX[i], -10f));
            gateButtons[i] = btn;
            gateDiamonds[i] = diamond;
            gateLabels[i] = label;
            gateIcons[i] = icon;
        }

        var legendText = CreateText(mapPanel.transform, "Legend", "Red = Monster    Purple = Boss    Gold = Key", 15, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(700, 30));
        legendText.color = new Color(0.7f, 0.7f, 0.75f);

        // ---- Combat Panel ----
        var combatPanel = CreateUIObject("CombatPanel", canvasGo.transform);
        StretchFull(combatPanel.GetComponent<RectTransform>());
        // CombatPanel không có Image nền riêng — panel trong suốt để lộ world phía sau (background world-space + nhân vật).

        var turnOrderText = CreateText(combatPanel.transform, "TurnOrderText", "Turn order: ", 22, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(900, 40));

        var partyNameTexts = new Text[4];
        var partyHpFills = new Image[4];
        var partyPortraits = new Image[4];
        var partyTagTexts = new Text[4];
        for (int i = 0; i < 4; i++)
        {
            var (fill, label, portrait, tagText) = CreateHealthBarSlot(combatPanel.transform, $"PartyHUD{i}",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(160, 40 + i * 46), new Vector2(220, 38));
            partyHpFills[i] = fill;
            partyNameTexts[i] = label;
            partyPortraits[i] = portrait;
            partyTagTexts[i] = tagText;
        }

        var (enemyFill, enemyLabel, enemyPortraitImg, enemyTagTextObj) = CreateHealthBarSlot(combatPanel.transform, "EnemyHUD",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-170, 220), new Vector2(260, 38));

        var logText = CreateText(combatPanel.transform, "LogText", "", 20, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 118), new Vector2(900, 50));

        var apText = CreateText(combatPanel.transform, "APText", "", 18, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 72), new Vector2(300, 20));
        apText.color = GoldColor;

        var skillButtons = new Button[3];
        float[] skillX = { -220f, 0f, 220f };
        for (int i = 0; i < 3; i++)
        {
            skillButtons[i] = CreateButton(combatPanel.transform, $"SkillButton{i}", "Skill",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(skillX[i], 35), new Vector2(200, 56));
        }

        // ---- Key Gate Panel ----
        var keyGatePanel = CreateUIObject("KeyGatePanel", canvasGo.transform);
        StretchFull(keyGatePanel.GetComponent<RectTransform>());
        keyGatePanel.AddComponent<Image>().color = new Color(0.03f, 0.02f, 0.05f, 1f);

        var keyTitle = CreateText(keyGatePanel.transform, "KeyTitle", "GATE OF KEYS", 30, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(700, 50));
        keyTitle.color = GoldColor;

        var keyLoreText = CreateText(keyGatePanel.transform, "KeyLore", "", 19, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(760, 180));

        var keyContinueBtn = CreateButton(keyGatePanel.transform, "KeyContinueButton", "Continue",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -110), new Vector2(220, 56));

        // ---- End Panel ----
        var endPanel = CreateUIObject("EndPanel", canvasGo.transform);
        StretchFull(endPanel.GetComponent<RectTransform>());
        endPanel.AddComponent<Image>().color = new Color(0.03f, 0.02f, 0.05f, 1f);

        var endTitle = CreateText(endPanel.transform, "EndTitle", "", 40, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(800, 120));
        endTitle.color = GoldColor;
        endTitle.fontStyle = FontStyle.Bold;

        var restartBtn = CreateButton(endPanel.transform, "RestartButton", "Back to Menu",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(240, 60));

        // ---- Pause Panel (overlay modal, KHÔNG nằm trong ShowOnly — tự bật/tắt riêng qua Esc) ----
        var pausePanel = CreateUIObject("PausePanel", canvasGo.transform);
        StretchFull(pausePanel.GetComponent<RectTransform>());
        pausePanel.AddComponent<Image>().color = new Color(0.02f, 0.015f, 0.03f, 0.82f);
        pausePanel.SetActive(false);

        var pauseTitle = CreateText(pausePanel.transform, "PauseTitle", "PAUSED", 34, TextAnchor.MiddleCenter,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(500, 50));
        pauseTitle.color = GoldColor;
        pauseTitle.fontStyle = FontStyle.Bold;

        var resumeBtn = CreateButton(pausePanel.transform, "ResumeButton", "Resume",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(220, 50));

        var pauseSettingsBtn = CreateButton(pausePanel.transform, "PauseSettingsButton", "Settings",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(220, 50));
        pauseSettingsBtn.interactable = false;

        var pauseQuitBtn = CreateButton(pausePanel.transform, "PauseQuitButton", "Quit to Menu",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(220, 50));

        // ---- Transition Overlay (hiệu ứng "xuyên Cổng Không Gian" khi đổi panel) ----
        // Tạo sau cùng dưới Canvas để luôn render đè lên mọi panel khác.
        var transitionRoot = CreateUIObject("TransitionOverlay", canvasGo.transform);
        StretchFull(transitionRoot.GetComponent<RectTransform>());
        var fadeImg = transitionRoot.AddComponent<Image>();
        fadeImg.color = new Color(0f, 0f, 0f, 0f);

        var portalFrames = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/VFX/Portal/sprite-sheet.png")
            .OfType<Sprite>()
            .Select(s => new { sprite = s, index = ParseTrailingIndex(s.name) })
            .Where(x => x.index >= 0)
            .OrderBy(x => x.index)
            .Select(x => x.sprite)
            .ToArray();
        if (portalFrames.Length == 0)
            Debug.LogWarning("[SceneBuilder] Không tìm thấy frame Portal (sprite-sheet.png chưa cắt Sprite Editor?) — hiệu ứng chuyển cảnh sẽ chỉ có fade đen.");

        var portalGo = CreateUIObject("PortalImage", transitionRoot.transform);
        var portalRt = portalGo.GetComponent<RectTransform>();
        portalRt.anchorMin = new Vector2(0.5f, 0.5f);
        portalRt.anchorMax = new Vector2(0.5f, 0.5f);
        portalRt.anchoredPosition = Vector2.zero;
        portalRt.sizeDelta = new Vector2(150, 150);
        var portalImg = portalGo.AddComponent<Image>();
        portalImg.preserveAspect = true;
        portalImg.raycastTarget = false;
        portalImg.color = new Color(1f, 1f, 1f, 0f);
        if (portalFrames.Length > 0) portalImg.sprite = portalFrames[0];

        transitionRoot.SetActive(false);

        var transition = canvasGo.AddComponent<SceneTransition>();
        transition.overlayRoot = transitionRoot;
        transition.fadeImage = fadeImg;
        transition.portalImage = portalImg;
        transition.portalFrames = portalFrames;

        // ---- Spawn points (world space) ----
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
        var enemySpawnGo = new GameObject("EnemySpawn");
        enemySpawnGo.transform.SetParent(spawnRoot.transform);
        enemySpawnGo.transform.position = new Vector3(3.2f, 0.3f, 0);

        // ---- MapManager data: 5 chặng + Final Boss ----
        var mapMgrGo = new GameObject("MapManager");
        var mapMgr = mapMgrGo.AddComponent<MapManager>();
        mapMgr.gateButtons = gateButtons;
        mapMgr.gateDiamonds = gateDiamonds;
        mapMgr.gateLabels = gateLabels;
        mapMgr.gateIcons = gateIcons;
        mapMgr.stageTitleText = stageTitle;
        mapMgr.finalBossPrefab = zero;
        mapMgr.finalBossName = "Zero";
        mapMgr.finalBossIcon = zeroIcon;
        mapMgr.stages = new[]
        {
            new MapManager.StageDef{ stageName = "Stage 1", gates = new[]{
                new MapManager.Gate{ type = MapManager.GateType.Monster, enemyPrefab = slime, label = "Slime", icon = slimeIcon },
                new MapManager.Gate{ type = MapManager.GateType.Boss, enemyPrefab = treant, label = "Treant", icon = treantIcon },
                new MapManager.Gate{ type = MapManager.GateType.Key, label = "Sally" },
            }},
            new MapManager.StageDef{ stageName = "Stage 2", gates = new[]{
                new MapManager.Gate{ type = MapManager.GateType.Monster, enemyPrefab = bat, label = "Bat", icon = batIcon },
                new MapManager.Gate{ type = MapManager.GateType.Boss, enemyPrefab = asimole, label = "Asimole", icon = asimoleIcon },
            }},
            new MapManager.StageDef{ stageName = "Stage 3", gates = new[]{
                new MapManager.Gate{ type = MapManager.GateType.Monster, enemyPrefab = defencer, label = "Defencer", icon = defencerIcon },
                new MapManager.Gate{ type = MapManager.GateType.Boss, enemyPrefab = creeps, label = "Creeps", icon = creepsIcon },
                new MapManager.Gate{ type = MapManager.GateType.Key, label = "Violet" },
            }},
            new MapManager.StageDef{ stageName = "Stage 4", gates = new[]{
                new MapManager.Gate{ type = MapManager.GateType.Monster, enemyPrefab = zombie, label = "Zombie", icon = zombieIcon },
                new MapManager.Gate{ type = MapManager.GateType.Boss, enemyPrefab = demonpot, label = "Demonpot", icon = demonpotIcon },
            }},
            new MapManager.StageDef{ stageName = "Stage 5", gates = new[]{
                new MapManager.Gate{ type = MapManager.GateType.Monster, enemyPrefab = genie, label = "Genie", icon = genieIcon },
                new MapManager.Gate{ type = MapManager.GateType.Boss, enemyPrefab = mechasphere, label = "Mechasphere", icon = mechasphereIcon },
                new MapManager.Gate{ type = MapManager.GateType.Key, label = "James" },
            }},
        };

        // ---- CombatManager ----
        var combatMgrGo = new GameObject("CombatManager");
        var combatMgr = combatMgrGo.AddComponent<CombatManager>();
        combatMgr.partySpawnPoints = partySpawns;
        combatMgr.enemySpawnPoint = enemySpawnGo.transform;
        combatMgr.turnOrderText = turnOrderText;
        combatMgr.partyNameText = partyNameTexts;
        combatMgr.partyHpFill = partyHpFills;
        combatMgr.enemyNameText = enemyLabel;
        combatMgr.enemyHpFill = enemyFill;
        combatMgr.logText = logText;
        combatMgr.apText = apText;
        combatMgr.skillButtons = skillButtons;
        combatMgr.backgroundRenderer = combatBgRenderer;
        combatMgr.battlegroundSprites = battlegroundSprites;
        combatMgr.cameraShake = shake;
        combatMgr.sfxSource = sfxSource;
        combatMgr.partyPortrait = partyPortraits;
        combatMgr.partyTagText = partyTagTexts;
        combatMgr.enemyPortrait = enemyPortraitImg;
        combatMgr.enemyTagText = enemyTagTextObj;
        combatMgr.portraitLookup = new[]
        {
            new CombatManager.NamedSprite{ unitName = "Graham", sprite = grahamPortrait },
            new CombatManager.NamedSprite{ unitName = "Sally", sprite = sallyPortrait },
            new CombatManager.NamedSprite{ unitName = "Violet", sprite = violetPortrait },
            new CombatManager.NamedSprite{ unitName = "James", sprite = jamesPortrait },
            new CombatManager.NamedSprite{ unitName = "Slime", sprite = slimeIcon },
            new CombatManager.NamedSprite{ unitName = "Bat", sprite = batIcon },
            new CombatManager.NamedSprite{ unitName = "Defencer", sprite = defencerIcon },
            new CombatManager.NamedSprite{ unitName = "Zombie", sprite = zombieIcon },
            new CombatManager.NamedSprite{ unitName = "Genie", sprite = genieIcon },
            new CombatManager.NamedSprite{ unitName = "Treant", sprite = treantIcon },
            new CombatManager.NamedSprite{ unitName = "Asimole", sprite = asimoleIcon },
            new CombatManager.NamedSprite{ unitName = "Creeps", sprite = creepsIcon },
            new CombatManager.NamedSprite{ unitName = "Demonpot", sprite = demonpotIcon },
            new CombatManager.NamedSprite{ unitName = "Mechasphere", sprite = mechasphereIcon },
            new CombatManager.NamedSprite{ unitName = "Zero", sprite = zeroIcon },
        };

        // ---- GameFlow ----
        var flowGo = new GameObject("GameFlow");
        var flow = flowGo.AddComponent<GameFlow>();
        flow.mainMenuPanel = menuPanel;
        flow.mapPanel = mapPanel;
        flow.combatPanel = combatPanel;
        flow.keyGatePanel = keyGatePanel;
        flow.endPanel = endPanel;
        flow.creditsPanel = creditsPanel;
        flow.pausePanel = pausePanel;
        flow.startButton = startBtn;
        flow.creditsButton = creditsBtn;
        flow.creditsBackButton = creditsBackBtn;
        flow.quitButton = quitBtn;
        flow.restartButton = restartBtn;
        flow.resumeButton = resumeBtn;
        flow.pauseSettingsButton = pauseSettingsBtn;
        flow.pauseQuitButton = pauseQuitBtn;
        flow.keyGateContinueButton = keyContinueBtn;
        flow.keyGateLoreText = keyLoreText;
        flow.endTitleText = endTitle;
        flow.mapManager = mapMgr;
        flow.combatManager = combatMgr;
        flow.transition = transition;
        flow.mainHeroPrefab = graham;
        flow.recruitOrder = new[] { sally, violet, james };
        flow.recruitLoreLines = new[]
        {
            "Beyond a cracked archway, you find Sally — a wandering mage trapped between two crumbling worlds. She joins your cause, drawn to the light of your resolve.",
            "In the ruins of a fallen watchtower, Violet the Hunter has been tracking the same portals as you. Seeing a chance for revenge, she falls in step beside you.",
            "James, a mercenary abandoned by his last contract, offers his blade for passage back to his own shattered world. You accept.",
        };

        Directory.CreateDirectory("Assets/Scenes");
        string scenePath = "Assets/Scenes/Main.unity";
        EditorSceneManager.SaveScene(scene, scenePath);

        var scenesList = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!scenesList.Any(s => s.path == scenePath))
            scenesList.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenesList.ToArray();

        // Scene toàn bộ là 2D phẳng (mọi sprite/UI nằm trên các mặt phẳng Z khác nhau) — Scene view mặc định
        // là camera 3D góc nghiêng nên sẽ thấy các mặt phẳng đó dí cạnh, trông như bị "cắt lát". Ép về chế độ
        // 2D (nhìn thẳng góc, orthographic) mỗi lần build lại để luôn xem được đúng ngay khi mở scene.
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.in2DMode = true;
            SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(0, 0, 0), new Vector3(20, 12, 1)), false);
        }

        Debug.Log("[SceneBuilder] Xong! Scene 'Main' (5+1 chặng, tuyển mộ đầy đủ) tại " + scenePath + " — mở lên và bấm Play để test.");
    }

    static Tile GetOrCreateTileAsset(Sprite sprite, string folder)
    {
        Directory.CreateDirectory(folder);
        string path = $"{folder}/Tile_{sprite.name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (existing != null) return existing;
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    static void BuildDungeonTilemap()
    {
        const string framesFolder = "Assets/Art/Environment/DungeonTileset/frames";
        const string tileFolder = "Assets/Art/Environment/DungeonTileset/GeneratedTiles";

        Tile LoadTile(string fileName)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{framesFolder}/{fileName}.png");
            if (sprite == null)
            {
                Debug.LogWarning($"[SceneBuilder] Không tìm thấy tile: {fileName}.png");
                return null;
            }
            return GetOrCreateTileAsset(sprite, tileFolder);
        }

        var floorTiles = Enumerable.Range(1, 8)
            .Select(i => LoadTile($"floor_{i}"))
            .Where(t => t != null)
            .ToArray();

        var wallTopLeft = LoadTile("wall_top_left");
        var wallTopMid = LoadTile("wall_top_mid");
        var wallTopRight = LoadTile("wall_top_right");
        var wallLeft = LoadTile("wall_left");
        var wallMid = LoadTile("wall_mid");
        var wallRight = LoadTile("wall_right");

        if (floorTiles.Length == 0)
        {
            Debug.LogWarning("[SceneBuilder] Không tìm thấy tile sàn — bỏ qua dựng Tilemap nền.");
            return;
        }

        var gridGo = new GameObject("DungeonGrid");
        gridGo.transform.position = new Vector3(0, 0, 5f);
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 1f);

        var floorTmGo = new GameObject("FloorTilemap");
        floorTmGo.transform.SetParent(gridGo.transform, false);
        var floorTm = floorTmGo.AddComponent<Tilemap>();
        var floorRend = floorTmGo.AddComponent<TilemapRenderer>();
        floorRend.sortingOrder = -100;

        const int left = -12, right = 12;
        const int floorTop = 1, floorBottom = -6;

        var rng = new System.Random(42);
        for (int x = left; x <= right; x++)
            for (int y = floorBottom; y <= floorTop; y++)
                floorTm.SetTile(new Vector3Int(x, y, 0), floorTiles[rng.Next(floorTiles.Length)]);

        if (wallTopMid != null && wallMid != null)
        {
            var wallTmGo = new GameObject("WallTilemap");
            wallTmGo.transform.SetParent(gridGo.transform, false);
            var wallTm = wallTmGo.AddComponent<Tilemap>();
            var wallRend = wallTmGo.AddComponent<TilemapRenderer>();
            wallRend.sortingOrder = -90;

            int wallFaceY = floorTop + 1; // hàng mặt tường, ngay trên sàn
            int wallTopY = wallFaceY + 1; // hàng nóc tường

            for (int x = left; x <= right; x++)
            {
                var face = (x == left) ? wallLeft : (x == right) ? wallRight : wallMid;
                var top = (x == left) ? wallTopLeft : (x == right) ? wallTopRight : wallTopMid;
                if (face != null) wallTm.SetTile(new Vector3Int(x, wallFaceY, 0), face);
                if (top != null) wallTm.SetTile(new Vector3Int(x, wallTopY, 0), top);
            }
        }

        AssetDatabase.SaveAssets();
    }

    static int ParseTrailingIndex(string spriteName)
    {
        var m = Regex.Match(spriteName, @"_(\d+)$");
        return m.Success ? int.Parse(m.Groups[1].Value) : -1;
    }

    static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor align,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = CreateUIObject(name, parent);
        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = content;
        txt.fontSize = fontSize;
        txt.alignment = align;
        txt.color = Color.white;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        SetRect(go.GetComponent<RectTransform>(), anchorMin, anchorMax, anchoredPos, sizeDelta);
        return txt;
    }

    static (Image fill, Text label, Image portrait, Text tagText) CreateHealthBarSlot(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var container = CreateUIObject(name, parent);
        SetRect(container.GetComponent<RectTransform>(), anchorMin, anchorMax, anchoredPos, sizeDelta);
        var bg = container.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.06f, 0.1f, 0.92f);

        // Khung portrait nhô ra bên trái thanh máu
        var portraitGo = CreateUIObject("Portrait", container.transform);
        var portraitRt = portraitGo.GetComponent<RectTransform>();
        portraitRt.anchorMin = new Vector2(0f, 0.5f);
        portraitRt.anchorMax = new Vector2(0f, 0.5f);
        portraitRt.anchoredPosition = new Vector2(-28, 0);
        portraitRt.sizeDelta = new Vector2(50, 50);
        var portraitBg = portraitGo.AddComponent<Image>();
        portraitBg.color = new Color(0.55f, 0.48f, 0.3f, 1f);
        var portraitInnerGo = CreateUIObject("Icon", portraitGo.transform);
        var portraitInnerRt = portraitInnerGo.GetComponent<RectTransform>();
        portraitInnerRt.anchorMin = Vector2.zero;
        portraitInnerRt.anchorMax = Vector2.one;
        portraitInnerRt.offsetMin = new Vector2(3, 3);
        portraitInnerRt.offsetMax = new Vector2(-3, -3);
        var portraitImg = portraitInnerGo.AddComponent<Image>();
        portraitImg.preserveAspect = true;
        portraitImg.color = Color.white;
        portraitImg.enabled = false;

        var fillGo = CreateUIObject("Fill", container.transform);
        var fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(26, 3);
        fillRt.offsetMax = new Vector2(-3, -3);
        var fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.35f, 0.75f, 0.35f);
        fillImg.type = Image.Type.Filled;
        fillImg.fillMethod = Image.FillMethod.Horizontal;
        fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImg.fillAmount = 1f;

        var labelGo = CreateUIObject("Label", container.transform);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(23, 0);
        labelRt.offsetMax = Vector2.zero;
        var labelTxt = labelGo.AddComponent<Text>();
        labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelTxt.alignment = TextAnchor.MiddleCenter;
        labelTxt.color = Color.white;
        labelTxt.fontSize = 14;
        labelTxt.fontStyle = FontStyle.Bold;
        var labelOutline = labelGo.AddComponent<Outline>();
        labelOutline.effectColor = new Color(0, 0, 0, 0.85f);
        labelOutline.effectDistance = new Vector2(1f, -1f);

        // Nhãn Tag (Vulnerable/Stunned) nổi phía trên thanh máu
        var tagGo = CreateUIObject("TagText", container.transform);
        var tagRt = tagGo.GetComponent<RectTransform>();
        tagRt.anchorMin = new Vector2(0.5f, 1f);
        tagRt.anchorMax = new Vector2(0.5f, 1f);
        tagRt.anchoredPosition = new Vector2(0, 13);
        tagRt.sizeDelta = new Vector2(sizeDelta.x, 18);
        var tagTxt = tagGo.AddComponent<Text>();
        tagTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tagTxt.alignment = TextAnchor.MiddleCenter;
        tagTxt.fontSize = 13;
        tagTxt.fontStyle = FontStyle.Bold;
        tagTxt.color = Color.white;
        var tagOutline = tagGo.AddComponent<Outline>();
        tagOutline.effectColor = new Color(0, 0, 0, 0.85f);
        tagOutline.effectDistance = new Vector2(1f, -1f);

        return (fillImg, labelTxt, portraitImg, tagTxt);
    }

    static (Button btn, Image diamond, Text label, Image icon) CreateGateDiamond(Transform parent, Vector2 anchoredPos)
    {
        var go = CreateUIObject("Gate", parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(120, 120);
        rt.localRotation = Quaternion.Euler(0, 0, 45f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.4f, 0.4f, 0.4f);
        var btn = go.AddComponent<Button>();

        // Icon quái/boss, đặt phía trên chữ, tự xoay ngược lại cho thẳng
        var iconGo = CreateUIObject("Icon", go.transform);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(0, 18);
        iconRt.sizeDelta = new Vector2(40, 40);
        iconRt.localRotation = Quaternion.Euler(0, 0, -45f);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.enabled = false;

        var labelGo = CreateUIObject("Label", go.transform);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.anchoredPosition = new Vector2(0, -22);
        labelRt.sizeDelta = new Vector2(140, 50);
        labelRt.localRotation = Quaternion.Euler(0, 0, -45f);
        var txt = labelGo.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.fontSize = 15;
        txt.fontStyle = FontStyle.Bold;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = 8;
        txt.resizeTextMaxSize = 16;
        var outline = labelGo.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.85f);
        outline.effectDistance = new Vector2(1f, -1f);

        return (btn, img, txt, iconImg);
    }

    static Button CreateButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = CreateUIObject(name, parent);
        var img = go.AddComponent<Image>();
        img.color = ButtonColor;
        go.AddComponent<Button>();
        SetRect(go.GetComponent<RectTransform>(), anchorMin, anchorMax, anchoredPos, sizeDelta);

        var txtGo = CreateUIObject("Label", go.transform);
        var txt = txtGo.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.fontSize = 16;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Truncate;
        txt.resizeTextForBestFit = true;
        txt.resizeTextMinSize = 10;
        txt.resizeTextMaxSize = 20;
        var btnOutline = txtGo.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0, 0, 0, 0.8f);
        btnOutline.effectDistance = new Vector2(1f, -1f);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(12, 8);
        txtRt.offsetMax = new Vector2(-12, -8);

        return go.GetComponent<Button>();
    }
}
