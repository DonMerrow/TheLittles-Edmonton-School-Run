using UnityEngine;
using UnityEngine.InputSystem;

public enum RiverEncounterKind
{
    PositiveAdult,
    PetDog,
    PetRabbit,
    LostKid,
    ParentHandoff,
    PlayfulKid,
    SkateDare,
    CoyoteScare,
    GiveLunch,
    HelperRescue,
    HelpfulTeen,
    TalkativeAdult,
    MomCall,
    MomEvasion,
    StormWarning,
    TeenAdvice,
    CrowEcho,
    Finish
}

[RequireComponent(typeof(Collider))]
public sealed class RiverValleyEncounter : MonoBehaviour
{
    [SerializeField] private RiverEncounterKind kind;
    [SerializeField] private string speaker;
    [SerializeField, TextArea] private string line;
    [SerializeField] private string prompt = "Talk";
    [SerializeField] private float sparkChange = 8f;
    [SerializeField] private float minimumSpark;
    [SerializeField] private bool requiresInteraction = true;
    [SerializeField] private Transform actor;
    [SerializeField] private Transform railStart;
    [SerializeField] private Transform railEnd;
    [SerializeField] private int groupSize = 1;
    private RiverValleyGameDirector director;
    private bool used;
    private bool dannyInside;
    private bool interactionHeld;

    public RiverEncounterKind Kind => kind;
    public string Speaker => speaker;
    public string Line => line;
    public string Prompt => prompt;
    public float SparkChange => sparkChange;
    public float MinimumSpark => minimumSpark;
    public Transform Actor => actor;
    public Transform RailStart => railStart;
    public Transform RailEnd => railEnd;
    public int GroupSize => Mathf.Max(1, groupSize);
    public bool Used { get => used; set => used = value; }

    private void Awake() => director = FindFirstObjectByType<RiverValleyGameDirector>();

    private void Update()
    {
        if (used || !requiresInteraction || !dannyInside || director == null)
        {
            interactionHeld = false;
            return;
        }

        // Read interaction during the rendered frame. OnTriggerStay runs on
        // physics ticks and could miss a quick E press even while its prompt
        // was visible, which was especially noticeable at the principal.
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        bool pressed = (keyboard != null && keyboard.eKey.isPressed) ||
            (gamepad != null && gamepad.buttonWest.isPressed) || RiverValleyMobileControls.ActionHeld;
        if (pressed && !interactionHeld)
            director.Resolve(this);
        interactionHeld = pressed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsDanny(other)) dannyInside = true;
        RiverValleyKidFollower returning = actor != null
            ? actor.GetComponent<RiverValleyKidFollower>() : null;
        // Once a recruited child has become a straggler, Danny only needs to
        // come back for them. They step into formation automatically instead
        // of standing beside a prompt and appearing abandoned.
        if (!used && returning != null && returning.IsScattered && IsDanny(other))
        {
            director?.Resolve(this);
            return;
        }
        if (!requiresInteraction && IsDanny(other))
        {
            director?.Resolve(this);
            return;
        }
        // Recruited children can collect another waiting cluster by walking
        // into it. The whole line bunches up, calls them over, and grows.
        RiverValleyKidFollower helper = other.GetComponentInParent<RiverValleyKidFollower>();
        if (!used && helper != null && !helper.IsScattered && !helper.IsWaiting &&
            (kind == RiverEncounterKind.LostKid || kind == RiverEncounterKind.ParentHandoff))
            director?.Resolve(this);
    }

    private void OnTriggerStay(Collider other)
    {
        if (used || !requiresInteraction || !IsDanny(other) || director == null) return;
        dannyInside = true;
        director.SetPrompt($"E — {prompt}", this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsDanny(other)) return;
        dannyInside = false;
        interactionHeld = false;
        director?.ClearPrompt(this);
    }

    private static bool IsDanny(Collider other) => other.GetComponentInParent<DannySpark>() != null;

    public void PrepareRegather(Vector3 position, float requiredSpark)
    {
        used = false;
        minimumSpark = requiredSpark;
        prompt = "Bring this child back into the group";
        transform.position = position + Vector3.up;
        Collider trigger = GetComponent<Collider>();
        if (trigger != null) trigger.enabled = true;
    }

    public void PrepareLaterLevelSearch(Vector3 position,float requiredSpark)
    {
        used=false;
        minimumSpark=requiredSpark;
        prompt="Call the missing children into Danny's group";
        transform.position=position+Vector3.up;
        Collider trigger=GetComponent<Collider>();
        if(trigger!=null)trigger.enabled=true;
        if(actor==null)return;
        actor.gameObject.SetActive(true);
        RiverValleyKidFollower follower=actor.GetComponent<RiverValleyKidFollower>();
        if(follower!=null)follower.ResetAsLostAt(position,Quaternion.LookRotation(Vector3.back,Vector3.up));
        else actor.SetPositionAndRotation(position,Quaternion.LookRotation(Vector3.back,Vector3.up));
    }

#if UNITY_EDITOR
    public void Configure(RiverEncounterKind newKind, string newSpeaker, string newLine,
        string newPrompt, float newSparkChange, float newMinimumSpark, bool newRequiresInteraction,
        Transform newActor = null, Transform newRailStart = null, Transform newRailEnd = null)
    {
        kind = newKind;
        speaker = newSpeaker;
        line = newLine;
        prompt = newPrompt;
        sparkChange = newSparkChange;
        minimumSpark = newMinimumSpark;
        requiresInteraction = newRequiresInteraction;
        actor = newActor;
        railStart = newRailStart;
        railEnd = newRailEnd;
        groupSize = 1;
    }

    public void SetGroupSize(int size) => groupSize = Mathf.Max(1, size);
#endif
}
