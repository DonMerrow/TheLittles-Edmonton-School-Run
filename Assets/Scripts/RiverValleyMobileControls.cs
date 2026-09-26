using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>Small, dependency-free touch controls for the River Valley game.</summary>
[DefaultExecutionOrder(-1000)]
public sealed class RiverValleyMobileControls : MonoBehaviour
{
    public static Vector2 Move { get; private set; }
    public static Vector2 LookDelta { get; private set; }
    public static bool ActionHeld { get; private set; }
    public static bool ActionPressed => actionPressedFrame == Time.frameCount;
    public static bool SecondaryPressed => secondaryPressedFrame == Time.frameCount;
    public static bool JumpPressed => jumpPressedFrame == Time.frameCount;
    public static bool FlashlightPressed => flashlightPressedFrame == Time.frameCount;
    public static bool CameraPressed => cameraPressedFrame == Time.frameCount;
    public static bool IsAvailable => Application.isMobilePlatform || Touchscreen.current != null;

    private static int actionPressedFrame = -1;
    private static int secondaryPressedFrame = -1;
    private static int jumpPressedFrame = -1;
    private static int flashlightPressedFrame = -1;
    private static int cameraPressedFrame = -1;
    private bool previousAction;
    private bool previousSecondary;
    private bool previousJump;
    private bool previousFlashlight;
    private bool previousCamera;
    private GUIStyle buttonStyle;
    private GUIStyle hintStyle;
    private RiverValleyGameDirector director;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (!IsAvailable || FindFirstObjectByType<RiverValleyMobileControls>() != null) return;
        GameObject controls = new("Phone touch controls");
        controls.AddComponent<RiverValleyMobileControls>();
    }

    private void Update()
    {
        if (!IsAvailable) return;
        if (director == null) director = FindFirstObjectByType<RiverValleyGameDirector>();
        float s = ControlSize;
        Rect left = BottomRect(22f, 22f + s, s, s);
        Rect right = BottomRect(22f + s * 2f, 22f + s, s, s);
        Rect up = BottomRect(22f + s, 22f + s * 2f, s, s);
        Rect down = BottomRect(22f + s, 22f, s, s);
        Rect action = BottomRect(Screen.width - s - 22f, 22f, s, s);
        Rect jump = BottomRect(Screen.width - s * 2.15f - 22f, 22f, s, s);
        Rect secondary = BottomRect(Screen.width - s - 22f, 22f + s * 1.18f, s, s);
        Rect flashlight = BottomRect(Screen.width - s * 2.15f - 22f, 22f + s * 1.18f, s, s);
        Rect camera = BottomRect(Screen.width - s * 3.30f - 22f, 22f + s * 1.18f, s, s);

        bool l = IsTouched(left), r = IsTouched(right), u = IsTouched(up), d = IsTouched(down);
        Move = Vector2.ClampMagnitude(new Vector2((r ? 1f : 0f) - (l ? 1f : 0f),
            (u ? 1f : 0f) - (d ? 1f : 0f)), 1f);
        bool newAction = IsTouched(action);
        bool newSecondary = IsTouched(secondary);
        bool newJump = IsTouched(jump);
        bool newFlashlight = IsTouched(flashlight);
        bool newCamera = IsTouched(camera);
        if (newAction && !previousAction) actionPressedFrame = Time.frameCount;
        if (newSecondary && !previousSecondary) secondaryPressedFrame = Time.frameCount;
        if (newJump && !previousJump) jumpPressedFrame = Time.frameCount;
        if (newFlashlight && !previousFlashlight) flashlightPressedFrame = Time.frameCount;
        if (newCamera && !previousCamera) cameraPressedFrame = Time.frameCount;
        ActionHeld = newAction;
        previousAction = newAction;
        previousSecondary = newSecondary;
        previousJump = newJump;
        previousFlashlight = newFlashlight;
        previousCamera = newCamera;

        LookDelta = Vector2.zero;
        Touchscreen screen = Touchscreen.current;
        if (screen == null) return;
        foreach (TouchControl touch in screen.touches)
        {
            if (!touch.press.isPressed) continue;
            Vector2 position = touch.position.ReadValue();
            if (position.x < Screen.width * 0.42f || IsAnyControl(position, left, right, up, down,
                    action, jump, secondary, flashlight, camera)) continue;
            LookDelta += touch.delta.ReadValue();
        }
    }

    private void OnGUI()
    {
        if (!IsAvailable) return;
        float s = ControlSize;
        buttonStyle ??= new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold,
            fontSize = Mathf.RoundToInt(Mathf.Max(15f, s * 0.22f)),
            normal = { textColor = Color.white }
        };
        hintStyle ??= new GUIStyle(buttonStyle)
        {
            fontSize = Mathf.RoundToInt(Mathf.Max(12f, s * 0.16f)), wordWrap = true
        };
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.66f);
        DrawBottom(new Rect(22f, 22f + s, s, s), "◀");
        DrawBottom(new Rect(22f + s * 2f, 22f + s, s, s), "▶");
        DrawBottom(new Rect(22f + s, 22f + s * 2f, s, s), "▲");
        DrawBottom(new Rect(22f + s, 22f, s, s), "▼");
        DrawBottom(new Rect(Screen.width - s - 22f, 22f, s, s),
            director != null && director.HasHelpAction ? "OK\nINTERACT" : "OK\nACTION");
        DrawBottom(new Rect(Screen.width - s * 2.15f - 22f, 22f, s, s), "JUMP");
        DrawBottom(new Rect(Screen.width - s - 22f, 22f + s * 1.18f, s, s),
            director != null && director.HasChoiceAction ? "CHOICE\nSELECT" : "CHOICE\nLOCKED");
        DrawBottom(new Rect(Screen.width - s * 2.15f - 22f, 22f + s * 1.18f, s, s), "FLASH\nLIGHT");
        DrawBottom(new Rect(Screen.width - s * 3.30f - 22f, 22f + s * 1.18f, s, s), "CAMERA");
        GUI.Box(new Rect(Screen.width * 0.42f, Screen.height - 31f, Screen.width * 0.25f, 25f),
            "Swipe open space to look", hintStyle);
        GUI.color = old;
    }

    private static float ControlSize => Mathf.Clamp(Screen.height * 0.105f, 64f, 118f);
    private static Rect BottomRect(float x, float y, float width, float height) => new(x, y, width, height);
    private static Rect GuiRect(Rect bottom) => new(bottom.x, Screen.height - bottom.y - bottom.height,
        bottom.width, bottom.height);
    private void DrawBottom(Rect rect, string label) => GUI.Box(GuiRect(rect), label, buttonStyle);

    private static bool IsTouched(Rect rect)
    {
        Touchscreen screen = Touchscreen.current;
        if (screen == null) return false;
        foreach (TouchControl touch in screen.touches)
            if (touch.press.isPressed && rect.Contains(touch.position.ReadValue())) return true;
        return false;
    }

    private static bool IsAnyControl(Vector2 point, params Rect[] rects)
    {
        foreach (Rect rect in rects) if (rect.Contains(point)) return true;
        return false;
    }
}
