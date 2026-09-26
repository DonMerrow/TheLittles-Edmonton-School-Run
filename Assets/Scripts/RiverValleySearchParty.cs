using UnityEngine;

/// <summary>Visible Level 3 patrol for the river rescue/construction crew.</summary>
[DisallowMultipleComponent]
public sealed class RiverValleySearchParty : MonoBehaviour
{
    private RiverValleyGameDirector director;
    private Vector3 home;
    private Vector3 target;
    private Animator[] animators;
    private float phase;
    private readonly RaycastHit[] hits=new RaycastHit[12];
    private static readonly int SpeedHash=Animator.StringToHash("Speed");

    public void Configure(RiverValleyGameDirector owner,int index)
    {
        director=owner;
        phase=index*1.73f;
    }

    private void Start()
    {
        if(director==null)director=FindFirstObjectByType<RiverValleyGameDirector>();
        home=transform.position;
        animators=GetComponentsInChildren<Animator>(true);
        ChooseTarget();
        AddSearchLight();
    }

    private void Update()
    {
        if(director==null||!director.LevelThreeActive)
        {
            SetSpeed(0f);
            return;
        }
        Vector3 flat=Vector3.ProjectOnPlane(target-transform.position,Vector3.up);
        if(flat.magnitude<0.22f){ChooseTarget();SetSpeed(0f);return;}
        Vector3 next=transform.position+flat.normalized*0.92f*Time.deltaTime;
        if(!TryGround(next,out float y)||Mathf.Abs(y-transform.position.y)>0.55f)
        {
            ChooseTarget();
            return;
        }
        next.y=y+0.015f;
        transform.position=next;
        transform.rotation=Quaternion.Slerp(transform.rotation,Quaternion.LookRotation(flat.normalized,Vector3.up),
            1f-Mathf.Exp(-4f*Time.deltaTime));
        SetSpeed(2.45f);
    }

    private void ChooseTarget()
    {
        float angle=Time.time*0.21f+phase;
        target=home+new Vector3(Mathf.Sin(angle),0f,Mathf.Cos(angle))*6.5f;
        if(TryGround(target,out float y))target.y=y+0.015f;
    }

    private bool TryGround(Vector3 point,out float y)
    {
        y=point.y;
        float closest=float.PositiveInfinity;
        int count=Physics.RaycastNonAlloc(point+Vector3.up*6f,Vector3.down,hits,18f,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            RaycastHit hit=hits[i];
            if(hit.collider==null||hit.collider.transform.IsChildOf(transform)||hit.normal.y<0.55f||hit.distance>=closest)continue;
            string label=(hit.collider.name+" "+hit.collider.transform.root.name).ToLowerInvariant();
            if(label.Contains("building")||label.Contains("roof")||label.Contains("solid exterior")||
                label.Contains("tree")||label.Contains("vehicle"))continue;
            closest=hit.distance;
            y=hit.point.y;
        }
        return closest<float.PositiveInfinity;
    }

    private void AddSearchLight()
    {
        GameObject lightObject=new("Crew search lantern");
        lightObject.transform.SetParent(transform,false);
        lightObject.transform.localPosition=new Vector3(0f,1.35f,0.28f);
        Light light=lightObject.AddComponent<Light>();
        light.type=LightType.Point;
        light.color=new Color(1f,0.70f,0.30f);
        light.range=7f;
        light.intensity=2.4f;
    }

    private void SetSpeed(float speed)
    {
        if(animators==null)return;
        foreach(Animator animator in animators)if(animator!=null)animator.SetFloat(SpeedHash,speed);
    }
}
