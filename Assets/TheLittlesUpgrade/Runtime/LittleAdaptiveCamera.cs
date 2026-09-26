using UnityEngine;

namespace TheLittles.Upgrade
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class LittleAdaptiveCamera : MonoBehaviour
    {
        private enum ViewMode { ThirdPerson, FirstPerson, Developer }

        [SerializeField] private Transform target;
        [SerializeField] private LayerMask collisionLayers = ~0;
        [SerializeField] private float focusHeight = 1.32f;
        [SerializeField] private float distance = 4.7f;
        [SerializeField] private float minimumDistance = 0.42f;
        [SerializeField] private float maximumDistance = 8.5f;
        [SerializeField] private float shoulderOffset = 0.42f;
        [SerializeField] private float yaw;
        [SerializeField] private float pitch = 16f;
        [SerializeField] private float positionSharpness = 16f;
        [SerializeField] private float rotationSharpness = 20f;
        [SerializeField] private float collisionRadius = 0.18f;
        [SerializeField] private float firstPersonForwardOffset = 0.16f;
        [SerializeField] private float developerSpeed = 6f;

        private ViewMode mode;
        private Camera controlledCamera;
        private float shoulderSign = 1f;
        private Vector3 smoothFocus;
        private Vector3 savedPosition;
        private Quaternion savedRotation;

        public void Configure(Transform followTarget, CharacterController characterController)
        {
            target = followTarget;
            if (characterController != null)
            {
                focusHeight = characterController.height * 0.73f;
                minimumDistance = Mathf.Max(0.22f, characterController.radius * 0.9f);
                collisionRadius = Mathf.Max(0.08f, characterController.radius * 0.42f);
            }
            InitializeOrbitFromCurrentPose();
        }

        private void Awake()
        {
            controlledCamera = GetComponent<Camera>();
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }
            InitializeOrbitFromCurrentPose();
        }

        private void InitializeOrbitFromCurrentPose()
        {
            if (target == null) return;
            Vector3 direction = transform.position - (target.position + Vector3.up * focusHeight);
            if (direction.sqrMagnitude < 0.04f) return;
            distance = Mathf.Clamp(direction.magnitude, minimumDistance, maximumDistance);
            yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 180f;
            pitch = Mathf.Clamp(Mathf.Asin(direction.normalized.y) * Mathf.Rad2Deg, -15f, 68f);
            smoothFocus = target.position + Vector3.up * focusHeight;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5)) ToggleDeveloperMode();
            if (mode == ViewMode.Developer)
            {
                UpdateDeveloperCamera();
                return;
            }

            if (Input.GetKeyDown(KeyCode.F))
                mode = mode == ViewMode.FirstPerson ? ViewMode.ThirdPerson : ViewMode.FirstPerson;
            if (Input.GetKeyDown(KeyCode.V)) shoulderSign *= -1f;

            if (Input.GetMouseButton(1) || mode == ViewMode.FirstPerson)
            {
                yaw += Input.GetAxis("Mouse X") * 3.2f;
                pitch -= Input.GetAxis("Mouse Y") * 2.6f;
            }
            pitch = Mathf.Clamp(pitch, -15f, 68f);

            if (mode == ViewMode.ThirdPerson)
                distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * 0.48f,
                    minimumDistance, maximumDistance);
        }

        private void LateUpdate()
        {
            if (target == null || mode == ViewMode.Developer) return;

            Vector3 wantedFocus = target.position + Vector3.up * focusHeight;
            smoothFocus = Vector3.Lerp(smoothFocus, wantedFocus,
                1f - Mathf.Exp(-positionSharpness * Time.deltaTime));
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);

            if (mode == ViewMode.FirstPerson)
            {
                Vector3 position = wantedFocus + orbit * Vector3.forward * firstPersonForwardOffset;
                transform.position = Vector3.Lerp(transform.position, position,
                    1f - Mathf.Exp(-positionSharpness * Time.deltaTime));
                transform.rotation = Quaternion.Slerp(transform.rotation, orbit,
                    1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
                controlledCamera.nearClipPlane = 0.025f;
                return;
            }

            controlledCamera.nearClipPlane = 0.06f;
            Vector3 shoulder = orbit * Vector3.right * (shoulderOffset * shoulderSign);
            Vector3 desired = smoothFocus + shoulder + orbit * Vector3.back * distance;
            Vector3 ray = desired - smoothFocus;
            float safeDistance = ray.magnitude;
            Vector3 direction = ray.normalized;

            RaycastHit[] hits = Physics.SphereCastAll(smoothFocus, collisionRadius, direction,
                safeDistance, collisionLayers, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit hit in hits)
            {
                if (hit.transform == target || hit.transform.IsChildOf(target)) continue;
                safeDistance = Mathf.Min(safeDistance, Mathf.Max(minimumDistance, hit.distance - 0.08f));
            }

            Vector3 collisionSafe = smoothFocus + direction * safeDistance;
            transform.position = Vector3.Lerp(transform.position, collisionSafe,
                1f - Mathf.Exp(-positionSharpness * Time.deltaTime));
            Quaternion look = Quaternion.LookRotation(smoothFocus - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look,
                1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
        }

        private void ToggleDeveloperMode()
        {
            if (mode != ViewMode.Developer)
            {
                savedPosition = transform.position;
                savedRotation = transform.rotation;
                mode = ViewMode.Developer;
            }
            else
            {
                transform.SetPositionAndRotation(savedPosition, savedRotation);
                mode = ViewMode.ThirdPerson;
            }
        }

        private void UpdateDeveloperCamera()
        {
            if (Input.GetMouseButton(1))
            {
                Vector3 euler = transform.eulerAngles;
                float developerPitch = euler.x > 180f ? euler.x - 360f : euler.x;
                developerPitch -= Input.GetAxis("Mouse Y") * 2.6f;
                float developerYaw = euler.y + Input.GetAxis("Mouse X") * 3.2f;
                transform.rotation = Quaternion.Euler(Mathf.Clamp(developerPitch, -89f, 89f), developerYaw, 0f);
            }

            Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (Input.GetKey(KeyCode.E)) input.y += 1f;
            if (Input.GetKey(KeyCode.Q)) input.y -= 1f;
            float multiplier = Input.GetKey(KeyCode.LeftShift) ? 3f : 1f;
            transform.position += transform.TransformDirection(Vector3.ClampMagnitude(input, 1f)) *
                                  (developerSpeed * multiplier * Time.unscaledDeltaTime);
        }
    }
}
