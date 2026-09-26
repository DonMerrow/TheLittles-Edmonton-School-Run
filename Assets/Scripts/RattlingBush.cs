using UnityEngine;

namespace TheLittles
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class RattlingBush : MonoBehaviour
    {
        private float depth;
        private Vector3 home;
        private float rattleUntil;

        public void Configure(float value) => depth = value;

        private void Awake() => home = transform.position;

        private void Update()
        {
            float shake = Time.time < rattleUntil ? Mathf.Sin(Time.time * 38f) * 0.08f : 0f;
            transform.position = home + Vector3.right * shake;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            LittleMotor2D little = other.GetComponent<LittleMotor2D>();
            if (little == null || Mathf.Abs(little.DepthPosition - depth) > 0.45f) return;
            rattleUntil = Time.time + 0.25f;
            little.ApplyDepthPull(-depth, 0.12f);
        }
    }
}
