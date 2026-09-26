using UnityEngine;

/// <summary>A brief, readable puff of snow when Danny pulls a child out of a bank.</summary>
public sealed class SnowRescueBurst : MonoBehaviour
{
    private static Material whiteSnow;
    private static Material blueSnow;
    private Transform[] flakes;
    private Vector3[] velocities;
    private float born;

    public static void Spawn(Vector3 position)
    {
        GameObject root = new("Snow rescue puff");
        root.transform.position = position;
        SnowRescueBurst burst = root.AddComponent<SnowRescueBurst>();
        burst.Build();
    }

    private void Build()
    {
        born = Time.time;
        flakes = new Transform[14];
        velocities = new Vector3[flakes.Length];
        for (int i = 0; i < flakes.Length; i++)
        {
            float angle = i * 2.399963f;
            float radius = 0.28f + (i % 4) * 0.07f;
            GameObject flake = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Sphere : PrimitiveType.Cube);
            flake.name = "Flying snow puff";
            flake.transform.SetParent(transform, false);
            flake.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.12f, (i % 5) * 0.035f,
                Mathf.Sin(angle) * 0.12f);
            float size = 0.075f + (i % 4) * 0.025f;
            flake.transform.localScale = Vector3.one * size;
            Collider collider = flake.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            Renderer renderer = flake.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetSnowMaterial(i % 3 == 0);
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            flakes[i] = flake.transform;
            velocities[i] = new Vector3(Mathf.Cos(angle) * radius, 0.72f + (i % 5) * 0.11f,
                Mathf.Sin(angle) * radius);
        }
    }

    private void Update()
    {
        float age = Time.time - born;
        for (int i = 0; i < flakes.Length; i++)
        {
            if (flakes[i] == null) continue;
            velocities[i] += Vector3.down * (0.92f * Time.deltaTime);
            flakes[i].position += velocities[i] * Time.deltaTime;
            flakes[i].Rotate(80f * Time.deltaTime, 120f * Time.deltaTime, 55f * Time.deltaTime);
            flakes[i].localScale *= 1f - Mathf.Min(0.9f, Time.deltaTime * 0.65f);
        }
        if (age >= 1.35f) Destroy(gameObject);
    }

    private static Material GetSnowMaterial(bool blue)
    {
        Material cached = blue ? blueSnow : whiteSnow;
        if (cached != null) return cached;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ??
            Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        if (shader == null) return null;
        Color color = blue ? new Color(0.72f, 0.90f, 1f) : Color.white;
        Material created = new(shader) { color = color };
        if (created.HasProperty("_BaseColor")) created.SetColor("_BaseColor", color);
        if (blue) blueSnow = created;
        else whiteSnow = created;
        return created;
    }
}
