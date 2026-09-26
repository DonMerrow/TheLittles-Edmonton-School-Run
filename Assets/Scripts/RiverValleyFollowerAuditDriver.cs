using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>Opt-in, physical corner-following regression test for a recruited family.</summary>
public sealed class RiverValleyFollowerAuditDriver : MonoBehaviour
{
    private Vector3[] route={new(-30f,0f,-5f),new(-12f,0f,-5f),
        new(0f,0f,-5f),new(0f,0f,28f),new(0f,0f,54f),new(0f,0f,82f)};
    private bool stairAudit;
    private readonly List<string> notes=new();
    private Transform danny;
    private CharacterController controller;
    private RiverValleyKidFollower follower;
    private int waypoint;
    private float started;
    private float finishedAt=-1f;
    private float maximumGap;
    private int stuckFrames;
    private int errors;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartFromCommandLine()
    {
        if(!System.Environment.GetCommandLineArgs().Contains("-riverFollowerAudit")&&
           !System.Environment.GetCommandLineArgs().Contains("-riverStairFollowerAudit"))return;
        GameObject runner=new("FOLLOWER CORNER AUDIT — not gameplay");
        DontDestroyOnLoad(runner);
        runner.AddComponent<RiverValleyFollowerAuditDriver>();
    }

    private void Start()
    {
        stairAudit=System.Environment.GetCommandLineArgs().Contains("-riverStairFollowerAudit");
        if(stairAudit)route=new[]{new Vector3(0f,0f,135f),new Vector3(0f,0f,153f),
            new Vector3(0f,0f,173f),new Vector3(0f,0f,193f),new Vector3(0f,0f,212f)};
        Application.runInBackground=true;
        Application.targetFrameRate=75;
        QualitySettings.vSyncCount=0;
        Time.timeScale=3f;
        Application.logMessageReceived+=OnLog;
        DannySpark spark=FindFirstObjectByType<DannySpark>();
        danny=spark!=null?spark.transform:null;
        controller=danny!=null?danny.GetComponent<CharacterController>():null;
        if(danny==null||controller==null){Finish("Danny is missing.");return;}
        DannyTestController movement=danny.GetComponent<DannyTestController>();
        if(movement!=null)movement.enabled=false;
        foreach(RiverValleyMomChase mom in FindObjectsByType<RiverValleyMomChase>(FindObjectsSortMode.None))mom.enabled=false;
        foreach(RiverValleyHazardMover hazard in FindObjectsByType<RiverValleyHazardMover>(FindObjectsSortMode.None))
        { hazard.enabled=false; foreach(Collider collider in hazard.GetComponentsInChildren<Collider>())collider.enabled=false; }
        foreach(RiverValleySafeCar car in FindObjectsByType<RiverValleySafeCar>(FindObjectsSortMode.None))
        { car.enabled=false; foreach(Collider collider in car.GetComponentsInChildren<Collider>())collider.enabled=false; }
        controller.enabled=false;
        danny.position=new Vector3(-47f,0.08f,-5f);
        controller.enabled=true;
        RiverValleyEncounter encounter=FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None)
            .FirstOrDefault(item=>item.name=="Parent handoff on Danny's approach block");
        RiverValleyGameDirector director=FindFirstObjectByType<RiverValleyGameDirector>();
        if(encounter==null||director==null){Finish("Opening family encounter is missing.");return;}
        director.Resolve(encounter);
        follower=encounter.Actor!=null?encounter.Actor.GetComponent<RiverValleyKidFollower>():null;
        if(follower==null){Finish("The children did not join Danny.");return;}
        if(stairAudit)
        {
            controller.enabled=false;
            danny.position=new Vector3(0f,0.08f,132.7f);
            controller.enabled=true;
            follower.transform.position=new Vector3(0f,0.08f,130.7f);
            follower.Regather(danny,0);
        }
        foreach(RiverValleyEncounter other in FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None))
            if(other!=null&&other!=encounter&&other.TryGetComponent(out Collider trigger))trigger.enabled=false;
        started=Time.time;
        notes.Add(stairAudit?"Three children recruited; Danny physically descends the long stair route.":
            "Three children recruited; Danny physically walks an L-shaped street route.");
    }

    private void OnDestroy()=>Application.logMessageReceived-=OnLog;
    private void OnLog(string message,string stack,LogType type)
    { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){errors++;notes.Add("Runtime error: "+message);} }

    private void Update()
    {
        if(danny==null||controller==null||follower==null)return;
        if(Time.time-started>(stairAudit?130f:110f)){Finish("The walk timed out.");return;}
        if(waypoint>=route.Length)
        {
            if(finishedAt<0f||Time.time-finishedAt<4f)return;
            float finalGap=Vector3.ProjectOnPlane(follower.transform.position-danny.position,Vector3.up).magnitude;
            notes.Add($"Final child gap after stopping: {finalGap:F2} m.");
            if(stairAudit)ProbeStairBlock();
            Finish(finalGap>5f||maximumGap>10f?"The children did not stay close enough.":null);
            return;
        }
        Vector3 flat=Vector3.ProjectOnPlane(route[waypoint]-danny.position,Vector3.up);
        if(flat.magnitude<0.65f)
        {
            float gap=Vector3.ProjectOnPlane(follower.transform.position-danny.position,Vector3.up).magnitude;
            notes.Add($"At waypoint {waypoint+1}: child gap {gap:F2} m, position {follower.transform.position:F1}.");
            waypoint++;
            if(waypoint==route.Length)finishedAt=Time.time;
            return;
        }
        Vector3 before=danny.position;
        controller.Move(flat.normalized*Mathf.Min(flat.magnitude,2.65f*Time.deltaTime)+Vector3.down*3f*Time.deltaTime);
        if(Vector3.ProjectOnPlane(danny.position-before,Vector3.up).magnitude<0.004f)stuckFrames++;
        float distance=Vector3.ProjectOnPlane(follower.transform.position-danny.position,Vector3.up).magnitude;
        maximumGap=Mathf.Max(maximumGap,distance);
    }

    private void Finish(string failure)
    {
        if(failure!=null)notes.Add("FAIL: "+failure);
        notes.Add($"Waypoints: {waypoint}/{route.Length}; maximum gap {maximumGap:F2} m; stuck player frames {stuckFrames}; runtime errors {errors}.");
        string folder=stairAudit?"/tmp/thelittles-stair-follower-audit":"/tmp/thelittles-follower-audit";
        Directory.CreateDirectory(folder);
        File.WriteAllLines(folder+"/report.txt",notes);
        enabled=false;
        Application.Quit(failure==null&&errors==0&&waypoint==route.Length?0:2);
    }

    private void ProbeStairBlock()
    {
        Vector3 at=follower.transform.position;
        foreach(RaycastHit hit in Physics.RaycastAll(at+Vector3.forward*0.18f+Vector3.up*6f,
            Vector3.down,20f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            if(hit.collider!=null&&hit.point.y>at.y-2f)
                notes.Add($"Ground probe: {hit.collider.name} at y={hit.point.y:F2} normal={hit.normal.y:F2}.");
        foreach(RaycastHit hit in Physics.CapsuleCastAll(at+Vector3.up*0.27f,
            at+Vector3.up*1.28f,0.22f,Vector3.forward,0.25f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            if(hit.collider!=null)notes.Add($"Step obstacle: {hit.collider.name} at {hit.distance:F2}.");
    }
}
