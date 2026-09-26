using UnityEngine;

/// <summary>
/// Keeps a retargeted humanoid visually connected to the CharacterController floor.
/// Different avatars and low crawl poses do not share the same animated body height,
/// so the lowest rendered point is aligned while grounded. Airborne animation remains
/// untouched so jumping and falling keep their proper arcs.
/// </summary>
[DisallowMultipleComponent]
public sealed class MothGrounding : MonoBehaviour
{
    public Transform VisualRoot;
    public CharacterController CharacterController;
    public Animator Animator;

    [Tooltip("Maximum automatic vertical correction in metres.")]
    public float MaximumCorrection = 1.25f;

    [Tooltip("How quickly the visible body settles onto the ground.")]
    public float GroundingSpeed = 12f;

    [Tooltip("Small clearance between the rendered feet and the floor.")]
    public float SoleClearance = 0.01f;

    [Tooltip("Snap the visible feet to the floor while grounded. This prevents a slow settling drift after landing.")]
    public bool SnapWhileGrounded = true;

    private int _groundedId;
    private bool _hasGroundedParameter;
    private bool _ready;
    private bool _baseHeightCaptured;
    private float _baseLocalY;

    private void Awake()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (VisualRoot == null) VisualRoot = FindDeepChild(transform, "MothVisual");
        if (VisualRoot == null) VisualRoot = FindDeepChild(transform, "Moth Model");
        if (CharacterController == null) CharacterController = GetComponent<CharacterController>();
        if (Animator == null) Animator = GetComponentInChildren<Animator>(true);

        if (VisualRoot != null && !_baseHeightCaptured)
        {
            _baseLocalY = VisualRoot.localPosition.y;
            _baseHeightCaptured = true;
        }

        _groundedId = UnityEngine.Animator.StringToHash("Grounded");
        _hasGroundedParameter = Animator != null && HasBoolParameter(Animator, _groundedId);
        _ready = VisualRoot != null && CharacterController != null;
    }

    private void LateUpdate()
    {
        if (!_ready)
        {
            ResolveReferences();
            if (!_ready) return;
        }

        // The controller is the physical source of truth. Animator parameters can
        // be one frame late, and the older 3D test controller stores its Animator
        // on the visual child rather than on this GameObject.
        bool physicallyGrounded = CharacterController.isGrounded;
        bool animationGrounded = _hasGroundedParameter && Animator.GetBool(_groundedId);
        if (!physicallyGrounded && !animationGrounded) return;

        Renderer[] renderers = VisualRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        Bounds visibleBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            visibleBounds.Encapsulate(renderers[i].bounds);

        float controllerBottom = transform.TransformPoint(CharacterController.center).y -
            CharacterController.height * 0.5f * Mathf.Abs(transform.lossyScale.y);
        float groundY = controllerBottom;

        // RaycastAll is intentional. A single ray can return the player's own
        // collider first and silently discard the actual floor underneath it.
        Vector3 rayOrigin = transform.position + Vector3.up *
            (CharacterController.height + CharacterController.stepOffset + 0.25f);
        RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down,
            CharacterController.height + CharacterController.stepOffset + 1.5f,
            ~0, QueryTriggerInteraction.Ignore);
        float bestFloor = float.NegativeInfinity;
        float highestWalkableFloor = controllerBottom + CharacterController.stepOffset +
            CharacterController.skinWidth + 0.15f;
        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == null || hitTransform == transform ||
                hitTransform.IsChildOf(transform)) continue;
            if (hits[i].point.y <= highestWalkableFloor && hits[i].point.y > bestFloor)
                bestFloor = hits[i].point.y;
        }
        if (!float.IsNegativeInfinity(bestFloor)) groundY = bestFloor;

        float worldCorrection = groundY + SoleClearance - visibleBounds.min.y;
        float localCorrection = worldCorrection / Mathf.Max(0.0001f, transform.lossyScale.y);

        Vector3 localPosition = VisualRoot.localPosition;
        float targetY = Mathf.Clamp(localPosition.y + localCorrection,
            _baseLocalY - MaximumCorrection, _baseLocalY + MaximumCorrection);
        localPosition.y = SnapWhileGrounded
            ? targetY
            : Mathf.MoveTowards(localPosition.y, targetY, GroundingSpeed * Time.deltaTime);
        VisualRoot.localPosition = localPosition;
    }

    private static bool HasBoolParameter(Animator animator, int nameHash)
    {
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.nameHash == nameHash && parameter.type == AnimatorControllerParameterType.Bool)
                return true;
        return false;
    }

    private static Transform FindDeepChild(Transform root, string objectName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == objectName) return child;
        return null;
    }
}
