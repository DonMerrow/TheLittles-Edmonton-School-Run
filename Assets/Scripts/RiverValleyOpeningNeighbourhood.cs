using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes Danny's opening block read as an Edmonton neighbourhood rather than
/// an empty route: two house fronts, a distant roof line and local residents.
/// The additions are scenery only and never join the rescue count.
/// </summary>
[DisallowMultipleComponent]
public sealed class RiverValleyOpeningNeighbourhood : MonoBehaviour
{
    private Transform additions;

    private void Start()
    {
        if(GameObject.Find("Opening neighbourhood — lived-in block")!=null)return;
        GameObject root=new("Opening neighbourhood — lived-in block");
        root.transform.SetParent(transform,false);
        additions=root.transform;
        MoveCornerHomesOffTheRoad();
        BuildHouseRows();
        BuildDannyHome();
        BuildResidents();
    }

    private void BuildHouseRows()
    {
        GameObject[] houseSources=
        {
            GameObject.Find("West approach neighbour home 1"),
            GameObject.Find("West approach neighbour home 2"),
            GameObject.Find("West approach neighbour home 3"),
            GameObject.Find("West approach neighbour home 4")
        };
        List<GameObject> houses=new();
        foreach(GameObject source in houseSources)if(source!=null)houses.Add(source);
        if(houses.Count==0)
        {
            GameObject fallback=GameObject.Find("Danny's modest Jasper-area home");
            if(fallback!=null)houses.Add(fallback);
        }
        if(houses.Count==0)return;

        float[] streetX={-72f,-55f,-38f,-21f};
        for(int i=0;i<streetX.Length;i++)
        {
            CloneHouse(houses[i%houses.Count],$"Opening south-side home {i+1}",
                new Vector3(streetX[i],0f,-20f),0f,true,0.94f);
            CloneHouse(houses[(i+1)%houses.Count],$"Opening north-side home {i+1}",
                new Vector3(streetX[i],0f,28f),180f,true,0.94f);
        }

        // Real homes at the west end, in front of the distant flat skyline.
        // Leave the carriageway and the exploration path clear.
        CloneHouse(houses[1%houses.Count],"West-end corner home south",
            new Vector3(-88f,0f,-20f),0f,true,0.88f);
        CloneHouse(houses[2%houses.Count],"West-end corner home north",
            new Vector3(-88f,0f,28f),180f,true,0.88f);
        CloneHouse(houses[3%houses.Count],"West-end corner home beyond",
            new Vector3(-88f,0f,51f),180f,true,0.82f);

        // A second, non-colliding row remains visible beyond the nearby roofs
        // and closes the old empty horizon without fencing off exploration.
        for(int i=0;i<6;i++)
        {
            float x=-72f+i*17f;
            CloneHouse(houses[(i+2)%houses.Count],$"Distant neighbourhood roof south {i+1}",
                new Vector3(x,0f,-34f),0f,false,0.88f);
            CloneHouse(houses[(i+3)%houses.Count],$"Distant neighbourhood roof north {i+1}",
                new Vector3(x,0f,51f),180f,false,0.88f);
        }

        // Fill the long northbound block with real homes instead of leaving
        // a huge white lot in front of the flat skyline. These sit well back
        // from both sidewalks, so Danny and the following children retain a
        // broad, readable route to school.
        float[] mainStreetZ={18f,45f,72f,99f,126f};
        for(int i=0;i<mainStreetZ.Length;i++)
        {
            CloneHouse(houses[(i+1)%houses.Count],$"Main street west infill home {i+1}",
                new Vector3(-27f,0f,mainStreetZ[i]),90f,true,0.86f);
            // Stop the east row before the two corner cross streets. A house
            // at x=34 looked fine from the main path but physically occupied
            // the side road around the corner.
            if(mainStreetZ[i]<90f)
                CloneHouse(houses[(i+2)%houses.Count],$"Main street east infill home {i+1}",
                    new Vector3(30f,0f,mainStreetZ[i]+8f),270f,true,0.82f);
        }
    }

    private GameObject CloneHouse(GameObject source,string cloneName,Vector3 position,float yaw,
        bool solid,float scaleMultiplier)
    {
        if(source==null)return null;
        GameObject clone=Instantiate(source,additions);
        clone.name=cloneName;
        clone.transform.SetPositionAndRotation(new Vector3(position.x,source.transform.position.y,position.z),
            Quaternion.Euler(0f,yaw,0f));
        clone.transform.localScale=source.transform.localScale*scaleMultiplier;
        foreach(MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
            if(behaviour!=null)behaviour.enabled=false;
        foreach(Collider collider in clone.GetComponentsInChildren<Collider>(true))
            Destroy(collider);
        if(!solid)return clone;

        Renderer[] renderers=clone.GetComponentsInChildren<Renderer>(true);
        if(renderers.Length==0)return clone;
        Bounds bounds=renderers[0].bounds;
        for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
        BoxCollider wall=clone.AddComponent<BoxCollider>();
        wall.center=clone.transform.InverseTransformPoint(bounds.center);
        Vector3 localSize=clone.transform.InverseTransformVector(bounds.size);
        wall.size=new Vector3(Mathf.Abs(localSize.x)*0.88f,Mathf.Abs(localSize.y),Mathf.Abs(localSize.z)*0.88f);
        return clone;
    }

    private void BuildDannyHome()
    {
        GameObject source=GameObject.Find("Danny's modest Jasper-area home");
        if(source==null)return;
        // Use the authored home instead of cloning a second garage onto the
        // pedestrian route. Pull it close enough to read, but keep its whole
        // footprint behind the front walk.
        source.transform.position=new Vector3(-78.5f,source.transform.position.y,-24f);
        source.name="Danny and Mom's home";
        GameObject house=source;
        if(house==null)return;
        Renderer[] pieces=house.GetComponentsInChildren<Renderer>(true);
        if(pieces.Length==0)return;
        Bounds bounds=pieces[0].bounds;
        for(int i=1;i<pieces.Length;i++)bounds.Encapsulate(pieces[i].bounds);
        // The entrance faces the walking route. The short porch also makes
        // Mom's later emergence readable from Danny's starting camera.
        float front=Mathf.Min(-9.4f,bounds.max.z+0.04f);
        Vector3 door=new(bounds.center.x,1.12f,front);
        Material paint=new(Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard"))
        { color=new Color(0.24f,0.10f,0.075f) };
        GameObject entrance=GameObject.CreatePrimitive(PrimitiveType.Cube);
        entrance.name="Danny and Mom's front door";
        entrance.transform.SetParent(additions,true);
        entrance.transform.position=door;
        entrance.transform.localScale=new Vector3(1.35f,2.22f,0.14f);
        entrance.GetComponent<Renderer>().sharedMaterial=paint;
        Destroy(entrance.GetComponent<Collider>());
        GameObject porch=GameObject.CreatePrimitive(PrimitiveType.Cube);
        porch.name="Danny and Mom's front step";
        porch.transform.SetParent(additions,true);
        porch.transform.position=new Vector3(door.x,0.045f,front+0.75f);
        porch.transform.localScale=new Vector3(2.2f,0.09f,1.5f);
        porch.GetComponent<Renderer>().sharedMaterial=paint;
        RiverValleyMomChase mom=FindFirstObjectByType<RiverValleyMomChase>();
        if(mom!=null)mom.ConfigureOpeningHome(new Vector3(door.x,0.08f,front+0.20f),
            new Vector3(door.x,0.08f,-6.8f));
    }

    private static void MoveCornerHomesOffTheRoad()
    {
        foreach(string houseName in new[]{"Corner neighbourhood home 3","Corner neighbourhood home 4"})
        {
            GameObject house=GameObject.Find(houseName);
            if(house==null)continue;
            Vector3 position=house.transform.position;
            position.x=Mathf.Max(position.x,80f);
            house.transform.position=position;
        }
    }

    private void BuildResidents()
    {
        GameObject child=FindCast(true);
        GameObject adult=FindCast(false);
        if(child!=null)
        {
            SpawnResident(child,"Kids waiting near Danny's house 1",new Vector3(-65f,0.08f,-8.2f),new Vector3(-63.4f,0.08f,-8.2f),true,0);
            SpawnResident(child,"Kids waiting near Danny's house 2",new Vector3(-61.8f,0.08f,-8.2f),new Vector3(-60.2f,0.08f,-8.2f),true,1);
            SpawnResident(child,"School kid comparing hockey cards",new Vector3(-48f,0.08f,9.2f),new Vector3(-38f,0.08f,9.2f),true,2);
            SpawnResident(child,"School kid planning the snow fort",new Vector3(-35f,0.08f,-5f),new Vector3(-24f,0.08f,-5f),true,3);
            SpawnResident(child,"School kid with a fast sled",new Vector3(-20f,0.08f,9.2f),new Vector3(-9f,0.08f,9.2f),true,4);
            SpawnResident(child,"Kid waiting at the school corner",new Vector3(-7f,0.08f,-8.2f),new Vector3(-7f,0.08f,-8.2f),true,5);
        }
        if(adult!=null)
        {
            SpawnResident(adult,"Neighbour clearing a front walk",new Vector3(-69f,0.08f,9.2f),new Vector3(-58f,0.08f,9.2f),false,0);
            SpawnResident(adult,"Neighbour walking to the bus",new Vector3(-56f,0.08f,-5f),new Vector3(-43f,0.08f,-5f),false,1);
            SpawnResident(adult,"Neighbour carrying groceries",new Vector3(-38f,0.08f,9.2f),new Vector3(-27f,0.08f,9.2f),false,2);
            SpawnResident(adult,"Neighbour checking the icy sidewalk",new Vector3(-24f,0.08f,-8.2f),new Vector3(-24f,0.08f,-8.2f),false,3);
            SpawnResident(adult,"Neighbour heading toward the corner",new Vector3(-17f,0.08f,9.2f),new Vector3(-5f,0.08f,9.2f),false,4);
            SpawnResident(adult,"Neighbour watching the storm",new Vector3(-9f,0.08f,-8.2f),new Vector3(-9f,0.08f,-8.2f),false,5);
        }
        if(child!=null)
        {
            SpawnResident(child,"Main street kid heading to the rink",new Vector3(-9f,0.08f,38f),new Vector3(-9f,0.08f,52f),true,0);
            SpawnResident(child,"Main street kids building a fort",new Vector3(-9f,0.08f,71f),new Vector3(-9f,0.08f,79f),true,3);
            SpawnResident(child,"Main street kid walking home",new Vector3(-9f,0.08f,104f),new Vector3(-9f,0.08f,118f),true,4);
        }
        if(adult!=null)
        {
            SpawnResident(adult,"Main street neighbour shovelling",new Vector3(-17f,0.08f,25f),new Vector3(-17f,0.08f,32f),false,0);
            SpawnResident(adult,"Main street neighbour walking a block",new Vector3(21f,0.08f,86f),new Vector3(21f,0.08f,101f),false,4);
        }
    }

    private GameObject FindCast(bool child)
    {
        WinterCastIdentity[] cast=FindObjectsByType<WinterCastIdentity>(FindObjectsSortMode.None);
        foreach(WinterCastIdentity identity in cast)
        {
            if(identity==null||identity.GetComponentInParent<RiverValleyMomChase>()!=null)continue;
            string role=(identity.StoryRole+" "+identity.name).ToLowerInvariant();
            bool childRole=role.Contains("child")||role.Contains("kid")||role.Contains("classmate")||
                role.Contains("snow_fort")||role.Contains("sledding");
            if(childRole==child)return identity.gameObject;
        }
        return null;
    }

    private void SpawnResident(GameObject source,string name,Vector3 pointA,Vector3 pointB,
        bool child,int lineIndex)
    {
        GameObject clone=Instantiate(source,additions);
        clone.name=name;
        clone.transform.position=pointA;
        foreach(MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
            if(behaviour!=null)behaviour.enabled=false;
        foreach(Collider collider in clone.GetComponentsInChildren<Collider>(true))collider.enabled=false;
        foreach(Animator animator in clone.GetComponentsInChildren<Animator>(true))
            if(animator!=null)animator.applyRootMotion=false;
        RiverValleyKidFollower follower=clone.GetComponent<RiverValleyKidFollower>();
        if(follower!=null)Destroy(follower);
        RiverValleyNeighbourResident resident=clone.AddComponent<RiverValleyNeighbourResident>();
        resident.Configure(pointA,pointB,child,lineIndex);
    }
}
