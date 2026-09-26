using UnityEngine;

namespace TheLittles
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class SacrificeSwitch : MonoBehaviour
    {
        [SerializeField] private GameObject door;
        private LittleMotor2D holder;

        public void Configure(GameObject targetDoor) => door = targetDoor;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (holder != null) return;
            LittleMotor2D candidate = other.GetComponent<LittleMotor2D>();
            if (candidate == null || !candidate.IsActiveLittle) return;

            holder = candidate;
            holder.HoldAtSwitch(true);
            if (door != null) door.SetActive(false);

            LittleTeam team = FindFirstObjectByType<LittleTeam>();
            if (team != null) team.TouchSwitch();
        }
    }
}
