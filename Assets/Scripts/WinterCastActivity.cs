using UnityEngine;

public enum WinterCastActivityKind
{
    HockeyGoalie,
    HockeyStickhandler,
    Snowboarder
}

/// <summary>Continuous hockey and snowboard performances for the winter cast.</summary>
[DisallowMultipleComponent]
public sealed class WinterCastActivity : MonoBehaviour
{
    [SerializeField] private WinterCastActivityKind kind;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private Vector3 home;
    private Quaternion restRotation;
    private Animator[] animators;
    private Transform prop;
    private readonly RaycastHit[] stickGroundHits = new RaycastHit[12];
    private float phase;
    private Vector3 rallyTarget;
    private float rallySwing;
    private bool rallyActive;

    private void Start()
    {
        RiverValleyAmbientActor ambient = GetComponent<RiverValleyAmbientActor>();
        if (ambient != null) ambient.enabled = false;
        home = transform.position;
        restRotation = transform.rotation;
        phase = Mathf.Abs(home.x * 0.43f + home.z * 0.17f);
        animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
            if (animator != null && animator.GetComponent<WinterAnimationEventRelay>() == null)
                animator.gameObject.AddComponent<WinterAnimationEventRelay>();
        if (kind == WinterCastActivityKind.Snowboarder) BuildSnowboard();
        else BuildHockeySet();
    }

    private void Update()
    {
        float time = Time.time + phase;
        if (kind == WinterCastActivityKind.Snowboarder)
        {
            float carve = Mathf.Sin(time * 1.18f);
            float glide = Mathf.Sin(time * 0.58f);
            float hop = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * 1.16f)), 9f) * 0.09f;
            transform.position = home + restRotation * new Vector3(carve * 0.48f,
                hop, glide * 1.35f);
            transform.rotation = restRotation * Quaternion.Euler(0f, carve * 21f, -carve * 9f);
            if (prop != null) prop.localRotation = Quaternion.Euler(0f, carve * 11f, carve * 7f);
            // The board supplies the travel; an idle body with a controlled
            // lean reads as riding instead of walking on top of it.
            SetAnimation(0f);
            return;
        }

        Vector3 before=transform.position;
        float shuffle = Mathf.Sin(time * (kind == WinterCastActivityKind.HockeyGoalie ? 1.7f : 2.2f));
        float reach = rallyActive ? rallySwing : Mathf.Sin(time * 3.15f) * 0.35f;
        float width = kind == WinterCastActivityKind.HockeyGoalie ? 0.46f : 0.82f;
        transform.position = home + restRotation * new Vector3(shuffle * width, 0f, 0f);
        if (rallyActive)
        {
            Vector3 face = Vector3.ProjectOnPlane(rallyTarget - transform.position, Vector3.up);
            if (face.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(face.normalized, Vector3.up),
                    1f - Mathf.Exp(-8f * Time.deltaTime));
        }
        else transform.rotation = restRotation * Quaternion.Euler(0f, shuffle * 7f, 0f);
        if (prop != null)
            GroundHockeyStick(reach);
        float actualSpeed=Time.deltaTime>0f?Vector3.Distance(before,transform.position)/Time.deltaTime:0f;
        // A short locomotion blend now accompanies the shuffle. Keeping the
        // body idle while translating was what made the players look as if
        // they were sliding across the ice without moving their boots.
        SetAnimation(actualSpeed>0.045f?Mathf.Lerp(1.85f,2.35f,
            Mathf.InverseLerp(0.05f,1.4f,actualSpeed)):0f);
    }

    private void BuildHockeySet()
    {
        Material shaft = MakeMaterial(new Color(0.20f, 0.11f, 0.045f));
        Material tape = MakeMaterial(new Color(0.04f, 0.04f, 0.05f));
        GameObject stick = new("Working hockey stick");
        // The winger FBX contains an old rigid accessory stick. It follows a
        // hand bone and can point into the sky during retargeted animation;
        // hide it because the grounded gameplay stick below replaces it.
        foreach (Transform candidate in GetComponentsInChildren<Transform>(true))
        {
            string normalized = candidate.name.ToLowerInvariant().Replace(" ",string.Empty).Replace("_",string.Empty);
            if (normalized.Contains("accessoryhockeystick") || normalized.Contains("accessoryhockeyblade") ||
                normalized.Contains("accessorysledrope") || normalized.Contains("accessoryleash"))
                candidate.gameObject.SetActive(false);
        }
        // The blade, not the imported hand animation, owns the stick pose.
        // This prevents a raised hand or retargeting wobble from making the
        // entire stick fly above the hockey game.
        stick.transform.SetParent(transform, true);
        stick.transform.position = transform.position + Vector3.up * 0.65f;
        prop = stick.transform;
        Part(PrimitiveType.Cylinder, "Hockey stick shaft", prop,
            Vector3.zero, new Vector3(0.032f, 0.59f, 0.032f), shaft);
        Part(PrimitiveType.Cube, "Hockey stick blade", prop,
            new Vector3(0f, -0.57f, -0.14f), new Vector3(0.09f, 0.07f, 0.39f), tape);
    }

    private void GroundHockeyStick(float reach)
    {
        Vector3 blade = transform.position + transform.forward * 0.48f +
            transform.right * (kind == WinterCastActivityKind.HockeyGoalie ? -0.28f : 0.34f);
        float groundY = transform.position.y;
        float closest = float.PositiveInfinity;
        int count = Physics.RaycastNonAlloc(blade + Vector3.up * 1.8f, Vector3.down,
            stickGroundHits, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = stickGroundHits[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
            if(hit.normal.y<0.55f||hit.collider is CharacterController||
                hit.collider.GetComponentInParent<DannySpark>()!=null||
                hit.collider.GetComponentInParent<RiverValleyKidFollower>()!=null||
                hit.collider.GetComponentInParent<RiverValleyAnimalMotion>()!=null||
                hit.collider.GetComponentInParent<RiverValleySafeCar>()!=null||
                hit.collider.GetComponentInParent<RiverValleyHazardMover>()!=null)continue;
            string label=(hit.collider.name+" "+hit.collider.transform.root.name).ToLowerInvariant();
            if(label.Contains("solid exterior")||label.Contains("tree trunk")||
                label.Contains("visible brown trunk"))continue;
            if (hit.distance >= closest) continue;
            closest = hit.distance;
            groundY = hit.point.y;
        }
        blade.y = groundY + 0.075f;
        // Imported humanoid hand bones can be retargeted metres away from a
        // child-sized body. Never let that malformed pose stretch a stick
        // across the street: the grip is deliberately body-relative while the
        // blade remains planted by the ground raycast.
        Vector3 grip = transform.position + transform.right *
            (kind == WinterCastActivityKind.HockeyGoalie ? -0.18f : 0.26f) +
            transform.forward * 0.10f + Vector3.up * 0.98f;
        Vector3 shaft = grip - blade;
        if (shaft.sqrMagnitude < 0.1f) shaft = Vector3.up;
        Vector3 axis = shaft.normalized;
        prop.position = Vector3.Lerp(blade,grip,0.51f);
        prop.rotation = Quaternion.AngleAxis(reach * 18f,axis) *
            Quaternion.FromToRotation(Vector3.up,axis);
    }

    public void SetRallyPose(Vector3 targetPosition, float swing)
    {
        rallyActive = true;
        rallyTarget = targetPosition;
        rallySwing = Mathf.Clamp(swing, -1f, 1f);
    }

    private void BuildSnowboard()
    {
        Material board = MakeMaterial(new Color(0.05f, 0.62f, 0.92f));
        GameObject boardObject = Part(PrimitiveType.Cube, "Blue stair-dare snowboard", transform,
            new Vector3(0f, 0.055f, 0f), new Vector3(0.60f, 0.055f, 1.48f), board);
        prop = boardObject.transform;
    }

    private void SetAnimation(float speed)
    {
        if (animators == null) return;
        foreach (Animator animator in animators)
            animator?.SetFloat(SpeedHash, speed, 0.15f, Time.deltaTime);
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
        if (collider != null) Destroy(collider);
        return part;
    }

    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new(shader) { color = color, hideFlags = HideFlags.DontSave };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        return material;
    }

    public void OnFootstep(AnimationEvent animationEvent) { }
    public void OnLand(AnimationEvent animationEvent) { }

#if UNITY_EDITOR
    public void Configure(WinterCastActivityKind newKind) => kind = newKind;
#endif
}
