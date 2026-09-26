using UnityEngine;

/// <summary>
/// Story metadata shared by Mom and the Alberta winter neighbourhood cast.
/// The later interaction system can read this without hard-coding character
/// names into Danny's movement or Spark mechanics.
/// </summary>
public sealed class WinterCastIdentity : MonoBehaviour
{
    [SerializeField] private string displayName;
    [SerializeField] private string storyRole;
    [SerializeField, Range(0f, 100f)] private float sparkDrain;
    [SerializeField] private bool friendlyDistraction = true;
    [SerializeField, TextArea] private string interactionPrompt;

    public string DisplayName => displayName;
    public string StoryRole => storyRole;
    public float SparkDrain => sparkDrain;
    public bool FriendlyDistraction => friendlyDistraction;
    public string InteractionPrompt => interactionPrompt;

#if UNITY_EDITOR
    public void Configure(string newDisplayName, string newStoryRole,
        float newSparkDrain, bool newFriendlyDistraction, string newPrompt)
    {
        displayName = newDisplayName;
        storyRole = newStoryRole;
        sparkDrain = Mathf.Clamp(newSparkDrain, 0f, 100f);
        friendlyDistraction = newFriendlyDistraction;
        interactionPrompt = newPrompt;
    }
#endif
}
