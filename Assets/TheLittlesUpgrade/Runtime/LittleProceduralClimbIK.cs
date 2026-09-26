using UnityEngine;

namespace TheLittles.Upgrade
{
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    public sealed class LittleProceduralClimbIK : MonoBehaviour
    {
        [SerializeField] private LittleTraversalController traversal;
        [SerializeField] private Animator animator;
        [SerializeField, Range(0f, 1f)] private float positionWeight = 0.92f;
        [SerializeField, Range(0f, 1f)] private float rotationWeight = 0.72f;
        [SerializeField, Min(1f)] private float weightSharpness = 11f;

        private float weight;

        public void Configure(LittleTraversalController newTraversal, Animator newAnimator)
        {
            traversal = newTraversal;
            animator = newAnimator;
        }

        private void Awake()
        {
            if (traversal == null) traversal = GetComponent<LittleTraversalController>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
        }

        private void Update()
        {
            float target = traversal != null && traversal.IsClimbing ? 1f : 0f;
            weight = Mathf.MoveTowards(weight, target, weightSharpness * Time.deltaTime);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null || traversal == null || !animator.isHuman)
                return;

            if (weight <= 0.001f)
            {
                ClearWeights();
                return;
            }

            Vector3 normal = traversal.WallNormal.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, normal).normalized;
            float scale = Mathf.Max(0.22f, GetComponent<CharacterController>().height / 1.8f);
            float phase = traversal.ClimbPhase;
            float movement = Mathf.Clamp01(traversal.ClimbInput.magnitude * 1.5f);
            float leftWave = Mathf.Sin(phase) * 0.16f * scale * movement;
            float rightWave = Mathf.Sin(phase + Mathf.PI) * 0.16f * scale * movement;
            Vector3 wallPlane = transform.position - normal * traversal.WallClearance;

            Vector3 leftHand = wallPlane + Vector3.up * (1.43f * scale + leftWave) - right * 0.28f * scale;
            Vector3 rightHand = wallPlane + Vector3.up * (1.43f * scale + rightWave) + right * 0.28f * scale;
            Vector3 leftFoot = wallPlane + Vector3.up * (0.43f * scale + rightWave) - right * 0.19f * scale;
            Vector3 rightFoot = wallPlane + Vector3.up * (0.43f * scale + leftWave) + right * 0.19f * scale;

            Quaternion handRotation = Quaternion.LookRotation(normal, Vector3.up);
            Quaternion footRotation = Quaternion.LookRotation(-normal, Vector3.up);

            ApplyGoal(AvatarIKGoal.LeftHand, leftHand, handRotation);
            ApplyGoal(AvatarIKGoal.RightHand, rightHand, handRotation);
            ApplyGoal(AvatarIKGoal.LeftFoot, leftFoot, footRotation);
            ApplyGoal(AvatarIKGoal.RightFoot, rightFoot, footRotation);

            animator.bodyPosition = Vector3.Lerp(animator.bodyPosition,
                transform.position + Vector3.up * 0.88f * scale, weight * 0.28f);
        }

        private void ApplyGoal(AvatarIKGoal goal, Vector3 position, Quaternion rotation)
        {
            animator.SetIKPositionWeight(goal, weight * positionWeight);
            animator.SetIKRotationWeight(goal, weight * rotationWeight);
            animator.SetIKPosition(goal, position);
            animator.SetIKRotation(goal, rotation);
        }

        private void ClearWeights()
        {
            foreach (AvatarIKGoal goal in new[]
                     {
                         AvatarIKGoal.LeftHand, AvatarIKGoal.RightHand,
                         AvatarIKGoal.LeftFoot, AvatarIKGoal.RightFoot
                     })
            {
                animator.SetIKPositionWeight(goal, 0f);
                animator.SetIKRotationWeight(goal, 0f);
            }
        }
    }
}
