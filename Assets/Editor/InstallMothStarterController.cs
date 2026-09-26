using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Joins the finished Starter Assets third-person movement with the exported
/// Moth humanoid and the supplied Mixamo crawl cycle. It runs once after import,
/// so the delivered project does not require hand-editing an Animator graph.
/// </summary>
public static class InstallMothStarterController
{
    // The exported Moth FBX already faces the same forward direction as the
    // Starter Assets PlayerArmature. Rotating the visual 180 degrees makes the
    // locomotion clips appear to walk and crawl backwards.
    private const float MothVisualYaw = 0f;
    private const string MothModelPath = "Assets/Resources/Littles/Moth.fbx";
    private const string CrawlPath = "Assets/Resources/Littles/Animations/Moth@CrawlingNew.fbx";
    private const string UalAnimationPath =
        "Assets/Resources/Littles/Animations/UAL2_Standard.fbx";
    private const string StarterControllerPath =
        "Assets/StarterAssets/ThirdPersonController/Character/Animations/StarterAssetsThirdPerson.controller";
    private const string MothControllerPath =
        "Assets/Resources/Littles/Animations/MothStarterAssets.controller";
    private const string PlayerPrefabPath =
        "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";

    [InitializeOnLoadMethod]
    private static void QueueInstall()
    {
        EditorApplication.delayCall += InstallIfNeeded;
    }

    private static void InstallIfNeeded()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        Transform mothVisual = prefab != null ? FindDeepChild(prefab.transform, "MothVisual") : null;
        bool hasController = AssetDatabase.LoadAssetAtPath<AnimatorController>(MothControllerPath) != null;
        AnimatorController controller =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(MothControllerPath);
        bool hasAnimationUpgrade = controller != null && controller.parameters.Any(parameter =>
            parameter.name == "LittlesMantleOnlyV21" &&
            parameter.type == AnimatorControllerParameterType.Bool);
        bool hasGrounding = prefab != null && prefab.GetComponent<MothGrounding>() != null;
        bool hasCorrectFacing = mothVisual != null && Quaternion.Angle(
            mothVisual.localRotation, Quaternion.Euler(0f, MothVisualYaw, 0f)) < 0.5f;

        // This orientation check also upgrades the first crawl build, whose
        // installer accidentally saved Moth facing backwards.
        if (hasController && hasCorrectFacing && hasGrounding && hasAnimationUpgrade) return;
        Install(false);
    }

    [MenuItem("The Littles/Install Moth on Starter Assets")]
    public static void Reinstall()
    {
        Install(true);
    }

    private static void Install(bool force)
    {
        if (!File.Exists(MothModelPath) || !File.Exists(CrawlPath) ||
            !File.Exists(UalAnimationPath) ||
            !File.Exists(StarterControllerPath) || !File.Exists(PlayerPrefabPath))
        {
            if (force) Debug.LogError(
                "Moth, UAL2 animations, the crawl clip, or Unity Starter Assets is missing.");
            return;
        }

        ConfigureHumanoid(MothModelPath, false);
        ConfigureHumanoid(CrawlPath, true);
        ConfigureHumanoid(UalAnimationPath, false);
        ConfigureUalClips();

        if (!BuildController(force)) return;
        if (!InstallVisual()) return;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Moth is installed with the normal Starter Assets jump and UAL2 mantle.");
    }

    private static void ConfigureHumanoid(string path, bool crawl)
    {
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;

        bool changed = importer.animationType != ModelImporterAnimationType.Human;
        importer.animationType = ModelImporterAnimationType.Human;
        if (crawl)
        {
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++)
            {
                ModelImporterClipAnimation clip = clips[i];
                if (!clip.loopTime || !clip.loopPose || !clip.lockRootPositionXZ ||
                    !clip.lockRootHeightY || !clip.lockRootRotation || !clip.heightFromFeet)
                    changed = true;
                clip.loopTime = true;
                clip.loopPose = true;
                clip.lockRootPositionXZ = true;
                clip.lockRootHeightY = true;
                clip.lockRootRotation = true;
                clip.heightFromFeet = true;
            }
            importer.clipAnimations = clips;
        }

        if (changed) importer.SaveAndReimport();
    }

    private static void ConfigureUalClips()
    {
        ModelImporter importer = AssetImporter.GetAtPath(UalAnimationPath) as ModelImporter;
        if (importer == null) return;
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        bool changed = false;
        for (int i = 0; i < clips.Length; i++)
        {
            ModelImporterClipAnimation clip = clips[i];
            bool shouldLoop = clip.name.IndexOf("Idle_Loop", StringComparison.OrdinalIgnoreCase) >= 0;
            if (clip.loopTime != shouldLoop || !clip.lockRootPositionXZ ||
                !clip.lockRootHeightY || !clip.lockRootRotation || !clip.heightFromFeet)
                changed = true;
            clip.loopTime = shouldLoop;
            clip.loopPose = shouldLoop;
            clip.lockRootPositionXZ = true;
            clip.lockRootHeightY = true;
            clip.lockRootRotation = true;
            clip.heightFromFeet = true;
        }
        importer.clipAnimations = clips;
        if (changed) importer.SaveAndReimport();
    }

    private static bool BuildController(bool force)
    {
        if (force) AssetDatabase.DeleteAsset(MothControllerPath);
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(MothControllerPath) == null)
        {
            if (!AssetDatabase.CopyAsset(StarterControllerPath, MothControllerPath))
            {
                Debug.LogError("Could not copy the Starter Assets Animator controller.");
                return false;
            }
            AssetDatabase.ImportAsset(MothControllerPath, ImportAssetOptions.ForceSynchronousImport);
        }

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(MothControllerPath);
        AnimatorController starterController =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(StarterControllerPath);
        AnimationClip crawlClip = AssetDatabase.LoadAllAssetsAtPath(CrawlPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
        AnimationClip[] ualClips = AssetDatabase.LoadAllAssetsAtPath(UalAnimationPath)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        AnimationClip mantleClip = FindClip(ualClips, "ClimbUp_1m");
        if (controller == null || starterController == null || crawlClip == null || mantleClip == null)
        {
            Debug.LogError("Could not load the Moth controller, Starter controller, or mantle clip.");
            return false;
        }

        AddParameter(controller, "Crawling", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "CrawlMotionSpeed", AnimatorControllerParameterType.Float);
        AddParameter(controller, "Mantling", AnimatorControllerParameterType.Bool);
        AddParameter(controller, "LittlesMantleOnlyV21", AnimatorControllerParameterType.Bool);

        // Free wall climbing and its procedural IK are intentionally disabled.
        AnimatorControllerLayer[] layers = controller.layers;
        layers[0].iKPass = false;
        controller.layers = layers;

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState crawl = machine.states.Select(child => child.state)
            .FirstOrDefault(state => state.name == "Moth Crawl");
        if (crawl == null)
        {
            crawl = machine.AddState("Moth Crawl", new Vector3(620f, 260f));
            crawl.writeDefaultValues = true;

            AnimatorStateTransition enter = machine.AddAnyStateTransition(crawl);
            enter.hasExitTime = false;
            enter.hasFixedDuration = true;
            enter.duration = 0.16f;
            enter.canTransitionToSelf = false;
            enter.AddCondition(AnimatorConditionMode.If, 0f, "Crawling");
            enter.AddCondition(AnimatorConditionMode.If, 0f, "Grounded");

            AnimatorState idle = machine.states.Select(child => child.state)
                .FirstOrDefault(state => state.name == "Idle Walk Run Blend");
            if (idle != null)
            {
                AnimatorStateTransition leave = crawl.AddTransition(idle);
                leave.hasExitTime = false;
                leave.hasFixedDuration = true;
                leave.duration = 0.18f;
                leave.AddCondition(AnimatorConditionMode.IfNot, 0f, "Crawling");
            }
        }

        crawl.motion = crawlClip;
        crawl.speedParameter = "CrawlMotionSpeed";
        crawl.speedParameterActive = true;
        EditorUtility.SetDirty(crawl);

        CopyStateMotion(starterController.layers[0].stateMachine, machine, "JumpStart");
        CopyStateMotion(starterController.layers[0].stateMachine, machine, "InAir");
        CopyStateMotion(starterController.layers[0].stateMachine, machine, "JumpLand");

        AnimatorState mantle = machine.states.Select(child => child.state)
            .FirstOrDefault(state => state.name == "Little Mantle");
        if (mantle == null)
        {
            mantle = machine.AddState("Little Mantle", new Vector3(820f, 410f));
            mantle.writeDefaultValues = true;
            AnimatorStateTransition enterMantle = machine.AddAnyStateTransition(mantle);
            enterMantle.hasExitTime = false;
            enterMantle.hasFixedDuration = true;
            enterMantle.duration = 0.08f;
            enterMantle.canTransitionToSelf = false;
            enterMantle.AddCondition(AnimatorConditionMode.If, 0f, "Mantling");

            AnimatorState idle = machine.states.Select(child => child.state)
                .FirstOrDefault(state => state.name == "Idle Walk Run Blend");
            if (idle != null)
            {
                AnimatorStateTransition leaveMantle = mantle.AddTransition(idle);
                leaveMantle.hasExitTime = false;
                leaveMantle.hasFixedDuration = true;
                leaveMantle.duration = 0.12f;
                leaveMantle.AddCondition(AnimatorConditionMode.IfNot, 0f, "Mantling");
            }
        }
        mantle.motion = mantleClip;
        EditorUtility.SetDirty(mantle);
        EditorUtility.SetDirty(controller);
        return true;
    }

    private static AnimationClip FindClip(AnimationClip[] clips, string name)
    {
        return clips.FirstOrDefault(clip => string.Equals(clip.name, name,
                   StringComparison.OrdinalIgnoreCase)) ??
               clips.FirstOrDefault(clip => clip.name.EndsWith(name,
                   StringComparison.OrdinalIgnoreCase));
    }

    private static void SetStateMotion(AnimatorStateMachine machine, string stateName,
        AnimationClip clip)
    {
        AnimatorState state = machine.states.Select(child => child.state)
            .FirstOrDefault(candidate => candidate.name == stateName);
        if (state == null) return;
        state.motion = clip;
        EditorUtility.SetDirty(state);
    }

    private static void CopyStateMotion(AnimatorStateMachine source,
        AnimatorStateMachine target, string stateName)
    {
        AnimatorState sourceState = source.states.Select(child => child.state)
            .FirstOrDefault(candidate => candidate.name == stateName);
        if (sourceState == null || sourceState.motion == null) return;
        AnimatorState targetState = target.states.Select(child => child.state)
            .FirstOrDefault(candidate => candidate.name == stateName);
        if (targetState == null) return;
        targetState.motion = sourceState.motion;
        EditorUtility.SetDirty(targetState);
    }

    private static void AddParameter(AnimatorController controller, string name,
        AnimatorControllerParameterType type)
    {
        if (controller.parameters.Any(parameter => parameter.name == name)) return;
        controller.AddParameter(name, type);
    }

    private static bool InstallVisual()
    {
        GameObject mothAsset = AssetDatabase.LoadAssetAtPath<GameObject>(MothModelPath);
        Avatar mothAvatar = AssetDatabase.LoadAllAssetsAtPath(MothModelPath).OfType<Avatar>().FirstOrDefault();
        RuntimeAnimatorController mothController =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MothControllerPath);
        if (mothAsset == null || mothAvatar == null || mothController == null) return false;

        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            RemoveChild(root.transform, "Geometry");
            RemoveChild(root.transform, "Skeleton");
            RemoveChild(root.transform, "MothVisual");

            GameObject visual = PrefabUtility.InstantiatePrefab(mothAsset, root.transform) as GameObject;
            if (visual == null) return false;
            visual.name = "MothVisual";
            visual.transform.SetLocalPositionAndRotation(
                Vector3.zero, Quaternion.Euler(0f, MothVisualYaw, 0f));
            visual.transform.localScale = Vector3.one;

            foreach (Animator nested in visual.GetComponentsInChildren<Animator>(true))
                UnityEngine.Object.DestroyImmediate(nested);

            FitVisual(root.transform, visual.transform, 1.72f);
            SetLayerRecursively(visual, root.layer);

            Animator animator = root.GetComponent<Animator>();
            if (animator == null) animator = root.AddComponent<Animator>();
            animator.avatar = mothAvatar;
            animator.runtimeAnimatorController = mothController;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            MothGrounding grounding = root.GetComponent<MothGrounding>();
            if (grounding == null) grounding = root.AddComponent<MothGrounding>();
            grounding.VisualRoot = visual.transform;
            grounding.CharacterController = root.GetComponent<CharacterController>();
            grounding.Animator = animator;
            grounding.MaximumCorrection = 1.25f;
            grounding.GroundingSpeed = 12f;
            grounding.SoleClearance = 0.01f;

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void FitVisual(Transform player, Transform visual, float targetHeight)
    {
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        float scale = targetHeight / Mathf.Max(0.01f, bounds.size.y);
        visual.localScale *= scale;

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        Vector3 localCenter = player.InverseTransformPoint(bounds.center);
        Vector3 localMinimum = player.InverseTransformPoint(bounds.min);
        visual.localPosition += new Vector3(-localCenter.x, -localMinimum.y, -localCenter.z);
    }

    private static void RemoveChild(Transform root, string name)
    {
        Transform child = FindDeepChild(root, name);
        if (child != null && child != root) UnityEngine.Object.DestroyImmediate(child.gameObject);
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform) SetLayerRecursively(child.gameObject, layer);
    }
}
