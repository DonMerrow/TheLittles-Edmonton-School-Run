using System.Collections.Generic;
using UnityEngine;

namespace TheLittles
{
    /// <summary>
    /// Adapts an imported humanoid prefab to the existing 2D physics game.
    /// Animator parameters are optional: a model without a configured controller
    /// still appears, turns and follows the complete v0.7 gameplay route.
    /// </summary>
    public sealed class HumanoidLittleVisual : LittleVisualController
    {
        private const float TargetHeight = 1.12f;
        private const float FeetHeight = -0.43f;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int GroundedId = Animator.StringToHash("Grounded");
        private static readonly int CrawlingId = Animator.StringToHash("Crawling");
        private static readonly int VerticalSpeedId = Animator.StringToHash("VerticalSpeed");
        private static readonly int AttentionId = Animator.StringToHash("Attention");
        private static readonly int SocialActionId = Animator.StringToHash("SocialAction");
        private static readonly int LandId = Animator.StringToHash("Land");

        private readonly HashSet<int> animatorParameters = new();
        private Transform modelRoot;
        private Vector3 modelHome;
        private Animator animator;
        private Transform hips;
        private Transform spine;
        private Transform chest;
        private Transform leftUpperLeg;
        private Transform leftLowerLeg;
        private Transform leftFoot;
        private Transform rightUpperLeg;
        private Transform rightLowerLeg;
        private Transform rightFoot;
        private Transform leftUpperArm;
        private Transform leftLowerArm;
        private Transform rightUpperArm;
        private Transform rightLowerArm;
        private readonly Dictionary<Transform, Quaternion> restRotations = new();
        private float movement;
        private float requestedMovement;
        private float facing = 1f;
        private float crawlBlend;
        private float landingPose;
        private bool grounded;
        private bool underAttention;
        private LittleSocialAction socialAction;
        private float gaitPhase;
        private bool proceduralBones;
        private float previousLandingPose;

        public void Configure(GameObject modelInstance)
        {
            modelRoot = modelInstance.transform;
            animator = modelInstance.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.runtimeAnimatorController == null)
            {
                RuntimeAnimatorController controller =
                    Resources.Load<RuntimeAnimatorController>("Littles/Animations/MothAnimator");
                if (controller != null) animator.runtimeAnimatorController = controller;
            }
            CacheAnimatorParameters();
            CacheHumanoidBones();
            NormalizeModel();
            modelHome = modelRoot.localPosition;

            foreach (Renderer renderer in modelInstance.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sortingLayerName = "Default";
                renderer.sortingOrder = 30;
            }
        }

        public override void SetMotion(float horizontal, float requested, bool isGrounded,
            bool attention, bool crawling, float landing, float vertical, float weight)
        {
            movement = Mathf.Clamp(horizontal, -1f, 1f);
            requestedMovement = Mathf.Clamp(requested, -1f, 1f);
            grounded = isGrounded;
            underAttention = attention;
            landingPose = Mathf.Clamp01(landing);
            crawlBlend = Mathf.MoveTowards(crawlBlend, crawling ? 1f : 0f, Time.deltaTime * 6f);

            SetFloat(SpeedId, Mathf.Abs(movement));
            SetBool(GroundedId, grounded);
            SetBool(CrawlingId, crawling);
            SetFloat(VerticalSpeedId, vertical);
            SetBool(AttentionId, underAttention);
            if (landingPose > 0.55f && previousLandingPose <= 0.55f)
                SetTrigger(LandId);
            previousLandingPose = landingPose;
        }

        public override void SetSocial(LittleSocialAction action, Vector2 direction)
        {
            socialAction = action;
            SetInt(SocialActionId, (int)action);
            if (Mathf.Abs(direction.x) > 0.04f && action != LittleSocialAction.None)
                facing = Mathf.Sign(direction.x);
        }

        private void Update()
        {
            if (modelRoot == null) return;

            float direction = Mathf.Abs(movement) > 0.03f ? movement : requestedMovement;
            if (Mathf.Abs(direction) > 0.03f) facing = Mathf.Sign(direction);

            // Imported Unity humanoids normally face +Z. Rotate them into the
            // side-on 2.5D route while retaining a small amount of their face.
            float targetYaw = facing > 0f ? 135f : -135f;
            float poseLean = socialAction == LittleSocialAction.HelpUp ? facing * 7f : 0f;
            float crawlLean = facing * crawlBlend * 24f;
            Quaternion targetRotation = Quaternion.Euler(0f, targetYaw, -(poseLean + crawlLean));
            modelRoot.localRotation = Quaternion.Slerp(modelRoot.localRotation, targetRotation,
                Time.deltaTime * 10f);

            // A restrained physical response remains visible even before an
            // Animator Controller is assigned to the imported prefab.
            bool hasAuthoredAnimation = animator != null && animator.runtimeAnimatorController != null;
            float step = hasAuthoredAnimation ? 0f : Mathf.Sin(Time.time * 9f) * Mathf.Abs(movement);
            float nervous = underAttention ? Mathf.Sin(Time.time * 18f) * 0.018f : 0f;
            float verticalOffset = hasAuthoredAnimation ? 0f :
                Mathf.Abs(step) * 0.018f + nervous - landingPose * 0.035f;
            modelRoot.localPosition = modelHome + Vector3.up * verticalOffset;

            // LittleMotor2D owns the wrapper's lean and depth transform. Keeping
            // that transform in one place prevents imported rigs from fighting
            // the movement code and corkscrewing during a turn or crawl.
        }

        private void LateUpdate()
        {
            if (!proceduralBones) return;

            float speed = Mathf.Clamp01(Mathf.Abs(movement));
            gaitPhase += Time.deltaTime * Mathf.Lerp(4.5f, 9.5f, speed);

            float wave = Mathf.Sin(gaitPhase);
            float leftLift = Mathf.Max(0f, wave);
            float rightLift = Mathf.Max(0f, -wave);
            float stride = wave * 28f * speed;
            float crawl = crawlBlend;

            // The rotations are applied around the model's own anatomical axes,
            // so the gait keeps working after the character turns on the route.
            Pose(leftUpperLeg, stride + crawl * 48f, 0f);
            Pose(rightUpperLeg, -stride + crawl * 48f, 0f);
            Pose(leftLowerLeg, -(5f * speed + leftLift * 31f + crawl * 35f), 0f);
            Pose(rightLowerLeg, -(5f * speed + rightLift * 31f + crawl * 35f), 0f);
            Pose(leftFoot, 8f * speed - leftLift * 15f, 0f);
            Pose(rightFoot, 8f * speed - rightLift * 15f, 0f);

            // Lower the exported A-pose arms, then counter-swing them against
            // the legs. This is deliberately restrained rather than cartoony.
            Pose(leftUpperArm, -stride * 0.58f - crawl * 38f, 38f);
            Pose(rightUpperArm, stride * 0.58f - crawl * 38f, -38f);
            Pose(leftLowerArm, -(9f + leftLift * 8f + crawl * 48f), 0f);
            Pose(rightLowerArm, -(9f + rightLift * 8f + crawl * 48f), 0f);

            float bodySway = Mathf.Sin(gaitPhase * 2f) * 2.2f * speed;
            Pose(hips, -bodySway * 0.45f, bodySway);
            Pose(spine, bodySway * 0.35f, -bodySway * 0.7f);
            Pose(chest, bodySway * 0.2f, -bodySway * 0.35f);
        }

        private void CacheHumanoidBones()
        {
            // A Humanoid import gives the most reliable mapping. The name
            // fallback also covers the usual UMA/FBX bone names when Unity
            // imports the model as Generic.
            hips = HumanBone(HumanBodyBones.Hips, "hips", "pelvis");
            spine = HumanBone(HumanBodyBones.Spine, "spine");
            chest = HumanBone(HumanBodyBones.Chest, "chest", "spine1", "spine_01");
            leftUpperLeg = HumanBone(HumanBodyBones.LeftUpperLeg, "leftupleg", "leftthigh", "thigh_l");
            leftLowerLeg = HumanBone(HumanBodyBones.LeftLowerLeg, "leftleg", "leftcalf", "calf_l");
            leftFoot = HumanBone(HumanBodyBones.LeftFoot, "leftfoot", "foot_l");
            rightUpperLeg = HumanBone(HumanBodyBones.RightUpperLeg, "rightupleg", "rightthigh", "thigh_r");
            rightLowerLeg = HumanBone(HumanBodyBones.RightLowerLeg, "rightleg", "rightcalf", "calf_r");
            rightFoot = HumanBone(HumanBodyBones.RightFoot, "rightfoot", "foot_r");
            leftUpperArm = HumanBone(HumanBodyBones.LeftUpperArm, "leftarm", "leftupperarm", "upperarm_l");
            leftLowerArm = HumanBone(HumanBodyBones.LeftLowerArm, "leftforearm", "leftlowerarm", "lowerarm_l");
            rightUpperArm = HumanBone(HumanBodyBones.RightUpperArm, "rightarm", "rightupperarm", "upperarm_r");
            rightLowerArm = HumanBone(HumanBodyBones.RightLowerArm, "rightforearm", "rightlowerarm", "lowerarm_r");

            Remember(hips, spine, chest, leftUpperLeg, leftLowerLeg, leftFoot,
                rightUpperLeg, rightLowerLeg, rightFoot, leftUpperArm, leftLowerArm,
                rightUpperArm, rightLowerArm);

            proceduralBones = (animator == null || animator.runtimeAnimatorController == null) &&
                leftUpperLeg != null && leftLowerLeg != null &&
                rightUpperLeg != null && rightLowerLeg != null;
        }

        private Transform HumanBone(HumanBodyBones bone, params string[] nameHints)
        {
            if (animator != null && animator.isHuman)
            {
                Transform mapped = animator.GetBoneTransform(bone);
                if (mapped != null) return mapped;
            }

            Transform[] all = modelRoot.GetComponentsInChildren<Transform>(true);
            foreach (Transform candidate in all)
            {
                string compact = candidate.name.ToLowerInvariant()
                    .Replace(" ", "").Replace("_", "").Replace("-", "");
                foreach (string hint in nameHints)
                {
                    string compactHint = hint.Replace("_", "").Replace("-", "");
                    if (compact.Contains(compactHint)) return candidate;
                }
            }
            return null;
        }

        private void Remember(params Transform[] bones)
        {
            foreach (Transform bone in bones)
                if (bone != null && !restRotations.ContainsKey(bone))
                    restRotations.Add(bone, bone.localRotation);
        }

        private void Pose(Transform bone, float forwardAngle, float sideAngle)
        {
            if (bone == null || bone.parent == null || !restRotations.TryGetValue(bone, out Quaternion rest))
                return;

            Vector3 forwardAxis = bone.parent.InverseTransformDirection(modelRoot.right).normalized;
            Vector3 sideAxis = bone.parent.InverseTransformDirection(modelRoot.forward).normalized;
            bone.localRotation = Quaternion.AngleAxis(sideAngle, sideAxis) *
                Quaternion.AngleAxis(forwardAngle, forwardAxis) * rest;
        }

        private void NormalizeModel()
        {
            Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                modelRoot.localPosition = new Vector3(0f, FeetHeight, -0.18f);
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float height = Mathf.Max(0.01f, bounds.size.y);
            float scale = TargetHeight / height;
            modelRoot.localScale = Vector3.one * scale;

            // Recalculate after scaling so the feet and horizontal centre align
            // with the unchanged Rigidbody2D and BoxCollider2D.
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            Vector3 localCentre = transform.InverseTransformPoint(bounds.center);
            Vector3 localMinimum = transform.InverseTransformPoint(bounds.min);
            modelRoot.localPosition += new Vector3(-localCentre.x, FeetHeight - localMinimum.y, -0.18f);
        }

        private void CacheAnimatorParameters()
        {
            if (animator == null) return;
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                animatorParameters.Add(parameter.nameHash);
        }

        private void SetFloat(int id, float value)
        {
            if (animator != null && animatorParameters.Contains(id)) animator.SetFloat(id, value);
        }

        private void SetBool(int id, bool value)
        {
            if (animator != null && animatorParameters.Contains(id)) animator.SetBool(id, value);
        }

        private void SetInt(int id, int value)
        {
            if (animator != null && animatorParameters.Contains(id)) animator.SetInteger(id, value);
        }

        private void SetTrigger(int id)
        {
            if (animator != null && animatorParameters.Contains(id)) animator.SetTrigger(id);
        }
    }
}
