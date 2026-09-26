using UnityEngine;

public sealed class RiverValleySafeCar : MonoBehaviour
{
    [SerializeField] private Vector3 pointA;
    [SerializeField] private Vector3 pointB;
    [SerializeField] private float speed=5.2f;
    [SerializeField] private AudioClip engineLoop;
    [SerializeField] private AudioClip horn;
    private bool towardB=true;
    private float waitUntil;
    private float nextWarning;
    private RiverValleyGameDirector director;
    private AudioSource engine;
    private int warningIndex;
    private bool levelTwoDanger;
    private bool levelThreeDanger;
    private float nextLevelTwoLaunch;
    private float nextChildSnowbank;
    private float nextCastRefresh;
    private WinterCastIdentity[] visibleCast;
    public static int SafeStops { get; private set; }
    public static int DriverWarnings { get; private set; }

    private static readonly string[] Warnings=
    {
        "Whoa there, kids! Cross at the corner!",
        "Hey! Out of the road, you little maniacs!",
        "Watch it! I almost turned your school run into a snowbank!",
        "Back on the sidewalk, you crazy kids!"
    };

    private void Start()
    {
        director=FindFirstObjectByType<RiverValleyGameDirector>();
        engine=gameObject.AddComponent<AudioSource>(); engine.clip=engineLoop; engine.loop=true;
        engine.playOnAwake=false; engine.spatialBlend=1f; engine.minDistance=3f; engine.maxDistance=48f;
        engine.volume=0.46f; engine.dopplerLevel=0.35f; if(engineLoop!=null)engine.Play();
    }

    private void Update()
    {
        Vector3 destination=towardB?pointB:pointA;
        Vector3 direction=Vector3.ProjectOnPlane(destination-transform.position,Vector3.up).normalized;
        bool blocked=ChildAhead(direction,out bool dannyInRoad);
        float dannyDistance=director!=null&&director.Player!=null
            ? Vector3.ProjectOnPlane(director.Player.position-transform.position,Vector3.up).magnitude
            : float.PositiveInfinity;
        if(dannyInRoad&&dannyDistance<=2.45f&&Time.time>=nextLevelTwoLaunch)
        {
            nextLevelTwoLaunch=Time.time+4.5f;
            director?.LevelTwoHazardLaunch(transform,"DRIVER",
                "The car brakes, but the slippery bumper sends Danny into a soft roadside snow pile. Spark lost!",12f);
            blocked=true;
        }
        if(blocked)
        {
            SafeStops++;
            if(engine!=null)engine.pitch=Mathf.MoveTowards(engine.pitch,0.72f,Time.deltaTime*2f);
            if(Time.time>=nextWarning&&CanNarrateWarning())
            {
                nextWarning=Time.time+5f;
                string line=Warnings[warningIndex++%Warnings.Length];
                director?.Show("DRIVER",line,4f); DriverWarnings++;
                if(horn!=null)AudioSource.PlayClipAtPoint(horn,transform.position,dannyInRoad?0.72f:0.42f);
            }
            return;
        }
        if(Time.time<waitUntil)return;
        if(engine!=null)engine.pitch=Mathf.MoveTowards(engine.pitch,0.96f,Time.deltaTime*1.5f);
        Vector3 before=transform.position;
        transform.position=Vector3.MoveTowards(transform.position,destination,speed*Time.deltaTime);
        Vector3 moved=transform.position-before;
        if(moved.sqrMagnitude>0.001f)transform.rotation=Quaternion.LookRotation(moved.normalized,Vector3.up);
        if((transform.position-destination).sqrMagnitude<0.02f)
        {towardB=!towardB;waitUntil=Time.time+1.2f;}
    }

    private bool ChildAhead(Vector3 direction,out bool dannyInRoad)
    {
        dannyInRoad=false;
        DannySpark danny=FindAnyObjectByType<DannySpark>();
        if(danny!=null&&InSafetyBox(danny.transform.position,direction,8f,2.7f))
        {
            float bumperDistance=Vector3.ProjectOnPlane(danny.transform.position-transform.position,Vector3.up).magnitude;
            if(!levelTwoDanger||bumperDistance<=2.45f)
            {
                dannyInRoad=true;
                return true;
            }
        }
        foreach(RiverValleyKidFollower child in FindObjectsByType<RiverValleyKidFollower>(FindObjectsSortMode.None))
            if(child!=null&&InSafetyBox(child.transform.position,direction,7f,2.8f))
            {
                float bumper=Vector3.ProjectOnPlane(child.transform.position-transform.position,Vector3.up).magnitude;
                if((levelTwoDanger||levelThreeDanger)&&bumper<=2.35f&&Time.time>=nextChildSnowbank&&
                   child.CanTakePlowHit)
                {
                    nextChildSnowbank=Time.time+6f;
                    Vector3 side=Vector3.Cross(Vector3.up,direction).normalized;
                    if(Vector3.Dot(child.transform.position-transform.position,side)<0f)side=-side;
                    director?.FollowerBuriedByCar(child,child.transform.position+side*3.2f);
                }
                return true;
            }
        if(visibleCast==null||Time.time>=nextCastRefresh)
        {
            visibleCast=FindObjectsByType<WinterCastIdentity>(FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            nextCastRefresh=Time.time+1f;
        }
        if(visibleCast!=null)
            foreach(WinterCastIdentity person in visibleCast)
            {
                if(person==null||!person.gameObject.activeInHierarchy||
                   person.GetComponentInParent<RiverValleyKidFollower>()!=null||
                   person.GetComponentInParent<DannySpark>()!=null||
                   person.transform.IsChildOf(transform))continue;
                if(InSafetyBox(person.transform.position,direction,7f,2.8f))return true;
            }
        return false;
    }

    private bool InSafetyBox(Vector3 position,Vector3 forward,float ahead,float side)
    {
        Vector3 delta=Vector3.ProjectOnPlane(position-transform.position,Vector3.up);
        float longitudinal=Vector3.Dot(delta,forward);
        float lateral=Mathf.Abs(Vector3.Dot(delta,Vector3.Cross(Vector3.up,forward)));
        return longitudinal>-1.5f&&longitudinal<ahead&&lateral<side;
    }

    public void StageSafetyTest(Vector3 crossingPosition)
    {
        pointA=crossingPosition-Vector3.right*14f;
        pointB=crossingPosition+Vector3.right*14f;
        towardB=true;
        transform.position=crossingPosition-Vector3.right*4.5f;
        waitUntil=0f;
        nextWarning=0f;
    }

    public void EnableLevelTwoDanger()
    {
        if(levelTwoDanger)return;
        levelTwoDanger=true;
        speed*=1.18f;
    }

    public void EnableLevelThreeDanger()
    {
        if(levelThreeDanger)return;
        levelThreeDanger=true;
        levelTwoDanger=true;
        speed*=1.26f;
    }

    private bool CanNarrateWarning()
    {
        if(director==null||director.IsBusy||director.Player==null)return false;
        if(Vector3.ProjectOnPlane(director.Player.position-transform.position,Vector3.up).magnitude>24f)
            return false;
        Camera gameCamera=Camera.main;
        if(gameCamera==null)return true;
        Vector3 viewport=gameCamera.WorldToViewportPoint(transform.position+Vector3.up*0.6f);
        return viewport.z>0f&&viewport.x>-0.12f&&viewport.x<1.12f&&
            viewport.y>-0.12f&&viewport.y<1.12f;
    }

#if UNITY_EDITOR
    public void Configure(Vector3 a,Vector3 b,float newSpeed,AudioClip engineClip,AudioClip hornClip)
    {pointA=a;pointB=b;speed=newSpeed;engineLoop=engineClip;horn=hornClip;}
#endif
}
