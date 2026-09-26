using UnityEngine;

/// <summary>
/// Receives the footstep events embedded in the shared Starter Assets clips.
/// It belongs beside each Animator because Unity sends animation events to
/// that exact GameObject, not to a controller on a parent group.
/// </summary>
[DisallowMultipleComponent]
public sealed class WinterAnimationEventRelay : MonoBehaviour
{
    public void OnFootstep(AnimationEvent animationEvent) { }
    public void OnLand(AnimationEvent animationEvent) { }
}
