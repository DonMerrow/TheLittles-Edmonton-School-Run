using System.Reflection;
using StarterAssets;
using UnityEngine;

/// <summary>
/// Disables the unsuitable imported ground-crawl feature while preserving wall
/// drop on C. Reflection keeps this compatible with Starter Assets versions
/// where Crawling is either a field or a property.
/// </summary>
[DefaultExecutionOrder(350)]
[DisallowMultipleComponent]
public sealed class LittleCrawlBlocker : MonoBehaviour
{
    private static readonly int CrawlingId = Animator.StringToHash("Crawling");
    private ThirdPersonController _motor;
    private LittleLedgeClimber _climber;
    private Animator _animator;
    private CharacterController _controller;
    private FieldInfo _crawlingField;
    private PropertyInfo _crawlingProperty;
    private float _standingHeight;
    private Vector3 _standingCenter;

    private void Awake()
    {
        _motor = GetComponent<ThirdPersonController>();
        _climber = GetComponent<LittleLedgeClimber>();
        _animator = GetComponent<Animator>();
        _controller = GetComponent<CharacterController>();
        if (_controller != null)
        {
            _standingHeight = _controller.height;
            _standingCenter = _controller.center;
        }

        if (_motor == null) return;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                   BindingFlags.NonPublic;
        System.Type type = _motor.GetType();
        _crawlingField = type.GetField("Crawling", flags);
        _crawlingProperty = type.GetProperty("Crawling", flags);
    }

    private void LateUpdate()
    {
        // Wall traversal owns C as Drop. Do not interfere while attached.
        if (_climber != null && _climber.IsTraversing) return;

        if (_motor != null)
        {
            if (_crawlingField != null && _crawlingField.FieldType == typeof(bool))
                _crawlingField.SetValue(_motor, false);
            if (_crawlingProperty != null && _crawlingProperty.CanWrite &&
                _crawlingProperty.PropertyType == typeof(bool))
                _crawlingProperty.SetValue(_motor, false);
        }

        if (_animator != null)
        {
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash != CrawlingId) continue;
                _animator.SetBool(CrawlingId, false);
                break;
            }
        }

        // Recover the standing capsule if the old controller shortened it for
        // one frame before Crawling was blocked.
        if (_controller != null && _controller.enabled)
        {
            _controller.height = Mathf.MoveTowards(_controller.height,
                _standingHeight, Time.deltaTime * 8f);
            _controller.center = Vector3.MoveTowards(_controller.center,
                _standingCenter, Time.deltaTime * 8f);
        }
    }
}
