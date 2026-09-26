using UnityEngine;

/// <summary>Lightweight prototype motion: Mom follows; others turn toward Danny.</summary>
public sealed class WinterCastNpcMotor : MonoBehaviour
{
    [SerializeField] private bool chaseDanny;
    [SerializeField, Min(0f)] private float moveSpeed = 2.6f;
    [SerializeField, Min(0.2f)] private float stopDistance = 1.35f;
    [SerializeField, Min(0f)] private float attentionDistance = 4.5f;
    private DannySpark danny;
    private Animator animator;

    private void Start()
    {
        danny = FindFirstObjectByType<DannySpark>();
        animator = GetComponent<Animator>();
        if (animator != null && animator.GetComponent<WinterAnimationEventRelay>() == null)
            animator.gameObject.AddComponent<WinterAnimationEventRelay>();
    }

    private void Update()
    {
        if (danny == null) return;
        Vector3 offset = danny.transform.position - transform.position;
        offset.y = 0f;
        float distance = offset.magnitude;
        float speed = 0f;
        if (distance < attentionDistance && distance > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(offset.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * 4f);
        }
        if (chaseDanny && distance > stopDistance)
        {
            speed = moveSpeed;
            transform.position += offset.normalized * (moveSpeed * Time.deltaTime);
        }
        if (animator != null)
        {
            float gait=speed>0.05f?Mathf.Lerp(2.18f,4.65f,Mathf.InverseLerp(0.5f,3.2f,speed)):0f;
            animator.SetFloat("Speed",gait,0.09f,Time.deltaTime);
            animator.speed=speed>0.05f?1.10f:1f;
        }
    }

    // Starter Assets clips contain these events. Supporting characters do not
    // need sound yet, but receiving them keeps the Console clean.
    public void OnFootstep(AnimationEvent animationEvent) { }
    public void OnLand(AnimationEvent animationEvent) { }

#if UNITY_EDITOR
    public void Configure(bool shouldChase, float speed, float stoppingDistance)
    {
        chaseDanny = shouldChase;
        moveSpeed = speed;
        stopDistance = stoppingDistance;
    }
#endif
}
