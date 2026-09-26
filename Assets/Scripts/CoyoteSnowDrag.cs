using UnityEngine;

/// <summary>
/// Coyotes investigate children left buried in a plow bank. The drag is slow,
/// comic pressure rather than an attack, and stops as soon as Danny regathers
/// the child.
/// </summary>
public sealed class CoyoteSnowDrag : MonoBehaviour
{
    private RiverValleyKidFollower target;
    private Transform coyote;
    private RiverValleyAnimalMotion coyoteMotion;
    private RiverValleyGameDirector director;
    private float nextSearch;
    private float nextTug;
    private float draggedDistance;
    private bool announced;
    private static int tugEvents;

    public static int TugEvents => tugEvents;

    private void Start()
    {
        director=FindFirstObjectByType<RiverValleyGameDirector>();
    }

    private void Update()
    {
        if(target==null||!target.IsBuried)
        {
            ReleaseCoyote();
            if(Time.time<nextSearch)return;
            nextSearch=Time.time+0.65f;
            foreach(RiverValleyKidFollower child in FindObjectsByType<RiverValleyKidFollower>(FindObjectsSortMode.None))
                if(child!=null&&child.IsBuried&&!child.IsTumbling){target=child;break;}
            if(target==null)return;
            coyote=FindNearestCoyote(target.transform.position);
            if(coyote==null){target=null;return;}
            coyoteMotion=coyote.GetComponent<RiverValleyAnimalMotion>();
            if(coyoteMotion!=null)coyoteMotion.enabled=false;
            draggedDistance=0f;
            announced=false;
        }

        Vector3 flat=Vector3.ProjectOnPlane(target.transform.position-coyote.position,Vector3.up);
        if(flat.magnitude>1.18f)
        {
            MoveCoyote(flat.normalized,1.55f);
            return;
        }
        if(!announced)
        {
            announced=true;
            director?.CoyoteDraggingChild();
        }
        Vector3 away=director!=null&&director.Player!=null?
            Vector3.ProjectOnPlane(coyote.position-director.Player.position,Vector3.up).normalized:coyote.right;
        if(away.sqrMagnitude<0.01f)away=coyote.right;
        if(draggedDistance<4.2f)MoveCoyote(away,0.82f);
        if(Time.time>=nextTug)
        {
            nextTug=Time.time+0.18f;
            float tug=0.16f;
            target.TugByCoyote(coyote.position,tug);
            draggedDistance+=tug;
            tugEvents++;
        }
    }

    private void MoveCoyote(Vector3 direction,float speed)
    {
        if(coyote==null)return;
        Vector3 next=coyote.position+direction*speed*Time.deltaTime;
        if(Physics.Raycast(next+Vector3.up*2f,Vector3.down,out RaycastHit hit,5f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&
            Mathf.Abs(hit.point.y-coyote.position.y)<=0.42f)next.y=hit.point.y;
        else next.y=coyote.position.y;
        coyote.position=next;
        coyote.rotation=Quaternion.Slerp(coyote.rotation,Quaternion.LookRotation(direction,Vector3.up),
            1f-Mathf.Exp(-6f*Time.deltaTime));
    }

    private static Transform FindNearestCoyote(Vector3 position)
    {
        Transform best=null;
        float distance=float.PositiveInfinity;
        foreach(RiverValleyAnimalMotion motion in FindObjectsByType<RiverValleyAnimalMotion>(FindObjectsSortMode.None))
        {
            if(motion==null||!motion.name.ToLowerInvariant().Contains("coyote"))continue;
            float candidate=(motion.transform.position-position).sqrMagnitude;
            if(candidate<distance){distance=candidate;best=motion.transform;}
        }
        return best;
    }

    private void ReleaseCoyote()
    {
        if(coyoteMotion!=null)coyoteMotion.enabled=true;
        coyoteMotion=null;
        coyote=null;
        target=null;
    }
}
