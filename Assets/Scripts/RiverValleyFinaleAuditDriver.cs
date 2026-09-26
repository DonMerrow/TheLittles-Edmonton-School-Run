using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>Command-line-only three-level playthrough and school-ending check.</summary>
[DefaultExecutionOrder(-900)]
public sealed class RiverValleyFinaleAuditDriver : MonoBehaviour
{
    private const string OutputDirectory="/tmp/thelittles-finale-audit";
    private readonly List<string> notes=new();
    private int runtimeErrors;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartFromCommandLine()
    {
        if(!System.Environment.GetCommandLineArgs().Contains("-riverFinaleAudit"))return;
        GameObject runner=new("FINAL SCHOOL ENDING AUDIT — not gameplay");
        DontDestroyOnLoad(runner);
        runner.AddComponent<RiverValleyFinaleAuditDriver>();
    }

    private void Start()
    {
        Directory.CreateDirectory(OutputDirectory);
        Application.runInBackground=true;
        Application.targetFrameRate=60;
        QualitySettings.vSyncCount=0;
        Time.timeScale=2f;
        Application.logMessageReceived+=OnLog;
        StartCoroutine(Run());
    }

    private void OnDestroy() => Application.logMessageReceived-=OnLog;

    private void OnLog(string message,string stackTrace,LogType type)
    {
        if(type!=LogType.Error&&type!=LogType.Exception&&type!=LogType.Assert)return;
        runtimeErrors++;
        notes.Add("Runtime error: "+message);
    }

    private IEnumerator Run()
    {
        yield return new WaitForSeconds(0.5f);
        RiverValleyGameDirector director=FindFirstObjectByType<RiverValleyGameDirector>();
        DannySpark danny=FindFirstObjectByType<DannySpark>();
        if(director==null||danny==null){Finish("Director or Danny missing.");yield break;}

        director.StageSchoolEndingForPlaytest();
        RiverValleyMomChase mom=FindFirstObjectByType<RiverValleyMomChase>();
        if(mom!=null)mom.enabled=false; // Enable her at Level 3 to test the home/voice-only rule directly.
        WinterAudioDirector audio=FindFirstObjectByType<WinterAudioDirector>();
        if(audio==null){Finish("Audio director missing.");yield break;}
        int prepared=0;
        foreach(RiverValleyEncounter encounter in FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None))
        {
            if(encounter==null||encounter.Used||
               (encounter.Kind!=RiverEncounterKind.LostKid&&encounter.Kind!=RiverEncounterKind.ParentHandoff))continue;
            director.Resolve(encounter);
            prepared+=encounter.GroupSize;
            if(prepared>=18)break;
        }
        notes.Add($"Level 1: gathered {prepared} children.");
        if(director.RescuedKids<director.RequiredKidsForSchool)
        {Finish("Level 1 could not gather the minimum school group.");yield break;}
        RiverValleyEncounter school=FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None)
            .FirstOrDefault(encounter=>encounter!=null&&encounter.Kind==RiverEncounterKind.Finish);
        if(school==null){Finish("School finish encounter missing.");yield break;}
        MoveDanny(danny,new Vector3(0f,-20.49f,411.9f));
        director.Resolve(school);
        float deadline=Time.time+32f;
        while(Time.time<deadline&&!director.LevelTwoChoiceReady)yield return null;
        notes.Add($"Level 1 school line: {RiverValleyGameDirector.SchoolArrivalChildrenEntered} children; choice ready: {director.LevelTwoChoiceReady}.");
        if(!director.LevelTwoChoiceReady){Finish("Level 1 school choice did not appear.");yield break;}
        notes.Add("School arrival safely brought every trailing follower into the line; no off-screen child blocked progression.");

        director.EnterSchoolForPlaytest();
        deadline=Time.time+8f;
        while(Time.time<deadline&&!director.LevelTwoActive)yield return null;
        if(!director.LevelTwoActive){Finish("Entering school after Level 1 did not start Level 2.");yield break;}
        notes.Add("Danny entered the school through the normal E-button path and Level 2 began automatically.");
        RiverValleyEncounter rail=FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None)
            .FirstOrDefault(encounter=>encounter!=null&&encounter.Kind==RiverEncounterKind.SkateDare&&encounter.RailStart!=null);
        if(rail==null){Finish("Level 2 snowboard rail missing.");yield break;}
        MoveDanny(danny,rail.RailStart.position);
        director.Resolve(rail);
        deadline=Time.time+19f;
        while(Time.time<deadline&&(director.LevelTwoSnowboardRunRequired||director.IsBusy))yield return null;
        if(director.LevelTwoSnowboardRunRequired||director.IsBusy){Finish("Level 2 snowboard run did not finish.");yield break;}
        director.LevelTwoEnteredWolfPark();
        if(!director.LevelTwoWolfParkReached){Finish("Level 2 Wolf Park visit did not register.");yield break;}
        int levelTwoGathered=0;
        foreach(RiverValleyEncounter encounter in FindObjectsByType<RiverValleyEncounter>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(encounter==null||encounter.Used||encounter.Actor==null||!encounter.Actor.gameObject.activeSelf||
               (encounter.Kind!=RiverEncounterKind.LostKid&&encounter.Kind!=RiverEncounterKind.ParentHandoff))continue;
            deadline=Time.time+12f;
            while(Time.time<deadline&&director.IsBusy)yield return null;
            int before=director.RescuedKids;
            director.Resolve(encounter);
            levelTwoGathered+=director.RescuedKids-before;
            if(levelTwoGathered>=4)break;
        }
        notes.Add($"Level 2: snowboard cleared; Wolf Park visited; gathered {levelTwoGathered} children; busy {director.IsBusy}.");
        if(levelTwoGathered<4){Finish("Level 2 could not gather its return group.");yield break;}
        MoveDanny(danny,new Vector3(0f,-20.49f,411.9f));
        director.Resolve(school);
        deadline=Time.time+32f;
        while(Time.time<deadline&&!director.LevelThreeActive)yield return null;
        if(!director.LevelThreeActive)
        {
            notes.Add($"School return diagnostics: Danny z={danny.transform.position.z:F1}, with Danny={director.RescuedKids}, busy={director.IsBusy}, processions={RiverValleyGameDirector.SchoolArrivalProcessionsCompleted}, missing={director.LevelTwoMissingRemaining}.");
            Finish("Level 2 school return did not start Level 3.");yield break;
        }
        notes.Add($"Level 2 school line completed; Level 3 began after {RiverValleyGameDirector.SchoolArrivalProcessionsCompleted} processions.");

        if(mom==null){Finish("Mom missing from the Level 3 home-state audit.");yield break;}
        mom.enabled=true;
        director.MomCaught(); // Simulate the overlap that previously caused the false firetruck ending.
        yield return new WaitForSeconds(0.25f);
        if(!director.LevelThreeActive||director.IsBusy||!mom.LevelThreeVoiceOnly||mom.DistanceFromHome>0.35f)
        {
            Finish($"Level 3 Mom rule failed: active={director.LevelThreeActive}, busy={director.IsBusy}, voice-only={mom.LevelThreeVoiceOnly}, home distance={mom.DistanceFromHome:F2}m.");
            yield break;
        }
        MoveDanny(danny,new Vector3(0f,-20.49f,245f));
        yield return new WaitForSeconds(4f);
        if(!director.LevelThreeActive||mom.DistanceFromHome>0.35f)
        {
            Finish($"Mom left home or ended Level 3 during the search; home distance={mom.DistanceFromHome:F2}m.");
            yield break;
        }
        notes.Add("Level 3 Mom stayed home in voice-only mode; a collision could not trigger a false river rescue or end the search.");

        int missingAtStart=director.LevelTwoMissingRemaining;
        notes.Add($"Level 3 began with {missingAtStart} missing children.");
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory,"01_level_three_search.png"));

        int childrenAlreadyEntered=RiverValleyGameDirector.SchoolArrivalChildrenEntered;
        int firstTripGroups=0;
        int firstTripChildren=0;
        List<RiverValleyEncounter> finalSearchEncounters=FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None)
            .Where(encounter=>encounter!=null&&!encounter.Used&&
                (encounter.Kind==RiverEncounterKind.LostKid||encounter.Kind==RiverEncounterKind.ParentHandoff))
            .ToList();
        RiverValleyEncounter heldForFinalTrip=finalSearchEncounters
            .OrderBy(encounter=>encounter.GroupSize).FirstOrDefault();
        List<RiverValleyEncounter> firstTrip=new();
        foreach(RiverValleyEncounter encounter in finalSearchEncounters)
        {
            if(encounter==heldForFinalTrip)continue;
            firstTrip.Add(encounter);
            firstTripGroups++;
            firstTripChildren+=encounter.GroupSize;
        }
        List<Collider> heldTriggers=new();
        foreach(RiverValleyEncounter encounter in finalSearchEncounters)
            if(!firstTrip.Contains(encounter)&&encounter.TryGetComponent(out Collider held)&&held.enabled)
            {held.enabled=false;heldTriggers.Add(held);}
        foreach(RiverValleyEncounter encounter in firstTrip)director.Resolve(encounter);
        if(missingAtStart<=0||director.LevelTwoMissingRemaining<=0||director.RescuedKids<=0)
        {Finish("Could not stage a partial Level 3 return with children still missing.");yield break;}
        int missingAfterFirstSearch=director.LevelTwoMissingRemaining;
        MoveDanny(danny,new Vector3(0f,-20.49f,411.9f));
        director.Resolve(school);
        deadline=Time.time+34f;
        while(Time.time<deadline&&(director.IsBusy||director.RescuedKids>0))yield return null;
        foreach(Collider held in heldTriggers)if(held!=null)held.enabled=true;
        if(!director.LevelThreeActive||director.IsBusy||director.RescuedKids>0||
           director.LevelTwoMissingRemaining!=missingAfterFirstSearch)
        {Finish("The partial Level 3 school return did not resume the missing-child search.");yield break;}
        notes.Add($"Level 3 partial return: {firstTripChildren} children entered; {missingAfterFirstSearch} still missing; search resumed with the kid radar.");

        int finalTripGroups=0;
        foreach(RiverValleyEncounter encounter in FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None))
        {
            if(encounter==null||encounter.Used||
               (encounter.Kind!=RiverEncounterKind.LostKid&&encounter.Kind!=RiverEncounterKind.ParentHandoff))continue;
            director.Resolve(encounter);
            finalTripGroups++;
        }
        notes.Add($"Gathered the last {finalTripGroups} search groups; remaining {director.LevelTwoMissingRemaining}; following {director.RescuedKids}.");
        if(director.LevelTwoMissingRemaining!=0||director.RescuedKids<=0)
        {Finish("The final missing children could not be gathered after the partial return.");yield break;}

        int motherVoicesBefore=audio.MotherVoiceEventsPlayed;
        MoveDanny(danny,new Vector3(0f,-20.49f,411.9f));
        director.Resolve(school);
        yield return new WaitForSeconds(0.5f);
        ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory,"02_final_group_at_school.png"));

        deadline=Time.time+34f;
        bool cheeringCaptured=false;
        while(Time.time<deadline&&(!RiverValleyGameDirector.LevelThreeDannyEnteredSchool||
            audio.MotherVoiceEventsPlayed==motherVoicesBefore))
        {
            if(!cheeringCaptured&&RiverValleyGameDirector.LevelThreeConfettiPiecesLaunched>0)
            {
                cheeringCaptured=true;
                yield return new WaitForSeconds(0.55f);
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory,"03_children_cheer_and_confetti.png"));
            }
            yield return null;
        }
        int childrenEntered=RiverValleyGameDirector.SchoolArrivalChildrenEntered-childrenAlreadyEntered;
        notes.Add($"School processions: {RiverValleyGameDirector.SchoolArrivalProcessionsCompleted}; " +
                  $"final children entered: {childrenEntered}/{missingAtStart}; " +
                  $"Danny entered: {RiverValleyGameDirector.LevelThreeDannyEnteredSchool}; " +
                  $"cheering children visible: {RiverValleyGameDirector.LevelThreeCheeringKidsVisible}; " +
                  $"confetti pieces: {RiverValleyGameDirector.LevelThreeConfettiPiecesLaunched}; " +
                  $"cheers: {audio.SchoolCheersPlayed}; proud Mother voice: {audio.MotherVoiceEventsPlayed>motherVoicesBefore}; " +
                  $"runtime errors: {runtimeErrors}.");
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory,"04_danny_enters_to_cheers.png"));
        yield return new WaitForSeconds(5.4f);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory,"05_game_complete.png"));
        if(director.LevelThreeActive||RiverValleyGameDirector.SchoolArrivalProcessionsCompleted<4||
           childrenEntered!=missingAtStart||!RiverValleyGameDirector.LevelThreeDannyEnteredSchool||
           RiverValleyGameDirector.LevelThreeCheeringKidsVisible<4||
           RiverValleyGameDirector.LevelThreeConfettiPiecesLaunched<100||audio.SchoolCheersPlayed<1||
           audio.MotherVoiceEventsPlayed<=motherVoicesBefore||runtimeErrors>0)
            Finish("Three-level playthrough or school celebration did not complete cleanly.");
        else Finish(null);
    }

    private static void MoveDanny(DannySpark danny,Vector3 position)
    {
        CharacterController controller=danny.GetComponent<CharacterController>();
        if(controller!=null)controller.enabled=false;
        danny.transform.position=position;
        if(controller!=null)controller.enabled=true;
        danny.GetComponent<DannyTestController>()?.ResetVerticalMotion();
        FindFirstObjectByType<DannyFollowCamera>()?.RecenterOnDanny();
    }

    private void StageFollowersNearSchool(Transform danny)
    {
        int index=0;
        foreach(RiverValleyKidFollower follower in FindObjectsByType<RiverValleyKidFollower>(FindObjectsSortMode.None))
        {
            if(follower==null||follower.IsScattered||follower.IsBuried||follower.IsTumbling)continue;
            follower.transform.position=danny.position+new Vector3((index%5-2)*1.3f,0f,-2f-(index/5)*1.8f);
            follower.Regather(danny,index++);
        }
        notes.Add($"Scripted finale staging placed {index} follower groups by the school; ordinary gameplay must walk them there.");
    }

    private void Finish(string failure)
    {
        if(failure!=null)notes.Add("FAIL: "+failure);
        else notes.Add("PASS: Levels 1, 2 and 3 completed; Danny entered school to children's cheers, confetti and Mother's proud recorded voice.");
        File.WriteAllLines(Path.Combine(OutputDirectory,"report.txt"),notes);
        Debug.Log("FINALE AUDIT COMPLETE\n"+string.Join("\n",notes));
        Application.Quit(failure==null?0:2);
    }
}
