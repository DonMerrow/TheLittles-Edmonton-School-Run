using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Imports the downloaded Moth Mixamo clips as Humanoid animations and builds
/// the controller expected by HumanoidLittleVisual. The first setup is
/// automatic; the menu command can be used to deliberately rebuild it.
/// </summary>
public static class ConfigureMothAnimations
{
    private const string ModelPath = "Assets/Resources/Littles/Moth.fbx";
    private const string AnimationFolder = "Assets/Resources/Littles/Animations";
    private const string ControllerPath = AnimationFolder + "/MothAnimator.controller";

    private sealed class ClipSpec
    {
        public readonly string File;
        public readonly string State;
        public readonly bool Loop;

        public ClipSpec(string file, string state, bool loop)
        {
            File = file;
            State = state;
            Loop = loop;
        }
    }

    private static readonly ClipSpec[] Clips =
    {
        new("Moth@Breathing Idle.fbx", "Idle", true),
        new("Moth@Walking.fbx", "Walk", true),
        new("Moth@Crawling.fbx", "Crawl", true),
        new("Moth@Jumping.fbx", "Jump", false),
        new("Moth@Falling Idle.fbx", "Fall", true),
        new("Moth@Falling To Landing.fbx", "Land", false)
    };

    [InitializeOnLoadMethod]
    private static void QueueAutomaticSetup()
    {
        EditorApplication.delayCall += SetupIfNeeded;
    }

    private static void SetupIfNeeded()
    {
        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) != null) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null) return;
        if (Clips.Any(spec => !File.Exists(spec.File.StartsWith("Assets/")
                ? spec.File
                : Path.Combine(AnimationFolder, spec.File)))) return;

        Build(false);
    }

    [MenuItem("The Littles/Configure Moth Animations")]
    public static void Rebuild()
    {
        Build(true);
    }

    private static void Build(bool reportMissing)
    {
        if (!AssetDatabase.IsValidFolder(AnimationFolder))
        {
            if (reportMissing) Debug.LogError("Create " + AnimationFolder + " and place the six Moth animation FBX files inside it.");
            return;
        }

        if (!ConfigureHumanoidImporter(ModelPath, false))
        {
            if (reportMissing) Debug.LogError("Moth.fbx was not found at " + ModelPath);
            return;
        }

        AnimationClip[] loaded = new AnimationClip[Clips.Length];
        for (int i = 0; i < Clips.Length; i++)
        {
            string path = AnimationFolder + "/" + Clips[i].File;
            if (!ConfigureHumanoidImporter(path, Clips[i].Loop))
            {
                if (reportMissing) Debug.LogError("Missing animation: " + path);
                return;
            }

            loaded[i] = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .FirstOrDefault(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase));
            if (loaded[i] == null)
            {
                Debug.LogError("No animation clip was found inside " + path);
                return;
            }
        }

        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Crawling", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attention", AnimatorControllerParameterType.Bool);
        controller.AddParameter("SocialAction", AnimatorControllerParameterType.Int);
        controller.AddParameter("Land", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = AddState(machine, Clips[0].State, loaded[0], 0, 0);
        AnimatorState walk = AddState(machine, Clips[1].State, loaded[1], 260, 0);
        AnimatorState crawl = AddState(machine, Clips[2].State, loaded[2], 260, 90);
        AnimatorState jump = AddState(machine, Clips[3].State, loaded[3], 0, 180);
        AnimatorState fall = AddState(machine, Clips[4].State, loaded[4], 260, 180);
        AnimatorState land = AddState(machine, Clips[5].State, loaded[5], 520, 180);
        machine.defaultState = idle;

        AddTransition(idle, walk, false,
            Condition(AnimatorConditionMode.Greater, 0.08f, "Speed"),
            Condition(AnimatorConditionMode.If, 0f, "Grounded"));
        AddTransition(walk, idle, false,
            Condition(AnimatorConditionMode.Less, 0.06f, "Speed"));

        AnimatorStateTransition crawlIn = machine.AddAnyStateTransition(crawl);
        Configure(crawlIn, false,
            Condition(AnimatorConditionMode.If, 0f, "Crawling"),
            Condition(AnimatorConditionMode.If, 0f, "Grounded"));
        AddTransition(crawl, idle, false,
            Condition(AnimatorConditionMode.IfNot, 0f, "Crawling"));

        AnimatorStateTransition jumpIn = machine.AddAnyStateTransition(jump);
        Configure(jumpIn, false,
            Condition(AnimatorConditionMode.IfNot, 0f, "Grounded"),
            Condition(AnimatorConditionMode.Greater, 0.05f, "VerticalSpeed"));
        AddTransition(jump, fall, false,
            Condition(AnimatorConditionMode.Less, 0f, "VerticalSpeed"));

        AnimatorStateTransition fallIn = machine.AddAnyStateTransition(fall);
        Configure(fallIn, false,
            Condition(AnimatorConditionMode.IfNot, 0f, "Grounded"),
            Condition(AnimatorConditionMode.Less, -0.05f, "VerticalSpeed"));

        AnimatorStateTransition landIn = machine.AddAnyStateTransition(land);
        Configure(landIn, false,
            Condition(AnimatorConditionMode.If, 0f, "Land"));
        AddTransition(land, idle, true);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Moth animation controller is ready: " + ControllerPath);
    }

    private static bool ConfigureHumanoidImporter(string assetPath, bool loop)
    {
        ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
        if (importer == null) return false;

        bool changed = importer.animationType != ModelImporterAnimationType.Human;
        importer.animationType = ModelImporterAnimationType.Human;

        if (assetPath != ModelPath)
        {
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].loopTime = loop;
                clips[i].loopPose = loop;
            }
            importer.clipAnimations = clips;
            changed = true;
        }

        if (changed) importer.SaveAndReimport();
        return true;
    }

    private static AnimatorState AddState(AnimatorStateMachine machine, string name,
        Motion motion, float x, float y)
    {
        AnimatorState state = machine.AddState(name, new Vector3(x, y));
        state.motion = motion;
        state.writeDefaultValues = true;
        return state;
    }

    private static AnimatorCondition Condition(AnimatorConditionMode mode, float threshold,
        string parameter)
    {
        return new AnimatorCondition { mode = mode, threshold = threshold, parameter = parameter };
    }

    private static void AddTransition(AnimatorState from, AnimatorState to, bool exitTime,
        params AnimatorCondition[] conditions)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        Configure(transition, exitTime, conditions);
    }

    private static void Configure(AnimatorStateTransition transition, bool exitTime,
        params AnimatorCondition[] conditions)
    {
        transition.hasExitTime = exitTime;
        transition.exitTime = exitTime ? 0.82f : 0f;
        transition.hasFixedDuration = true;
        transition.duration = exitTime ? 0.18f : 0.24f;
        transition.canTransitionToSelf = false;
        foreach (AnimatorCondition condition in conditions)
            transition.AddCondition(condition.mode, condition.threshold, condition.parameter);
    }
}
