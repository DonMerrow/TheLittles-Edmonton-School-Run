using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class DannyFootGrounding : MonoBehaviour
{
    [SerializeField] private float probeHeight = 0.20f;
    [SerializeField] private float probeDistance = 0.42f;
    [SerializeField] private float probeRadius = 0.025f;
    [SerializeField] private float soleHeight = 0.028f;
    [SerializeField] private float positionStrength = 0.92f;
    [SerializeField] private float rotationStrength = 0.55f;
    [SerializeField] private float weightSpeed = 12f;

    private readonly RaycastHit[] hits = new RaycastHit[12];
    private Animator animator;
    private CharacterController controller;
    private Transform playerRoot;
    private DannyWinterTrail winterTrail;
    private float leftWeight;
    private float rightWeight;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponentInParent<CharacterController>();
        playerRoot = controller != null ? controller.transform : transform.root;
        winterTrail = GetComponent<DannyWinterTrail>();
        animator.stabilizeFeet = true;
        animator.feetPivotActive = 0.5f;
    }

    private void OnAnimatorIK(int layerIndex)
    {
        bool allowGrounding = controller == null || controller.isGrounded;
        ApplyFoot(AvatarIKGoal.LeftFoot, ref leftWeight, allowGrounding);
        ApplyFoot(AvatarIKGoal.RightFoot, ref rightWeight, allowGrounding);
    }

    // Starter Assets locomotion clips contain these optional sound events.
    // Danny's test scene has no audio system yet, so receive them quietly.
    public void OnFootstep(AnimationEvent animationEvent) => winterTrail?.OnFootstep(animationEvent);
    public void OnLand(AnimationEvent animationEvent) => winterTrail?.OnLand(animationEvent);

    private void ApplyFoot(AvatarIKGoal goal, ref float currentWeight, bool allowGrounding)
    {
        Vector3 animatedPosition = animator.GetIKPosition(goal);
        bool foundGround = false;
        RaycastHit bestHit = default;
        float bestDistance = float.PositiveInfinity;

        if (allowGrounding)
        {
            Vector3 origin = animatedPosition + Vector3.up * probeHeight;
            int count = Physics.SphereCastNonAlloc(origin, probeRadius, Vector3.down, hits,
                probeDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int index = 0; index < count; index++)
            {
                RaycastHit candidate = hits[index];
                if (candidate.collider == null || candidate.collider.transform.IsChildOf(playerRoot))
                    continue;
                if (candidate.distance < bestDistance)
                {
                    bestDistance = candidate.distance;
                    bestHit = candidate;
                    foundGround = true;
                }
            }
        }

        float targetWeight = foundGround ? positionStrength : 0f;
        currentWeight = Mathf.MoveTowards(currentWeight, targetWeight, weightSpeed * Time.deltaTime);
        animator.SetIKPositionWeight(goal, currentWeight);
        float rotationWeight = foundGround ? Mathf.Max(0.72f, currentWeight * rotationStrength) : 0.62f;
        animator.SetIKRotationWeight(goal, rotationWeight);

        Vector3 surfaceNormal = foundGround ? bestHit.normal : Vector3.up;
        Quaternion animatedRotation = animator.GetIKRotation(goal);
        Vector3 footForward = Vector3.ProjectOnPlane(animatedRotation * Vector3.forward, surfaceNormal).normalized;
        Vector3 bodyForward = Vector3.ProjectOnPlane(playerRoot.forward, surfaceNormal).normalized;
        // The imported humanoid occasionally resolves an ankle 180 degrees
        // around during retargeting. Correct only that backwards case, while
        // preserving the natural toe lift and small toe-out of the animation.
        if (footForward.sqrMagnitude > 0.1f && bodyForward.sqrMagnitude > 0.1f &&
            Vector3.Dot(footForward, bodyForward) < -0.15f)
            animatedRotation = Quaternion.AngleAxis(180f, surfaceNormal) * animatedRotation;
        animator.SetIKRotation(goal, foundGround
            ? Quaternion.FromToRotation(Vector3.up, surfaceNormal) * animatedRotation
            : animatedRotation);

        if (!foundGround) return;
        Vector3 groundedPosition = animatedPosition;
        groundedPosition.y = bestHit.point.y + soleHeight;
        animator.SetIKPosition(goal, groundedPosition);
    }
}
