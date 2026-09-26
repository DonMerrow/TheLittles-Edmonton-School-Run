using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Level 3 search light: readable, damageable, and useful around wildlife.</summary>
[DisallowMultipleComponent]
public sealed class DannyStormFlashlight : MonoBehaviour
{
    private RiverValleyGameDirector director;
    private Light beam;
    private GameObject prop;
    private bool switchedOn=true;
    private int integrity=3;
    private float nextWildlifeScan;
    private float brightPulseUntil;
    private float flickerUntil;
    private float nextMessage;
    private GUIStyle style;

    public bool IsBroken => integrity<=0;
    public bool IsOn => switchedOn&&!IsBroken;
    public int Integrity => integrity;

    public void Configure(RiverValleyGameDirector owner)
    {
        director=owner;
        BuildProp();
    }

    private void Start()
    {
        if(director==null)director=FindFirstObjectByType<RiverValleyGameDirector>();
        BuildProp();
    }

    private void BuildProp()
    {
        if(prop!=null)return;
        prop=new GameObject("School-issued storm flashlight");
        prop.transform.SetParent(transform,false);
        prop.transform.localPosition=new Vector3(0.34f,0.90f,0.30f);

        Shader shader=Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard");
        Material body=new(shader){color=new Color(0.96f,0.55f,0.06f),hideFlags=HideFlags.DontSave};
        Material lens=new(shader){color=new Color(1f,0.92f,0.58f),hideFlags=HideFlags.DontSave};
        if(body.HasProperty("_BaseColor"))body.SetColor("_BaseColor",body.color);
        if(lens.HasProperty("_BaseColor"))lens.SetColor("_BaseColor",lens.color);

        GameObject casing=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        casing.name="Orange flashlight body";
        casing.transform.SetParent(prop.transform,false);
        casing.transform.localRotation=Quaternion.Euler(90f,0f,0f);
        casing.transform.localScale=new Vector3(0.075f,0.16f,0.075f);
        casing.GetComponent<Renderer>().sharedMaterial=body;
        Destroy(casing.GetComponent<Collider>());

        GameObject glass=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glass.name="Warm flashlight lens";
        glass.transform.SetParent(prop.transform,false);
        glass.transform.localPosition=new Vector3(0f,0f,0.17f);
        glass.transform.localRotation=Quaternion.Euler(90f,0f,0f);
        glass.transform.localScale=new Vector3(0.10f,0.025f,0.10f);
        glass.GetComponent<Renderer>().sharedMaterial=lens;
        Destroy(glass.GetComponent<Collider>());

        GameObject lightObject=new("Flashlight beam");
        lightObject.transform.SetParent(prop.transform,false);
        lightObject.transform.localPosition=new Vector3(0f,0f,0.20f);
        beam=lightObject.AddComponent<Light>();
        beam.type=LightType.Spot;
        beam.color=new Color(1f,0.88f,0.60f);
        beam.range=20f;
        beam.spotAngle=52f;
        beam.innerSpotAngle=28f;
        beam.intensity=11f;
        beam.shadows=LightShadows.Soft;
    }

    private void Update()
    {
        if(((Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame)||
            RiverValleyMobileControls.FlashlightPressed)&&!IsBroken)
        {
            switchedOn=!switchedOn;
            director?.Show("DANNY",switchedOn
                ? "Flashlight on. Wolves, deer and rabbits can see me coming."
                : "Flashlight off. Save it for the darkest search paths.",2.8f);
        }

        if(beam!=null)
        {
            bool flicker=Time.time<flickerUntil&&Mathf.Sin(Time.time*34f)>0.2f;
            beam.enabled=IsOn&&!flicker;
            beam.intensity=Time.time<brightPulseUntil?18f:Mathf.Lerp(6.5f,11f,integrity/3f);
        }
        if(!IsOn||Time.time<nextWildlifeScan)return;
        nextWildlifeScan=Time.time+0.16f;
        ScanWildlife();
    }

    private void ScanWildlife()
    {
        Vector3 origin=transform.position+Vector3.up*0.85f;
        Vector3 forward=Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;
        foreach(RiverValleyAnimalMotion animal in FindObjectsByType<RiverValleyAnimalMotion>(FindObjectsSortMode.None))
        {
            if(animal==null||!animal.gameObject.activeInHierarchy||animal.Kind==RiverAnimalKind.Dog)continue;
            Vector3 toward=animal.transform.position-origin;
            float distance=toward.magnitude;
            if(distance>8f||distance<0.01f)continue;
            float cone=Vector3.Dot(forward,Vector3.ProjectOnPlane(toward,Vector3.up).normalized);
            // A coyote that gets very close is caught by the edge of the beam;
            // other wildlife must be visibly in front of Danny.
            if(cone<0.28f&&!(animal.Kind==RiverAnimalKind.Coyote&&distance<3.4f))continue;
            animal.FleeFromLight(origin);
            brightPulseUntil=Time.time+0.24f;
            if(Time.time>=nextMessage)
            {
                nextMessage=Time.time+8f;
                string name=animal.Kind==RiverAnimalKind.Coyote?"wolf":
                    animal.Kind==RiverAnimalKind.Deer?"deer":"rabbit";
                director?.Show("DANNY",$"The school flashlight sends the {name} back into the snow. Keep searching.",2.8f);
            }
        }
    }

    public void DamageByVehicle(string vehicle)
    {
        if(IsBroken)return;
        integrity--;
        flickerUntil=Time.time+1.7f;
        if(integrity<=0)
        {
            switchedOn=false;
            director?.Show("DANNY",$"The {vehicle} hit cracked the flashlight. It is dark now—stay off the road and finish the search.",4.8f);
        }
        else director?.Show("DANNY",$"The {vehicle} knocked the flashlight. It flickers—{integrity} good hit{(integrity==1?"":"s")} left.",3.6f);
    }

    private void OnGUI()
    {
        if(director==null||!director.LevelThreeActive)return;
        style??=new GUIStyle(GUI.skin.box)
        {
            fontSize=16,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,
            normal={textColor=new Color(1f,0.83f,0.30f)}
        };
        string charge=IsBroken?"BROKEN":new string('■',integrity)+new string('□',3-integrity);
        GUI.Box(new Rect(Screen.width-284f,20f,260f,42f),$"F — FLASHLIGHT   {charge}",style);
    }
}
