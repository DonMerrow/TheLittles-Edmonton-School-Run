using System;
using UnityEngine;

/// <summary>Danny's curiosity, joy and youthful energy—not physical health.</summary>
public sealed class DannySpark : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maximumSpark = 100f;
    [SerializeField] private float currentSpark = 100f;
    public event Action<float, string> SparkChanged;

    public float Current => currentSpark;
    public float Maximum => maximumSpark;
    public float Normalized => maximumSpark <= 0f ? 0f : currentSpark / maximumSpark;

    private void Awake() => currentSpark = Mathf.Clamp(currentSpark, 0f, maximumSpark);

    public void Drain(float amount, string reason)
    {
        if (amount <= 0f) return;
        currentSpark = Mathf.Max(0f, currentSpark - amount);
        SparkChanged?.Invoke(Normalized, reason);
        Debug.Log($"Danny Spark: {currentSpark:0}/{maximumSpark:0} — {reason}");
    }

    public void Restore(float amount, string reason = "childlike wonder")
    {
        if (amount <= 0f) return;
        currentSpark = Mathf.Min(maximumSpark, currentSpark + amount);
        SparkChanged?.Invoke(Normalized, reason);
    }
}
