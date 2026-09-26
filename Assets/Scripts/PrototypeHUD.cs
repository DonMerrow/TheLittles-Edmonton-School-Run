using UnityEngine;

namespace TheLittles
{
    public sealed class PrototypeHUD : MonoBehaviour
    {
        [SerializeField] private LittleTeam team;
        private GUIStyle titleStyle;
        private GUIStyle bodyStyle;
        private GUIStyle buttonStyle;

        public void Configure(LittleTeam value) => team = value;

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            titleStyle.normal.textColor = new Color(1f, 0.31f, 0.57f);
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 17 };
            bodyStyle.normal.textColor = Color.white;
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, fontStyle = FontStyle.Bold };
        }

        private void OnGUI()
        {
            EnsureStyles();
            if (team == null) return;

            GUI.Label(new Rect(22, 16, 600, 40), "THE LITTLES: THE BUS STOP", titleStyle);
            float y = 58f;
            for (int i = 0; i < team.Members.Count; i++)
            {
                LittleMotor2D little = team.Members[i];
                string state = little.Willpower.IsInClass ? "OVERWHELMED" :
                    little.IsHeldAtSwitch ? "HOLDING DOOR" :
                    little.IsStumbled ? "NEEDS A HAND" :
                    little == team.Active ? "LEADING" : "FOLLOWING";
                GUI.Label(new Rect(24, y, 390, 26),
                    $"{little.Voice.LittleName}  {Mathf.RoundToInt(little.Willpower.Current)} will  /  {state}", bodyStyle);
                y += 24f;
            }

            LittleVoice activeVoice = null;
            foreach (LittleMotor2D little in team.Members)
                if (Time.time < little.Voice.LineExpiresAt &&
                    (activeVoice == null || little.Voice.LineExpiresAt > activeVoice.LineExpiresAt))
                    activeVoice = little.Voice;
            if (activeVoice != null)
                GUI.Label(new Rect(24, 138, 760, 34),
                    $"{activeVoice.LittleName}: “{activeVoice.CurrentLine}”", bodyStyle);

            if (team.HasWon)
                GUI.Label(new Rect(22, 180, 760, 80), "ALL ABOARD!\\nNEXT STOP: SCHOOL.", titleStyle);
            else if (team.HasLost)
                GUI.Label(new Rect(22, 180, 760, 80), "EVERYBODY NEEDS A BREATHER.\\nTRY THE MORNING AGAIN.", titleStyle);
            else
                GUI.Label(new Rect(22, 180, 900, 35), "Keep the crew together. A/D move, W/S steer depth. Return for anyone calling.", bodyStyle);

            float h = Screen.height;
            float w = Screen.width;
            bool left = GUI.RepeatButton(new Rect(18, h - 58, 48, 42), "←", buttonStyle);
            bool right = GUI.RepeatButton(new Rect(126, h - 58, 48, 42), "→", buttonStyle);
            team.SetTouchMove(left ? -1f : right ? 1f : 0f);
            bool away = GUI.RepeatButton(new Rect(72, h - 106, 48, 42), "↑", buttonStyle);
            bool near = GUI.RepeatButton(new Rect(72, h - 58, 48, 42), "↓", buttonStyle);
            if (left || right) near = false;
            team.SetTouchDepth(away ? 1f : near ? -1f : 0f);
            bool crawl = GUI.RepeatButton(new Rect(w - 174, h - 58, 50, 42), "CRAWL", buttonStyle);
            team.SetTouchCrawl(crawl);
            if (GUI.Button(new Rect(w - 118, h - 58, 48, 42), "JUMP", buttonStyle)) team.TouchJump();
            if (GUI.Button(new Rect(w - 64, h - 58, 48, 42), "NEXT", buttonStyle)) team.TouchSwitch();
        }
    }
}
