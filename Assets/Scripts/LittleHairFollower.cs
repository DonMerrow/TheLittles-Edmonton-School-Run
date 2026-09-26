using UnityEngine;

/// <summary>
/// Keeps the textureless replacement hair aligned to the visible crown without
/// inheriting the unusual retargeted axes of the source UMA Head bone.
/// </summary>
[DisallowMultipleComponent]
public sealed class LittleHairFollower : MonoBehaviour
{
    public Transform Head;
    public Transform Character;
    public float Height = 0.065f;
    public float Forward = 0.008f;

    public void SnapNow()
    {
        if (Head == null || Character == null) return;
        // The player is deliberately scaled to make him one of "the Littles".
        // Preserve a one-to-one local mesh scale instead of inheriting the
        // inverse scale produced by SetParent(worldPositionStays: true).
        transform.localScale = Vector3.one;
        transform.position = Head.position + Character.up * Height +
            Character.forward * Forward;
        transform.rotation = Quaternion.LookRotation(Character.forward, Character.up);
    }

    private void LateUpdate()
    {
        SnapNow();
    }
}
