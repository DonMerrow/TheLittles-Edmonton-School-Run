using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class RiverValleyGameDirector : MonoBehaviour
{
    public static int SnowPullRescues { get; private set; }
    public static float SnowPullProgress { get; private set; }
    public static int SchoolArrivalChildrenEntered { get; private set; }
    public static int SchoolArrivalProcessionsCompleted { get; private set; }
    public static bool LevelThreeDannyEnteredSchool { get; private set; }
    public static int LevelThreeConfettiPiecesLaunched { get; private set; }
    public static int LevelThreeCheeringKidsVisible { get; private set; }
    public static int LevelTwoHiddenRouteSigns { get; private set; }
    public static int LevelTwoSnowPileLandings { get; private set; }
    public static bool LevelTwoReturnRampBuilt { get; private set; }
    public static int SnowboardFamiliesOffered { get; private set; }
    public static int SnowboardFamiliesCollected { get; private set; }
    public static int SnowboardChildrenCollected { get; private set; }
    public static int SnowboardChildrenAtBottom { get; private set; }
    [SerializeField] private Transform player;
    [SerializeField] private DannySpark spark;
    [SerializeField] private DannyTestController movement;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private RiverValleyWhiteout whiteout;
    [SerializeField] private RiverValleyMomChase mom;
    [SerializeField] private float routeStartZ = -5f;
    [SerializeField] private float routeFinishZ = 418f;
    private string speaker = "DANNY";
    private string message = "Danny is a little guy doing his best while Mom keeps tabs on him. In Edmonton, neighbours help neighbours, strangers and visitors—so Danny helps every child get safely to school. Edmonton and Danny: we remember you.";
    private string prompt;
    private RiverValleyEncounter promptOwner;
    private float messageUntil = float.PositiveInfinity;
    private int rescuedKids;
    private int knownKids;
    private int totalRescuableKids;
    private int requiredKidsForSchool;
    private int momCatches;
    private int momCallouts;
    private int hockeyCards = 3;
    private int streetCred;
    private float nextTimeCred = 42f;
    private float nextStormPressure = 14f;
    private float nextColdGroupPressure = 18f;
    private bool helpedNeighbour;
    private bool hockeyPlayed;
    private bool snowboardCompleted;
    private bool dogPetted;
    private bool rabbitPetted;
    private bool groupWaiting;
    private Transform waitPost;
    private float waitStarted;
    private string groupPrompt;
    private Transform groupPromptOwner;
    private bool busy;
    private bool recoveringSpark;
    private bool groupScattered;
    private bool finished;
    private bool schoolChoiceActive;
    private bool levelTwoActive;
    private bool levelTwoFinaleStarted;
    private float nextSchoolWaitMessage;
    private bool levelTwoSnowboardRunRequired;
    private bool levelTwoWolfParkReached;
    private int levelOneDelivered;
    private int levelTwoTargetCount;
    private int levelTwoRequiredDelivery;
    private int levelTwoDelivered;
    private bool levelThreeActive;
    private bool finalSchoolCelebrating;
    private int levelThreeTargetCount;
    private int levelThreeDelivered;
    private RiverValleyEncounter schoolFinishEncounter;
    private readonly List<RiverValleyKidFollower> kids = new();
    private readonly List<Transform> schoolCelebrationChildren = new();
    private readonly Dictionary<RiverValleyEncounter,Vector3> encounterStartingPositions = new();
    private RiverValleyEncounter[] allEncounters;
    private WinterAudioDirector audioDirector;
    private Vector3 checkpoint;
    private Quaternion checkpointRotation;
    private GUIStyle titleStyle;
    private GUIStyle statusStyle;
    private GUIStyle objectiveStyle;
    private GUIStyle bodyStyle;
    private GUIStyle promptStyle;
    private float controlsUntil;
    private float nextLevelTwoLaunch;

    private sealed class SnowboardFamilyPickup
    {
        public RiverValleyEncounter Encounter;
        public Transform Actor;
        public Vector3 OriginalPosition;
        public Quaternion OriginalRotation;
        public float Gate;
        public bool Collected;
        public int TrailDelay;
        public GameObject Board;
        public Behaviour[] PausedBehaviours;
        public bool[] PausedBehaviourStates;
    }

    public Transform Player => player;
    public float Progress
    {
        get
        {
            if(finished)return 1f;
            if(player==null)return 0f;
            float travelled=player.position.z<1.5f&&player.position.x<2.5f
                ? Mathf.Clamp(player.position.x+66f,0f,68f)
                : 66f+Mathf.Max(0f,player.position.z-routeStartZ);
            return Mathf.Clamp01(travelled/Mathf.Max(1f,66f+routeFinishZ-routeStartZ));
        }
    }
    public int RescuedKids => rescuedKids;
    public bool IsBusy => busy;
    public void SetExternalCinematicBusy(bool value) => busy = value;
    public int TotalRescuableKids => totalRescuableKids;
    public int RequiredKidsForSchool => requiredKidsForSchool;
    public bool LevelTwoChoiceReady => schoolChoiceActive;
    public bool LevelTwoActive => levelTwoActive;
    public bool LevelTwoSnowboardRunRequired => levelTwoSnowboardRunRequired;
    public bool LevelTwoWolfParkReached => levelTwoWolfParkReached;
    public int LevelTwoTargetCount => levelTwoTargetCount;
    public int LevelTwoMissingRemaining => CountMissingChildren();
    public bool LevelThreeActive => levelThreeActive;
    public bool HasHelpAction => promptOwner != null || schoolChoiceActive;
    public bool HasChoiceAction => schoolChoiceActive || !string.IsNullOrEmpty(groupPrompt);
    public int ColdKids
    {
        get
        {
            int count = 0;
            foreach (RiverValleyKidFollower kid in kids)
                if (kid != null && kid.IsCold && !kid.IsScattered)
                    count += kid.Encounter != null ? kid.Encounter.GroupSize : 1;
            return count;
        }
    }

    private void Start()
    {
        SchoolArrivalChildrenEntered=0;
        SchoolArrivalProcessionsCompleted=0;
        LevelThreeDannyEnteredSchool=false;
        LevelThreeConfettiPiecesLaunched=0;
        LevelThreeCheeringKidsVisible=0;
        LevelTwoHiddenRouteSigns=0;
        LevelTwoSnowPileLandings=0;
        LevelTwoReturnRampBuilt=false;
        SnowboardFamiliesOffered=0;
        SnowboardFamiliesCollected=0;
        SnowboardChildrenCollected=0;
        SnowboardChildrenAtBottom=0;
        schoolCelebrationChildren.Clear();
        if (player == null)
        {
            DannySpark found = FindFirstObjectByType<DannySpark>();
            if (found != null) player = found.transform;
        }
        if (spark == null && player != null) spark = player.GetComponent<DannySpark>();
        if (movement == null && player != null) movement = player.GetComponent<DannyTestController>();
        if (characterController == null && player != null) characterController = player.GetComponent<CharacterController>();
        allEncounters = FindObjectsByType<RiverValleyEncounter>(FindObjectsSortMode.None);
        audioDirector = FindFirstObjectByType<WinterAudioDirector>();
        foreach (RiverValleyEncounter encounter in allEncounters)
        {
            if(encounter!=null)
                encounterStartingPositions[encounter]=encounter.Actor!=null
                    ? encounter.Actor.position : encounter.transform.position-Vector3.up;
            if (encounter != null && (encounter.Kind == RiverEncounterKind.LostKid ||
                encounter.Kind == RiverEncounterKind.ParentHandoff))
                totalRescuableKids += encounter.GroupSize;
        }
        requiredKidsForSchool = Mathf.Min(12, totalRescuableKids);
        controlsUntil = Time.time + 24f;
        ActivateAmbientCast();
        if (GetComponent<RiverValleyWildlifeDirector>() == null)
            gameObject.AddComponent<RiverValleyWildlifeDirector>();
        if (GetComponent<RiverValleyOpeningNeighbourhood>() == null)
            gameObject.AddComponent<RiverValleyOpeningNeighbourhood>();
        foreach (RiverValleyHazardMover hazard in FindObjectsByType<RiverValleyHazardMover>())
            if (hazard != null && hazard.name.ToLowerInvariant().Contains("plow") &&
                hazard.GetComponent<RiverValleyPlowPolish>() == null)
                hazard.gameObject.AddComponent<RiverValleyPlowPolish>();
        checkpoint = player != null ? player.position : Vector3.zero;
        checkpointRotation = player != null ? player.rotation : Quaternion.identity;
        if (spark != null) spark.SparkChanged += OnSparkChanged;
        speaker="DANNY'S STORY";
        messageUntil=Time.time+12f;
    }

    private void OnDestroy()
    {
        if (spark != null) spark.SparkChanged -= OnSparkChanged;
    }

    private void ActivateAmbientCast()
    {
        // Give the named neighbourhood cast a little life while keeping
        // grouped school children and scripted chasers under their own motors.
        WinterCastIdentity[] cast = FindObjectsByType<WinterCastIdentity>();
        foreach (WinterCastIdentity identity in cast)
        {
            if (identity == null || identity.transform.parent != transform ||
                identity.GetComponent<RiverValleyMomChase>() != null ||
                identity.GetComponent<WinterCastActivity>() != null ||
                identity.GetComponentInParent<RiverValleyParentWalker>() != null ||
                identity.GetComponent<RiverValleyAmbientActor>() != null)
                continue;
            identity.gameObject.AddComponent<RiverValleyAmbientActor>();
        }
    }

    private void Update()
    {
        if(schoolChoiceActive&&!busy)
        {
            Keyboard keyboard=Keyboard.current;
            Gamepad gamepad=Gamepad.current;
            if((keyboard!=null&&keyboard.eKey.wasPressedThisFrame)||
               (gamepad!=null&&gamepad.buttonSouth.wasPressedThisFrame)||
               RiverValleyMobileControls.ActionPressed)
                StartCoroutine(DannyEntersSchool());
            else if((keyboard!=null&&keyboard.qKey.wasPressedThisFrame)||
                    (gamepad!=null&&gamepad.buttonWest.wasPressedThisFrame)||
                    RiverValleyMobileControls.SecondaryPressed)
                BeginLevelTwoAsHooky();
        }
        if((levelTwoActive||levelThreeActive)&&!levelTwoFinaleStarted&&!busy&&rescuedKids>0&&
            !groupWaiting&&player!=null&&player.position.z>=routeFinishZ-12f)
        {
            BringFollowingChildrenToSchoolDoor();
            if(CanDeliverAtSchool(out int nearChildren,out int trailingChildren))
            {
                levelTwoFinaleStarted=true;
                StartCoroutine(SchoolArrival(schoolFinishEncounter,true,
                    levelThreeActive?"Level 3 Search Team":"Missing-Kid Return"));
            }
            else if(Time.time>=nextSchoolWaitMessage)
            {
                Show("DANNY",$"Wait at the school for the group: {nearChildren} here, {trailingChildren} still behind. Nobody goes in without them.",5f);
                nextSchoolWaitMessage=Time.time+7f;
            }
        }
        if(groupPrompt!=null&&(groupPromptOwner==null||player==null||
            Vector3.Distance(player.position,groupPromptOwner.position)>5.5f))
        {
            groupPrompt=null;
            groupPromptOwner=null;
        }
        if(RiverValleyMobileControls.ActionPressed&&!busy&&!schoolChoiceActive&&
            promptOwner==null&&string.IsNullOrEmpty(groupPrompt))
        {
            string hint=levelThreeActive
                ? "Follow the CHILDREN direction at the top. Walk close to a missing child, then HELP brings them into the group. The flashlight moves wildlife away."
                : levelTwoActive
                    ? "Follow the CHILDREN direction at the top. Walk close to a child or neighbour; HELP will light up when Danny can assist them."
                    : "Follow the CHILDREN direction at the top. Walk close to neighbours and children; HELP will light up when Danny can do something kind.";
            Show("HELP",hint,5.5f);
        }
        if (Time.time >= nextTimeCred && !busy && movement != null && movement.CurrentPlanarSpeed >= 0.35f)
        {
            nextTimeCred = Time.time + 42f;
            streetCred++;
        }
        if (!busy && whiteout != null && whiteout.Intensity >= 0.52f &&
            Time.time >= nextStormPressure)
        {
            nextStormPressure = Time.time + Mathf.Lerp(14f, 9f, whiteout.Intensity);
            float groupPressure = 1f + Mathf.Min(1f, rescuedKids / 16f) * 0.45f;
            float drain = Mathf.Lerp(2.5f, 5f, whiteout.Intensity) * groupPressure;
            if(levelThreeActive)drain*=0.72f;
            spark?.Drain(drain, "Keeping the group together in the whiteout costs Spark.");
        }
        if (!busy && ColdKids > 0 && Time.time >= nextColdGroupPressure)
        {
            nextColdGroupPressure = Time.time + 12f;
            float coldDrain=Mathf.Min(4.5f,1.5f+ColdKids*0.32f);
            if(levelThreeActive)coldDrain*=0.72f;
            spark?.Drain(coldDrain,
                "Wet mittens and shivering children turn the last stretch into a final run.");
            Show("FINAL RUN", $"{ColdKids} cold {(ColdKids == 1 ? "child needs" : "children need")} the warm school. Keep the huddle moving!", 4f);
        }
    }

    private void OnSparkChanged(float normalized, string reason)
    {
        if (normalized <= 0.001f && !busy && !recoveringSpark)
        {
            recoveringSpark = true;
            spark.Restore(levelThreeActive?22f:18f, "Danny stops, breathes, and notices one interesting snowflake.");
            Show("DANNY", levelThreeActive
                ? "Everything went grey. The flashlight is all I have left—find the last children."
                : "Everything went grey—but one snowflake is shaped like a tiny dinosaur. Start there.", 5f);
            recoveringSpark = false;
        }
    }

    public void Resolve(RiverValleyEncounter encounter)
    {
        if (encounter == null || encounter.Used || busy || spark == null) return;
        ClearPrompt(encounter);
        bool rescueEncounter=encounter.Kind==RiverEncounterKind.LostKid||
            encounter.Kind==RiverEncounterKind.ParentHandoff;
        if (rescueEncounter&&!levelTwoActive&&!levelThreeActive&&spark.Current<encounter.MinimumSpark)
        {
            Show("DANNY", "I want to help, but my Spark is too low. I need one good moment first.", 4.5f);
            return;
        }
        if (rescueEncounter&&!levelTwoActive&&!levelThreeActive&&streetCred<=-6)
        {
            Show("CHILD", "The group heard Danny keeps losing people. Help someone, play well, or rebuild Street Cred first.", 5f);
            return;
        }

        encounter.Used = true;
        switch (encounter.Kind)
        {
            case RiverEncounterKind.MomEvasion:
                checkpoint = player.position;
                checkpointRotation = player.rotation;
                spark.Restore(encounter.SparkChange, encounter.Line);
                mom?.GiveHeadStart(7f, 26f);
                Show(encounter.Speaker, encounter.Line, 4.5f);
                break;
            case RiverEncounterKind.LostKid:
            case RiverEncounterKind.ParentHandoff:
                RiverValleyKidFollower existing = encounter.Actor != null
                    ? encounter.Actor.GetComponent<RiverValleyKidFollower>() : null;
                rescuedKids += encounter.GroupSize;
                if (existing != null && existing.IsScattered)
                {
                    bool wasBuried = existing.IsBuried;
                    if (wasBuried)
                    {
                        StartCoroutine(PullChildFromSnow(existing));
                        break;
                    }
                    existing.Regather(player, Mathf.Max(0, kids.IndexOf(existing)));
                    spark.Restore(9f, "Danny gathers the group again.");
                    groupScattered = rescuedKids < knownKids;
                    Show("DANNY", (levelTwoActive||levelThreeActive)
                        ? $"School's this way—follow me. Nobody gets left in the dark. {rescuedKids} with Danny; {CountMissingChildren()} still missing."
                        : $"Back together. Stay close to the orange hood. Kids together: {rescuedKids}/{knownKids}", 5f);
                    if(IsWolfParkChildEncounter(encounter))StartCoroutine(MomNoticesWolfParkChildren());
                    break;
                }
                knownKids += encounter.GroupSize;
                spark.Restore(encounter.SparkChange, encounter.Line);
                if (encounter.Actor != null)
                {
                    RiverValleyKidFollower follower = existing ?? encounter.Actor.gameObject.AddComponent<RiverValleyKidFollower>();
                    follower.Configure(player, Mathf.Max(0, kids.Count), encounter);
                    if (!kids.Contains(follower)) kids.Add(follower);
                }
                groupScattered = false;
                Show((levelTwoActive||levelThreeActive)?"DANNY":encounter.Speaker, (levelTwoActive||levelThreeActive)
                    ? $"School's this way—follow me. Stay close to the orange hood. {rescuedKids} with Danny; {CountMissingChildren()} still missing."
                    : encounter.Line + $"  Kids together: {rescuedKids}/{knownKids}", 5f);
                if(IsWolfParkChildEncounter(encounter))StartCoroutine(MomNoticesWolfParkChildren());
                break;
            case RiverEncounterKind.PlayfulKid:
                StartCoroutine(HockeyGame(encounter));
                break;
            case RiverEncounterKind.SkateDare:
                if(encounter.RailStart!=null&&player!=null&&
                    Vector3.Distance(player.position,encounter.RailStart.position)>9f)
                {
                    encounter.Used=false;
                    Show("DANNY","The snowboard dare is at the stair rail. I am not teleporting across Edmonton to do it.",4.5f);
                    break;
                }
                if (hockeyCards <= 0)
                {
                    encounter.Used = false;
                    Show("DANNY", "I cannot stake a card I do not have. That is not confidence; that is accounting.", 4.5f);
                    break;
                }
                if (encounter.RailStart != null && encounter.RailEnd != null)
                    StartCoroutine(RailRide(encounter));
                break;
            case RiverEncounterKind.PetRabbit:
                rabbitPetted = true;
                spark.Restore(Mathf.Abs(encounter.SparkChange), "Danny pets a winter rabbit.");
                streetCred += 1;
                Show(encounter.Speaker, encounter.Line, 4.5f);
                break;
            case RiverEncounterKind.PetDog:
                dogPetted = true;
                spark.Restore(Mathf.Abs(encounter.SparkChange), "Danny pets a dog on the school route.");
                Show(encounter.Speaker, encounter.Line, 4.5f);
                break;
            case RiverEncounterKind.CoyoteScare:
                StartCoroutine(CoyoteMoment(encounter));
                break;
            case RiverEncounterKind.GiveLunch:
                helpedNeighbour = true;
                streetCred += 6;
                spark.Restore(Mathf.Abs(encounter.SparkChange), "Danny helps a neighbour with his lunch.");
                Show(encounter.Speaker, encounter.Line + "  Street Cred +6.", 6f);
                break;
            case RiverEncounterKind.HelperRescue:
                StartCoroutine(HelperRescue(encounter));
                break;
            case RiverEncounterKind.HelpfulTeen:
                spark.Restore(Mathf.Abs(encounter.SparkChange), "A helpful teenager keeps the smaller kids together.");
                mom?.GiveHeadStart(8f, 32f);
                streetCred += 2;
                Show(encounter.Speaker, encounter.Line, 5f);
                break;
            case RiverEncounterKind.TalkativeAdult:
                StartCoroutine(EndlessAdultTalk(encounter));
                break;
            case RiverEncounterKind.MomCall:
                spark.Drain(Mathf.Abs(encounter.SparkChange), encounter.Line);
                mom?.GiveHeadStart(13f, 38f);
                Show(encounter.Speaker, encounter.Line + "  The other children try very hard not to laugh. They fail.", 6f);
                break;
            case RiverEncounterKind.TeenAdvice:
            case RiverEncounterKind.CrowEcho:
                spark.Drain(Mathf.Abs(encounter.SparkChange), encounter.Line);
                Show(encounter.Speaker, encounter.Line, 5f);
                break;
            case RiverEncounterKind.Finish:
                BringFollowingChildrenToSchoolDoor();
                if(levelTwoActive||levelThreeActive)
                {
                    if(rescuedKids<=0)
                    {
                        encounter.Used=false;
                        Show("PRINCIPAL","Find the children still outside, then bring the group back through this door.",6f);
                        break;
                    }
                    if(!CanDeliverAtSchool(out int laterNear,out int laterTrailing))
                    {
                        encounter.Used=false;
                        Show("PRINCIPAL",$"I can see {laterNear} children here, but {laterTrailing} are still behind. Wait for the whole group.",7f);
                        break;
                    }
                    levelTwoFinaleStarted=true;
                    StartCoroutine(SchoolArrival(encounter,true,
                        levelThreeActive?"Level 3 Search Team":"Missing-Kid Return"));
                    break;
                }
                if (rescuedKids < requiredKidsForSchool)
                {
                    encounter.Used = false;
                    Show("PRINCIPAL", $"I can see the school group is still scattered through the storm. Bring at least {requiredKidsForSchool} children safely here. You have {rescuedKids}.", 7f);
                    break;
                }
                if(!CanDeliverAtSchool(out int nearChildren,out int trailingChildren))
                {
                    encounter.Used=false;
                    Show("PRINCIPAL",$"I can see {nearChildren} children here, but {trailingChildren} are still behind. Wait together or go back for them.",7f);
                    break;
                }
                // Hockey, snowboarding and animal moments are rewarding side
                // adventures. They must never silently block the school-day
                // ending after Danny has safely delivered enough children.
                spark.Restore(25f, encounter.Line);
                string rank = rescuedKids >= 40 ? "River Valley Legend" : rescuedKids >= 25 ? "Storm Captain" : "School-Run Leader";
                int sideAdventures=(hockeyPlayed?1:0)+(snowboardCompleted?1:0)+
                    (dogPetted?1:0)+(rabbitPetted?1:0);
                if(sideAdventures==4)rank="Full-Adventure "+rank;
                else if(sideAdventures>=2)rank="Curious "+rank;
                finished = true;
                levelOneDelivered=rescuedKids;
                schoolFinishEncounter=encounter;
                StartCoroutine(SchoolArrival(encounter,false,rank));
                break;
            default:
                spark.Restore(Mathf.Abs(encounter.SparkChange), encounter.Line);
                Show(encounter.Speaker, encounter.Line, 5f);
                break;
        }
    }

    private IEnumerator SchoolArrival(RiverValleyEncounter encounter,bool laterLevelDelivery,string rank)
    {
        busy=true;
        schoolChoiceActive=false;
        groupWaiting=false;
        waitPost=null;
        groupPrompt=null;
        groupPromptOwner=null;
        ClearAllPrompt();
        movement?.SetAutomatedInput(Vector2.zero,false,false);
        if(movement!=null)movement.enabled=false;

        DannyFollowCamera followCamera=FindFirstObjectByType<DannyFollowCamera>();
        Transform principal=encounter!=null?encounter.Actor:null;
        if(principal==null&&schoolFinishEncounter!=null)principal=schoolFinishEncounter.Actor;
        float groundY=player!=null?player.position.y:-20.47f;
        Vector3 lineHead=new(0f,groundY,routeFinishZ-5.4f);
        Vector3 principalStart=lineHead+Vector3.forward*1.25f;
        Vector3 doorway=new(0f,groundY,routeFinishZ+0.55f);
        Vector3 inside=doorway+Vector3.forward*2.8f;
        if(principal!=null)
        {
            principal.gameObject.SetActive(true);
            RiverValleyAmbientActor principalAmbient=principal.GetComponent<RiverValleyAmbientActor>();
            if(principalAmbient!=null)principalAmbient.enabled=false;
            RiverValleySchoolStream principalStream=principal.GetComponent<RiverValleySchoolStream>();
            if(principalStream!=null)principalStream.enabled=false;
            principal.SetPositionAndRotation(principalStart,Quaternion.LookRotation(Vector3.back,Vector3.up));
        }

        List<RiverValleyKidFollower> departing=new();
        List<Transform> children=new();
        int deliveredThisTrip=0;
        for(int i=0;i<kids.Count;i++)
        {
            RiverValleyKidFollower follower=kids[i];
            if(follower==null||!follower.gameObject.activeInHierarchy||follower.IsScattered||
               follower.IsBuried||follower.IsTumbling)continue;
            departing.Add(follower);
            deliveredThisTrip+=follower.Encounter!=null?follower.Encounter.GroupSize:1;
            Transform[] released=follower.ReleaseForSchoolLine();
            for(int j=0;j<released.Length;j++)if(released[j]!=null)
            {
                children.Add(released[j]);
                if(!schoolCelebrationChildren.Contains(released[j]))
                    schoolCelebrationChildren.Add(released[j]);
            }
        }
        for(int i=0;i<departing.Count;i++)kids.Remove(departing[i]);
        groupScattered=false;

        string principalLine=laterLevelDelivery
            ? levelThreeActive&&CountMissingChildren()==0
                ? "The last group is safe! Single file inside—and save a place for Danny!"
                : "This group is safe. Single file inside; Danny can keep searching for everyone still outside."
            : $"Everyone behind me—single file into school. Danny brought {levelOneDelivered} children through the storm. Rank: {rank}.";
        Show("PRINCIPAL",principalLine,6.5f);
        followCamera?.FocusCinematic(lineHead+Vector3.up*1.05f,3f);

        Vector3[] starts=new Vector3[children.Count];
        Vector3[] slots=new Vector3[children.Count];
        for(int i=0;i<children.Count;i++)
        {
            starts[i]=children[i].position;
            slots[i]=lineHead-Vector3.forward*(i*0.62f);
            slots[i].y=groundY;
            SetSchoolWalker(children[i],true,Vector3.forward);
        }
        const float lineUpSeconds=2.35f;
        for(float time=0f;time<lineUpSeconds;time+=Time.deltaTime)
        {
            float t=Mathf.SmoothStep(0f,1f,time/lineUpSeconds);
            for(int i=0;i<children.Count;i++)
                if(children[i]!=null)children[i].position=Vector3.Lerp(starts[i],slots[i],t);
            followCamera?.TrackCinematic(lineHead+Vector3.up*1.0f);
            yield return null;
        }

        if(principal!=null)
        {
            SetSchoolWalker(principal,true,Vector3.forward);
            for(float time=0f;time<1.35f;time+=Time.deltaTime)
            {
                principal.position=Vector3.Lerp(principalStart,inside,Mathf.SmoothStep(0f,1f,time/1.35f));
                followCamera?.TrackCinematic(doorway+Vector3.up*1.0f);
                yield return null;
            }
            principal.gameObject.SetActive(false);
        }

        bool[] entered=new bool[children.Count];
        float farthestWalk=children.Count>0
            ? Vector3.Distance(slots[children.Count-1],inside)/4.7f
            : 0f;
        float processionSeconds=Mathf.Max(4.6f,farthestWalk+0.35f);
        for(float time=0f;time<processionSeconds;time+=Time.deltaTime)
        {
            for(int i=0;i<children.Count;i++)
            {
                Transform child=children[i];
                if(child==null||entered[i])continue;
                float distance=Vector3.Distance(slots[i],inside);
                float t=Mathf.Clamp01(time/Mathf.Max(0.35f,distance/4.7f));
                child.position=Vector3.Lerp(slots[i],inside,Mathf.SmoothStep(0f,1f,t));
                if(t>=1f)
                {
                    entered[i]=true;
                    SchoolArrivalChildrenEntered++;
                    child.gameObject.SetActive(false);
                }
            }
            followCamera?.TrackCinematic(doorway+Vector3.up*1.0f);
            yield return null;
        }
        for(int i=0;i<children.Count;i++)
            if(children[i]!=null&&children[i].gameObject.activeSelf)
            {
                SchoolArrivalChildrenEntered++;
                children[i].gameObject.SetActive(false);
            }
        SchoolArrivalProcessionsCompleted++;

        if(laterLevelDelivery)
        {
            rescuedKids=Mathf.Max(0,rescuedKids-deliveredThisTrip);
            knownKids=Mathf.Max(rescuedKids,knownKids-deliveredThisTrip);
            groupScattered=false;
            int remaining=CountMissingChildren();
            bool wasLevelThree=levelThreeActive;
            if(wasLevelThree)levelThreeDelivered+=deliveredThisTrip;
            else levelTwoDelivered+=deliveredThisTrip;

            bool storyReady=wasLevelThree||(!levelTwoSnowboardRunRequired&&levelTwoWolfParkReached);
            bool deliveryGoalMet=wasLevelThree?remaining<=0:levelTwoDelivered>=levelTwoRequiredDelivery;
            if(deliveryGoalMet&&storyReady)
            {
                if(wasLevelThree)
                {
                    levelThreeActive=false;
                    levelTwoActive=false;
                    yield return StartCoroutine(FinalSchoolCelebration(followCamera,doorway,inside,children));
                    yield break;
                }
                levelTwoActive=false;
                finished=true;
                busy=true;
                Show("LEVEL 2 COMPLETE","The Wolf Park group is safe inside. School has started, but four groups are still out in the storm.",6f);
                yield return new WaitForSeconds(4.2f);
                BeginLevelThree();
                yield break;
            }

            levelTwoFinaleStarted=false;
            if(schoolFinishEncounter!=null)schoolFinishEncounter.Used=false;
            busy=false;
            if(movement!=null)movement.enabled=true;
            followCamera?.RecenterOnDanny();
            if(!storyReady&&remaining<=0)
                Show("PRINCIPAL",levelTwoSnowboardRunRequired
                    ? "The children are safe. The stair snowboard is still waiting before this adventure can close."
                    : "The children are safe. Check Wolf Park once, then come straight back.",6f);
            else if(wasLevelThree)
                Show("LEVEL 3 CHECKPOINT",$"{levelThreeDelivered} children are warm inside. {remaining} are still missing—TURN AROUND and follow Danny's KID RADAR at the top of the screen.",8f);
            else
                Show("PRINCIPAL",$"{deliveredThisTrip} arrived safely. {remaining} still missing—bring the next group back when you find them.",6f);
            yield break;
        }

        busy=false;
        schoolChoiceActive=true;
        if(movement!=null)movement.enabled=true;
        followCamera?.RecenterOnDanny();
        Show("DANNY","The first group is safe. I can enter school—or press Q to play hooky, ride the stair snowboard again, and find the missing children.",7f);
    }

    private bool CanDeliverAtSchool(out int nearChildren,out int trailingChildren)
    {
        nearChildren=0;
        trailingChildren=0;
        if(player==null)return false;
        foreach(RiverValleyKidFollower follower in kids)
        {
            if(follower==null||!follower.gameObject.activeInHierarchy||follower.IsScattered||
               follower.IsBuried||follower.IsTumbling)continue;
            int group=follower.Encounter!=null?follower.Encounter.GroupSize:1;
            Vector3 delta=follower.transform.position-player.position;
            if(new Vector2(delta.x,delta.z).magnitude<=17f&&Mathf.Abs(delta.y)<=3.5f&&!follower.IsWaiting)
                nearChildren+=group;
            else trailingChildren+=group;
        }
        return nearChildren>=(levelTwoActive||levelThreeActive?1:requiredKidsForSchool)&&trailingChildren==0;
    }

    private int BringFollowingChildrenToSchoolDoor()
    {
        if(player==null||player.position.z<routeFinishZ-24f)return 0;
        int index=0;
        int caughtUp=0;
        foreach(RiverValleyKidFollower follower in kids)
        {
            if(follower==null||!follower.gameObject.activeInHierarchy||follower.IsScattered||
               follower.IsBuried||follower.IsTumbling)continue;
            int group=follower.Encounter!=null?follower.Encounter.GroupSize:1;
            Vector3 delta=follower.transform.position-player.position;
            bool needsCatchUp=new Vector2(delta.x,delta.z).magnitude>14f||
                Mathf.Abs(delta.y)>3.2f||follower.IsWaiting;
            follower.Regather(player,index);
            if(needsCatchUp)
            {
                int row=index/5;
                int column=index%5-2;
                Vector3 schoolSlot=player.position+new Vector3(column*1.25f,0f,-2.2f-row*1.55f);
                if(Physics.Raycast(schoolSlot+Vector3.up*5f,Vector3.down,out RaycastHit ground,12f,
                    Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))schoolSlot.y=ground.point.y;
                follower.transform.position=schoolSlot;
                caughtUp+=group;
            }
            index++;
        }
        if(caughtUp>0)Show("DANNY",$"Everybody catches the school line—{caughtUp} children were right behind us. Nobody gets stranded off-screen.",4f);
        return caughtUp;
    }

    private IEnumerator FinalSchoolCelebration(DannyFollowCamera followCamera,
        Vector3 doorway,Vector3 inside,List<Transform> children)
    {
        // Everyone has crossed the threshold. Let their thanks come from
        // inside while Danny takes his own last walk, instead of ending with
        // him frozen on the pavement.
        busy=true;
        finalSchoolCelebrating=true;
        // A low view from the approach keeps the awning above the children
        // and the doorway, rather than across the middle of the closing shot.
        followCamera?.FocusCinematicFrom(doorway+Vector3.up*1.05f,12f,
            new Vector3(-0.9f,0.55f,-6.6f));
        // A few of the children turn back at the entrance to thank Danny in
        // person; the rest are already safely inside cheering with them.
        List<Transform> cheering=new();
        for(int i=0;i<schoolCelebrationChildren.Count;i++)
        {
            Transform candidate=schoolCelebrationChildren[i];
            if(candidate!=null&&!cheering.Contains(candidate))cheering.Add(candidate);
        }
        for(int i=0;i<children.Count;i++)
            if(children[i]!=null&&!cheering.Contains(children[i]))cheering.Add(children[i]);
        for(int i=0;i<Mathf.Min(8,cheering.Count);i++)
        {
            Transform child=cheering[i];
            if(child==null)continue;
            child.position=new Vector3(1.35f+(i%4)*0.78f,doorway.y,
                doorway.z-2.2f-(i/4)*0.88f);
            SetSchoolWalker(child,false,Vector3.back);
            child.gameObject.SetActive(true);
            LevelThreeCheeringKidsVisible++;
        }
        LaunchSchoolConfetti(doorway);
        audioDirector?.PlaySchoolCheer();
        Show("CHILDREN","Thank you, Danny! You found us! Hooray!",3.4f);

        if(player!=null)
        {
            if(characterController!=null)characterController.enabled=false;
            Vector3 start=player.position;
            Vector3 destination=new(doorway.x,start.y,inside.z);
            SetSchoolWalker(player,true,destination-start);
            const float walkSeconds=2.15f;
            for(float time=0f;time<walkSeconds;time+=Time.deltaTime)
            {
                player.position=Vector3.Lerp(start,destination,
                    Mathf.SmoothStep(0f,1f,time/walkSeconds));
                followCamera?.TrackCinematic(doorway+Vector3.up*1.05f);
                yield return null;
            }
            player.position=destination;
            SetSchoolWalker(player,false,Vector3.forward);
        }
        LevelThreeDannyEnteredSchool=player!=null&&player.position.z>doorway.z+1f;

        const string motherLine="My Honey Muffin! Mommy is so proud, and everybody is going to hear about it.";
        speaker="MOTHER";
        message=motherLine;
        messageUntil=Time.time+5.2f;
        audioDirector?.SpeakPriority("MOTHER",motherLine);
        yield return new WaitForSeconds(5.15f);

        finished=true;
        finalSchoolCelebrating=false;
        Show("GAME COMPLETE","Every child is safe inside The Little Public School. Danny made it in too—and the whole school is cheering for him!",14f);
    }

    private static void LaunchSchoolConfetti(Vector3 doorway)
    {
        Color[] colours={new(1f,0.75f,0.18f),new(0.34f,0.85f,0.98f),
            new(1f,0.34f,0.53f),new(0.54f,0.95f,0.45f)};
        Shader shader=Shader.Find("Sprites/Default");
        if(shader==null)shader=Shader.Find("Particles/Standard Unlit");
        for(int i=0;i<colours.Length;i++)
        {
            GameObject emitter=new("School celebration confetti");
            // Fall in front of the awning, where the celebration is visible
            // from Danny's approach instead of hidden above the school door.
            emitter.transform.position=doorway+new Vector3(0f,3.5f,-4f);
            ParticleSystem particles=emitter.AddComponent<ParticleSystem>();
            var main=particles.main;
            main.playOnAwake=false;
            main.loop=false;
            main.startLifetime=new ParticleSystem.MinMaxCurve(2.8f,4.2f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(0.65f,2f);
            main.startSize=new ParticleSystem.MinMaxCurve(0.065f,0.11f);
            main.startRotation=new ParticleSystem.MinMaxCurve(-3.14f,3.14f);
            main.startColor=Color.white;
            main.gravityModifier=0.52f;
            main.simulationSpace=ParticleSystemSimulationSpace.World;
            var emission=particles.emission;
            emission.enabled=false;
            var shape=particles.shape;
            shape.enabled=true;
            shape.shapeType=ParticleSystemShapeType.Box;
            shape.scale=new Vector3(6.8f,0.15f,2.5f);
            ParticleSystemRenderer renderer=emitter.GetComponent<ParticleSystemRenderer>();
            if(shader!=null)
            {
                renderer.material=new Material(shader);
                renderer.material.color=colours[i];
            }
            particles.Play();
            particles.Emit(40);
            LevelThreeConfettiPiecesLaunched+=40;
            Destroy(emitter,7f);
        }
    }

    private static void SetSchoolWalker(Transform actor,bool walking,Vector3 facing)
    {
        if(actor==null)return;
        Vector3 flat=Vector3.ProjectOnPlane(facing,Vector3.up);
        if(flat.sqrMagnitude>0.01f)actor.rotation=Quaternion.LookRotation(flat.normalized,Vector3.up);
        foreach(Animator animator in actor.GetComponentsInChildren<Animator>(true))
            if(animator!=null)animator.SetFloat("Speed",walking?3.2f:0f);
    }

    private IEnumerator DannyEntersSchool()
    {
        if(!schoolChoiceActive||player==null)yield break;
        schoolChoiceActive=false;
        busy=true;
        finished=true;
        movement?.SetAutomatedInput(Vector2.zero,false,false);
        if(movement!=null)movement.enabled=false;
        CharacterController controller=characterController;
        if(controller!=null)controller.enabled=false;
        DannyFollowCamera followCamera=FindFirstObjectByType<DannyFollowCamera>();
        Vector3 start=player.position;
        Vector3 inside=new(0f,start.y,routeFinishZ+3.35f);
        SetSchoolWalker(player,true,inside-start);
        for(float time=0f;time<1.7f;time+=Time.deltaTime)
        {
            player.position=Vector3.Lerp(start,inside,Mathf.SmoothStep(0f,1f,time/1.7f));
            followCamera?.TrackCinematic(player.position+Vector3.up*1.0f);
            yield return null;
        }
        SetSchoolWalker(player,false,Vector3.forward);
        Show("LEVEL 1 COMPLETE","Danny and the first school group made it inside. Level 2 begins now: ride the snowboard and rescue the children still outside!",6f);
        yield return new WaitForSeconds(2.6f);
        Vector3 nextStart=new(0f,start.y,routeFinishZ-4.8f);
        player.SetPositionAndRotation(nextStart,Quaternion.LookRotation(Vector3.back,Vector3.up));
        if(controller!=null)controller.enabled=true;
        movement?.ResetVerticalMotion();
        busy=false;
        BeginLevelTwo();
    }

    public void BeginLevelTwoForPlaytest()
    {
        if(!busy&&!levelTwoActive)BeginLevelTwo();
    }

    public void EnterSchoolForPlaytest()
    {
        if(!busy&&schoolChoiceActive)StartCoroutine(DannyEntersSchool());
    }

    private void BeginLevelTwoAsHooky()
    {
        if(busy||!schoolChoiceActive)return;
        BeginLevelTwo();
        Show("DANNY","The first group is safe inside. I choose the fun way back: play hooky on the stair snowboard, find the rabbit kids and wolf-park kids, then return them to school.",9f);
    }

    public void BeginLevelThreeForPlaytest()
    {
        if(!busy&&!levelThreeActive)BeginLevelThree();
    }

    public void StageSchoolEndingForPlaytest()
    {
        hockeyPlayed=true;
        snowboardCompleted=true;
        dogPetted=true;
        rabbitPetted=true;
        spark?.Restore(100f,"Focused school-ending verification restores Spark.");
    }

    private void BeginLevelTwo()
    {
        schoolChoiceActive=false;
        levelTwoActive=true;
        levelTwoFinaleStarted=false;
        levelTwoSnowboardRunRequired=true;
        levelTwoWolfParkReached=false;
        levelTwoDelivered=0;
        levelThreeActive=false;
        if(schoolFinishEncounter!=null)schoolFinishEncounter.Used=false;
        finished=false;
        rescuedKids=0;
        knownKids=0;
        for(int i=0;i<kids.Count;i++)
        {
            RiverValleyKidFollower follower=kids[i];
            if(follower!=null&&follower.IsScattered)
                knownKids+=follower.Encounter!=null?follower.Encounter.GroupSize:1;
        }
        groupScattered=knownKids>0;
        hockeyCards=Mathf.Max(1,hockeyCards);
        snowboardCompleted=false;
        LevelTwoHiddenRouteSigns=0;
        foreach(RiverValleyFacingSign sign in FindObjectsByType<RiverValleyFacingSign>(
            FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(sign==null||!sign.gameObject.activeSelf)continue;
            sign.gameObject.SetActive(false);
            LevelTwoHiddenRouteSigns++;
        }
        if(allEncounters!=null)
            for(int i=0;i<allEncounters.Length;i++)
                if(allEncounters[i]!=null&&allEncounters[i].Kind==RiverEncounterKind.SkateDare)
                    allEncounters[i].Used=false;
        ReopenLevelTwoDetourChildren();
        levelTwoTargetCount=CountMissingChildren();
        levelTwoRequiredDelivery=Mathf.Min(4,levelTwoTargetCount);
        foreach(CoyoteCubDetour detour in FindObjectsByType<CoyoteCubDetour>(FindObjectsSortMode.None))
            detour?.RearmForLevelTwo();
        whiteout?.BeginLevelTwoStorm();
        BuildLevelTwoStairReturnRamp();
        mom?.WaitForLevelTwoWolfParkClue();
        foreach(RiverValleyHazardMover hazard in FindObjectsByType<RiverValleyHazardMover>(FindObjectsSortMode.None))
            hazard?.EnableLevelTwoDanger();
        foreach(RiverValleySafeCar car in FindObjectsByType<RiverValleySafeCar>(FindObjectsSortMode.None))
            car?.EnableLevelTwoDanger();
        if(movement!=null)movement.enabled=true;
        if(characterController!=null&&!characterController.enabled)characterController.enabled=true;
        FindFirstObjectByType<DannyFollowCamera>()?.RecenterOnDanny();
        Show("DANNY","The children are inside. I want to ride that snowboard again. Then I find everybody left outside and say: School's this way—follow me.",10f);
    }

    private void BeginLevelThree()
    {
        levelTwoFinaleStarted=false;
        levelThreeActive=true;
        finalSchoolCelebrating=false;
        finished=false;
        rescuedKids=0;
        knownKids=0;
        levelThreeDelivered=0;
        if(schoolFinishEncounter!=null)schoolFinishEncounter.Used=false;
        kids.RemoveAll(kid=>kid==null||!kid.gameObject.activeInHierarchy);

        List<RiverValleyEncounter> candidates=new();
        if(allEncounters!=null)
            for(int i=0;i<allEncounters.Length;i++)
            {
                RiverValleyEncounter encounter=allEncounters[i];
                if(encounter==null||encounter.Actor==null||
                   (encounter.Kind!=RiverEncounterKind.LostKid&&encounter.Kind!=RiverEncounterKind.ParentHandoff))continue;
                if(encounter.Actor.GetComponent<RiverValleyKidFollower>()!=null)candidates.Add(encounter);
                // Level 3 is a focused four-group search, not every unused
                // encounter accumulated across the earlier free-roam levels.
                encounter.Used=true;
                if(encounter.Actor.gameObject.activeSelf)encounter.Actor.gameObject.SetActive(false);
            }

        List<RiverValleyEncounter> selected=SelectLevelThreeChildren(candidates,12);
        for(int i=0;i<selected.Count;i++)
        {
            RiverValleyEncounter encounter=selected[i];
            Vector3 position=encounterStartingPositions.TryGetValue(encounter,out Vector3 original)
                ? original : encounter.transform.position-Vector3.up;
            encounter.PrepareLaterLevelSearch(position,Mathf.Min(72f,46f+i*5f));
        }
        // Earlier companions that are not part of the last search stay hidden.
        // Do not let the school procession count or move those inactive actors.
        kids.RemoveAll(kid=>kid==null||!kid.gameObject.activeInHierarchy);
        levelThreeTargetCount=CountMissingChildren();
        whiteout?.BeginLevelThreeStorm();
        mom?.BeginLevelThree();
        DannyStormFlashlight flashlight=player!=null?player.GetComponent<DannyStormFlashlight>():null;
        if(flashlight==null&&player!=null)flashlight=player.gameObject.AddComponent<DannyStormFlashlight>();
        flashlight?.Configure(this);
        foreach(RiverValleyHazardMover hazard in FindObjectsByType<RiverValleyHazardMover>(FindObjectsSortMode.None))
            hazard?.EnableLevelThreeDanger();
        foreach(RiverValleySafeCar car in FindObjectsByType<RiverValleySafeCar>(FindObjectsSortMode.None))
            car?.EnableLevelThreeDanger();
        int searcherIndex=0;
        foreach(WinterCastIdentity identity in FindObjectsByType<WinterCastIdentity>(FindObjectsSortMode.None))
        {
            if(identity==null)continue;
            string label=(identity.name+" "+identity.StoryRole).ToLowerInvariant();
            if(!label.Contains("rescue")&&!label.Contains("firefighter")&&!label.Contains("paramedic")&&
                !label.Contains("captain"))continue;
            RiverValleySearchParty searcher=identity.GetComponent<RiverValleySearchParty>();
            if(searcher==null)searcher=identity.gameObject.AddComponent<RiverValleySearchParty>();
            searcher.Configure(this,searcherIndex++);
        }
        if(movement!=null)movement.enabled=true;
        if(characterController!=null&&!characterController.enabled)characterController.enabled=true;
        FindFirstObjectByType<DannyFollowCamera>()?.RecenterOnDanny();
        busy=false;
        Show("PRINCIPAL",$"Danny, take the school flashlight. The last {levelThreeTargetCount} children are still outside. Search every branch and bring everyone home. Mom is staying at home—though you may still hear her somehow.",10f);
    }

    private List<RiverValleyEncounter> SelectLevelThreeChildren(List<RiverValleyEncounter> candidates,int target)
    {
        List<RiverValleyEncounter> ordered=new(candidates);
        ordered.Sort((a,b)=>
        {
            Vector3 pa=encounterStartingPositions.TryGetValue(a,out Vector3 av)?av:a.transform.position;
            Vector3 pb=encounterStartingPositions.TryGetValue(b,out Vector3 bv)?bv:b.transform.position;
            return pa.z.CompareTo(pb.z);
        });
        // Search in a spread-out order so the final search uses the city,
        // stairs and valley rather than reopening four adjacent groups.
        List<RiverValleyEncounter> spread=new();
        int left=0,right=ordered.Count-1;
        while(left<=right)
        {
            spread.Add(ordered[left++]);
            if(left<=right)spread.Add(ordered[right--]);
        }
        List<RiverValleyEncounter> exact=new();
        List<RiverValleyEncounter> searchPool=new(spread);
        // Keep a real stair search in the finale. Earlier deliveries could
        // otherwise make the exact-total solver choose only street groups,
        // leaving the children visibly authored on the stair landings absent.
        RiverValleyEncounter stairGroup=null;
        foreach(RiverValleyEncounter encounter in spread)
        {
            Vector3 position=encounterStartingPositions.TryGetValue(encounter,out Vector3 original)
                ? original : encounter.transform.position;
            if(position.z<136f||position.z>228f||encounter.GroupSize>target)continue;
            stairGroup=encounter;
            break;
        }
        if(stairGroup!=null)
        {
            exact.Add(stairGroup);
            searchPool.Remove(stairGroup);
        }
        int remainder=target-(stairGroup!=null?stairGroup.GroupSize:0);
        if(TrySelectChildTotal(searchPool,0,remainder,exact))return exact;
        exact.Clear();
        if(TrySelectChildTotal(spread,0,target,exact))return exact;
        exact.Clear();
        int running=0;
        foreach(RiverValleyEncounter encounter in spread)
        {
            if(encounter==null||running>=target)continue;
            exact.Add(encounter);
            running+=encounter.GroupSize;
        }
        return exact;
    }

    private static bool TrySelectChildTotal(List<RiverValleyEncounter> source,int index,int remaining,
        List<RiverValleyEncounter> selected)
    {
        if(remaining==0)return true;
        if(remaining<0)return false;
        for(int i=index;i<source.Count;i++)
        {
            RiverValleyEncounter encounter=source[i];
            if(encounter==null||encounter.GroupSize>remaining)continue;
            selected.Add(encounter);
            if(TrySelectChildTotal(source,i+1,remaining-encounter.GroupSize,selected))return true;
            selected.RemoveAt(selected.Count-1);
        }
        return false;
    }

    private int CountMissingChildren()
    {
        int remaining=0;
        if(allEncounters==null)return remaining;
        for(int i=0;i<allEncounters.Length;i++)
        {
            RiverValleyEncounter encounter=allEncounters[i];
            if(encounter!=null&&!encounter.Used&&
               (encounter.Kind==RiverEncounterKind.LostKid||encounter.Kind==RiverEncounterKind.ParentHandoff))
                remaining+=encounter.GroupSize;
        }
        return remaining;
    }

    private void ReopenLevelTwoDetourChildren()
    {
        if(allEncounters==null)return;
        for(int i=0;i<allEncounters.Length;i++)
        {
            RiverValleyEncounter encounter=allEncounters[i];
            if(encounter==null||encounter.Actor==null||
               (encounter.Kind!=RiverEncounterKind.LostKid&&encounter.Kind!=RiverEncounterKind.ParentHandoff))continue;
            string label=(encounter.name+" "+encounter.Actor.name).ToLowerInvariant();
            if(!label.Contains("rabbit")&&!label.Contains("coyote-woods")&&!label.Contains("coyote woods"))continue;
            if(!encounter.Used)continue;
            Vector3 position=encounterStartingPositions.TryGetValue(encounter,out Vector3 original)
                ? original : encounter.transform.position-Vector3.up;
            encounter.PrepareLaterLevelSearch(position,0f);
        }
    }

    public void LevelTwoEnteredWolfPark()
    {
        if(!levelTwoActive)return;
        if(levelTwoWolfParkReached)return;
        levelTwoWolfParkReached=true;
        Show("DANNY","Wolf Park. The howling is real, the storm is darker, and I am still the only sign: School's this way—follow me.",8f);
        CoyoteCubDetour detour=FindFirstObjectByType<CoyoteCubDetour>();
        if(detour!=null&&spark!=null)detour.TriggerForLevelTwo(spark);
    }

    private bool IsWolfParkChildEncounter(RiverValleyEncounter encounter)
    {
        if(!levelTwoActive||encounter==null)return false;
        string encounterName=encounter.name.ToLowerInvariant();
        string actorName=encounter.Actor!=null?encounter.Actor.name.ToLowerInvariant():string.Empty;
        return encounterName.Contains("coyote woods")||actorName.Contains("coyote-woods");
    }

    private IEnumerator MomNoticesWolfParkChildren()
    {
        yield return new WaitForSeconds(2.4f);
        mom?.NoticeLevelTwoWolfParkEscape();
    }

    private static void BuildLevelTwoStairReturnRamp()
    {
        if(GameObject.Find("LEVEL 2 — packed-snow return ramp at stair top")!=null)
        {
            LevelTwoReturnRampBuilt=true;
            return;
        }
        const float topZ=134.72f;
        const float bottomZ=142.95f;
        const float topY=0.035f;
        const float bottomY=-2.42f;
        float length=Mathf.Sqrt((bottomZ-topZ)*(bottomZ-topZ)+(bottomY-topY)*(bottomY-topY));
        float angle=Mathf.Atan2(topY-bottomY,bottomZ-topZ)*Mathf.Rad2Deg;
        GameObject ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name="LEVEL 2 — packed-snow return ramp at stair top";
        // Keep the packed surface above every square stair nose.  Placing its
        // top exactly on the stair-centre line left half of each tread poking
        // through, which caught the CharacterController and looked like Danny
        // was repeatedly tripping at the landing.
        ramp.transform.position=new Vector3(0f,(topY+bottomY)*0.5f+0.06f,(topZ+bottomZ)*0.5f);
        ramp.transform.rotation=Quaternion.Euler(angle,0f,0f);
        ramp.transform.localScale=new Vector3(5.80f,0.14f,length+0.30f);
        Shader shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
        Material packedSnow=new(shader){color=new Color(0.68f,0.82f,0.93f),hideFlags=HideFlags.DontSave};
        if(packedSnow.HasProperty("_BaseColor"))packedSnow.SetColor("_BaseColor",packedSnow.color);
        ramp.GetComponent<Renderer>().sharedMaterial=packedSnow;
        // The old square treads stay visible under the snow, but the ramp is
        // the only walking collider here during Level 2.  Two overlapping
        // walkable surfaces produced a deterministic catch at Stair 007.
        for(int stairNumber=1;stairNumber<=22;stairNumber++)
        {
            GameObject stair=GameObject.Find($"Stair {stairNumber:000}");
            Collider stairCollider=stair!=null?stair.GetComponent<Collider>():null;
            if(stairCollider!=null)stairCollider.enabled=false;
        }
        LevelTwoReturnRampBuilt=true;
    }

    private IEnumerator RailRide(RiverValleyEncounter encounter)
    {
        busy = true;
        SnowPullProgress = 0f;
        hockeyCards--;
        Show(encounter.Speaker, encounter.Line + $"  Danny stakes one Edmonton hockey card. Cards left: {hockeyCards}.", 4.2f);
        yield return new WaitForSeconds(1.1f);
        movement.ResetVerticalMotion();
        Animator animator = player.GetComponentInChildren<Animator>();
        animator?.ResetTrigger("Jump");
        animator?.SetBool("FreeFall", false);
        animator?.SetBool("Grounded", true);
        animator?.SetTrigger("Stumble");
        movement.enabled = false;
        characterController.enabled = false;
        GameObject board = BuildSnowboard();
        Vector3 start = encounter.RailStart.position;
        Vector3 end = encounter.RailEnd.position;
        List<RiverValleyKidFollower> watchingKids=StageFollowersToWatch(end,start);
        List<SnowboardFamilyPickup> familyPickups=StageSnowboardFamilies(start,end);
        List<Vector3> rideTrail=new() { start };
        bool pickupButtonHeld=false;
        bool auditAutoCollect=HasCommandLineArgument("-riverSnowboardPickupAudit");
        RiverValleySwingRing swingRing = FindFirstObjectByType<RiverValleySwingRing>();
        player.SetPositionAndRotation(start, Quaternion.LookRotation((end - start).normalized, Vector3.up));
        Vector3 preRing = swingRing != null ? Vector3.Lerp(start,end,0.40f) : end;
        Vector3 postRing = swingRing != null ? Vector3.Lerp(start,end,0.50f) : end;
        float approachSeconds = swingRing != null ? 2.25f : 5.8f;
        for (float t = 0f; t < approachSeconds; t += Time.deltaTime)
        {
            float p = Mathf.SmoothStep(0f, 1f, t / approachSeconds);
            player.position = Vector3.Lerp(start, preRing, p) + Vector3.up * (Mathf.Sin(p * Mathf.PI) * 0.08f);
            UpdateSnowboardFamilies(familyPickups,p*0.40f,rideTrail,ref pickupButtonHeld,auditAutoCollect,encounter);
            yield return null;
        }

        if (swingRing != null)
        {
            if (board != null) board.SetActive(false);
            animator?.CrossFade("Jump",0.08f);
            animator?.SetBool("Grounded",false);
            Vector3 swingDirection=Vector3.ProjectOnPlane(end-start,Vector3.up).normalized;
            const float length=1.32f;
            const float swingSeconds=1.55f;
            for(float t=0f;t<swingSeconds;t+=Time.deltaTime)
            {
                float p=Mathf.SmoothStep(0f,1f,t/swingSeconds);
                float angle=Mathf.Lerp(-30f,38f,p)*Mathf.Deg2Rad;
                player.position=swingRing.transform.position-
                    Vector3.up*(Mathf.Cos(angle)*length)+swingDirection*(Mathf.Sin(angle)*length);
                player.rotation=Quaternion.LookRotation(swingDirection,Vector3.up);
                UpdateSnowboardFamilies(familyPickups,0.40f+p*0.10f,rideTrail,ref pickupButtonHeld,auditAutoCollect,encounter);
                yield return null;
            }
            swingRing.MarkCompleted();
            player.position=postRing;
            animator?.SetBool("Grounded",true);
            animator?.SetTrigger("Stumble");
            if (board != null) board.SetActive(true);
        }

        float finishSeconds=swingRing != null?3.0f:0f;
        for(float t=0f;t<finishSeconds;t+=Time.deltaTime)
        {
            float p=Mathf.SmoothStep(0f,1f,t/finishSeconds);
            player.position=Vector3.Lerp(postRing,end,p)+Vector3.up*(Mathf.Sin(p*Mathf.PI)*0.07f);
            UpdateSnowboardFamilies(familyPickups,0.50f+p*0.50f,rideTrail,ref pickupButtonHeld,auditAutoCollect,encounter);
            yield return null;
        }
        player.position = end;
        ClearPrompt(encounter);
        FinishSnowboardFamilies(familyPickups,end);
        characterController.enabled = true;
        movement.ResetVerticalMotion();
        movement.enabled = true;
        animator?.CrossFade("Friendly Idle", 0.12f);
        if (board != null) Destroy(board);
        ResumeWatchingFollowers(watchingKids);
        Camera.main?.GetComponent<DannyFollowCamera>()?.RecenterOnDanny();
        hockeyCards += 2;
        snowboardCompleted = true;
        streetCred += 8;
        spark.Restore(18f, "Danny wins the Edmonton hockey-card rail challenge.");
        int rideFamilies=0;
        int rideChildren=0;
        for(int i=0;i<familyPickups.Count;i++)
            if(familyPickups[i].Collected)
            {
                rideFamilies++;
                rideChildren+=familyPickups[i].Encounter.GroupSize;
            }
        if(levelTwoActive)
        {
            levelTwoSnowboardRunRequired=false;
            Show("BUNNY CREW",rideFamilies>0
                ? $"Snowboard rescue: {rideChildren} children slid in behind Danny! We packed safe snow piles beside the plow roads. Now back up the stairs!"
                : "No families caught that run. We packed safe snow piles beside the plow roads. The missing kids followed wolf tracks into Wolf Park—back up the stairs!",8f);
        }
        else
            Show("SKATE KID", rideFamilies>0
                ? $"You collected {rideChildren} children on the fly! They are around you at the bottom. Your card back—and the rare Northern Lights Goalie."
                : $"You did it, but missed the families. They are still waiting along the route. Cards: {hockeyCards}. Street Cred +8.", 7f);
        busy = false;
    }

    private List<SnowboardFamilyPickup> StageSnowboardFamilies(Vector3 start,Vector3 end)
    {
        List<SnowboardFamilyPickup> staged=new();
        if(allEncounters==null)return staged;
        float[] gates={0.18f,0.64f,0.84f};
        Vector3 direction=Vector3.ProjectOnPlane(end-start,Vector3.up).normalized;
        if(direction.sqrMagnitude<0.01f)direction=Vector3.forward;
        Vector3 right=Vector3.Cross(Vector3.up,direction).normalized;
        for(int pass=0;pass<2&&staged.Count<gates.Length;pass++)
            for(int i=0;i<allEncounters.Length&&staged.Count<gates.Length;i++)
            {
                RiverValleyEncounter candidate=allEncounters[i];
                if(candidate==null||candidate.Used||candidate.Actor==null||
                   !candidate.Actor.gameObject.activeInHierarchy||
                   (candidate.Kind!=RiverEncounterKind.LostKid&&candidate.Kind!=RiverEncounterKind.ParentHandoff))continue;
                if((pass==0&&candidate.GroupSize>3)||(pass==1&&candidate.GroupSize<=3))continue;
                if(candidate.Actor.GetComponent<RiverValleyKidFollower>()!=null)continue;
                int slot=staged.Count;
                float side=slot%2==0?-1f:1f;
                Vector3 point=Vector3.Lerp(start,end,gates[slot])+right*(1.45f*side);
                if(TryGroundPoint(point,out Vector3 grounded))point=grounded;
                SnowboardFamilyPickup pickup=new()
                {
                    Encounter=candidate,
                    Actor=candidate.Actor,
                    OriginalPosition=candidate.Actor.position,
                    OriginalRotation=candidate.Actor.rotation,
                    Gate=gates[slot],
                    TrailDelay=6+slot*6
                };
                List<Behaviour> paused=new();
                foreach(Behaviour behaviour in candidate.Actor.GetComponentsInChildren<Behaviour>(true))
                    if(behaviour is RiverValleyAmbientActor||behaviour is RiverValleySchoolStream||
                       behaviour is RiverValleyNeighbourResident)
                        paused.Add(behaviour);
                pickup.PausedBehaviours=paused.ToArray();
                pickup.PausedBehaviourStates=new bool[pickup.PausedBehaviours.Length];
                for(int state=0;state<pickup.PausedBehaviours.Length;state++)
                {
                    pickup.PausedBehaviourStates[state]=pickup.PausedBehaviours[state].enabled;
                    pickup.PausedBehaviours[state].enabled=false;
                }
                candidate.Actor.SetPositionAndRotation(point,Quaternion.LookRotation(-direction,Vector3.up));
                foreach(Animator familyAnimator in candidate.Actor.GetComponentsInChildren<Animator>(true))
                    familyAnimator.SetFloat("Speed",0f);
                staged.Add(pickup);
            }
        SnowboardFamiliesOffered=staged.Count;
        return staged;
    }

    private void UpdateSnowboardFamilies(List<SnowboardFamilyPickup> pickups,float progress,
        List<Vector3> trail,ref bool buttonHeld,bool auditAutoCollect,RiverValleyEncounter railEncounter)
    {
        trail.Add(player.position);
        if(trail.Count>240)trail.RemoveAt(0);
        Keyboard keyboard=Keyboard.current;
        Gamepad gamepad=Gamepad.current;
        bool pressed=(keyboard!=null&&keyboard.eKey.isPressed)||
            (gamepad!=null&&gamepad.buttonWest.isPressed)||RiverValleyMobileControls.ActionHeld;
        bool freshPress=pressed&&!buttonHeld;
        buttonHeld=pressed;
        SnowboardFamilyPickup nearest=null;
        float nearestGap=float.PositiveInfinity;
        Vector3 collectedCentre=Vector3.zero;
        int visibleCollected=0;
        for(int i=0;i<pickups.Count;i++)
        {
            SnowboardFamilyPickup pickup=pickups[i];
            if(pickup.Collected)
            {
                int trailIndex=Mathf.Max(0,trail.Count-1-pickup.TrailDelay);
                Vector3 target=trail[trailIndex];
                Vector3 facing=Vector3.ProjectOnPlane(player.position-target,Vector3.up);
                if(facing.sqrMagnitude<0.01f)facing=player.forward;
                pickup.Actor.position=Vector3.Lerp(pickup.Actor.position,target,Mathf.Clamp01(Time.deltaTime*10f));
                pickup.Actor.rotation=Quaternion.Slerp(pickup.Actor.rotation,
                    Quaternion.LookRotation(facing.normalized,Vector3.up),Mathf.Clamp01(Time.deltaTime*9f));
                collectedCentre+=pickup.Actor.position;
                visibleCollected++;
                continue;
            }
            float gap=Mathf.Abs(progress-pickup.Gate);
            if(gap<nearestGap){nearestGap=gap;nearest=pickup;}
        }
        if(visibleCollected>0)
        {
            Vector3 lineCentre=Vector3.Lerp(player.position,collectedCentre/visibleCollected,0.42f)+Vector3.up*0.78f;
            Camera.main?.GetComponent<DannyFollowCamera>()?.TrackCinematic(lineCentre,0.22f);
        }
        bool inWindow=nearest!=null&&nearestGap<=0.075f;
        if(inWindow)
            SetPrompt("E / X — COLLECT THIS FAMILY!",railEncounter);
        else
            ClearPrompt(railEncounter);
        if(nearest==null||nearest.Collected||(!freshPress&&!(auditAutoCollect&&inWindow))||!inWindow)return;
        nearest.Collected=true;
        nearest.Encounter.Used=true;
        nearest.Board=BuildFamilySnowboard(nearest.Actor,SnowboardFamiliesCollected);
        SnowboardFamiliesCollected++;
        SnowboardChildrenCollected+=nearest.Encounter.GroupSize;
        foreach(Animator familyAnimator in nearest.Actor.GetComponentsInChildren<Animator>(true))
        {
            familyAnimator.SetFloat("Speed",2.4f);
            familyAnimator.speed=1.12f;
        }
        Show("DANNY",$"Got you! {nearest.Encounter.GroupSize} aboard—slide in behind the orange hood!",1.4f);
    }

    private void FinishSnowboardFamilies(List<SnowboardFamilyPickup> pickups,Vector3 bottom)
    {
        Vector3 forward=Vector3.ProjectOnPlane(player.forward,Vector3.up).normalized;
        if(forward.sqrMagnitude<0.01f)forward=Vector3.forward;
        Vector3 right=Vector3.Cross(Vector3.up,forward).normalized;
        int collectedIndex=0;
        for(int i=0;i<pickups.Count;i++)
        {
            SnowboardFamilyPickup pickup=pickups[i];
            if(pickup.Board!=null)Destroy(pickup.Board);
            if(!pickup.Collected)
            {
                pickup.Actor.SetPositionAndRotation(pickup.OriginalPosition,pickup.OriginalRotation);
                RestoreSnowboardFamilyBehaviours(pickup);
                continue;
            }
            int row=collectedIndex/2;
            Vector3 surround=bottom-forward*(1.55f+row*0.92f)+
                right*((collectedIndex%2==0?-1f:1f)*(0.78f+row*0.08f));
            if(TryGroundPoint(surround,out Vector3 grounded))surround=grounded;
            pickup.Actor.position=surround;
            RiverValleyKidFollower follower=pickup.Actor.GetComponent<RiverValleyKidFollower>()??
                pickup.Actor.gameObject.AddComponent<RiverValleyKidFollower>();
            follower.Configure(player,kids.Count,pickup.Encounter);
            if(!kids.Contains(follower))kids.Add(follower);
            rescuedKids+=pickup.Encounter.GroupSize;
            knownKids+=pickup.Encounter.GroupSize;
            SnowboardChildrenAtBottom+=pickup.Encounter.GroupSize;
            collectedIndex++;
        }
        groupScattered=false;
    }

    private static void RestoreSnowboardFamilyBehaviours(SnowboardFamilyPickup pickup)
    {
        if(pickup.PausedBehaviours==null)return;
        for(int i=0;i<pickup.PausedBehaviours.Length;i++)
            if(pickup.PausedBehaviours[i]!=null)
                pickup.PausedBehaviours[i].enabled=pickup.PausedBehaviourStates[i];
    }

    private static GameObject BuildFamilySnowboard(Transform rider,int index)
    {
        GameObject board=GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name=$"Collected family's snowboard {index+1}";
        Destroy(board.GetComponent<Collider>());
        board.transform.SetParent(rider,false);
        board.transform.localPosition=new Vector3(0f,0.055f,0f);
        board.transform.localScale=new Vector3(0.78f,0.055f,1.72f);
        Shader shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
        Color[] colours={new(0.10f,0.72f,0.94f),new(0.76f,0.18f,0.88f),new(1f,0.58f,0.04f)};
        Renderer renderer=board.GetComponent<Renderer>();
        renderer.material=new Material(shader){color=colours[index%colours.Length]};
        return board;
    }

    private static bool HasCommandLineArgument(string expected)
    {
        string[] arguments=System.Environment.GetCommandLineArgs();
        for(int i=0;i<arguments.Length;i++)if(arguments[i]==expected)return true;
        return false;
    }

    private IEnumerator PullChildFromSnow(RiverValleyKidFollower child)
    {
        if (child == null || player == null)
            yield break;

        busy = true;
        List<RiverValleyKidFollower> watchingKids=StageFollowersToWatch(child.transform.position,player.position);
        int slot = Mathf.Max(0, kids.IndexOf(child));
        DannyTestController playerMotor = movement != null ? movement : player.GetComponent<DannyTestController>();
        CharacterController controller = characterController != null
            ? characterController : player.GetComponent<CharacterController>();
        Animator playerAnimator = player.GetComponentInChildren<Animator>();
        Animator[] childAnimators = child.GetComponentsInChildren<Animator>();
        bool childWasEnabled = child.enabled;

        if (playerMotor != null) playerMotor.enabled = false;
        if (controller != null) controller.enabled = false;
        child.enabled = false;

        string[] pleas =
        {
            "Help me, Danny! I can only see my boots!",
            "Pull me out! The snowbank is trying to keep me!",
            "Help! I am upside down and my mittens are full of winter!",
            "Danny! Please pluck me out before I become a snowman!"
        };
        Show("CHILD", pleas[SnowPullRescues % pleas.Length], 3.2f);

        Vector3 childPosition = child.transform.position;
        FindFirstObjectByType<DannyFollowCamera>()?.FocusCinematic(
            Vector3.Lerp(player.position, childPosition, 0.58f) + Vector3.up * 0.72f, 6.8f);
        Vector3 away = Vector3.ProjectOnPlane(player.position - childPosition, Vector3.up);
        if (away.sqrMagnitude < 0.04f)
            away = -Vector3.ProjectOnPlane(player.forward, Vector3.up);
        if (away.sqrMagnitude < 0.04f) away = Vector3.back;
        away.Normalize();
        Vector3 approach = childPosition + away * 1.12f;
        if (TryGroundPoint(approach, out Vector3 groundedApproach)) approach = groundedApproach;
        Vector3 playerStart = player.position;
        Vector3 approachTravel = Vector3.ProjectOnPlane(approach - playerStart, Vector3.up);
        if (approachTravel.sqrMagnitude > 0.001f)
            player.rotation = Quaternion.LookRotation(approachTravel.normalized, Vector3.up);
        playerAnimator?.SetFloat("Speed", 2.6f);

        float approachDuration = Mathf.Clamp(approachTravel.magnitude / 2.8f, 0.65f, 1.45f);
        for (float time = 0f; time < approachDuration; time += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, time / approachDuration);
            Vector3 position = Vector3.Lerp(playerStart, approach, t);
            if (TryGroundPoint(position, out Vector3 grounded)) position = grounded;
            player.position = position;
            yield return null;
        }
        player.position = approach;
        playerAnimator?.SetFloat("Speed", 0f);
        player.rotation = Quaternion.LookRotation(-away, Vector3.up);
        yield return new WaitForSeconds(0.32f);

        SnowRescueBurst.Spawn(child.transform.position + Vector3.up * 0.35f);
        playerAnimator?.SetTrigger("Stumble");
        Vector3 liftStart = child.transform.position;
        Quaternion liftRotation = child.transform.rotation;
        Vector3 upright = liftStart;
        if (TryGroundPoint(liftStart, out Vector3 groundedChild)) upright = groundedChild;
        Quaternion uprightRotation = Quaternion.LookRotation(away, Vector3.up);
        foreach (Animator animator in childAnimators)
            if (animator != null) animator.speed = 0.72f;

        WinterCastIdentity[] clusteredChildren = child.GetComponentsInChildren<WinterCastIdentity>(true);
        bool liftOneAtATime = clusteredChildren.Length > 1;
        if (liftOneAtATime)
        {
            Vector3[] buriedPositions = new Vector3[clusteredChildren.Length];
            Quaternion[] buriedRotations = new Quaternion[clusteredChildren.Length];
            for (int i = 0; i < clusteredChildren.Length; i++)
            {
                buriedPositions[i] = clusteredChildren[i].transform.position;
                buriedRotations[i] = clusteredChildren[i].transform.rotation;
            }
            child.transform.SetPositionAndRotation(upright, uprightRotation);
            for (int i = 0; i < clusteredChildren.Length; i++)
                clusteredChildren[i].transform.SetPositionAndRotation(buriedPositions[i], buriedRotations[i]);

            for (int i = 0; i < clusteredChildren.Length; i++)
            {
                Transform little = clusteredChildren[i].transform;
                float centred = i - (clusteredChildren.Length - 1) * 0.5f;
                Vector3 finish = upright + (uprightRotation * Vector3.right) * centred * 0.72f +
                    (uprightRotation * Vector3.forward) * ((i % 2) * 0.16f);
                if (TryGroundPoint(finish, out Vector3 groundedLittle)) finish = groundedLittle;
                Vector3 littleStart = little.position;
                Quaternion littleRotation = little.rotation;
                Show("CHILD", i == 0 ? pleas[SnowPullRescues % pleas.Length] :
                    (i == clusteredChildren.Length - 1 ? "Me too, Danny! I am the last snow kid!" : "Me next! My toes are turning into icicles!"), 2.0f);
                SnowRescueBurst.Spawn(littleStart + Vector3.up * 0.22f);
                const float littleLiftDuration = 0.72f;
                for (float time = 0f; time < littleLiftDuration; time += Time.deltaTime)
                {
                    float t = Mathf.SmoothStep(0f, 1f, time / littleLiftDuration);
                    SnowPullProgress = (i + t) / clusteredChildren.Length;
                    little.position = Vector3.Lerp(littleStart, finish, t) +
                        Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.42f);
                    little.rotation = Quaternion.Slerp(littleRotation, uprightRotation, t);
                    yield return null;
                }
                little.SetPositionAndRotation(finish, uprightRotation);
            }
        }
        else
        {
            Show("CHILD", pleas[SnowPullRescues % pleas.Length], 2.4f);
            const float liftDuration = 1.65f;
            for (float time = 0f; time < liftDuration; time += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, time / liftDuration);
                SnowPullProgress = t;
                Vector3 position = Vector3.Lerp(liftStart, upright, t);
                position.y += Mathf.Sin(t * Mathf.PI) * 0.58f;
                position += away * Mathf.Sin(t * Mathf.PI) * 0.18f;
                child.transform.SetPositionAndRotation(position,
                    Quaternion.Slerp(liftRotation, uprightRotation, t));
                yield return null;
            }
            child.transform.SetPositionAndRotation(upright, uprightRotation);
        }
        SnowRescueBurst.Spawn(upright + Vector3.up * 0.16f);
        child.Regather(player, slot);
        child.enabled = childWasEnabled;
        foreach (Animator animator in childAnimators)
            if (animator != null) animator.speed = 1f;

        SnowPullRescues++;
        SnowPullProgress = 1f;
        spark.Restore(9f, "Danny pulls a cold child out of the plow bank.");
        groupScattered = rescuedKids < knownKids;
        Show("DANNY", $"Got you! Shake off the plow snow and follow the orange hood. Kids together: {rescuedKids}/{knownKids}", 4.8f);
        yield return new WaitForSeconds(0.45f);

        if (controller != null) controller.enabled = true;
        if (playerMotor != null)
        {
            playerMotor.enabled = true;
            playerMotor.ResetVerticalMotion();
        }
        playerAnimator?.SetFloat("Speed", 0f);
        ResumeWatchingFollowers(watchingKids);
        Camera.main?.GetComponent<DannyFollowCamera>()?.RecenterOnDanny();
        busy = false;
    }

    private static bool TryGroundPoint(Vector3 position, out Vector3 grounded)
    {
        grounded = position;
        if (!Physics.Raycast(position + Vector3.up * 7f, Vector3.down, out RaycastHit hit, 22f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
        grounded.y = hit.point.y + 0.015f;
        return true;
    }

    private List<RiverValleyKidFollower> StageFollowersToWatch(Vector3 focus,Vector3 dannyPosition)
    {
        List<RiverValleyKidFollower> staged=new();
        Vector3 toward=Vector3.ProjectOnPlane(focus-dannyPosition,Vector3.up);
        if(toward.sqrMagnitude<0.01f)toward=player!=null?player.forward:Vector3.forward;
        toward.Normalize();
        Vector3 right=Vector3.Cross(Vector3.up,toward).normalized;
        foreach(RiverValleyKidFollower kid in kids)
        {
            if(kid==null||kid.IsScattered||kid.IsWaiting||kid.IsBuried||kid.IsTumbling||
                Vector3.ProjectOnPlane(kid.transform.position-dannyPosition,Vector3.up).magnitude>18f)continue;
            int slot=staged.Count;
            int row=slot/2;
            float side=(slot%2==0?-0.5f:0.5f)*1.05f;
            Vector3 watch=dannyPosition-toward*(2.4f+row*0.95f)+right*side;
            if(TryGroundPoint(watch,out Vector3 grounded))watch=grounded;
            kid.HoldForCinematic(watch,focus);
            staged.Add(kid);
        }
        return staged;
    }

    private void ResumeWatchingFollowers(List<RiverValleyKidFollower> staged)
    {
        if(staged==null||player==null)return;
        Vector3 forward=Vector3.ProjectOnPlane(player.forward,Vector3.up).normalized;
        if(forward.sqrMagnitude<0.01f)forward=Vector3.forward;
        Vector3 right=Vector3.Cross(Vector3.up,forward).normalized;
        for(int i=0;i<staged.Count;i++)
        {
            RiverValleyKidFollower kid=staged[i];
            if(kid==null)continue;
            int row=i/2;
            Vector3 resume=player.position-forward*(1.5f+row*0.90f)+right*((i%2==0?-0.42f:0.42f));
            if(TryGroundPoint(resume,out Vector3 grounded))resume=grounded;
            kid.ResumeAfterCinematic(player,resume);
        }
    }

    private IEnumerator HockeyGame(RiverValleyEncounter encounter)
    {
        busy=true;
        ClearAllPrompt();
        movement?.SetAutomatedInput(Vector2.zero,false,false);
        if(movement!=null)movement.enabled=false;
        if(characterController!=null)characterController.enabled=false;
        Animator playerAnimator=player!=null?player.GetComponentInChildren<Animator>():null;
        WinterHockeyRally rally=FindNearestHockeyRally(encounter!=null?encounter.Actor:null);
        DannyFollowCamera followCamera=FindFirstObjectByType<DannyFollowCamera>();
        GameObject stick=null;
        List<RiverValleyKidFollower> watchingKids=null;

        if(rally!=null&&player!=null)
        {
            Vector3 side=Vector3.Cross(Vector3.up,rally.PlayAxis).normalized;
            if(side.sqrMagnitude<0.01f)side=Vector3.right;
            Vector3 gameSlot=rally.Centre+side*1.35f;
            if(TryGroundPoint(gameSlot,out Vector3 groundedSlot))gameSlot=groundedSlot;
            watchingKids=StageFollowersToWatch(rally.Centre,gameSlot);
            Vector3 walkStart=player.position;
            Vector3 travel=Vector3.ProjectOnPlane(gameSlot-walkStart,Vector3.up);
            if(travel.sqrMagnitude>0.01f)player.rotation=Quaternion.LookRotation(travel.normalized,Vector3.up);
            followCamera?.FocusCinematic(rally.Centre+Vector3.up*0.75f,10f);
            playerAnimator?.SetFloat("Speed",2.1f);
            float approachSeconds=Mathf.Clamp(travel.magnitude/2.8f,0.45f,1.35f);
            for(float time=0f;time<approachSeconds;time+=Time.deltaTime)
            {
                player.position=Vector3.Lerp(walkStart,gameSlot,Mathf.SmoothStep(0f,1f,time/approachSeconds));
                followCamera?.TrackCinematic(Vector3.Lerp(player.position,rally.Centre,0.55f)+Vector3.up*0.72f);
                yield return null;
            }
            player.position=gameSlot;
            playerAnimator?.SetFloat("Speed",0f);
            stick=BuildDannyHockeyStick();
            rally.BeginGuestRally(player);
            Show(encounter.Speaker,"Danny, you are in. First to three—keep the tennis ball out of the snowbank!",3.2f);

            bool secondLine=false;
            bool finalLine=false;
            const float gameSeconds=7.4f;
            for(float time=0f;time<gameSeconds;time+=Time.deltaTime)
            {
                Vector3 puckDirection=Vector3.ProjectOnPlane(rally.BallPosition-player.position,Vector3.up);
                if(puckDirection.sqrMagnitude>0.01f)
                    player.rotation=Quaternion.Slerp(player.rotation,
                        Quaternion.LookRotation(puckDirection.normalized,Vector3.up),
                        1f-Mathf.Exp(-8f*Time.deltaTime));
                float shuffle=Mathf.Sin(time*4.2f)*0.18f;
                player.position=gameSlot+side*shuffle;
                float hit=Mathf.Pow(Mathf.Abs(Mathf.Sin(time*2.15f)),10f);
                playerAnimator?.SetFloat("Speed",hit>0.35f?2.0f:0f);
                PoseDannyHockeyStick(stick,rally.BallPosition,hit);
                followCamera?.TrackCinematic(Vector3.Lerp(player.position,rally.Centre,0.55f)+Vector3.up*0.72f);
                if(!secondLine&&time>2.55f)
                {
                    secondLine=true;
                    Show("HOCKEY KIDS","Two–two! Danny blocks one with a boot. Next goal wins.",3.0f);
                }
                if(!finalLine&&time>5.25f)
                {
                    finalLine=true;
                    Show("DANNY","Snowbank pass—back to me!",2.0f);
                }
                yield return null;
            }
            rally.EndGuestRally();
        }
        else
        {
            Show(encounter.Speaker,"Danny plays the full first-to-three street-hockey game.",4f);
            yield return new WaitForSeconds(3f);
        }

        if(stick!=null)Destroy(stick);
        ResumeWatchingFollowers(watchingKids);
        spark.Restore(Mathf.Abs(encounter.SparkChange), "A long street-hockey game restores Danny's joy.");
        hockeyPlayed = true;
        streetCred += 4;
        Show("HOCKEY KIDS","Danny scores off the snowbank! Game over—school run back on. Street Cred +4.",5f);
        playerAnimator?.SetFloat("Speed",0f);
        playerAnimator?.CrossFade("Friendly Idle",0.10f);
        if(characterController!=null)characterController.enabled=true;
        movement?.ResetVerticalMotion();
        if(movement!=null)movement.enabled=true;
        followCamera?.RecenterOnDanny();
        busy=false;
    }

    private static WinterHockeyRally FindNearestHockeyRally(Transform reference)
    {
        WinterHockeyRally best=null;
        float bestDistance=float.PositiveInfinity;
        Vector3 point=reference!=null?reference.position:Vector3.zero;
        foreach(WinterHockeyRally candidate in FindObjectsByType<WinterHockeyRally>(FindObjectsSortMode.None))
        {
            if(candidate==null)continue;
            float distance=(candidate.Centre-point).sqrMagnitude;
            if(distance>=bestDistance)continue;
            bestDistance=distance;
            best=candidate;
        }
        return best;
    }

    private GameObject BuildDannyHockeyStick()
    {
        GameObject stick=new("Danny's grounded street-hockey stick");
        Shader shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
        Material wood=new(shader){color=new Color(0.28f,0.13f,0.045f),hideFlags=HideFlags.DontSave};
        Material tape=new(shader){color=new Color(0.035f,0.04f,0.05f),hideFlags=HideFlags.DontSave};
        GameObject shaft=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.name="Danny hockey-stick shaft";
        shaft.transform.SetParent(stick.transform,false);
        shaft.transform.localScale=new Vector3(0.032f,0.57f,0.032f);
        shaft.GetComponent<Renderer>().sharedMaterial=wood;
        Destroy(shaft.GetComponent<Collider>());
        GameObject blade=GameObject.CreatePrimitive(PrimitiveType.Cube);
        blade.name="Danny hockey-stick blade";
        blade.transform.SetParent(stick.transform,false);
        blade.transform.localPosition=new Vector3(0f,-0.55f,-0.14f);
        blade.transform.localScale=new Vector3(0.09f,0.07f,0.40f);
        blade.GetComponent<Renderer>().sharedMaterial=tape;
        Destroy(blade.GetComponent<Collider>());
        return stick;
    }

    private void PoseDannyHockeyStick(GameObject stick,Vector3 ballPosition,float swing)
    {
        if(stick==null||player==null)return;
        Vector3 forward=Vector3.ProjectOnPlane(ballPosition-player.position,Vector3.up);
        if(forward.sqrMagnitude<0.01f)forward=player.forward;
        else forward.Normalize();
        Vector3 blade=player.position+forward*(0.48f+0.16f*swing)+player.right*0.27f;
        if(TryGroundPoint(blade,out Vector3 grounded))blade.y=grounded.y+0.07f;
        Vector3 grip=player.position+player.right*0.24f+player.forward*0.08f+Vector3.up*0.98f;
        Vector3 shaft=grip-blade;
        if(shaft.sqrMagnitude<0.001f)shaft=Vector3.up;
        else shaft.Normalize();
        stick.transform.position=Vector3.Lerp(blade,grip,0.51f);
        stick.transform.rotation=Quaternion.AngleAxis(Mathf.Sin(Time.time*9f)*swing*16f,shaft)*
            Quaternion.FromToRotation(Vector3.up,shaft);
    }

    private IEnumerator EndlessAdultTalk(RiverValleyEncounter encounter)
    {
        busy = true;
        movement.enabled = false;
        Show(encounter.Speaker, encounter.Line, 4f);
        yield return new WaitForSeconds(3f);
        Show(encounter.Speaker, "And another thing about sidewalks in 1987—there was a shovel, but it was not this shovel...", 4f);
        yield return new WaitForSeconds(3f);
        Show("DANNY", "This story has developed weather systems. I may live here now.", 4f);
        yield return new WaitForSeconds(2f);
        spark.Drain(Mathf.Abs(encounter.SparkChange), "An adult story refuses to find its ending.");
        streetCred = Mathf.Max(-10, streetCred - 1);
        movement.ResetVerticalMotion();
        movement.enabled = true;
        busy = false;
    }

    private IEnumerator CoyoteMoment(RiverValleyEncounter encounter)
    {
        busy = true;
        RiverValleyKidFollower chosen = null;
        for (int i = kids.Count - 1; i >= 0; i--)
            if (kids[i] != null && !kids[i].IsScattered && !kids[i].IsWaiting) { chosen = kids[i]; break; }
        if (chosen == null)
        {
            Show("DANNY", "A coyote trots away by itself, deeply disappointed by the lack of drama.", 4f);
            busy = false;
            yield break;
        }
        Vector3 destination = encounter.Actor != null ? encounter.Actor.position + encounter.Actor.right * 2.4f : chosen.transform.position + Vector3.right * 4f;
        chosen.ScatterTo(destination, 45f, "Coyote Kid — temporarily feral");
        rescuedKids = Mathf.Max(0, rescuedKids - 1);
        groupScattered = true;
        spark.Drain(5f, "A coyote startles one child away from the group.");
        Show("COYOTE KID", "Tell the school I live with coyotes now!  Danny: You have been gone for four seconds.", 6f);
        yield return new WaitForSeconds(3f);
        busy = false;
    }

    private IEnumerator HelperRescue(RiverValleyEncounter encounter)
    {
        busy = true;
        if (helpedNeighbour)
        {
            Show(encounter.Speaker, encounter.Line, 4.5f);
            yield return new WaitForSeconds(1.4f);
            Vector3 safe = player.position + player.right * 3.2f + player.forward * 2.2f;
            if (Physics.Raycast(safe + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 35f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) safe.y = hit.point.y;
            characterController.enabled = false;
            player.position = safe;
            characterController.enabled = true;
            movement.ResetVerticalMotion();
            spark.Restore(12f, "The neighbour Danny helped returns the kindness.");
            streetCred += 5;
            Show("DANNY", "He saw the plow before I did. Kindness has excellent visibility.", 5f);
        }
        else
        {
            spark.Drain(7f, "Danny dives into a snowbank to miss the hidden plow.");
            Show("DANNY", "I have become a snowbank. A cold but respected profession.", 4f);
        }
        yield return new WaitForSeconds(1f);
        busy = false;
    }

    private GameObject BuildSnowboard()
    {
        GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.name = "Danny's orange snowboard";
        Destroy(board.GetComponent<Collider>());
        board.transform.SetParent(player, false);
        board.transform.localPosition = new Vector3(0f,0.08f,0f);
        board.transform.localScale = new Vector3(0.58f,0.055f,1.55f);
        Renderer renderer = board.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        renderer.material = new Material(shader) { color = new Color(0.95f,0.22f,0.025f) };
        return board;
    }

    private void ScatterGroup(string reason)
    {
        int scatterIndex = 0;
        RiverValleyKidFollower lostKid = null;
        foreach (RiverValleyKidFollower kid in kids)
        {
            if (kid == null || kid.IsScattered || kid.IsWaiting) continue;
            kid.Scatter(scatterIndex++);
            lostKid = kid;
            // Losing one child is an urgent, readable problem. Losing the
            // entire school group at once felt like followers simply stopped
            // working and made the same recovery repeat too often.
            break;
        }
        if (scatterIndex == 0) return;
        int lost = lostKid != null && lostKid.Encounter != null ? lostKid.Encounter.GroupSize : 1;
        rescuedKids = Mathf.Max(0, rescuedKids - lost);
        groupScattered = true;
        Show("DANNY", "My Spark is fading—one child fell behind! Circle back and they will step into the huddle.", 6f);
    }

    public void ToggleGroupWait(Transform post, string placeName)
    {
        if (busy || rescuedKids == 0) { Show("DANNY", "There is nobody to ask yet.", 3f); return; }
        if (groupWaiting)
        {
            groupWaiting = false;
            waitPost = null;
            int index = 0;
            foreach (RiverValleyKidFollower kid in kids)
                if (kid != null && !kid.IsScattered) kid.Regather(player, index++);
            Show("DANNY", "Okay, group—orange hood moving. Stay close.", 4f);
            return;
        }
        groupWaiting = true;
        waitPost = post;
        waitStarted = Time.time;
        int waitingIndex = 0;
        foreach (RiverValleyKidFollower kid in kids)
            if (kid != null && !kid.IsScattered) kid.WaitAt(post, waitingIndex++);
        Show("DANNY", $"Wait together at {placeName}. No wandering, no joining a coyote family.", 5f);
    }

    public void SetGroupPrompt(Transform owner,string value)
    {
        groupPromptOwner=owner;
        groupPrompt=value;
    }

    public void ClearGroupPrompt(Transform owner)
    {
        if(owner!=null&&groupPromptOwner!=null&&owner!=groupPromptOwner)return;
        groupPromptOwner=null;
        groupPrompt=null;
    }

    private void ResumeWaitingGroup()
    {
        groupWaiting = false;
        waitPost = null;
        int index = 0;
        foreach (RiverValleyKidFollower kid in kids)
            if(kid!=null&&!kid.IsScattered)kid.Regather(player,index++);
        Show("CHILD", "Danny is moving—everyone back into the huddle.", 3.5f);
    }

    public void MomCaught()
    {
        if(busy)return;
        if(levelThreeActive)
        {
            // Level 3 is Danny's full flashlight search. Mom is voice-only at
            // home unless a real wolf or river event explicitly stages her.
            // A stray overlap must never end the level or fake a river fall.
            mom?.BeginLevelThree();
            Show("MOTHER","Pooky-Wooky! Mommy is calling from home. Keep the flashlight up and bring those children back safely!",4.8f);
            return;
        }
        StartCoroutine(CollectionRoutine(false));
    }

    private IEnumerator CollectionRoutine(bool emptySpark)
    {
        busy = true;
        if (movement != null) movement.enabled = false;
        string[] momLines =
        {
            "There is my precious little Poopsie!",
            "Mommy found her tiny pudding cup!",
            "Come here, my brave little Pumpkin-Pants.",
            "Do not forget your emergency kisses, Snuggle-Bottom.",
            "Mommy caught her runaway marshmallow! Hold still while I fix your hood.",
            "There is my little snow pea! One kiss for every block you made me walk.",
            "Got you, Pumpkin-Pants! Mommy knew the orange hood would betray you.",
            "Caught my speedy pudding cup! Now let Mommy wipe your nose.",
            "Come here, Bunny-Boots! Mommy packed your backup socks and your dignity.",
            "My Honey Muffin! Mommy is so proud, and everybody is going to hear about it.",
            "Those cheeks are cold, Cuddle Bug! Mommy must warm them with kisses.",
            "Pooky-Wooky! Mommy brought the baby wipes, just in case.",
            "Caught you, Snow Angel! Mommy has been carrying one perfectly warm mitten.",
            "There you are, Little Moose! Stand still for a full coat inspection.",
            "Found my Cinnamon Bun! Mommy must check both ears for frost.",
            "Got you, Noodle Knees! Your scarf is one centimetre crooked.",
            "Come here, Baby Blizzard! Mommy owes that forehead three emergency kisses.",
            "My wandering Waffle Mittens! Time for the official mitten count.",
            "Caught my Cozy Bean! Mommy brought the comb you said was embarrassing.",
            "There is my Tiny Toque! Mommy could spot that pom-pom in a blizzard.",
            "Found you, Cocoa Button! Your thermos and Mommy were both worried.",
            "Come back, Snowy Biscuit! Mommy has a very public hug ready.",
            "Got my Jellybean Jacket! The crossing guard will hear how brave you were.",
            "There you are, Darling Dumpling! Mommy packed a celebratory napkin."
        };
        string line = momLines[(momCatches*7+5) % momLines.Length];
        momCatches++;
        Show("MOTHER", line, 3.0f);
        yield return new WaitForSeconds(2.0f);
        Show("DANNY", emptySpark ? "Everything went grey. I need one good reason to keep wondering." : "I need an adult. A different adult.", 3.4f);
        yield return new WaitForSeconds(2.1f);
        if (!emptySpark) spark.Drain(28f, line);
        // A catch no longer teleports Danny back to a checkpoint. The pause,
        // embarrassing coat inspection, and Spark cost all happen exactly
        // where Mom caught him, so every visible character remains physical.
        spark.Restore(55f, "A fresh chance to protect Danny's Spark.");
        mom?.GiveHeadStart(10f, 34f);
        if (movement != null) movement.enabled = true;
        Show("DANNY", "New plan: curiosity first, dignity if available.", 4f);
        busy = false;
    }

    public void Hazard(string hazardSpeaker, string hazardLine, float damage, float slowScale, float slowSeconds)
    {
        if (spark == null || busy) return;
        spark.Drain(damage, hazardLine);
        movement?.ApplySlow(slowScale, slowSeconds);
        Show(hazardSpeaker, hazardLine, 4.2f);
    }

    public void LevelTwoHazardLaunch(Transform hazard,string hazardSpeaker,string hazardLine,float damage)
    {
        if(busy||player==null||Time.time<nextLevelTwoLaunch)return;
        nextLevelTwoLaunch=Time.time+3.5f;
        StartCoroutine(LevelTwoSnowPileTumble(hazard,hazardSpeaker,hazardLine,damage));
    }

    private IEnumerator LevelTwoSnowPileTumble(Transform hazard,string hazardSpeaker,string hazardLine,float damage)
    {
        busy=true;
        ClearAllPrompt();
        spark?.Drain(damage,hazardLine);
        Show(hazardSpeaker,hazardLine,2.2f);
        Vector3 start=player.position;
        Vector3 away=hazard!=null
            ? Vector3.ProjectOnPlane(player.position-hazard.position,Vector3.up)
            : player.right;
        if(away.sqrMagnitude<0.02f)away=player.right;
        away.Normalize();
        Vector3 side=Vector3.Cross(Vector3.up,away).normalized;
        Vector3 landing=start;
        bool found=false;
        Vector3[] directions={away,(away+side*0.55f).normalized,(away-side*0.55f).normalized,-side,side};
        for(int i=0;i<directions.Length&&!found;i++)
            for(float distance=3.4f;distance>=1.8f&&!found;distance-=0.8f)
            {
                Vector3 candidate=start+directions[i]*distance;
                if(candidate.x<-97f||candidate.x>97f||candidate.z<-49f||candidate.z>433f)continue;
                found=TryFindLevelTwoLanding(candidate,start.y,out landing);
            }
        if(!found)TryFindLevelTwoLanding(start,start.y,out landing);
        landing+=Vector3.up*0.08f;
        BuildLevelTwoSnowPile(landing,away);

        movement?.SetAutomatedInput(Vector2.zero,false,false);
        if(movement!=null)movement.enabled=false;
        if(characterController!=null)characterController.enabled=false;
        Animator animator=player.GetComponentInChildren<Animator>();
        animator?.SetTrigger("Stumble");
        DannyFollowCamera camera=FindFirstObjectByType<DannyFollowCamera>();
        Quaternion startRotation=player.rotation;
        Quaternion endRotation=Quaternion.LookRotation(-away,Vector3.up);
        const float tumbleSeconds=0.95f;
        for(float time=0f;time<tumbleSeconds;time+=Time.deltaTime)
        {
            float t=Mathf.Clamp01(time/tumbleSeconds);
            float eased=Mathf.SmoothStep(0f,1f,t);
            player.position=Vector3.Lerp(start,landing,eased)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*1.35f);
            player.rotation=Quaternion.Slerp(startRotation*Quaternion.AngleAxis(t*540f,Vector3.forward),endRotation,eased*eased);
            camera?.TrackCinematic(player.position+Vector3.up*0.7f);
            yield return null;
        }
        player.SetPositionAndRotation(landing,endRotation);
        LevelTwoSnowPileLandings++;
        string vehicle=hazard!=null&&hazard.name.ToLowerInvariant().Contains("bicycle")?"bike"
            : hazard!=null&&hazard.name.ToLowerInvariant().Contains("car")?"car":"plow";
        Show("DANNY",$"The {vehicle} has professionally delivered me into a snow pile. I am stuck.",3.2f);

        // Make the rescue physical. Followers run into a close semicircle and
        // pull; if Danny is alone during Level 3, Mom radar supplies the
        // dramatic rescue instead of letting him quietly pop out by himself.
        List<RiverValleyKidFollower> rescuers=new();
        List<Vector3> rescuerStarts=new();
        List<Vector3> rescuerTargets=new();
        for(int i=0;i<kids.Count&&rescuers.Count<4;i++)
        {
            RiverValleyKidFollower kid=kids[i];
            if(kid==null||kid.IsScattered||kid.IsWaiting||kid.IsBuried||kid.IsTumbling)continue;
            int slot=rescuers.Count;
            Vector3 rescueSlot=landing-away*(1.05f+(slot/2)*0.58f)+side*((slot%2==0?-1f:1f)*0.62f);
            if(TryGroundPoint(rescueSlot,out Vector3 groundedRescueSlot))rescueSlot=groundedRescueSlot;
            rescuers.Add(kid);
            rescuerStarts.Add(kid.transform.position);
            rescuerTargets.Add(rescueSlot);
            kid.HoldForCinematic(kid.transform.position,landing);
        }

        bool momRescue=levelThreeActive&&rescuers.Count==0&&mom!=null;
        Transform momTransform=momRescue?mom.transform:null;
        Animator momAnimator=momRescue?mom.GetComponentInChildren<Animator>():null;
        Vector3 momStart=Vector3.zero;
        Vector3 momStop=Vector3.zero;
        if(momRescue)
        {
            mom.enabled=false;
            momStart=landing-away*6.2f+side*2.1f;
            momStop=landing-away*1.25f+side*0.72f;
            if(TryGroundPoint(momStart,out Vector3 groundedMomStart))momStart=groundedMomStart;
            if(TryGroundPoint(momStop,out Vector3 groundedMomStop))momStop=groundedMomStop;
            momTransform.position=momStart;
            Vector3 momTravel=Vector3.ProjectOnPlane(momStop-momStart,Vector3.up);
            if(momTravel.sqrMagnitude>0.01f)momTransform.rotation=Quaternion.LookRotation(momTravel.normalized,Vector3.up);
            momAnimator?.SetFloat("Speed",4.2f);
        }

        const float rescueApproachSeconds=0.85f;
        for(float time=0f;time<rescueApproachSeconds;time+=Time.deltaTime)
        {
            float t=Mathf.SmoothStep(0f,1f,time/rescueApproachSeconds);
            for(int i=0;i<rescuers.Count;i++)
                if(rescuers[i]!=null)rescuers[i].transform.position=Vector3.Lerp(rescuerStarts[i],rescuerTargets[i],t);
            if(momTransform!=null)momTransform.position=Vector3.Lerp(momStart,momStop,t);
            camera?.TrackCinematic(player.position+Vector3.up*0.70f);
            yield return null;
        }
        momAnimator?.SetFloat("Speed",0f);
        Show(rescuers.Count>0?"CHILDREN":momRescue?"MOTHER":"DANNY",rescuers.Count>0
            ? "Danny is stuck! Huddle close—grab the orange sleeves and pull!"
            : momRescue
                ? "Mom radar found the orange snow pile. Hold still, Danny Boy—Mommy is pulling you out!"
                : "No rescue group yet. Wiggle, breathe, and climb out before the next bike.",3.5f);
        Vector3 buriedPosition=player.position;
        for(float time=0f;time<2.15f;time+=Time.deltaTime)
        {
            float wiggle=Mathf.Sin(time*11f)*Mathf.Lerp(5f,1f,time/2.15f);
            player.rotation=endRotation*Quaternion.Euler(0f,0f,wiggle);
            player.position=buriedPosition+Vector3.up*Mathf.Abs(Mathf.Sin(time*7f))*0.025f;
            camera?.TrackCinematic(player.position+Vector3.up*0.70f);
            yield return null;
        }
        Vector3 recovery=landing-away*0.65f;
        if(TryFindLevelTwoLanding(recovery,landing.y,out Vector3 groundedRecovery))recovery=groundedRecovery+Vector3.up*0.08f;
        for(float time=0f;time<0.72f;time+=Time.deltaTime)
        {
            float t=Mathf.SmoothStep(0f,1f,time/0.72f);
            player.position=Vector3.Lerp(buriedPosition,recovery,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*0.22f;
            player.rotation=Quaternion.Slerp(endRotation,Quaternion.LookRotation(away,Vector3.up),t);
            camera?.TrackCinematic(player.position+Vector3.up*0.72f);
            yield return null;
        }
        player.position=recovery;
        for(int i=0;i<rescuers.Count;i++)
            if(rescuers[i]!=null)
            {
                Vector3 resume=recovery-away*(1.1f+(i/2)*0.82f)+side*((i%2==0?-1f:1f)*0.72f);
                if(TryGroundPoint(resume,out Vector3 groundedResume))resume=groundedResume;
                rescuers[i].ResumeAfterCinematic(player,resume);
            }
        if(momRescue)
        {
            mom.enabled=true;
            mom.ReturnHomeAfterWolfRescue();
        }
        if(characterController!=null)characterController.enabled=true;
        movement?.ResetVerticalMotion();
        if(movement!=null)movement.enabled=true;
        animator?.CrossFade("Friendly Idle",0.10f);
        Show("DANNY",levelThreeActive
            ? "Out! Level 3 traffic is faster. Find the next missing group and keep off the road."
            : "Out! Wolf Park and the missing kids are still waiting.",4.5f);
        if(levelThreeActive)player.GetComponent<DannyStormFlashlight>()?.DamageByVehicle(vehicle);
        camera?.RecenterOnDanny();
        busy=false;
    }

    private bool TryFindLevelTwoLanding(Vector3 candidate,float sourceY,out Vector3 landing)
    {
        landing=candidate;
        RaycastHit[] hits=Physics.RaycastAll(candidate+Vector3.up*5f,Vector3.down,12f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        float nearest=float.PositiveInfinity;
        for(int i=0;i<hits.Length;i++)
        {
            Collider collider=hits[i].collider;
            if(collider==null||collider.transform.IsChildOf(player)||hits[i].normal.y<0.62f)continue;
            if(collider.GetComponentInParent<RiverValleyHazardMover>()!=null||
                collider.GetComponentInParent<RiverValleySafeCar>()!=null||
                collider.GetComponentInParent<WinterCastIdentity>()!=null)continue;
            if(Mathf.Abs(hits[i].point.y-sourceY)>1.5f||hits[i].distance>=nearest)continue;
            nearest=hits[i].distance;
            landing=hits[i].point;
        }
        return nearest<float.PositiveInfinity;
    }

    private static void BuildLevelTwoSnowPile(Vector3 position,Vector3 launchDirection)
    {
        GameObject pile=new("Level 2 soft roadside landing pile");
        pile.transform.position=position;
        float yaw=Mathf.Atan2(launchDirection.x,launchDirection.z)*Mathf.Rad2Deg;
        pile.transform.rotation=Quaternion.Euler(0f,yaw,0f);
        Shader shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
        Material snow=new(shader){color=new Color(0.78f,0.90f,0.98f),hideFlags=HideFlags.DontSave};
        Vector3[] positions={new(-0.55f,0.18f,0f),new(0.48f,0.15f,0.12f),new(0f,0.28f,0.28f)};
        Vector3[] scales={new(1.45f,0.48f,1.15f),new(1.30f,0.42f,1.05f),new(1.40f,0.55f,1.20f)};
        for(int i=0;i<positions.Length;i++)
        {
            GameObject mound=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mound.name=$"Soft snow mound {i+1}";
            mound.transform.SetParent(pile.transform,false);
            mound.transform.localPosition=positions[i];
            mound.transform.localScale=scales[i];
            Collider collider=mound.GetComponent<Collider>();
            if(collider!=null)Destroy(collider);
            mound.GetComponent<Renderer>().sharedMaterial=snow;
        }
        Destroy(pile,14f);
    }

    public void FollowerBuriedByPlow(RiverValleyKidFollower follower, Vector3 snowbankPosition)
    {
        if (follower == null || follower.IsScattered || follower.IsWaiting || busy) return;
        int lost = follower.Encounter != null ? follower.Encounter.GroupSize : 1;
        follower.BuryInSnow(snowbankPosition);
        rescuedKids = Mathf.Max(0, rescuedKids - lost);
        groupScattered = true;
        streetCred = Mathf.Max(-10, streetCred - 2);
        spark?.Drain(Mathf.Min(12f, 7f + lost),
            "A direct plow spray sent children tumbling into the snowbank, wet and shivering.");
        nextColdGroupPressure = Time.time + 8f;
        Show("CHILD", lost > 1
            ? $"The plow spun {lost} children into a snowbank! Find the waving boots, dig them out, then make the final run."
            : "The plow spun one child into a snowbank! Find the waving boots, dig them out, then make the final run.", 6f);
    }

    public void FollowerBuriedByCar(RiverValleyKidFollower follower,Vector3 snowbankPosition)
    {
        if(follower==null||follower.IsScattered||follower.IsWaiting||busy)return;
        int lost=follower.Encounter!=null?follower.Encounter.GroupSize:1;
        follower.BuryInSnow(snowbankPosition);
        rescuedKids=Mathf.Max(0,rescuedKids-lost);
        groupScattered=true;
        streetCred=Mathf.Max(-10,streetCred-2);
        spark?.Drain(Mathf.Min(10f,5f+lost),
            "A slippery car bumper sent children into a soft roadside snowbank.");
        nextColdGroupPressure=Time.time+8f;
        Show("DRIVER",lost>1
            ? $"Brake! {lost} children slid into that snowbank—Danny, follow the waving mittens and help them out!"
            : "Brake! One child slid into that snowbank—Danny, follow the waving mitten and help them out!",6f);
    }

    public void FollowerStuckOnIce(RiverValleyKidFollower follower, Vector3 patchPosition)
    {
        if (follower == null || follower.IsScattered || follower.IsWaiting || busy) return;
        spark?.Drain(1f, "A child slips but the huddle keeps them close.");
        Show("CHILD", "Whoa—hands together! I almost slid away.", 3.5f);
    }

    public void CoyoteDraggingChild()
    {
        spark?.Drain(2f,"A curious coyote is tugging a snow-stuck child farther from the group.");
        Show("CHILD","Danny! The coyote thinks my snowsuit has a handle! Come dig me out!",5.5f);
    }

    public void LowBranchWipeout()
    {
        spark?.Drain(3f,"A spinning jump met a low spruce branch.");
        Show("DANNY","The tree has rejected my trick on technical grounds.",4.5f);
    }

    public void MomCallout()
    {
        string[] calls=
        {
            "Pooky Bear! Mommy found your toddler snow pants! The ones with the ducks!",
            "Pumpkin-Pants! Do you want Mommy to sing your getting-dressed song?",
            "Snuggle-Bottom! Stop running! Mommy has to inspect your goodbye kiss!",
            "Pudding Cup! I put a love note in your lunch. Read it to everybody!",
            "Bunny-Boots! Your clean long underwear is in the front pocket!",
            "Sweet Pea! Your mittens are still tied together like when you were little!",
            "Honey Muffin! Mrs. Patel remembers your bubble-bath dance!",
            "Cuddle Bug! Wait at the corner! Mommy brought your emergency cuddle!",
            "Pooky-Wooky! Mommy can still do your baby voice if you cannot hear me!",
            "Pumpkin-Pants! Did you remember to use the toilet before school?",
            "Snuggle-Bottom! Your name is written inside every sock!",
            "Pudding Cup! I packed your dinosaur pajamas for after school!",
            "Bunny-Boots! Tell your principal Mommy calls you her tiny snow dumpling!",
            "Sweet Pea! Mommy brought enough apple slices for all your new little friends!",
            "Honey Muffin! School does not cancel your seven-thirty bedtime!",
            "Cuddle Bug! Mommy is gaining! Prepare for public smooches!",
            "Pooky Bear! Take the stairs one at a time like Mommy's careful little penguin!",
            "Pumpkin-Pants! Mommy can find that orange hood in any blizzard!",
            "Snuggle-Bottom! Do not forget Mommy taught you that hockey stance!",
            "Pudding Cup! Wait at the school! Mommy needs a first-day picture!",
            "That is my brave Bunny-Boots! Everybody look how responsible he is!",
            "Sweet Pea! Introduce Mommy to every single child. Mommy has snacks!",
            "Honey Muffin! Are your socks wet? Mommy can change them right here!",
            "Cuddle Bug! Mommy made it! Time for a hug in front of the whole school!",
            "Snow Angel! Mommy found the mitten you named Captain Fluffy!",
            "Muffin Boots! I packed the spoon with the little train on it!",
            "Pookie Pie! Your thermos says Mommy's brave explorer in enormous letters!",
            "Cinnamon Bun! Mommy ironed your emergency handkerchief!",
            "Little Moose! Remember, Mommy can see that orange coat from space!",
            "Cupcake Toes! I told Mrs. Nguyen about your bath-time hockey league!",
            "Sugar Mittens! You left your lucky pebble beside the toothbrush!",
            "Baby Blizzard! Mommy brought the scarf with your full name embroidered on both ends!",
            "Noodle Knees! Your lunch banana has a smiley face and three kisses!",
            "Marshmallow Cheeks! Mommy still has your kindergarten snowman drawing!",
            "Tiny Turnip! Do not make Mommy use the family whistle!",
            "Bumble Boots! I warmed your spare socks on the radiator!",
            "Pudding Paws! Mommy packed the crusts exactly how you pretend not to like them!",
            "Snowsuit Sprout! Grandma says your hood makes you look extremely huggable!",
            "Waffle Mittens! I found the sticker chart from your tooth-brushing phase!",
            "Cozy Bean! Mommy put your indoor shoes in the bag marked Danny's Very Special Shoes!",
            "Little Icicle! Your teddy bear is watching your school attendance!",
            "Pumpkin Puff! Mommy knows you can hear the snack container rattling!",
            "Darling Dumpling! I brought the tissues with cartoon ducks!",
            "Fuzzy Pea! Your baby photo is still in Mommy's wallet and Mommy has witnesses!",
            "Snowy Biscuit! Do not forget our secret handshake has seven hugs!",
            "Jellybean Jacket! Mommy told the crossing guard you are an excellent listener!",
            "Tiny Toque! I packed the backup toque with the pom-pom the size of an orange!",
            "Cocoa Button! Mommy saved the note where you promised to marry your snow shovel!",
            "Snow Pea! Mommy found the permission slip you signed with a purple crayon! I brought it so you can show everybody.",
            "Pooky Bear! Mommy is proud—you have not wet the bed in such a long time! I told everybody!",
            "Pumpkin-Pants! Mommy found the note where you promised to marry your mittens!",
            "Snuggle-Bottom! Your baby teeth are still in Mommy's purse!",
            "Pudding Cup! Should Mommy perform your bathtub dinosaur song for the hockey players?",
            "Bunny-Boots! Mommy brought Captain Cozy, the blanket you said was only for emergencies!",
            "Sweet Pea! Your whole class should know you still ask for toast cut into stars!",
            "Honey Muffin! Mommy found the drawing where you made us both glitter unicorns!",
            "Cuddle Bug! Your emergency underwear has rockets on it, just like you requested!"
        };
        Show("MOTHER",calls[(momCallouts++*17+11)%calls.Length],4.2f);
    }

    public void Show(string newSpeaker, string newMessage, float seconds = 4f)
    {
        speaker = newSpeaker;
        message = newMessage;
        messageUntil = Time.time + seconds;
        audioDirector?.Speak(newSpeaker, newMessage);
    }

    public bool TryShowAmbient(string newSpeaker,string newMessage,float seconds=3.4f)
    {
        if(busy||promptOwner!=null||Time.time<=messageUntil)return false;
        speaker=newSpeaker;
        message=newMessage;
        messageUntil=Time.time+seconds;
        // Ambient neighbourhood chatter is currently subtitled.  It is not
        // sent through the voice system until a matching recording exists, so
        // the heard words can never disagree with the visible conversation.
        return true;
    }

    public void SetPrompt(string value, RiverValleyEncounter owner)
    {
        prompt = value;
        promptOwner = owner;
    }

    public void ClearPrompt(RiverValleyEncounter owner)
    {
        if (promptOwner != owner) return;
        prompt = null;
        promptOwner = null;
    }

    public void ClearAllPrompt()
    {
        prompt = null;
        promptOwner = null;
    }

    private void OnGUI()
    {
        if (spark == null) return;
        titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize=19, fontStyle=FontStyle.Bold, normal={textColor=Color.white} };
        statusStyle ??= new GUIStyle(GUI.skin.label) { fontSize=14, normal={textColor=new Color(0.88f,0.94f,1f)} };
        objectiveStyle ??= new GUIStyle(GUI.skin.box) { fontSize=16, fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleCenter, wordWrap=true, normal={textColor=Color.white} };
        bodyStyle ??= new GUIStyle(GUI.skin.box) { fontSize=17, alignment=TextAnchor.MiddleCenter, wordWrap=true, normal={textColor=Color.white} };
        promptStyle ??= new GUIStyle(GUI.skin.box) { fontSize=20, fontStyle=FontStyle.Bold, alignment=TextAnchor.MiddleCenter, normal={textColor=new Color(1f,0.78f,0.18f)} };

        GUI.Box(new Rect(20f, 18f, 370f, 105f), GUIContent.none);
        GUI.Label(new Rect(36f, 25f, 300f, 26f), "DANNY'S SPARK", titleStyle);
        Rect bar = new(36f, 55f, 330f, 18f);
        GUI.Box(bar, GUIContent.none);
        Color old = GUI.color;
        GUI.color = Color.Lerp(new Color(0.48f,0.50f,0.53f), new Color(1f,0.57f,0.06f), spark.Normalized);
        GUI.DrawTexture(new Rect(bar.x+2f, bar.y+2f, (bar.width-4f)*spark.Normalized, bar.height-4f), Texture2D.whiteTexture);
        GUI.color = old;
        string storm = whiteout == null ? "Clear" : whiteout.Stage;
        string audioState = audioDirector != null && audioDirector.Muted ? "Audio muted" : "Audio on";
        bool finalSchoolComplete=finished&&levelThreeTargetCount>0&&
            levelThreeDelivered>=levelThreeTargetCount;
        string group = finalSchoolComplete
            ? $"GAME COMPLETE — final {levelThreeDelivered} children safely inside"
            : finalSchoolCelebrating
            ? $"Final {levelThreeDelivered} children inside  •  Danny entering school"
            : schoolChoiceActive
            ? $"{levelOneDelivered} children safely inside"
            : levelThreeActive
                ? $"Level 3 — {CountMissingChildren()} still missing  •  {levelThreeDelivered} safe at school  •  {rescuedKids} with Danny"
            : levelTwoActive
                ? $"Level 2 — {CountMissingChildren()} still missing  •  {levelTwoDelivered} safe at school  •  {rescuedKids} with Danny"
                : groupWaiting ? $"{rescuedKids}/{totalRescuableKids} children waiting" : groupScattered ? $"Find your group — {rescuedKids}/{totalRescuableKids} together" : $"{rescuedKids}/{totalRescuableKids} children with you";
        GUI.Label(new Rect(36f, 78f, 345f, 38f), $"{group}  •  Cred {streetCred}  •  Route {Progress * 100f:0}%\n{hockeyCards} Oilers cards  •  {storm}  •  {audioState}", statusStyle);
        string guidance = NearestChildGuidance();
        if (!string.IsNullOrEmpty(guidance))
        {
            Color guideColour=GUI.color;
            if(levelThreeActive)GUI.color=new Color(1f,0.72f,0.20f);
            GUI.Box(new Rect(410f,18f,620f,58f), guidance, objectiveStyle);
            GUI.color=guideColour;
        }

        if (Time.time <= messageUntil)
        {
            float width = Mathf.Min(840f, Screen.width - 70f);
            GUI.Box(new Rect((Screen.width-width)*0.5f, Screen.height-128f, width, 88f), $"{speaker}:  {message}", bodyStyle);
        }
        string visiblePrompt = (finished && !schoolChoiceActive)||finalSchoolCelebrating
            ? null
            : schoolChoiceActive
            ? "E — Enter school and continue     •     Q — Play hooky: find the missing kids"
            : !string.IsNullOrEmpty(prompt) ? prompt : groupPrompt;
        if (!string.IsNullOrEmpty(visiblePrompt))
        {
            float width = Mathf.Min(520f, Screen.width - 80f);
            GUI.Box(new Rect((Screen.width-width)*0.5f, Screen.height-190f, width, 48f), visiblePrompt, promptStyle);
        }
        if (Time.time <= controlsUntil&&!finished&&!finalSchoolCelebrating)
        {
            float width = Mathf.Min(760f, Screen.width - 60f);
            GUI.Box(new Rect((Screen.width-width)*0.5f, Screen.height-36f, width, 28f),
                levelThreeActive
                    ? "WASD move  •  Mouse look  •  E gather  •  F flashlight  •  Space hop  •  C camera  •  M sound  •  R centre"
                    : "WASD move  •  Mouse look  •  E help/play  •  Q wait at marked posts  •  Space hop  •  C camera  •  M sound", statusStyle);
        }
    }

    private string NearestChildGuidance()
    {
        if (player == null) return null;
        if(finalSchoolCelebrating)return "EVERY CHILD IS SAFE   •   DANNY'S TURN TO GO INSIDE";
        if(finished&&levelThreeTargetCount>0&&levelThreeDelivered>=levelThreeTargetCount)
            return "GAME COMPLETE   •   EVERY CHILD IS SAFE AT SCHOOL";
        if(schoolChoiceActive)return "LEVEL 1 COMPLETE   •   E SCHOOL   OR   Q PLAY HOOKY AND SEARCH";
        if(levelThreeActive)
        {
            int remaining=CountMissingChildren();
            if(remaining<=0)
            {
                Vector3 school=new(0f,player.position.y,routeFinishZ-4f);
                return player.position.z<routeFinishZ-12f
                    ? DirectionGuidance(school,"LEVEL 3: BRING THE LAST GROUP TO SCHOOL")
                    : "LEVEL 3 COMPLETE   •   Take the group through the school door";
            }
        }
        if(levelTwoActive)
        {
            int remaining=CountMissingChildren();
            if(levelTwoSnowboardRunRequired)
            {
                RiverValleyEncounter snowboard=null;
                if(allEncounters!=null)
                    for(int i=0;i<allEncounters.Length;i++)
                        if(allEncounters[i]!=null&&allEncounters[i].Kind==RiverEncounterKind.SkateDare)
                        {snowboard=allEncounters[i];break;}
                return snowboard!=null
                    ? DirectionGuidance(snowboard.transform.position,"DANNY: SNOWBOARD AGAIN")
                    : "DANNY: Find the stair snowboard again";
            }
            if(!levelTwoWolfParkReached)
            {
                RiverValleySideAdventure[] detours=FindObjectsByType<RiverValleySideAdventure>(FindObjectsSortMode.None);
                for(int i=0;i<detours.Length;i++)
                    if(detours[i]!=null&&detours[i].Kind==RiverSideAdventureKind.CoyoteWoods)
                        return DirectionGuidance(detours[i].transform.position,"DANNY: WOLF PARK — BACK UP THE STAIRS");
                return "DANNY: Wolf Park is back up the stairs";
            }
            if(levelTwoDelivered+rescuedKids>=levelTwoRequiredDelivery)
            {
                Vector3 school=new(0f,player.position.y,routeFinishZ-4f);
                return player.position.z<routeFinishZ-12f
                    ? DirectionGuidance(school,"DANNY: BRING THE RESCUED GROUP BACK TO SCHOOL")
                    : "LEVEL 2 COMPLETE   •   Take the rescued group through the school door";
            }
            if(remaining<=0)
            {
                Vector3 school=new(0f,player.position.y,routeFinishZ-4f);
                return player.position.z<routeFinishZ-12f
                    ? DirectionGuidance(school,"DANNY: SCHOOL'S THIS WAY — FOLLOW ME")
                    : "LEVEL 2 COMPLETE   •   Every missing child reached school";
            }
        }
        if (finished) return $"THE LITTLE PUBLIC SCHOOL REACHED   •   {rescuedKids}/{totalRescuableKids} children safe";
        RiverValleyEncounter nearest = null;
        float best = 9999f;
        if (allEncounters == null) return null;
        foreach (RiverValleyEncounter encounter in allEncounters)
        {
            if (encounter == null || encounter.Used ||
                (encounter.Kind != RiverEncounterKind.LostKid && encounter.Kind != RiverEncounterKind.ParentHandoff)) continue;
            Vector3 destination=encounter.Actor!=null&&encounter.Actor.gameObject.activeInHierarchy
                ? encounter.Actor.position : encounter.transform.position;
            float distance = Vector3.Distance(player.position,destination);
            if (distance < best) { best=distance; nearest=encounter; }
        }
        if(nearest==null)return (levelTwoActive||levelThreeActive)
            ? "DANNY: Every child is found. Take the group back to school."
            : $"THE LITTLE PUBLIC SCHOOL ↑   Follow the half-buried signs   •   Need {requiredKidsForSchool} children";
        if(best>90f&&!levelTwoActive&&!levelThreeActive)return $"THE LITTLE PUBLIC SCHOOL ↑   Follow the half-buried signs   •   Need {requiredKidsForSchool} children";
        Vector3 nearestPosition=nearest.Actor!=null&&nearest.Actor.gameObject.activeInHierarchy
            ? nearest.Actor.position : nearest.transform.position;
        Vector3 direction=Vector3.ProjectOnPlane(nearestPosition-player.position,Vector3.up).normalized;
        Transform reference = Camera.main != null ? Camera.main.transform : player;
        Vector3 referenceForward = Vector3.ProjectOnPlane(reference.forward, Vector3.up).normalized;
        Vector3 referenceRight = Vector3.ProjectOnPlane(reference.right, Vector3.up).normalized;
        float side=Vector3.Dot(referenceRight,direction); float front=Vector3.Dot(referenceForward,direction);
        string arrow=Mathf.Abs(side)>Mathf.Abs(front)?(side>0f?"→":"←"):(front>0f?"↑":"↓");
        string goal = levelThreeActive
            ? $"DANNY'S KID RADAR — {CountMissingChildren()} KIDS THIS WAY"
            : levelTwoActive
            ? $"Level 2 rescue: {Mathf.Min(levelTwoRequiredDelivery,levelTwoDelivered+rescuedKids)}/{levelTwoRequiredDelivery}"
            : rescuedKids >= requiredKidsForSchool
                ? $"Goal met {rescuedKids}/{requiredKidsForSchool}"
                : $"School group {rescuedKids}/{requiredKidsForSchool}";
        string turn=front<-0.35f?"TURN AROUND  •  ":string.Empty;
        return levelThreeActive
            ? $"{goal}   {arrow}   {turn}{best:0} m"
            : $"CHILDREN {arrow}   {best:0} m   •   {goal}";
    }

    private string DirectionGuidance(Vector3 destination,string label)
    {
        Vector3 direction=Vector3.ProjectOnPlane(destination-player.position,Vector3.up).normalized;
        Transform reference=Camera.main!=null?Camera.main.transform:player;
        Vector3 forward=Vector3.ProjectOnPlane(reference.forward,Vector3.up).normalized;
        Vector3 right=Vector3.ProjectOnPlane(reference.right,Vector3.up).normalized;
        float side=Vector3.Dot(right,direction);
        float front=Vector3.Dot(forward,direction);
        string arrow=Mathf.Abs(side)>Mathf.Abs(front)?(side>0f?"→":"←"):(front>0f?"↑":"↓");
        return $"{label} {arrow}   {Vector3.Distance(player.position,destination):0} m";
    }

#if UNITY_EDITOR
    public void Configure(Transform newPlayer, DannySpark newSpark, DannyTestController newMovement,
        CharacterController newController, RiverValleyWhiteout newWhiteout, RiverValleyMomChase newMom,
        float newStartZ, float newFinishZ)
    {
        player=newPlayer; spark=newSpark; movement=newMovement; characterController=newController;
        whiteout=newWhiteout; mom=newMom; routeStartZ=newStartZ; routeFinishZ=newFinishZ;
    }
#endif
}
