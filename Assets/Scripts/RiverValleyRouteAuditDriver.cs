using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>Command-line-only physical walking audit; never runs in the normal game.</summary>
[DefaultExecutionOrder(-900)]
public sealed class RiverValleyRouteAuditDriver : MonoBehaviour
{
    private const string OutputDirectory="/tmp/thelittles-route-audit";
    private readonly List<(string Name,Vector3 Position)> route=new()
    {
        ("home block",new Vector3(-60f,0f,-5f)),
        ("approach one",new Vector3(-42f,0f,-5f)),
        ("approach two",new Vector3(-20f,0f,-5f)),
        ("school corner",new Vector3(0f,0f,-5f)),
        ("early main street",new Vector3(0f,0f,28f)),
        ("coyote entrance",new Vector3(0f,0f,62f)),
        ("coyote woods centre",new Vector3(-28f,0f,73f)),
        ("coyote lost children",new Vector3(-24f,0f,92f)),
        ("return from coyote woods",new Vector3(0f,0f,62f)),
        ("rabbit crossing near side",new Vector3(0f,0f,74f)),
        ("rabbit crossing far side",new Vector3(15f,0f,74f)),
        ("rabbit entrance",new Vector3(18f,0f,72f)),
        ("rabbit club",new Vector3(31f,0f,78f)),
        ("small rabbit children",new Vector3(40f,0f,97f)),
        ("return from rabbit field",new Vector3(15f,0f,74f)),
        ("return across rabbit crosswalk",new Vector3(0f,0f,74f)),
        ("hockey block",new Vector3(0f,0f,110f)),
        ("stair overlook",new Vector3(0f,0f,130f)),
        ("second dead-end turn",new Vector3(61f,0f,130f)),
        ("dead-end hockey game",new Vector3(61f,0f,144f)),
        ("back from dead-end hockey",new Vector3(61f,0f,130f)),
        ("east winter loop end",new Vector3(82f,0f,130f)),
        ("return to stair overlook",new Vector3(0f,0f,130f)),
        ("stair top",new Vector3(0f,0f,135f)),
        ("landing one",new Vector3(0f,0f,154f)),
        ("landing two",new Vector3(0f,0f,174f)),
        ("landing three",new Vector3(0f,0f,193f)),
        ("stair bottom",new Vector3(0f,0f,212f)),
        ("left city-hill branch",new Vector3(-19f,0f,237f)),
        ("back to stair split",new Vector3(0f,0f,214f)),
        ("right river-view branch",new Vector3(14f,0f,237f)),
        ("right branch return",new Vector3(4f,0f,270f)),
        ("valley path one",new Vector3(0f,0f,300f)),
        ("valley path two",new Vector3(0f,0f,340f)),
        ("whiteout path",new Vector3(0f,0f,380f)),
        ("around school-approach drift",new Vector3(-2.6f,0f,390f)),
        ("school gate",new Vector3(0f,0f,412f))
    };
    private readonly List<string> notes=new();
    private readonly Dictionary<WinterCastIdentity,Vector3> actorPositions=new();
    private readonly Dictionary<string,int> warpCounts=new();
    private readonly Dictionary<string,float> warpMaximums=new();
    private Transform player;
    private CharacterController controller;
    private int waypoint;
    private float waypointStarted;
    private int unsupportedFrames;
    private int stalledWaypoints;
    private int actorWarpEvents;
    private float maximumActorStep;
    private float maximumDrop;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartFromCommandLine()
    {
        if(!System.Environment.GetCommandLineArgs().Contains("-riverRouteAudit"))return;
        Directory.CreateDirectory(OutputDirectory);
        GameObject runner=new("PHYSICAL ROUTE AUDIT — not gameplay");
        DontDestroyOnLoad(runner);
        runner.AddComponent<RiverValleyRouteAuditDriver>();
    }

    private void Start()
    {
        Application.runInBackground=true;
        QualitySettings.vSyncCount=0;
        Application.targetFrameRate=90;
        Time.timeScale=6f;
        DannySpark danny=FindFirstObjectByType<DannySpark>();
        player=danny!=null?danny.transform:null;
        controller=player!=null?player.GetComponent<CharacterController>():null;
        if(player==null||controller==null){Finish("Danny or his controller was missing.");return;}
        DannyTestController movement=player.GetComponent<DannyTestController>();
        if(movement!=null)movement.enabled=false;
        foreach(RiverValleyMomChase mom in FindObjectsByType<RiverValleyMomChase>(FindObjectsSortMode.None))mom.enabled=false;
        foreach(RiverValleyHazardMover hazard in FindObjectsByType<RiverValleyHazardMover>(FindObjectsSortMode.None))
        {
            hazard.enabled=false;
            foreach(Collider obstacle in hazard.GetComponentsInChildren<Collider>())obstacle.enabled=false;
        }
        foreach(RiverValleySafeCar car in FindObjectsByType<RiverValleySafeCar>(FindObjectsSortMode.None))
        {
            car.enabled=false;
            foreach(Collider obstacle in car.GetComponentsInChildren<Collider>())obstacle.enabled=false;
        }
        foreach(RiverValleyEncounter encounter in FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None))
            if(encounter!=null&&encounter.TryGetComponent(out Collider trigger))trigger.enabled=false;
        foreach(WinterCastIdentity actor in FindObjectsByType<WinterCastIdentity>(FindObjectsSortMode.None))
            if(actor!=null)actorPositions[actor]=actor.transform.position;
        waypointStarted=Time.time;
        notes.Add("A physical CharacterController began a continuous walk from Danny's home; route waypoints were not teleport hops.");
    }

    private void Update()
    {
        if(player==null||controller==null)return;
        if(waypoint>=route.Count){Finish(null);return;}
        Vector3 target=route[waypoint].Position;
        Vector3 flat=Vector3.ProjectOnPlane(target-player.position,Vector3.up);
        if(flat.magnitude<0.65f)
        {
            notes.Add($"Reached {route[waypoint].Name} at {player.position}.");
            waypoint++;
            waypointStarted=Time.time;
            return;
        }
        // Long legs such as the deliberately extended winter loop take about
        // seventy in-game seconds at this careful walking pace.
        if(Time.time-waypointStarted>120f)
        {
            notes.Add($"STALLED before {route[waypoint].Name} at {player.position}.");
            foreach(Collider nearby in Physics.OverlapSphere(player.position+Vector3.up*0.8f,2.5f,
                Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {
                if(nearby==controller)continue;
                notes.Add($"Nearby solid: {nearby.gameObject.name} on {nearby.transform.root.name}; bounds {nearby.bounds.min} to {nearby.bounds.max}.");
            }
            stalledWaypoints++;
            Finish("The continuous route was blocked.");
            return;
        }
        Vector3 direction=flat.normalized;
        player.rotation=Quaternion.Slerp(player.rotation,Quaternion.LookRotation(direction,Vector3.up),
            1f-Mathf.Exp(-9f*Time.deltaTime));
        float beforeY=player.position.y;
        // Accelerated audit frames can step beyond a waypoint and oscillate
        // around it forever. Clamp the horizontal step to the remaining gap.
        float horizontalStep=Mathf.Min(flat.magnitude,1.18f*Time.deltaTime);
        controller.Move(direction*horizontalStep+Vector3.down*3.2f*Time.deltaTime);
        maximumDrop=Mathf.Max(maximumDrop,beforeY-player.position.y);
        bool supported=Physics.Raycast(player.position+Vector3.up*0.5f,Vector3.down,4.5f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        if(!supported)
        {
            unsupportedFrames++;
            if(unsupportedFrames<=12)
                notes.Add($"Briefly unsupported near {route[waypoint].Name} at {player.position}.");
        }
    }

    private void LateUpdate()
    {
        foreach(WinterCastIdentity actor in actorPositions.Keys.ToArray())
        {
            if(actor==null){actorPositions.Remove(actor);continue;}
            Vector3 previous=actorPositions[actor];
            float step=Vector3.Distance(previous,actor.transform.position);
            maximumActorStep=Mathf.Max(maximumActorStep,step);
            float velocity=Time.deltaTime>0.0001f?step/Time.deltaTime:0f;
            if(Time.time>2f&&step>0.30f&&velocity>4.5f)
            {
                actorWarpEvents++;
                string actorName=actor.DisplayName+" ("+actor.gameObject.name+")";
                warpCounts[actorName]=warpCounts.TryGetValue(actorName,out int count)?count+1:1;
                warpMaximums[actorName]=Mathf.Max(warpMaximums.TryGetValue(actorName,out float old)?old:0f,step);
            }
            actorPositions[actor]=actor.transform.position;
        }
    }

    private void Finish(string failure)
    {
        if(!string.IsNullOrEmpty(failure))notes.Add("FAIL: "+failure);
        notes.Add($"Waypoints reached: {waypoint}/{route.Count}.");
        notes.Add($"Unsupported walking frames: {unsupportedFrames}.");
        notes.Add($"Stalled waypoints: {stalledWaypoints}.");
        notes.Add($"Visible actor warp events over 0.55m in one frame: {actorWarpEvents}.");
        notes.Add($"Largest visible actor frame step: {maximumActorStep:0.000}m.");
        foreach(string actorName in warpCounts.Keys.OrderByDescending(key=>warpMaximums[key]).Take(8))
            notes.Add($"Warp candidate: {actorName}; {warpCounts[actorName]} frames; max {warpMaximums[actorName]:0.000}m.");
        notes.Add($"Largest player one-frame vertical drop: {maximumDrop:0.000}m.");
        Directory.CreateDirectory(OutputDirectory);
        File.WriteAllLines(Path.Combine(OutputDirectory,"report.txt"),notes);
        Debug.Log("ROUTE AUDIT COMPLETE\n"+string.Join("\n",notes));
        enabled=false;
        Application.Quit(string.IsNullOrEmpty(failure)?0:2);
    }
}
