using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>Opt-in automated proof of the snowboard family pickup sequence.</summary>
[DefaultExecutionOrder(-900)]
public sealed class RiverValleySnowboardPickupAuditDriver : MonoBehaviour
{
    private const string OutputDirectory="/tmp/thelittles-snowboard-pickup-audit";
    private readonly List<string> notes=new();
    private int runtimeErrors;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartFromCommandLine()
    {
        if(!System.Environment.GetCommandLineArgs().Contains("-riverSnowboardPickupAudit"))return;
        GameObject runner=new("SNOWBOARD FAMILY PICKUP AUDIT — not gameplay");
        DontDestroyOnLoad(runner);
        runner.AddComponent<RiverValleySnowboardPickupAuditDriver>();
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
        if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;
        runtimeErrors++;
        notes.Add("Runtime error: "+message);
    }

    private IEnumerator Run()
    {
        yield return new WaitForSeconds(1f);
        RiverValleyGameDirector director=FindFirstObjectByType<RiverValleyGameDirector>();
        DannySpark danny=FindFirstObjectByType<DannySpark>();
        RiverValleyEncounter rail=FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None)
            .FirstOrDefault(item=>item!=null&&item.Kind==RiverEncounterKind.SkateDare&&
                item.RailStart!=null&&item.RailEnd!=null);
        if(director==null||danny==null||rail==null)
        { Finish("Director, Danny, or snowboard rail missing."); yield break; }

        RiverValleyMomChase mom=FindFirstObjectByType<RiverValleyMomChase>();
        if(mom!=null)mom.enabled=false;
        CharacterController controller=danny.GetComponent<CharacterController>();
        if(controller!=null)controller.enabled=false;
        danny.transform.position=rail.RailStart.position;
        if(controller!=null)controller.enabled=true;
        int before=director.RescuedKids;
        director.Resolve(rail);

        float deadline=Time.time+16f;
        bool slidingCaptureTaken=false;
        while(Time.time<deadline&&director.IsBusy)
        {
            if(!slidingCaptureTaken&&RiverValleyGameDirector.SnowboardFamiliesCollected>=2)
            {
                yield return new WaitForSeconds(0.25f);
                ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory,"01_family_sliding_behind_danny.png"));
                slidingCaptureTaken=true;
            }
            yield return null;
        }
        yield return new WaitForSeconds(0.7f);
        ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory,"02_children_surround_danny_at_bottom.png"));

        int offered=RiverValleyGameDirector.SnowboardFamiliesOffered;
        int collected=RiverValleyGameDirector.SnowboardFamiliesCollected;
        int children=RiverValleyGameDirector.SnowboardChildrenCollected;
        int atBottom=RiverValleyGameDirector.SnowboardChildrenAtBottom;
        int gained=director.RescuedKids-before;
        notes.Add($"Families offered {offered}; collected {collected}.");
        notes.Add($"Children collected {children}; at bottom {atBottom}; added to Danny's group {gained}.");
        notes.Add($"Ride finished {!director.IsBusy}; runtime errors {runtimeErrors}.");

        bool passed=offered==3&&collected==offered&&children>=3&&
            atBottom==children&&gained==children&&!director.IsBusy&&runtimeErrors==0;
        Finish(passed?"PASS":"FAIL");
    }

    private void Finish(string result)
    {
        notes.Insert(0,result);
        File.WriteAllLines(Path.Combine(OutputDirectory,"report.txt"),notes);
        Debug.Log($"SNOWBOARD PICKUP AUDIT: {result}");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(result=="PASS"?0:1);
#else
        Application.Quit(result=="PASS"?0:1);
#endif
    }
}
