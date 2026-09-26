using UnityEngine;

public sealed class DannySparkFeedback : MonoBehaviour
{
    [SerializeField] private DannySpark spark;
    [SerializeField] private Transform player;
    [SerializeField] private AudioClip sparkUp;
    [SerializeField] private AudioClip sparkDown;
    private ParticleSystem particles;
    private float lastNormalized;

    private void Start()
    {
        if (spark == null) spark = FindFirstObjectByType<DannySpark>();
        if (player == null && spark != null) player = spark.transform;
        if (spark != null) lastNormalized = spark.Normalized;
        particles = BuildParticles();
        if (spark != null) spark.SparkChanged += Changed;
    }

    private void OnDestroy()
    {
        if (spark != null) spark.SparkChanged -= Changed;
    }

    private void Changed(float normalized, string reason)
    {
        bool positive = normalized >= lastNormalized;
        lastNormalized = normalized;
        AudioClip clip = positive ? sparkUp : sparkDown;
        if (clip != null && player != null) AudioSource.PlayClipAtPoint(clip, player.position + Vector3.up, 0.72f);
        if (particles == null) return;
        ParticleSystem.MainModule main = particles.main;
        main.startColor = positive
            ? new ParticleSystem.MinMaxGradient(new Color(1f,0.72f,0.08f), new Color(1f,0.28f,0.03f))
            : new ParticleSystem.MinMaxGradient(new Color(0.58f,0.72f,0.88f), new Color(0.20f,0.24f,0.31f));
        particles.Emit(positive ? 38 : 25);
    }

    private ParticleSystem BuildParticles()
    {
        GameObject go = new("Danny Spark burst");
        go.transform.SetParent(player != null ? player : transform, false);
        go.transform.localPosition = new Vector3(0f,1.25f,0f);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false; main.loop = false; main.startLifetime = 0.75f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f,3.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f,0.12f); main.maxParticles = 160;
        ParticleSystem.EmissionModule emission = ps.emission; emission.enabled = false;
        ParticleSystem.ShapeModule shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = 0.45f;
        // A runtime-created particle system otherwise receives Unity's hidden
        // default particle material. That shader can be stripped from a
        // standalone build, producing the large magenta squares seen during
        // Spark changes. Reuse the snow material which the scene references
        // explicitly, while retaining each particle's positive/negative tint.
        Material particleMaterial = Resources.Load<Material>("Littles/RiverValley/Materials/Blowing Snow");
        if (particleMaterial != null)
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial;
        // Spark changes remain clear in the bar and sound. On the target
        // renderer even the snow material occasionally expanded into large
        // coloured cards, so retire this redundant burst completely.
        go.GetComponent<ParticleSystemRenderer>().enabled=false;
        return ps;
    }

#if UNITY_EDITOR
    public void Configure(DannySpark newSpark, Transform newPlayer, AudioClip up, AudioClip down)
    { spark=newSpark; player=newPlayer; sparkUp=up; sparkDown=down; }
#endif
}
