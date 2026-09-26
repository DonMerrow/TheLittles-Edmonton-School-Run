using UnityEngine;

namespace TheLittles
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class LibraryGoal : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<LittleMotor2D>() == null) return;
            LittleTeam team = FindFirstObjectByType<LittleTeam>();
            if (team != null) team.Win();
        }
    }
}
