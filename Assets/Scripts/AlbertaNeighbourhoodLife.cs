using UnityEngine;

/// <summary>
/// Keeps Edmonton friendly. Nearby neighbours briefly call out, turn toward
/// Danny, then carry on with their own winter walk.
/// </summary>
[DisallowMultipleComponent]
public sealed class AlbertaNeighbourhoodLife : MonoBehaviour
{
    private static readonly string[] KidGreetings =
    {
        "Hi, Danny!", "Hey there!", "How's it going?", "See you at school!"
    };

    private static readonly string[] AdultGreetings =
    {
        "Morning!", "Stay warm!", "Good morning!", "Hi, neighbour!",
        "Cold one, eh?", "Beautiful Alberta day!"
    };

    private readonly System.Collections.Generic.List<WinterCastIdentity> neighbours = new();
    private Transform player;
    private WinterAudioDirector audioDirector;
    private float nextGreeting;
    private int greetingCursor;
    public int GreetingsShown { get; private set; }

    private void Start()
    {
        DannySpark danny = FindFirstObjectByType<DannySpark>();
        player = danny != null ? danny.transform : null;
        audioDirector = FindFirstObjectByType<WinterAudioDirector>();
        neighbours.AddRange(FindObjectsByType<WinterCastIdentity>(FindObjectsSortMode.None));
        neighbours.RemoveAll(identity => identity == null ||
            identity.GetComponent<RiverValleyMomChase>() != null ||
            identity.GetComponentInParent<RiverValleyMomChase>() != null);
        nextGreeting = Time.time + 6.5f;
    }

    private void Update()
    {
        if (player == null) return;
        if (Time.time >= nextGreeting)
        {
            GreetFromBestNeighbour();
            nextGreeting = Time.time + 9.5f + Mathf.PingPong(GreetingsShown * 1.37f, 4.5f);
        }
    }

    private void GreetFromBestNeighbour()
    {
        WinterCastIdentity best = null;
        float bestScore = float.PositiveInfinity;
        for (int step = 0; step < neighbours.Count; step++)
        {
            int index = (greetingCursor + step) % neighbours.Count;
            WinterCastIdentity identity = neighbours[index];
            if (identity == null || !identity.gameObject.activeInHierarchy) continue;
            float distance = Vector3.Distance(identity.transform.position, player.position);
            if (distance < 2.2f || distance > 18f) continue;
            float score = distance + step * 0.08f;
            if (score >= bestScore) continue;
            bestScore = score;
            best = identity;
            greetingCursor = index + 1;
        }
        if (best == null) return;

        string role=(best.StoryRole+" "+best.name).ToLowerInvariant();
        bool child=role.Contains("child")||role.Contains("kid")||role.Contains("classmate")||
            role.Contains("snow_fort")||role.Contains("sledding");
        string[] greetings=child?KidGreetings:AdultGreetings;
        string greeting = greetings[GreetingsShown % greetings.Length];
        // Alberta still says hello, but only as an occasional recorded voice.
        // Floating text balloons obscured the route and made the scene feel
        // like a developer overlay, so greetings now stay in the soundscape.
        if(audioDirector!=null&&!audioDirector.SpeakGreeting(best.DisplayName,greeting))return;
        best.GetComponent<RiverValleyAmbientActor>()?.NoticePlayer(1.35f);
        GreetingsShown++;
    }

#if UNITY_EDITOR
    public void Configure(Material newBubbleMaterial)
    {
        // Retained so older scene builders continue to compile. Greetings are
        // deliberately audio-only now, so no material is required.
    }
#endif
}
