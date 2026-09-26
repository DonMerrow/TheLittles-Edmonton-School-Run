using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>Opt-in visual and grounding check for Danny's home block.</summary>
public sealed class RiverValleyOpeningAuditDriver : MonoBehaviour
{
    private const string OutputDirectory="/tmp/thelittles-opening-audit";
    private readonly List<string> notes=new();
    private int errors;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartFromCommandLine()
    {
        if(!System.Environment.GetCommandLineArgs().Contains("-riverOpeningAudit"))return;
        GameObject runner=new("OPENING NEIGHBOURHOOD AUDIT — not gameplay");
        DontDestroyOnLoad(runner);
        runner.AddComponent<RiverValleyOpeningAuditDriver>();
    }

    private void Start()
    {
        Directory.CreateDirectory(OutputDirectory);
        Application.runInBackground=true;
        Application.targetFrameRate=60;
        QualitySettings.vSyncCount=0;
        Application.logMessageReceived+=OnLog;
        StartCoroutine(Run());
    }

    private void OnDestroy()=>Application.logMessageReceived-=OnLog;

    private void OnLog(string message,string stack,LogType type)
    {
        if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)
        { errors++; notes.Add("Runtime error: "+message); }
    }

    private IEnumerator Run()
    {
        yield return new WaitForSeconds(2f);
        GameObject home=GameObject.Find("Danny and Mom's home");
        RiverValleyMomChase mom=FindFirstObjectByType<RiverValleyMomChase>();
        DannySpark danny=FindFirstObjectByType<DannySpark>();
        RiverValleyNeighbourResident[] neighbours=FindObjectsByType<RiverValleyNeighbourResident>(FindObjectsSortMode.None);
        BoxCollider[] borders=FindObjectsByType<BoxCollider>(FindObjectsSortMode.None)
            .Where(c=>c.name.Contains("outside edge")).ToArray();
        notes.Add($"Home: {home!=null}; Mom: {mom!=null}; neighbours: {neighbours.Length}; world-edge colliders: {borders.Length}.");
        int unsafeHouses=0;
        foreach(Transform building in FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if(!building.name.Contains("home")||building.parent==null||
                building.GetComponentInChildren<WinterCastIdentity>(true)!=null)continue;
            Renderer[] pieces=building.GetComponentsInChildren<Renderer>(true);
            if(pieces.Length==0)continue;
            Bounds footprint=pieces[0].bounds;
            for(int i=1;i<pieces.Length;i++)footprint.Encapsulate(pieces[i].bounds);
            bool mainSidewalk=footprint.max.x>-2.1f&&footprint.min.x<2.1f&&
                footprint.max.z>-10f&&footprint.min.z<135f;
            bool approachSidewalk=footprint.max.x>-74f&&footprint.min.x<1f&&
                footprint.max.z>-7.1f&&footprint.min.z<-2.9f;
            bool firstCornerStreet=footprint.max.x>29.75f&&footprint.min.x<38.25f&&
                footprint.max.z>105f&&footprint.min.z<155f;
            bool secondCornerStreet=footprint.max.x>56.75f&&footprint.min.x<65.25f&&
                footprint.max.z>108f&&footprint.min.z<152f;
            if(mainSidewalk||approachSidewalk||firstCornerStreet||secondCornerStreet)
            { unsafeHouses++; notes.Add($"House crowds a sidewalk: {building.name}, {footprint.min:F1}..{footprint.max:F1}."); }
        }
        int unsupportedEdgeSamples=0;
        foreach(Vector3 sample in new[]{new Vector3(-95f,0f,20f),new Vector3(95f,0f,20f),
            new Vector3(-95f,0f,105f),new Vector3(95f,0f,105f),new Vector3(0f,0f,-47f)})
            if(!Physics.Raycast(sample+Vector3.up*2f,Vector3.down,5f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                unsupportedEdgeSamples++;
        notes.Add($"Houses on sidewalks: {unsafeHouses}; unsupported upper-edge samples: {unsupportedEdgeSamples}.");
        int floatingGuardPieces=FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Count(part=>part.GetComponentInParent<WinterCastVisualPolish>(true)!=null&&
                part.gameObject.activeInHierarchy&&
                (part.name.ToLowerInvariant().Contains("stopsign")||
                 part.name.ToLowerInvariant().Contains("stoppole")));
        notes.Add($"Active rigid crossing-guard sign pieces: {floatingGuardPieces}.");
        int floating=0;
        foreach(RiverValleyNeighbourResident child in neighbours)
        {
            Renderer[] renderers=child.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0)continue;
            float feet=renderers.Min(r=>r.bounds.min.y);
            RaycastHit[] hits=Physics.RaycastAll(child.transform.position+Vector3.up*3f,Vector3.down,10f);
            float ground=float.NegativeInfinity;
            foreach(RaycastHit hit in hits)
                if(hit.collider!=null&&hit.normal.y>0.55f&&
                    !hit.collider.transform.IsChildOf(child.transform)&&
                    !hit.collider.name.ToLowerInvariant().Contains("home"))
                    ground=Mathf.Max(ground,hit.point.y);
            if(ground>float.NegativeInfinity&&Mathf.Abs(feet-ground)>0.22f)
            { floating++; notes.Add($"Floating {child.name}: feet {feet:F2}, ground {ground:F2}."); }
        }
        notes.Add($"Neighbour foot-height failures: {floating}.");
        ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory,"01_home_and_waiting_kids.png"));
        yield return new WaitForSeconds(1f);
        if(danny!=null)
        {
            CharacterController controller=danny.GetComponent<CharacterController>();
            DannyTestController movement=danny.GetComponent<DannyTestController>();
            if(movement!=null)movement.enabled=false;
            if(controller!=null)controller.enabled=false;
            danny.transform.position=new Vector3(-66f,0.08f,-5f);
            danny.transform.rotation=Quaternion.Euler(0f,232f,0f);
            if(controller!=null)controller.enabled=true;
            Camera.main?.GetComponent<DannyFollowCamera>()?.Configure(danny.transform);
        }
        yield return new WaitForSeconds(2f);
        ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory,"01b_danny_and_mom_home.png"));
        if(danny!=null)
        {
            CharacterController controller=danny.GetComponent<CharacterController>();
            if(controller!=null)controller.enabled=false;
            danny.transform.position=new Vector3(-49f,0.08f,-5f);
            danny.transform.rotation=Quaternion.Euler(0f,270f,0f);
            if(controller!=null)controller.enabled=true;
            Camera.main?.GetComponent<DannyFollowCamera>()?.Configure(danny.transform);
        }
        yield return new WaitForSeconds(8f);
        notes.Add($"Mom after Danny leaves: {(mom!=null?mom.transform.position.ToString("F2"):"missing")}.");
        ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory,"02_mom_comes_out.png"));
        yield return new WaitForSeconds(1f);
        if(danny!=null)
        {
            CharacterController controller=danny.GetComponent<CharacterController>();
            if(controller!=null)controller.enabled=false;
            danny.transform.position=new Vector3(-25f,0.08f,-39f);
            danny.transform.rotation=Quaternion.Euler(0f,180f,0f);
            if(controller!=null)controller.enabled=true;
        }
        yield return new WaitForSeconds(2f);
        int backdrops=borders.Count(c=>c.transform.Find("City edge backdrop")!=null);
        notes.Add($"City backdrops on physical world edges: {backdrops}/{borders.Length}; runtime errors: {errors}.");
        ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory,"03_city_edge.png"));
        yield return new WaitForSeconds(0.5f);
        if(danny!=null)
        {
            CharacterController controller=danny.GetComponent<CharacterController>();
            if(controller!=null)controller.enabled=false;
            danny.transform.position=new Vector3(104f,0.08f,15f);
            if(controller!=null)controller.enabled=true;
        }
        yield return new WaitForSeconds(0.5f);
        bool edgeRecovered=danny!=null&&danny.transform.position.x<101f;
        notes.Add($"Danny recovered from a forced outside-edge position: {edgeRecovered}.");
        WinterAudioDirector audio=FindFirstObjectByType<WinterAudioDirector>();
        notes.Add($"Recorded voice events during opening: {(audio!=null?audio.RecordedVoiceEventsPlayed:0)}.");
        File.WriteAllLines(Path.Combine(OutputDirectory,"report.txt"),notes);
        Application.Quit((home!=null&&mom!=null&&neighbours.Length>=6&&floating==0&&
            unsafeHouses==0&&unsupportedEdgeSamples==0&&floatingGuardPieces==0&&
            borders.Length==4&&backdrops==4&&edgeRecovered&&errors==0)?0:2);
    }
}
