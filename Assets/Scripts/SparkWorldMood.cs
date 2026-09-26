using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prototype visual language for Spark loss: the neighbourhood gradually
/// desaturates while Danny remains in colour. This is emotional comedy, not a
/// health/death effect.
/// </summary>
public sealed class SparkWorldMood : MonoBehaviour
{
    private sealed class Entry
    {
        public Material Material;
        public Color Original;
    }

    [SerializeField] private DannySpark spark;
    [SerializeField, Range(0f, 1f)] private float smoothing = 0.10f;
    private readonly List<Entry> entries = new();
    private float amount;

    public void Configure(DannySpark source) => spark = source;

    private void Start()
    {
        if (spark == null) spark = FindFirstObjectByType<DannySpark>();
        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (renderer.GetComponentInParent<DannySpark>() != null) continue;
            foreach (Material material in renderer.materials)
            {
                if (material == null) continue;
                entries.Add(new Entry { Material=material, Original=material.color });
            }
        }
    }

    private void Update()
    {
        if (spark == null) return;
        float target = Mathf.InverseLerp(0.70f, 0.12f, spark.Normalized);
        amount = Mathf.Lerp(amount, target, 1f - Mathf.Pow(smoothing, Time.deltaTime));
        foreach (Entry entry in entries)
        {
            if (entry.Material == null) continue;
            float grey = entry.Original.grayscale;
            Color desaturated = new(grey, grey, grey, entry.Original.a);
            entry.Material.color = Color.Lerp(entry.Original, desaturated, amount);
        }
    }
}
