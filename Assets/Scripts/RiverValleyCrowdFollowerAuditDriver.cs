using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Opt-in regression test for a real school-sized crowd through the corner and stairs.</summary>
public sealed class RiverValleyCrowdFollowerAuditDriver : MonoBehaviour
{
    private readonly Vector3[] route={new(0f,0f,124f),new(0f,0f,134f),new(0f,0f,153f),
        new(0f,0f,174f),new(0f,0f,194f),new(0f,0f,212f)};
    private readonly List<RiverValleyKidFollower> followers=new();
    private readonly List<string> notes=new();
    private Transform danny;
    private CharacterController controller;
    private int waypoint;
    private float started;
    private float stoppedAt=-1f;
    private float maximumGap;
    private int runtimeErrors;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartFromCommandLine()
    {
        foreach(string argument in System.Environment.GetCommandLineArgs())
            if(argument=="-riverCrowdFollowerAudit")
            {
                GameObject runner=new("CROWD FOLLOWER AUDIT — not gameplay");
                DontDestroyOnLoad(runner);
                runner.AddComponent<RiverValleyCrowdFollowerAuditDriver>();
                return;
            }
    }

    private void Start()
    {
        Application.runInBackground=true;
        Application.targetFrameRate=75;
        QualitySettings.vSyncCount=0;
        Time.timeScale=2f;
        Application.logMessageReceived+=OnLog;
        DannySpark spark=FindFirstObjectByType<DannySpark>();
        danny=spark!=null?spark.transform:null;
        controller=danny!=null?danny.GetComponent<CharacterController>():null;
        if(danny==null||controller==null){Finish("Danny is missing.");return;}
        DannyTestController movement=danny.GetComponent<DannyTestController>();
        if(movement!=null)movement.enabled=false;
        foreach(RiverValleyMomChase mom in FindObjectsByType<RiverValleyMomChase>())mom.enabled=false;
        foreach(RiverValleyHazardMover hazard in FindObjectsByType<RiverValleyHazardMover>())hazard.enabled=false;
        foreach(RiverValleySafeCar car in FindObjectsByType<RiverValleySafeCar>())car.enabled=false;

        controller.enabled=false;
        danny.position=new Vector3(0f,0.08f,108f);
        controller.enabled=true;
        int slot=0;
        foreach(RiverValleyEncounter encounter in FindObjectsByType<RiverValleyEncounter>())
        {
            if(encounter==null||encounter.Actor==null||!encounter.Actor.gameObject.activeInHierarchy||
               (encounter.Kind!=RiverEncounterKind.LostKid&&encounter.Kind!=RiverEncounterKind.ParentHandoff))continue;
            RiverValleyKidFollower follower=encounter.Actor.GetComponent<RiverValleyKidFollower>();
            if(follower==null)follower=encounter.Actor.gameObject.AddComponent<RiverValleyKidFollower>();
            follower.Configure(danny,slot,encounter);
            follower.transform.position=danny.position+new Vector3((slot%2==0?-0.55f:0.55f),0f,-2f-(slot/2)*1.05f);
            follower.Regather(danny,slot++);
            followers.Add(follower);
            if(followers.Count>=10)break;
        }
        foreach(RiverValleyEncounter encounter in FindObjectsByType<RiverValleyEncounter>())
            if(encounter!=null&&encounter.TryGetComponent(out Collider trigger))trigger.enabled=false;
        if(followers.Count<8){Finish($"Only {followers.Count} follower groups were available.");return;}
        notes.Add($"Staged {followers.Count} physical follower groups, containing a school-sized crowd.");
        started=Time.time;
    }

    private void Update()
    {
        if(danny==null||controller==null||followers.Count==0)return;
        if(Time.time-started>150f){Finish("Crowd route timed out.");return;}
        if(waypoint>=route.Length)
        {
            if(stoppedAt<0f)stoppedAt=Time.time;
            if(Time.time-stoppedAt<5f)return;
            float worst=0f;
            foreach(RiverValleyKidFollower follower in followers)
                if(follower!=null)worst=Mathf.Max(worst,FlatGap(follower.transform.position,danny.position));
            notes.Add($"Worst final group gap after Danny stopped: {worst:F2} m.");
            Finish(worst>7f||maximumGap>15f?"The full crowd did not stay with Danny.":null);
            return;
        }
        Vector3 flat=Vector3.ProjectOnPlane(route[waypoint]-danny.position,Vector3.up);
        if(flat.magnitude<0.62f)
        {
            float worst=0f;
            foreach(RiverValleyKidFollower follower in followers)
                if(follower!=null)worst=Mathf.Max(worst,FlatGap(follower.transform.position,danny.position));
            notes.Add($"Waypoint {waypoint+1}/{route.Length}: worst crowd gap {worst:F2} m.");
            if(waypoint==1||waypoint==2)
                foreach(RiverValleyKidFollower follower in followers)
                    if(follower!=null)
                    {
                        notes.Add($"  {follower.name}: {follower.transform.position:F1}; gap {FlatGap(follower.transform.position,danny.position):F2} m; moving {follower.IsMoving}.");
                        if(!follower.IsMoving)ProbeBlock(follower);
                    }
            waypoint++;
            return;
        }
        controller.Move(flat.normalized*Mathf.Min(flat.magnitude,2.45f*Time.deltaTime)+Vector3.down*3f*Time.deltaTime);
        foreach(RiverValleyKidFollower follower in followers)
            if(follower!=null)maximumGap=Mathf.Max(maximumGap,FlatGap(follower.transform.position,danny.position));
    }

    private static float FlatGap(Vector3 a,Vector3 b) => Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;
    private void ProbeBlock(RiverValleyKidFollower follower)
    {
        Vector3 at=follower.transform.position;
        Vector3 direction=Vector3.ProjectOnPlane(danny.position-at,Vector3.up).normalized;
        foreach(RaycastHit hit in Physics.CapsuleCastAll(at+Vector3.up*0.27f,at+Vector3.up*1.28f,
            0.25f,direction,1.5f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            if(hit.collider!=null&&!hit.collider.transform.IsChildOf(follower.transform))
                notes.Add($"    obstacle: {hit.collider.name}; root {hit.collider.transform.root.name}; distance {hit.distance:F2}.");
        foreach(RaycastHit hit in Physics.RaycastAll(at+direction*0.35f+Vector3.up*5f,Vector3.down,12f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            if(hit.collider!=null&&!hit.collider.transform.IsChildOf(follower.transform))
                notes.Add($"    ground: {hit.collider.name} y={hit.point.y:F2} normal={hit.normal.y:F2}.");
    }
    private void OnLog(string message,string stack,LogType type)
    {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert){runtimeErrors++;notes.Add("Runtime error: "+message);}}
    private void OnDestroy()=>Application.logMessageReceived-=OnLog;

    private void Finish(string failure)
    {
        if(failure!=null)notes.Add("FAIL: "+failure);
        else notes.Add("PASS: the full crowd physically followed Danny through the corner and down all 180 stairs.");
        notes.Add($"Waypoints {waypoint}/{route.Length}; maximum gap {maximumGap:F2} m; runtime errors {runtimeErrors}.");
        Directory.CreateDirectory("/tmp/thelittles-crowd-follower-audit");
        File.WriteAllLines("/tmp/thelittles-crowd-follower-audit/report.txt",notes);
        Application.Quit(failure==null&&runtimeErrors==0?0:2);
    }
}
