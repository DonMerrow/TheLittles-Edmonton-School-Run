using UnityEngine;

/// <summary>One-shot prototype interaction between a cast member and Danny.</summary>
[RequireComponent(typeof(WinterCastIdentity))]
public sealed class WinterCastInteraction : MonoBehaviour
{
    [SerializeField, Min(0f)] private float repeatDelay = 8f;
    private float nextAllowedTime;
    private WinterCastIdentity identity;

    private void Awake() => identity = GetComponent<WinterCastIdentity>();

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time < nextAllowedTime) return;
        DannySpark spark = other.GetComponentInParent<DannySpark>();
        if (spark == null || identity == null) return;
        nextAllowedTime = Time.time + repeatDelay;
        string moment = $"{identity.DisplayName}: {identity.InteractionPrompt}";
        if (identity.FriendlyDistraction)
            spark.Restore(identity.SparkDrain, moment);
        else
            spark.Drain(identity.SparkDrain, moment);
    }
}
