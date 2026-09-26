using System.Collections;
using StarterAssets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Small-character traversal. Solid, mostly vertical faces are climbable.
/// The CharacterController remains active on walls; only a validated mantle
/// temporarily takes direct control of the transform.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class LittleLedgeClimber : MonoBehaviour
{
    [Header("Detection")]
    public float ForwardCheck = 0.68f;
    public float MinimumLedgeHeight = 0.12f;
    public float MaximumLedgeHeight = 1.25f;
    public float WallClearance = 0.025f;
    public LayerMask ClimbableLayers = ~0;

    [Header("Motion")]
    [Tooltip("When enabled, Space only starts a validated ledge mantle. The player cannot free-climb walls.")]
    public bool MantleOnly = true;
    public float HangBodyDrop = 0.5f;
    public float ClimbDuration = 0.5f;
    public float WallClimbSpeed = 1.0f;
    public float WallTraverseSpeed = 0.85f;
    public float WallTurnSpeed = 14f;
    public float JumpAwaySpeed = 1.8f;

    [Header("Personality")]
    [Range(0f, 1f)] public float EffortVariation = 0.18f;
    public float FatigueRate = 0.085f;
    public float RecoveryRate = 0.22f;
    public float MinimumSecondsBeforeSlip = 5.5f;
    public float MaximumSecondsBeforeSlip = 8.5f;

    private enum TraversalState { None, Attach, Wall, Hang, Mantle }

    private CharacterController _capsule;
    private ThirdPersonController _motor;
    private StarterAssetsInputs _input;
    private Animator _animator;
    private MothGrounding _grounding;
    private TraversalState _state;
    private Vector3 _wallNormal;
    private Vector3 _ledgePoint;
    private float _stateTime;
    private bool _jumpHeldLastFrame;
    private float _climbPhase;
    private float _fatigue;
    private float _slipTimer;
    private float _nextSlipTime;
    private Transform _visualRoot;
    private Transform _hips;
    private Transform _leftUpperArm;
    private Transform _leftLowerArm;
    private Transform _leftHand;
    private Transform _rightUpperArm;
    private Transform _rightLowerArm;
    private Transform _rightHand;
    private Transform _leftUpperLeg;
    private Transform _leftLowerLeg;
    private Transform _leftFoot;
    private Transform _rightUpperLeg;
    private Transform _rightLowerLeg;
    private Transform _rightFoot;
    private Quaternion _visualGroundRotation;
    private Vector3 _visualGroundPosition;
    private bool _visualRotationCaptured;
    private Vector3 _attachWallPoint;
    private float _lostWallContactTime;

    private readonly Vector3[] _ikPositions = new Vector3[4];
    private readonly Quaternion[] _ikRotations = new Quaternion[4];
    private readonly bool[] _ikValid = new bool[4];
    private float _ikWeight;

    public bool IsHanging => _state == TraversalState.Hang;
    public bool IsClimbing => _state == TraversalState.Mantle;
    public bool IsWallClimbing => _state == TraversalState.Wall;
    public bool IsTraversing => _state != TraversalState.None;

    private float RootWallDistance => _capsule.radius + _capsule.skinWidth + WallClearance;

    private void Awake()
    {
        _capsule = GetComponent<CharacterController>();
        _motor = GetComponent<ThirdPersonController>();
        _input = GetComponent<StarterAssetsInputs>();
        _animator = GetComponent<Animator>();
        _grounding = GetComponent<MothGrounding>();
    }

    private void Update()
    {
        bool jumpHeld = ReadJumpHeld();
        bool jumpPressed = jumpHeld && !_jumpHeldLastFrame;
        _jumpHeldLastFrame = jumpHeld;
        _stateTime += Time.deltaTime;

        if (_state != TraversalState.None)
        {
            if (_state == TraversalState.Hang)
                _fatigue = Mathf.MoveTowards(_fatigue, 0f, RecoveryRate * Time.deltaTime);
            if (DropPressed() || (_state == TraversalState.Hang && ReadMove().y < -0.65f))
            {
                EndTraversal(_wallNormal * 0.12f);
                return;
            }

            if (JumpAwayPressed())
            {
                EndTraversal((_wallNormal + Vector3.up * 0.45f).normalized * JumpAwaySpeed);
                return;
            }

            if (_state == TraversalState.Attach) UpdateAttach();
            else if (_state == TraversalState.Wall) UpdateWall(jumpHeld);
            else if (_state == TraversalState.Hang && _stateTime > 0.12f &&
                     (jumpPressed || jumpHeld || ReadMove().y > 0.35f))
                StartCoroutine(MantleRoutine());
            return;
        }

        Vector2 move = ReadMove();
        bool wantsSurface = move.y > 0.12f && (jumpHeld || (_motor != null && !_motor.Grounded));
        if (wantsSurface) TryBeginTraversal();
    }

    private void TryBeginTraversal()
    {
        if (!TryFindWall(transform.position, transform.forward, out RaycastHit wall)) return;
        if (!IsBroadClimbableFace(wall)) return;

        if (MantleOnly)
        {
            // A normal wall is not a ladder. Only take control when there is a
            // real, reachable top surface; otherwise Starter Assets keeps the
            // input and performs its ordinary jump.
            if (!TryFindTop(wall.point, wall.normal, transform.position.y,
                    MaximumLedgeHeight, out RaycastHit top)) return;
            BeginDirectMantle(top.point, wall.normal);
            return;
        }

        // Always make contact with the wall first. Going straight to Hang made
        // the character flash/blip into place before the climb had visibly begun.
        BeginWall(wall.point, wall.normal);
    }

    private void BeginDirectMantle(Vector3 ledgePoint, Vector3 normal)
    {
        TakeControl(TraversalState.Hang, normal);
        _ledgePoint = ledgePoint;
        FaceWallImmediate();
        StartCoroutine(MantleRoutine());
    }

    private bool TryFindWall(Vector3 root, Vector3 direction, out RaycastHit best)
    {
        best = default;
        float bestDistance = float.MaxValue;
        float[] heights = { _capsule.height * 0.28f, _capsule.height * 0.55f };

        for (int h = 0; h < heights.Length; h++)
        {
            Vector3 origin = root + Vector3.up * heights[h];
            RaycastHit[] hits = Physics.SphereCastAll(origin, _capsule.radius * 0.34f,
                direction.normalized, ForwardCheck, ClimbableLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (IsSelf(hit.transform) || Vector3.Dot(hit.normal, Vector3.up) > 0.36f) continue;
                if (hit.distance >= bestDistance) continue;
                best = hit;
                bestDistance = hit.distance;
            }
        }
        return bestDistance < float.MaxValue;
    }

    private bool TryFindTop(Vector3 wallPoint, Vector3 wallNormal, float rootY, float searchHeight,
        out RaycastHit best)
    {
        best = default;
        Vector3 origin = wallPoint - wallNormal * (_capsule.radius + 0.09f) +
                         Vector3.up * (searchHeight + 0.18f);
        RaycastHit[] hits = Physics.SphereCastAll(origin, _capsule.radius * 0.42f, Vector3.down,
            searchHeight + 0.35f, ClimbableLayers, QueryTriggerInteraction.Ignore);
        float bestY = float.MaxValue;
        bool found = false;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (IsSelf(hit.transform) || Vector3.Dot(hit.normal, Vector3.up) < 0.68f) continue;
            float height = hit.point.y - rootY;
            if (height < MinimumLedgeHeight || height > searchHeight + 0.05f) continue;
            if (height >= bestY) continue;
            best = hit;
            bestY = height;
            found = true;
        }
        return found;
    }

    private void BeginWall(Vector3 wallPoint, Vector3 normal)
    {
        TakeControl(TraversalState.Attach, normal);
        _lostWallContactTime = 0f;
        _attachWallPoint = wallPoint;
        FaceWall();
        SnapToWall(wallPoint);
    }

    private void UpdateAttach()
    {
        FaceWall();
        SnapToWall(_attachWallPoint);
        SetFloatIfPresent("CrawlMotionSpeed", 0.28f);
        // A small deliberate grab/lift prevents the rotated crawl pose from
        // beginning folded on the floor.
        _capsule.Move(Vector3.up * (0.32f * Time.deltaTime));
        if (_stateTime < 0.24f) return;
        _state = TraversalState.Wall;
        _stateTime = 0f;
        SetAnimatorTraversalPose();
    }

    private void BeginHang(Vector3 ledgePoint, Vector3 normal)
    {
        TakeControl(TraversalState.Hang, normal);
        _ledgePoint = ledgePoint;
        FaceWall();
        Vector3 target = ledgePoint + _wallNormal * RootWallDistance - Vector3.up * HangBodyDrop;
        _capsule.Move(target - transform.position);
    }

    private void TakeControl(TraversalState state, Vector3 normal)
    {
        _state = state;
        _stateTime = 0f;
        _wallNormal = normal.normalized;
        _fatigue = 0f;
        _slipTimer = 0f;
        _nextSlipTime = Time.time + Random.Range(MinimumSecondsBeforeSlip, MaximumSecondsBeforeSlip);
        if (_motor != null) _motor.enabled = false;
        if (_grounding != null) _grounding.enabled = false;
        if (_input != null) _input.jump = false;
        CaptureVisualRoot();
        SetAnimatorTraversalPose();
    }

    private void LateUpdate()
    {
        if (_state == TraversalState.None) return;
        // Animator evaluation happens after Update. Reassert wall alignment in
        // LateUpdate so authored root curves cannot turn the character backwards.
        FaceWallImmediate();
        ApplyWallVisualRotation();
    }

    // Wall translation remains available, but limb IK is deliberately disabled.
    // This character's imported Avatar has incompatible limb axes; forcing IK
    // produces crossed arms and reversed legs. A neutral pose is safer until a
    // compatible authored wall-climb animation is supplied.
    private void OnAnimatorIK(int layerIndex)
    {
        if (_animator == null) return;
        _ikWeight = 0f;
        ClearWallIK();
    }

    private void UpdateWall(bool jumpHeld)
    {
        Vector2 input = ReadMove();
        float vertical = Mathf.Clamp(input.y, -1f, 1f);
        if (jumpHeld) vertical = Mathf.Max(vertical, 1f);
        float lateral = input.x;
        float activity = Mathf.Clamp01(Mathf.Abs(vertical) + Mathf.Abs(lateral) * 0.65f);
        _climbPhase += Time.deltaTime * Mathf.Lerp(1.8f, 6.5f, activity);
        SetFloatIfPresent("CrawlMotionSpeed", Mathf.Lerp(0.3f, 1.15f, activity));

        if (vertical > 0.15f)
            _fatigue = Mathf.Clamp01(_fatigue + FatigueRate * vertical * Time.deltaTime);
        else
            _fatigue = Mathf.MoveTowards(_fatigue, 0f, RecoveryRate * Time.deltaTime);

        if (_slipTimer <= 0f && vertical > 0.55f && _fatigue > 0.62f && Time.time >= _nextSlipTime)
        {
            _slipTimer = 0.34f;
            _fatigue = Mathf.Max(0.32f, _fatigue - 0.24f);
            _nextSlipTime = Time.time + Random.Range(MinimumSecondsBeforeSlip, MaximumSecondsBeforeSlip);
        }

        float effortPulse = 1f - EffortVariation *
            (0.5f + 0.5f * Mathf.Sin(_climbPhase * 1.15f));
        float tiredSpeed = Mathf.Lerp(1f, 0.72f, _fatigue);
        Vector3 desired;
        if (_slipTimer > 0f)
        {
            _slipTimer -= Time.deltaTime;
            float slipFall = Mathf.Sin(Mathf.Clamp01(_slipTimer / 0.34f) * Mathf.PI);
            desired = Vector3.down * (0.18f + 0.34f * slipFall);
        }
        else
        {
            desired = Vector3.up * (vertical * WallClimbSpeed * effortPulse * tiredSpeed) +
                      transform.right * (lateral * WallTraverseSpeed * tiredSpeed);
        }
        _capsule.Move(desired * Time.deltaTime);

        if (TryFollowWall(lateral))
        {
            _lostWallContactTime = 0f;
            FaceWall();
            if (vertical > 0.1f && TryFindTop(transform.position - _wallNormal * RootWallDistance,
                    _wallNormal, transform.position.y, 0.82f, out RaycastHit top))
            {
                _ledgePoint = top.point;
                _state = TraversalState.Hang;
                _stateTime = 0f;
                SetAnimatorTraversalPose();
                Vector3 target = top.point + _wallNormal * RootWallDistance - Vector3.up * HangBodyDrop;
                _capsule.Move(target - transform.position);
            }
            return;
        }

        // Curved poles can briefly look like a wall, then provide a completely
        // different normal on the following frame. Never leave the motor locked
        // while contact is ambiguous: give a flat wall a few frames to recover,
        // then release cleanly.
        _lostWallContactTime += Time.deltaTime;
        if (_lostWallContactTime < 0.22f) return;

        // The face ended. A top means a mantle; otherwise try wrapping a corner.
        if (TryFindTop(transform.position - _wallNormal * RootWallDistance, _wallNormal,
                transform.position.y, 0.95f, out RaycastHit rim))
        {
            _ledgePoint = rim.point;
            _state = TraversalState.Hang;
            _stateTime = 0f;
            SetAnimatorTraversalPose();
            return;
        }

        if (Mathf.Abs(lateral) < 0.15f || !TryTurnCorner(Mathf.Sign(lateral)))
            EndTraversal(_wallNormal * 0.08f);
    }

    private bool TryFollowWall(float lateral)
    {
        Vector3 chest = transform.position + Vector3.up * (_capsule.height * 0.53f);
        Vector3 toward = -_wallNormal;
        if (TryFindWallAt(chest, toward, ForwardCheck + RootWallDistance, out RaycastHit wall))
        {
            _wallNormal = Vector3.Slerp(_wallNormal, wall.normal.normalized,
                1f - Mathf.Exp(-WallTurnSpeed * Time.deltaTime));
            SnapToWall(wall.point);
            return true;
        }

        if (Mathf.Abs(lateral) > 0.15f) return TryTurnCorner(Mathf.Sign(lateral));
        return false;
    }

    private bool TryTurnCorner(float direction)
    {
        Vector3 chest = transform.position + Vector3.up * (_capsule.height * 0.53f);
        float[] angles = { 38f, 72f, 100f };
        for (int i = 0; i < angles.Length; i++)
        {
            Vector3 probeDirection = Quaternion.AngleAxis(angles[i] * direction, Vector3.up) * -_wallNormal;
            if (!TryFindWallAt(chest, probeDirection, ForwardCheck + RootWallDistance, out RaycastHit corner))
                continue;
            _wallNormal = corner.normal.normalized;
            SnapToWall(corner.point);
            return true;
        }
        return false;
    }

    private bool TryFindWallAt(Vector3 origin, Vector3 direction, float distance, out RaycastHit best)
    {
        best = default;
        float nearest = float.MaxValue;
        RaycastHit[] hits = Physics.SphereCastAll(origin, _capsule.radius * 0.28f,
            direction.normalized, distance, ClimbableLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (IsSelf(hit.transform) || Vector3.Dot(hit.normal, Vector3.up) > 0.4f) continue;
            if (hit.distance >= nearest) continue;
            best = hit;
            nearest = hit.distance;
        }
        return nearest < float.MaxValue;
    }

    private bool IsBroadClimbableFace(RaycastHit centre)
    {
        Vector3 side = Vector3.Cross(Vector3.up, centre.normal).normalized;
        if (side.sqrMagnitude < 0.5f) return false;
        Vector3 chest = transform.position + Vector3.up * (_capsule.height * 0.53f);
        float spread = Mathf.Max(_capsule.radius * 0.72f, 0.11f);
        bool left = TryFindWallAt(chest - side * spread, -centre.normal,
            ForwardCheck + RootWallDistance, out RaycastHit leftHit);
        bool right = TryFindWallAt(chest + side * spread, -centre.normal,
            ForwardCheck + RootWallDistance, out RaycastHit rightHit);
        if (!left || !right) return false;
        // Both shoulder probes must describe one planar surface. This rejects
        // poles without changing normal walls or wide rounded building corners.
        return Vector3.Dot(leftHit.normal, centre.normal) > 0.90f &&
               Vector3.Dot(rightHit.normal, centre.normal) > 0.90f &&
               Vector3.Dot(leftHit.normal, rightHit.normal) > 0.90f;
    }

    private void SnapToWall(Vector3 wallPoint)
    {
        float distance = Vector3.Dot(transform.position - wallPoint, _wallNormal);
        Vector3 correction = _wallNormal * (RootWallDistance - distance);
        correction.y = 0f;
        _capsule.Move(correction);
    }

    private void FaceWall()
    {
        Quaternion target = Quaternion.LookRotation(-_wallNormal, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target,
            1f - Mathf.Exp(-WallTurnSpeed * Time.deltaTime));
    }

    private void FaceWallImmediate()
    {
        if (_wallNormal.sqrMagnitude < 0.5f) return;
        transform.rotation = Quaternion.LookRotation(-_wallNormal, Vector3.up);
    }

    private void CaptureVisualRoot()
    {
        if (_visualRoot == null && _grounding != null) _visualRoot = _grounding.VisualRoot;
        if (_visualRoot == null)
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                if (child.name == "MothVisual") { _visualRoot = child; break; }
        if (_visualRoot == null || _visualRotationCaptured) return;
        _visualGroundRotation = _visualRoot.localRotation;
        _visualGroundPosition = _visualRoot.localPosition;
        if (_animator != null && _animator.isHuman)
        {
            _hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
            _leftUpperArm = _animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            _leftLowerArm = _animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            _leftHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
            _rightUpperArm = _animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            _rightLowerArm = _animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            _rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            _leftUpperLeg = _animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            _leftLowerLeg = _animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            _leftFoot = _animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            _rightUpperLeg = _animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            _rightLowerLeg = _animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            _rightFoot = _animator.GetBoneTransform(HumanBodyBones.RightFoot);
        }
        _visualRotationCaptured = true;
    }

    /// <summary>
    /// Animator IK is not invoked by every imported controller. This solver runs
    /// after Animator evaluation and therefore always produces a visible wall
    /// pose. Repeated two-bone aiming gives a stable, lightweight parkour motion
    /// without requiring another retargeted Mixamo clip.
    /// </summary>
    private void ApplyGuaranteedWallPose()
    {
        bool wallState = _state == TraversalState.Attach ||
                         _state == TraversalState.Wall ||
                         _state == TraversalState.Hang;
        if (!wallState || _animator == null || !_animator.isHuman) return;
        CaptureVisualRoot();
        if (_leftHand == null || _rightHand == null ||
            _leftFoot == null || _rightFoot == null) return;

        Vector2 input = ReadMove();
        float scale = Mathf.Max(0.22f, _capsule.height / 1.8f);
        // FaceWall points the character toward -wallNormal. This cross-product
        // therefore matches the character's actual right side. The reverse
        // order swapped left/right IK targets and crossed every limb.
        Vector3 right = Vector3.Cross(_wallNormal, Vector3.up).normalized;
        Vector3 wallPoint = transform.position - _wallNormal * RootWallDistance +
                            _wallNormal * 0.018f;
        float activity = Mathf.Clamp01(input.magnitude * 1.35f);
        float waveA = Mathf.Sin(_climbPhase) * activity;
        float waveB = Mathf.Sin(_climbPhase + Mathf.PI) * activity;
        float verticalAmount = Mathf.Abs(input.y);
        float sidewaysAmount = Mathf.Abs(input.x);

        Vector3 leftHandTarget = wallPoint +
            Vector3.up * ((1.39f + waveA * 0.16f * verticalAmount) * scale) +
            right * ((-0.27f + waveA * 0.17f * sidewaysAmount) * scale);
        Vector3 rightHandTarget = wallPoint +
            Vector3.up * ((1.39f + waveB * 0.16f * verticalAmount) * scale) +
            right * ((0.27f + waveB * 0.17f * sidewaysAmount) * scale);
        Vector3 leftFootTarget = wallPoint +
            Vector3.up * ((0.43f + waveB * 0.13f * verticalAmount) * scale) +
            right * ((-0.19f + waveB * 0.13f * sidewaysAmount) * scale);
        Vector3 rightFootTarget = wallPoint +
            Vector3.up * ((0.43f + waveA * 0.13f * verticalAmount) * scale) +
            right * ((0.19f + waveA * 0.13f * sidewaysAmount) * scale);

        if (_state == TraversalState.Attach)
        {
            float grab = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_stateTime / 0.24f));
            leftHandTarget += Vector3.up * (0.13f * scale * grab);
            rightHandTarget += Vector3.up * (0.13f * scale * grab);
        }
        else if (_state == TraversalState.Hang)
        {
            leftHandTarget = _ledgePoint + _wallNormal * 0.018f - right * (0.25f * scale);
            rightHandTarget = _ledgePoint + _wallNormal * 0.018f + right * (0.25f * scale);
            leftFootTarget = wallPoint + Vector3.up * (0.34f * scale) - right * (0.18f * scale);
            rightFootTarget = wallPoint + Vector3.up * (0.34f * scale) + right * (0.18f * scale);
        }

        SolveTwoBone(_leftUpperArm, _leftLowerArm, _leftHand, leftHandTarget);
        SolveTwoBone(_rightUpperArm, _rightLowerArm, _rightHand, rightHandTarget);
        SolveTwoBone(_leftUpperLeg, _leftLowerLeg, _leftFoot, leftFootTarget);
        SolveTwoBone(_rightUpperLeg, _rightLowerLeg, _rightFoot, rightFootTarget);

        Quaternion palmFacing = Quaternion.LookRotation(-_wallNormal, Vector3.up);
        _leftHand.rotation = Quaternion.Slerp(_leftHand.rotation, palmFacing, 0.72f);
        _rightHand.rotation = Quaternion.Slerp(_rightHand.rotation, palmFacing, 0.72f);
    }

    private static void SolveTwoBone(Transform root, Transform middle, Transform end,
        Vector3 target)
    {
        if (root == null || middle == null || end == null) return;
        // Two CCD passes are enough for this short humanoid chain and avoid the
        // snapping produced by solving an unreachable target in one rotation.
        for (int iteration = 0; iteration < 2; iteration++)
        {
            AimBone(middle, end, target, 0.82f);
            AimBone(root, end, target, 0.72f);
        }
    }

    private static void AimBone(Transform bone, Transform end, Vector3 target, float weight)
    {
        Vector3 current = end.position - bone.position;
        Vector3 desired = target - bone.position;
        if (current.sqrMagnitude < 0.000001f || desired.sqrMagnitude < 0.000001f) return;
        Quaternion delta = Quaternion.FromToRotation(current.normalized, desired.normalized);
        bone.rotation = Quaternion.Slerp(bone.rotation, delta * bone.rotation, weight);
    }

    private void ApplyWallVisualRotation()
    {
        CaptureVisualRoot();
        if (_visualRoot == null) return;
        // Wall traversal now uses an upright humanoid base pose plus IK. The old
        // version rotated a floor-crawl clip by ninety degrees, which is why the
        // character looked horizontal, clumsy and spider-like on the wall.
        _visualRoot.localRotation = _visualGroundRotation;
        _visualRoot.localPosition = _visualGroundPosition;
    }

    private IEnumerator MantleRoutine()
    {
        if (_state == TraversalState.Mantle) yield break;
        _state = TraversalState.Mantle;
        _stateTime = 0f;
        SetAnimatorTraversalPose();
        SetFloatIfPresent("CrawlMotionSpeed", 1.15f);
        _capsule.enabled = false;

        Vector3 start = transform.position;
        Vector3 finish = _ledgePoint - _wallNormal * (_capsule.radius + 0.1f) + Vector3.up * 0.035f;
        Vector3 pullUp = new Vector3(start.x, finish.y + 0.045f, start.z);
        float elapsed = 0f;
        while (elapsed < ClimbDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / ClimbDuration));
            if (t < 0.58f)
                transform.position = Vector3.Lerp(start, pullUp, Mathf.SmoothStep(0f, 1f, t / 0.58f));
            else
                transform.position = Vector3.Lerp(pullUp, finish,
                    Mathf.SmoothStep(0f, 1f, (t - 0.58f) / 0.42f));
            yield return null;
        }
        transform.position = finish;
        _capsule.enabled = true;
        EndTraversal(Vector3.zero);
    }

    private void EndTraversal(Vector3 releaseVelocity)
    {
        StopAllCoroutines();
        if (!_capsule.enabled) _capsule.enabled = true;
        _state = TraversalState.None;
        _stateTime = 0f;
        _fatigue = 0f;
        _slipTimer = 0f;
        _lostWallContactTime = 0f;
        if (_grounding != null) _grounding.enabled = true;
        if (_motor != null) _motor.enabled = true;
        if (_visualRoot != null && _visualRotationCaptured)
        {
            _visualRoot.localRotation = _visualGroundRotation;
            _visualRoot.localPosition = _visualGroundPosition;
        }
        if (_input != null)
        {
            _input.jump = false;
            if (releaseVelocity.sqrMagnitude > 0.01f)
                _input.move = new Vector2(0f, -0.2f);
        }
        SetAnimatorTraversalPose();
    }

    public void CancelTraversal()
    {
        // Public fail-safe used by Escape and the on-screen GIVE UP tab.
        StopAllCoroutines();
        if (_capsule != null && !_capsule.enabled) _capsule.enabled = true;
        _state = TraversalState.None;
        _stateTime = 0f;
        _fatigue = 0f;
        _slipTimer = 0f;
        _lostWallContactTime = 0f;
        if (_visualRoot != null && _visualRotationCaptured)
        {
            _visualRoot.localRotation = _visualGroundRotation;
            _visualRoot.localPosition = _visualGroundPosition;
        }
        if (_grounding != null) _grounding.enabled = true;
        if (_motor != null) _motor.enabled = true;
        if (_input != null)
        {
            _input.jump = false;
            _input.move = Vector2.zero;
        }
        SetAnimatorTraversalPose();
    }

    private void OnDisable()
    {
        if (_visualRoot != null && _visualRotationCaptured)
        {
            _visualRoot.localRotation = _visualGroundRotation;
            _visualRoot.localPosition = _visualGroundPosition;
        }
    }

    private void SetAnimatorTraversalPose()
    {
        if (_animator == null) return;
        SetFloatIfPresent("Speed", 0f);
        SetFloatIfPresent("MotionSpeed", _state == TraversalState.None ? 1f : 0f);
        SetBoolIfPresent("Grounded", true);
        SetBoolIfPresent("Jump", false);
        SetBoolIfPresent("FreeFall", false);
        // Never reuse the ground-crawl clip on a wall. Hands and feet are posed
        // procedurally below while the upright humanoid base remains stable.
        SetBoolIfPresent("Crawling", false);
        SetBoolIfPresent("Mantling", _state == TraversalState.Mantle);
        SetFloatIfPresent("CrawlMotionSpeed", _state == TraversalState.Hang ? 0.18f :
            (_state == TraversalState.Attach ? 0.28f : 0.85f));
    }

    private void ApplyOptionalWallIK(int layerIndex)
    {
        if (_animator == null || !_animator.isHuman) return;

        bool wallState = _state == TraversalState.Attach || _state == TraversalState.Wall ||
                         _state == TraversalState.Hang;
        float targetWeight = wallState ? 1f : 0f;
        _ikWeight = Mathf.MoveTowards(_ikWeight, targetWeight, 7.5f * Time.deltaTime);
        if (_ikWeight <= 0.001f)
        {
            ClearWallIK();
            return;
        }

        Vector2 input = ReadMove();
        float activity = Mathf.Clamp01(input.magnitude * 1.4f);
        float scale = Mathf.Max(0.22f, _capsule.height / 1.8f);
        Vector3 right = Vector3.Cross(_wallNormal, Vector3.up).normalized;
        Vector3 wallPoint = transform.position - _wallNormal * RootWallDistance;
        float phaseA = Mathf.Sin(_climbPhase);
        float phaseB = Mathf.Sin(_climbPhase + Mathf.PI);
        float verticalActivity = Mathf.Abs(input.y);
        float lateralActivity = Mathf.Abs(input.x);
        float verticalA = phaseA * 0.15f * scale * verticalActivity;
        float verticalB = phaseB * 0.15f * scale * verticalActivity;
        float sidewaysA = phaseA * 0.16f * scale * lateralActivity;
        float sidewaysB = phaseB * 0.16f * scale * lateralActivity;

        if (_state == TraversalState.Attach)
        {
            // Both hands reach first; feet follow as the grab settles.
            float grab = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_stateTime / 0.24f));
            verticalA += 0.12f * scale * grab;
            verticalB += 0.12f * scale * grab;
        }

        Vector3[] nominal =
        {
            wallPoint + Vector3.up * (1.38f * scale + verticalA) +
                right * (-0.27f * scale + sidewaysA),
            wallPoint + Vector3.up * (1.38f * scale + verticalB) +
                right * (0.27f * scale + sidewaysB),
            wallPoint + Vector3.up * (0.43f * scale + verticalB) +
                right * (-0.19f * scale + sidewaysB),
            wallPoint + Vector3.up * (0.43f * scale + verticalA) +
                right * (0.19f * scale + sidewaysA)
        };

        if (_state == TraversalState.Hang)
        {
            nominal[0] = _ledgePoint + _wallNormal * 0.012f - right * (0.25f * scale);
            nominal[1] = _ledgePoint + _wallNormal * 0.012f + right * (0.25f * scale);
            nominal[2] = wallPoint + Vector3.up * (0.34f * scale) - right * (0.18f * scale);
            nominal[3] = wallPoint + Vector3.up * (0.34f * scale) + right * (0.18f * scale);
        }

        for (int i = 0; i < 4; i++)
        {
            if (!TryFindLimbContact(nominal[i], i % 2 == 0 ? -1f : 1f,
                    out Vector3 contact, out Vector3 normal))
            {
                _ikValid[i] = false;
                continue;
            }
            float smoothing = 1f - Mathf.Exp(-18f * Time.deltaTime);
            _ikPositions[i] = _ikValid[i]
                ? Vector3.Lerp(_ikPositions[i], contact, smoothing) : contact;
            Quaternion rotation = Quaternion.LookRotation(-normal, Vector3.up);
            _ikRotations[i] = _ikValid[i]
                ? Quaternion.Slerp(_ikRotations[i], rotation, smoothing) : rotation;
            _ikValid[i] = true;
        }

        ApplyWallIK(AvatarIKGoal.LeftHand, 0, 1f, 0.42f);
        ApplyWallIK(AvatarIKGoal.RightHand, 1, 1f, 0.42f);
        ApplyWallIK(AvatarIKGoal.LeftFoot, 2, 0.92f, 0.24f);
        ApplyWallIK(AvatarIKGoal.RightFoot, 3, 0.92f, 0.24f);

        // Hints keep elbows and knees bending away from the body. This is the
        // important safeguard against the folded/twisted procedural poses from
        // the earlier direct-bone version.
        Vector3 outward = _wallNormal * (0.14f * scale);
        Vector3 hips = transform.position + Vector3.up * (0.82f * scale);
        SetHint(AvatarIKHint.LeftElbow, hips + Vector3.up * (0.36f * scale) -
            right * (0.42f * scale) + outward, 0.62f);
        SetHint(AvatarIKHint.RightElbow, hips + Vector3.up * (0.36f * scale) +
            right * (0.42f * scale) + outward, 0.62f);
        SetHint(AvatarIKHint.LeftKnee, hips - Vector3.up * (0.35f * scale) -
            right * (0.27f * scale) + outward, 0.72f);
        SetHint(AvatarIKHint.RightKnee, hips - Vector3.up * (0.35f * scale) +
            right * (0.27f * scale) + outward, 0.72f);

        Vector3 lookPoint = wallPoint + Vector3.up * (1.52f * scale) +
                            right * (input.x * 0.18f * scale);
        _animator.SetLookAtWeight(_ikWeight * 0.62f, 0.28f, 0.72f, 0.15f, 0.65f);
        _animator.SetLookAtPosition(lookPoint);
    }

    private bool TryFindLimbContact(Vector3 nominal, float side,
        out Vector3 contact, out Vector3 contactNormal)
    {
        contact = default;
        contactNormal = _wallNormal;
        float bestScore = float.MaxValue;
        Vector3 towardWall = -_wallNormal;
        float[] turns = { 0f, 28f * side, 62f * side, 92f * side, -24f * side };
        for (int d = 0; d < turns.Length; d++)
        {
            Vector3 direction = Quaternion.AngleAxis(turns[d], Vector3.up) * towardWall;
            Vector3 origin = nominal - direction * (RootWallDistance + 0.30f);
            RaycastHit[] hits = Physics.SphereCastAll(origin, 0.028f, direction,
                RootWallDistance + 0.72f, ClimbableLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (IsSelf(hit.transform) || Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up)) > 0.48f)
                    continue;
                Vector3 candidate = hit.point + hit.normal * 0.012f;
                float score = (candidate - nominal).sqrMagnitude + Mathf.Abs(turns[d]) * 0.00035f;
                if (score >= bestScore) continue;
                bestScore = score;
                contact = candidate;
                contactNormal = hit.normal.normalized;
            }
        }
        return bestScore < float.MaxValue;
    }

    private void ApplyWallIK(AvatarIKGoal goal, int index, float positionWeight, float rotationWeight)
    {
        float validWeight = _ikValid[index] ? _ikWeight : 0f;
        _animator.SetIKPositionWeight(goal, validWeight * positionWeight);
        _animator.SetIKRotationWeight(goal, validWeight * rotationWeight);
        if (!_ikValid[index]) return;
        _animator.SetIKPosition(goal, _ikPositions[index]);
        _animator.SetIKRotation(goal, _ikRotations[index]);
    }

    private void SetHint(AvatarIKHint hint, Vector3 position, float weight)
    {
        _animator.SetIKHintPositionWeight(hint, _ikWeight * weight);
        _animator.SetIKHintPosition(hint, position);
    }

    private void ClearWallIK()
    {
        _animator.SetLookAtWeight(0f);
        _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
        _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
        _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
        _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
        _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
        _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 0f);
        _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 0f);
        _animator.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0f);
        _animator.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0f);
        _animator.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, 0f);
        _animator.SetIKHintPositionWeight(AvatarIKHint.RightKnee, 0f);
        for (int i = 0; i < _ikValid.Length; i++) _ikValid[i] = false;
    }

    private bool IsSelf(Transform candidate)
    {
        return candidate == null || candidate == transform || candidate.IsChildOf(transform);
    }

    private Vector2 ReadMove() => _input != null ? _input.move : Vector2.zero;

    private bool ReadJumpHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) return Keyboard.current.spaceKey.isPressed;
#else
        if (Input.GetKey(KeyCode.Space)) return true;
#endif
        return _input != null && _input.jump;
    }

    private bool DropPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.C);
#endif
    }

    private bool JumpAwayPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed &&
               Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private void SetFloatIfPresent(string parameter, float value)
    {
        for (int i = 0; i < _animator.parameterCount; i++)
            if (_animator.parameters[i].name == parameter &&
                _animator.parameters[i].type == AnimatorControllerParameterType.Float)
                _animator.SetFloat(parameter, value);
    }

    private void SetBoolIfPresent(string parameter, bool value)
    {
        for (int i = 0; i < _animator.parameterCount; i++)
            if (_animator.parameters[i].name == parameter &&
                _animator.parameters[i].type == AnimatorControllerParameterType.Bool)
                _animator.SetBool(parameter, value);
    }

    private static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    private void OnGUI()
    {
        if (_state == TraversalState.None) return;
        string text = _state == TraversalState.Mantle ? "MANTLING" :
            _state == TraversalState.Attach ? "GRABBING WALL..." :
            _state == TraversalState.Hang ? "HANG — Space/W mantle • C drop" :
            (_slipTimer > 0f ? "SLIPPED — HOLD ON..." :
                "CLIMB — W/S vertical • A/D around corners • Shift+Space jump away");
        GUI.Box(new Rect(Screen.width * 0.5f - 250f, 62f, 500f, 30f), text);
    }
}
