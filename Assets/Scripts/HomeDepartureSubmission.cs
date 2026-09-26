using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Submission bootstrap for the opening scene. It keeps setup reproducible without
/// requiring prefab references: movement tuning, ledge climbing and the shallow
/// open-door foyer are installed when HomeDeparture starts.
/// </summary>
public sealed class HomeDepartureSubmission : MonoBehaviour
{
    private Transform _player;
    private Transform _doorHinge;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private bool _doorOpen = true;
    private GUIStyle _helpStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (SceneManager.GetActiveScene().name != "HomeDeparture") return;
        if (FindFirstObjectByType<HomeDepartureSubmission>() != null) return;
        new GameObject("HomeDeparture_Submission").AddComponent<HomeDepartureSubmission>();
    }

    private void Awake()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null) return;
        _player = playerObject.transform;

        ThirdPersonController motor = playerObject.GetComponent<ThirdPersonController>();
        if (motor != null)
        {
            motor.MoveSpeed = 2.4f;
            motor.SprintSpeed = 5.8f;
            motor.JumpHeight = 0.32f;
            motor.Gravity = -22f;
            motor.JumpTimeout = 0.22f;
            motor.FallTimeout = 0.08f;
            motor.GroundedRadius = 0.14f;
            motor.GroundedOffset = -0.02f;
        }

        MothGrounding grounding = playerObject.GetComponent<MothGrounding>();
        if (grounding != null) grounding.SnapWhileGrounded = true;
        LittleLedgeClimber climber = playerObject.GetComponent<LittleLedgeClimber>();
        if (climber == null) climber = playerObject.AddComponent<LittleLedgeClimber>();
        if (playerObject.GetComponent<LittleStableFollowInput>() == null)
            playerObject.AddComponent<LittleStableFollowInput>();
        // The supplied crawl clip does not retarget correctly to this UMA/Moth
        // skeleton: shoulders, elbows, hips and knees fold in incompatible bone
        // axes. Disable ground crawl cleanly until a matching humanoid clip is
        // available. C remains reserved for dropping from a wall.
        LittleGroundCrawlAssist crawlAssist = playerObject.GetComponent<LittleGroundCrawlAssist>();
        if (crawlAssist != null) Destroy(crawlAssist);
        if (playerObject.GetComponent<LittleCrawlBlocker>() == null)
            playerObject.AddComponent<LittleCrawlBlocker>();
        climber.ForwardCheck = 0.68f;
        climber.MantleOnly = true;
        climber.MinimumLedgeHeight = 0.12f;
        climber.MaximumLedgeHeight = 1.25f;
        climber.WallClearance = 0.025f;
        climber.HangBodyDrop = 0.50f;
        climber.ClimbDuration = 0.50f;
        climber.WallClimbSpeed = 1.0f;
        climber.WallTraverseSpeed = 0.85f;

        ApplyMothMaterials(playerObject);
        ConfigurePlayerCamera(motor);

        SidewalkGameDirector campaign = FindFirstObjectByType<SidewalkGameDirector>();
        if (campaign == null)
            campaign = new GameObject("WalkToSchool_Director").AddComponent<SidewalkGameDirector>();
        campaign.Initialize(playerObject);
    }

    private void BuildEntrance()
    {
        Vector3 front = Vector3.forward;
        Camera camera = Camera.main;
        if (camera != null)
        {
            front = _player.position - camera.transform.position;
            front.y = 0f;
            if (front.sqrMagnitude < 0.1f) front = Vector3.forward;
            front.Normalize();
        }

        Vector3 right = Vector3.Cross(Vector3.up, front).normalized;
        Vector3 threshold = _player.position + front * 0.72f;
        float width = 1.02f;
        float height = 2.08f;
        Quaternion facing = Quaternion.LookRotation(-front, Vector3.up);

        Transform entrance = new GameObject("Open_Home_Entrance").transform;
        entrance.SetPositionAndRotation(threshold, facing);

        Material shadow = MakeMaterial("Foyer Shadow", new Color(0.035f, 0.045f, 0.065f));
        Material wood = MakeMaterial("Warm Door", new Color(0.28f, 0.09f, 0.045f));
        Material trim = MakeMaterial("Door Trim", new Color(0.72f, 0.66f, 0.52f));
        Material floor = MakeMaterial("Foyer Floor", new Color(0.24f, 0.18f, 0.13f));
        Material fabric = MakeMaterial("Foyer Furniture", new Color(0.14f, 0.28f, 0.31f));
        Material mirror = MakeMirrorMaterial("Foyer Mirror and Storefront Glass");

        CreateBlock("Dark Interior", entrance, new Vector3(0f, height * 0.5f, 0.035f),
            new Vector3(width, height, 0.08f), shadow, false);
        CreateBlock("Threshold", entrance, new Vector3(0f, 0.035f, -0.05f),
            new Vector3(width + 0.16f, 0.07f, 0.34f), trim, true);
        CreateBlock("Left Frame", entrance, new Vector3(-width * 0.5f - 0.065f, height * 0.5f, -0.05f),
            new Vector3(0.13f, height + 0.18f, 0.16f), trim, false);
        CreateBlock("Right Frame", entrance, new Vector3(width * 0.5f + 0.065f, height * 0.5f, -0.05f),
            new Vector3(0.13f, height + 0.18f, 0.16f), trim, false);
        CreateBlock("Top Frame", entrance, new Vector3(0f, height + 0.065f, -0.05f),
            new Vector3(width + 0.26f, 0.13f, 0.16f), trim, false);

        _doorHinge = new GameObject("Working Front Door Hinge").transform;
        _doorHinge.SetParent(entrance, false);
        _doorHinge.localPosition = new Vector3(-width * 0.5f, height * 0.5f, -0.11f);
        Transform leaf = CreateBlock("Working Front Door", _doorHinge, new Vector3(width * 0.5f, 0f, 0f),
            new Vector3(width, height, 0.075f), wood, true);
        CreateBlock("Door Window", leaf, new Vector3(0f, 0.22f, -0.041f),
            new Vector3(0.22f, 0.7f, 0.025f), trim, false);
        CreateBlock("Door Handle", leaf, new Vector3(width * 0.36f, -0.05f, -0.07f),
            new Vector3(0.045f, 0.045f, 0.08f), trim, false);
        _closedRotation = Quaternion.identity;
        _openRotation = Quaternion.Euler(0f, -78f, 0f);
        _doorHinge.localRotation = _openRotation;

        // A deliberately shallow, inexpensive foyer: enough depth and furniture
        // for the departure shot without constructing an entire house interior.
        CreateBlock("Foyer Floor", entrance, new Vector3(0f, -0.015f, -1.35f),
            new Vector3(2.5f, 0.06f, 2.7f), floor, false);
        CreateBlock("Foyer Back Wall", entrance, new Vector3(0f, 1.25f, -2.68f),
            new Vector3(2.5f, 2.5f, 0.08f), shadow, false);
        CreateBlock("Foyer Left Wall", entrance, new Vector3(-1.22f, 1.25f, -1.35f),
            new Vector3(0.08f, 2.5f, 2.7f), shadow, false);
        CreateBlock("Foyer Right Wall", entrance, new Vector3(1.22f, 1.25f, -1.35f),
            new Vector3(0.08f, 2.5f, 2.7f), shadow, false);
        CreateBlock("Small Sofa", entrance, new Vector3(0.58f, 0.28f, -2.25f),
            new Vector3(1.05f, 0.48f, 0.42f), fabric, false);
        CreateBlock("Sofa Back", entrance, new Vector3(0.58f, 0.58f, -2.43f),
            new Vector3(1.05f, 0.62f, 0.16f), fabric, false);
        CreateBlock("Console Table", entrance, new Vector3(-0.72f, 0.48f, -2.42f),
            new Vector3(0.72f, 0.08f, 0.28f), wood, false);
        CreateBlock("Foyer Character Mirror", entrance, new Vector3(-0.58f, 1.35f, -2.625f),
            new Vector3(0.72f, 1.08f, 0.035f), mirror, false);
        CreateBlock("Porch Climb Trunk", entrance, new Vector3(1.22f, 0.36f, 0.2f),
            new Vector3(0.8f, 0.72f, 0.42f), wood, true);

        GameObject reflectionObject = new GameObject("Character Mirror Reflection Probe");
        reflectionObject.transform.SetParent(entrance, false);
        reflectionObject.transform.localPosition = new Vector3(0f, 1.05f, -1.45f);
        ReflectionProbe reflection = reflectionObject.AddComponent<ReflectionProbe>();
        reflection.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
        reflection.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.EveryFrame;
        reflection.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
        reflection.renderDynamicObjects = true;
        reflection.resolution = 128;
        reflection.size = new Vector3(3.2f, 2.6f, 3.2f);
        reflection.boxProjection = true;

        GameObject warmLightObject = new GameObject("Warm Foyer Light");
        warmLightObject.transform.SetParent(entrance, false);
        warmLightObject.transform.localPosition = new Vector3(0f, 1.65f, -1.4f);
        Light warmLight = warmLightObject.AddComponent<Light>();
        warmLight.type = LightType.Point;
        warmLight.color = new Color(1f, 0.69f, 0.42f);
        warmLight.range = 3.8f;
        warmLight.intensity = 3.2f;
    }

    private void Update()
    {
        if (_doorHinge == null || _player == null) return;
        bool toggle = false;
#if ENABLE_INPUT_SYSTEM
        toggle = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        toggle = Input.GetKeyDown(KeyCode.E);
#endif
        if (toggle && Vector3.Distance(_player.position, _doorHinge.position) < 2.5f)
            _doorOpen = !_doorOpen;
        Quaternion target = _doorOpen ? _openRotation : _closedRotation;
        _doorHinge.localRotation = Quaternion.Slerp(_doorHinge.localRotation, target, 7f * Time.deltaTime);
    }

    private void OnGUI()
    {
        if (_helpStyle == null)
        {
            _helpStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 16
            };
            _helpStyle.normal.textColor = Color.white;
        }
        GUI.Box(new Rect(18f, 18f, 690f, 34f),
            "WASD Move • Shift Run • Space Jump/Mantle • V View • Wheel Zoom", _helpStyle);
    }

    private static Transform CreateBlock(string objectName, Transform parent, Vector3 localPosition,
        Vector3 localScale, Material material, bool keepCollider)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = objectName;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localPosition;
        block.transform.localScale = localScale;
        block.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider) Destroy(block.GetComponent<Collider>());
        return block.transform;
    }

    private static Material MakeMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = materialName };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        return material;
    }

    private static void ApplyMothMaterials(GameObject playerObject)
    {
        SkinnedMeshRenderer target = null;
        foreach (SkinnedMeshRenderer renderer in playerObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.sharedMaterials.Length < 10) continue;
            target = renderer;
            break;
        }
        if (target == null) return;

        Material skin = MakeCharacterMaterial("Moth Skin", new Color(0.62f, 0.34f, 0.22f), 0.26f);
        Material hiddenHairCards = MakeHiddenMaterial("Moth Broken Hair Cards Hidden");
        Material eyes = MakeCharacterMaterial("Moth Eyes", new Color(0.16f, 0.42f, 0.36f), 0.72f);
        Material shorts = MakeCharacterMaterial("Moth Shorts and Shoes", new Color(0.025f, 0.045f, 0.075f), 0.16f);
        // The FBX combines most clothing into one untextured UMA submesh. A
        // restrained teal reads as clothing instead of one bright red body suit.
        Material shirt = MakeCharacterMaterial("Moth Main Outfit", new Color(0.12f, 0.31f, 0.36f), 0.18f);

        // Remove the three broken hair-card submeshes completely. A transparent
        // material was still leaving black polygon patches on some Linux/URP GPUs.
        Mesh sourceMesh = target.sharedMesh;
        if (sourceMesh != null && sourceMesh.subMeshCount > 9)
        {
            Mesh cleanMesh = Instantiate(sourceMesh);
            cleanMesh.name = sourceMesh.name + " (Hair Cards Removed)";
            cleanMesh.SetTriangles(System.Array.Empty<int>(), 5);
            cleanMesh.SetTriangles(System.Array.Empty<int>(), 8);
            cleanMesh.SetTriangles(System.Array.Empty<int>(), 9);
            target.sharedMesh = cleanMesh;
        }

        Material[] slots = target.sharedMaterials;
        for (int i = 0; i < Mathf.Min(5, slots.Length); i++) slots[i] = skin;
        if (slots.Length > 5) slots[5] = hiddenHairCards;
        if (slots.Length > 6) slots[6] = eyes;
        if (slots.Length > 7) slots[7] = shorts;
        if (slots.Length > 8) slots[8] = hiddenHairCards;
        if (slots.Length > 9) slots[9] = hiddenHairCards;
        if (slots.Length > 10) slots[10] = shirt;
        target.sharedMaterials = slots;
        // The imported alpha cards do not survive this UMA retarget. Replace
        // them with small rounded coils which follow the real humanoid Head.
        RemoveGeneratedHairCap(playerObject);
        Material hair = MakeCharacterMaterial("Allen Soft Dark Hair",
            new Color(0.055f, 0.028f, 0.018f), 0.22f);
        BuildCleanHair(playerObject, hair);
    }

    private static Material MakeCharacterMaterial(string materialName, Color color, float smoothness)
    {
        Material material = MakeMaterial(materialName, color);
        material.enableInstancing = true;
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        return material;
    }

    private static Material MakeMirrorMaterial(string materialName)
    {
        Material material = MakeMaterial(materialName, new Color(0.58f, 0.7f, 0.76f, 1f));
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.88f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.96f);
        return material;
    }

    private static Material MakeHiddenMaterial(string materialName)
    {
        Material material = MakeMaterial(materialName, new Color(0f, 0f, 0f, 0f));
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", 5f);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", 10f);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        return material;
    }

    private static void RemoveGeneratedHairCap(GameObject playerObject)
    {
        foreach (Transform child in playerObject.GetComponentsInChildren<Transform>(true))
        {
            if (child.name != "Moth_Clean_Hair") continue;
            Destroy(child.gameObject);
        }
    }

    private static void BuildCleanHair(GameObject playerObject, Material hairMaterial)
    {
        RemoveGeneratedHairCap(playerObject);
        Animator animator = playerObject.GetComponent<Animator>();
        if (animator == null || !animator.isHuman) return;
        Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (head == null) return;

        GameObject hairObject = new GameObject("Moth_Clean_Hair");
        hairObject.transform.SetParent(playerObject.transform, false);
        LittleHairFollower follower = hairObject.AddComponent<LittleHairFollower>();
        follower.Head = head;
        follower.Character = playerObject.transform;
        follower.Height = 0.018f;
        follower.Forward = -0.004f;
        follower.SnapNow();

        MeshFilter filter = hairObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = hairObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = hairMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();
        // Full ellipsoids have no open rim, so they cannot become the floating
        // umbrella/crescent produced by the former one-sided hair cap.
        Vector3[] centers =
        {
            new Vector3(-0.030f, 0.011f, -0.020f), new Vector3(0f, 0.017f, -0.024f),
            new Vector3(0.030f, 0.011f, -0.020f), new Vector3(-0.041f, 0.007f, 0.004f),
            new Vector3(-0.014f, 0.021f, 0.002f), new Vector3(0.015f, 0.021f, 0.002f),
            new Vector3(0.041f, 0.007f, 0.004f), new Vector3(-0.030f, 0.012f, 0.027f),
            new Vector3(0f, 0.020f, 0.030f), new Vector3(0.030f, 0.012f, 0.027f),
            new Vector3(-0.012f, 0.030f, 0.014f), new Vector3(0.013f, 0.030f, 0.014f)
        };
        for (int i = 0; i < centers.Length; i++)
            AddEllipsoid(vertices, normals, triangles, centers[i],
                new Vector3(0.020f, 0.017f, 0.020f), 10, 6);

        Mesh mesh = new Mesh { name = "Moth Seated Short Hair" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        filter.sharedMesh = mesh;
    }

    private static void AddHairTuft(List<Vector3> vertices, List<Vector3> normals,
        List<int> triangles, Vector3 baseCenter, Vector3 tipOffset)
    {
        const int sides = 6;
        int start = vertices.Count;
        float radius = 0.009f;
        for (int i = 0; i < sides; i++)
        {
            float angle = i / (float)sides * Mathf.PI * 2f;
            Vector3 radial = new Vector3(Mathf.Cos(angle) * radius, 0f,
                Mathf.Sin(angle) * radius);
            vertices.Add(baseCenter + radial);
            normals.Add(radial.normalized);
        }
        int tip = vertices.Count;
        vertices.Add(baseCenter + tipOffset);
        normals.Add(tipOffset.normalized);
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            triangles.Add(start + i);
            triangles.Add(start + next);
            triangles.Add(tip);
        }
    }

    private static void AddHairCap(List<Vector3> vertices, List<Vector3> normals,
        List<int> triangles, Vector3 center, Vector3 radius, int segments, int rings)
    {
        int start = vertices.Count;
        // Only the upper 58% of an ellipsoid: hair cap, not a full helmet/sphere.
        float maximumPhi = Mathf.PI * 0.46f;
        for (int y = 0; y <= rings; y++)
        {
            float phi = y / (float)rings * maximumPhi;
            for (int x = 0; x <= segments; x++)
            {
                float theta = x / (float)segments * Mathf.PI * 2f;
                Vector3 unit = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta),
                    Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                vertices.Add(center + Vector3.Scale(unit, radius));
                normals.Add(new Vector3(unit.x / radius.x, unit.y / radius.y,
                    unit.z / radius.z).normalized);
            }
        }
        for (int y = 0; y < rings; y++)
        for (int x = 0; x < segments; x++)
        {
            int a = start + y * (segments + 1) + x;
            int b = a + segments + 1;
            triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
            triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
        }
    }

    private static void AddEllipsoid(List<Vector3> vertices, List<Vector3> normals,
        List<int> triangles, Vector3 center, Vector3 radius, int segments, int rings)
    {
        int start = vertices.Count;
        for (int y = 0; y <= rings; y++)
        {
            float v = y / (float)rings;
            float phi = v * Mathf.PI;
            for (int x = 0; x <= segments; x++)
            {
                float u = x / (float)segments;
                float theta = u * Mathf.PI * 2f;
                Vector3 unit = new Vector3(Mathf.Sin(phi) * Mathf.Cos(theta),
                    Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(theta));
                vertices.Add(center + Vector3.Scale(unit, radius));
                normals.Add(new Vector3(unit.x / radius.x, unit.y / radius.y,
                    unit.z / radius.z).normalized);
            }
        }
        for (int y = 0; y < rings; y++)
        for (int x = 0; x < segments; x++)
        {
            int a = start + y * (segments + 1) + x;
            int b = a + segments + 1;
            triangles.Add(a); triangles.Add(b); triangles.Add(a + 1);
            triangles.Add(a + 1); triangles.Add(b); triangles.Add(b + 1);
        }
    }

    private static void AddTuft(List<Vector3> vertices, List<Vector3> normals,
        List<int> triangles, Vector3 baseCenter, Vector3 direction, float radius)
    {
        const int sides = 7;
        int start = vertices.Count;
        Vector3 axis = direction.normalized;
        Vector3 tangent = Vector3.Cross(axis, Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.9f
            ? Vector3.forward : Vector3.up).normalized;
        Vector3 bitangent = Vector3.Cross(axis, tangent).normalized;
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2f / sides;
            Vector3 radial = tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle);
            vertices.Add(baseCenter + radial * radius);
            normals.Add((radial + axis * 0.18f).normalized);
        }
        int tip = vertices.Count;
        vertices.Add(baseCenter + direction);
        normals.Add(axis);
        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            triangles.Add(start + i); triangles.Add(start + next); triangles.Add(tip);
        }
    }

    private void ConfigurePlayerCamera(ThirdPersonController motor)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;
        LittleAdaptiveCamera adaptive = mainCamera.GetComponent<LittleAdaptiveCamera>();
        if (adaptive == null) adaptive = mainCamera.gameObject.AddComponent<LittleAdaptiveCamera>();
        Transform pivot = motor != null && motor.CinemachineCameraTarget != null
            ? motor.CinemachineCameraTarget.transform
            : _player;
        adaptive.Configure(_player, pivot);
    }
}
