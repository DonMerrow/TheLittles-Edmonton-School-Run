using UnityEngine;

public sealed class RiverValleySchoolStream : MonoBehaviour
{
    [SerializeField] private Vector3 destination;
    [SerializeField] private float speed=0.82f;
    private Animator[] animators;
    private Transform[] castMembers;
    private readonly RaycastHit[] groundHits=new RaycastHit[12];
    private static readonly int SpeedHash=Animator.StringToHash("Speed");
    private void Start()
    {
        animators=GetComponentsInChildren<Animator>(true);
        WinterCastIdentity[] identities=GetComponentsInChildren<WinterCastIdentity>(true);
        castMembers=new Transform[identities.Length];
        for(int i=0;i<identities.Length;i++)castMembers[i]=identities[i]!=null?identities[i].transform:null;
    }
    private void Update()
    {
        Vector3 flat=Vector3.ProjectOnPlane(destination-transform.position,Vector3.up);
        float gait=0f;
        if(flat.magnitude>0.25f)
        {
            Vector3 direction=flat.normalized;
            Vector3 next=transform.position+direction*speed*Time.deltaTime;
            if(!TryFindGround(next,out RaycastHit hit)||Mathf.Abs(hit.point.y-transform.position.y)>0.62f)
            {
                foreach(Animator stopped in animators)if(stopped!=null)stopped.SetFloat(SpeedHash,0f,0.1f,Time.deltaTime);
                return;
            }
            next.y=hit.point.y+0.015f;
            transform.position=next;
            transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(direction,Vector3.up),Time.deltaTime*4f);
            gait=2.35f;
        }
        foreach(Animator animator in animators)if(animator!=null)animator.SetFloat(SpeedHash,gait,0.1f,Time.deltaTime);
    }

    private void LateUpdate()
    {
        if(castMembers==null)return;
        foreach(Transform member in castMembers)
            if(member!=null&&member!=transform)member.rotation=transform.rotation;
    }

    private bool TryFindGround(Vector3 position,out RaycastHit best)
    {
        best=default;
        float nearest=float.PositiveInfinity;
        int count=Physics.RaycastNonAlloc(position+Vector3.up*4f,Vector3.down,groundHits,10f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            RaycastHit candidate=groundHits[i];
            if(candidate.collider==null||candidate.collider.transform.IsChildOf(transform)||candidate.distance>=nearest)continue;
            nearest=candidate.distance;
            best=candidate;
        }
        return nearest<float.PositiveInfinity;
    }
#if UNITY_EDITOR
    public void Configure(Vector3 newDestination,float newSpeed){destination=newDestination;speed=newSpeed;}
#endif
}
