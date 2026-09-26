using StarterAssets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Player camera for the Little: collision-aware third person, first person,
/// and an explicitly toggled developer free camera.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public sealed class LittleAdaptiveCamera : MonoBehaviour
{
    public enum CameraMode { ThirdPerson, FirstPerson, Developer }

    public Transform Player;
    public Transform Pivot;
    public float DefaultDistance = 1.75f;
    public float MinimumDistance = 0.55f;
    public float MaximumDistance = 3.4f;
    public float ShoulderOffset = 0.16f;
    public float VerticalOffset = 0.07f;
    public float CollisionRadius = 0.1f;
    public float FollowSmoothTime = 0.055f;
    public float DeveloperSpeed = 4f;
    [Header("One-hand camera assistance")]
    public bool AutoFollowPlayer = true;
    public float AutoFollowSpeed = 3.8f;
    public float KeyboardOrbitSpeed = 85f;
    public float MouseLookSensitivity = 0.11f;
    public float FirstPersonYawLimit = 88f;
    public float FirstPersonFov = 62f;
    public float MinimumFov = 42f;
    public float MaximumFov = 78f;
    public CameraMode Mode = CameraMode.ThirdPerson;

    private Camera _camera;
    private ThirdPersonController _motor;
    private StarterAssetsInputs _inputs;
    private float _wantedDistance;
    private Vector3 _followVelocity;
    private float _developerYaw;
    private float _developerPitch;
    private bool _motorWasEnabled;
    private float _wantedFov;
    private bool _guiDeveloperToggle;
    private float _lastModeToggleTime = -1f;
    private float _turnDistanceFactor = 1f;
    private Transform _head;
    private float _thirdPersonYaw;
    private float _thirdPersonYawVelocity;
    private float _firstPersonPitch;
    private float _firstPersonYawOffset;
    private Vector2 _lastStrongMoveInput;

    public string ModeLabel => Mode == CameraMode.ThirdPerson ? "THIRD PERSON" :
        (Mode == CameraMode.FirstPerson ? "FIRST PERSON" : "DEVELOPER CAMERA");

    public void Configure(Transform player, Transform pivot)
    {
        Player = player;
        Pivot = pivot;
        ResolveReferences();
    }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _wantedDistance = DefaultDistance;
        _wantedFov = FirstPersonFov;
    }

    private void Start()
    {
        ResolveReferences();
        DisableCinemachineControl();
        if (Player != null)
        {
            _thirdPersonYaw = Player.eulerAngles.y;
        }
        if (Pivot != null) SnapToThirdPerson();
    }

    private void ResolveReferences()
    {
        if (Player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) Player = found.transform;
        }
        if (Player == null) return;
        _motor = Player.GetComponent<ThirdPersonController>();
        _inputs = Player.GetComponent<StarterAssetsInputs>();
        Animator animator = Player.GetComponent<Animator>();
        if (animator != null && animator.isHuman)
            _head = animator.GetBoneTransform(HumanBodyBones.Head);
        if (Pivot == null && _motor != null && _motor.CinemachineCameraTarget != null)
            Pivot = _motor.CinemachineCameraTarget.transform;
    }

    private void DisableCinemachineControl()
    {
        foreach (Behaviour component in GetComponents<Behaviour>())
        {
            if (component == this) continue;
            if (component.GetType().Name.Contains("CinemachineBrain")) component.enabled = false;
        }
        GameObject virtualCamera = GameObject.Find("PlayerFollowCamera");
        if (virtualCamera != null) virtualCamera.SetActive(false);
    }

    private void Update()
    {
        if (Player == null || Pivot == null) ResolveReferences();
        HandleModeInput();
        HandleZoom();
        if (Mode == CameraMode.Developer) UpdateDeveloperCamera();
    }

    private void LateUpdate()
    {
        if (Pivot == null || Mode == CameraMode.Developer) return;
        if (Mode == CameraMode.FirstPerson)
        {
            HandleFirstPersonFollow();
            UpdateFirstPerson();
            return;
        }
        HandleThirdPersonFollow();
        UpdateThirdPerson();
    }

    private void HandleModeInput()
    {
        bool perspectivePressed = false;
        bool developerPressed = false;
        bool recoverPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            perspectivePressed = Keyboard.current.vKey.wasPressedThisFrame;
            developerPressed = Keyboard.current.f3Key.wasPressedThisFrame ||
                               Keyboard.current.f5Key.wasPressedThisFrame;
            recoverPressed = Keyboard.current.escapeKey.wasPressedThisFrame;
        }
#else
        perspectivePressed = Input.GetKeyDown(KeyCode.V);
        developerPressed = Input.GetKeyDown(KeyCode.F3) || Input.GetKeyDown(KeyCode.F5);
        recoverPressed = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (recoverPressed)
        {
            RecoverPlayerControl();
            return;
        }
        developerPressed |= _guiDeveloperToggle;
        _guiDeveloperToggle = false;
        if (developerPressed && Time.unscaledTime - _lastModeToggleTime > 0.2f)
        {
            _lastModeToggleTime = Time.unscaledTime;
            if (Mode == CameraMode.Developer) SetMode(CameraMode.ThirdPerson);
            else SetMode(CameraMode.Developer);
        }
        else if (perspectivePressed && Mode != CameraMode.Developer)
        {
            SetMode(Mode == CameraMode.FirstPerson ? CameraMode.ThirdPerson : CameraMode.FirstPerson);
        }
    }

    private void SetMode(CameraMode mode)
    {
        if (Mode == CameraMode.Developer && mode != CameraMode.Developer && _motor != null)
            _motor.enabled = _motorWasEnabled;

        Mode = mode;
        _followVelocity = Vector3.zero;
        if (_camera != null) _camera.nearClipPlane = mode == CameraMode.FirstPerson ? 0.025f : 0.08f;

        if (mode == CameraMode.Developer)
        {
            LittleLedgeClimber climber = Player != null
                ? Player.GetComponent<LittleLedgeClimber>() : null;
            if (climber != null) climber.CancelTraversal();
            Vector3 angles = transform.eulerAngles;
            _developerYaw = angles.y;
            _developerPitch = NormalizeAngle(angles.x);
            if (_motor != null)
            {
                // Developer mode must never remember the motor as disabled just
                // because it was entered during climbing.
                _motorWasEnabled = true;
                _motor.enabled = false;
            }
            if (_inputs != null) _inputs.move = Vector2.zero;
        }
        else if (mode == CameraMode.ThirdPerson)
        {
            _thirdPersonYaw = Player != null ? Player.eulerAngles.y : Pivot.eulerAngles.y;
            _thirdPersonYawVelocity = 0f;
            _turnDistanceFactor = 1f;
            SnapToThirdPerson();
        }
        else if (mode == CameraMode.FirstPerson)
        {
            _turnDistanceFactor = 1f;
            _firstPersonPitch = 0f;
            _firstPersonYawOffset = 0f;
        }
    }

    public void RecoverPlayerControl()
    {
        LittleLedgeClimber climber = Player != null
            ? Player.GetComponent<LittleLedgeClimber>() : null;
        if (climber != null) climber.CancelTraversal();
        if (_motor != null) _motor.enabled = true;
        _motorWasEnabled = true;
        SetMode(CameraMode.ThirdPerson);
    }

    private void HandleThirdPersonFollow()
    {
        if (Pivot == null || Player == null) return;
        // Starter Assets also writes camera look. Clear it while auto-follow is
        // active so the two systems do not fight and produce visible wobble.
        if (AutoFollowPlayer && _inputs != null) _inputs.look = Vector2.zero;
        float horizontal = 0f;
        float vertical = 0f;
        bool recenter = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
            if (Keyboard.current.upArrowKey.isPressed) vertical -= 1f;
            if (Keyboard.current.downArrowKey.isPressed) vertical += 1f;
            recenter = Keyboard.current.rKey.wasPressedThisFrame;
        }
#else
        horizontal = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                     (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
        vertical = (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f) -
                   (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f);
        recenter = Input.GetKeyDown(KeyCode.R);
#endif
        Vector3 angles = Pivot.eulerAngles;
        float pitch = NormalizeAngle(angles.x);
        float yaw = angles.y;
        if (Mathf.Abs(horizontal) > 0.01f || Mathf.Abs(vertical) > 0.01f)
        {
            yaw += horizontal * KeyboardOrbitSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch + vertical * KeyboardOrbitSpeed * 0.65f * Time.deltaTime,
                -25f, 62f);
            Pivot.rotation = Quaternion.Euler(pitch, yaw, 0f);
            return;
        }

        if (!AutoFollowPlayer && !recenter) return;
        // Follow the motor-owned character root. The input helper freezes the
        // requested world heading during the turn, so this camera can rotate
        // behind the player without changing where the player is trying to go.
        float desiredYaw = Player.eulerAngles.y;
        _turnDistanceFactor = 1f;
        _thirdPersonYaw = Mathf.SmoothDampAngle(_thirdPersonYaw, desiredYaw,
            ref _thirdPersonYawVelocity, 0.16f, 540f, Time.deltaTime);
        if (Mathf.Abs(Mathf.DeltaAngle(_thirdPersonYaw, desiredYaw)) < 0.35f)
        {
            _thirdPersonYaw = desiredYaw;
            _thirdPersonYawVelocity = 0f;
        }
        Pivot.rotation = Quaternion.Euler(12f, _thirdPersonYaw, 0f);
    }

    private void HandleFirstPersonFollow()
    {
        if (Player == null || Pivot == null) return;
        if (_inputs != null) _inputs.look = Vector2.zero;
        Vector2 mouseLook = Vector2.zero;
        float vertical = 0f;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) mouseLook = Mouse.current.delta.ReadValue();
        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.isPressed) vertical += 1f;
            if (Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
        }
#else
        mouseLook = new Vector2(Input.GetAxis("Mouse X") * 10f,
            Input.GetAxis("Mouse Y") * 10f);
        vertical = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) -
                   (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
#endif
        _firstPersonYawOffset = Mathf.Clamp(_firstPersonYawOffset +
            mouseLook.x * MouseLookSensitivity, -FirstPersonYawLimit, FirstPersonYawLimit);
        _firstPersonPitch = Mathf.Clamp(_firstPersonPitch -
            mouseLook.y * MouseLookSensitivity -
            vertical * KeyboardOrbitSpeed * 0.55f * Time.deltaTime, -55f, 70f);
        // The camera rides on the animated head. Character yaw supplies the
        // base direction; the mouse supplies a reversible head-look offset.
        Pivot.rotation = Quaternion.Euler(_firstPersonPitch,
            Player.eulerAngles.y + _firstPersonYawOffset, 0f);
    }

    private void HandleZoom()
    {
        float scroll = 0f;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) scroll = Mouse.current.scroll.ReadValue().y;
#else
        scroll = Input.mouseScrollDelta.y * 120f;
#endif
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // Linux mouse drivers report either +/-1 or +/-120 per wheel notch.
            // Normalize both forms so zoom is visible and consistent.
            float notches = Mathf.Abs(scroll) > 10f ? scroll / 120f : scroll;
            if (Mode == CameraMode.ThirdPerson)
                _wantedDistance = Mathf.Clamp(_wantedDistance - notches * 0.24f,
                    MinimumDistance, MaximumDistance);
            else
                _wantedFov = Mathf.Clamp(_wantedFov - notches * 3f, MinimumFov, MaximumFov);
        }
        if (_camera != null && Mode != CameraMode.ThirdPerson)
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _wantedFov,
                1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        else if (_camera != null)
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, FirstPersonFov,
                1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
    }

    private void UpdateThirdPerson()
    {
        Vector3 focus = _head != null ? _head.position + Player.up * 0.018f : Pivot.position;
        Vector3 localOffset = new Vector3(ShoulderOffset, VerticalOffset,
            -_wantedDistance * _turnDistanceFactor);
        Vector3 wanted = focus + Pivot.rotation * localOffset;
        Vector3 ray = wanted - focus;
        float allowedDistance = ray.magnitude;
        int mask = Player != null ? ~(1 << Player.gameObject.layer) : ~0;
        if (FindCameraObstruction(focus, ray.normalized, ray.magnitude, mask,
                out float obstructionDistance))
            allowedDistance = Mathf.Max(0.12f, obstructionDistance - 0.07f);

        Vector3 safePosition = focus + ray.normalized * allowedDistance;
        if (Physics.CheckSphere(safePosition, CollisionRadius * 0.82f, mask,
                QueryTriggerInteraction.Ignore))
        {
            // Search only along the verified player-to-camera segment. This can
            // tuck close like first person, but can never cross to the far side
            // of a wall during a rapid 180-degree camera whip.
            bool foundPocket = false;
            for (float distance = 0.12f; distance <= allowedDistance; distance += 0.06f)
            {
                Vector3 candidate = focus + ray.normalized * distance;
                if (Physics.CheckSphere(candidate, CollisionRadius * 0.82f, mask,
                        QueryTriggerInteraction.Ignore)) continue;
                safePosition = candidate;
                foundPocket = true;
                break;
            }
            if (!foundPocket) safePosition = focus;
            transform.position = Vector3.SmoothDamp(transform.position, safePosition,
                ref _followVelocity, FollowSmoothTime * 0.65f);
            transform.rotation = Pivot.rotation;
            return;
        }
        transform.position = Vector3.SmoothDamp(transform.position, safePosition,
            ref _followVelocity, FollowSmoothTime);
        Vector3 look = focus - transform.position;
        if (look.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
    }

    private bool FindCameraObstruction(Vector3 origin, Vector3 direction, float distance,
        int mask, out float nearestDistance)
    {
        nearestDistance = float.MaxValue;
        RaycastHit[] hits = Physics.SphereCastAll(origin, CollisionRadius, direction, distance,
            mask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            Transform candidate = hits[i].transform;
            if (candidate == null) continue;
            if (Player != null && (candidate == Player || candidate.IsChildOf(Player))) continue;
            if (hits[i].distance < nearestDistance) nearestDistance = hits[i].distance;
        }
        return nearestDistance < float.MaxValue;
    }

    private void UpdateFirstPerson()
    {
        // Attach to the animated head position but use the stable character yaw;
        // this gives natural head height without inheriting unusual UMA bone axes.
        Vector3 headPosition = _head != null ? _head.position : Pivot.position;
        transform.position = headPosition + Player.up * 0.012f + Pivot.forward * 0.055f;
        transform.rotation = Pivot.rotation;
    }

    private void UpdateDeveloperCamera()
    {
        float lookX = 0f;
        float lookY = 0f;
        Vector3 move = Vector3.zero;
        bool fast = false;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            lookX = delta.x;
            lookY = delta.y;
        }
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed) move.z += 1f;
            if (Keyboard.current.sKey.isPressed) move.z -= 1f;
            if (Keyboard.current.dKey.isPressed) move.x += 1f;
            if (Keyboard.current.aKey.isPressed) move.x -= 1f;
            if (Keyboard.current.spaceKey.isPressed) move.y += 1f;
            if (Keyboard.current.leftCtrlKey.isPressed) move.y -= 1f;
            fast = Keyboard.current.leftShiftKey.isPressed;
        }
#else
        lookX = Input.GetAxis("Mouse X") * 10f;
        lookY = Input.GetAxis("Mouse Y") * 10f;
        move = new Vector3(Input.GetAxisRaw("Horizontal"),
            (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f),
            Input.GetAxisRaw("Vertical"));
        fast = Input.GetKey(KeyCode.LeftShift);
#endif
        _developerYaw += lookX * 0.11f;
        _developerPitch = Mathf.Clamp(_developerPitch - lookY * 0.11f, -89f, 89f);
        transform.rotation = Quaternion.Euler(_developerPitch, _developerYaw, 0f);
        float speed = DeveloperSpeed * (fast ? 3f : 1f);
        transform.position += transform.TransformDirection(move.normalized) * speed * Time.unscaledDeltaTime;
    }

    private void SnapToThirdPerson()
    {
        if (Pivot == null) return;
        transform.position = Pivot.position + Pivot.rotation *
            new Vector3(ShoulderOffset, VerticalOffset, -_wantedDistance);
        transform.LookAt(Pivot.position);
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(Screen.width - 325f, 18f, 305f, 32f),
            ModeLabel + "   Auto-follow • Arrows look • R centre");
        if (GUI.Button(new Rect(Screen.width - 325f, 54f, 148f, 30f), "DEV / PLAY (F5)"))
            _guiDeveloperToggle = true;
        if (GUI.Button(new Rect(Screen.width - 173f, 54f, 153f, 30f), "GIVE UP / RESET (Esc)"))
            RecoverPlayerControl();
    }
}
