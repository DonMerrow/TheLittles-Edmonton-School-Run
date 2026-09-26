using UnityEngine;

namespace TheLittles
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class AttentionDrainZone : MonoBehaviour
    {
        [SerializeField] private float drainPerSecond = 32f;
        [SerializeField] private float attentionPull = 13f;

        private void OnTriggerStay2D(Collider2D other)
        {
            Willpower willpower = other.GetComponent<Willpower>();
            if (willpower != null) willpower.Drain(drainPerSecond * Time.deltaTime);
            LittleMotor2D motor = other.GetComponent<LittleMotor2D>();
            if (motor != null) motor.ApplyAttentionPull(transform.position, attentionPull);
        }
    }
}
