using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ConfigureDannyCharacter
{
    private const string ModelPath = "Assets/Resources/Littles/Danny/Danny.fbx";
    private const string Folder = "Assets/Resources/Littles/Danny";
    private const string ControllerPath = Folder + "/DannyAnimator.controller";
    private const string PrefabPath = Folder + "/Danny.prefab";
    private const string StylizedMaterialFolder = Folder + "/StylizedMaterials";
    private const string StarterAnimationFolder = "Assets/StarterAssets/ThirdPersonController/Character/Animations";
    private const string StarterIdlePath = StarterAnimationFolder + "/Stand--Idle.anim.fbx";
    private const string StarterWalkPath = StarterAnimationFolder + "/Locomotion--Walk_N.anim.fbx";
    private const string StarterRunPath = StarterAnimationFolder + "/Locomotion--Run_N.anim.fbx";
    private const string StarterJumpPath = StarterAnimationFolder + "/Jump--Jump.anim.fbx";
    private const string StarterInAirPath = StarterAnimationFolder + "/Jump--InAir.anim.fbx";

    private static readonly string[] DesiredClips =
    {
        "DANNY_Idle_Friendly",
        "DANNY_Walk",
        "DANNY_Run_Fast",
        "DANNY_Jump",
        "DANNY_Stumble_Clumsy",
        "DANNY_Embarrassed_Annoyed"
    };

    [MenuItem("Tools/The Littles/Configure Danny Character")]
    public static void ConfigureFromMenu() => Configure(false);

    public static void ConfigureBatch()
    {
        Configure(true);
        EditorApplication.Exit(0);
    }

    private static void Configure(bool batchMode)
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null)
            throw new FileNotFoundException("Danny model was not found", ModelPath);

        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.importAnimation = true;
        importer.importBlendShapes = true;
        importer.importCameras = false;
        importer.importLights = false;
        // Danny's Mixamo-style skeleton maps cleanly to Unity Humanoid.  This
        // lets him use the project's professionally authored locomotion instead
        // of the earlier hand-keyed, mechanical direct-bone cycle.
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        HumanDescription humanDescription = importer.humanDescription;
        humanDescription.hasTranslationDoF = true;
        importer.humanDescription = humanDescription;
        importer.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        importer.SaveAndReimport();

        ConfigureClipLoops(importer);
        importer.SaveAndReimport();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Dictionary<string, AnimationClip> clips = LoadClips();
        List<string> missing = DesiredClips.Where(n => !clips.ContainsKey(n)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException("Danny animation clips missing after import: " + string.Join(", ", missing));

        AnimatorController controller = CreateController(clips);
        CreatePrefab(controller);
        ValidatePrefabAndClips(clips);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Avatar avatar = LoadAvatar();
        Debug.Log($"Danny configured successfully. Humanoid avatar valid: {avatar != null && avatar.isValid}; human: {avatar != null && avatar.isHuman}; clips: {string.Join(", ", DesiredClips)}");
        if (!batchMode)
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }

    private static void ConfigureClipLoops(ModelImporter importer)
    {
        ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
        List<ModelImporterClipAnimation> configured = new();
        foreach (ModelImporterClipAnimation source in defaults)
        {
            string desired = DesiredClips.FirstOrDefault(n => source.name.Contains(n, StringComparison.OrdinalIgnoreCase));
            if (desired == null) continue;
            ModelImporterClipAnimation clip = source;
            clip.name = desired;
            clip.loopTime = desired is "DANNY_Idle_Friendly" or "DANNY_Walk" or "DANNY_Run_Fast";
            clip.loopPose = clip.loopTime;
            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionXZ = true;
            clip.keepOriginalPositionY = true;
            configured.Add(clip);
        }
        if (configured.Count > 0)
            importer.clipAnimations = configured.ToArray();
    }

    private static Dictionary<string, AnimationClip> LoadClips()
    {
        AnimationClip[] imported = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))
            .ToArray();
        Dictionary<string, AnimationClip> result = new(StringComparer.OrdinalIgnoreCase)
        {
            ["DANNY_Idle_Friendly"] = LoadExternalClip(StarterIdlePath, "Idle"),
            ["DANNY_Walk"] = LoadExternalClip(StarterWalkPath, "Walk_N"),
            ["DANNY_Run_Fast"] = LoadExternalClip(StarterRunPath, "Run_N"),
            ["DANNY_Jump"] = LoadExternalClip(StarterJumpPath, "JumpStart"),
            ["DANNY_InAir"] = LoadExternalClip(StarterInAirPath, "InAir"),
        };
        foreach (string desired in new[] { "DANNY_Stumble_Clumsy", "DANNY_Embarrassed_Annoyed" })
        {
            AnimationClip clip = imported.FirstOrDefault(c => c.name.Equals(desired, StringComparison.OrdinalIgnoreCase))
                ?? imported.FirstOrDefault(c => c.name.Contains(desired, StringComparison.OrdinalIgnoreCase));
            if (clip != null) result[desired] = clip;
        }
        return result;
    }

    private static AnimationClip LoadExternalClip(string path, string preferredName)
    {
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))
            .ToArray();
        AnimationClip clip = clips.FirstOrDefault(c => c.name.Equals(preferredName, StringComparison.OrdinalIgnoreCase))
            ?? clips.FirstOrDefault();
        if (clip == null)
            throw new InvalidOperationException($"Professional locomotion clip missing: {path}");
        if (!clip.humanMotion)
            throw new InvalidOperationException($"Professional locomotion clip is not Humanoid: {path}/{clip.name}");
        return clip;
    }

    private static AnimatorController CreateController(IReadOnlyDictionary<string, AnimationClip> clips)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("FreeFall", AnimatorControllerParameterType.Bool);
        controller.AddParameter("MotionSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Stumble", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Embarrassed", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        controller.layers[0].iKPass = true;
        machine.name = "Danny Base Layer";
        AnimatorState idle = machine.AddState("Friendly Idle", new Vector3(260, 80));
        AnimatorState walk = machine.AddState("Walk", new Vector3(500, 20));
        AnimatorState run = machine.AddState("Fast Run", new Vector3(740, 20));
        AnimatorState jump = machine.AddState("Jump", new Vector3(500, 180));
        AnimatorState inAir = machine.AddState("In Air", new Vector3(650, 180));
        AnimatorState stumble = machine.AddState("Clumsy Stumble", new Vector3(740, 180));
        AnimatorState embarrassed = machine.AddState("Embarrassed", new Vector3(500, 330));
        idle.motion = clips["DANNY_Idle_Friendly"];
        walk.motion = clips["DANNY_Walk"];
        run.motion = clips["DANNY_Run_Fast"];
        jump.motion = clips["DANNY_Jump"];
        inAir.motion = clips["DANNY_InAir"];
        stumble.motion = clips["DANNY_Stumble_Clumsy"];
        embarrassed.motion = clips["DANNY_Embarrassed_Annoyed"];
        idle.iKOnFeet = true;
        walk.iKOnFeet = true;
        run.iKOnFeet = true;
        walk.speed = 0.88f;
        run.speed = 1.0f;
        jump.speed = 1.0f;
        inAir.speed = 0.90f;
        machine.defaultState = idle;

        AddCondition(idle, walk, AnimatorConditionMode.Greater, 0.10f, "Speed", 0.12f);
        AddCondition(walk, idle, AnimatorConditionMode.Less, 0.10f, "Speed", 0.12f);
        AddCondition(walk, run, AnimatorConditionMode.Greater, 0.65f, "Speed", 0.10f);
        AddCondition(run, walk, AnimatorConditionMode.Less, 0.65f, "Speed", 0.10f);

        AnimatorStateTransition toJump = machine.AddAnyStateTransition(jump);
        toJump.hasExitTime = false;
        toJump.duration = 0.08f;
        toJump.canTransitionToSelf = false;
        toJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        AnimatorStateTransition jumpToAir = jump.AddTransition(inAir);
        jumpToAir.hasExitTime = false;
        jumpToAir.duration = 0.10f;
        jumpToAir.AddCondition(AnimatorConditionMode.If, 0, "FreeFall");

        AnimatorStateTransition airToIdle = inAir.AddTransition(idle);
        airToIdle.hasExitTime = false;
        airToIdle.duration = 0.14f;
        airToIdle.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        airToIdle.AddCondition(AnimatorConditionMode.Less, 0.10f, "Speed");

        AnimatorStateTransition airToWalk = inAir.AddTransition(walk);
        airToWalk.hasExitTime = false;
        airToWalk.duration = 0.14f;
        airToWalk.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        airToWalk.AddCondition(AnimatorConditionMode.Greater, 0.10f, "Speed");
        airToWalk.AddCondition(AnimatorConditionMode.Less, 0.65f, "Speed");

        AnimatorStateTransition airToRun = inAir.AddTransition(run);
        airToRun.hasExitTime = false;
        airToRun.duration = 0.14f;
        airToRun.AddCondition(AnimatorConditionMode.If, 0, "Grounded");
        airToRun.AddCondition(AnimatorConditionMode.Greater, 0.65f, "Speed");

        AnimatorStateTransition toStumble = machine.AddAnyStateTransition(stumble);
        toStumble.hasExitTime = false;
        toStumble.duration = 0.06f;
        toStumble.AddCondition(AnimatorConditionMode.If, 0, "Stumble");
        AnimatorStateTransition stumbleReturn = stumble.AddTransition(idle);
        stumbleReturn.hasExitTime = true;
        stumbleReturn.exitTime = 0.92f;
        stumbleReturn.duration = 0.16f;

        AnimatorStateTransition toEmbarrassed = machine.AddAnyStateTransition(embarrassed);
        toEmbarrassed.hasExitTime = false;
        toEmbarrassed.duration = 0.12f;
        toEmbarrassed.AddCondition(AnimatorConditionMode.If, 0, "Embarrassed");
        AnimatorStateTransition embarrassedReturn = embarrassed.AddTransition(idle);
        embarrassedReturn.hasExitTime = true;
        embarrassedReturn.exitTime = 0.94f;
        embarrassedReturn.duration = 0.18f;
        return controller;
    }

    private static void AddCondition(AnimatorState from, AnimatorState to,
        AnimatorConditionMode mode, float threshold, string parameter, float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.AddCondition(mode, threshold, parameter);
    }

    private static void CreatePrefab(RuntimeAnimatorController controller)
    {
        AssetDatabase.DeleteAsset(PrefabPath);
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null) throw new InvalidOperationException("Danny model prefab could not be loaded");
        GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (instance == null) throw new InvalidOperationException("Danny model could not be instantiated");
        instance.name = "Danny";

        // Keep one Animator on the character root so Humanoid retargeting has
        // one authoritative avatar and controller.
        Animator[] importedAnimators = instance.GetComponentsInChildren<Animator>(true);
        Avatar importedAvatar = importedAnimators.Select(a => a.avatar).FirstOrDefault(a => a != null);
        Animator animator = instance.GetComponent<Animator>();
        if (animator == null) animator = instance.AddComponent<Animator>();
        if (importedAvatar != null) animator.avatar = importedAvatar;
        foreach (Animator childAnimator in importedAnimators)
            if (childAnimator != null && childAnimator != animator)
                UnityEngine.Object.DestroyImmediate(childAnimator);
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Danny's hair uses a secondary sway bone. Imported skinned bounds can
        // lag behind that bone and make the hair (or clothing) flicker offscreen.
        foreach (SkinnedMeshRenderer renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int index = 0; index < materials.Length; index++)
                materials[index] = ResolveStylizedMaterial(materials[index]);
            renderer.sharedMaterials = materials;
            renderer.updateWhenOffscreen = true;
            Bounds bounds = renderer.localBounds;
            bounds.Expand(new Vector3(1.5f, 1.5f, 1.5f));
            renderer.localBounds = bounds;
        }
        PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        UnityEngine.Object.DestroyImmediate(instance);
    }

    private static Material ResolveStylizedMaterial(Material source)
    {
        if (source == null) return null;
        string name = source.name;
        if (name.Contains("Skin", StringComparison.OrdinalIgnoreCase))
            return CreateStylizedMaterial("Danny_Stylized_Skin", new Color(0.56f, 0.33f, 0.22f), 0.16f);
        if (name.Contains("Eye_White", StringComparison.OrdinalIgnoreCase))
            return CreateStylizedMaterial("Danny_Stylized_EyeWhite", new Color(0.92f, 0.95f, 0.96f), 0.20f);
        if (name.Contains("Eye_Dark", StringComparison.OrdinalIgnoreCase))
            return CreateStylizedMaterial("Danny_Stylized_EyeDark", new Color(0.035f, 0.012f, 0.006f), 0.18f);
        if (name.Contains("Hair", StringComparison.OrdinalIgnoreCase))
            return CreateStylizedMaterial("Danny_Stylized_Hair", new Color(0.07f, 0.022f, 0.012f), 0.14f);
        if (name.Contains("WinterCoat_Gold", StringComparison.OrdinalIgnoreCase))
        {
            Material hoodie = CreateStylizedMaterial("Danny_Stylized_PuffyWinterCoat", new Color(0.82f, 0.31f, 0.055f), 0.06f);
            ApplyHoodieFabric(hoodie);
            return hoodie;
        }
        if (name.Contains("WinterCoat_Trim", StringComparison.OrdinalIgnoreCase))
            return CreateStylizedMaterial("Danny_Stylized_WinterTrim", new Color(0.18f, 0.055f, 0.025f), 0.08f);
        if (name.Contains("SnowPants", StringComparison.OrdinalIgnoreCase))
            return CreateStylizedMaterial("Danny_Stylized_SnowPants", new Color(0.025f, 0.075f, 0.16f), 0.08f);
        if (name.Contains("WinterBoots", StringComparison.OrdinalIgnoreCase))
            return CreateStylizedMaterial("Danny_Stylized_WinterBoots", new Color(0.18f, 0.055f, 0.025f), 0.08f);
        if (name.Contains("Shirt", StringComparison.OrdinalIgnoreCase))
            return CreateStylizedMaterial("Danny_Stylized_Shirt", new Color(0.035f, 0.30f, 0.73f), 0.12f);
        if (name.Contains("Short", StringComparison.OrdinalIgnoreCase))
            return CreateStylizedMaterial("Danny_Stylized_Shorts", new Color(0.035f, 0.052f, 0.09f), 0.10f);
        if (name.Contains("Sneaker", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Shoe", StringComparison.OrdinalIgnoreCase))
            return CreateStylizedMaterial("Danny_Stylized_Shoes", new Color(0.62f, 0.045f, 0.035f), 0.16f);
        return source;
    }

    private static void ApplyHoodieFabric(Material material)
    {
        string texturePath = StylizedMaterialFolder + "/Danny_PuffyCoat_Fabric.png";
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (texture == null)
        {
            const int size = 128;
            Texture2D generated = new(size, size, TextureFormat.RGBA32, false);
            generated.name = "Danny_PuffyCoat_Fabric";
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int hash = (x * 73856093) ^ (y * 19349663);
                    float grain = ((hash & 255) / 255f - 0.5f) * 0.075f;
                    float horizontalThread = (y % 5 == 0) ? -0.055f : 0f;
                    float verticalThread = (x % 7 == 0) ? 0.035f : 0f;
                    float paddedBand = 0.045f * Mathf.Cos((y / (float)size) * Mathf.PI * 8f);
                    int seamDistance = Math.Min(y % 32, 32 - (y % 32));
                    float quiltedSeam = seamDistance < 2 ? -0.16f : 0f;
                    float value = Mathf.Clamp01(0.88f + grain + horizontalThread + verticalThread + paddedBand + quiltedSeam);
                    generated.SetPixel(x, y, new Color(value, value, value, 1f));
                }
            }
            generated.Apply();
            File.WriteAllBytes(texturePath, generated.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(generated);
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.sRGBTexture = true;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = true;
                importer.maxTextureSize = 128;
                importer.SaveAndReimport();
            }
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        }
        if (texture == null) return;
        material.mainTexture = texture;
        material.mainTextureScale = new Vector2(2.0f, 1.0f);
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", new Vector2(2.0f, 1.0f));
        }
        EditorUtility.SetDirty(material);
    }

    private static Material CreateStylizedMaterial(string assetName, Color color, float smoothness)
    {
        if (!AssetDatabase.IsValidFolder(StylizedMaterialFolder))
            AssetDatabase.CreateFolder(Folder, "StylizedMaterials");
        string path = StylizedMaterialFolder + "/" + assetName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit")
            ?? Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard");
        if (material == null)
        {
            material = new Material(shader) { name = assetName };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader)
        {
            material.shader = shader;
        }
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_SpecColor")) material.SetColor("_SpecColor", Color.white * 0.12f);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Avatar LoadAvatar() =>
        AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();

    private static void ValidatePrefabAndClips(IReadOnlyDictionary<string, AnimationClip> clips)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new InvalidOperationException("Danny prefab validation failed: prefab missing");
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) throw new InvalidOperationException("Danny prefab validation failed: instantiation failed");
        try
        {
            Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
            if (animators.Length != 1 || animators[0].gameObject != instance)
                throw new InvalidOperationException($"Danny prefab validation failed: expected one Animator on the prefab root, found {animators.Length}");
            if (animators[0].runtimeAnimatorController == null)
                throw new InvalidOperationException("Danny prefab validation failed: root Animator has no controller");
            if (animators[0].avatar == null || !animators[0].avatar.isValid || !animators[0].avatar.isHuman)
                throw new InvalidOperationException("Danny prefab validation failed: Humanoid avatar is missing or invalid");

            SkinnedMeshRenderer[] renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length < 4)
                throw new InvalidOperationException($"Danny prefab validation failed: expected at least 4 skinned meshes, found {renderers.Length}");
            if (renderers.Any(r => !r.updateWhenOffscreen))
                throw new InvalidOperationException("Danny prefab validation failed: a skinned renderer can still be culled while animating");
            Transform[] transforms = instance.GetComponentsInChildren<Transform>(true);
            if (!transforms.Any(t => t.name == "DannyHairSway"))
                throw new InvalidOperationException("Danny prefab validation failed: hair-sway bone missing");
            HashSet<string> blendShapes = new();
            foreach (SkinnedMeshRenderer renderer in renderers)
                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                    blendShapes.Add(renderer.sharedMesh.GetBlendShapeName(i));
            foreach (string expected in new[] { "ElbowWrinkle", "TorsoWrinkle", "HipWrinkle", "KneeWrinkle" })
                if (!blendShapes.Any(n => n.Contains(expected, StringComparison.OrdinalIgnoreCase)))
                    Debug.LogWarning($"Danny wrinkle blend shape not exported in FBX: {expected}. Bone deformation remains active.");

            foreach (KeyValuePair<string, AnimationClip> pair in clips)
            {
                EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(pair.Value);
                string[] unresolvedPaths = pair.Value.humanMotion ? Array.Empty<string>() : bindings
                    .Where(b => b.type == typeof(Transform) && !string.IsNullOrEmpty(b.path))
                    .Select(b => b.path)
                    .Distinct()
                    .Where(path => instance.transform.Find(path) == null)
                    .ToArray();
                if (unresolvedPaths.Length > 0)
                    throw new InvalidOperationException($"Danny clip {pair.Key} has {unresolvedPaths.Length} bone paths that do not resolve from the Animator root. First: {unresolvedPaths[0]}");
                bool motionClip = !pair.Value.humanMotion && pair.Key is "DANNY_Stumble_Clumsy";
                bool hasUpperLeg = bindings.Any(b => b.path.Contains("UpLeg", StringComparison.OrdinalIgnoreCase));
                bool hasKnee = bindings.Any(b => b.path.EndsWith("LeftLeg", StringComparison.OrdinalIgnoreCase)
                    || b.path.EndsWith("RightLeg", StringComparison.OrdinalIgnoreCase));
                if (motionClip && (!hasUpperLeg || !hasKnee))
                    throw new InvalidOperationException($"Danny clip {pair.Key} is missing direct leg/knee curves ({bindings.Length} bindings)");
                pair.Value.SampleAnimation(instance, Mathf.Max(0f, pair.Value.length * 0.5f));
                Bounds combined = renderers[0].bounds;
                foreach (SkinnedMeshRenderer renderer in renderers.Skip(1)) combined.Encapsulate(renderer.bounds);
                float largest = Mathf.Max(combined.size.x, combined.size.y, combined.size.z);
                if (!float.IsFinite(largest) || largest < 0.2f || largest > 10f)
                    throw new InvalidOperationException($"Danny clip {pair.Key} produced invalid renderer bounds: {combined.size}");
            }
            int humanoidClips = clips.Count(pair => pair.Value.humanMotion);
            Debug.Log($"Danny prefab validation passed: valid Humanoid avatar, {humanoidClips} retargeted professional clips, all remaining curve paths resolved, {renderers.Length} always-visible skinned meshes, {blendShapes.Count} blend shapes.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    public static void DiagnosePoseBatch()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        AnimationClip walk = LoadExternalClip(StarterWalkPath, "Walk_N");
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(
            UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,
            UnityEditor.SceneManagement.NewSceneMode.Single);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null) throw new InvalidOperationException("Danny diagnostic instantiation failed");

        SkinnedMeshRenderer[] renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            Mesh mesh = renderer.sharedMesh;
            BoneWeight[] weights = mesh.boneWeights;
            int weighted = weights.Count(w => w.weight0 + w.weight1 + w.weight2 + w.weight3 > 0.001f);
            Debug.Log($"DANNY_RENDERER name={renderer.name} enabled={renderer.enabled} active={renderer.gameObject.activeInHierarchy} " +
                      $"mesh={mesh.name} verts={mesh.vertexCount} bones={renderer.bones.Length} root={renderer.rootBone?.name} " +
                      $"weighted={weighted}/{weights.Length} materials={string.Join(",", renderer.sharedMaterials.Select(m => m ? m.name : "NULL"))} " +
                      $"meshBounds={mesh.bounds} localBounds={renderer.localBounds} worldBounds={renderer.bounds}");
        }

        walk.SampleAnimation(instance, walk.length * 0.25f);
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            Mesh baked = new Mesh();
            renderer.BakeMesh(baked);
            Debug.Log($"DANNY_POSED name={renderer.name} bakedBounds={baked.bounds} worldBounds={renderer.bounds}");
            UnityEngine.Object.DestroyImmediate(baked);
        }

        GameObject lightObject = new("Diagnostic Light");
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.72f, 0.72f, 0.72f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.4f;
        lightObject.transform.rotation = Quaternion.Euler(45f, -25f, 0f);
        GameObject cameraObject = new("Diagnostic Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
        camera.transform.position = new Vector3(1.55f, 1.05f, -2.45f);
        camera.transform.LookAt(new Vector3(0f, 0.65f, 0f));
        camera.fieldOfView = 32f;
        RenderTexture target = new(800, 900, 24, RenderTextureFormat.ARGB32);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        Texture2D image = new(800, 900, TextureFormat.RGBA32, false);
        image.ReadPixels(new Rect(0, 0, 800, 900), 0, 0);
        image.Apply();
        File.WriteAllBytes("/tmp/danny-unity-posed.png", image.EncodeToPNG());
        Debug.Log("DANNY_DIAGNOSTIC_IMAGE /tmp/danny-unity-posed.png");
        UnityEngine.Object.DestroyImmediate(image);
        RenderTexture.active = null;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(target);
        EditorApplication.Exit(0);
    }
}
