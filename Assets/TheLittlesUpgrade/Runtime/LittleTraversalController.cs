using System;
using UnityEngine;

namespace TheLittles.Upgrade
{
    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class LittleTraversalController : MonoBehaviour
    {
        [Header("Climbing")]
        [SerializeField] private LayerMask climbableLayers = ~0;
        [SerializeField, Min(0.25f)] private float wallCheckDistance = 0.75f;
        [SerializeField, Min(0.1f)] private float climbSpeed = 1.9f;
        [SerializeField, Min(0.1f)] private float sidewaysSpeed = 1.65f;
        [SerializeField, Min(0.05f)] private float wallClearance = 0.28f;
        [SerializeField, Range(0f, 0.65f)] private float maximumWallUpNormal = 0.35f;
        [SerializeField, Min(0.1f)] private float wallTurnSharpness = 18f;
        [SerializeField, Min(0.1f)] private float mantleDuration = 0.48f;
        [SerializeField, Min(0.2f)] private float mantleProbeHeight = 0.85f;
        [SerializeField, Min(0.2f)] private float jumpAwaySpeed = 4.2f;

        [Header("References (automatically assigned)")]
        [SerializeField] private Animator animator;
        [SerializeField] private Behaviour normalLocomotion;

        private CharacterController controller;
        private Vector3 wallNormal;
        private Vector2 climbInput;
        private float climbPhase;
        private bool climbing;
        private bool hanging;
        private bool mantling;
        private Vector3 mantleStart;
        private Vector3 mantleTarget;
        private Quaternion mantleStartRotation;
        private float mantleClock;

        public bool IsClimbing => climbing;
        public bool IsHanging => hanging;
        public Vector3 WallNormal => wallNormal;
        public Vector2 ClimbInput => climbInput;
        public float ClimbPhase => climbPhase;
        public float WallClearance => wallClearance;

        public void Configure(Animator newAnimator, Behaviour locomotion)
        {
            animator = newAnimator;
            normalLocomotion = locomotion;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (normalLocomotion == null) normalLocomotion = FindNormalLocomotion();
        }

        private void Update()
        {
            if (mantling)
            {
                UpdateMantle();
                return;
            }

            if (!climbing)
            {
                if (PressedJump() && TryFindWall(transform.forward, out RaycastHit wall))
                    BeginClimb(wall);
                return;
            }

            UpdateClimb();
        }

        private void UpdateClimb()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            if (Input.GetKey(KeyCode.Space)) vertical = Mathf.Max(vertical, 1f);
            climbInput = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);

            if (Input.GetKeyDown(KeyCode.C))
            {
                EndClimb(false);
                return;
            }

            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) &&
                Input.GetKeyDown(KeyCode.Space))
            {
                Vector3 jump = wallNormal * jumpAwaySpeed + Vector3.up * (jumpAwaySpeed * 0.72f);
                EndClimb(false);
                controller.Move(jump * Time.deltaTime);
                return;
            }

            Vector3 towardWall = -wallNormal;
            if (TryFindWall(towardWall, out RaycastHit wall))
            {
                wallNormal = Vector3.Slerp(wallNormal, wall.normal, 1f - Mathf.Exp(-16f * Time.deltaTime));
                wallNormal.Normalize();
                SnapTowardWall(wall);
            }
            else if (climbInput.y > 0.15f && TryFindMantleTop(out RaycastHit top))
            {
                BeginMantle(top);
                return;
            }
            else
            {
                EndClimb(false);
                return;
            }

            Vector3 wallRight = Vector3.Cross(Vector3.up, wallNormal).normalized;
            Vector3 motion = wallRight * (climbInput.x * sidewaysSpeed) +
                             Vector3.up * (climbInput.y * climbSpeed);
            controller.Move(motion * Time.deltaTime);

            Quaternion faceWall = Quaternion.LookRotation(-wallNormal, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, faceWall,
                1f - Mathf.Exp(-wallTurnSharpness * Time.deltaTime));

            climbPhase += climbInput.magnitude * Time.deltaTime * 5.4f;
            hanging = climbInput.sqrMagnitude < 0.015f;
            SetAnimatorState();

            if (climbInput.y < -0.1f && controller.isGrounded)
                EndClimb(false);
        }

        private void BeginClimb(RaycastHit wall)
        {
            climbing = true;
            hanging = false;
            wallNormal = wall.normal.normalized;
            climbInput = Vector2.zero;
            if (normalLocomotion != null) normalLocomotion.enabled = false;
            SnapTowardWall(wall);
            SetAnimatorState();
        }

        private void EndClimb(bool completedMantle)
        {
            climbing = false;
            hanging = false;
            mantling = false;
            climbInput = Vector2.zero;
            if (controller != null && !controller.enabled) controller.enabled = true;
            if (normalLocomotion != null) normalLocomotion.enabled = true;
            SetAnimatorState();
            if (completedMantle && controller != null)
                controller.Move(Vector3.down * 0.03f);
        }

        private void SnapTowardWall(RaycastHit wall)
        {
            Vector3 wantedRoot = wall.point + wall.normal * wallClearance;
            Vector3 correction = Vector3.ProjectOnPlane(wantedRoot - transform.position, Vector3.up);
            controller.Move(Vector3.ClampMagnitude(correction, 0.18f));
        }

        private bool TryFindWall(Vector3 direction, out RaycastHit selected)
        {
            float scale = Mathf.Max(0.25f, controller.height / 1.8f);
            Vector3 origin = transform.position + Vector3.up * controller.height * 0.55f;
            float radius = Mathf.Max(0.08f, controller.radius * 0.55f);
            RaycastHit[] hits = Physics.SphereCastAll(origin, radius, direction.normalized,
                wallCheckDistance * scale, climbableLayers, QueryTriggerInteraction.Ignore);

            selected = default;
            float nearest = float.PositiveInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (IsSelf(hit.transform) || Mathf.Abs(hit.normal.y) > maximumWallUpNormal) continue;
                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    selected = hit;
                }
            }
            return nearest < float.PositiveInfinity;
        }

        private bool TryFindMantleTop(out RaycastHit selected)
        {
            float scale = Mathf.Max(0.25f, controller.height / 1.8f);
            Vector3 origin = transform.position + Vector3.up * (controller.height + mantleProbeHeight * scale)
                             - wallNormal * (controller.radius + 0.18f * scale);
            RaycastHit[] hits = Physics.SphereCastAll(origin, controller.radius * 0.55f, Vector3.down,
                mantleProbeHeight * 2.4f * scale, climbableLayers, QueryTriggerInteraction.Ignore);

            selected = default;
            float nearest = float.PositiveInfinity;
            foreach (RaycastHit hit in hits)
            {
                if (IsSelf(hit.transform) || hit.normal.y < 0.65f) continue;
                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    selected = hit;
                }
            }
            return nearest < float.PositiveInfinity;
        }

        private void BeginMantle(RaycastHit top)
        {
            mantling = true;
            hanging = true;
            mantleClock = 0f;
            mantleStart = transform.position;
            mantleStartRotation = transform.rotation;
            float scale = Mathf.Max(0.25f, controller.height / 1.8f);
            mantleTarget = top.point - wallNormal * (controller.radius + 0.18f * scale) +
                           Vector3.up * (controller.skinWidth + 0.025f);
            controller.enabled = false;
            SetAnimatorState();
        }

        private void UpdateMantle()
        {
            mantleClock += Time.deltaTime;
            float t = Mathf.Clamp01(mantleClock / Mathf.Max(0.05f, mantleDuration));
            float smooth = t * t * (3f - 2f * t);
            Vector3 liftedStart = mantleStart + Vector3.up * Mathf.Max(0f, mantleTarget.y - mantleStart.y) * smooth;
            transform.position = Vector3.Lerp(liftedStart, mantleTarget, smooth);
            transform.rotation = Quaternion.Slerp(mantleStartRotation,
                Quaternion.LookRotation(-wallNormal, Vector3.up), smooth);

            if (t >= 1f) EndClimb(true);
        }

        private bool IsSelf(Transform candidate)
        {
            return candidate == transform || candidate.IsChildOf(transform) || transform.IsChildOf(candidate);
        }

        private Behaviour FindNormalLocomotion()
        {
            foreach (Behaviour behaviour in GetComponents<Behaviour>())
            {
                if (behaviour == this) continue;
                string typeName = behaviour.GetType().Name;
                if (typeName == "ThirdPersonController" || typeName == "MothMotor3D") return behaviour;
            }
            return null;
        }

        private static bool PressedJump()
        {
            try { return Input.GetButtonDown("Jump"); }
            catch (ArgumentException) { return Input.GetKeyDown(KeyCode.Space); }
        }

        private void SetAnimatorState()
        {
            if (animator == null) return;
            SetBoolIfPresent("Climbing", climbing);
            SetBoolIfPresent("Hanging", hanging || mantling);
            SetFloatIfPresent("ClimbHorizontal", climbInput.x);
            SetFloatIfPresent("ClimbVertical", climbInput.y);
        }

        private void SetBoolIfPresent(string name, bool value)
        {
            int hash = Animator.StringToHash(name);
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.nameHash == hash && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(hash, value);
                    return;
                }
        }

        private void SetFloatIfPresent(string name, float value)
        {
            int hash = Animator.StringToHash(name);
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                if (parameter.nameHash == hash && parameter.type == AnimatorControllerParameterType.Float)
                {
                    animator.SetFloat(hash, value, 0.08f, Time.deltaTime);
                    return;
                }
        }

        private void OnDisable()
        {
            if (normalLocomotion != null) normalLocomotion.enabled = true;
            if (controller != null && !controller.enabled) controller.enabled = true;
        }
    }
}
