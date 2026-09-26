using UnityEngine;

/// <summary>Marker and playtest counter for Danny's stair-rail grab.</summary>
public sealed class RiverValleySwingRing : MonoBehaviour
{
    private static int completedSwings;
    public static int CompletedSwings => completedSwings;

    public void MarkCompleted()
    {
        completedSwings++;
    }
}
