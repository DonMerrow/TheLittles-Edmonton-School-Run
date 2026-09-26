using UnityEngine;

namespace TheLittles
{
    public enum LittleLineKind
    {
        Selected,
        Jump,
        Attention,
        Sacrifice,
        Land,
        Crawl,
        LeftBehind,
        Rescue,
        Question,
        Hurry
    }

    public sealed class LittleVoice : MonoBehaviour
    {
        private string littleName = "Little";
        private int personality;
        private float nextAttentionLine;

        public string LittleName => littleName;
        public string CurrentLine { get; private set; } = "";
        public float LineExpiresAt { get; private set; }

        public void Configure(string displayName, int personalityIndex)
        {
            littleName = displayName;
            personality = personalityIndex % 3;
        }

        public void Say(LittleLineKind kind)
        {
            if (kind == LittleLineKind.Attention && Time.time < nextAttentionLine) return;
            if (kind == LittleLineKind.Attention) nextAttentionLine = Time.time + 2.8f;

            string[][] lines =
            {
                new[]
                {
                    "Yes. I was already going.",
                    "That jump was intentional.",
                    "No thank you. I have somewhere to be.",
                    "Please continue without making this dramatic.",
                    "I meant to use all four limbs.",
                    "This floor is temporarily acceptable.",
                    "Come back. You forgot an entire person.",
                    "Hold still. I am pulling you out.",
                    "Why are humans so unnecessarily tall?",
                    "We are going to miss the bus."
                },
                new[]
                {
                    "YIPPEE. Quietly.",
                    "I regret nothing except the landing.",
                    "Please stop helping.",
                    "Tell the library I was useful.",
                    "OY OY OY. I am fine.",
                    "I have become extremely aerodynamic.",
                    "HEY. Do not leave me with the humans!",
                    "Grab my sleeve. YIPPEE responsibly.",
                    "Have either of you met broccoli?",
                    "BUS. NOW. TINY LEGS, GO!"
                },
                new[]
                {
                    "I have a library appointment.",
                    "Gravity remains disappointing.",
                    "Please do not pick me up.",
                    "This counts as participation.",
                    "The ground arrived unexpectedly.",
                    "Please ignore the knees.",
                    "Please come back. I dislike being alone.",
                    "I have you. Please stop wiggling.",
                    "Why is the sky allowed to be blue?",
                    "The driver appears emotionally punctual."
                }
            };

            CurrentLine = lines[personality][(int)kind];
            LineExpiresAt = Time.time + 2.6f;
        }
    }
}
