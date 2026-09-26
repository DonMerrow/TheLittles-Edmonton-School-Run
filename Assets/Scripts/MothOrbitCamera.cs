using UnityEngine;

namespace TheLittles
{
    public sealed class MothOrbitCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 5.2f;
        [SerializeField] private float height = 1.15f;
        [SerializeField] private float yaw = 25f;
        [SerializeField] private float pitch = 18f;
        [SerializeField] private float followSharpness = 12f;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * 3.2f;
                pitch -= Input.GetAxis("Mouse Y") * 2.5f;
            }
            pitch = Mathf.Clamp(pitch, 6f, 55f);
            distance = Mathf.Clamp(distance - Input.mouseScrollDelta.y * 0.45f, 2.6f, 8.5f);

            Vector3 focus = target.position + Vector3.up * height;
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 desired = focus + orbit * new Vector3(0f, 0f, -distance);
            transform.position = Vector3.Lerp(transform.position, desired,
                1f - Mathf.Exp(-followSharpness * Time.deltaTime));
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
    }
}
