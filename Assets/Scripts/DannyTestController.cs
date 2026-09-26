using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class DannyTestController : MonoBehaviour
{
    private const int GroundHitCapacity = 16;
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.0f;
    [SerializeField] private float runSpeed = 2.7f;
    [SerializeField] private float rotationSpeed = 6.5f;
    [SerializeField] private float acceleration = 4.5f;
    [SerializeField] private float deceleration = 6.0f;
    [SerializeField] private float jumpHeight = 0.26f;
    [SerializeField] private float gravity = -32f;
    [SerializeField] private float coyoteTime = 0.14f;

    private CharacterController characterController;
    private Animator animator;
    private Transform cameraTransform;
    private float verticalSpeed;
    private float lastGroundedTime = -10f;
    private float currentPlanarSpeed;
    private Vector3 currentMoveDirection = Vector3.forward;
    private float movementScale = 1f;
    private float movementScaleUntil;
    private bool isGrounded;
    private float airborneSince = -10f;
    private readonly RaycastHit[] groundHits = new RaycastHit[GroundHitCapacity];
    private bool wasGrounded;
    private Vector2 automatedInput;
    private bool automatedRun;
    private bool automatedJumpQueued;
    private float automatedInputUntil;
    private Vector3 automatedWorldDirection;
    private float jumpStartHeight;
    private float trickSpinRemaining;
    private float jumpAnimationUntil = -1f;
    private static int lowBranchWipeouts;
    private Collider levelTwoReturnRamp;

    public float CurrentPlanarSpeed => currentPlanarSpeed;
    public bool IsGrounded => isGrounded;
    public static int LowBranchWipeouts => lowBranchWipeouts;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int FreeFallHash = Animator.StringToHash("FreeFall");
    private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");
    private static readonly int StumbleHash = Animator.StringToHash("Stumble");
    private static readonly int EmbarrassedHash = Animator.StringToHash("Embarrassed");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        cameraTransform = Camera.main != null ? Camera.main.transform : null;
    }

    private void OnEnable()
    {
        // Scripted sequences disable this motor. Never resume one with the
        // upward velocity from an interrupted jump, which previously let
        // Danny drift above the river-valley stairs.
        verticalSpeed = -1.2f;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        Vector2 input = Vector2.zero;
        bool running = false;
        bool jumpPressed = false;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;
            running = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            jumpPressed = keyboard.spaceKey.wasPressedThisFrame;
            if (keyboard.tKey.wasPressedThisFrame) animator?.SetTrigger(StumbleHash);
            // E belongs exclusively to world interactions. Sharing it with
            // this animation made Danny flinch whenever he helped someone.
            if (keyboard.bKey.wasPressedThisFrame) animator?.SetTrigger(EmbarrassedHash);
        }
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            if (stick.sqrMagnitude > input.sqrMagnitude) input = stick;
            running |= gamepad.leftStickButton.isPressed || gamepad.rightShoulder.isPressed;
            jumpPressed |= gamepad.buttonSouth.wasPressedThisFrame;
            if (gamepad.buttonWest.wasPressedThisFrame) animator?.SetTrigger(StumbleHash);
            if (gamepad.buttonNorth.wasPressedThisFrame) animator?.SetTrigger(EmbarrassedHash);
        }
        if (RiverValleyMobileControls.IsAvailable)
        {
            Vector2 touchMove = RiverValleyMobileControls.Move;
            if (touchMove.sqrMagnitude > input.sqrMagnitude) input = touchMove;
            running |= touchMove.magnitude > 0.82f;
            jumpPressed |= RiverValleyMobileControls.JumpPressed;
        }
        if (Time.unscaledTime < automatedInputUntil)
        {
            input = automatedInput;
            running = automatedRun;
            jumpPressed |= automatedJumpQueued;
            automatedJumpQueued = false;
        }

        input = Vector2.ClampMagnitude(input, 1f);
        Vector3 forward = cameraTransform != null ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 right = cameraTransform != null ? Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized : Vector3.right;
        bool useAutomatedWorldDirection=Time.unscaledTime<automatedInputUntil&&
            automatedWorldDirection.sqrMagnitude>0.001f;
        Vector3 move = useAutomatedWorldDirection
            ? automatedWorldDirection*input.magnitude
            : forward * input.y + right * input.x;
        if (move.sqrMagnitude > 0.001f)
        {
            Vector3 desiredDirection = move.normalized;
            currentMoveDirection = Vector3.Slerp(currentMoveDirection, desiredDirection,
                1f - Mathf.Exp(-8f * Time.deltaTime)).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(currentMoveDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                1f - Mathf.Exp(-rotationSpeed * Time.deltaTime));
        }

        // Only use the small backup ray while falling. The previous long ray
        // reached the floor during takeoff and announced a false landing,
        // which caused the visible jump shake and occasional foot sinking.
        // CharacterController can retain its previous grounded flag for one
        // takeoff frame. Never let that stale contact refresh coyote time or
        // repeated Space presses can stack upward speed into a "flight".
        bool grounded = verticalSpeed <= 0.05f && characterController.isGrounded;
        if (!grounded && verticalSpeed <= 0.1f)
        {
            grounded = TryFindGround(0.20f, out _);
        }
        if (grounded) lastGroundedTime = Time.time;
        if (grounded && verticalSpeed < 0f) verticalSpeed = -1.2f;
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        if (canJump && jumpPressed)
        {
            verticalSpeed = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastGroundedTime = -10f;
            grounded = false;
            airborneSince = Time.time;
            jumpStartHeight = transform.position.y;
            trickSpinRemaining = currentPlanarSpeed > 0.35f ? 360f : 150f;
            jumpAnimationUntil = Time.time + 0.55f;
            animator?.SetTrigger(JumpHash);
        }
        if (!grounded) verticalSpeed += gravity * Time.deltaTime;
        // Danny gets a quick, clumsy hop—not a superhero glide. This also
        // protects against an animation/state edge case holding him aloft.
        if (!grounded && Time.time - airborneSince > 0.34f)
            verticalSpeed = Mathf.Min(verticalSpeed, -8.5f);
        if (!grounded && verticalSpeed > 0f && transform.position.y > jumpStartHeight + jumpHeight + 0.10f)
            verticalSpeed = -8.5f;

        if (Time.time >= movementScaleUntil) movementScale = 1f;
        float targetSpeed = input.magnitude * (running ? runSpeed : walkSpeed) * movementScale;
        float rate = targetSpeed > currentPlanarSpeed ? acceleration : deceleration;
        currentPlanarSpeed = Mathf.MoveTowards(currentPlanarSpeed, targetSpeed, rate * Time.deltaTime);
        Vector3 planarVelocity=currentMoveDirection*currentPlanarSpeed;
        float appliedVerticalSpeed=verticalSpeed;
        if(grounded&&currentPlanarSpeed>0.01f&&TryFindGround(0.28f,out RaycastHit slopeGround)&&
            slopeGround.normal.y>0.66f&&slopeGround.normal.y<0.995f)
        {
            // Follow gentle packed-snow slopes instead of repeatedly driving
            // the capsule into their face. This prevents the top-stair
            // "trip twitch" while retaining normal gravity on flat ground.
            Vector3 surfaceDirection=Vector3.ProjectOnPlane(currentMoveDirection,slopeGround.normal);
            if(surfaceDirection.sqrMagnitude>0.01f&&Vector3.Dot(surfaceDirection,currentMoveDirection)>0f)
            {
                planarVelocity=surfaceDirection.normalized*currentPlanarSpeed;
                appliedVerticalSpeed=Mathf.Max(appliedVerticalSpeed,-0.20f);
            }
        }
        CollisionFlags movement;
        if(!TryGuidedLevelTwoRampMove(planarVelocity,out movement))
            movement = characterController.Move(
                (planarVelocity + Vector3.up * appliedVerticalSpeed) * Time.deltaTime);
        if (!grounded && trickSpinRemaining > 0f)
        {
            float spin = Mathf.Min(trickSpinRemaining, 760f * Time.deltaTime);
            transform.Rotate(Vector3.up,spin,Space.World);
            trickSpinRemaining -= spin;
        }
        if ((movement & CollisionFlags.Below) != 0 && verticalSpeed <= 0f)
        {
            grounded = true;
            if (verticalSpeed < 0f) verticalSpeed = -1.2f;
            airborneSince = -10f;
            trickSpinRemaining = 0f;
        }
        // Keep a descending hop attached to stairs and uneven snow.  The
        // controller's skin can otherwise leave a visible gap for a frame or
        // two, which reads as hovering in the follow camera.
        if (!grounded && verticalSpeed < 0f && TryFindGround(0.24f, out RaycastHit landing))
        {
            float snap = Mathf.Max(0f, landing.distance - 0.075f);
            if (snap > 0.001f) characterController.Move(Vector3.down * snap);
            grounded = true;
            verticalSpeed = -1.2f;
            airborneSince = -10f;
        }
        isGrounded = grounded;

        if (grounded && !wasGrounded)
        {
            // A missed Animator transition used to leave the full arms-up
            // jump pose playing after Danny had already landed. Explicitly
            // clear the airborne state and return to locomotion immediately.
            ClearJumpPose(0.07f);
        }
        // Animator transitions can miss the single landing frame when a
        // sequence teleports or briefly disables the motor. This watchdog
        // guarantees the visual pose cannot keep flying after the controller
        // is already grounded.
        if (grounded && jumpAnimationUntil > 0f && Time.time >= jumpAnimationUntil)
            ClearJumpPose(0.07f);
        wasGrounded = grounded;

        float animationSpeed = currentPlanarSpeed < 0.04f ? 0f : running ? 1f : 0.45f;
        animator?.SetFloat(SpeedHash, animationSpeed, 0.10f, Time.deltaTime);
        animator?.SetFloat(MotionSpeedHash, Mathf.Clamp01(currentPlanarSpeed / Mathf.Max(walkSpeed, 0.01f)));
        animator?.SetBool(GroundedHash, grounded);
        animator?.SetBool(FreeFallHash, !grounded && verticalSpeed < -0.1f);
    }

    /// <summary>Used only by the hands-off whole-level playtest.</summary>
    public void SetAutomatedInput(Vector2 move, bool run, bool jump)
    {
        automatedInput = Vector2.ClampMagnitude(move, 1f);
        automatedWorldDirection=Vector3.zero;
        automatedRun = run;
        automatedJumpQueued |= jump;
        // Keep hands-off audit input alive across very slow software-rendered
        // frames. Real keyboard, touch and gamepad input are unaffected.
        automatedInputUntil = Time.unscaledTime + 1.00f;
    }

    /// <summary>World-space companion used by focused geometry audits.</summary>
    public void SetAutomatedWorldInput(Vector3 direction,bool run)
    {
        Vector3 flat=Vector3.ProjectOnPlane(direction,Vector3.up);
        automatedWorldDirection=flat.sqrMagnitude>0.001f?flat.normalized:Vector3.zero;
        automatedInput=automatedWorldDirection.sqrMagnitude>0.001f?Vector2.up:Vector2.zero;
        automatedRun=run;
        automatedJumpQueued=false;
        automatedInputUntil=Time.unscaledTime+1.00f;
    }

    /// <summary>Runs one deterministic movement step for the visual geometry audit.</summary>
    public void MoveWorldForPlaytest(Vector3 direction,float speed)
    {
        Vector3 flat=Vector3.ProjectOnPlane(direction,Vector3.up);
        if(flat.sqrMagnitude<0.001f)return;
        currentMoveDirection=flat.normalized;
        currentPlanarSpeed=Mathf.Max(0f,speed);
        if(!TryGuidedLevelTwoRampMove(currentMoveDirection*currentPlanarSpeed,out _))
            characterController.Move((currentMoveDirection*currentPlanarSpeed+Vector3.down*0.20f)*Time.deltaTime);
        transform.rotation=Quaternion.LookRotation(currentMoveDirection,Vector3.up);
    }

    private bool TryGuidedLevelTwoRampMove(Vector3 planarVelocity,out CollisionFlags movement)
    {
        movement=CollisionFlags.None;
        if(currentPlanarSpeed<0.01f)return false;
        if(levelTwoReturnRamp==null)
        {
            GameObject ramp=GameObject.Find("LEVEL 2 — packed-snow return ramp at stair top");
            if(ramp!=null)levelTwoReturnRamp=ramp.GetComponent<Collider>();
        }
        if(levelTwoReturnRamp==null||!levelTwoReturnRamp.enabled)return false;

        Bounds bounds=levelTwoReturnRamp.bounds;
        Vector3 desired=transform.position+Vector3.ProjectOnPlane(planarVelocity,Vector3.up)*Time.deltaTime;
        if(desired.x<bounds.min.x-0.03f||desired.x>bounds.max.x+0.03f||
           desired.z<bounds.min.z-0.08f||desired.z>bounds.max.z+0.08f)return false;
        Ray surfaceRay=new(new Vector3(desired.x,bounds.max.y+2f,desired.z),Vector3.down);
        if(!levelTwoReturnRamp.Raycast(surfaceRay,out RaycastHit surface,5f))return false;
        if(Mathf.Abs(surface.point.y-transform.position.y)>0.48f)return false;

        // This is a short Level 2 accessibility surface, not a teleport: x/z
        // still advance at Danny's ordinary speed, while only the foot height
        // is conformed to the snow. The ramp's own narrow bounds are the safety
        // corridor; casting against the square stair noses underneath it made
        // them reappear as zero-distance walls and stopped Danny at Stair 007.
        desired.y=surface.point.y+0.04f;
        characterController.enabled=false;
        transform.position=desired;
        characterController.enabled=true;
        verticalSpeed=-0.20f;
        movement=CollisionFlags.Below;
        return true;
    }

    private bool TryFindGround(float distance, out RaycastHit bestHit)
    {
        bestHit = default;
        float bestDistance = float.PositiveInfinity;
        int count = Physics.RaycastNonAlloc(transform.position + Vector3.up * 0.08f, Vector3.down,
            groundHits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
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

    public void ApplySlow(float scale, float seconds)
    {
        movementScale = Mathf.Clamp(scale, 0.15f, 1f);
        movementScaleUntil = Mathf.Max(movementScaleUntil, Time.time + Mathf.Max(0f, seconds));
    }

    public void ResetVerticalMotion()
    {
        verticalSpeed = -1.2f;
        lastGroundedTime = Time.time;
        isGrounded = true;
        wasGrounded = true;
        airborneSince = -10f;
        trickSpinRemaining = 0f;
        ClearJumpPose(0.06f);
    }

    private void ClearJumpPose(float blendSeconds)
    {
        jumpAnimationUntil = -1f;
        animator?.ResetTrigger(JumpHash);
        animator?.SetBool(FreeFallHash, false);
        animator?.SetBool(GroundedHash, true);
        if (animator != null)
            animator.CrossFade(currentPlanarSpeed > 0.04f ? "Walk" : "Friendly Idle", blendSeconds);
    }

    public void HitLowBranch()
    {
        if (isGrounded) return;
        verticalSpeed = -11f;
        trickSpinRemaining = 0f;
        lastGroundedTime = -10f;
        lowBranchWipeouts++;
        ApplySlow(0.24f,1.7f);
        animator?.ResetTrigger(JumpHash);
        animator?.SetTrigger(StumbleHash);
        RiverValleyGameDirector director = FindFirstObjectByType<RiverValleyGameDirector>();
        director?.LowBranchWipeout();
    }
}
