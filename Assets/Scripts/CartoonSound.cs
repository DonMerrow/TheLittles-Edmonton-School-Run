using UnityEngine;

namespace TheLittles
{
    public enum CartoonSoundKind { Kid, Bark, Bus }

    public static class CartoonSound
    {
        private static AudioClip kid;
        private static AudioClip bark;
        private static AudioClip bus;

        public static void Play(Vector3 position, CartoonSoundKind kind, float pitch = 1f)
        {
            AudioClip clip = kind == CartoonSoundKind.Bark ? bark ??= Make("Friendly bark", 155f, 0.28f, true) :
                kind == CartoonSoundKind.Bus ? bus ??= Make("Bus bell", 330f, 0.65f, false) :
                kid ??= Make("Kid chatter", 520f, 0.22f, true);
            GameObject sound = new("Cartoon sound");
            sound.transform.position = position;
            AudioSource source = sound.AddComponent<AudioSource>();
            source.clip = clip;
            source.pitch = pitch;
            source.volume = 0.18f;
            source.spatialBlend = 0f;
            source.Play();
            Object.Destroy(sound, clip.length / Mathf.Max(0.1f, pitch) + 0.1f);
        }

        private static AudioClip Make(string name, float frequency, float seconds, bool pulse)
        {
            const int rate = 22050;
            int samples = Mathf.CeilToInt(rate * seconds);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float envelope = Mathf.Sin(Mathf.PI * i / samples);
                float gate = pulse && Mathf.Sin(t * 42f) < -0.15f ? 0.18f : 1f;
                data[i] = Mathf.Sin(t * frequency * Mathf.PI * 2f) * envelope * gate * 0.28f;
            }
            AudioClip clip = AudioClip.Create(name, samples, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
