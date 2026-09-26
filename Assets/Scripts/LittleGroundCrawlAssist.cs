using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Small corrective layer for the imported one-direction crawl clip.
/// The main controller still owns forward motion and collision. This adds the
/// missing A/D ground shuffle and removes imported root roll without affecting
/// normal locomotion or wall traversal.
/// </summary>
[DefaultExecutionOrder(120)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class LittleGroundCrawlAssist : MonoBehaviour
{
    public float SidewaysCrawlSpeed = 0.72f;

    private static readonly int CrawlingId = Animator.StringToHash("Crawling");
    private readonly HashSet<int> _parameters = new HashSet<int>();
    private Animator _animator;
    private CharacterController _controller;
    private LittleLedgeClimber _climber;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
        _climber = GetComponent<LittleLedgeClimber>();
        if (_animator == null) return;
        foreach (AnimatorControllerParameter parameter in _animator.parameters)
            _parameters.Add(parameter.nameHash);
    }

    private bool IsGroundCrawling => _animator != null &&
        _parameters.Contains(CrawlingId) && _animator.GetBool(CrawlingId) &&
        (_climber == null || !_climber.IsTraversing);

    private void Update()
    {
        if (!IsGroundCrawling || _controller == null || !_controller.enabled) return;
        float side = ReadSideInput();
        if (Mathf.Abs(side) < 0.05f) return;
        // A/D now always supplies a real lateral ground-crawl displacement.
        // The regular controller continues to provide forward/back movement.
        _controller.Move(transform.right * (side * SidewaysCrawlSpeed * Time.deltaTime));
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!IsGroundCrawling || _animator == null || !_animator.isHuman) return;
        Quaternion body = _animator.bodyRotation;
        Vector3 forward = Vector3.ProjectOnPlane(body * Vector3.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f) forward = transform.forward;
        // Retain the crawl pose from the limbs, but strip the imported root roll
        // that made the whole character lean permanently to the left.
        _animator.bodyRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private static float ReadSideInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null) return 0f;
        return (Keyboard.current.dKey.isPressed ? 1f : 0f) -
               (Keyboard.current.aKey.isPressed ? 1f : 0f);
#else
        return Input.GetAxisRaw("Horizontal");
#endif
    }
}
