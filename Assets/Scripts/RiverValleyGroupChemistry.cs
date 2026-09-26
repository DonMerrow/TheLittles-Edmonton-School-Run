using System.Collections.Generic;
using UnityEngine;

/// <summary>Small, occasional physical moments that make the school group feel like friends.</summary>
[DefaultExecutionOrder(1200)]
public sealed class RiverValleyGroupChemistry : MonoBehaviour
{
    private sealed class Rig
    {
        public RiverValleyKidFollower Follower;
        public Animator Animator;
        public Transform Root;
        public Transform Head;
        public Transform Spine;
        public Transform LeftUpper;
        public Transform LeftLower;
        public Transform LeftHand;
        public Transform RightUpper;
        public Transform RightLower;
        public Transform RightHand;
    }

    [SerializeField] private Transform player;
    [SerializeField] private RiverValleyGameDirector director;
    private Rig first;
    private Rig second;
    private float momentStarted;
    private float momentUntil;
    private float nextMoment;
    private bool nudge;
    private bool alternate;
    private Vector3 previousPlayerPosition;
    private float playerSpeed;

    public static int HighFives { get; private set; }
    public static int EncouragingNudges { get; private set; }

    private void Start()
    {
        if(player==null)
        {
            DannySpark spark=FindFirstObjectByType<DannySpark>();
            if(spark!=null)player=spark.transform;
        }
        if(director==null)director=FindFirstObjectByType<RiverValleyGameDirector>();
        if(player!=null)previousPlayerPosition=player.position;
        nextMoment=Time.time+11f;
    }

    private void Update()
    {
        if(player!=null)
        {
            playerSpeed=Vector3.ProjectOnPlane(player.position-previousPlayerPosition,Vector3.up).magnitude/
                Mathf.Max(Time.deltaTime,0.0001f);
            previousPlayerPosition=player.position;
        }
        if(Time.time<nextMoment||Time.time<momentUntil||director==null||director.IsBusy)return;
        // A high-five or shoulder nudge is a stopped-group beat. Layering the
        // procedural arm pose over running children caused the fluttering
        // elbows and crouched silhouettes seen in playtesting.
        if(playerSpeed>0.18f)return;
        BeginMoment(false);
    }

    public bool StageHighFiveForPlaytest()
    {
        return BeginMoment(true,false);
    }

    public bool StageNudgeForPlaytest()
    {
        return BeginMoment(false,true);
    }

    private bool BeginMoment(bool forceHighFive,bool forceNudge=false)
    {
        if(player==null||Time.time<momentUntil)return false;
        List<Rig> children=FindNearbyChildRigs();
        if(children.Count==0)
        {
            nextMoment=Time.time+5f;
            return false;
        }

        Rig danny=BuildRig(player.GetComponentInChildren<Animator>(),null);
        bool useDanny=forceHighFive||(!forceNudge&&(children.Count<2||!alternate));
        nudge=!useDanny;
        if(useDanny&&danny!=null)
        {
            Rig closest=children.Find(child=>Vector3.Distance(child.Root.position,danny.Root.position)<2.75f);
            if(closest==null)
            {
                nextMoment=Time.time+2f;
                return false;
            }
            first=danny;
            second=closest;
            HighFives++;
            director?.Show("CHILD","High five, Danny! School squad stays together!",2.4f);
        }
        else if(children.Count>=2)
        {
            first=children[0];
            second=FindClosestPartner(children,first);
            if(second==null)return false;
            EncouragingNudges++;
            director?.Show("CHILD","Come on—little mitten nudge toward school!",2.4f);
        }
        else return false;

        alternate=!alternate;
        momentStarted=Time.time;
        momentUntil=Time.time+(nudge?1.15f:1.05f);
        nextMoment=momentUntil+Random.Range(20f,31f);
        return true;
    }

    private List<Rig> FindNearbyChildRigs()
    {
        List<Rig> result=new();
        foreach(RiverValleyKidFollower follower in FindObjectsByType<RiverValleyKidFollower>(FindObjectsSortMode.None))
        {
            if(follower==null||follower.IsScattered||follower.IsWaiting||follower.IsTumbling||follower.IsMoving||
                Vector3.Distance(follower.transform.position,player.position)>5.4f)continue;
            foreach(Animator animator in follower.GetComponentsInChildren<Animator>())
            {
                Rig rig=BuildRig(animator,follower);
                if(rig!=null)result.Add(rig);
            }
        }
        result.Sort((a,b)=>Vector3.Distance(a.Root.position,player.position)
            .CompareTo(Vector3.Distance(b.Root.position,player.position)));
        return result;
    }

    private static Rig FindClosestPartner(List<Rig> candidates,Rig source)
    {
        Rig best=null;
        float distance=float.PositiveInfinity;
        foreach(Rig candidate in candidates)
        {
            if(candidate==source)continue;
            float current=Vector3.Distance(source.Root.position,candidate.Root.position);
            if(current<distance&&current<2.5f){distance=current;best=candidate;}
        }
        return best;
    }

    private static Rig BuildRig(Animator animator,RiverValleyKidFollower follower)
    {
        if(animator==null||!animator.isHuman)return null;
        Rig rig=new()
        {
            Follower=follower,
            Animator=animator,
            Root=animator.transform,
            Head=animator.GetBoneTransform(HumanBodyBones.Head),
            Spine=animator.GetBoneTransform(HumanBodyBones.Spine),
            LeftUpper=animator.GetBoneTransform(HumanBodyBones.LeftUpperArm),
            LeftLower=animator.GetBoneTransform(HumanBodyBones.LeftLowerArm),
            LeftHand=animator.GetBoneTransform(HumanBodyBones.LeftHand),
            RightUpper=animator.GetBoneTransform(HumanBodyBones.RightUpperArm),
            RightLower=animator.GetBoneTransform(HumanBodyBones.RightLowerArm),
            RightHand=animator.GetBoneTransform(HumanBodyBones.RightHand)
        };
        return rig.LeftHand!=null&&rig.RightHand!=null?rig:null;
    }

    private void LateUpdate()
    {
        if(Time.time>=momentUntil||first==null||second==null||first.Root==null||second.Root==null)return;
        if(Vector3.Distance(first.Root.position,second.Root.position)>3f)
        {
            momentUntil=0f;
            return;
        }
        float strength=Mathf.Sin(Mathf.InverseLerp(momentStarted,momentUntil,Time.time)*Mathf.PI);
        if(nudge)ApplyNudge(first,second,strength);
        else ApplyHighFive(first,second,strength);
    }

    private static void ApplyHighFive(Rig a,Rig b,float strength)
    {
        bool aRight=Vector3.Distance(a.RightHand.position,b.Root.position)<
            Vector3.Distance(a.LeftHand.position,b.Root.position);
        bool bRight=Vector3.Distance(b.RightHand.position,a.Root.position)<
            Vector3.Distance(b.LeftHand.position,a.Root.position);
        Transform aUpper=aRight?a.RightUpper:a.LeftUpper;
        Transform aLower=aRight?a.RightLower:a.LeftLower;
        Transform aHand=aRight?a.RightHand:a.LeftHand;
        Transform bUpper=bRight?b.RightUpper:b.LeftUpper;
        Transform bLower=bRight?b.RightLower:b.LeftLower;
        Transform bHand=bRight?b.RightHand:b.LeftHand;
        if(aUpper==null||aLower==null||bUpper==null||bLower==null)return;
        float headHeight=Mathf.Min(a.Head!=null?a.Head.position.y:a.Root.position.y+1.45f,
            b.Head!=null?b.Head.position.y:b.Root.position.y+1.45f);
        Vector3 meeting=Vector3.Lerp(a.Root.position,b.Root.position,0.5f);
        meeting.y=headHeight-0.10f;
        AimArm(aUpper,aLower,aHand,meeting,54f*strength);
        AimArm(bUpper,bLower,bHand,meeting,54f*strength);
        aHand.position=Vector3.Lerp(aHand.position,meeting,strength);
        bHand.position=Vector3.Lerp(bHand.position,meeting,strength);
    }

    private static void ApplyNudge(Rig source,Rig target,float strength)
    {
        if(target.LeftUpper==null||target.RightUpper==null)return;
        bool useRight=Vector3.Distance(source.RightHand.position,target.Root.position)<
            Vector3.Distance(source.LeftHand.position,target.Root.position);
        Transform upper=useRight?source.RightUpper:source.LeftUpper;
        Transform lower=useRight?source.RightLower:source.LeftLower;
        Transform hand=useRight?source.RightHand:source.LeftHand;
        Transform shoulder=Vector3.Distance(source.Root.position,target.LeftUpper.position)<
            Vector3.Distance(source.Root.position,target.RightUpper.position)?target.LeftUpper:target.RightUpper;
        if(upper==null||lower==null||hand==null||shoulder==null)return;
        Vector3 touch=shoulder.position+Vector3.up*0.03f;
        AimArm(upper,lower,hand,touch,42f*strength);
        hand.position=Vector3.Lerp(hand.position,touch,strength*0.86f);
        if(target.Spine!=null)
            target.Spine.rotation=Quaternion.AngleAxis(5f*strength,target.Spine.right)*target.Spine.rotation;
    }

    private static void AimArm(Transform upper,Transform lower,Transform hand,Vector3 target,float degrees)
    {
        AimJoint(upper,hand,target,degrees*0.52f);
        AimJoint(lower,hand,target,degrees);
        AimJoint(upper,hand,target,degrees*0.28f);
    }

    private static void AimJoint(Transform joint,Transform hand,Vector3 target,float maximumDegrees)
    {
        Vector3 current=hand.position-joint.position;
        Vector3 wanted=target-joint.position;
        if(current.sqrMagnitude<0.00001f||wanted.sqrMagnitude<0.00001f)return;
        Quaternion delta=Quaternion.FromToRotation(current,wanted);
        delta.ToAngleAxis(out float angle,out Vector3 axis);
        if(angle>180f)angle-=360f;
        if(Mathf.Abs(angle)>maximumDegrees)
            delta=Quaternion.AngleAxis(Mathf.Sign(angle)*maximumDegrees,axis);
        joint.rotation=delta*joint.rotation;
    }

#if UNITY_EDITOR
    public void Configure(Transform newPlayer,RiverValleyGameDirector newDirector)
    {
        player=newPlayer;
        director=newDirector;
    }
#endif
}
