using TheLittles;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CreateMoth3DMovementTest
{
    [MenuItem("The Littles/Create Moth 3D Movement Test")]
    public static void Create()
    {
        ConfigureMothAnimations.Rebuild();

        GameObject mothPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Resources/Littles/Moth.fbx");
        if (mothPrefab == null)
        {
            Debug.LogError("Moth.fbx was not found in Assets/Resources/Littles.");
            return;
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "3D movement ground";
        ground.transform.localScale = new Vector3(5f, 1f, 5f);

        GameObject player = new("Moth 3D Player");
        player.transform.position = Vector3.zero;
        CharacterController character = player.AddComponent<CharacterController>();
        character.height = 1.75f;
        character.radius = 0.30f;
        character.center = new Vector3(0f, 0.875f, 0f);
        character.stepOffset = 0.28f;

        GameObject model = Object.Instantiate(mothPrefab, player.transform, false);
        model.name = "Moth Model";

        GameObject cameraObject = new("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 52f;
        camera.nearClipPlane = 0.08f;
        cameraObject.transform.position = new Vector3(-2.2f, 2.8f, -4.5f);
        MothOrbitCamera orbit = cameraObject.AddComponent<MothOrbitCamera>();
        orbit.Configure(player.transform);

        MothMotor3D motor = player.AddComponent<MothMotor3D>();
        motor.Configure(cameraObject.transform, model);

        MothGrounding grounding = player.AddComponent<MothGrounding>();
        grounding.VisualRoot = model.transform;
        grounding.CharacterController = character;
        grounding.Animator = model.GetComponentInChildren<Animator>(true);
        grounding.MaximumCorrection = 1.25f;
        grounding.GroundingSpeed = 12f;
        grounding.SoleClearance = 0.01f;

        GameObject lightObject = new("Sun");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

        AddBlock(new Vector3(3f, 0.5f, 3f), new Vector3(2f, 1f, 2f));
        AddBlock(new Vector3(-3f, 0.3f, 2f), new Vector3(2.4f, 0.6f, 1.2f));
        AddBlock(new Vector3(1f, 0.2f, -3f), new Vector3(3f, 0.4f, 1f));

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        string path = "Assets/Scenes/Moth3DMovementTest.unity";
        EditorSceneManager.SaveScene(scene, path);
        Selection.activeGameObject = player;
        Debug.Log("Created " + path + ". WASD moves, Space jumps, C toggles crawl, right mouse orbits.");
    }

    private static void AddBlock(Vector3 position, Vector3 scale)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = "Movement test block";
        block.transform.position = position;
        block.transform.localScale = scale;
    }
}
