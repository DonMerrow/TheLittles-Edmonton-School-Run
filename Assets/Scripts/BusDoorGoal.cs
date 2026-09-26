using System.Collections.Generic;
using UnityEngine;

namespace TheLittles
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class BusDoorGoal : MonoBehaviour
    {
        private readonly HashSet<LittleMotor2D> present = new();
        private LittleTeam team;
        private TextMesh status;
        private float stackProgress;
        private bool finished;

        public void Configure(LittleTeam value, TextMesh label)
        {
            team = value;
            status = label;
        }

        private void Update()
        {
            if (team == null || finished) return;
            present.RemoveWhere(l => l == null || l.Willpower.IsInClass || l.IsHeldAtSwitch);
            bool together = present.Count >= team.AvailableCount && team.AvailableCount >= 3;
            if (together)
            {
                team.SetDoorStack(true, new Vector2(transform.position.x, -2.02f));
                stackProgress += Time.deltaTime;
                if (status != null) status.text = "STACK!  " + Mathf.RoundToInt(Mathf.Clamp01(stackProgress / 1.8f) * 100f) + "%";
                if (stackProgress >= 1.8f)
                {
                    finished = true;
                    if (status != null) status.text = "ALL ABOARD!";
                    CartoonSound.Play(transform.position, CartoonSoundKind.Bus, 1f);
                    team.Win();
                }
            }
            else
            {
                team.SetDoorStack(false, Vector2.zero);
                stackProgress = Mathf.MoveTowards(stackProgress, 0f, Time.deltaTime * 1.6f);
                if (status != null) status.text = "ALL THREE REACH THE DOOR";
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            LittleMotor2D little = other.GetComponent<LittleMotor2D>();
            if (little != null) present.Add(little);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            LittleMotor2D little = other.GetComponent<LittleMotor2D>();
            if (little != null) present.Remove(little);
        }
    }
}
