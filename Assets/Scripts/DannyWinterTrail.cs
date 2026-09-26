using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public sealed class DannyWinterTrail : MonoBehaviour
{
    [SerializeField] private AudioClip[] snowCrunches;
    [SerializeField] private AudioClip snowLanding;
    [SerializeField] private Material footprintMaterial;
    [SerializeField] private float distancePerStep = 0.43f;
    [SerializeField] private float footprintLifetime = 42f;
    [SerializeField] private int maximumFootprints = 90;

    private readonly Queue<GameObject> footprints = new();
    private Animator animator;
    private CharacterController controller;
    private DannyTestController motor;
    private Transform playerRoot;
    private AudioSource source;
    private Vector3 previousPosition;
    private float distanceSinceStep;
    private float nextStepAt;
    private bool leftStep;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponentInParent<CharacterController>();
        motor = GetComponentInParent<DannyTestController>();
        playerRoot = controller != null ? controller.transform : transform;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0.35f;
        source.volume = 0.58f;
        previousPosition = playerRoot.position;
    }

    private void Update()
    {
        Vector3 delta = Vector3.ProjectOnPlane(playerRoot.position - previousPosition, Vector3.up);
        previousPosition = playerRoot.position;
        if (motor == null || !motor.IsGrounded || motor.CurrentPlanarSpeed < 0.12f)
        {
            distanceSinceStep = 0f;
            return;
        }

        distanceSinceStep += delta.magnitude;
        float stride = motor.CurrentPlanarSpeed > 1.45f ? distancePerStep * 1.42f : distancePerStep;
        if (distanceSinceStep >= stride)
        {
            distanceSinceStep = 0f;
            MakeStep();
        }
    }

    public void OnFootstep(AnimationEvent animationEvent) => MakeStep();

    public void OnLand(AnimationEvent animationEvent)
    {
        if (snowLanding == null || source == null) return;
        source.pitch = Random.Range(0.94f, 1.04f);
        source.PlayOneShot(snowLanding, 0.68f);
    }

    private void MakeStep()
    {
        if (Time.time < nextStepAt || (controller != null && !controller.isGrounded)) return;
        nextStepAt = Time.time + 0.14f;
        leftStep = !leftStep;

        if (snowCrunches != null && snowCrunches.Length > 0 && source != null)
        {
            AudioClip clip = snowCrunches[Random.Range(0, snowCrunches.Length)];
            if (clip != null)
            {
                source.pitch = Random.Range(0.91f, 1.09f);
                source.PlayOneShot(clip, Random.Range(0.46f, 0.64f));
            }
        }

        if (footprintMaterial == null || animator == null) return;
        Transform foot = animator.GetBoneTransform(leftStep ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
        Vector3 origin = (foot != null ? foot.position : playerRoot.position) + Vector3.up * 0.35f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.1f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return;

        string surface = (hit.collider.name + " " + hit.collider.GetComponent<Renderer>()?.sharedMaterial?.name).ToLowerInvariant();
        if (!surface.Contains("snow") && !surface.Contains("sidewalk") &&
            !surface.Contains("stair") && !surface.Contains("path")) return;

        GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mark.name = leftStep ? "Left boot print" : "Right boot print";
        Destroy(mark.GetComponent<Collider>());
        mark.transform.position = hit.point + hit.normal * 0.006f;
        mark.transform.rotation = Quaternion.LookRotation(
            Vector3.ProjectOnPlane(playerRoot.forward, hit.normal).normalized, hit.normal);
        mark.transform.localScale = new Vector3(0.105f, 0.006f, 0.235f);
        mark.GetComponent<Renderer>().sharedMaterial = footprintMaterial;
        footprints.Enqueue(mark);
        Destroy(mark, footprintLifetime);
        while (footprints.Count > maximumFootprints)
        {
            GameObject oldest = footprints.Dequeue();
            if (oldest != null) Destroy(oldest);
        }
    }

#if UNITY_EDITOR
    public void Configure(AudioClip[] newCrunches, AudioClip newLanding, Material newFootprintMaterial)
    {
        snowCrunches = newCrunches;
        snowLanding = newLanding;
        footprintMaterial = newFootprintMaterial;
    }
#endif
}
