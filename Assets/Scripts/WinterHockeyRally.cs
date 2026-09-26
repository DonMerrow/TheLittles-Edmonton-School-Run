using UnityEngine;

/// <summary>
/// Runs one readable street-hockey rally between two children. Edmonton kids
/// often substitute a tennis ball, so the bright ball remains visible against
/// both asphalt and snow while the players take turns swinging.
/// </summary>
public sealed class WinterHockeyRally : MonoBehaviour
{
    [SerializeField] private Transform playerA;
    [SerializeField] private Transform playerB;
    [SerializeField] private float travelSeconds = 1.35f;
    [SerializeField] private float tinyBounceHeight = 0.012f;
    private WinterCastActivity activityA;
    private WinterCastActivity activityB;
    private Transform ball;
    private Transform guestPlayer;
    private int previousHalfCycle = -1;
    private static int completedPasses;
    private static float highestGroundClearance;

    public static int CompletedPasses => completedPasses;
    public static float HighestGroundClearance => highestGroundClearance;
    public Vector3 Centre => playerA!=null&&playerB!=null
        ? Vector3.Lerp(playerA.position,playerB.position,0.5f) : transform.position;
    public Vector3 BallPosition => ball!=null?ball.position:Centre;
    public Vector3 PlayAxis
    {
        get
        {
            Vector3 axis=playerA!=null&&playerB!=null
                ? Vector3.ProjectOnPlane(playerB.position-playerA.position,Vector3.up) : Vector3.forward;
            return axis.sqrMagnitude>0.01f?axis.normalized:Vector3.forward;
        }
    }

    private void Start()
    {
        if (playerA == null || playerB == null) return;
        activityA = playerA.GetComponent<WinterCastActivity>();
        activityB = playerB.GetComponent<WinterCastActivity>();
        Material tennis = MakeMaterial(new Color(0.72f, 0.95f, 0.08f));
        GameObject ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballObject.name = "Bright tennis ball rolling between hockey players";
        ballObject.transform.SetParent(transform, true);
        ballObject.transform.localScale = Vector3.one * 0.14f;
        Collider collider = ballObject.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        ballObject.GetComponent<Renderer>().sharedMaterial = tennis;
        ball = ballObject.transform;
    }

    private void Update()
    {
        if (ball == null || playerA == null || playerB == null) return;
        float cycle = Time.time / Mathf.Max(0.35f, travelSeconds);
        int halfCycle = Mathf.FloorToInt(cycle);
        float t = cycle - halfCycle;
        bool towardB = (halfCycle & 1) == 0;
        Transform from;
        Transform to;
        if(guestPlayer!=null)
        {
            switch(Mathf.Abs(halfCycle)%4)
            {
                case 0: from=playerA; to=guestPlayer; break;
                case 1: from=guestPlayer; to=playerB; break;
                case 2: from=playerB; to=guestPlayer; break;
                default: from=guestPlayer; to=playerA; break;
            }
        }
        else
        {
            from=towardB?playerA:playerB;
            to=towardB?playerB:playerA;
        }
        Vector3 start = ContactPoint(from, to.position);
        Vector3 end = ContactPoint(to, from.position);
        float eased = Mathf.SmoothStep(0f, 1f, t);
        Vector3 roadPosition = Vector3.Lerp(start, end, eased);
        float groundY;
        if (Physics.Raycast(roadPosition + Vector3.up * 1.8f, Vector3.down, out RaycastHit hit, 4f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            groundY = hit.point.y;
        else
            groundY = Mathf.Min(start.y, end.y);
        roadPosition.y = groundY + 0.071f;
        // A tennis ball may chatter on frozen asphalt, but it must never read
        // as a puck floating waist-high through a hockey game.
        roadPosition.y += Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * tinyBounceHeight;
        ball.position = roadPosition;
        highestGroundClearance = Mathf.Max(highestGroundClearance,ball.position.y-groundY);
        ball.Rotate(Vector3.right, 520f * Time.deltaTime, Space.Self);

        float hitWindow = Mathf.Clamp01(1f - Mathf.Min(t, 1f - t) * 8f);
        float hitDirection = towardB ? 1f : -1f;
        // Each teen watches the other end of the pass.  During Danny's guest
        // rally, using `to` for both actors occasionally made the receiver
        // look at their own position and keep a stale facing direction.
        Vector3 playerATarget=from==playerA?to.position:from.position;
        Vector3 playerBTarget=from==playerB?to.position:from.position;
        activityA?.SetRallyPose(playerATarget, from==playerA ? hitWindow * hitDirection : -hitWindow * 0.35f);
        activityB?.SetRallyPose(playerBTarget, from==playerB ? -hitWindow * hitDirection : hitWindow * 0.35f);
        if (halfCycle != previousHalfCycle)
        {
            if (previousHalfCycle >= 0) completedPasses++;
            previousHalfCycle = halfCycle;
        }
    }

    public void BeginGuestRally(Transform guest) => guestPlayer=guest;
    public void EndGuestRally() => guestPlayer=null;

    private static Vector3 ContactPoint(Transform player, Vector3 toward)
    {
        Vector3 direction = Vector3.ProjectOnPlane(toward - player.position, Vector3.up);
        if(direction.sqrMagnitude<0.001f)direction=Vector3.forward;
        else direction.Normalize();
        return player.position + direction * 0.78f;
    }

    private static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new(shader) { color = color, hideFlags = HideFlags.DontSave };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.22f);
        }
        return material;
    }

#if UNITY_EDITOR
    public void Configure(Transform firstPlayer, Transform secondPlayer)
    {
        playerA = firstPlayer;
        playerB = secondPlayer;
    }
#endif
}
