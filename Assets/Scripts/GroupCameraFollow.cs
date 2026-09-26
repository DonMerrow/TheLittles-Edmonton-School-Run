using UnityEngine;

namespace TheLittles
{
    public sealed class GroupCameraFollow : MonoBehaviour
    {
        private LittleTeam team;
        private float minimumX;
        private float maximumX;
        private float fixedY;

        public void Configure(LittleTeam value, float minX, float maxX)
        {
            team = value;
            minimumX = minX;
            maximumX = maxX;
            fixedY = transform.position.y;
        }

        private void LateUpdate()
        {
            if (team == null || team.Active == null) return;
            float targetX = Mathf.Clamp(team.Active.Position.x + 2.0f, minimumX, maximumX);
            Vector3 target = new(targetX, fixedY, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-4.5f * Time.deltaTime));
        }
    }
}
