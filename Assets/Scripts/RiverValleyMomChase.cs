using UnityEngine;

public sealed class RiverValleyMomChase : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private RiverValleyGameDirector director;
    [SerializeField] private RiverValleyWhiteout whiteout;
    [SerializeField] private float clearWeatherSpeed = 0.82f;
    [SerializeField] private float stormSpeed = 1.22f;
    [SerializeField] private float catchDistance = 1.18f;
    private Animator animator;
    private DannyTestController movement;
    private float pausedUntil;
    private float dawdleSeconds;
    private Vector3 chaseVelocity;
    private RiverValleySlipPatch[] icePatches;
    private float nextCallout;
    private bool aheadAuthorized;
    private bool walkingToCutoff;
    private Vector3 cutoffDestination;
    private float headStartGap;
    private float catchDisabledUntil;
    private bool waitingForWolfParkClue;
    private bool levelThreeChase;
    private bool levelThreeVoiceOnly;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private bool openingHomeConfigured;
    private bool openingHomeEmerged;
    private bool openingHomeGoodbye;
    private Vector3 openingPorchExit;
    private float openingHomeChaseAt;
    private readonly RaycastHit[] groundHits = new RaycastHit[16];
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    public bool WaitingForWolfParkClue => waitingForWolfParkClue;
    public bool LevelThreeVoiceOnly => levelThreeVoiceOnly;
    public float DistanceFromHome => Vector3.Distance(transform.position,homePosition);

    private void Start()
    {
        homePosition=transform.position;
        homeRotation=transform.rotation;
        animator = GetComponent<Animator>();
        if (player != null) movement = player.GetComponent<DannyTestController>();
        icePatches = FindObjectsByType<RiverValleySlipPatch>(FindObjectsSortMode.None);
        nextCallout=Time.time+5.5f;
    }

    private void Update()
    {
        if(openingHomeConfigured&&!openingHomeGoodbye&&
            (director==null||(!director.LevelTwoActive&&!director.LevelThreeActive)))
        {
            UpdateOpeningHome();
            return;
        }
        // Story scenes own Mom's movement and dialogue. Her normal chase
        // must not cut across the school procession or interrupt her proud
        // recorded line at the final doorway.
        if(director!=null&&director.IsBusy)
        {
            chaseVelocity=Vector3.zero;
            animator?.SetFloat(SpeedHash,0f);
            return;
        }
        if(waitingForWolfParkClue)
        {
            chaseVelocity=Vector3.zero;
            animator?.SetFloat(SpeedHash,0f);
            if(Time.time>=nextCallout)
            {
                nextCallout=Time.time+Random.Range(8f,12f);
                director?.MomCallout();
            }
            return;
        }
        // During the final search Mom stays at home. Her comic concern is
        // heard as a distant voice, but her body cannot chase, collide with,
        // catch, or accidentally launch Danny into a rescue sequence.
        if(levelThreeVoiceOnly)
        {
            if(Vector3.Distance(transform.position,homePosition)>0.02f)
                transform.SetPositionAndRotation(homePosition,homeRotation);
            chaseVelocity=Vector3.zero;
            animator?.SetFloat(SpeedHash,0f);
            if(animator!=null)animator.speed=1f;
            if(Time.time>=nextCallout)
            {
                nextCallout=Time.time+Random.Range(8.5f,12.5f);
                director?.MomCallout();
            }
            return;
        }
        if (player == null) { animator?.SetFloat(SpeedHash,0f); return; }
        Vector3 offset = player.position - transform.position;
        Vector3 flat = Vector3.ProjectOnPlane(offset, Vector3.up);
        if(Time.time>=nextCallout)
        {
            // Mom's running commentary is the game's comic spine. Danny can
            // cross the neighbourhood or earn a head start, but he cannot get
            // beyond the reach of an embarrassing maternal reminder.
            nextCallout=Time.time+Random.Range(7.5f,11.5f);
            director?.MomCallout();
        }
        if(Time.time<pausedUntil) { animator?.SetFloat(SpeedHash,0f); return; }
        if (!walkingToCutoff && Time.time>=catchDisabledUntil && flat.magnitude < catchDistance)
        { director?.MomCaught(); GiveHeadStart(8f, 28f); return; }
        bool dawdling = movement != null && movement.CurrentPlanarSpeed < 0.22f;
        dawdleSeconds = Mathf.Clamp(dawdleSeconds + Time.deltaTime * (dawdling ? 1f : -1.8f), 0f, 18f);
        float pressure = Mathf.InverseLerp(3f, 18f, dawdleSeconds);
        float speed = Mathf.Lerp(clearWeatherSpeed, stormSpeed, whiteout != null ? whiteout.Intensity : 0f);
        speed *= Mathf.Lerp(1f, 1.58f, pressure);
        if(levelThreeChase)speed*=1.28f;
        bool skatingOnIce = false;
        if (icePatches != null)
            foreach (RiverValleySlipPatch patch in icePatches)
                if (patch != null && Vector3.ProjectOnPlane(patch.transform.position - transform.position, Vector3.up).sqrMagnitude < 10f)
                { skatingOnIce = true; break; }
        if (skatingOnIce) speed *= 1.22f;

        Vector3 chaseTarget=player.position;
        if(walkingToCutoff)
        {
            chaseTarget=cutoffDestination;
            speed=Mathf.Max(speed,2.35f);
            if(Vector3.ProjectOnPlane(cutoffDestination-transform.position,Vector3.up).sqrMagnitude<0.55f)
            {
                walkingToCutoff=false;
                chaseTarget=player.position;
            }
        }
        else if(!aheadAuthorized)
        {
            // On the honest school route Mom follows the same two street legs
            // as Danny. She can close the gap when he dawdles, but she may not
            // cut a diagonal corner or overtake him. Only a completed side
            // adventure authorizes her comic ambush farther up the route.
            float playerProgress=RouteProgress(player.position);
            headStartGap=Mathf.MoveTowards(headStartGap,0f,Time.deltaTime*0.32f);
            float desiredGap=Mathf.Lerp(9.5f,2.8f,pressure)+headStartGap;
            chaseTarget=RoutePoint(playerProgress-desiredGap,transform.position.y);
        }

        Vector3 targetFlat=Vector3.ProjectOnPlane(chaseTarget-transform.position,Vector3.up);
        Vector3 desiredVelocity = targetFlat.sqrMagnitude>0.002f ? targetFlat.normalized * speed : Vector3.zero;
        float steering = skatingOnIce ? 1.35f : 2.55f;
        chaseVelocity = Vector3.Lerp(chaseVelocity, desiredVelocity,
            1f - Mathf.Exp(-steering * Time.deltaTime));
        Vector3 next = transform.position + chaseVelocity * Time.deltaTime;
        if(!aheadAuthorized&&!walkingToCutoff)
        {
            float maximumProgress=RouteProgress(player.position)-0.18f;
            if(RouteProgress(next)>maximumProgress)
            {
                next=RoutePoint(maximumProgress,next.y);
                chaseVelocity=Vector3.zero;
            }
        }
        if (TryFindGround(next,out RaycastHit hit)) next.y = hit.point.y;
        transform.position = next;
        if (chaseVelocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(chaseVelocity.normalized,Vector3.up), Time.deltaTime*3f);
        if(animator!=null)
        {
            float gait=Mathf.Lerp(2.18f,4.85f,Mathf.InverseLerp(0.55f,2.8f,chaseVelocity.magnitude));
            animator.SetFloat(SpeedHash,gait,0.09f,Time.deltaTime);
            animator.speed=Mathf.Lerp(0.98f,1.22f,Mathf.InverseLerp(0.55f,2.8f,chaseVelocity.magnitude));
        }
    }

    public void ConfigureOpeningHome(Vector3 doorstep,Vector3 porchExit)
    {
        openingHomeConfigured=true;
        openingPorchExit=porchExit;
        openingHomeChaseAt=Time.time+18f;
        transform.SetPositionAndRotation(doorstep,Quaternion.LookRotation(Vector3.forward));
        homePosition=doorstep;
        homeRotation=transform.rotation;
        chaseVelocity=Vector3.zero;
    }

    private void UpdateOpeningHome()
    {
        chaseVelocity=Vector3.zero;
        if(player==null||player.position.x<-57f||Time.time<openingHomeChaseAt-12f)
        {
            animator?.SetFloat(SpeedHash,0f);
            return;
        }
        if(!openingHomeEmerged)
        {
            Vector3 next=Vector3.MoveTowards(transform.position,openingPorchExit,Time.deltaTime*1.65f);
            if(TryFindGround(next,out RaycastHit ground))next.y=ground.point.y;
            transform.position=next;
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(Vector3.forward),
                Time.deltaTime*4f);
            animator?.SetFloat(SpeedHash,2.1f);
            if(Vector3.Distance(transform.position,openingPorchExit)<0.3f)
            {
                openingHomeEmerged=true;
                animator?.SetFloat(SpeedHash,0f);
                // Start the running joke immediately. The first thing Danny
                // hears after leaving home is one of Mom's lovingly mortifying
                // reminders, not a generic goodbye.
                director?.MomCallout();
            }
            return;
        }
        animator?.SetFloat(SpeedHash,0f);
        Vector3 look=Vector3.ProjectOnPlane(player.position-transform.position,Vector3.up);
        if(look.sqrMagnitude>0.02f)
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(look.normalized),Time.deltaTime*2f);
        if(Time.time<openingHomeChaseAt||
            (player.position.x<-25f&&player.position.z<20f))return;
        openingHomeGoodbye=true;
        pausedUntil=Time.time+4f;
        catchDisabledUntil=Time.time+10f;
        director?.MomCallout();
        nextCallout=Time.time+Random.Range(6.5f,8.5f);
        headStartGap=10f;
    }

    public void GiveHeadStart(float seconds, float distance)
    {
        pausedUntil = Time.time + seconds;
        catchDisabledUntil=pausedUntil+5f;
        // A head start is now exactly that: Mom waits, then walks. Relocating
        // her while off camera was the source of the remaining visible warps.
        aheadAuthorized=false;
        walkingToCutoff=false;
        chaseVelocity = Vector3.zero;
        headStartGap=Mathf.Max(headStartGap,Mathf.Clamp(distance*0.24f,4f,11f));
    }

    public void WaitForLevelTwoWolfParkClue()
    {
        waitingForWolfParkClue=true;
        walkingToCutoff=false;
        aheadAuthorized=false;
        chaseVelocity=Vector3.zero;
        transform.SetPositionAndRotation(homePosition,homeRotation);
        animator?.SetFloat(SpeedHash,0f);
    }

    public void ReturnHomeAfterWolfRescue()
    {
        // Once the rescue shot ends Mom leaves the play space instead of
        // becoming a motionless road obstacle. Her normal chase resumes from
        // home after a generous pause, now that the children's screams have
        // switched her famous "Mom radar" on.
        transform.SetPositionAndRotation(homePosition,homeRotation);
        waitingForWolfParkClue=false;
        walkingToCutoff=false;
        aheadAuthorized=false;
        chaseVelocity=Vector3.zero;
        pausedUntil=Time.time+9f;
        catchDisabledUntil=Time.time+14f;
        headStartGap=9f;
        // Keep Mom present in the comedy after she leaves the rescue shot.
        // Her next embarrassing reminder should arrive while the player still
        // remembers the wolf lecture, not after the scene has gone quiet.
        nextCallout=Time.time+4.5f;
        animator?.SetFloat(SpeedHash,0f);
    }

    public void NoticeLevelTwoWolfParkEscape()
    {
        if(!waitingForWolfParkClue)return;
        waitingForWolfParkClue=false;
        pausedUntil=Time.time+1.2f;
        catchDisabledUntil=Time.time+7f;
        nextCallout=Time.time+8f;
        chaseVelocity=Vector3.zero;
        director?.Show("MOTHER","That is a whole bunch of children running out of Wolf Park. Danny? Danny Boy, are you actually in school?",7f);
    }

    public void BeginLevelThree()
    {
        levelThreeChase=false;
        levelThreeVoiceOnly=true;
        waitingForWolfParkClue=false;
        aheadAuthorized=false;
        walkingToCutoff=false;
        chaseVelocity=Vector3.zero;
        transform.SetPositionAndRotation(homePosition,homeRotation);
        pausedUntil=float.PositiveInfinity;
        catchDisabledUntil=float.PositiveInfinity;
        headStartGap=0f;
        nextCallout=Time.time+7f;
        animator?.SetFloat(SpeedHash,0f);
    }

    public void CutOffSchoolRoute(float distanceAhead)
    {
        if(player==null)return;
        aheadAuthorized=true;
        cutoffDestination=RoutePoint(RouteProgress(player.position)+Mathf.Max(6f,distanceAhead),transform.position.y);
        if(TryFindGround(cutoffDestination,out RaycastHit hit))cutoffDestination.y=hit.point.y;
        walkingToCutoff=true;
        chaseVelocity=Vector3.zero;
        pausedUntil=Time.time+0.45f;
    }

    private static float RouteProgress(Vector3 position)
    {
        // Danny begins west of the old street and walks east to the corner,
        // then turns left/north toward the river-valley stairs and school.
        if(position.z<1.5f&&position.x<2.5f)return position.x+66f;
        return 66f+Mathf.Max(0f,position.z+5f);
    }

    private static Vector3 RoutePoint(float progress,float y)
    {
        if(progress<=66f)return new Vector3(-66f+progress,y,-5f);
        return new Vector3(0f,y,-5f+(progress-66f));
    }

    public void ArriveForRiverRescue(Vector3 position, float pauseSeconds)
    {
        if(TryFindGround(position,out RaycastHit hit))position.y=hit.point.y;
        transform.position=position;
        Vector3 towardPlayer=player!=null
            ? Vector3.ProjectOnPlane(player.position-position,Vector3.up) : Vector3.forward;
        if(towardPlayer.sqrMagnitude>0.001f)
            transform.rotation=Quaternion.LookRotation(towardPlayer.normalized,Vector3.up);
        chaseVelocity=Vector3.zero;
        pausedUntil=Time.time+Mathf.Max(0f,pauseSeconds);
        animator?.SetFloat(SpeedHash,0f);
    }

    private bool TryFindGround(Vector3 position,out RaycastHit bestHit)
    {
        bestHit=default;
        float bestDistance=float.PositiveInfinity;
        int count=Physics.RaycastNonAlloc(position+Vector3.up*8f,Vector3.down,groundHits,30f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            RaycastHit candidate=groundHits[i];
            if(candidate.collider==null||candidate.collider.transform.IsChildOf(transform))continue;
            if(candidate.normal.y<0.55f||candidate.collider.GetComponentInParent<RiverValleyHazardMover>()!=null||
                candidate.collider.name.ToLowerInvariant().Contains("home")||
                candidate.collider.name.ToLowerInvariant().Contains("building")||
                candidate.collider.name.ToLowerInvariant().Contains("boundary"))continue;
            if(candidate.distance>=bestDistance)continue;
            bestDistance=candidate.distance;
            bestHit=candidate;
        }
        return bestDistance<float.PositiveInfinity;
    }

    private bool IsVisibleToGameCamera()
    {
        Camera gameCamera=Camera.main;
        if(gameCamera==null)return true;
        Vector3 viewport=gameCamera.WorldToViewportPoint(transform.position+Vector3.up);
        return viewport.z>0f&&viewport.x>-0.08f&&viewport.x<1.08f&&
            viewport.y>-0.08f&&viewport.y<1.08f;
    }

    public void OnFootstep(AnimationEvent animationEvent) { }
    public void OnLand(AnimationEvent animationEvent) { }

#if UNITY_EDITOR
    public void Configure(Transform newPlayer, RiverValleyGameDirector newDirector, RiverValleyWhiteout newWhiteout)
    { player=newPlayer; director=newDirector; whiteout=newWhiteout; }
#endif
}
