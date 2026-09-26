using UnityEngine;

/// <summary>Alternating roof beacons make the river rescue readable through the storm.</summary>
public sealed class RiverValleyEmergencyBeacon : MonoBehaviour
{
    [SerializeField] private Transform blueBeacon;
    [SerializeField] private Transform redBeacon;
    private Vector3 blueScale;
    private Vector3 redScale;
    private bool rescueActive;

    private void Start()
    {
        if(blueBeacon!=null)blueScale=blueBeacon.localScale;
        if(redBeacon!=null)redScale=redBeacon.localScale;
    }

    private void Update()
    {
        if(!rescueActive)
        {
            SetScale(blueBeacon,blueScale);
            SetScale(redBeacon,redScale);
            return;
        }
        bool blueFlash=Mathf.Sin(Time.time*11f)>=0f;
        SetScale(blueBeacon,blueScale*(blueFlash?1.75f:0.82f));
        SetScale(redBeacon,redScale*(blueFlash?0.82f:1.75f));
        if(blueBeacon!=null)blueBeacon.Rotate(Vector3.up,180f*Time.deltaTime,Space.Self);
        if(redBeacon!=null)redBeacon.Rotate(Vector3.up,-180f*Time.deltaTime,Space.Self);
    }

    public void SetRescueActive(bool value)=>rescueActive=value;

    private static void SetScale(Transform item,Vector3 scale)
    {
        if(item!=null)item.localScale=scale;
    }

#if UNITY_EDITOR
    public void Configure(Transform blue,Transform red)
    {
        blueBeacon=blue;
        redBeacon=red;
    }
#endif
}
