using UnityEngine;

namespace TheLittles
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class FriendlyKidObstacle : MonoBehaviour
    {
        private string line = "Do you need help, tiny?";
        private TextMesh bubble;
        private float bubbleUntil;
        private float soundAfter;
        private Vector3 home;
        private float attractionDepth;

        private void Awake() => home = transform.position;

        public void Configure(string value, TextMesh text, float depth)
        {
            line = value;
            bubble = text;
            attractionDepth = depth;
            if (bubble != null) bubble.text = "";
        }

        private void Update()
        {
            transform.position = home + Vector3.right * Mathf.Sin(Time.time * 0.42f + home.x) * 0.48f;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.7f + transform.position.x) * 2.3f);
            if (bubble != null && Time.time >= bubbleUntil) bubble.text = "";
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            LittleMotor2D little = other.GetComponent<LittleMotor2D>();
            if (little == null) return;
            if (Mathf.Abs(little.DepthPosition - attractionDepth) > 0.48f) return;
            little.ApplyAttentionPull(transform.position, 3.2f);
            little.ApplyDepthPull(attractionDepth, 0.32f);
            little.Willpower.Drain(1.0f * Time.deltaTime);
            if (bubble != null)
            {
                bubble.text = line;
                bubbleUntil = Time.time + 1.3f;
            }
            if (Time.time >= soundAfter)
            {
                CartoonSound.Play(transform.position, CartoonSoundKind.Kid, 0.86f + Mathf.Repeat(transform.position.x, 3f) * 0.08f);
                soundAfter = Time.time + 2.2f;
            }
        }
    }
}
