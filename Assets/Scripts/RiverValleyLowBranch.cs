using UnityEngine;

/// <summary>A safe walking-height branch that catches only airborne tricks.</summary>
public sealed class RiverValleyLowBranch : MonoBehaviour
{
    private float nextWipeout;

    private void OnTriggerEnter(Collider other)
    {
        if(Time.time<nextWipeout)return;
        DannyTestController danny=other.GetComponentInParent<DannyTestController>();
        if(danny==null||danny.IsGrounded)return;
        nextWipeout=Time.time+2.5f;
        danny.HitLowBranch();
    }
}
