using UnityEngine;

/// <summary>
/// Compatibility cleanup for v13. The external UMA meshes use an older bind
/// space than this generated Moth FBX, so they are intentionally not rendered.
/// </summary>
[DisallowMultipleComponent]
public sealed class LittleUmaWardrobe : MonoBehaviour
{
    private void Start()
    {
        string[] names = { "Moth_Real_Hair", "Moth_Baggy_Shirt", "Moth_Loose_Shorts" };
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
            for (int i = 0; i < names.Length; i++)
                if (child.name == names[i]) Destroy(child.gameObject);
        enabled = false;
    }
}
