using UnityEngine;

public enum RiverSideAdventureKind { CoyoteWoods, RabbitMeadow }

/// <summary>Marks a comic optional detour and lets Mom cut off the return route.</summary>
[RequireComponent(typeof(Collider))]
public sealed class RiverValleySideAdventure : MonoBehaviour
{
    [SerializeField] private RiverSideAdventureKind kind;
    [SerializeField] private RiverValleyMomChase mom;
    private RiverValleyGameDirector director;
    private DannySpark spark;
    private float enteredAt;
    private bool inside;
    private bool rewarded;
    public static int DetoursEntered { get; private set; }
    public static int ReturnsCutOffByMom { get; private set; }
    public RiverSideAdventureKind Kind => kind;

    private void Start()
    {
        director=FindFirstObjectByType<RiverValleyGameDirector>();
        spark=FindFirstObjectByType<DannySpark>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponentInParent<DannySpark>()==null)return;
        inside=true; enteredAt=Time.time; DetoursEntered++;
        if(kind==RiverSideAdventureKind.RabbitMeadow)
        {
            if(!rewarded){rewarded=true;spark?.Restore(16f,"A whole field of rabbits restores Danny's Spark.");}
            director?.Show("RABBIT CLUB","We counted twenty-three. Then they moved. Now we have to start again!",5f);
        }
        else
        {
            if(director!=null&&director.LevelTwoActive)
                director.LevelTwoEnteredWolfPark();
            else
                director?.Show("DANNY","Going into the coyote woods is definitely skipping school. It may also be a career choice.",5f);
            spark?.Drain(4f,"Danny takes the extremely unofficial forest route.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(!inside||other.GetComponentInParent<DannySpark>()==null)return;
        inside=false;
        if(Time.time-enteredAt<1.2f)return;
        CompleteReturn();
    }

    private void CompleteReturn()
    {
        // During Level 2 Mom believes Danny is in school and stays at home
        // until the Wolf Park children shout for her.  The old generic detour
        // exit immediately placed her on the road again after either park.
        if(director!=null&&director.LevelTwoActive)return;
        mom?.CutOffSchoolRoute(13f);
        ReturnsCutOffByMom++;
        director?.Show("MOTHER",kind==RiverSideAdventureKind.RabbitMeadow
            ? "There is my Bunny-Boots! Mommy knew the rabbits would return you to the sidewalk!"
            : "Pooky-Wooky! Did you honestly think Mommy would not check the coyote forest?",5.5f);
    }

    public void ForceReturnCutoffForPlaytest()
    {
        inside=false;
        CompleteReturn();
    }

#if UNITY_EDITOR
    public void Configure(RiverSideAdventureKind newKind,RiverValleyMomChase newMom)
    {kind=newKind;mom=newMom;}
#endif
}
