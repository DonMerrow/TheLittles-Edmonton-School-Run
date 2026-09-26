using UnityEngine;

/// <summary>A short sidewalk patrol plus infrequent age-appropriate chatter.</summary>
[DisallowMultipleComponent]
public sealed class RiverValleyNeighbourResident : MonoBehaviour
{
    private static readonly int SpeedHash=Animator.StringToHash("Speed");
    private static readonly string[] KidLines=
    {
        "We traded hockey cards. Mine has the goalie with the giant pads.",
        "The snow fort needs one more tunnel after school.",
        "I brought my fastest sled, even though this is definitely a school day.",
        "Do you think the rabbits know when school starts?",
        "My toque keeps sliding over my eyes.",
        "Race you to the corner—but we stop at the curb."
    };
    private static readonly string[] AdultLines=
    {
        "Morning, Danny. The plow cleared this block before breakfast.",
        "Stay on the sidewalk—the side streets are slippery.",
        "The river-valley stairs will be icy today.",
        "Everyone on this block is keeping an eye on the storm.",
        "The bus is running slowly in this snow.",
        "Looks like the whole neighbourhood is awake early."
    };

    private static float nextNeighbourhoodLine;
    private Vector3 pointA;
    private Vector3 pointB;
    private Vector3 target;
    private bool child;
    private int lineIndex;
    private float speed;
    private float pauseUntil;
    private float nextPersonalLine;
    private Animator[] animators;
    private Transform danny;
    private RiverValleyGameDirector director;
    private WinterAudioDirector audioDirector;
    private float visualFootOffset;
    private bool greetedDanny;
    private readonly RaycastHit[] groundHits=new RaycastHit[12];

    public void Configure(Vector3 a,Vector3 b,bool isChild,int chatterIndex)
    {
        pointA=a;
        pointB=b;
        child=isChild;
        lineIndex=chatterIndex;
        target=b;
        speed=isChild?0.72f:0.58f;
    }

    private void Start()
    {
        animators=GetComponentsInChildren<Animator>(true);
        foreach(Animator animator in animators)
            if(animator!=null)animator.applyRootMotion=false;
        DannySpark player=FindFirstObjectByType<DannySpark>();
        danny=player!=null?player.transform:null;
        director=FindFirstObjectByType<RiverValleyGameDirector>();
        audioDirector=FindFirstObjectByType<WinterAudioDirector>();
        Renderer[] renderers=GetComponentsInChildren<Renderer>(true);
        float foot=float.PositiveInfinity;
        foreach(Renderer renderer in renderers)
            if(renderer!=null&&renderer.enabled)foot=Mathf.Min(foot,renderer.bounds.min.y);
        visualFootOffset=foot<float.PositiveInfinity?foot-transform.position.y:0f;
        SnapToGround();
        pointA.y=pointB.y=target.y=transform.position.y;
        nextPersonalLine=Time.time+1.5f+(lineIndex%3)*0.75f;
        if((pointB-pointA).sqrMagnitude<0.2f)FaceRoad();
    }

    private void Update()
    {
        UpdateWalk();
        UpdateChatter();
    }

    private void UpdateWalk()
    {
        if((pointB-pointA).sqrMagnitude<0.2f||Time.time<pauseUntil)
        {
            SetAnimation(0f);
            return;
        }
        Vector3 flat=Vector3.ProjectOnPlane(target-transform.position,Vector3.up);
        if(flat.magnitude<0.14f)
        {
            target=(target-pointA).sqrMagnitude<(target-pointB).sqrMagnitude?pointB:pointA;
            pauseUntil=Time.time+1.2f+(lineIndex%3)*0.45f;
            SetAnimation(0f);
            return;
        }
        Vector3 direction=flat.normalized;
        transform.position+=direction*speed*Time.deltaTime;
        SnapToGround();
        transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction,Vector3.up),
            1f-Mathf.Exp(-5f*Time.deltaTime));
        SetAnimation(child?2.35f:2.05f);
    }

    private void UpdateChatter()
    {
        if(danny==null||director==null||Time.time<nextPersonalLine||
            Time.time<nextNeighbourhoodLine||director.Progress>0.24f)return;
        float distance=Vector3.ProjectOnPlane(danny.position-transform.position,Vector3.up).magnitude;
        if(distance>7.5f)return;
        string speaker=child?"NEIGHBOURHOOD KID":"NEIGHBOUR";
        string line;
        if(!greetedDanny)line=child?"Hi, Danny!":"Morning!";
        else
        {
            string[] lines=child?KidLines:AdultLines;
            line=lines[Mathf.Abs(lineIndex)%lines.Length];
        }
        // The opening child gets one unmistakable spoken greeting as Danny
        // passes. The long intro otherwise occupies the subtitle panel until
        // he has already walked beyond every child on his home block.
        if(child&&!greetedDanny&&Time.time>3.2f&&Time.time<12f&&distance<6.5f&&!director.IsBusy)
        {
            director.Show(speaker,line,2.6f);
            greetedDanny=true;
            nextNeighbourhoodLine=Time.time+9.5f;
            nextPersonalLine=Time.time+24f;
            pauseUntil=Time.time+2f;
            Vector3 toward=Vector3.ProjectOnPlane(danny.position-transform.position,Vector3.up);
            if(toward.sqrMagnitude>0.02f)
                transform.rotation=Quaternion.LookRotation(toward.normalized,Vector3.up);
            return;
        }
        if(!director.TryShowAmbient(speaker,line,3.5f))return;
        if(!greetedDanny)
        {
            audioDirector?.SpeakGreeting(speaker,line);
            greetedDanny=true;
        }
        nextNeighbourhoodLine=Time.time+9.5f;
        nextPersonalLine=Time.time+24f;
        Vector3 look=Vector3.ProjectOnPlane(danny.position-transform.position,Vector3.up);
        if(look.sqrMagnitude>0.02f)transform.rotation=Quaternion.LookRotation(look.normalized,Vector3.up);
        pauseUntil=Time.time+2f;
    }

    private void SnapToGround()
    {
        Vector3 position=transform.position;
        int count=Physics.RaycastNonAlloc(position+Vector3.up*4f,Vector3.down,groundHits,12f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        float best=float.PositiveInfinity;
        for(int i=0;i<count;i++)
        {
            RaycastHit hit=groundHits[i];
            if(hit.collider==null||hit.collider.transform.IsChildOf(transform)||
                hit.normal.y<0.55f||
                hit.collider.GetComponentInParent<RiverValleyHazardMover>()!=null||
                hit.collider.name.ToLowerInvariant().Contains("home")||
                hit.collider.name.ToLowerInvariant().Contains("building"))continue;
            if(hit.distance>=best)continue;
            best=hit.distance;
            position.y=hit.point.y-visualFootOffset+0.015f;
        }
        if(best<float.PositiveInfinity)transform.position=position;
    }

    private void FaceRoad()
    {
        Vector3 road=new Vector3(transform.position.x,transform.position.y,2.4f)-transform.position;
        if(road.sqrMagnitude>0.02f)transform.rotation=Quaternion.LookRotation(road.normalized,Vector3.up);
    }

    private void SetAnimation(float value)
    {
        if(animators==null)return;
        foreach(Animator animator in animators)
            if(animator!=null)animator.SetFloat(SpeedHash,value,0.12f,Time.deltaTime);
    }
}
