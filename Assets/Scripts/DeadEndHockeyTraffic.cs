using System.Collections;
using UnityEngine;

/// <summary>A dead-end road game that physically clears its goal for a passing car.</summary>
public sealed class DeadEndHockeyTraffic : MonoBehaviour
{
    [SerializeField] private Transform goal;
    [SerializeField] private Transform trafficCar;
    [SerializeField] private Transform teenA;
    [SerializeField] private Transform teenB;
    [SerializeField] private WinterHockeyRally rally;
    [SerializeField] private Vector3 parkedGoalPosition;
    private Vector3 goalHome;
    private Vector3 teenAHome;
    private Vector3 teenBHome;
    private Quaternion teenARotation;
    private Quaternion teenBRotation;
    private RiverValleyGameDirector director;
    private bool clearing;
    private bool waving;
    private float nextClearance;
    private Transform waveArmA;
    private Transform waveArmB;
    private Animator[] teenAAnimators;
    private Animator[] teenBAnimators;
    private static readonly int SpeedHash=Animator.StringToHash("Speed");

    public static int GoalsCleared { get; private set; }
    public static int CarsWavedThrough { get; private set; }

    private void Start()
    {
        director=FindFirstObjectByType<RiverValleyGameDirector>();
        if(goal!=null)goalHome=goal.position;
        if(teenA!=null)
        {
            teenAHome=teenA.position;
            teenARotation=teenA.rotation;
            teenAAnimators=teenA.GetComponentsInChildren<Animator>(true);
            waveArmA=FindWaveArm(teenAAnimators);
        }
        if(teenB!=null)
        {
            teenBHome=teenB.position;
            teenBRotation=teenB.rotation;
            teenBAnimators=teenB.GetComponentsInChildren<Animator>(true);
            waveArmB=FindWaveArm(teenBAnimators);
        }
        nextClearance=Time.time+2f;
    }

    private void Update()
    {
        if(clearing||goal==null||trafficCar==null||Time.time<nextClearance)return;
        Vector3 delta=Vector3.ProjectOnPlane(trafficCar.position-goal.position,Vector3.up);
        if(delta.magnitude<13.5f)StartCoroutine(ClearForCar());
    }

    private void LateUpdate()
    {
        if(!waving)return;
        float wave=Mathf.Sin(Time.time*9f)*24f;
        Wave(waveArmA,wave);
        Wave(waveArmB,-wave);
    }

    private IEnumerator ClearForCar()
    {
        clearing=true;
        SetHockeyEnabled(false);
        bool narrate=ShouldNarrate();
        if(narrate)director?.Show("HOCKEY TEENS","CAR! Goal to the snowbank—move, move!",3.2f);
        Vector3 teenAPark=teenAHome+new Vector3(4.4f,0f,0.2f);
        Vector3 teenBPark=teenBHome+new Vector3(2.4f,0f,-0.1f);
        FaceCar();
        SetSpeed(teenAAnimators,2.5f);
        SetSpeed(teenBAnimators,2.5f);
        for(float time=0f;time<1.35f;time+=Time.deltaTime)
        {
            float t=Mathf.SmoothStep(0f,1f,time/1.35f);
            goal.position=Vector3.Lerp(goalHome,parkedGoalPosition,t);
            if(teenA!=null)teenA.position=Vector3.Lerp(teenAHome,teenAPark,t);
            if(teenB!=null)teenB.position=Vector3.Lerp(teenBHome,teenBPark,t);
            yield return null;
        }
        GoalsCleared++;
        SetSpeed(teenAAnimators,0f);
        SetSpeed(teenBAnimators,0f);
        waving=true;
        float waitStarted=Time.time;
        while(trafficCar!=null&&Vector3.Distance(trafficCar.position,goalHome)<17f&&Time.time-waitStarted<5f)
        {
            FaceCar();
            yield return null;
        }
        CarsWavedThrough++;
        if(narrate&&ShouldNarrate())
            director?.Show("HOCKEY TEENS","Thanks! Game on—first to three before the bell!",3.2f);
        yield return new WaitForSeconds(0.45f);
        waving=false;
        SetSpeed(teenAAnimators,2.3f);
        SetSpeed(teenBAnimators,2.3f);
        for(float time=0f;time<1.35f;time+=Time.deltaTime)
        {
            float t=Mathf.SmoothStep(0f,1f,time/1.35f);
            goal.position=Vector3.Lerp(parkedGoalPosition,goalHome,t);
            if(teenA!=null)teenA.position=Vector3.Lerp(teenAPark,teenAHome,t);
            if(teenB!=null)teenB.position=Vector3.Lerp(teenBPark,teenBHome,t);
            yield return null;
        }
        goal.position=goalHome;
        if(teenA!=null)teenA.SetPositionAndRotation(teenAHome,teenARotation);
        if(teenB!=null)teenB.SetPositionAndRotation(teenBHome,teenBRotation);
        SetSpeed(teenAAnimators,0f);
        SetSpeed(teenBAnimators,0f);
        SetHockeyEnabled(true);
        nextClearance=Time.time+7f;
        clearing=false;
    }

    private bool ShouldNarrate()
    {
        if(director==null||director.IsBusy||director.Player==null)return false;
        Vector3 centre=goal!=null?goal.position:transform.position;
        if(Vector3.ProjectOnPlane(director.Player.position-centre,Vector3.up).magnitude>30f)return false;
        Camera gameCamera=Camera.main;
        if(gameCamera==null)return true;
        Vector3 viewport=gameCamera.WorldToViewportPoint(centre+Vector3.up);
        return viewport.z>0f&&viewport.x>-0.12f&&viewport.x<1.12f&&
            viewport.y>-0.12f&&viewport.y<1.12f;
    }

    private void SetHockeyEnabled(bool value)
    {
        if(rally!=null)rally.enabled=value;
        if(teenA!=null&&teenA.TryGetComponent(out WinterCastActivity activityA))activityA.enabled=value;
        if(teenB!=null&&teenB.TryGetComponent(out WinterCastActivity activityB))activityB.enabled=value;
    }

    private void FaceCar()
    {
        if(trafficCar==null)return;
        Face(teenA,trafficCar.position);
        Face(teenB,trafficCar.position);
    }

    private static void Face(Transform actor,Vector3 point)
    {
        if(actor==null)return;
        Vector3 direction=Vector3.ProjectOnPlane(point-actor.position,Vector3.up);
        if(direction.sqrMagnitude>0.01f)actor.rotation=Quaternion.LookRotation(direction.normalized,Vector3.up);
    }

    private static Transform FindWaveArm(Animator[] animators)
    {
        if(animators==null)return null;
        foreach(Animator animator in animators)
            if(animator!=null&&animator.isHuman)
            {
                Transform arm=animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
                if(arm!=null)return arm;
            }
        return null;
    }

    private static void Wave(Transform arm,float degrees)
    {
        if(arm!=null)arm.localRotation*=Quaternion.Euler(-58f,degrees,24f);
    }

    private static void SetSpeed(Animator[] animators,float speed)
    {
        if(animators==null)return;
        foreach(Animator animator in animators)
            if(animator!=null)animator.SetFloat(SpeedHash,speed);
    }

#if UNITY_EDITOR
    public void Configure(Transform newGoal,Transform newTrafficCar,Transform firstTeen,Transform secondTeen,
        WinterHockeyRally newRally,Vector3 newParkedGoalPosition)
    {
        goal=newGoal;
        trafficCar=newTrafficCar;
        teenA=firstTeen;
        teenB=secondTeen;
        rally=newRally;
        parkedGoalPosition=newParkedGoalPosition;
    }
#endif
}
