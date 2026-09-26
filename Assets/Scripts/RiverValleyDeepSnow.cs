using UnityEngine;

/// <summary>Sticky, ankle-deep snow: dodgeable, but costly enough for Mom to gain.</summary>
[RequireComponent(typeof(Collider))]
public sealed class RiverValleyDeepSnow : MonoBehaviour
{
    [SerializeField] private float slowScale = 0.38f;
    [SerializeField] private float slowPulseSeconds = 0.48f;
    [SerializeField] private float sparkDamage = 2.5f;
    private RiverValleyGameDirector director;
    private float nextConsequence;
    private static int entries;

    public static int Entries => entries;

    private void Start() => director = FindFirstObjectByType<RiverValleyGameDirector>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<DannySpark>() == null) return;
        entries++;
        Apply(other, true);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.GetComponentInParent<DannySpark>() == null) return;
        Apply(other, false);
    }

    private void Apply(Collider other, bool entered)
    {
        DannyTestController movement = other.GetComponentInParent<DannyTestController>();
        movement?.ApplySlow(slowScale, slowPulseSeconds);
        if (!entered && Time.time < nextConsequence) return;
        nextConsequence = Time.time + 3.2f;
        director?.Hazard("DEEP SNOW",
            "Clinging snow grabs Danny's boots. Mom is gaining while he pulls free!",
            sparkDamage, slowScale, 0.65f);
    }

    public void OnFootstep(AnimationEvent animationEvent) { }
    public void OnLand(AnimationEvent animationEvent) { }
}
