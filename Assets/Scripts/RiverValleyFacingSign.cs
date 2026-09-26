using UnityEngine;

/// <summary>Keeps route and school signs readable from the active play camera.</summary>
[DisallowMultipleComponent]
public sealed class RiverValleyFacingSign : MonoBehaviour
{
    private Camera view;

    private void LateUpdate()
    {
        if (view == null) view = Camera.main;
        if (view == null) return;
        Vector3 towardViewer = Vector3.ProjectOnPlane(view.transform.position - transform.position, Vector3.up);
        if (towardViewer.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.LookRotation(towardViewer.normalized, Vector3.up);
    }
}
