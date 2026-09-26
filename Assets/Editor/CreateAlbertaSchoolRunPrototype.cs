using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreateAlbertaSchoolRunPrototype
{
    private const string SourceScene = "Assets/Scenes/DannyGameplayTest.unity";
    private const string ScenePath = "Assets/Scenes/AlbertaSchoolRunPrototype.unity";
    private const string CastRoot = "Assets/Resources/Littles/WinterCast";
    private const string TestMaterials = "Assets/Resources/Littles/Danny/TestMaterials";

    private static readonly (string name, Vector3 position, float yaw)[] Placements =
    {
        ("Mom", new Vector3(0f, 0.03f, -7.0f), 0f),
        ("Principal", new Vector3(0f, 0.03f, 9.6f), 180f),
        ("CrossingGuard", new Vector3(-4.0f, 0.12f, -0.8f), 70f),
        ("HockeyGoalie", new Vector3(2.25f, 0.04f, 2.9f), 210f),
        ("HockeyWinger", new Vector3(1.15f, 0.04f, 1.5f), 35f),
        ("SnowFortKid", new Vector3(-4.05f, 0.12f, 3.2f), 95f),
        ("SledKid", new Vector3(4.0f, 0.12f, -3.2f), 265f),
        ("ClassmateMaya", new Vector3(-3.9f, 0.12f, 6.0f), 105f),
        ("NeighbourRose", new Vector3(4.0f, 0.12f, 6.6f), 245f),
        ("DogWalkerTheo", new Vector3(-4.0f, 0.12f, -5.2f), 80f),
    };

    [MenuItem("Tools/The Littles/Create Alberta School Run Prototype")]
    public static void CreateMenu() => Create(false);

    public static void CreateBatch()
    {
        Create(true);
        EditorApplication.Exit(0);
    }

    private static void Create(bool batch)
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Scene scene = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
        if (!scene.IsValid()) throw new InvalidOperationException("Danny gameplay source scene could not be opened");

        GameObject previous = GameObject.Find("Winter Cast and Distractions");
        if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
        GameObject castParent = new("Winter Cast and Distractions");

        foreach ((string name, Vector3 position, float yaw) in Placements)
        {
            string prefabPath = $"{CastRoot}/{name}/{name}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) throw new InvalidOperationException("Cast prefab missing: " + prefabPath);
            GameObject npc = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (npc == null) throw new InvalidOperationException("Could not instantiate " + name);
            npc.name = name;
            npc.transform.SetParent(castParent.transform, true);
            npc.transform.position = position;
            npc.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        GameObject danny = GameObject.Find("Danny Player");
        if (danny == null) throw new InvalidOperationException("Danny Player is missing");
        DannySpark spark = danny.GetComponent<DannySpark>() ?? danny.AddComponent<DannySpark>();
        if (danny.GetComponent<DannySparkHUD>() == null) danny.AddComponent<DannySparkHUD>();
        Camera camera = Camera.main;
        if (camera == null) throw new InvalidOperationException("Main camera is missing");
        SparkWorldMood mood = camera.GetComponent<SparkWorldMood>() ?? camera.gameObject.AddComponent<SparkWorldMood>();
        mood.Configure(spark);

        BuildHockeyCorner(castParent.transform);
        BuildSnowFort(castParent.transform);
        BuildSchoolFinish(castParent.transform);

        GameObject concept = new("Spark = curiosity, joy, imagination and youthful energy (not health)");
        concept.transform.SetParent(castParent.transform, false);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Validate();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
        Debug.Log("Alberta school-run prototype created and validated: " + ScenePath);
        if (!batch) Selection.activeGameObject = danny;
    }

    private static Material Material(string name) =>
        AssetDatabase.LoadAssetAtPath<Material>($"{TestMaterials}/{name}.mat");

    private static GameObject Primitive(string name, PrimitiveType type, Vector3 position,
        Vector3 scale, Material material, Transform parent)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = name;
        item.transform.SetParent(parent, true);
        item.transform.position = position;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        return item;
    }

    private static void BuildHockeyCorner(Transform parent)
    {
        Material snow = Material("DannyTest_Snow");
        Material ice = Material("DannyTest_Ice");
        Material obstacle = Material("DannyTest_Obstacle") ?? Material("DannyTest_Lamp");
        Primitive("Street Hockey Ice Patch", PrimitiveType.Cylinder,
            new Vector3(1.8f, 0.035f, 2.2f), new Vector3(1.55f, 0.018f, 1.75f), ice, parent);
        Primitive("Hockey Goal Crossbar", PrimitiveType.Cube,
            new Vector3(2.6f, 0.72f, 3.65f), new Vector3(1.25f, 0.055f, 0.055f), obstacle, parent);
        Primitive("Hockey Goal Left Post", PrimitiveType.Cube,
            new Vector3(2.0f, 0.38f, 3.65f), new Vector3(0.055f, 0.72f, 0.055f), obstacle, parent);
        Primitive("Hockey Goal Right Post", PrimitiveType.Cube,
            new Vector3(3.2f, 0.38f, 3.65f), new Vector3(0.055f, 0.72f, 0.055f), obstacle, parent);
        Primitive("Street Hockey Puck", PrimitiveType.Cylinder,
            new Vector3(1.55f, 0.075f, 2.0f), new Vector3(0.12f, 0.018f, 0.12f), obstacle, parent);
        Primitive("Hockey Snow Edge", PrimitiveType.Sphere,
            new Vector3(3.4f, 0.18f, 2.5f), new Vector3(1.0f, 0.25f, 1.6f), snow, parent);
    }

    private static void BuildSnowFort(Transform parent)
    {
        Material snow = Material("DannyTest_Snow");
        Primitive("Snow Fort Left Wall", PrimitiveType.Cube,
            new Vector3(-4.55f, 0.42f, 3.9f), new Vector3(0.30f, 0.72f, 1.75f), snow, parent);
        Primitive("Snow Fort Back Wall", PrimitiveType.Cube,
            new Vector3(-3.95f, 0.42f, 4.65f), new Vector3(1.45f, 0.72f, 0.30f), snow, parent);
        Primitive("Snow Fort Tower", PrimitiveType.Sphere,
            new Vector3(-4.55f, 0.85f, 4.62f), new Vector3(0.48f, 0.58f, 0.48f), snow, parent);
    }

    private static void BuildSchoolFinish(Transform parent)
    {
        Material obstacle = Material("DannyTest_Obstacle") ?? Material("DannyTest_Lamp");
        Material warm = Material("DannyTest_WarmLamp");
        Primitive("School Gate Left", PrimitiveType.Cube,
            new Vector3(-1.65f, 1.05f, 10.4f), new Vector3(0.16f, 2.1f, 0.16f), obstacle, parent);
        Primitive("School Gate Right", PrimitiveType.Cube,
            new Vector3(1.65f, 1.05f, 10.4f), new Vector3(0.16f, 2.1f, 0.16f), obstacle, parent);
        Primitive("School Sign", PrimitiveType.Cube,
            new Vector3(0f, 1.95f, 10.4f), new Vector3(2.9f, 0.55f, 0.16f), warm, parent);
    }

    private static void AddSceneToBuildSettings(string path)
    {
        if (EditorBuildSettings.scenes.Any(s => s.path == path)) return;
        EditorBuildSettings.scenes = EditorBuildSettings.scenes
            .Concat(new[] { new EditorBuildSettingsScene(path, true) }).ToArray();
    }

    private static void Validate()
    {
        WinterCastIdentity[] cast = UnityEngine.Object.FindObjectsByType<WinterCastIdentity>(FindObjectsSortMode.None);
        if (cast.Length != Placements.Length)
            throw new InvalidOperationException($"School-run scene cast count is {cast.Length}, expected {Placements.Length}");
        if (UnityEngine.Object.FindFirstObjectByType<DannySpark>() == null)
            throw new InvalidOperationException("Danny Spark component is missing");
        if (Camera.main == null || Camera.main.GetComponent<SparkWorldMood>() == null)
            throw new InvalidOperationException("Spark world-mood component is missing");
        if (!cast.Any(c => c.StoryRole == "chaser") || !cast.Any(c => c.StoryRole == "authority"))
            throw new InvalidOperationException("Mom or principal role is missing");
    }
}
