using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public sealed class RiverValleyWaitPost : MonoBehaviour
{
    [SerializeField] private string placeName = "the warm streetlight";
    private RiverValleyGameDirector director;
    private bool inside;
    private Transform player;

    private void Awake() => director = FindFirstObjectByType<RiverValleyGameDirector>();

    private void Update()
    {
        if (!inside || director == null) return;
        if(player==null||Vector3.Distance(player.position,transform.position)>5.5f)
        {
            inside=false;
            director.ClearGroupPrompt(transform);
            return;
        }
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        if ((keyboard != null && keyboard.qKey.wasPressedThisFrame) ||
            (gamepad != null && gamepad.leftShoulder.wasPressedThisFrame) ||
            RiverValleyMobileControls.SecondaryPressed)
            director.ToggleGroupWait(transform, placeName);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsDanny(other)) return;
        inside = true;
        player=other.GetComponentInParent<DannySpark>().transform;
        director?.SetGroupPrompt(transform,$"Q — Ask the children to wait at {placeName}");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsDanny(other)) return;
        inside = false;
        player=null;
        director?.ClearGroupPrompt(transform);
    }

    private void OnDisable()=>director?.ClearGroupPrompt(transform);

    private static bool IsDanny(Collider other) => other.GetComponentInParent<DannySpark>() != null;

#if UNITY_EDITOR
    public void Configure(string newPlaceName) => placeName = newPlaceName;
#endif
}
