using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class RiverValleySlipPatch : MonoBehaviour
{
    [SerializeField] private float sparkDamage = 4f;
    [SerializeField] private float cooldown = 2.5f;
    private RiverValleyGameDirector director;
    private WinterAudioDirector audioDirector;
    private float nextSlip;
    private static int followerSlipSequence;

    private void Start()
    {
        director = FindFirstObjectByType<RiverValleyGameDirector>();
        audioDirector = FindFirstObjectByType<WinterAudioDirector>();
    }

    private void OnTriggerEnter(Collider other)
    {
        RiverValleyKidFollower follower = other.GetComponentInParent<RiverValleyKidFollower>();
        if (follower != null && !follower.IsScattered && !follower.IsWaiting)
        {
            // Only some followers lose their footing; the group should feel
            // vulnerable on ice without dissolving at every patch.
            followerSlipSequence++;
            if (followerSlipSequence % 3 == 0)
                director?.FollowerStuckOnIce(follower, transform.position);
            return;
        }
        DannySpark spark = other.GetComponentInParent<DannySpark>();
        if (spark == null || Time.time < nextSlip) return;
        nextSlip = Time.time + cooldown;
        director?.Hazard("ICE", "The path steals one boot-length of dignity.", sparkDamage, 0.46f, 1.1f);
        spark.GetComponentInChildren<Animator>()?.SetTrigger("Stumble");
        audioDirector?.PlayIceScrape(other.transform.position);
    }
}
