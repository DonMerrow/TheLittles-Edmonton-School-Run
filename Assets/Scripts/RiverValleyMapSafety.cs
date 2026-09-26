using UnityEngine;

// Last-resort containment for a very long, highly vertical level. Physical
// stair rails stop ordinary mistakes; this catches a controller that somehow
// leaves every collider and returns Danny to his last grounded step.
[DefaultExecutionOrder(900)]
public sealed class RiverValleyMapSafety : MonoBehaviour
{
    public static int Restorations { get; private set; }
    private Transform player;
    private CharacterController controller;
    private DannyTestController movement;
    private Vector3 lastSafePosition;
    private Quaternion lastSafeRotation;
    private float nextSample;
    private float nextRestore;
    private float unsupportedSince=-1f;
    private readonly RaycastHit[] supportHits=new RaycastHit[24];

    private void Awake()
    {
        // The generated scene is also opened as a fresh standalone build, so
        // editor-time references cannot be relied on here. Reconnect to Danny
        // on every launch before the first safety check runs.
        if(player!=null)return;
        DannySpark spark=FindFirstObjectByType<DannySpark>();
        if(spark==null)return;
        Transform foundPlayer=spark.transform;
        Configure(foundPlayer,foundPlayer.GetComponent<CharacterController>());
    }

    private void Start()
    {
        ReplaceBuildingProxyColliders();
        FillUpperStreetEdge();
        MakeWorldEdgesVisible();
    }

    private static void FillUpperStreetEdge()
    {
        if(GameObject.Find("Upper district snow continuation")!=null)return;
        // The backdrop was beyond the last snow platform, leaving a pit in
        // front of a solid-looking city wall. Continue the ground to that wall.
        GameObject ground=GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name="Upper district snow continuation";
        ground.transform.SetPositionAndRotation(new Vector3(0f,-0.475f,42f),Quaternion.identity);
        ground.transform.localScale=new Vector3(201f,0.76f,189f);
        Shader shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
        ground.GetComponent<Renderer>().sharedMaterial=EdgeMaterial(shader,new Color(0.81f,0.91f,0.97f));
    }

    private static void ReplaceBuildingProxyColliders()
    {
        foreach(BoxCollider proxy in FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
        {
            if(proxy==null||!proxy.name.ToLowerInvariant().Contains("solid exterior"))continue;
            string buildingName=proxy.name;
            int suffix=buildingName.IndexOf("— solid exterior",System.StringComparison.OrdinalIgnoreCase);
            if(suffix<0)suffix=buildingName.IndexOf("- solid exterior",System.StringComparison.OrdinalIgnoreCase);
            if(suffix>=0)buildingName=buildingName.Substring(0,suffix).Trim();
            GameObject building=GameObject.Find(buildingName);
            if(building==null||building.transform==proxy.transform)continue;

            bool added=false;
            foreach(MeshFilter filter in building.GetComponentsInChildren<MeshFilter>(true))
            {
                if(filter==null||filter.sharedMesh==null||filter.GetComponent<Renderer>()==null)continue;
                if(filter.GetComponent<Collider>()!=null){added=true;continue;}
                Bounds bounds=filter.sharedMesh.bounds;
                if(bounds.size.sqrMagnitude<0.0025f)continue;
                BoxCollider visibleCollider=filter.gameObject.AddComponent<BoxCollider>();
                visibleCollider.center=bounds.center;
                visibleCollider.size=bounds.size;
                added=true;
            }
            // The old single bounding box blocked courtyards, awnings and
            // alleys that had no wall. Mesh-piece boxes follow only visible
            // building parts, so an obstacle now always has a visible cause.
            // Residential buildings have no playable interior. Keep their
            // close-fitting exterior shell solid so Danny cannot slip through
            // gaps between imported wall meshes or pass through a doorway.
            if(added&&!buildingName.ToLowerInvariant().Contains("home")&&
               !buildingName.ToLowerInvariant().Contains("house"))proxy.enabled=false;
        }
    }

    private static void MakeWorldEdgesVisible()
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
        Material[] fronts=
        {
            EdgeMaterial(shader,new Color(0.28f,0.38f,0.43f)),
            EdgeMaterial(shader,new Color(0.48f,0.31f,0.28f)),
            EdgeMaterial(shader,new Color(0.34f,0.37f,0.32f)),
            EdgeMaterial(shader,new Color(0.36f,0.32f,0.43f))
        };
        Material windows=EdgeMaterial(shader,new Color(0.74f,0.86f,0.89f));
        Material roof=EdgeMaterial(shader,new Color(0.16f,0.23f,0.29f));
        Material stone=EdgeMaterial(shader,new Color(0.32f,0.37f,0.40f));
        foreach(BoxCollider edge in FindObjectsByType<BoxCollider>(FindObjectsSortMode.None))
        {
            if(edge==null||!edge.name.ToLowerInvariant().Contains("playable world")||
                edge.transform.Find("City edge backdrop")!=null)continue;
            Vector3 world=edge.transform.TransformPoint(edge.center);
            Vector3 size=Vector3.Scale(edge.size,edge.transform.lossyScale);
            bool alongX=size.x>size.z;
            float length=alongX?size.x:size.z;
            Vector3 inward=alongX
                ? (world.z<0f?Vector3.forward:Vector3.back)
                : (world.x<0f?Vector3.right:Vector3.left);
            Vector3 along=alongX?Vector3.right:Vector3.forward;
            Quaternion facing=Quaternion.LookRotation(inward,Vector3.up);
            GameObject backdrop=new("City edge backdrop");
            backdrop.transform.SetParent(edge.transform,true);
            backdrop.transform.position=world;
            const float width=15.5f;
            int tiles=Mathf.CeilToInt(length/width);
            for(int i=0;i<tiles;i++)
            {
                float centre=-length*0.5f+(i+0.5f)*length/tiles;
                float tileWidth=length/tiles+0.08f;
                float height=6.5f+(i*7%5)*0.75f;
                Vector3 basePoint=world+along*centre-inward*0.30f;
                basePoint.y=0f;
                Transform block=new GameObject($"Neighbourhood edge building {i+1}").transform;
                block.SetParent(backdrop.transform,true);
                block.SetPositionAndRotation(basePoint,facing);
                // A real skyline hides the old featureless horizon. The
                // invisible 48-metre safety collider stays behind each front.
                AddVisibleBoundary(block,"Brick facade",new Vector3(0f,height*0.5f,0f),
                    new Vector3(tileWidth,height,1.15f),fronts[i%fronts.Length]);
                AddVisibleBoundary(block,"Snowy roofline",new Vector3(0f,height+0.12f,0.10f),
                    new Vector3(tileWidth+0.25f,0.30f,1.55f),roof);
                for(int row=0;row<2;row++)
                    for(int col=0;col<3;col++)
                        AddVisibleBoundary(block,$"Window {row+1}-{col+1}",
                            new Vector3((col-1)*tileWidth*0.27f,1.8f+row*2.35f,0.60f),
                            new Vector3(1.45f,1.25f,0.07f),windows);
                // Below street level, the same edge reads as a stepped stone
                // retaining wall from the river valley, not a fall into void.
                AddVisibleBoundary(block,"Valley retaining stone",new Vector3(0f,-12.5f,0f),
                    new Vector3(tileWidth,25f,1.35f),stone);
                for(int course=0;course<5;course++)
                    AddVisibleBoundary(block,$"Stone course {course+1}",
                        new Vector3(0f,-2.4f-course*4.5f,0.73f),
                        new Vector3(tileWidth,0.11f,0.08f),roof);
            }
        }
    }

    private static Material EdgeMaterial(Shader shader,Color color)
    {
        Material material=new(shader){color=color,hideFlags=HideFlags.DontSave};
        if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);
        return material;
    }

    private static void AddVisibleBoundary(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        GameObject wall=GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name=name;
        wall.transform.SetParent(parent,true);
        wall.transform.localPosition=position;
        wall.transform.localRotation=Quaternion.identity;
        wall.transform.localScale=size;
        wall.GetComponent<Renderer>().sharedMaterial=material;
        Collider duplicate=wall.GetComponent<Collider>();
        if(duplicate!=null)Destroy(duplicate);
    }

    public void Configure(Transform newPlayer,CharacterController newController)
    {
        player=newPlayer;
        controller=newController;
        movement=player!=null?player.GetComponent<DannyTestController>():null;
        if(player!=null)
        {
            lastSafePosition=player.position;
            lastSafeRotation=player.rotation;
        }
    }

    private void LateUpdate()
    {
        if(player==null)return;
        Vector3 position=player.position;
        // Every interior district is explorable. Containment begins only at
        // the four true outside edges of the map (plus a genuine fall through
        // the world), so shortcuts and lost-child areas are never fenced by
        // an invisible gameplay footprint.
        bool outsideWorld=position.y<-35f||position.x<-101f||position.x>101f||position.z<-53f||position.z>437f;
        bool supported=TryFindSupport(position,6f,out _);
        if(supported)unsupportedSince=-1f;
        else if(unsupportedSince<0f)unsupportedSince=Time.time;
        bool fellThroughBorder=!supported&&unsupportedSince>0f&&Time.time-unsupportedSince>0.22f&&
            position.y<lastSafePosition.y-1.15f;
        if((outsideWorld||fellThroughBorder)&&Time.time>=nextRestore)
        {
            Restore();
            return;
        }

        if(Time.time<nextSample)return;
        nextSample=Time.time+0.10f;
        if(controller!=null&&controller.isGrounded&&TryFindSupport(position,0.72f,out _))
        {
            lastSafePosition=position;
            lastSafeRotation=player.rotation;
        }
    }

    private bool TryFindSupport(Vector3 position,float distance,out RaycastHit best)
    {
        best=default;
        float nearest=float.PositiveInfinity;
        int count=Physics.RaycastNonAlloc(position+Vector3.up*0.35f,Vector3.down,supportHits,distance,
            Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
        for(int i=0;i<count;i++)
        {
            Collider candidate=supportHits[i].collider;
            if(candidate==null||candidate.transform.IsChildOf(player))continue;
            if(candidate.GetComponentInParent<RiverValleyHazardMover>()!=null||
                candidate.GetComponentInParent<RiverValleySafeCar>()!=null||
                candidate.GetComponentInParent<RiverValleyAmbientActor>()!=null)continue;
            if(supportHits[i].distance>=nearest)continue;
            nearest=supportHits[i].distance;
            best=supportHits[i];
        }
        return nearest<float.PositiveInfinity;
    }

    private void Restore()
    {
        Restorations++;
        nextRestore=Time.time+1f;
        bool wasEnabled=controller!=null&&controller.enabled;
        if(wasEnabled)controller.enabled=false;
        player.SetPositionAndRotation(lastSafePosition+Vector3.up*0.08f,lastSafeRotation);
        if(wasEnabled)controller.enabled=true;
        movement?.ResetVerticalMotion();
        unsupportedSince=-1f;
    }
}
