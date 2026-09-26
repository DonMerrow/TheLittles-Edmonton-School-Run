using UnityEngine;

/// <summary>A devoted rabbit-club member comments on the meadow but stays with Danny.</summary>
public sealed class RiverValleyRabbitClubRunaway : MonoBehaviour
{
    [SerializeField] private Vector3 rabbitHome;
    private RiverValleyKidFollower follower;
    private RiverValleyGameDirector director;
    private bool ranBack;

    private void Start()
    {
        director=FindFirstObjectByType<RiverValleyGameDirector>();
        if(rabbitHome==Vector3.zero)rabbitHome=transform.position;
    }

    private void Update()
    {
        if(ranBack)return;
        if(follower==null)follower=GetComponent<RiverValleyKidFollower>();
        // The one-time rabbit-club joke belongs to the first school run. In
        // later search levels, a just-rescued child must remain rescued and
        // follow Danny back to the school like every other group member.
        if(director!=null&&(director.LevelTwoActive||director.LevelThreeActive))
        {
            ranBack=true;
            return;
        }
        if(follower==null||follower.IsScattered||follower.IsWaiting||follower.FollowingSeconds<5.5f)return;
        if(Vector3.ProjectOnPlane(transform.position-rabbitHome,Vector3.up).magnitude<4f)return;
        ranBack=true;
        director?.Show("JUNIE","I miss the rabbits, but I am staying with Danny. We can visit the fluffy club after school.",5f);
    }

#if UNITY_EDITOR
    public void Configure(Vector3 home) => rabbitHome=home;
#endif
}
