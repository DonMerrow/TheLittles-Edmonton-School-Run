using UnityEngine;

/// <summary>Small prototype HUD that makes Danny's Spark and encounters readable.</summary>
[RequireComponent(typeof(DannySpark))]
public sealed class DannySparkHUD : MonoBehaviour
{
    [SerializeField, Min(1f)] private float messageSeconds = 4.5f;
    private DannySpark spark;
    private string message = "Get to school—but the winter neighbourhood is full of wonders.";
    private float hideMessageAt = float.PositiveInfinity;
    private GUIStyle titleStyle;
    private GUIStyle messageStyle;

    private void Awake()
    {
        spark = GetComponent<DannySpark>();
        spark.SparkChanged += OnSparkChanged;
    }

    private void OnDestroy()
    {
        if (spark != null) spark.SparkChanged -= OnSparkChanged;
    }

    private void OnSparkChanged(float normalized, string reason)
    {
        message = reason;
        hideMessageAt = Time.time + messageSeconds;
    }

    private void OnGUI()
    {
        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        messageStyle ??= new GUIStyle(GUI.skin.box)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = Color.white }
        };

        Rect panel = new(24f, 22f, 330f, 72f);
        GUI.Box(panel, GUIContent.none);
        GUI.Label(new Rect(40f, 30f, 260f, 28f), "DANNY'S SPARK", titleStyle);
        Rect track = new(40f, 64f, 290f, 16f);
        GUI.Box(track, GUIContent.none);
        Color previous = GUI.color;
        GUI.color = Color.Lerp(new Color(0.35f, 0.38f, 0.42f), new Color(1f, 0.62f, 0.08f), spark.Normalized);
        GUI.DrawTexture(new Rect(track.x + 2f, track.y + 2f,
            (track.width - 4f) * spark.Normalized, track.height - 4f), Texture2D.whiteTexture);
        GUI.color = previous;

        if (Time.time <= hideMessageAt)
        {
            float width = Mathf.Min(760f, Screen.width - 80f);
            GUI.Box(new Rect((Screen.width - width) * 0.5f, Screen.height - 110f,
                width, 72f), message, messageStyle);
        }
    }
}
