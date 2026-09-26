using System;
using UnityEngine;

namespace TheLittles
{
    public sealed class Willpower : MonoBehaviour
    {
        [SerializeField] private float maximum = 100f;
        [SerializeField] private float current = 100f;
        [SerializeField] private float recoveryPerSecond = 2.5f;
        [SerializeField] private float recoveryDelay = 2.0f;
        private float lastDrainedAt = -100f;

        public float Current => current;
        public float Normalized => maximum <= 0f ? 0f : current / maximum;
        public bool IsInClass { get; private set; }
        public event Action<Willpower> SentToClass;

        private void Update()
        {
            if (!IsInClass && Time.time - lastDrainedAt >= recoveryDelay)
                Restore(recoveryPerSecond * Time.deltaTime);
        }

        public void Drain(float amount)
        {
            if (IsInClass || amount <= 0f) return;
            lastDrainedAt = Time.time;
            current = Mathf.Max(0f, current - amount);
            if (current <= 0f) SendToClass();
        }

        public void Restore(float amount)
        {
            current = Mathf.Min(maximum, current + Mathf.Max(0f, amount));
            if (IsInClass && current >= 12f) IsInClass = false;
        }

        private void SendToClass()
        {
            IsInClass = true;
            SentToClass?.Invoke(this);
        }
    }
}
