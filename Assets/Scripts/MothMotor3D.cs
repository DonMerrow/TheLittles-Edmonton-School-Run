using System.Collections.Generic;
using UnityEngine;

namespace TheLittles
{
    /// <summary>
    /// Camera-relative third-person movement for the genuine 3D test scene.
    /// It deliberately does not inherit the old Rigidbody2D/fake-depth motor.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class MothMotor3D : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int CrawlingId = Animator.StringToHash("Crawling");
        private static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");
        private static readonly int LandId = Animator.StringToHash("Land");

        [SerializeField] private Transform movementCamera;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Animator animator;
        [SerializeField] private float walkSpeed = 3.4f;
        [SerializeField] private float crawlSpeed = 1.55f;
        [SerializeField] private float acceleration = 14f;
        [SerializeField] private float turnSharpness = 13f;
        [SerializeField] private float jumpHeight = 1.25f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float modelYawOffset = 180f;

        private readonly HashSet<int> parameters = new();
        private CharacterController controller;
        private Vector3 planarVelocity;
        private float verticalVelocity;
        private float standingHeight;
        private Vector3 standingCenter;
        private bool crawling;
        private bool wasGrounded;

        public void Configure(Transform cameraTransform, GameObject model)
        {
            movementCamera = cameraTransform;
            modelRoot = model.transform;
            animator = model.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                if (animator.runtimeAnimatorController == null)
                    animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(
                        "Littles/Animations/MothAnimator");
                animator.applyRootMotion = false;
            }

            PrepareModel();
            CacheParameters();
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            standingHeight = controller.height;
            standingCenter = controller.center;
            CacheParameters();
            InstallGrounding();
        }

        private void InstallGrounding()
        {
            if (modelRoot == null) return;
            MothGrounding grounding = GetComponent<MothGrounding>();
            if (grounding == null) grounding = gameObject.AddComponent<MothGrounding>();
            grounding.VisualRoot = modelRoot;
            grounding.CharacterController = controller != null ? controller : GetComponent<CharacterController>();
            grounding.Animator = animator != null ? animator : modelRoot.GetComponentInChildren<Animator>(true);
            grounding.MaximumCorrection = 1.25f;
            grounding.GroundingSpeed = 12f;
            grounding.SoleClearance = 0.01f;
        }

        private void Update()
        {
            if (controller == null) return;

            if (Input.GetKeyDown(KeyCode.C) && controller.isGrounded)
                crawling = !crawling;

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector2 input = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);

            Vector3 cameraForward = movementCamera != null ? movementCamera.forward : Vector3.forward;
            Vector3 cameraRight = movementCamera != null ? movementCamera.right : Vector3.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            Vector3 desiredDirection = cameraRight * input.x + cameraForward * input.y;
            if (desiredDirection.sqrMagnitude > 1f) desiredDirection.Normalize();

            float selectedSpeed = crawling ? crawlSpeed : walkSpeed;
            Vector3 desiredVelocity = desiredDirection * selectedSpeed;
            planarVelocity = Vector3.MoveTowards(planarVelocity, desiredVelocity,
                acceleration * Time.deltaTime);

            if (desiredDirection.sqrMagnitude > 0.0025f)
            {
                Quaternion desiredRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation,
                    1f - Mathf.Exp(-turnSharpness * Time.deltaTime));
            }

            bool grounded = controller.isGrounded;
            if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;

            if (grounded && Input.GetButtonDown("Jump"))
            {
                crawling = false;
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                grounded = false;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            controller.Move((planarVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

            bool groundedAfterMove = controller.isGrounded;
            if (!wasGrounded && groundedAfterMove && verticalVelocity < -3f)
                SetTrigger(LandId);
            if (groundedAfterMove && verticalVelocity < 0f) verticalVelocity = -2f;
            wasGrounded = groundedAfterMove;

            float targetHeight = crawling ? standingHeight * 0.52f : standingHeight;
            controller.height = Mathf.MoveTowards(controller.height, targetHeight, Time.deltaTime * 5f);
            float bottom = standingCenter.y - standingHeight * 0.5f;
            controller.center = new Vector3(standingCenter.x,
                bottom + controller.height * 0.5f, standingCenter.z);

            SetFloat(SpeedId, planarVelocity.magnitude / Mathf.Max(0.01f, walkSpeed));
            SetBool(GroundedId, groundedAfterMove);
            SetBool(CrawlingId, crawling);
            SetFloat(VerticalSpeedId, verticalVelocity);
        }

        private void PrepareModel()
        {
            if (modelRoot == null) return;
            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            modelRoot.localPosition = Vector3.zero;
            modelRoot.localRotation = Quaternion.Euler(0f, modelYawOffset, 0f);
            modelRoot.localScale = Vector3.one;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            modelRoot.localScale = Vector3.one * (1.72f / Mathf.Max(0.01f, bounds.size.y));

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 centre = transform.InverseTransformPoint(bounds.center);
            Vector3 minimum = transform.InverseTransformPoint(bounds.min);
            modelRoot.localPosition = new Vector3(-centre.x, -minimum.y, -centre.z);
        }

        private void CacheParameters()
        {
            parameters.Clear();
            if (animator == null) return;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                parameters.Add(parameter.nameHash);
        }

        private void SetFloat(int id, float value)
        {
            if (animator != null && parameters.Contains(id)) animator.SetFloat(id, value, 0.10f, Time.deltaTime);
        }

        private void SetBool(int id, bool value)
        {
            if (animator != null && parameters.Contains(id)) animator.SetBool(id, value);
        }

        private void SetTrigger(int id)
        {
            if (animator != null && parameters.Contains(id)) animator.SetTrigger(id);
        }
    }
}
