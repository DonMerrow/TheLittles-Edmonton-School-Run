using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ConfigureWinterCast
{
    private const string Root = "Assets/Resources/Littles/WinterCast";
    private const string MaterialFolder = Root + "/Materials";
    private const string ControllerPath = Root + "/WinterCastAnimator.controller";
    private const string StarterAnimationFolder = "Assets/StarterAssets/ThirdPersonController/Character/Animations";

    private sealed class CastSpec
    {
        public string Name;
        public string Label;
        public string Role;
        public Color Skin;
        public Color Hair;
        public Color Coat;
        public Color Accent;
        public Color Pants;
        public Color Boots;
        public float SparkDrain;
        public bool Friendly;
        public string Prompt;
    }

    private static readonly CastSpec[] Cast =
    {
        Spec("Mom", "Danny's Mom", "chaser", C(0.50f,0.28f,0.18f), C(0.075f,0.025f,0.015f), C(0.06f,0.34f,0.39f), C(0.58f,0.08f,0.17f), C(0.035f,0.07f,0.12f), C(0.18f,0.055f,0.03f), 35f, false, "Come along, Pooky Bear."),
        Spec("Principal", "School Principal", "authority", C(0.66f,0.43f,0.29f), C(0.10f,0.07f,0.055f), C(0.20f,0.13f,0.34f), C(0.73f,0.55f,0.20f), C(0.05f,0.055f,0.075f), C(0.08f,0.055f,0.035f), 18f, false, "Danny, we have been expecting you."),
        Spec("CrossingGuard", "Crossing Guard", "guide", C(0.40f,0.23f,0.15f), C(0.035f,0.025f,0.020f), C(0.12f,0.20f,0.34f), C(0.96f,0.54f,0.035f), C(0.05f,0.08f,0.14f), C(0.10f,0.075f,0.045f), 4f, true, "Wait for the signal—then race the snowplow!"),
        Spec("HockeyGoalie", "Street Hockey Goalie", "hockey", C(0.57f,0.34f,0.22f), C(0.07f,0.03f,0.015f), C(0.67f,0.08f,0.08f), C(0.92f,0.86f,0.70f), C(0.06f,0.07f,0.10f), C(0.08f,0.07f,0.06f), 7f, true, "Bet you cannot score before the bell."),
        Spec("HockeyWinger", "Street Hockey Winger", "hockey", C(0.73f,0.51f,0.34f), C(0.16f,0.08f,0.025f), C(0.06f,0.32f,0.68f), C(0.95f,0.80f,0.12f), C(0.025f,0.055f,0.12f), C(0.10f,0.08f,0.06f), 7f, true, "One quick game, Danny!"),
        Spec("SnowFortKid", "Snow Fort Kid", "snow_fort", C(0.44f,0.25f,0.16f), C(0.055f,0.025f,0.012f), C(0.40f,0.13f,0.62f), C(0.16f,0.74f,0.77f), C(0.04f,0.08f,0.16f), C(0.14f,0.07f,0.035f), 6f, true, "The fort needs one more tower!"),
        Spec("SledKid", "Sledding Kid", "sledding", C(0.63f,0.40f,0.25f), C(0.12f,0.055f,0.018f), C(0.89f,0.29f,0.05f), C(0.08f,0.54f,0.38f), C(0.08f,0.10f,0.16f), C(0.16f,0.075f,0.035f), 8f, true, "The hill is perfect right now!"),
        Spec("ClassmateMaya", "Maya", "classmate", C(0.36f,0.20f,0.13f), C(0.025f,0.012f,0.009f), C(0.73f,0.11f,0.44f), C(0.22f,0.72f,0.82f), C(0.045f,0.075f,0.13f), C(0.12f,0.065f,0.04f), 3f, true, "Did you finish the snow-creature drawing?"),
        Spec("NeighbourRose", "Neighbour Rose", "neighbour", C(0.69f,0.48f,0.33f), C(0.46f,0.44f,0.40f), C(0.35f,0.46f,0.16f), C(0.78f,0.25f,0.10f), C(0.06f,0.065f,0.075f), C(0.14f,0.075f,0.04f), 5f, true, "Would you help me find my mitten?"),
        Spec("DogWalkerTheo", "Theo", "pedestrian", C(0.55f,0.32f,0.20f), C(0.035f,0.022f,0.015f), C(0.18f,0.28f,0.48f), C(0.76f,0.42f,0.08f), C(0.035f,0.05f,0.08f), C(0.11f,0.07f,0.04f), 5f, true, "My dog found something under the snow."),
    };

    private static CastSpec Spec(string name, string label, string role, Color skin,
        Color hair, Color coat, Color accent, Color pants, Color boots,
        float drain, bool friendly, string prompt) => new()
    {
        Name=name, Label=label, Role=role, Skin=skin, Hair=hair, Coat=coat,
        Accent=accent, Pants=pants, Boots=boots, SparkDrain=drain,
        Friendly=friendly, Prompt=prompt
    };

    private static Color C(float r, float g, float b) => new(r,g,b,1f);

    [MenuItem("Tools/The Littles/Configure Winter Cast")]
    public static void ConfigureMenu() => Configure(false, true);

    public static void ConfigureMomBatch()
    {
        Configure(true, false, "Mom");
        EditorApplication.Exit(0);
    }

    public static void ConfigureBatch()
    {
        Configure(true, true);
        EditorApplication.Exit(0);
    }

    private static void Configure(bool batch, bool requireAll, string only = null)
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        CastSpec[] targets = Cast.Where(s => only == null || s.Name == only).ToArray();
        foreach (CastSpec spec in targets)
        {
            string modelPath = ModelPath(spec);
            ModelImporter importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null)
            {
                if (requireAll) throw new FileNotFoundException("Winter cast model missing", modelPath);
                continue;
            }
            importer.globalScale = 1f;
            importer.useFileScale = true;
            importer.importAnimation = false;
            importer.importBlendShapes = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.SaveAndReimport();
        }

        RuntimeAnimatorController controller = CreateController();
        foreach (CastSpec spec in targets)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(spec)) == null) continue;
            CreatePrefab(spec, controller);
            Validate(spec);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("Winter cast configured successfully: " + string.Join(", ", targets.Select(s => s.Name)));
    }

    private static string ModelPath(CastSpec spec) => $"{Root}/{spec.Name}/{spec.Name}.fbx";
    private static string PrefabPath(CastSpec spec) => $"{Root}/{spec.Name}/{spec.Name}.prefab";

    private static RuntimeAnimatorController CreateController()
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder(Root, "Materials");
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        AnimatorState state = controller.layers[0].stateMachine.AddState("Winter Locomotion");
        controller.layers[0].stateMachine.defaultState = state;
        BlendTree blend = new() { name="Winter Cast Locomotion", blendType=BlendTreeType.Simple1D,
            blendParameter="Speed", useAutomaticThresholds=false };
        AssetDatabase.AddObjectToAsset(blend, controller);
        blend.AddChild(LoadClip("Stand--Idle.anim.fbx", "Idle"), 0f);
        blend.AddChild(LoadClip("Locomotion--Walk_N.anim.fbx", "Walk_N"), 2f);
        blend.AddChild(LoadClip("Locomotion--Run_N.anim.fbx", "Run_N"), 5f);
        state.motion = blend;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimationClip LoadClip(string file, string preferred)
    {
        string path = StarterAnimationFolder + "/" + file;
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
            .FirstOrDefault(c => c.name.Equals(preferred, StringComparison.OrdinalIgnoreCase))
            ?? AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal));
        if (clip == null) throw new FileNotFoundException("Winter cast animation missing", path);
        return clip;
    }

    private static void CreatePrefab(CastSpec spec, RuntimeAnimatorController controller)
    {
        string prefabPath = PrefabPath(spec);
        AssetDatabase.DeleteAsset(prefabPath);
        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(spec));
        GameObject instance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (instance == null) throw new InvalidOperationException("Could not instantiate " + spec.Name);
        instance.name = spec.Name;
        Animator[] animators = instance.GetComponentsInChildren<Animator>(true);
        Avatar avatar = animators.Select(a => a.avatar).FirstOrDefault(a => a != null)
            ?? AssetDatabase.LoadAllAssetsAtPath(ModelPath(spec)).OfType<Avatar>().FirstOrDefault();
        Animator animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
        animator.avatar = avatar;
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (animator.GetComponent<WinterAnimationEventRelay>() == null)
            animator.gameObject.AddComponent<WinterAnimationEventRelay>();
        foreach (Animator child in animators)
            if (child != animator) UnityEngine.Object.DestroyImmediate(child);

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterial = ResolveMaterial(spec, renderer.name);
            if (renderer is SkinnedMeshRenderer skinned)
            {
                skinned.updateWhenOffscreen = true;
                Bounds bounds = skinned.localBounds;
                bounds.Expand(Vector3.one * 1.5f);
                skinned.localBounds = bounds;
            }
        }

        WinterCastIdentity identity = instance.GetComponent<WinterCastIdentity>();
        if (identity == null)
            identity = instance.AddComponent<WinterCastIdentity>();
        identity.Configure(spec.Label, spec.Role, spec.SparkDrain, spec.Friendly, spec.Prompt);
        if (instance.GetComponent<WinterCastInteraction>() == null)
            instance.AddComponent<WinterCastInteraction>();
        WinterCastNpcMotor motor = instance.GetComponent<WinterCastNpcMotor>();
        if (motor == null)
            motor = instance.AddComponent<WinterCastNpcMotor>();
        motor.Configure(spec.Role == "chaser", spec.Role == "chaser" ? 3.1f : 0f, 1.30f);

        Renderer[] visibleRenderers = instance.GetComponentsInChildren<Renderer>(true);
        Bounds visibleBounds = visibleRenderers[0].bounds;
        foreach (Renderer renderer in visibleRenderers.Skip(1)) visibleBounds.Encapsulate(renderer.bounds);
        CapsuleCollider interactionZone = instance.GetComponent<CapsuleCollider>();
        if (interactionZone == null)
            interactionZone = instance.AddComponent<CapsuleCollider>();
        interactionZone.isTrigger = true;
        interactionZone.center = instance.transform.InverseTransformPoint(visibleBounds.center);
        interactionZone.height = Mathf.Max(1f, visibleBounds.size.y + 0.40f);
        interactionZone.radius = Mathf.Max(0.65f, visibleBounds.extents.x + 0.25f);
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        UnityEngine.Object.DestroyImmediate(instance);
    }

    private static Material ResolveMaterial(CastSpec spec, string rendererName)
    {
        Color color;
        string part;
        if (rendererName.Contains("EyeWhite")) { color=C(0.93f,0.95f,0.96f); part="EyeWhite"; }
        else if (rendererName.Contains("EyeDark") || rendererName.Contains("Face_Mouth")) { color=C(0.018f,0.010f,0.008f); part="EyeDark"; }
        else if (rendererName.Contains("Hair") || rendererName.Contains("Face_Brow")) { color=spec.Hair; part="Hair"; }
        else if (rendererName.Contains("Skin")) { color=spec.Skin; part="Skin"; }
        else if (rendererName.Contains("Pants")) { color=spec.Pants; part="Pants"; }
        else if (rendererName.Contains("Boots")) { color=spec.Boots; part="Boots"; }
        else if (rendererName.Contains("Accent") || rendererName.Contains("Accessory")) { color=spec.Accent; part="Accent"; }
        else if (rendererName.Contains("Metal")) { color=C(0.32f,0.35f,0.38f); part="Metal"; }
        else { color=spec.Coat; part="Coat"; }
        return CreateMaterial(spec.Name + "_" + part, color, part == "Metal" ? 0.42f : 0.08f);
    }

    private static Material CreateMaterial(string name, Color color, float smoothness)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");
        if (material == null)
        {
            material = new Material(shader) { name=name };
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader != shader) material.shader = shader;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.color = color;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void Validate(CastSpec spec)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(spec));
        if (prefab == null) throw new InvalidOperationException(spec.Name + " prefab missing");
        Animator animator = prefab.GetComponent<Animator>();
        if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
            throw new InvalidOperationException(spec.Name + " Humanoid avatar is invalid");
        if (prefab.GetComponent<WinterCastIdentity>() == null)
            throw new InvalidOperationException(spec.Name + " identity metadata is missing");
        if (prefab.GetComponent<WinterCastInteraction>() == null ||
            prefab.GetComponent<WinterCastNpcMotor>() == null ||
            prefab.GetComponent<CapsuleCollider>() == null)
            throw new InvalidOperationException(spec.Name + " interaction components are missing");
        if (prefab.GetComponentsInChildren<Renderer>(true).Length < 12)
            throw new InvalidOperationException(spec.Name + " has incomplete visible meshes");
        Debug.Log($"Winter cast prefab validated: {spec.Name}, Humanoid avatar valid, role={spec.Role}");
    }
}
