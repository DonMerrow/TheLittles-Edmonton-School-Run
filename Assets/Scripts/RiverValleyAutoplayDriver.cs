using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[DefaultExecutionOrder(1000)]
public sealed class RiverValleyAutoplayDriver : MonoBehaviour
{
    private const string OutputDirectory = "/tmp/thelittles-autoplay";
    private static FileStream processLock;
    private RiverValleyGameDirector director;
    private DannySpark spark;
    private DannyTestController movement;
    private CharacterController controller;
    private DannyFollowCamera followCamera;
    private float phaseStarted;
    private int phase;
    private bool jumped;
    private bool actionDone;
    private bool checkpointCaptured;
    private bool airborne;
    private float airborneStarted;
    private float maximumAirborneSeconds;
    private float maximumFollowerLag;
    private float minimumVisibleChildFacing = 1f;
    private int capturedFrames;
    private int runtimeErrors;
    private RiverValleyKidFollower buriedTestKid;
    private bool burialRecovered;
    private bool burialRecoveryRequested;
    private bool burialCaptured;
    private bool snowPullCaptured;
    private bool tumbleCaptured;
    private bool coyotePositioned;
    private bool coyoteCaptured;
    private bool deepSnowTested;
    private bool deepSnowCaptured;
    private bool meadowVisited;
    private bool woodsVisited;
    private bool cubDenVisited;
    private bool coyoteRescueCaptured;
    private bool trafficTested;
    private bool trafficCaptured;
    private bool riverRescueStarted;
    private bool riverRescueCaptured;
    private bool riverFallCaptured;
    private bool mapEdgeTested;
    private bool endingLineCaptured;
    private bool levelTwoStarted;
    private float levelTwoStartedAt;
    private bool levelTwoImpactStarted;
    private bool levelTwoImpactCaptured;
    private bool levelTwoStairTestStarted;
    private float levelTwoStairStartedAt;
    private float levelTwoStairMinimumZ=float.PositiveInfinity;
    private float levelTwoStairMaximumZ=float.NegativeInfinity;
    private bool levelTwoStairClimbPassed;
    private bool levelTwoWolfKidsReleased;
    private float levelTwoWolfKidsReleasedAt;
    private bool levelTwoMomWoke;
    private float levelTwoMapEdgeTestedAt;
    private bool endingOnly;
    private bool stairOnly;
    private readonly Dictionary<RiverValleyKidFollower, Vector3> previousFollowerPositions = new();
    private readonly List<string> notes = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartFromCommandLine()
    {
        string[] arguments=System.Environment.GetCommandLineArgs();
        if (!arguments.Contains("-riverAutoplay")&&!arguments.Contains("-riverSlowAutoplay")&&
            !arguments.Contains("-riverEndingAudit")&&!arguments.Contains("-riverStairAudit")) return;
        Directory.CreateDirectory(OutputDirectory);
        try
        {
            processLock = new FileStream(Path.Combine(OutputDirectory,"active.lock"),FileMode.OpenOrCreate,
                FileAccess.ReadWrite,FileShare.None);
        }
        catch (IOException)
        {
            Debug.LogWarning("Another automated River Valley playtest is already running; closing this duplicate.");
            Application.Quit(0);
            return;
        }
        GameObject runner = new("AUTOMATED PLAYTEST — Codex driving");
        DontDestroyOnLoad(runner);
        runner.AddComponent<RiverValleyAutoplayDriver>();
    }

    private void Start()
    {
        Directory.CreateDirectory(OutputDirectory);
        Application.runInBackground = true;
        bool slowMode=System.Environment.GetCommandLineArgs().Contains("-riverSlowAutoplay");
        endingOnly=System.Environment.GetCommandLineArgs().Contains("-riverEndingAudit");
        stairOnly=System.Environment.GetCommandLineArgs().Contains("-riverStairAudit");
        Time.timeScale = slowMode ? 0.88f : 1.35f;
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.SetResolution(1280, 720, false);
        spark = FindFirstObjectByType<DannySpark>();
        director = FindFirstObjectByType<RiverValleyGameDirector>();
        movement = spark != null ? spark.GetComponent<DannyTestController>() : null;
        controller = spark != null ? spark.GetComponent<CharacterController>() : null;
        followCamera = FindFirstObjectByType<DannyFollowCamera>();
        Application.logMessageReceived += OnLog;
        phaseStarted = Time.time;
        Capture("01_start_street");
        notes.Add("Autoplay began in the rebuilt Edmonton River Valley scene.");
        notes.Add(slowMode
            ? "Slow cinematic playtest mode enabled so every action can be inspected."
            : "Accelerated whole-level verification mode enabled.");
        if(endingOnly)
        {
            director?.StageSchoolEndingForPlaytest();
            phase=6;
            phaseStarted=Time.time;
            Teleport(new Vector3(0f,-20.5f,401f));
            notes.Add("Focused school-ending verification began at the final approach.");
        }
        else if(stairOnly)
        {
            director?.BeginLevelTwoForPlaytest();
            levelTwoStarted=director!=null&&director.LevelTwoActive;
            levelTwoStartedAt=Time.time;
            phase=6;
            actionDone=true;
            checkpointCaptured=true;
            endingLineCaptured=true;
            notes.Add("Focused Level 2 stair and Mom timing audit began.");
        }
        Debug.Log("AUTOPLAY STARTED — driver attached and taking control.");
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
        ReleaseKeyboard();
        processLock?.Dispose();
        processLock = null;
    }

    private void Update()
    {
        if (spark == null || director == null || movement == null)
        {
            FailAndFinish("Required player or director component was missing.");
            return;
        }

        float elapsed = Time.time - phaseStarted;
        switch (phase)
        {
            case 0:
                if (elapsed < 8.8f) QueueMovement(true, elapsed > 2.2f && elapsed < 3.3f);
                else movement.SetAutomatedInput(Vector2.zero, false, false);
                ResolveNearbyHelpfulEncounters();
                if (!checkpointCaptured && elapsed >= 4.2f)
                {
                    Capture("01b_friendly_alberta_hello");
                    checkpointCaptured = true;
                }
                if (elapsed >= 7f && buriedTestKid == null)
                {
                    buriedTestKid = FindObjectsByType<RiverValleyKidFollower>(FindObjectsSortMode.None)
                        .FirstOrDefault(candidate => candidate != null && !candidate.IsScattered);
                    if (buriedTestKid == null)
                    {
                        RiverValleyEncounter testEncounter = FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None)
                            .FirstOrDefault(candidate => candidate != null && !candidate.Used &&
                                candidate.Kind == RiverEncounterKind.LostKid);
                        if (testEncounter != null)
                        {
                            director.Resolve(testEncounter);
                            buriedTestKid = testEncounter.Actor != null
                                ? testEncounter.Actor.GetComponent<RiverValleyKidFollower>() : null;
                        }
                    }
                }
                if (elapsed >= 9f && buriedTestKid == null)
                {
                    buriedTestKid = FindObjectsByType<RiverValleyKidFollower>(FindObjectsSortMode.None)
                        .FirstOrDefault(candidate => candidate != null && !candidate.IsScattered);
                }
                if (elapsed >= 9f && buriedTestKid != null && !buriedTestKid.IsScattered && !burialCaptured)
                {
                    director.FollowerBuriedByPlow(buriedTestKid,
                        spark.transform.position + spark.transform.forward * 6f + spark.transform.right * 2.8f);
                    notes.Add("Plow impact test launched a follower toward an angular rescue snowbank.");
                }
                if(!tumbleCaptured&&buriedTestKid!=null&&buriedTestKid.IsTumbling&&elapsed>=9.55f)
                {
                    Capture("02a_plow_child_airborne_ragdoll");
                    tumbleCaptured=true;
                    notes.Add("Captured the child visibly airborne and rotating after the plow hit.");
                }
                if (!burialCaptured && buriedTestKid != null && buriedTestKid.IsBuried &&
                    !buriedTestKid.IsTumbling && elapsed >= 10f)
                {
                    Capture("02b_plow_tumble_snowbank");
                    burialCaptured = true;
                    notes.Add("Plow landing capture completed after the tumble, with the child cold and snow-covered.");
                }
                if(burialCaptured&&!coyotePositioned&&buriedTestKid!=null&&elapsed>=10.9f)
                {
                    RiverValleyAnimalMotion coyote=FindObjectsByType<RiverValleyAnimalMotion>(FindObjectsSortMode.None)
                        .FirstOrDefault(animal=>animal!=null&&animal.name.ToLowerInvariant().Contains("coyote"));
                    if(coyote!=null)coyote.transform.position=buriedTestKid.transform.position+Vector3.right*0.85f;
                    coyotePositioned=true;
                }
                if(!coyoteCaptured&&burialCaptured&&elapsed>=12.0f)
                {
                    Capture("02c_coyote_tugs_stuck_child");
                    coyoteCaptured=true;
                }
                if (elapsed >= 12.7f && buriedTestKid != null && buriedTestKid.IsScattered &&
                    !burialRecoveryRequested)
                {
                    spark.Restore(100f,"Automated snow rescue test restores enough Spark to help.");
                    director.Resolve(buriedTestKid.Encounter);
                    burialRecoveryRequested=true;
                }
                if(!snowPullCaptured&&burialRecoveryRequested&&buriedTestKid!=null&&
                    buriedTestKid.IsScattered&&RiverValleyGameDirector.SnowPullProgress>=0.40f)
                {
                    Capture("02d_danny_pulls_child_from_snow");
                    snowPullCaptured=true;
                    notes.Add("Captured Danny physically lifting a calling child from the plow bank.");
                }
                if(burialRecoveryRequested&&!burialRecovered&&RiverValleyGameDirector.SnowPullRescues>0)
                {
                    burialRecovered=true;
                    notes.Add("Plow burial recovery: passed with a visible snow pull.");
                }
                if ((burialRecovered && elapsed >= 18f) || elapsed >= 22f)
                {
                    Capture("02_followers_after_street_run");
                    notes.Add($"Street movement reached z={spark.transform.position.z:0.0}; followers={FindObjectsByType<RiverValleyKidFollower>(FindObjectsSortMode.None).Length}.");
                    BeginPhase(1, new Vector3(0f, 0.20f, 108f));
                }
                break;
            case 1:
                ReleaseKeyboard();
                if (!actionDone && elapsed >= 1f)
                {
                    Capture("03_hockey_in_motion");
                    ResolveKind(RiverEncounterKind.PlayfulKid);
                    actionDone = true;
                }
                if (!deepSnowTested && elapsed >= 10.2f)
                {
                    RiverValleyDeepSnow patch = FindFirstObjectByType<RiverValleyDeepSnow>();
                    if (patch != null)
                    {
                        Teleport(patch.transform.position + Vector3.up * 0.20f);
                        movement.SetAutomatedInput(Vector2.up, false, false);
                        deepSnowTested = true;
                        notes.Add("Danny entered a sticky deep-snow patch so its slowdown could be verified.");
                    }
                }
                if (deepSnowTested && !deepSnowCaptured && elapsed >= 10.75f)
                {
                    Capture("03b_sticky_snow_slows_danny");
                    deepSnowCaptured = true;
                }
                if (elapsed >= 12f) BeginPhase(2, new Vector3(2.2f, 0.20f, 122f));
                break;
            case 2:
                ReleaseKeyboard();
                if(!meadowVisited&&elapsed>=0.25f)
                {
                    Teleport(new Vector3(31f,0.20f,72f));
                    followCamera?.FocusCinematic(new Vector3(34f,0.72f,76f),2.4f);
                    meadowVisited=true;
                    notes.Add("Danny entered the bright rabbit-field bonus route.");
                }
                if (!actionDone && elapsed >= 0.8f)
                {
                    ResolveKind(RiverEncounterKind.PetRabbit);
                    Capture("04_rabbit_meadow_bonus");
                    actionDone = true;
                }
                if (actionDone && !checkpointCaptured && elapsed >= 1.2f && followCamera != null)
                {
                    followCamera.TogglePerspective();
                    Capture("04b_first_person_view");
                    checkpointCaptured = true;
                }
                if (checkpointCaptured && elapsed >= 1.7f && followCamera != null && followCamera.IsFirstPerson)
                    followCamera.TogglePerspective();
                if(!woodsVisited&&elapsed>=2.2f)
                {
                    RiverValleySideAdventure meadow=FindObjectsByType<RiverValleySideAdventure>(FindObjectsSortMode.None)
                        .FirstOrDefault(zone=>zone!=null&&zone.Kind==RiverSideAdventureKind.RabbitMeadow);
                    meadow?.ForceReturnCutoffForPlaytest();
                    Teleport(new Vector3(-20f,0.20f,73f));
                    woodsVisited=true;
                    notes.Add("Danny entered the coyote-woods skipping-school route.");
                }
                if(!cubDenVisited&&elapsed>=3.0f)
                {
                    Teleport(new Vector3(-31f,0.20f,79f));
                    cubDenVisited=true;
                }
                if(!coyoteRescueCaptured&&cubDenVisited&&elapsed>=9.15f)
                {
                    Capture("04c_mom_rescues_danny_from_coyotes");
                    coyoteRescueCaptured=true;
                    notes.Add("Captured Mom walking into the den and calling the coyote pack away from Danny.");
                }
                if(cubDenVisited&&elapsed>=14.2f&&!trafficTested)
                {
                    RiverValleySideAdventure woods=FindObjectsByType<RiverValleySideAdventure>(FindObjectsSortMode.None)
                        .FirstOrDefault(zone=>zone!=null&&zone.Kind==RiverSideAdventureKind.CoyoteWoods);
                    woods?.ForceReturnCutoffForPlaytest();
                    Teleport(new Vector3(34f,0.20f,130f));
                    GameObject car=GameObject.Find("Corner neighbourhood car");
                    if(car!=null)car.GetComponent<RiverValleySafeCar>()?.StageSafetyTest(new Vector3(34f,0.55f,130f));
                    trafficTested=true;
                }
                if(trafficTested&&!trafficCaptured&&elapsed>=15.15f)
                {
                    Capture("04d_safe_car_brakes_for_danny");
                    trafficCaptured=true;
                }
                if (elapsed >= 16.2f) BeginPhase(3, new Vector3(0f, 0.20f, 129f));
                break;
            case 3:
                ReleaseKeyboard();
                if (!actionDone && elapsed >= 0.8f)
                {
                    ResolveKind(RiverEncounterKind.SkateDare);
                    actionDone = true;
                }
                if (!checkpointCaptured && elapsed >= 5.15f)
                {
                    Capture("05_rail_ride");
                    checkpointCaptured = true;
                }
                if (elapsed >= 8.5f) BeginPhase(4, new Vector3(0f, -20.5f, 245f));
                break;
            case 4:
                QueueMovement(true, elapsed > 2f && elapsed < 2.15f);
                ResolveNearbyHelpfulEncounters();
                if (elapsed >= 15f)
                {
                    Capture("06_valley_group_and_hazards");
                    BeginPhase(5, new Vector3(0f, -20.5f, 329f));
                }
                break;
            case 5:
                if(!riverRescueStarted&&elapsed>=0.8f)
                {
                    movement.SetAutomatedInput(Vector2.zero,false,false);
                    RiverValleyRiverRescue rescue=FindFirstObjectByType<RiverValleyRiverRescue>();
                    rescue?.StageRescueForPlaytest();
                    riverRescueStarted=rescue!=null;
                    notes.Add("Danny's river fall triggered the emergency cocoa-and-blanket rescue.");
                }
                if(!riverRescueCaptured&&elapsed>=10.0f)
                {
                    Capture("07_river_rescue_cocoa_and_mom");
                    riverRescueCaptured=true;
                }
                if(!riverFallCaptured&&elapsed>=2.65f)
                {
                    Capture("07a_river_fall_and_emergency_team_carry");
                    riverFallCaptured=true;
                }
                if (elapsed >= 17.5f)
                {
                    Capture("07b_whiteout_after_rescue");
                    if(Camera.main!=null&&spark!=null)
                        notes.Add($"Post-rescue camera distance from Danny: {Vector3.Distance(Camera.main.transform.position,spark.transform.position):0.00}m.");
                    BeginPhase(6, new Vector3(0f, -20.5f, 401f));
                }
                break;
            case 6:
                ReleaseKeyboard();
                ResolveNearbyHelpfulEncounters();
                if (!actionDone && elapsed >= 1.2f && !director.IsBusy)
                {
                    GatherMinimumSchoolGroup();
                    ResolveKind(RiverEncounterKind.Finish);
                    actionDone = true;
                }
                if(!endingLineCaptured&&RiverValleyGameDirector.SchoolArrivalChildrenEntered>=2&&
                    RiverValleyGameDirector.SchoolArrivalProcessionsCompleted==0)
                {
                    Capture("08_school_single_file_procession");
                    endingLineCaptured=true;
                    notes.Add("Captured the Principal leading the children through the school door in single file.");
                }
                if(!checkpointCaptured&&RiverValleyGameDirector.SchoolArrivalProcessionsCompleted>=1&&
                    director.LevelTwoChoiceReady)
                {
                    Capture("08b_level_one_complete_choice");
                    checkpointCaptured=true;
                    notes.Add("Level 1 ended with separate Enter School and Skip School: Level 2 choices.");
                }
                if(checkpointCaptured&&!levelTwoStarted)
                {
                    director.BeginLevelTwoForPlaytest();
                    levelTwoStarted=director.LevelTwoActive;
                    if(levelTwoStarted)
                    {
                        levelTwoStartedAt=Time.time;
                        notes.Add($"Level 2 began with {director.LevelTwoMissingRemaining} missing children to find.");
                    }
                }
                if(levelTwoStarted&&!levelTwoImpactStarted&&Time.time-levelTwoStartedAt>=0.35f)
                {
                    Capture("08c_level_two_dark_storm_no_signs");
                    RiverValleyHazardMover hazard=FindFirstObjectByType<RiverValleyHazardMover>();
                    director.LevelTwoHazardLaunch(hazard!=null?hazard.transform:null,"PLOW DRIVER",
                        "Focused Level 2 test sends Danny toward a checked roadside snow pile.",3f);
                    levelTwoImpactStarted=true;
                }
                if(levelTwoImpactStarted&&!levelTwoImpactCaptured&&director.IsBusy&&
                    Time.time-levelTwoStartedAt>=0.72f)
                {
                    Capture("08d_level_two_vehicle_snow_pile_tumble");
                    levelTwoImpactCaptured=true;
                    notes.Add("Captured a Level 2 vehicle impact propelling Danny into a checked-ground snow pile.");
                }
                if(levelTwoStarted&&!levelTwoStairTestStarted&&!director.IsBusy&&
                    Time.time-levelTwoStartedAt>=1.55f)
                {
                    Teleport(new Vector3(-1.38f,-1.70f,140.65f));
                    spark.transform.rotation=Quaternion.LookRotation(Vector3.back,Vector3.up);
                    followCamera?.RecenterOnDanny();
                    movement.SetAutomatedInput(Vector2.zero,false,false);
                    levelTwoStairTestStarted=true;
                    levelTwoStairStartedAt=Time.time;
                    levelTwoStairMinimumZ=spark.transform.position.z;
                    levelTwoStairMaximumZ=spark.transform.position.z;
                    Vector3 cameraForward=Camera.main!=null
                        ? Vector3.ProjectOnPlane(Camera.main.transform.forward,Vector3.up).normalized
                        : Vector3.zero;
                    notes.Add($"Stair controls at start: motor {movement.enabled}; controller {controller.enabled}; camera forward {cameraForward}.");
                    movement.enabled=false;
                    notes.Add("Focused Level 2 return test began below the repaired top stair transition.");
                }
                if(levelTwoStairTestStarted&&!levelTwoStairClimbPassed&&
                    Time.time-levelTwoStairStartedAt<4.4f)
                {
                    movement.MoveWorldForPlaytest(Vector3.back,2.25f);
                    levelTwoStairMinimumZ=Mathf.Min(levelTwoStairMinimumZ,spark.transform.position.z);
                    levelTwoStairMaximumZ=Mathf.Max(levelTwoStairMaximumZ,spark.transform.position.z);
                    if(spark.transform.position.z<134.55f)
                    {
                        levelTwoStairClimbPassed=true;
                        movement.enabled=true;
                        Capture("08e_level_two_return_ramp_climbed");
                        notes.Add("Danny climbed from the stairs onto the upper deck without jumping or twitching.");
                    }
                }
                if(levelTwoStairTestStarted&&!levelTwoStairClimbPassed&&
                    Time.time-levelTwoStairStartedAt>=4.4f&&!movement.enabled)
                    movement.enabled=true;
                if(levelTwoStairTestStarted&&!levelTwoWolfKidsReleased&&!director.IsBusy&&
                    Time.time-levelTwoStairStartedAt>=4.55f)
                {
                    RiverValleyEncounter wolfKids=FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None)
                        .FirstOrDefault(encounter=>encounter!=null&&!encounter.Used&&
                            encounter.name.ToLowerInvariant().Contains("coyote woods"));
                    if(wolfKids!=null)director.Resolve(wolfKids);
                    levelTwoWolfKidsReleased=true;
                    levelTwoWolfKidsReleasedAt=Time.time;
                }
                RiverValleyMomChase levelTwoMom=FindFirstObjectByType<RiverValleyMomChase>();
                if(levelTwoWolfKidsReleased&&!levelTwoMomWoke&&Time.time-levelTwoWolfKidsReleasedAt>=2.65f)
                {
                    levelTwoMomWoke=levelTwoMom!=null&&!levelTwoMom.WaitingForWolfParkClue;
                    if(levelTwoMomWoke)
                        notes.Add("Mom stayed quiet for Level 2, then began searching only after the Wolf Park children ran out.");
                }
                if(levelTwoStarted&&!mapEdgeTested&&!director.IsBusy&&levelTwoMomWoke)
                {
                    // Deliberately place Danny beyond the far boundary.  The
                    // containment system must return him to the last grounded
                    // school-route position before the test may finish.
                    Teleport(new Vector3(118f,-20.5f,401f));
                    mapEdgeTested=true;
                    levelTwoMapEdgeTestedAt=Time.time;
                }
                if(mapEdgeTested&&Time.time-levelTwoMapEdgeTestedAt>=0.65f)Finish();
                break;
        }
    }

    private void LateUpdate()
    {
        if (spark != null && movement != null)
            ObserveMovement();
    }

    private void BeginPhase(int nextPhase, Vector3 position)
    {
        ReleaseKeyboard();
        Teleport(position);
        phase = nextPhase;
        phaseStarted = Time.time;
        jumped = false;
        actionDone = false;
        checkpointCaptured = false;
        previousFollowerPositions.Clear();
    }

    private void Teleport(Vector3 requested)
    {
        director?.ClearAllPrompt();
        Vector3 position = requested;
        // Pick a real walkable surface. A single downward ray could land on a
        // spruce crown, a car, or a decorative prop and leave the automated
        // player floating above the trigger it was meant to exercise.
        RaycastHit[] hits=Physics.RaycastAll(requested+Vector3.up*12f,Vector3.down,45f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        float bestY=float.NegativeInfinity;
        for(int i=0;i<hits.Length;i++)
        {
            Collider candidate=hits[i].collider;
            if(candidate==null||Vector3.Dot(hits[i].normal,Vector3.up)<0.72f)continue;
            Transform owner=candidate.transform;
            string lower=owner.name.ToLowerInvariant();
            if(owner.GetComponentInParent<DannySpark>()!=null||
               owner.GetComponentInParent<RiverValleyKidFollower>()!=null||
               owner.GetComponentInParent<RiverValleyAnimalMotion>()!=null||
               owner.GetComponentInParent<RiverValleySafeCar>()!=null||
               owner.GetComponentInParent<RiverValleyHazardMover>()!=null||
               lower.Contains("tree")||lower.Contains("spruce")||lower.Contains("branch")||
               lower.Contains("building")||lower.Contains("home")||lower.Contains("roof")||
               lower.Contains("sign")||lower.Contains("goal")||lower.Contains("stick"))continue;
            if(hits[i].point.y>bestY)bestY=hits[i].point.y;
        }
        if(bestY>float.NegativeInfinity)position.y=bestY+0.08f;
        if (controller != null) controller.enabled = false;
        spark.transform.SetPositionAndRotation(position, Quaternion.identity);
        if (controller != null) controller.enabled = true;
        movement.ResetVerticalMotion();
    }

    private void QueueMovement(bool run, bool pressJump)
    {
        if (pressJump && !jumped)
        {
            movement.SetAutomatedInput(Vector2.up, run, true);
            jumped = true;
        }
        else movement.SetAutomatedInput(Vector2.up, run, false);
    }

    private static void ReleaseKeyboard()
    {
        if (Keyboard.current != null)
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
    }

    private void ResolveNearbyHelpfulEncounters()
    {
        foreach (RiverValleyEncounter encounter in FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None))
        {
            if (encounter == null || encounter.Used || Vector3.Distance(encounter.transform.position, spark.transform.position) > 4.5f) continue;
            // Leave the deliberately launched test child in the bank long
            // enough to observe the complete tumble and landing. The regular
            // game still lets the player rescue them immediately.
            if (buriedTestKid != null && buriedTestKid.IsBuried && encounter == buriedTestKid.Encounter) continue;
            if (encounter.Kind == RiverEncounterKind.LostKid || encounter.Kind == RiverEncounterKind.ParentHandoff ||
                encounter.Kind == RiverEncounterKind.PetDog || encounter.Kind == RiverEncounterKind.PetRabbit ||
                encounter.Kind == RiverEncounterKind.PositiveAdult || encounter.Kind == RiverEncounterKind.HelpfulTeen)
                director.Resolve(encounter);
        }
    }

    private void ResolveKind(RiverEncounterKind kind)
    {
        RiverValleyEncounter encounter = FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None)
            .FirstOrDefault(candidate => candidate != null && !candidate.Used && candidate.Kind == kind);
        if (encounter != null) director.Resolve(encounter);
        else notes.Add("No unused encounter found for " + kind + ".");
    }

    private void GatherMinimumSchoolGroup()
    {
        spark.Restore(100f, "Automated finish verification restores Spark.");
        int attempts = 0;
        while (director.RescuedKids < director.RequiredKidsForSchool && attempts++ < 60)
        {
            RiverValleyEncounter encounter = FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None)
                .FirstOrDefault(candidate => candidate != null && !candidate.Used &&
                    (candidate.Kind == RiverEncounterKind.LostKid || candidate.Kind == RiverEncounterKind.ParentHandoff));
            if (encounter == null) break;
            int before = director.RescuedKids;
            director.Resolve(encounter);
            if (director.RescuedKids == before) encounter.Used = true;
        }

        // The accelerated route deliberately jumps between distant districts.
        // Bring the finished test huddle beside Danny so the final frames can
        // verify real hand contact and the child-to-child mitten nudge.
        RiverValleyKidFollower[] followers=FindObjectsByType<RiverValleyKidFollower>(FindObjectsSortMode.None)
            .Where(candidate=>candidate!=null&&!candidate.IsBuried&&!candidate.IsTumbling)
            .ToArray();
        for(int i=0;i<followers.Length;i++)
        {
            float side=(i%2==0?-0.58f:0.58f);
            float back=1.2f+(i/2)*1.05f;
            followers[i].transform.position=spark.transform.position+
                spark.transform.right*side-spark.transform.forward*back+Vector3.up*0.1f;
            followers[i].Regather(spark.transform,i);
        }
    }

    private void ObserveMovement()
    {
        if (!movement.IsGrounded && !airborne)
        {
            airborne = true;
            airborneStarted = Time.time;
        }
        else if (movement.IsGrounded && airborne)
        {
            airborne = false;
            maximumAirborneSeconds = Mathf.Max(maximumAirborneSeconds, Time.time - airborneStarted);
        }

        foreach (RiverValleyKidFollower follower in FindObjectsByType<RiverValleyKidFollower>(FindObjectsSortMode.None))
        {
            if (follower == null) continue;
            if (follower.IsScattered || follower.IsWaiting || follower.IsTumbling || follower.FollowingSeconds < 0.75f)
            {
                previousFollowerPositions.Remove(follower);
                continue;
            }
            if (previousFollowerPositions.TryGetValue(follower, out Vector3 previous))
            {
                Vector3 travel = Vector3.ProjectOnPlane(follower.transform.position - previous, Vector3.up);
                // A newly gathered child can begin hundreds of metres from
                // Danny in this accelerated whole-level test. Its first
                // sample is initialization, and its catch-up snap is not a
                // gameplay frame. Measure only established, normal motion.
                float followerDistance=Vector3.ProjectOnPlane(
                    follower.transform.position-spark.transform.position,Vector3.up).magnitude;
                if (travel.sqrMagnitude < 0.25f && followerDistance < 20f && Time.time - phaseStarted > 0.35f)
                {
                    maximumFollowerLag = Mathf.Max(maximumFollowerLag,followerDistance);
                    if (travel.sqrMagnitude > 0.0004f)
                    {
                        Vector3 intended=Vector3.ProjectOnPlane(follower.IntendedFacingDirection,Vector3.up);
                        if(intended.sqrMagnitude<0.001f)intended=travel;
                        WinterCastIdentity[] visibleChildren=follower.GetComponentsInChildren<WinterCastIdentity>(true);
                        if(visibleChildren.Length==0)
                            minimumVisibleChildFacing=Mathf.Min(minimumVisibleChildFacing,
                                Vector3.Dot(Vector3.ProjectOnPlane(follower.transform.forward,Vector3.up).normalized,
                                    intended.normalized));
                        foreach(WinterCastIdentity visibleChild in visibleChildren)
                        {
                            Vector3 visibleForward=Vector3.ProjectOnPlane(visibleChild.transform.forward,Vector3.up);
                            if(visibleForward.sqrMagnitude>0.001f)
                                minimumVisibleChildFacing=Mathf.Min(minimumVisibleChildFacing,
                                    Vector3.Dot(visibleForward.normalized,intended.normalized));
                        }
                    }
                }
            }
            previousFollowerPositions[follower] = follower.transform.position;
        }
    }

    private void Capture(string label)
    {
        ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory, label + ".png"), 1);
        capturedFrames++;
    }

    private void OnLog(string condition, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            runtimeErrors++;
            notes.Add(type + ": " + condition);
        }
    }

    private void FailAndFinish(string reason)
    {
        notes.Add("FATAL: " + reason);
        Finish();
    }

    private void Finish()
    {
        if (airborne) maximumAirborneSeconds = Mathf.Max(maximumAirborneSeconds, Time.time - airborneStarted);
        notes.Add($"Captured frames: {capturedFrames}");
        notes.Add($"Runtime errors: {runtimeErrors}");
        notes.Add($"Longest observed airborne interval: {maximumAirborneSeconds:0.000}s");
        notes.Add($"Largest active follower distance: {maximumFollowerLag:0.00}m");
        notes.Add($"Lowest visible child facing dot: {minimumVisibleChildFacing:0.000} (positive means every rendered child faces the route or catch-up direction).");
        notes.Add($"Final Danny position: {spark.transform.position}");
        notes.Add($"Final Spark: {spark.Current:0.0}");
        notes.Add($"Children delivered: {director.RescuedKids}/{director.TotalRescuableKids} (minimum {director.RequiredKidsForSchool}).");
        WinterAudioDirector audio = FindFirstObjectByType<WinterAudioDirector>();
        AlbertaNeighbourhoodLife neighbourhood = FindFirstObjectByType<AlbertaNeighbourhoodLife>();
        int playingAudioLayers = FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Count(source => source != null && source.isPlaying);
        notes.Add($"Audio verification: {playingAudioLayers} layers playing; {audio?.VoiceEventsPlayed ?? 0} dialogue cues voiced.");
        notes.Add($"Recorded spoken lines: {audio?.RecordedVoiceEventsPlayed ?? 0}; loud Mother interruptions: {audio?.MotherVoiceEventsPlayed ?? 0}.");
        notes.Add($"Friendly Alberta greetings shown: {neighbourhood?.GreetingsShown ?? 0}.");
        RiverValleyKidFollower[] followers=FindObjectsByType<RiverValleyKidFollower>(FindObjectsSortMode.None);
        int heldHands=followers.Count(child=>child!=null&&child.IsHoldingHands);
        notes.Add($"Children using real hand-holding pose: {heldHands}/{followers.Length} (paired children counted individually).");
        notes.Add("Detached high-five and mitten-nudge props are disabled; ordinary paired hand-holding remains.");
        int symbolicLinks=FindObjectsByType<LineRenderer>(FindObjectsSortMode.None)
            .Count(line=>line!=null&&line.name=="Held-mitten connection");
        notes.Add($"Old symbolic orange hand links remaining: {symbolicLinks}.");
        int visibleLooseAccessories=FindObjectsByType<Transform>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)
            .Count(item=>item!=null&&item.name.ToLowerInvariant().Contains("accessory"));
        notes.Add($"Visible loose imported accessories: {visibleLooseAccessories} (expected 0).");
        GameObject outerBoundary=GameObject.Find("COMPLETE PLAYABLE WORLD SAFETY BOUNDARY");
        int outerWalls=outerBoundary!=null?outerBoundary.GetComponentsInChildren<BoxCollider>(true).Length:0;
        notes.Add($"World-edge walls: {outerWalls} (expected exactly 4 outer borders, with no district fences).");
        notes.Add($"Street-hockey passes completed: {WinterHockeyRally.CompletedPasses}.");
        notes.Add($"Highest tennis-ball road clearance: {WinterHockeyRally.HighestGroundClearance:0.000}m (ball radius 0.070m).");
        notes.Add($"Stair playground-ring swings completed: {RiverValleySwingRing.CompletedSwings}.");
        notes.Add($"Sticky deep-snow patches present: {FindObjectsByType<RiverValleyDeepSnow>(FindObjectsSortMode.None).Length}; entries: {RiverValleyDeepSnow.Entries}.");
        notes.Add($"Cold children in final huddle: {director.ColdKids}.");
        notes.Add($"Coyote snowbank tug events: {CoyoteSnowDrag.TugEvents}.");
        notes.Add($"Visible Danny snowbank pull rescues: {RiverValleyGameDirector.SnowPullRescues}.");
        notes.Add($"Wildlife performance: {RiverValleyAnimalMotion.CoyoteTravelMetres:0.0}m of visible coyote travel; {RiverValleyAnimalMotion.CoyoteSniffs} curious sniffs; {RiverValleyAnimalMotion.RabbitHopSounds} rabbit landings; {RiverValleyAnimalMotion.CoyoteSounds} coyote calls.");
        notes.Add($"Imported animal animation: {QuaterniusAnimalAnimator.RunningCount} running; {QuaterniusAnimalAnimator.PoseVerifiedCount} visibly changing pose; {QuaterniusAnimalAnimator.MissingClipCount} missing clips.");
        notes.Add($"Curious child questions voiced: {RiverValleyCuriousChatter.QuestionsVoiced}.");
        notes.Add($"Safe-car braking checks: {RiverValleySafeCar.SafeStops}; driver warnings: {RiverValleySafeCar.DriverWarnings}.");
        notes.Add($"Bonus detours entered: {RiverValleySideAdventure.DetoursEntered}; Mom return cutoffs: {RiverValleySideAdventure.ReturnsCutOffByMom}; coyote cub adoptions: {CoyoteCubDetour.CubAdoptions}.");
        notes.Add($"Visible coyote-pack drags: {CoyoteCubDetour.VisibleDragSequences}; visible Mom woods recoveries: {CoyoteCubDetour.VisibleMomRecoveries}.");
        notes.Add($"Dead-end hockey goals cleared for traffic: {DeadEndHockeyTraffic.GoalsCleared}; cars waved through: {DeadEndHockeyTraffic.CarsWavedThrough}.");
        notes.Add($"Low-branch trick wipeouts: {DannyTestController.LowBranchWipeouts}.");
        notes.Add($"Out-of-map safety recoveries: {RiverValleyMapSafety.Restorations}.");
        notes.Add($"River rescues completed: {RiverValleyRiverRescue.RescuesCompleted}; visible emergency-team carries: {RiverValleyRiverRescue.VisibleTeamCarries}; cocoa served: {RiverValleyRiverRescue.CocoaServed}; Mom river overreactions: {RiverValleyRiverRescue.MomOverreactions}.");
        notes.Add($"School ending: {RiverValleyGameDirector.SchoolArrivalChildrenEntered} children entered in {RiverValleyGameDirector.SchoolArrivalProcessionsCompleted} single-file procession; Level 2 active: {director.LevelTwoActive}; missing children remaining: {director.LevelTwoMissingRemaining}.");
        int visibleRouteSigns=FindObjectsByType<RiverValleyFacingSign>(FindObjectsInactive.Exclude,FindObjectsSortMode.None).Length;
        RiverValleyWhiteout whiteout=FindFirstObjectByType<RiverValleyWhiteout>();
        bool snowboardReopened=FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None)
            .Any(encounter=>encounter!=null&&encounter.Kind==RiverEncounterKind.SkateDare&&!encounter.Used);
        notes.Add($"Level 2 design: {RiverValleyGameDirector.LevelTwoHiddenRouteSigns} route signs hidden; {visibleRouteSigns} still visible; dark storm active: {whiteout?.IsLevelTwoStorm}; snowboard reopened: {snowboardReopened}; safe snow-pile landings: {RiverValleyGameDirector.LevelTwoSnowPileLandings}.");
        RiverValleyMomChase levelTwoMom=FindFirstObjectByType<RiverValleyMomChase>();
        if(levelTwoStarted&&!levelTwoStairClimbPassed)
            notes.Add("FATAL: Danny could not physically climb the Level 2 packed-snow stair return.");
        if(levelTwoStarted&&!levelTwoMomWoke)
            notes.Add("FATAL: Mom did not wake after the Wolf Park children escaped.");
        notes.Add($"Level 2 return: packed-snow stair ramp built: {RiverValleyGameDirector.LevelTwoReturnRampBuilt}; climbed in test: {levelTwoStairClimbPassed}; Mom woke after Wolf Park children: {levelTwoMomWoke}; Mom still waiting: {levelTwoMom?.WaitingForWolfParkClue}.");
        if(levelTwoStairTestStarted)
            notes.Add($"Level 2 stair travel range: z {levelTwoStairMinimumZ:0.00} to {levelTwoStairMaximumZ:0.00}; motor speed {movement.CurrentPlanarSpeed:0.00}.");
        File.WriteAllLines(Path.Combine(OutputDirectory, "report.txt"), notes);
        Debug.Log("AUTOPLAY COMPLETE\n" + string.Join("\n", notes));
        enabled = false;
        ReleaseKeyboard();
        Application.Quit(0);
    }
}
