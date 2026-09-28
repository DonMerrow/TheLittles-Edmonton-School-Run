using System.Collections;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class CoyoteCubDetour : MonoBehaviour
{
    [SerializeField] private Transform den;
    [SerializeField] private float safeGroundY=0.08f;
    private bool used;
    public static int CubAdoptions { get; private set; }
    public static int VisibleDragSequences { get; private set; }
    public static int VisibleMomRecoveries { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        DannySpark spark=other.GetComponentInParent<DannySpark>();
        TryBegin(spark);
    }

    public void RearmForLevelTwo() => used=false;

    public void TriggerForLevelTwo(DannySpark spark) => TryBegin(spark);

    private void TryBegin(DannySpark spark)
    {
        if(used||spark==null)return;
        used=true;
        StartCoroutine(Adopt(spark));
    }

    private IEnumerator Adopt(DannySpark spark)
    {
        CubAdoptions++;
        RiverValleyGameDirector director=FindFirstObjectByType<RiverValleyGameDirector>();
        DannyTestController motor=spark.GetComponent<DannyTestController>();
        CharacterController controller=spark.GetComponent<CharacterController>();
        RiverValleyAnimalMotion leader=FindNearestCoyote(spark.transform.position);
        RiverValleyMomChase mom=FindFirstObjectByType<RiverValleyMomChase>();
        DannyFollowCamera followCamera=FindFirstObjectByType<DannyFollowCamera>();
        RiverValleyKidFollower[] travellingChildren=FindObjectsByType<RiverValleyKidFollower>(FindObjectsSortMode.None)
            .Where(child=>child!=null&&child.enabled&&!child.IsScattered&&!child.IsWaiting&&!child.IsBuried)
            .ToArray();
        director?.SetExternalCinematicBusy(true);
        if(motor!=null)motor.enabled=false;
        if(controller!=null)controller.enabled=false;
        Vector3 start=spark.transform.position;
        start.y=GroundY(start,safeGroundY);
        spark.transform.position=start;
        followCamera?.FocusCinematic(start+Vector3.up*0.9f,5.5f);

        // Freeze the current positions first, then let the children visibly
        // squeeze into a close huddle. The former hard-coded Wolf Park slots
        // could be half a field away and made the whole group appear to zoom.
        Vector3 huddleForward=Vector3.ProjectOnPlane(spark.transform.forward,Vector3.up).normalized;
        if(huddleForward.sqrMagnitude<0.01f)huddleForward=Vector3.forward;
        Vector3 huddleRight=Vector3.Cross(Vector3.up,huddleForward).normalized;
        Vector3[] childStarts=new Vector3[travellingChildren.Length];
        Vector3[] childTargets=new Vector3[travellingChildren.Length];
        for(int i=0;i<travellingChildren.Length;i++)
        {
            int column=i%3;
            int row=i/3;
            childStarts[i]=travellingChildren[i].transform.position;
            childTargets[i]=start-huddleForward*(1.7f+row*0.72f)+
                huddleRight*((column-1f)*0.72f);
            childTargets[i].y=GroundY(childTargets[i],start.y);
            travellingChildren[i].HoldForCinematic(childStarts[i],start);
        }
        director?.Show("CHILDREN","Huddle up! Eyes on Danny. Those are definitely not puppies.",2.3f);
        for(float time=0f;time<0.85f;time+=Time.deltaTime)
        {
            float t=Mathf.SmoothStep(0f,1f,time/0.85f);
            for(int i=0;i<travellingChildren.Length;i++)
                if(travellingChildren[i]!=null)
                    travellingChildren[i].transform.position=Vector3.Lerp(childStarts[i],childTargets[i],t);
            followCamera?.TrackCinematic(start+Vector3.up*0.86f);
            yield return null;
        }
        if(leader!=null)leader.enabled=false;
        director?.Show("DANNY","They are sniffing my mittens. That feels like an interview.",2.8f);
        Vector3 finish=den!=null?den.position:transform.position+Vector3.left*5f;
        finish.y=GroundY(finish,start.y);

        if(leader!=null)
        {
            Vector3 leaderStart=leader.transform.position;
            Vector3 toward=Vector3.ProjectOnPlane(start-leaderStart,Vector3.up);
            Vector3 inspection=toward.sqrMagnitude>0.01f?start-toward.normalized*1.05f:start+Vector3.right;
            inspection.y=GroundY(inspection,start.y);
            for(float time=0f;time<1.15f;time+=Time.deltaTime)
            {
                float t=Mathf.SmoothStep(0f,1f,time/1.15f);
                leader.transform.position=Vector3.Lerp(leaderStart,inspection,t);
                if(toward.sqrMagnitude>0.01f)leader.transform.rotation=Quaternion.LookRotation(toward.normalized,Vector3.up);
                followCamera?.TrackCinematic(Vector3.Lerp(spark.transform.position,leader.transform.position,0.5f)+Vector3.up*0.75f);
                yield return null;
            }
        }
        director?.Show("COYOTE PACK","The coyotes have voted. Danny is now their least furry cub.",3.4f);
        yield return new WaitForSeconds(0.35f);
        director?.Show("CHILDREN","Danny! Danny! The wolves are taking you! We are calling your mom!",3.2f);
        Vector3 leaderOffset=Vector3.ProjectOnPlane(start-finish,Vector3.up).normalized*0.85f;
        for(float time=0f;time<3.2f;time+=Time.deltaTime)
        {
            float t=Mathf.SmoothStep(0f,1f,time/3.2f);
            Vector3 p=Vector3.Lerp(start,finish,t);
            p.y=Mathf.Lerp(start.y,finish.y,t);
            spark.transform.position=p;
            Vector3 travel=Vector3.ProjectOnPlane(finish-start,Vector3.up);
            if(travel.sqrMagnitude>0.01f)spark.transform.rotation=Quaternion.LookRotation(travel.normalized,Vector3.up);
            if(leader!=null)
            {
                Vector3 leaderPosition=p+leaderOffset;
                leaderPosition.y=p.y;
                leader.transform.position=leaderPosition;
                if(travel.sqrMagnitude>0.01f)leader.transform.rotation=Quaternion.LookRotation(travel.normalized,Vector3.up);
            }
            followCamera?.TrackCinematic(Vector3.Lerp(p,leader!=null?leader.transform.position:p,0.5f)+Vector3.up*0.78f);
            yield return null;
        }
        VisibleDragSequences++;
        spark.transform.position=finish;
        spark.Drain(10f,"Coyote cub orientation uses a surprising amount of Spark.");
        director?.Show("DANNY","I have learned one howl and no useful directions. I may actually live here now.",3.0f);
        yield return new WaitForSeconds(0.55f);

        if(mom!=null)
        {
            mom.enabled=false;
            Animator momAnimator=mom.GetComponentInChildren<Animator>();
            Vector3 rescueStart=finish+new Vector3(7.5f,0f,-2.2f);
            Vector3 rescueStop=finish+new Vector3(2.0f,0f,-0.7f);
            rescueStart.y=GroundY(rescueStart,finish.y);
            rescueStop.y=GroundY(rescueStop,finish.y);
            mom.transform.position=rescueStart;
            mom.transform.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(rescueStop-rescueStart,Vector3.up),Vector3.up);
            director?.Show("MOTHER","Puppies! Shoo! That orange one is mine. Danny Spark, you are not being raised by coyotes today!",4.2f);
            yield return MoveActor(mom.transform,rescueStart,rescueStop,1.75f,momAnimator,followCamera,rescueStop.y);
            VisibleMomRecoveries++;

            if(leader!=null)
            {
                Vector3 retreat=leader.transform.position+new Vector3(-5.5f,0f,2.5f);
                retreat.y=GroundY(retreat,finish.y);
                director?.Show("COYOTE PACK","The pack respects the much louder parent and remembers an appointment elsewhere.",2.7f);
                yield return MoveActor(leader.transform,leader.transform.position,retreat,0.95f,
                    leader.GetComponentInChildren<Animator>(),followCamera,retreat.y);
            }

            director?.Show("MOTHER","Come along, my little wolf-puppy. Mommy is returning you to the sidewalk—with all your fingers counted.",4.4f);
            Vector3 trailGate=new(-18f,0.08f,72f);
            Vector3 safeReturn=new(-4.2f,0.08f,62f);
            yield return MoveDannyAndMom(spark.transform,mom.transform,finish,trailGate,1.35f,momAnimator,followCamera,safeGroundY);
            yield return MoveDannyAndMom(spark.transform,mom.transform,trailGate,safeReturn,1.55f,momAnimator,followCamera,safeGroundY);

            // The rescue needs a comic landing. Mom used to disappear as soon
            // as Danny reached the sidewalk, which removed the affectionate,
            // embarrassing lecture that gives her chase its personality.
            director?.Show("MOTHER",
                "Bad wolf! Danny Spark already has a mother, a lunch, and three perfectly good emergency mittens!",4.4f);
            yield return new WaitForSeconds(4.0f);
            director?.Show("DANNY",
                "They were organized, and I was helping the children.",3.2f);
            yield return new WaitForSeconds(2.9f);
            director?.Show("MOTHER",
                "You may finish helping. Then it is cocoa, a finger count, a coat inspection, and the complete wolf-safety lecture.",4.8f);
            yield return new WaitForSeconds(4.4f);
            director?.Show("DANNY",
                "I need an adult. A less prepared adult.",3.2f);
            yield return new WaitForSeconds(2.8f);
            mom.enabled=true;
            mom.ReturnHomeAfterWolfRescue();
        }
        else
        {
            director?.Show("DANNY","The pack has reconsidered. School is apparently safer than cub orientation.",3.0f);
        }

        if(controller!=null)controller.enabled=true;
        if(motor!=null){motor.enabled=true;motor.ResetVerticalMotion();}
        if(leader!=null)leader.enabled=true;
        Vector3 resumeForward=Vector3.ProjectOnPlane(spark.transform.forward,Vector3.up).normalized;
        if(resumeForward.sqrMagnitude<0.01f)resumeForward=Vector3.forward;
        Vector3 resumeRight=Vector3.Cross(Vector3.up,resumeForward).normalized;
        for(int i=0;i<travellingChildren.Length;i++)
        {
            int column=i%2;
            int row=i/2;
            Vector3 rejoin=spark.transform.position+resumeRight*((column-0.5f)*1.15f)-
                resumeForward*(1.25f+row*1.05f);
            rejoin.y=GroundY(rejoin,spark.transform.position.y);
            travellingChildren[i].ResumeAfterCinematic(spark.transform,rejoin);
        }
        followCamera?.RecenterOnDanny();
        director?.SetExternalCinematicBusy(false);
    }

    private static IEnumerator MoveActor(Transform actor,Vector3 from,Vector3 to,float duration,Animator animator,
        DannyFollowCamera camera,float groundY)
    {
        if(actor==null)yield break;
        Vector3 travel=Vector3.ProjectOnPlane(to-from,Vector3.up);
        if(travel.sqrMagnitude>0.001f)actor.rotation=Quaternion.LookRotation(travel.normalized,Vector3.up);
        animator?.SetFloat("Speed",3.2f);
        for(float time=0f;time<duration;time+=Time.deltaTime)
        {
            Vector3 position=Vector3.Lerp(from,to,Mathf.SmoothStep(0f,1f,time/duration));
            position.y=groundY;
            actor.position=position;
            camera?.TrackCinematic(actor.position+Vector3.up*0.82f);
            yield return null;
        }
        actor.position=to;
        animator?.SetFloat("Speed",0f);
    }

    private static IEnumerator MoveDannyAndMom(Transform danny,Transform mom,Vector3 from,Vector3 to,
        float duration,Animator momAnimator,DannyFollowCamera camera,float groundY)
    {
        Vector3 travel=Vector3.ProjectOnPlane(to-from,Vector3.up);
        Vector3 direction=travel.sqrMagnitude>0.001f?travel.normalized:Vector3.forward;
        if(danny!=null)danny.rotation=Quaternion.LookRotation(direction,Vector3.up);
        if(mom!=null)mom.rotation=Quaternion.LookRotation(direction,Vector3.up);
        momAnimator?.SetFloat("Speed",3.0f);
        for(float time=0f;time<duration;time+=Time.deltaTime)
        {
            float t=Mathf.SmoothStep(0f,1f,time/duration);
            Vector3 dannyPosition=Vector3.Lerp(from,to,t);
            dannyPosition.y=groundY;
            if(danny!=null)danny.position=dannyPosition;
            if(mom!=null)
            {
                Vector3 momPosition=dannyPosition-direction*0.95f+Vector3.right*0.55f;
                momPosition.y=groundY;
                mom.position=momPosition;
            }
            camera?.TrackCinematic(Vector3.Lerp(dannyPosition,mom!=null?mom.position:dannyPosition,0.5f)+Vector3.up*0.8f);
            yield return null;
        }
        momAnimator?.SetFloat("Speed",0f);
    }

    private static RiverValleyAnimalMotion FindNearestCoyote(Vector3 position)
    {
        RiverValleyAnimalMotion best=null;
        float bestDistance=float.PositiveInfinity;
        foreach(RiverValleyAnimalMotion candidate in FindObjectsByType<RiverValleyAnimalMotion>(FindObjectsSortMode.None))
        {
            if(candidate==null)continue;
            string animalName=candidate.name.ToLowerInvariant();
            if(!animalName.Contains("coyote")&&!animalName.Contains("wolf"))continue;
            float distance=(candidate.transform.position-position).sqrMagnitude;
            if(distance>=bestDistance)continue;
            bestDistance=distance;
            best=candidate;
        }
        return best;
    }

    private static float GroundY(Vector3 position,float fallback)
    {
        RaycastHit[] hits=Physics.RaycastAll(position+Vector3.up*7f,Vector3.down,20f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        float bestDistance=float.PositiveInfinity;
        float ground=fallback;
        for(int i=0;i<hits.Length;i++)
        {
            Collider collider=hits[i].collider;
            if(collider==null||hits[i].normal.y<0.58f||hits[i].distance>=bestDistance)continue;
            if(collider.GetComponentInParent<RiverValleyKidFollower>()!=null||
                collider.GetComponentInParent<DannySpark>()!=null||
                collider.GetComponentInParent<RiverValleyAnimalMotion>()!=null)continue;
            bestDistance=hits[i].distance;
            ground=hits[i].point.y+0.015f;
        }
        return ground;
    }

#if UNITY_EDITOR
    public void Configure(Transform newDen,float newSafeGroundY){den=newDen;safeGroundY=newSafeGroundY;}
#endif
}
