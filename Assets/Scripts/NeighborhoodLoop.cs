using UnityEngine;

namespace TheLittles
{
    public sealed class NeighborhoodLoop : MonoBehaviour
    {
        private SpriteRenderer[] renderers;
        private Vector3 start;

        private void Awake()
        {
            start = transform.position;
            renderers = GetComponentsInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            float loop = Mathf.Repeat(Time.time * 0.22f, 9f);
            transform.position = start + Vector3.left * loop;
            int colourStep = Mathf.FloorToInt(Time.time / 8f) % 3;
            Color tint = colourStep == 0 ? new Color(0.92f, 0.72f, 0.62f) :
                colourStep == 1 ? new Color(0.62f, 0.80f, 0.88f) : new Color(0.78f, 0.68f, 0.88f);
            foreach (SpriteRenderer item in renderers)
                if (item.name.Contains("House face")) item.color = tint;
        }
    }
}
