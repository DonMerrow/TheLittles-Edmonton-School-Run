using System.Collections.Generic;
using UnityEngine;

public enum RiverAnimalKind { Rabbit, Dog, Coyote, Deer, Fox }

/// <summary>Simple expressive motion for the stylised winter animals.</summary>
[DisallowMultipleComponent]
public sealed class RiverValleyAnimalMotion : MonoBehaviour
{
    private RiverAnimalKind kind;
    private bool roam;
    private Vector3 home;
    private Transform head;
    private Transform tail;
    private readonly List<Transform> legs = new();
    private readonly List<Quaternion> legRest = new();
    private float phase;
    private AudioSource animalVoice;
    private AudioClip rabbitHop;
    private AudioClip coyoteYip;
    private AudioClip coyoteHowl;
    private float previousRabbitHop;
    private float nextCoyoteCall;
    private int coyoteCallIndex;
    private bool riggedAnimation;
    private Transform danny;
    private Vector3 roamTarget;
    private Vector3 previousWorldPosition;
    private float nextRoamDecision;
    private float sniffUntil;
    private Transform sniffTarget;
    private bool sniffCounted;
    private readonly RaycastHit[] groundHits = new RaycastHit[12];
    private QuaterniusAnimalAnimator[] clipPlayers;
    private float frightenedUntil;
    private float groundOffset;
    private float rabbitSettleAt;
    private bool rabbitSettled;
    private bool rabbitAirborne;
    private float rabbitJumpStartedAt;
    private float rabbitJumpDuration;
    private float nextRabbitJump;
    private Vector3 rabbitJumpStart;
    private Vector3 rabbitJumpEnd;
    private Transform rabbitNose;
    private Vector3 rabbitNoseScale;

    public static int RabbitHopSounds { get; private set; }
    public static int CoyoteSounds { get; private set; }
    public static int CoyoteSniffs { get; private set; }
    public static float CoyoteTravelMetres { get; private set; }
    public static int VisibleRabbitJumps { get; private set; }
    public RiverAnimalKind Kind => kind;

    public void Configure(RiverAnimalKind newKind, bool shouldRoam)
    {
        kind = newKind;
        roam = shouldRoam;
    }

    private void Start()
    {
        if(TryFindGround(transform.position,out RaycastHit initialGround))
        {
            Renderer[] visuals=GetComponentsInChildren<Renderer>(true);
            if(kind==RiverAnimalKind.Rabbit&&visuals.Length>0)
            {
                Bounds visualBounds=visuals[0].bounds;
                for(int i=1;i<visuals.Length;i++)visualBounds.Encapsulate(visuals[i].bounds);
                transform.position+=Vector3.up*(initialGround.point.y+0.012f-visualBounds.min.y);
            }
            groundOffset=kind==RiverAnimalKind.Rabbit
                ? Mathf.Max(0.005f,transform.position.y-initialGround.point.y)
                : Mathf.Clamp(transform.position.y-initialGround.point.y,0.005f,0.35f);
        }
        else groundOffset=0.01f;
        home = transform.position;
        rabbitSettleAt=Time.time+0.35f;
        previousWorldPosition=transform.position;
        DannySpark found=FindFirstObjectByType<DannySpark>();
        danny=found!=null?found.transform:null;
        phase = Mathf.Abs(transform.position.x * 0.37f + transform.position.z * 0.19f);
        nextRabbitJump=Time.time+Random.Range(0.8f,2.2f);
        riggedAnimation = GetComponentInChildren<Animator>(true) != null ||
            GetComponentInChildren<QuaterniusAnimalAnimator>(true) != null;
        clipPlayers=GetComponentsInChildren<QuaterniusAnimalAnimator>(true);
        foreach (Transform part in GetComponentsInChildren<Transform>(true))
        {
            string lower = part.name.ToLowerInvariant();
            if(kind==RiverAnimalKind.Rabbit&&rabbitNose==null&&lower.Contains("nose"))
            {
                rabbitNose=part;
                rabbitNoseScale=part.localScale;
            }
            if (!riggedAnimation)
            {
                if (head == null && lower.Contains("head")) head = part;
                if (tail == null && lower.Contains("tail")) tail = part;
                if (lower.Contains("leg")) { legs.Add(part); legRest.Add(part.localRotation); }
            }
        }
        if (kind == RiverAnimalKind.Rabbit || kind == RiverAnimalKind.Coyote)
        {
            animalVoice = gameObject.AddComponent<AudioSource>();
            animalVoice.playOnAwake = false;
            animalVoice.loop = false;
            animalVoice.spatialBlend = 1f;
            animalVoice.dopplerLevel = 0f;
            animalVoice.minDistance = kind == RiverAnimalKind.Rabbit ? 1.4f : 4f;
            animalVoice.maxDistance = kind == RiverAnimalKind.Rabbit ? 18f : 58f;
            animalVoice.volume = kind == RiverAnimalKind.Rabbit ? 0.32f : 0.72f;
            rabbitHop = Resources.Load<AudioClip>("Littles/RiverValley/Audio/Rabbit_Hop");
            coyoteYip = Resources.Load<AudioClip>("Littles/RiverValley/Audio/Coyote_Yip");
            coyoteHowl = Resources.Load<AudioClip>("Littles/RiverValley/Audio/Coyote_Howl");
            nextCoyoteCall = Time.time + Random.Range(3.2f, 7.5f);
        }
        ChooseRoamTarget();
    }

    private void Update()
    {
        float time = Time.time + phase;
        float activity = kind == RiverAnimalKind.Rabbit ? 2.7f : kind == RiverAnimalKind.Dog ? 4.2f : 1.35f;
        float stride = Mathf.Sin(time * activity);

        if (kind == RiverAnimalKind.Rabbit)
        {
            // The playable animation is evaluated after Start.  Re-seat the
            // rabbit once its real sitting pose exists so bent paws cannot
            // end up below the snow even though the bind pose was grounded.
            if(!rabbitSettled&&Time.time>=rabbitSettleAt)
            {
                rabbitSettled=true;
                if(TryFindGround(transform.position,out RaycastHit settledGround))
                {
                    Renderer[] rabbitVisuals=GetComponentsInChildren<Renderer>(true);
                    if(rabbitVisuals.Length>0)
                    {
                        Bounds settledBounds=rabbitVisuals[0].bounds;
                        for(int i=1;i<rabbitVisuals.Length;i++)settledBounds.Encapsulate(rabbitVisuals[i].bounds);
                        transform.position+=Vector3.up*(settledGround.point.y+0.012f-settledBounds.min.y);
                        groundOffset=Mathf.Max(0.005f,transform.position.y-settledGround.point.y);
                        home=transform.position;
                    }
                }
            }
            if(!rabbitAirborne&&Time.time>=nextRabbitJump)
            {
                rabbitAirborne=true;
                rabbitJumpStartedAt=Time.time;
                rabbitJumpDuration=0.62f;
                rabbitJumpStart=transform.position;
                Vector3 hopDirection=Time.time<frightenedUntil
                    ? Vector3.ProjectOnPlane(roamTarget-transform.position,Vector3.up).normalized
                    : Quaternion.Euler(0f,Random.Range(-42f,42f),0f)*transform.forward;
                if(hopDirection.sqrMagnitude<0.01f)hopDirection=transform.forward;
                rabbitJumpEnd=rabbitJumpStart+hopDirection*Random.Range(0.55f,0.95f);
                if(TryFindGround(rabbitJumpEnd,out RaycastHit landing))
                    rabbitJumpEnd.y=landing.point.y+groundOffset;
                else rabbitJumpEnd=rabbitJumpStart;
                nextRabbitJump=Time.time+Random.Range(2.2f,4.2f);
                VisibleRabbitJumps++;
                if(clipPlayers!=null)
                    foreach(QuaterniusAnimalAnimator clipPlayer in clipPlayers)
                        if(clipPlayer!=null)
                            rabbitJumpDuration=Mathf.Max(rabbitJumpDuration,clipPlayer.PlayRabbitJumpNow());
                if(animalVoice!=null&&rabbitHop!=null&&!animalVoice.isPlaying)
                {
                    animalVoice.pitch=Random.Range(0.92f,1.12f);
                    animalVoice.PlayOneShot(rabbitHop);
                    RabbitHopSounds++;
                }
            }
            Vector3 direction;
            if(rabbitAirborne)
            {
                float jumpT=Mathf.Clamp01((Time.time-rabbitJumpStartedAt)/rabbitJumpDuration);
                Vector3 jumpPosition=Vector3.Lerp(rabbitJumpStart,rabbitJumpEnd,jumpT);
                jumpPosition.y+=Mathf.Sin(jumpT*Mathf.PI)*0.34f;
                transform.position=jumpPosition;
                direction=Vector3.ProjectOnPlane(rabbitJumpEnd-rabbitJumpStart,Vector3.up);
                if(jumpT>=1f)rabbitAirborne=false;
            }
            else
            {
                Vector3 grounded = transform.position;
                // Rabbits relocate only through visible jumps. The old slow
                // idle drift made the sitting model slide sideways on snow.
                Vector3 desired = Time.time<frightenedUntil ? roamTarget : grounded;
                if(TryFindGround(desired,out RaycastHit rabbitGround))desired.y=rabbitGround.point.y+groundOffset;
                else desired.y=home.y;
                direction = Vector3.ProjectOnPlane(desired - grounded, Vector3.up);
                if(Time.time<frightenedUntil&&direction.magnitude>0.35f)
                    nextRabbitJump=Mathf.Min(nextRabbitJump,Time.time+0.05f);
            }
            if (direction.sqrMagnitude > 0.002f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(direction.normalized, Vector3.up), Time.deltaTime * 4f);
            float pawBeat=Mathf.Max(0f,stride)*Mathf.Clamp01(direction.magnitude*4f);
            if (previousRabbitHop > 0.65f && pawBeat <= 0.18f && animalVoice != null && rabbitHop != null && !animalVoice.isPlaying)
            {
                animalVoice.pitch = Random.Range(0.92f, 1.12f);
                animalVoice.PlayOneShot(rabbitHop);
                RabbitHopSounds++;
            }
            previousRabbitHop = pawBeat;
        }
        else if (roam) UpdateGroundedRoaming();

        float travelled=Vector3.ProjectOnPlane(transform.position-previousWorldPosition,Vector3.up).magnitude;
        float realSpeed=Time.deltaTime>0f?travelled/Time.deltaTime:0f;
        if(kind==RiverAnimalKind.Coyote)CoyoteTravelMetres+=travelled;
        previousWorldPosition=transform.position;
        float motion=realSpeed>0.04f?Mathf.InverseLerp(0.04f,0.85f,realSpeed):0f;
        if(clipPlayers!=null)
            foreach(QuaterniusAnimalAnimator clipPlayer in clipPlayers)
                clipPlayer?.SetMotionAmount(motion);

        if (!riggedAnimation)
        {
            for (int i = 0; i < legs.Count; i++)
                if (legs[i] != null)
                    legs[i].localRotation = legRest[i] * Quaternion.Euler(((i & 1) == 0 ? stride : -stride) * 13f, 0f, 0f);
            if (tail != null)
                tail.localRotation = Quaternion.Euler(45f, Mathf.Sin(time * 5f) * (kind == RiverAnimalKind.Dog ? 30f : 9f), 0f);
            if (head != null)
                head.localRotation = Quaternion.Euler(Mathf.Sin(time * 1.7f) * 5f, Mathf.Sin(time * 0.8f) * 9f, 0f);
        }

        if (kind == RiverAnimalKind.Coyote && animalVoice != null && Time.time >= nextCoyoteCall)
        {
            AudioClip call = (coyoteCallIndex++ % 3 == 2) ? coyoteHowl : coyoteYip;
            if (call != null)
            {
                animalVoice.pitch = Random.Range(0.94f, 1.07f);
                animalVoice.PlayOneShot(call);
                CoyoteSounds++;
            }
            nextCoyoteCall = Time.time + Random.Range(7.5f, 14f);
        }
    }

    private void LateUpdate()
    {
        if(kind!=RiverAnimalKind.Rabbit)return;
        if(rabbitNose!=null)
            rabbitNose.localScale=rabbitNoseScale*(1f+Mathf.Sin((Time.time+phase)*18f)*0.075f);
        if(rabbitAirborne)return;
        if(!TryFindGround(transform.position,out RaycastHit ground))return;
        Renderer[] visuals=GetComponentsInChildren<Renderer>(true);
        if(visuals.Length==0)return;
        Bounds bounds=visuals[0].bounds;
        for(int i=1;i<visuals.Length;i++)bounds.Encapsulate(visuals[i].bounds);
        float correction=ground.point.y+0.008f-bounds.min.y;
        // While sitting, correct both sinking and floating. Root translation
        // remains free only during the synchronized visible jump.
        if(Mathf.Abs(correction)>0.002f&&Mathf.Abs(correction)<0.55f)
        {
            transform.position+=Vector3.up*correction;
            home.y+=correction;
            groundOffset=Mathf.Max(0.005f,transform.position.y-ground.point.y);
        }
    }

    private void UpdateGroundedRoaming()
    {
        if(kind==RiverAnimalKind.Coyote)UpdateCoyoteInterest();
        bool fleeingDeer=kind==RiverAnimalKind.Deer&&UpdateDeerAvoidance();
        if(Time.time>=nextRoamDecision||(sniffTarget==null&&
            Vector3.ProjectOnPlane(roamTarget-transform.position,Vector3.up).sqrMagnitude<0.18f))
            ChooseRoamTarget();

        Vector3 direction=Vector3.ProjectOnPlane(roamTarget-transform.position,Vector3.up);
        if(direction.sqrMagnitude<0.01f)return;
        float speed=kind==RiverAnimalKind.Coyote?0.92f:
            kind==RiverAnimalKind.Deer?(fleeingDeer?2.15f:0.62f):kind==RiverAnimalKind.Fox?0.74f:0.58f;
        if(Time.time<frightenedUntil)speed=Mathf.Max(speed,kind==RiverAnimalKind.Deer?3.1f:2.35f);
        float interestDistance=sniffTarget!=null
            ? Vector3.ProjectOnPlane(sniffTarget.position-transform.position,Vector3.up).magnitude
            : float.PositiveInfinity;
        if(sniffTarget!=null&&Time.time<sniffUntil&&interestDistance<2.3f)
        {
            Vector3 look=Vector3.ProjectOnPlane(sniffTarget.position-transform.position,Vector3.up);
            if(look.sqrMagnitude>0.01f)
                transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(look.normalized,Vector3.up),
                    1f-Mathf.Exp(-5f*Time.deltaTime));
            if(!sniffCounted){sniffCounted=true;CoyoteSniffs++;}
            return;
        }

        Vector3 next=transform.position+direction.normalized*speed*Time.deltaTime;
        if(!TryFindGround(next,out RaycastHit ground)||Mathf.Abs(ground.point.y-transform.position.y)>0.38f)
        {
            nextRoamDecision=0f;
            return;
        }
        next.y=ground.point.y+0.01f;
        transform.position=next;
        transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction.normalized,Vector3.up),
            1f-Mathf.Exp(-4.2f*Time.deltaTime));
    }

    private bool UpdateDeerAvoidance()
    {
        if(danny==null)return false;
        Vector3 away=Vector3.ProjectOnPlane(transform.position-danny.position,Vector3.up);
        float distance=away.magnitude;
        if(distance>7f)return false;
        if(away.sqrMagnitude<0.01f)away=transform.forward;
        Vector3 candidate=transform.position+away.normalized*Mathf.Lerp(3.2f,6.4f,
            Mathf.InverseLerp(7f,0f,distance));
        // Stay in the authored wildlife patch while making it unmistakable
        // that deer are wary animals, not another pet interaction.
        Vector3 fromHome=Vector3.ProjectOnPlane(candidate-home,Vector3.up);
        if(fromHome.magnitude>7.5f)candidate=home+fromHome.normalized*7.5f;
        if(TryFindGround(candidate,out RaycastHit ground))
        {
            candidate.y=ground.point.y+0.01f;
            roamTarget=candidate;
            nextRoamDecision=Time.time+0.65f;
        }
        return true;
    }

    private void UpdateCoyoteInterest()
    {
        if(sniffTarget!=null&&Time.time<sniffUntil)return;
        sniffTarget=null;
        if(danny==null||Time.time<nextRoamDecision)return;
        float distance=Vector3.ProjectOnPlane(danny.position-transform.position,Vector3.up).magnitude;
        if(distance>11f||Vector3.ProjectOnPlane(danny.position-home,Vector3.up).magnitude>10f)return;
        sniffTarget=danny;
        Vector3 away=Vector3.ProjectOnPlane(transform.position-danny.position,Vector3.up);
        if(away.sqrMagnitude<0.01f)away=transform.forward;
        roamTarget=danny.position+away.normalized*2.05f;
        if(TryFindGround(roamTarget,out RaycastHit ground))roamTarget.y=ground.point.y+0.01f;
        sniffUntil=Time.time+Random.Range(2.8f,4.4f);
        nextRoamDecision=sniffUntil;
        sniffCounted=false;
    }

    private void ChooseRoamTarget()
    {
        sniffTarget=null;
        sniffCounted=false;
        float radius=kind==RiverAnimalKind.Coyote?6.5f:
            kind==RiverAnimalKind.Deer?4.5f:kind==RiverAnimalKind.Fox?3.8f:2.7f;
        for(int attempt=0;attempt<8;attempt++)
        {
            Vector2 circle=Random.insideUnitCircle*radius;
            Vector3 candidate=home+new Vector3(circle.x,0f,circle.y);
            if(!TryFindGround(candidate,out RaycastHit ground)||Mathf.Abs(ground.point.y-home.y)>0.42f)continue;
            candidate.y=ground.point.y+0.01f;
            roamTarget=candidate;
            nextRoamDecision=Time.time+Random.Range(3.5f,7f);
            return;
        }
        roamTarget=home;
        nextRoamDecision=Time.time+1.2f;
    }

    public void FleeFromLight(Vector3 source)
    {
        Vector3 away=Vector3.ProjectOnPlane(transform.position-source,Vector3.up);
        if(away.sqrMagnitude<0.01f)away=transform.forward;
        float distance=kind==RiverAnimalKind.Rabbit?4.8f:kind==RiverAnimalKind.Deer?8f:6.5f;
        Vector3 candidate=transform.position+away.normalized*distance;
        if(TryFindGround(candidate,out RaycastHit ground))candidate.y=ground.point.y+0.01f;
        roamTarget=candidate;
        sniffTarget=null;
        sniffUntil=0f;
        frightenedUntil=Time.time+2.2f;
        nextRoamDecision=frightenedUntil;
    }

    private bool TryFindGround(Vector3 position,out RaycastHit bestHit)
    {
        bestHit=default;
        float closest=float.PositiveInfinity;
        int count=Physics.RaycastNonAlloc(position+Vector3.up*5f,Vector3.down,groundHits,14f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            RaycastHit hit=groundHits[i];
            if(hit.collider==null||hit.collider.transform.IsChildOf(transform)||hit.distance>=closest||
                !IsAnimalGround(hit.collider))continue;
            closest=hit.distance;
            bestHit=hit;
        }
        return closest<float.PositiveInfinity;
    }

    private static bool IsAnimalGround(Collider collider)
    {
        string label=collider.name.ToLowerInvariant();
        Transform parent=collider.transform.parent;
        if(parent!=null)label+=" "+parent.name.ToLowerInvariant();
        return !label.Contains("building")&&!label.Contains("roof")&&!label.Contains("solid exterior")&&
            !label.Contains("safety boundary")&&!label.Contains("sign")&&!label.Contains("tree")&&
            !label.Contains("trunk")&&!label.Contains("branch")&&!label.Contains("vehicle")&&
            !label.Contains("plow")&&!label.Contains("firetruck")&&!label.Contains("child")&&
            !label.Contains("danny")&&!label.Contains("mom");
    }
}
