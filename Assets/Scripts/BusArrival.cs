using UnityEngine;

namespace TheLittles
{
    public sealed class BusArrival : MonoBehaviour
    {
        private LittleTeam team;
        private GameObject doorZone;
        private Vector3 parkedPosition;
        private bool arriving;
        private bool announced;
        private TextMesh driverLabel;
        private float nextDriverLine;

        public void Configure(LittleTeam value, GameObject goal, Vector3 parked, TextMesh label)
        {
            team = value;
            doorZone = goal;
            parkedPosition = parked;
            driverLabel = label;
            if (driverLabel != null) driverLabel.text = "";
            transform.position = parked + Vector3.right * 8f;
            if (doorZone != null) doorZone.SetActive(false);
        }

        private void Update()
        {
            if (team == null || team.Active == null) return;
            if (team.Active.Position.x > 13f) arriving = true;
            if (!arriving) return;
            transform.position = Vector3.MoveTowards(transform.position, parkedPosition, Time.deltaTime * 5.2f);
            if (!announced)
            {
                announced = true;
                CartoonSound.Play(transform.position, CartoonSoundKind.Bus, 0.82f);
                team.Active.Voice.Say(LittleLineKind.Hurry);
                if (driverLabel != null) driverLabel.text = "DRIVER: ALL ABOARD!";
                nextDriverLine = Time.time + 4f;
            }
            if (announced && Time.time >= nextDriverLine && team.Active.Position.x < 25f)
            {
                if (driverLabel != null) driverLabel.text = "DRIVER: STOP PLAYING WITH THAT DOG! WE'RE LATE!";
                nextDriverLine = Time.time + 6f;
            }
            if (Vector3.Distance(transform.position, parkedPosition) < 0.05f && doorZone != null)
                doorZone.SetActive(true);
        }
    }
}
