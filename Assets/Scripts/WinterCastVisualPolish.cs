using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Gives repeated winter-cast prefabs individual coat/hat colour variation
/// and consistent shadow quality without duplicating their mesh assets.
/// </summary>
[DisallowMultipleComponent]
public sealed class WinterCastVisualPolish : MonoBehaviour
{
    private void Start()
    {
        int seed=gameObject.name.GetHashCode();
        float hueShift=((seed&255)/255f-0.5f)*0.11f;
        float valueShift=(((seed>>8)&255)/255f-0.5f)*0.10f;
        // The cast models contain rigid Blender accessories (snowballs,
        // sticks, bags, ropes and leashes). They are not skinned to the
        // humanoid animation and are therefore left hanging at their import
        // coordinates when a character walks. Gameplay creates the useful
        // versions separately, so hide every imported accessory.
        foreach(Transform candidate in GetComponentsInChildren<Transform>(true))
        {
            string normalized=candidate.name.ToLowerInvariant().Replace(" ",string.Empty).Replace("_",string.Empty);
            if(normalized.Contains("accessory")||normalized.Contains("hockeystick")||
                normalized.Contains("hockeyblade")||normalized.Contains("snowball")||
                normalized.Contains("shovel")||normalized.Contains("sledrope")||
                normalized.Contains("leash")||normalized.Contains("placard")||
                normalized.Contains("stopsign")||normalized.Contains("stoppole")||
                normalized.Contains("signboard")||normalized.Contains("safetyvest"))
                candidate.gameObject.SetActive(false);
        }
        foreach(Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode=ShadowCastingMode.On;
            renderer.receiveShadows=true;
            Material material=renderer.sharedMaterial;
            if(material==null)continue;
            string materialName=material.name.ToLowerInvariant();
            if(!materialName.Contains("coat")&&!materialName.Contains("accent")&&!materialName.Contains("hat"))continue;
            Color source=material.HasProperty("_BaseColor")?material.GetColor("_BaseColor"):material.color;
            Color.RGBToHSV(source,out float h,out float s,out float v);
            Color varied=Color.HSVToRGB(Mathf.Repeat(h+hueShift,1f),Mathf.Clamp01(s*1.05f),Mathf.Clamp01(v+valueShift));
            MaterialPropertyBlock block=new();
            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor",varied);
            block.SetColor("_Color",varied);
            block.SetFloat("_Smoothness",0.24f);
            renderer.SetPropertyBlock(block);
        }
    }
}
