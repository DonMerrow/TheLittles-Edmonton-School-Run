using UnityEngine;

/// <summary>
/// Small, bounded background performance for the neighbourhood cast. Actors
/// stroll, pause, and notice Danny instead of standing like scene markers.
/// Scripted followers and parent groups automatically take priority.
/// </summary>
[DisallowMultipleComponent]
public sealed class RiverValleyAmbientActor : MonoBehaviour
{
    private const int GroundHitCapacity = 16;
    private readonly static int SpeedHash = Animator.StringToHash("Speed");
    private readonly RaycastHit[] groundHits = new RaycastHit[GroundHitCapacity];
    private Transform danny;
    private Animator[] animators;
    private Vector3 home;
    private Vector3 destination;
    private float walkingSpeed;
    private float roamingRadius;
    private float pauseUntil;
    private float noticeUntil;
    private bool canRoam;
    private System.Random random;

    private void Start()
    {
        DannySpark found = FindAnyObjectByType<DannySpark>();
        danny = found != null ? found.transform : null;
        animators = GetComponentsInChildren<Animator>(true);
        foreach (Animator animator in animators)
            if (animator != null && animator.GetComponent<WinterAnimationEventRelay>() == null)
                animator.gameObject.AddComponent<WinterAnimationEventRelay>();

        home = transform.position;
        int positionSeed = Mathf.RoundToInt(home.x * 31f + home.z * 73f);
        random = new System.Random(gameObject.name.GetHashCode() ^ positionSeed);
        WinterCastIdentity identity = GetComponent<WinterCastIdentity>();
        string role = identity != null ? identity.StoryRole.ToLowerInvariant() : string.Empty;
        canRoam = role.Contains("child") || role.Contains("teen") || role.Contains("neighbour") ||
            role.Contains("pedestrian") || role.Contains("helper") || role.Contains("player") ||
            role.Contains("hockey") || role.Contains("snow_fort") || role.Contains("sledding") ||
            role.Contains("classmate");
        walkingSpeed = Mathf.Lerp(0.78f, 1.18f, Next01());
        roamingRadius = Mathf.Lerp(1.5f, 3.2f, Next01());
        pauseUntil = Time.time + Mathf.Lerp(0.35f, 1.5f, Next01());
        ChooseDestination();
    }

    private void Update()
    {
        if (GetComponent<RiverValleyKidFollower>() != null ||
            GetComponentInParent<RiverValleyParentWalker>() != null)
        {
            SetAnimation(0f);
            enabled = false;
            return;
        }

        Vector3 toDanny = danny != null ? Vector3.ProjectOnPlane(danny.position - transform.position, Vector3.up) : Vector3.zero;
        if (Time.time < noticeUntil && toDanny.sqrMagnitude > 0.04f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(toDanny.normalized, Vector3.up), Time.deltaTime * 2.2f);
            SetAnimation(0f);
            return;
        }

        if (!canRoam || Time.time < pauseUntil)
        {
            SetAnimation(0f);
            return;
        }

        Vector3 flat = Vector3.ProjectOnPlane(destination - transform.position, Vector3.up);
        if (flat.magnitude < 0.12f)
        {
            pauseUntil = Time.time + Mathf.Lerp(0.55f, 2.2f, Next01());
            ChooseDestination();
            SetAnimation(0f);
            return;
        }

        Vector3 direction = flat.normalized;
        Vector3 next = transform.position + direction * walkingSpeed * Time.deltaTime;
        if (!TryFindGround(next, out RaycastHit ground) ||
            Mathf.Abs(ground.point.y - transform.position.y) > 0.62f)
        {
            destination = transform.position;
            pauseUntil = Time.time + Mathf.Lerp(0.45f, 1.2f, Next01());
            ChooseDestination();
            SetAnimation(0f);
            return;
        }
        next.y = ground.point.y + 0.015f;
        transform.position = next;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(direction, Vector3.up), Time.deltaTime * 4f);
        SetAnimation(Mathf.Lerp(2.18f, 3.15f, Mathf.InverseLerp(0.72f, 1.20f, walkingSpeed)));
    }

    private void ChooseDestination()
    {
        destination = transform.position;
        for (int attempt = 0; attempt < 8; attempt++)
        {
            float angle = Next01() * Mathf.PI * 2f;
            float radius = roamingRadius * Mathf.Lerp(0.35f, 1f, Next01());
            Vector3 candidate = home + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            if (!TryFindGround(candidate, out RaycastHit ground) || Mathf.Abs(ground.point.y - home.y) > 0.62f) continue;
            candidate.y = ground.point.y + 0.015f;
            destination = candidate;
            return;
        }
    }

    // Called by the Alberta greeting director. A neighbour pauses just long
    // enough to say hello, then resumes their own walk instead of becoming a
    // permanent scene marker around Danny.
    public void NoticePlayer(float seconds)
    {
        noticeUntil = Mathf.Max(noticeUntil, Time.time + Mathf.Max(0.2f, seconds));
    }

    private bool TryFindGround(Vector3 position, out RaycastHit bestHit)
    {
        bestHit = default;
        float bestDistance = float.PositiveInfinity;
        int count = Physics.RaycastNonAlloc(position + Vector3.up * 4f, Vector3.down,
            groundHits, 12f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = groundHits[i];
            if (candidate.collider == null || candidate.collider.transform.IsChildOf(transform)) continue;
            if(candidate.normal.y<0.48f||candidate.collider is CharacterController||
                candidate.collider.GetComponentInParent<DannySpark>()!=null||
                candidate.collider.GetComponentInParent<RiverValleyKidFollower>()!=null||
                candidate.collider.GetComponentInParent<RiverValleyAnimalMotion>()!=null||
                candidate.collider.GetComponentInParent<RiverValleySafeCar>()!=null||
                candidate.collider.GetComponentInParent<RiverValleyHazardMover>()!=null)continue;
            string label=(candidate.collider.name+" "+candidate.collider.transform.root.name).ToLowerInvariant();
            if(label.Contains("solid exterior")||label.Contains("tree trunk")||
                label.Contains("visible brown trunk"))continue;
            if (candidate.distance >= bestDistance) continue;
            bestDistance = candidate.distance;
            bestHit = candidate;
        }
        return bestDistance < float.PositiveInfinity;
    }

    private float Next01() => random != null ? (float)random.NextDouble() : 0.5f;

    private void SetAnimation(float speed)
    {
        if (animators == null) return;
        foreach (Animator animator in animators)
            if (animator != null)
            {
                animator.SetFloat(SpeedHash, speed, 0.10f, Time.deltaTime);
                animator.speed = speed > 0.1f ? Mathf.Lerp(0.96f,1.12f,Mathf.InverseLerp(2f,3.2f,speed)) : 1f;
            }
    }
}
