using StarterAssets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Keeps keyboard movement aimed at one world direction while an automatic
/// follow camera moves behind the player. Without this compensation, Starter
/// Assets makes movement depend on camera yaw while camera yaw follows movement,
/// producing the reported one-way turn and oscillation.
/// </summary>
[DefaultExecutionOrder(-250)]
[DisallowMultipleComponent]
public sealed class LittleStableFollowInput : MonoBehaviour
{
    private StarterAssetsInputs _inputs;
    private LittleLedgeClimber _climber;
    private Camera _camera;
    private Vector2 _lastRaw;
    private float _worldHeading;
    private bool _headingReady;

    private void Awake()
    {
        _inputs = GetComponent<StarterAssetsInputs>();
        _climber = GetComponent<LittleLedgeClimber>();
        _camera = Camera.main;
    }

    private void Update()
    {
        if (_inputs == null) return;
        Vector2 raw = ReadKeyboardMove();
        if (raw.sqrMagnitude < 0.01f)
        {
            _inputs.move = Vector2.zero;
            _lastRaw = Vector2.zero;
            _headingReady = false;
            return;
        }

        // Wall controls must remain exactly as they are in the working climb.
        if (_climber != null && _climber.IsTraversing)
        {
            _inputs.move = raw.normalized;
            _lastRaw = raw;
            _headingReady = false;
            return;
        }

        if (_camera == null) _camera = Camera.main;
        float cameraYaw = _camera != null ? _camera.transform.eulerAngles.y : transform.eulerAngles.y;
        if (!_headingReady || Vector2.Dot(raw.normalized, _lastRaw.normalized) < 0.995f)
        {
            float inputAngle = Mathf.Atan2(raw.x, raw.y) * Mathf.Rad2Deg;
            _worldHeading = cameraYaw + inputAngle;
            _headingReady = true;
        }
        _lastRaw = raw;

        // Convert the latched world-space destination back to camera-relative
        // input. Do not rotate the player transform here: Starter Assets owns
        // that smoothing. A previous LateUpdate rotation fought the motor and
        // caused the 180-degree camera wobble/teleport.
        float relative = (_worldHeading - cameraYaw) * Mathf.Deg2Rad;
        _inputs.move = new Vector2(Mathf.Sin(relative), Mathf.Cos(relative)) *
            Mathf.Clamp01(raw.magnitude);
    }

    private static Vector2 ReadKeyboardMove()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return Vector2.zero;
        float x = (Keyboard.current.dKey.isPressed ? 1f : 0f) -
                  (Keyboard.current.aKey.isPressed ? 1f : 0f);
        float y = (Keyboard.current.wKey.isPressed ? 1f : 0f) -
                  (Keyboard.current.sKey.isPressed ? 1f : 0f);
        return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
#else
        return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")), 1f);
#endif
    }
}
