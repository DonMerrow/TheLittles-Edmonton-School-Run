using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// Plays an animation stored inside one of the Quaternius animal FBX files
/// without requiring a separate Animator Controller asset for every animal.
/// </summary>
[DisallowMultipleComponent]
public sealed class QuaterniusAnimalAnimator : MonoBehaviour
{
    [SerializeField] private string resourcePath;
    [SerializeField] private string preferredClip = "Walk";

    private PlayableGraph graph;
    private AnimationClipPlayable playable;
    private AnimationPlayableOutput output;
    private AnimationClip restingClip;
    private AnimationClip hopClip;
    private float hopEndsAt;
    private bool hopping;
    private float clipLength;
    private Transform[] observedBones;
    private Quaternion[] startingRotations;
    private float animationStarted;
    private bool poseVerified;
    private float motionAmount = 1f;

    public static int RunningCount { get; private set; }
    public static int MissingClipCount { get; private set; }
    public static int PoseVerifiedCount { get; private set; }
    public static int RabbitJumpsStarted { get; private set; }

    public void Configure(string path, string clipName)
    {
        resourcePath = path;
        preferredClip = clipName;
    }

    private void Start()
    {
        Animator animator = GetComponentInChildren<Animator>(true);
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>(resourcePath)
            .Where(clip => clip != null && !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        AnimationClip clip = Resources.Load<AnimationClip>(resourcePath + "_" + preferredClip) ??
            FindClip(clips, preferredClip) ?? FindClip(clips, "Walk") ??
            FindClip(clips, "Idle") ?? clips.FirstOrDefault();
        restingClip=clip;
        if(string.Equals(preferredClip,"Sitting",StringComparison.OrdinalIgnoreCase))
        {
            hopClip=FindClip(clips,"Jump");
        }

        if (clip == null)
        {
            MissingClipCount++;
            Debug.LogWarning($"Animated animal '{name}' could not load the {preferredClip} clip from Resources/{resourcePath}.", this);
            return;
        }

        // Blender's generic animal rigs do not always receive an Animator
        // component from Unity's model importer. An Animator on the imported
        // model root drives all of the clip's child-bone paths correctly.
        if (animator == null) animator = gameObject.AddComponent<Animator>();

        animator.applyRootMotion = false;
        graph = PlayableGraph.Create($"{name} — {clip.name}");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        playable = AnimationClipPlayable.Create(graph, clip);
        playable.SetApplyFootIK(false);
        playable.SetApplyPlayableIK(false);
        playable.SetDuration(double.MaxValue);
        clipLength = Mathf.Max(0.01f, clip.length);
        output = AnimationPlayableOutput.Create(graph, "Animal animation", animator);
        output.SetSourcePlayable(playable);
        graph.Play();
        observedBones = GetComponentsInChildren<Transform>(true).Where(part => part != transform).ToArray();
        startingRotations = observedBones.Select(part => part.localRotation).ToArray();
        animationStarted = Time.time;
        RunningCount++;
    }

    private void Update()
    {
        if (!graph.IsValid() || !playable.IsValid() || clipLength <= 0f) return;
        if(hopping&&Time.time>=hopEndsAt)
        {
            SwitchClip(restingClip);
            hopping=false;
        }
        // A sitting rabbit's long idle clip includes its subtle ear and nose
        // movement; playing it at twelve percent made it look frozen.
        playable.SetSpeed(hopClip!=null?1f:Mathf.Lerp(0.12f,1f,motionAmount));
        double time = playable.GetTime();
        if (time >= clipLength)
        {
            playable.SetTime(time % clipLength);
            playable.SetDone(false);
        }
        if (!poseVerified && Time.time - animationStarted >= 0.25f)
        {
            for (int i = 0; i < observedBones.Length; i++)
            {
                if (observedBones[i] == null || Quaternion.Angle(startingRotations[i], observedBones[i].localRotation) < 0.2f) continue;
                poseVerified = true;
                PoseVerifiedCount++;
                break;
            }
        }
    }

    public void SetMotionAmount(float amount)
    {
        motionAmount=Mathf.Clamp01(amount);
    }

    public float PlayRabbitJumpNow()
    {
        if(hopClip==null||!graph.IsValid())return 0.62f;
        SwitchClip(hopClip);
        hopping=true;
        RabbitJumpsStarted++;
        hopEndsAt=Time.time+Mathf.Max(0.42f,hopClip.length);
        return Mathf.Max(0.42f,hopClip.length);
    }

    private void SwitchClip(AnimationClip clip)
    {
        if(clip==null||!graph.IsValid())return;
        if(playable.IsValid())graph.DestroyPlayable(playable);
        playable=AnimationClipPlayable.Create(graph,clip);
        playable.SetApplyFootIK(false);
        playable.SetApplyPlayableIK(false);
        playable.SetDuration(double.MaxValue);
        clipLength=Mathf.Max(0.01f,clip.length);
        output.SetSourcePlayable(playable);
        playable.SetTime(0d);
    }

    private static AnimationClip FindClip(AnimationClip[] clips, string wanted)
    {
        if (clips == null || string.IsNullOrWhiteSpace(wanted)) return null;
        return clips.FirstOrDefault(clip => string.Equals(clip.name, wanted, StringComparison.OrdinalIgnoreCase)) ??
            clips.FirstOrDefault(clip => clip.name.EndsWith("|" + wanted, StringComparison.OrdinalIgnoreCase) ||
                clip.name.EndsWith("_" + wanted, StringComparison.OrdinalIgnoreCase) ||
                clip.name.Contains("|"+wanted+".",StringComparison.OrdinalIgnoreCase));
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
