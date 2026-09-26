using UnityEngine;

/// <summary>Brings the Edmonton street and river-valley animals to life.</summary>
[DisallowMultipleComponent]
public sealed class RiverValleyWildlifeDirector : MonoBehaviour
{
    private const string AnimalResourceRoot = "Littles/RiverValley/Animals/Quaternius/";
    private Material brown;
    private Material cream;
    private Material dark;

    private void Start()
    {
        if(GetComponent<CoyoteSnowDrag>()==null)gameObject.AddComponent<CoyoteSnowDrag>();
        brown = MakeMaterial(new Color(0.34f, 0.19f, 0.08f));
        cream = MakeMaterial(new Color(0.72f, 0.57f, 0.36f));
        dark = MakeMaterial(new Color(0.035f, 0.025f, 0.018f));

        int meadowRabbits = 0;
        foreach (Transform child in transform)
        {
            string lower = child.name.ToLowerInvariant();
            // Names such as "rabbit meadow", "rabbit clue" and "coyote kid"
            // are locations or people, not animals.  Moving every object whose
            // label happened to contain an animal name was the source of the
            // enormous flat rabbits, flying parks and drifting child groups.
            bool isRabbit = child.Find("Cartoon rabbit big head") != null ||
                child.Find("Found rabbit animated model") != null;
            bool isCoyote = child.Find("Wolf animated model") != null ||
                child.Find("Coyote body") != null;
            if (isRabbit)
            {
                RestoreCartoonRabbit(child);
                if (lower.Contains("rabbit meadow hopper") && ++meadowRabbits > 10)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }
                AddMotion(child.gameObject, RiverAnimalKind.Rabbit, true);
            }
            else if (isCoyote) AddMotion(child.gameObject, RiverAnimalKind.Coyote, true);
        }

        Transform theo = FindDirectChild("the very pettable dog");
        if (theo != null && theo.Find("Friendly Edmonton dog") == null)
        {
            GameObject dog = BuildDog(theo);
            AddMotion(dog, RiverAnimalKind.Dog, false);
        }

        Transform wildlife = transform.Find("River valley wildlife");
        if (wildlife == null)
        {
            GameObject group = new("River valley wildlife");
            group.transform.SetParent(transform, false);
            wildlife = group.transform;
            BuildDeer(wildlife, GroundedPosition(new Vector3(-7.2f, 20f, 274f)), 25f, "River deer one");
            BuildDeer(wildlife, GroundedPosition(new Vector3(7.5f, 20f, 342f)), 205f, "River deer two");
            BuildImportedAnimal(wildlife, GroundedPosition(new Vector3(-8.4f, 20f, 318f)), 145f,
                "Watchful river-valley stag", "Stag", 0.40f, "Walk", RiverAnimalKind.Deer, true);
            BuildImportedAnimal(wildlife, GroundedPosition(new Vector3(-24f, 20f, 82f)), 110f,
                "Red fox in the coyote woods", "Fox", 0.34f, "Walk", RiverAnimalKind.Fox, true);
            BuildImportedAnimal(wildlife, GroundedPosition(new Vector3(-4.8f, 20f, 46f)), 15f,
                "Neighbourhood shiba on the school route", "ShibaInu", 0.27f, "Walk", RiverAnimalKind.Dog, true);
        }
    }

    private Transform FindDirectChild(string fragment)
    {
        foreach (Transform child in transform)
            if (child.name.ToLowerInvariant().Contains(fragment)) return child;
        return null;
    }

    private static void RestoreCartoonRabbit(Transform rabbit)
    {
        if(rabbit==null)return;
        // The imported long-limbed bunny deformed into a flat or upright giant
        // in this scene.  Keep the readable authored snow-bunny and make it a
        // small animal beside the children.
        Transform imported=rabbit.Find("Bunny animated model");
        if(imported!=null)Destroy(imported.gameObject);
        foreach(Renderer renderer in rabbit.GetComponentsInChildren<Renderer>(true))
            if(renderer!=null&&renderer.transform!=imported&&
                (imported==null||!renderer.transform.IsChildOf(imported)))renderer.enabled=true;
        // The licensed FoundRabbit is already normalized when the scene is
        // generated; only the old cartoon fallback needs the extra shrink.
        if(rabbit.Find("Found rabbit animated model")==null)
            rabbit.localScale=Vector3.one*(rabbit.name.ToLowerInvariant().Contains("captain")?0.72f:0.62f);
    }

    private Vector3 GroundedPosition(Vector3 probe)
    {
        if (Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 100f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return hit.point;
        return new Vector3(probe.x, -30f, probe.z);
    }

    private GameObject BuildDog(Transform owner)
    {
        GameObject root = new("Friendly Edmonton dog");
        root.transform.SetParent(owner, false);
        root.transform.localPosition = new Vector3(1.05f, 0f, 0.28f);
        if (AddImportedVisual(root.transform, "Husky", 0.28f, "Walk")) return root;
        Part(PrimitiveType.Sphere, "Dog body", root.transform, new Vector3(0f, 0.38f, 0f), new Vector3(0.42f, 0.34f, 0.68f), brown);
        Part(PrimitiveType.Sphere, "Dog head", root.transform, new Vector3(0f, 0.67f, -0.48f), Vector3.one * 0.34f, cream);
        Part(PrimitiveType.Cube, "Dog muzzle", root.transform, new Vector3(0f, 0.60f, -0.72f), new Vector3(0.25f, 0.18f, 0.28f), cream);
        for (int i = 0; i < 4; i++)
            Part(PrimitiveType.Cylinder, "Dog leg " + i, root.transform,
                new Vector3(i % 2 == 0 ? -0.22f : 0.22f, 0.15f, i < 2 ? -0.25f : 0.30f),
                new Vector3(0.075f, 0.18f, 0.075f), brown);
        GameObject tail = Part(PrimitiveType.Cylinder, "Dog tail", root.transform,
            new Vector3(0f, 0.52f, 0.62f), new Vector3(0.06f, 0.34f, 0.06f), cream);
        tail.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
        Part(PrimitiveType.Sphere, "Dog nose", root.transform, new Vector3(0f, 0.63f, -0.88f), Vector3.one * 0.07f, dark);
        return root;
    }

    private void BuildDeer(Transform parent, Vector3 position, float yaw, string deerName)
    {
        GameObject root = new(deerName);
        root.transform.SetParent(parent, true);
        root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        if (AddImportedVisual(root.transform, "Deer", 0.42f, "Walk"))
        {
            AddMotion(root, RiverAnimalKind.Deer, true);
            return;
        }
        Part(PrimitiveType.Sphere, "Deer body", root.transform, new Vector3(0f, 0.95f, 0f), new Vector3(0.55f, 0.62f, 1.05f), brown);
        Part(PrimitiveType.Cylinder, "Deer neck", root.transform, new Vector3(0f, 1.48f, -0.64f), new Vector3(0.18f, 0.48f, 0.18f), cream).transform.localRotation = Quaternion.Euler(-24f, 0f, 0f);
        Part(PrimitiveType.Sphere, "Deer head", root.transform, new Vector3(0f, 1.88f, -0.86f), new Vector3(0.34f, 0.30f, 0.48f), cream);
        for (int i = 0; i < 4; i++)
            Part(PrimitiveType.Cylinder, "Deer leg " + i, root.transform,
                new Vector3(i % 2 == 0 ? -0.31f : 0.31f, 0.40f, i < 2 ? -0.52f : 0.54f),
                new Vector3(0.075f, 0.48f, 0.075f), brown);
        Part(PrimitiveType.Cube, "Deer ear left", root.transform, new Vector3(-0.25f, 2.14f, -0.82f), new Vector3(0.14f, 0.30f, 0.10f), cream).transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        Part(PrimitiveType.Cube, "Deer ear right", root.transform, new Vector3(0.25f, 2.14f, -0.82f), new Vector3(0.14f, 0.30f, 0.10f), cream).transform.localRotation = Quaternion.Euler(0f, 0f, 28f);
        Part(PrimitiveType.Cylinder, "Deer tail", root.transform, new Vector3(0f, 1.12f, 0.96f), new Vector3(0.08f, 0.23f, 0.08f), cream).transform.localRotation = Quaternion.Euler(55f, 0f, 0f);
        AddMotion(root, RiverAnimalKind.Deer, true);
    }

    private static GameObject BuildImportedAnimal(Transform parent, Vector3 position, float yaw,
        string animalName, string modelName, float scale, string animation, RiverAnimalKind kind, bool roam)
    {
        GameObject root = new(animalName);
        root.transform.SetParent(parent, true);
        root.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        if (!AddImportedVisual(root.transform, modelName, scale, animation))
        {
            UnityEngine.Object.Destroy(root);
            return null;
        }
        AddMotion(root, kind, roam);
        return root;
    }

    private static bool AddImportedVisual(Transform parent, string modelName, float scale, string animation)
    {
        GameObject prefab = Resources.Load<GameObject>(AnimalResourceRoot + modelName);
        if (prefab == null) return false;
        GameObject visual = Instantiate(prefab, parent, false);
        visual.name = modelName + " animated model";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * scale;
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) Destroy(collider);
        foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true)) animator.applyRootMotion = false;
        QuaterniusAnimalAnimator player = visual.AddComponent<QuaterniusAnimalAnimator>();
        player.Configure(AnimalResourceRoot + modelName, animation);
        return true;
    }

    private static void AddMotion(GameObject animal, RiverAnimalKind kind, bool roam)
    {
        RiverValleyAnimalMotion motion = animal.GetComponent<RiverValleyAnimalMotion>();
        if (motion == null) motion = animal.AddComponent<RiverValleyAnimalMotion>();
        motion.Configure(kind, roam);
    }

    private static GameObject Part(PrimitiveType type, string name, Transform parent,
        Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        if (collider != null) Object.Destroy(collider);
        return part;
    }

    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new(shader) { color = color, hideFlags = HideFlags.DontSave };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        return material;
    }
}
