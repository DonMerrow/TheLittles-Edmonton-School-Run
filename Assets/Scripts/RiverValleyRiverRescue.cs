using System.Collections;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class RiverValleyRiverRescue : MonoBehaviour
{
    [SerializeField] private Transform rescueSeat;
    [SerializeField] private Transform homeReturn;
    [SerializeField] private Transform momArrival;
    [SerializeField] private GameObject blanket;
    [SerializeField] private RiverValleyMomChase mom;
    [SerializeField] private AudioClip sirenClip;
    [SerializeField] private Transform[] rescueCrew;
    private RiverValleyGameDirector director;
    private WinterAudioDirector audioDirector;
    private AudioSource siren;
    private RiverValleyEmergencyBeacon beacon;
    private bool used;
    private bool rescuing;
    private float nextAllowedRescue;

    public bool IsRescuing => rescuing;
    public static int RescuesCompleted { get; private set; }
    public static int CocoaServed { get; private set; }
    public static int MomOverreactions { get; private set; }
    public static int VisibleTeamCarries { get; private set; }

    private static readonly Vector3[] CarryOffsets=
    {
        new(-0.78f,0f,-0.78f),
        new(0.78f,0f,-0.78f),
        new(-0.78f,0f,0.78f),
        new(0.78f,0f,0.78f)
    };

    private void Start()
    {
        director=FindFirstObjectByType<RiverValleyGameDirector>();
        audioDirector=FindFirstObjectByType<WinterAudioDirector>();
        beacon=FindFirstObjectByType<RiverValleyEmergencyBeacon>();
        if(blanket!=null)blanket.SetActive(false);
        if(sirenClip!=null&&rescueSeat!=null)
        {
            siren=rescueSeat.gameObject.AddComponent<AudioSource>();
            siren.clip=sirenClip;
            siren.loop=true;
            siren.playOnAwake=false;
            siren.spatialBlend=1f;
            siren.minDistance=5f;
            siren.maxDistance=85f;
            siren.volume=0.34f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        DannySpark danny=other.GetComponentInParent<DannySpark>();
        if(danny!=null)BeginRescue(danny);
    }

    public void StageRescueForPlaytest()
    {
        DannySpark danny=FindFirstObjectByType<DannySpark>();
        if(danny!=null)BeginRescue(danny);
    }

    private void BeginRescue(DannySpark danny)
    {
        if(used||rescuing||Time.time<nextAllowedRescue||danny==null||rescueSeat==null||homeReturn==null)return;
        used=true;
        StartCoroutine(RescueRoutine(danny));
    }

    private IEnumerator RescueRoutine(DannySpark spark)
    {
        rescuing=true;
        director?.SetExternalCinematicBusy(true);
        RiverValleyEncounter[] encounters=FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None);
        bool[] encounterStates=new bool[encounters.Length];
        for(int i=0;i<encounters.Length;i++)
        {
            Collider encounterCollider=encounters[i]!=null?encounters[i].GetComponent<Collider>():null;
            encounterStates[i]=encounterCollider!=null&&encounterCollider.enabled;
            if(encounterCollider!=null)encounterCollider.enabled=false;
        }
        DannyTestController movement=spark.GetComponent<DannyTestController>();
        CharacterController controller=spark.GetComponent<CharacterController>();
        if(movement!=null)movement.enabled=false;
        if(controller!=null)controller.enabled=false;
        RiverValleyKidFollower[] travellingChildren=FindObjectsByType<RiverValleyKidFollower>(FindObjectsSortMode.None)
            .Where(child=>child!=null&&child.enabled&&!child.IsScattered&&!child.IsWaiting&&!child.IsBuried)
            .ToArray();
        float huddleY=rescueSeat.position.y;
        for(int i=0;i<travellingChildren.Length;i++)
        {
            int column=i%6;
            int row=i/6;
            Vector3 huddle=rescueSeat.position+new Vector3(-4.0f+column*1.25f,0f,4.6f+row*1.15f);
            huddle.y=huddleY;
            travellingChildren[i].HoldForCinematic(huddle,spark.transform.position);
        }
        beacon?.SetRescueActive(true);
        siren?.Play();
        DannyFollowCamera followCamera=FindFirstObjectByType<DannyFollowCamera>();
        Vector3 start=spark.transform.position;
        Vector3 waterEntry=new Vector3(Mathf.Max(start.x,18.4f),start.y-1.05f,start.z+0.55f);
        followCamera?.FocusCinematic(waterEntry+Vector3.up*0.48f,3.7f);
        director?.Show("DANNY","That ice was absolutely a path one second ago!",2.2f);

        Quaternion upright=spark.transform.rotation;
        Quaternion fallen=upright*Quaternion.Euler(68f,0f,24f);
        for(float time=0f;time<1.35f;time+=Time.deltaTime)
        {
            float t=Mathf.Clamp01(time/1.35f);
            Vector3 position=Vector3.Lerp(start,waterEntry,Mathf.SmoothStep(0f,1f,t));
            position.y+=Mathf.Sin(t*Mathf.PI)*0.22f;
            spark.transform.position=position;
            spark.transform.rotation=Quaternion.Slerp(upright,fallen,t);
            yield return null;
        }
        spark.transform.SetPositionAndRotation(waterEntry,fallen);
        audioDirector?.PlaySnowImpact(waterEntry,0.85f);
        director?.Show("RESCUE CAPTAIN","Four-person pickup! He is small, dramatic, and extremely wet!",2.7f);
        for(float time=0f;time<1.15f;time+=Time.deltaTime)
        {
            float bob=Mathf.Sin(time*7.5f)*0.07f;
            spark.transform.position=waterEntry+Vector3.up*bob;
            yield return null;
        }

        Vector3 bank=new Vector3(14.1f,rescueSeat.position.y,rescueSeat.position.z+0.4f);
        Vector3 waterSurface=new(waterEntry.x,rescueSeat.position.y,waterEntry.z);
        followCamera?.FocusCinematic(Vector3.Lerp(waterSurface,bank,0.55f)+Vector3.up*0.85f,6.4f);
        director?.Show("CHILDREN","Danny! Danny! The rescue crew is coming—clear the bank!",2.8f);
        yield return MoveCrewIntoFormation(waterSurface,1.35f);
        director?.Show("PARAMEDIC","One, two, three—lift! Danny has become emergency-team luggage!",2.8f);
        yield return CarryDanny(spark,waterSurface,bank,fallen,2.3f);
        VisibleTeamCarries++;
        director?.Show("RESCUE CAPTAIN",
            "Keep carrying. Blanket first, cocoa second, explanations after.",3.5f);

        Vector3 finish=rescueSeat.position;
        yield return CarryDanny(spark,bank,finish,fallen,1.65f);
        spark.transform.SetPositionAndRotation(finish,rescueSeat.rotation);
        SetCrewWalking(false);
        followCamera?.FocusCinematic(rescueSeat.position+Vector3.up*0.95f,9.0f);
        if(blanket!=null)blanket.SetActive(true);
        CocoaServed++;
        director?.Show("PARAMEDIC",
            "Cocoa secured. Sit on the bumper and warm up while we call your mother.",3.2f);
        yield return new WaitForSeconds(2.0f);

        if(mom!=null&&momArrival!=null)mom.ArriveForRiverRescue(momArrival.position,18f);
        MomOverreactions++;
        director?.Show("MOTHER",
            "My poor frozen Pooky-Wooky! The river could have carried you all the way to Saskatchewan!",4.2f);
        yield return new WaitForSeconds(2.6f);
        director?.Show("CHILDREN",
            "He fell in the river and got hot chocolate? That is not fair!",3.1f);
        yield return new WaitForSeconds(1.8f);
        director?.Show("MOTHER",
            "Mommy is taking you home for cocoa, three blankets, and a full river-safety lecture!",4.0f);
        yield return new WaitForSeconds(2.3f);

        siren?.Stop();
        beacon?.SetRescueActive(false);
        if(blanket!=null)blanket.SetActive(false);
        spark.transform.SetPositionAndRotation(homeReturn.position,homeReturn.rotation);
        Vector3 homeForward=Vector3.ProjectOnPlane(homeReturn.forward,Vector3.up).normalized;
        if(homeForward.sqrMagnitude<0.01f)homeForward=Vector3.forward;
        Vector3 homeRight=Vector3.Cross(Vector3.up,homeForward).normalized;
        for(int i=0;i<travellingChildren.Length;i++)
        {
            int column=i%2;
            int row=i/2;
            Vector3 rejoin=homeReturn.position+homeRight*((column-0.5f)*1.15f)-
                homeForward*(1.3f+row*1.05f);
            rejoin.y=homeReturn.position.y;
            travellingChildren[i].ResumeAfterCinematic(spark.transform,rejoin);
        }
        // The rescue camera was still easing around the river after Danny had
        // already been returned to the route. Snap back to a useful, close
        // third-person view before control is restored.
        followCamera?.RecenterOnDanny();
        if(mom!=null)mom.ArriveForRiverRescue(homeReturn.position-Vector3.forward*8f,12f);
        if(controller!=null)controller.enabled=true;
        if(movement!=null)
        {
            movement.enabled=true;
            movement.ResetVerticalMotion();
        }
        for(int i=0;i<encounters.Length;i++)
        {
            Collider encounterCollider=encounters[i]!=null?encounters[i].GetComponent<Collider>():null;
            if(encounterCollider!=null)encounterCollider.enabled=encounterStates[i];
        }
        spark.Restore(18f,"Emergency cocoa restores Danny's courage.");
        director?.Show("DANNY",
            "School-run attempt two. I am never hearing the end of this.",3.4f);
        RescuesCompleted++;
        rescuing=false;
        director?.SetExternalCinematicBusy(false);
        used=false;
        nextAllowedRescue=Time.time+5f;
    }

    private IEnumerator MoveCrewIntoFormation(Vector3 centre,float duration)
    {
        if(rescueCrew==null||rescueCrew.Length==0)yield break;
        Vector3[] starts=new Vector3[rescueCrew.Length];
        for(int i=0;i<rescueCrew.Length;i++)
        {
            if(rescueCrew[i]!=null)starts[i]=rescueCrew[i].position;
        }
        SetCrewWalking(true);
        for(float time=0f;time<duration;time+=Time.deltaTime)
        {
            float t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(time/duration));
            for(int i=0;i<rescueCrew.Length;i++)
            {
                Transform responder=rescueCrew[i];
                if(responder==null)continue;
                Vector3 destination=centre+CarryOffsets[i%CarryOffsets.Length];
                Vector3 travel=destination-starts[i];
                responder.position=Vector3.Lerp(starts[i],destination,t);
                Vector3 flatTravel=Vector3.ProjectOnPlane(travel,Vector3.up);
                if(flatTravel.sqrMagnitude>0.01f)
                    responder.rotation=Quaternion.LookRotation(flatTravel.normalized,Vector3.up);
            }
            yield return null;
        }
    }

    private IEnumerator CarryDanny(DannySpark spark,Vector3 from,Vector3 to,Quaternion carriedRotation,float duration)
    {
        SetCrewWalking(true);
        Vector3 sparkStart=spark.transform.position;
        Quaternion rotationStart=spark.transform.rotation;
        Vector3 travel=Vector3.ProjectOnPlane(to-from,Vector3.up);
        for(float time=0f;time<duration;time+=Time.deltaTime)
        {
            float t=Mathf.SmoothStep(0f,1f,Mathf.Clamp01(time/duration));
            Vector3 centre=Vector3.Lerp(from,to,t);
            for(int i=0;i<(rescueCrew?.Length??0);i++)
            {
                Transform responder=rescueCrew[i];
                if(responder==null)continue;
                responder.position=centre+CarryOffsets[i%CarryOffsets.Length];
                if(travel.sqrMagnitude>0.01f)responder.rotation=Quaternion.LookRotation(travel.normalized,Vector3.up);
            }
            Vector3 carriedPosition=centre+Vector3.up*(1.02f+Mathf.Sin(t*Mathf.PI*4f)*0.045f);
            spark.transform.position=Vector3.Lerp(sparkStart,carriedPosition,Mathf.Clamp01(t*2.4f));
            spark.transform.rotation=Quaternion.Slerp(rotationStart,carriedRotation,Mathf.Clamp01(t*2.2f));
            yield return null;
        }
        spark.transform.SetPositionAndRotation(to+Vector3.up*1.02f,carriedRotation);
    }

    private void SetCrewWalking(bool walking)
    {
        if(rescueCrew==null)return;
        foreach(Transform responder in rescueCrew)
        {
            if(responder==null)continue;
            Animator animator=responder.GetComponentInChildren<Animator>();
            if(animator!=null)animator.SetFloat("Speed",walking?2.8f:0f);
        }
    }

#if UNITY_EDITOR
    public void Configure(Transform newRescueSeat,Transform newHomeReturn,Transform newMomArrival,
        GameObject newBlanket,RiverValleyMomChase newMom,AudioClip newSiren,Transform[] newRescueCrew)
    {
        rescueSeat=newRescueSeat;
        homeReturn=newHomeReturn;
        momArrival=newMomArrival;
        blanket=newBlanket;
        mom=newMom;
        sirenClip=newSiren;
        rescueCrew=newRescueCrew;
    }
#endif
}
