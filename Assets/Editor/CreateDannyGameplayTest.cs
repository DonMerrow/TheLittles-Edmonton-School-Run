using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreateDannyGameplayTest
{
    private const string PrefabPath = "Assets/Resources/Littles/Danny/Danny.prefab";
    private const string ScenePath = "Assets/Scenes/DannyGameplayTest.unity";
    private const string MaterialFolder = "Assets/Resources/Littles/Danny/TestMaterials";

    [MenuItem("Tools/The Littles/Create Danny Gameplay Test Scene")]
    public static void CreateFromMenu() => Create(false);

    public static void CreateBatch()
    {
        Create(true);
        EditorApplication.Exit(0);
    }

    private static void Create(bool batchMode)
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) throw new InvalidOperationException("Danny prefab is missing: " + PrefabPath);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "DannyGameplayTest";

        Material snowMaterial = CreateMaterial("DannyTest_Snow", new Color(0.82f, 0.90f, 0.96f));
        Material roadMaterial = CreateMaterial("DannyTest_WinterRoad", new Color(0.12f, 0.16f, 0.20f));
        Material sidewalkMaterial = CreateMaterial("DannyTest_SnowySidewalk", new Color(0.62f, 0.70f, 0.76f));
        Material iceMaterial = CreateMaterial("DannyTest_Ice", new Color(0.34f, 0.62f, 0.74f));
        Material lampMaterial = CreateMaterial("DannyTest_Lamp", new Color(0.08f, 0.10f, 0.13f));
        Material warmLightMaterial = CreateMaterial("DannyTest_WarmLamp", new Color(1.0f, 0.68f, 0.22f));

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Snow Covered Ground";
        ground.transform.position = new Vector3(0f, -0.10f, 0f);
        ground.transform.localScale = new Vector3(24f, 0.20f, 24f);
        ground.GetComponent<Renderer>().sharedMaterial = snowMaterial;

        CreateObstacle("Snowy Street", new Vector3(0f, 0.012f, 0f), new Vector3(6.2f, 0.024f, 24f), roadMaterial);
        CreateObstacle("Snowy Sidewalk Left", new Vector3(-4.15f, 0.055f, 0f), new Vector3(2.0f, 0.11f, 24f), sidewalkMaterial);
        CreateObstacle("Snowy Sidewalk Right", new Vector3(4.15f, 0.055f, 0f), new Vector3(2.0f, 0.11f, 24f), sidewalkMaterial);
        CreateObstacle("Snowbank Jump", new Vector3(0f, 0.20f, 5f), new Vector3(2.2f, 0.36f, 1.2f), snowMaterial);
        CreateObstacle("Icy Curb Left", new Vector3(-3.4f, 0.34f, 1.5f), new Vector3(1.25f, 0.58f, 3f), iceMaterial);
        CreateObstacle("Icy Curb Right", new Vector3(3.4f, 0.48f, 3.5f), new Vector3(1.25f, 0.86f, 3f), iceMaterial);
        CreateSnowPile("Snow Pile Left", new Vector3(-3.0f, 0.14f, -2.8f), new Vector3(1.5f, 0.30f, 0.75f), snowMaterial);
        CreateSnowPile("Snow Pile Right", new Vector3(3.0f, 0.12f, -1.4f), new Vector3(1.3f, 0.26f, 0.68f), snowMaterial);
        CreateStreetLight("Winter Street Light Left", new Vector3(-4.25f, 0f, -2.4f), lampMaterial, warmLightMaterial);
        CreateStreetLight("Winter Street Light Right", new Vector3(4.25f, 0f, 3.8f), lampMaterial, warmLightMaterial);

        GameObject danny = new("Danny Player");
        danny.transform.position = new Vector3(0f, 0.02f, 0f);
        GameObject visual = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (visual == null) throw new InvalidOperationException("Danny prefab could not be instantiated");
        visual.name = "Danny Visual";
        visual.transform.SetParent(danny.transform, false);
        // Danny's shoe sole extends just below the imported rig origin. Lift
        // only the artwork, not the controller, so the feet rest on the floor.
        visual.transform.localPosition = new Vector3(0f, 0.045f, 0f);
        if (visual.GetComponent<DannyFootGrounding>() == null)
            visual.AddComponent<DannyFootGrounding>();
        CharacterController capsule = danny.AddComponent<CharacterController>();
        capsule.height = 1.32f;
        capsule.radius = 0.24f;
        capsule.center = new Vector3(0f, 0.66f, 0f);
        capsule.stepOffset = 0.24f;
        capsule.skinWidth = 0.025f;
        if (danny.GetComponent<DannyTestController>() == null) danny.AddComponent<DannyTestController>();

        GameObject cameraObject = new("Danny Follow Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 1.55f, -3.2f);
        camera.nearClipPlane = 0.05f;
        camera.fieldOfView = 55f;
        cameraObject.AddComponent<AudioListener>();
        DannyFollowCamera follow = cameraObject.AddComponent<DannyFollowCamera>();
        follow.Configure(danny.transform);

        GameObject lightObject = new("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.05f;
        light.color = new Color(0.78f, 0.88f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.48f, 0.56f, 0.66f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.72f, 0.80f, 0.88f);
        RenderSettings.fogStartDistance = 10f;
        RenderSettings.fogEndDistance = 34f;

        GameObject instructions = new("Controls - WASD move, Shift run, Space hop, right-drag or Z/X look, R camera reset, wheel zoom, T stumble, E interact");
        instructions.transform.position = Vector3.zero;

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        ValidateScene();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        Debug.Log("Danny gameplay test scene created and validated: " + ScenePath);
        if (!batchMode) Selection.activeGameObject = danny;
    }

    private static Material CreateMaterial(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder(MaterialFolder))
        {
            string parent = "Assets/Resources/Littles/Danny";
            AssetDatabase.CreateFolder(parent, "TestMaterials");
        }
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
        }
        else material.color = color;
        return material;
    }

    private static void CreateObstacle(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = name;
        obstacle.transform.position = position;
        obstacle.transform.localScale = scale;
        obstacle.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static void CreateSnowPile(string name, Vector3 position, Vector3 scale, Material material)
    {
        GameObject pile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pile.name = name;
        pile.transform.position = position;
        pile.transform.localScale = scale;
        pile.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(pile.GetComponent<Collider>());
    }

    private static void CreateStreetLight(string name, Vector3 position, Material poleMaterial, Material glowMaterial)
    {
        GameObject root = new(name);
        root.transform.position = position;

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Pole";
        pole.transform.SetParent(root.transform, false);
        pole.transform.localPosition = new Vector3(0f, 1.45f, 0f);
        pole.transform.localScale = new Vector3(0.055f, 1.45f, 0.055f);
        pole.GetComponent<Renderer>().sharedMaterial = poleMaterial;
        UnityEngine.Object.DestroyImmediate(pole.GetComponent<Collider>());

        GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lamp.name = "Warm Lamp";
        lamp.transform.SetParent(root.transform, false);
        lamp.transform.localPosition = new Vector3(0f, 2.92f, 0f);
        lamp.transform.localScale = new Vector3(0.24f, 0.16f, 0.24f);
        lamp.GetComponent<Renderer>().sharedMaterial = glowMaterial;
        UnityEngine.Object.DestroyImmediate(lamp.GetComponent<Collider>());

        Light light = lamp.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.65f, 0.30f);
        light.intensity = 1.4f;
        light.range = 5f;
    }

    private static void AddSceneToBuildSettings(string path)
    {
        EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
        if (current.Any(s => s.path == path)) return;
        EditorBuildSettings.scenes = current.Concat(new[] { new EditorBuildSettingsScene(path, true) }).ToArray();
    }

    private static void ValidateScene()
    {
        GameObject player = GameObject.Find("Danny Player");
        if (player == null) throw new InvalidOperationException("Danny scene validation failed: player missing");
        Animator animator = player.GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null)
            throw new InvalidOperationException("Danny scene validation failed: configured Animator missing");
        if (player.GetComponent<CharacterController>() == null || player.GetComponent<DannyTestController>() == null)
            throw new InvalidOperationException("Danny scene validation failed: movement components missing");
        if (Camera.main == null || Camera.main.GetComponent<DannyFollowCamera>() == null)
            throw new InvalidOperationException("Danny scene validation failed: follow camera missing");
        if (!player.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any())
            throw new InvalidOperationException("Danny scene validation failed: skinned renderer missing");
    }
}
