using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class RiverValleyThoughtZone : MonoBehaviour
{
    [SerializeField, TextArea] private string thought;
    private RiverValleyGameDirector director;
    private bool heard;

    private void Start() => director=FindFirstObjectByType<RiverValleyGameDirector>();

    private void OnTriggerEnter(Collider other)
    {
        if(heard||other.GetComponentInParent<DannySpark>()==null)return;
        heard=true;
        director?.Show("DANNY",thought,4.8f);
    }

#if UNITY_EDITOR
    public void Configure(string newThought) => thought=newThought;
#endif
}
