using UnityEngine;

namespace TheLittles
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayfulDogBoss : MonoBehaviour
    {
        private TextMesh bubble;
        private float nextPounce;
        private Vector3 home;
        private float dogDepth;

        private void Awake() => home = transform.position;

        public void Configure(TextMesh value, float depth)
        {
            bubble = value;
            dogDepth = depth;
        }

        private void Update()
        {
            float pounceAge = nextPounce - Time.time;
            float leap = pounceAge > 1.15f ? Mathf.Sin(Mathf.InverseLerp(1.8f, 1.15f, pounceAge) * Mathf.PI) * 0.85f : 0f;
            transform.position = home + Vector3.up * leap;
            transform.localScale = new Vector3(1f + Mathf.Sin(Time.time * 4f) * 0.035f,
                1f - Mathf.Sin(Time.time * 4f) * 0.025f, 1f);
            if (bubble != null && Time.time > nextPounce + 1.2f) bubble.text = "";
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            LittleMotor2D little = other.GetComponent<LittleMotor2D>();
            if (little == null) return;
            if (Mathf.Abs(little.DepthPosition - dogDepth) > 0.55f) return;
            little.ApplyAttentionPull(transform.position, 7f);
            little.ApplyDepthPull(dogDepth, 0.55f);
            little.Willpower.Drain(3f * Time.deltaTime);
            if (Time.time < nextPounce) return;
            little.Stumble();
            little.Willpower.Drain(2f);
            if (bubble != null) bubble.text = "WOOF!  LICK!  PLAY!";
            CartoonSound.Play(transform.position, CartoonSoundKind.Bark, 0.9f);
            nextPounce = Time.time + 1.8f;
        }
    }
}
