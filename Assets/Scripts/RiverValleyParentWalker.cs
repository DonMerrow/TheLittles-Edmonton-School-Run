using UnityEngine;

public sealed class RiverValleyParentWalker : MonoBehaviour
{
    [SerializeField] private float distance = 2.2f;
    [SerializeField] private float speed = 0.72f;
    private Vector3 origin;
    private Vector3 routeAxis;
    private Animator[] animators;
    private Transform[] castMembers;
    private readonly RaycastHit[] groundHits = new RaycastHit[12];

    private void Start()
    {
        origin = transform.position;
        routeAxis = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        if (routeAxis.sqrMagnitude < 0.01f) routeAxis = Vector3.forward;
        animators = GetComponentsInChildren<Animator>();
        WinterCastIdentity[] identities=GetComponentsInChildren<WinterCastIdentity>(true);
        castMembers=new Transform[identities.Length];
        for(int i=0;i<identities.Length;i++)castMembers[i]=identities[i].transform;
        foreach (Animator animator in animators)
            if (animator != null && animator.GetComponent<WinterAnimationEventRelay>() == null)
                animator.gameObject.AddComponent<WinterAnimationEventRelay>();
    }

    private void Update()
    {
        float phase = Mathf.Sin(Time.time * speed) * distance;
        Vector3 desired = origin + routeAxis * phase;
        Vector3 delta = desired - transform.position;
        Vector3 before = transform.position;
        Vector3 next = Vector3.MoveTowards(transform.position, desired, speed * Time.deltaTime);
        if (!TryFindGround(next, out RaycastHit ground) ||
            Mathf.Abs(ground.point.y - transform.position.y) > 0.62f)
        {
            next = transform.position;
            desired = origin;
            delta = desired - transform.position;
        }
        else next.y = ground.point.y + 0.015f;
        transform.position = next;
        float actualSpeed = Time.deltaTime > 0f ? Vector3.Distance(before, transform.position) / Time.deltaTime : 0f;
        Vector3 travelled=Vector3.ProjectOnPlane(transform.position-before,Vector3.up);
        if (travelled.sqrMagnitude > 0.000001f&&castMembers!=null)
        {
            Quaternion facing=Quaternion.LookRotation(travelled.normalized,Vector3.up);
            foreach(Transform member in castMembers)
                if(member!=null)member.rotation=Quaternion.Slerp(member.rotation,facing,
                    1f-Mathf.Exp(-6f*Time.deltaTime));
        }
        if (animators != null)
        {
            float animationSpeed = actualSpeed > 0.025f ? Mathf.Lerp(2.18f, 3.05f,
                Mathf.InverseLerp(0.05f, Mathf.Max(0.06f, speed), actualSpeed)) : 0f;
            foreach(Animator animator in animators)
                if(animator!=null)
                {
                    animator.SetFloat("Speed", animationSpeed, 0.10f, Time.deltaTime);
                    animator.speed=animationSpeed>0.1f?1.06f:1f;
                }
        }
    }

    private bool TryFindGround(Vector3 position, out RaycastHit bestHit)
    {
        bestHit = default;
        float bestDistance = float.PositiveInfinity;
        int count = Physics.RaycastNonAlloc(position + Vector3.up * 4f, Vector3.down,
            groundHits, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = groundHits[i];
            if (candidate.collider == null || candidate.collider.transform.IsChildOf(transform)) continue;
            if (candidate.distance >= bestDistance) continue;
            bestDistance = candidate.distance;
            bestHit = candidate;
        }
        return bestDistance < float.PositiveInfinity;
    }

#if UNITY_EDITOR
    public void Configure(float newDistance, float newSpeed) { distance=newDistance; speed=newSpeed; }
#endif
}
