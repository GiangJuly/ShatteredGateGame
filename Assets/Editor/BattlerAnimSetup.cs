using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// Tự động tạo 4 Animation Clip (Idle/Hurt/Attack/Dead) + Animator Controller
/// (3 Trigger + 6 Transition chuẩn) + Prefab cho từng nhân vật, dựa theo cùng
/// bảng pose (hàng 0/3/4/12 trong sheet 14 hàng) đã dùng cho 4 hero làm tay.
public static class BattlerAnimSetup
{
    const int FrameRate = 12;

    // (frame bắt đầu trong sheet đã cắt, số frame, tên pose, có Loop không)
    static readonly (int start, int count, string clipName, bool loop)[] PoseRanges =
    {
        (0, 4, "Idle", true),
        (12, 4, "Hurt", false),
        (16, 4, "Attack", false),
        (48, 4, "Dead", false),
    };

    static readonly string[] TierA = { "Slime", "Training Dummy", "Bat", "Frog", "Barrel" };
    static readonly string[] TierB = { "Defencer", "Zombie", "Element Crystal" };
    static readonly string[] TierC = { "Genie", "Fairy", "Vampire Tulip", "Two Faced", "Cactuar" };
    static readonly string[] TierD = { "Treant", "Asimole", "Creeps", "Demonpot", "Zero" };

    const string EnemySourceFolder = "Assets/Art/Characters/Enemies/Anim";

    [MenuItem("ShatteredGate/Setup Enemy Battlers/Tier A (Slime, Dummy, Bat, Frog, Barrel)")]
    static void SetupTierA() => SetupBatch(TierA, "TierA");

    [MenuItem("ShatteredGate/Setup Enemy Battlers/Tier B (Defencer, Zombie, Element Crystal)")]
    static void SetupTierB() => SetupBatch(TierB, "TierB");

    [MenuItem("ShatteredGate/Setup Enemy Battlers/Tier C (Genie, Fairy, Vampire Tulip, Two Faced, Cactuar)")]
    static void SetupTierC() => SetupBatch(TierC, "TierC");

    [MenuItem("ShatteredGate/Setup Enemy Battlers/Tier D (Treant, Asimole, Creeps, Demonpot, Zero)")]
    static void SetupTierD() => SetupBatch(TierD, "TierD");

    [MenuItem("ShatteredGate/Setup Enemy Battlers/Tier D - Bổ sung Mechasphere (boss chặng thứ 5)")]
    static void SetupMechasphere() => SetupBatch(new[] { "Mechasphere" }, "TierD");

    static void SetupBatch(string[] names, string tierFolder)
    {
        int ok = 0, fail = 0;
        foreach (var n in names)
        {
            try
            {
                SetupCharacter(n, "Enemies", EnemySourceFolder, tierFolder);
                ok++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BattlerAnimSetup] Lỗi khi xử lý '{n}': {e.Message}");
                fail++;
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattlerAnimSetup] Hoàn tất: {ok} thành công, {fail} lỗi.");
    }

    static void SetupCharacter(string charName, string category, string sourceFolder, string tierFolder = null)
    {
        string pngPath = FindSourcePng(charName, sourceFolder);
        if (pngPath == null)
            throw new System.Exception($"Không tìm thấy file sprite sheet cho '{charName}' trong {sourceFolder}");

        var sprites = AssetDatabase.LoadAllAssetsAtPath(pngPath)
            .OfType<Sprite>()
            .Select(s => new { sprite = s, index = ParseIndex(s.name) })
            .Where(x => x.index >= 0)
            .OrderBy(x => x.index)
            .Select(x => x.sprite)
            .ToArray();

        if (sprites.Length < 52)
            throw new System.Exception($"Chỉ tìm thấy {sprites.Length} sprite con — file này có vẻ chưa được cắt bằng Sprite Editor (cần Grid slice trước).");

        string categoryFolder = string.IsNullOrEmpty(tierFolder)
            ? $"Assets/Animations/{category}"
            : $"Assets/Animations/{category}/{tierFolder}";
        string animFolder = $"{categoryFolder}/{charName}";
        Directory.CreateDirectory(animFolder);

        var clips = new AnimationClip[PoseRanges.Length];
        for (int p = 0; p < PoseRanges.Length; p++)
        {
            var (start, count, poseName, loop) = PoseRanges[p];
            var clip = new AnimationClip { frameRate = FrameRate };
            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[count];
            for (int i = 0; i < count; i++)
                keys[i] = new ObjectReferenceKeyframe { time = (float)i / FrameRate, value = sprites[start + i] };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            string clipPath = $"{animFolder}/{charName}_{poseName}.anim";
            DeleteIfExists(clipPath);
            AssetDatabase.CreateAsset(clip, clipPath);
            clips[p] = clip;
        }

        string controllerPath = $"{animFolder}/{charName}_Animator.controller";
        DeleteIfExists(controllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Dead", AnimatorControllerParameterType.Trigger);

        var sm = controller.layers[0].stateMachine;
        var idleState = sm.AddState($"{charName}_Idle");
        idleState.motion = clips[0];
        var hurtState = sm.AddState($"{charName}_Hurt");
        hurtState.motion = clips[1];
        var attackState = sm.AddState($"{charName}_Attack");
        attackState.motion = clips[2];
        var deadState = sm.AddState($"{charName}_Dead");
        deadState.motion = clips[3];

        sm.defaultState = idleState;

        var t1 = idleState.AddTransition(attackState);
        t1.hasExitTime = false; t1.duration = 0f;
        t1.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        var t2 = attackState.AddTransition(idleState);
        t2.hasExitTime = true; t2.exitTime = 1f; t2.duration = 0f;

        var t3 = sm.AddAnyStateTransition(hurtState);
        t3.hasExitTime = false; t3.duration = 0f;
        t3.AddCondition(AnimatorConditionMode.If, 0, "Hurt");

        var t4 = hurtState.AddTransition(idleState);
        t4.hasExitTime = true; t4.exitTime = 1f; t4.duration = 0f;

        var t5 = sm.AddAnyStateTransition(deadState);
        t5.hasExitTime = false; t5.duration = 0f;
        t5.AddCondition(AnimatorConditionMode.If, 0, "Dead");

        string prefabFolder = string.IsNullOrEmpty(tierFolder)
            ? $"Assets/Prefabs/{category}"
            : $"Assets/Prefabs/{category}/{tierFolder}";
        Directory.CreateDirectory(prefabFolder);
        string prefabPath = $"{prefabFolder}/{charName}.prefab";

        var go = new GameObject(charName);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[0];
        var animatorComp = go.AddComponent<Animator>();
        animatorComp.runtimeAnimatorController = controller;

        DeleteIfExists(prefabPath);
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        Debug.Log($"[BattlerAnimSetup] '{charName}' xong: 4 clip + Animator + Prefab tại {prefabPath}");
    }

    static readonly string[] Heroes = { "Graham", "Sally", "Violet", "James" };

    [MenuItem("ShatteredGate/Setup Hero Prefabs (from existing Animator Controllers)")]
    static void SetupHeroPrefabs()
    {
        int ok = 0, fail = 0;
        foreach (var name in Heroes)
        {
            try
            {
                string animFolder = $"Assets/Animations/Heroes/{name}";
                string controllerPath = $"{animFolder}/{name}_Animator.controller";
                string idleClipPath = $"{animFolder}/{name}_Idle.anim";

                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                if (controller == null)
                    throw new System.Exception($"Không tìm thấy {controllerPath}");

                var idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(idleClipPath);
                Sprite firstSprite = null;
                if (idleClip != null)
                {
                    var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
                    var keys = AnimationUtility.GetObjectReferenceCurve(idleClip, binding);
                    if (keys != null && keys.Length > 0)
                        firstSprite = keys[0].value as Sprite;
                }

                string prefabFolder = "Assets/Prefabs/Heroes";
                Directory.CreateDirectory(prefabFolder);
                string prefabPath = $"{prefabFolder}/{name}.prefab";

                var go = new GameObject(name);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = firstSprite;
                var animatorComp = go.AddComponent<Animator>();
                animatorComp.runtimeAnimatorController = controller;

                DeleteIfExists(prefabPath);
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);

                Debug.Log($"[BattlerAnimSetup] Prefab hero '{name}' tạo xong tại {prefabPath}");
                ok++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[BattlerAnimSetup] Lỗi khi tạo prefab '{name}': {e.Message}");
                fail++;
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattlerAnimSetup] Hero Prefabs hoàn tất: {ok} thành công, {fail} lỗi.");
    }

    [MenuItem("ShatteredGate/Setup Portal Loop Animation")]
    static void SetupPortal()
    {
        const string pngPath = "Assets/Art/VFX/Portal/sprite-sheet.png";
        const int portalFrameRate = 12;

        var sprites = AssetDatabase.LoadAllAssetsAtPath(pngPath)
            .OfType<Sprite>()
            .Select(s => new { sprite = s, index = ParseIndex(s.name) })
            .Where(x => x.index >= 0)
            .OrderBy(x => x.index)
            .Select(x => x.sprite)
            .ToArray();

        if (sprites.Length == 0)
        {
            Debug.LogError("[BattlerAnimSetup] Không tìm thấy sprite đã cắt trong sprite-sheet.png (Portal). Đã cắt bằng Sprite Editor chưa?");
            return;
        }

        string folder = "Assets/Animations/VFX/Portal";
        Directory.CreateDirectory(folder);

        var clip = new AnimationClip { frameRate = portalFrameRate };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = (float)i / portalFrameRate, value = sprites[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string clipPath = $"{folder}/Portal_Idle_Loop.anim";
        DeleteIfExists(clipPath);
        AssetDatabase.CreateAsset(clip, clipPath);

        string controllerPath = $"{folder}/Portal_Animator.controller";
        DeleteIfExists(controllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var sm = controller.layers[0].stateMachine;
        var idleState = sm.AddState("Portal_Idle_Loop");
        idleState.motion = clip;
        sm.defaultState = idleState;

        string prefabFolder = "Assets/Prefabs/VFX";
        Directory.CreateDirectory(prefabFolder);
        string prefabPath = $"{prefabFolder}/Portal.prefab";

        var go = new GameObject("Portal");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[0];
        var animatorComp = go.AddComponent<Animator>();
        animatorComp.runtimeAnimatorController = controller;

        DeleteIfExists(prefabPath);
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        Object.DestroyImmediate(go);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BattlerAnimSetup] Portal xong: clip {sprites.Length} frame (loop) + Animator + Prefab tại {prefabPath}");
    }

    static void DeleteIfExists(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }

    static string FindSourcePng(string charName, string folder)
    {
        string[] candidates = { $"{charName} 1A[anim].png", $"{charName} A[anim].png" };
        foreach (var c in candidates)
        {
            string p = (folder + "/" + c);
            if (File.Exists(p)) return p;
        }
        return null;
    }

    static int ParseIndex(string spriteName)
    {
        var m = Regex.Match(spriteName, @"_(\d+)$");
        return m.Success ? int.Parse(m.Groups[1].Value) : -1;
    }
}
