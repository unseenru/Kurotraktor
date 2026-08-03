using UnityEngine;
using Zenject;

[RequireComponent(typeof(CharacterController))]
public class ChickenMovement : MonoBehaviour
{
    private CharacterController _controller;
    private ChickenSettings _settings;
    private IEntityRegistry<Player> _playerRegistry;
    private Transform _overridePlayerTransform;
    private float _verticalVelocity;

    [Inject]
    private void Construct(IEntityRegistry<Player> registry)
    {
        _playerRegistry = registry;
    }

    private void Awake() => _controller = GetComponent<CharacterController>();

    // Вытаскиваем Transform: либо из ручного оверрайда, либо из Zenject-реестра
    private Transform TargetTransform => _overridePlayerTransform != null
        ? _overridePlayerTransform
        : (_playerRegistry != null && _playerRegistry.HasTarget && _playerRegistry.Current != null
            ? _playerRegistry.Current.transform
            : null);

    public void Initialize(ChickenSettings settings, Transform player = null)
    {
        _settings = settings;
        if (player != null) _overridePlayerTransform = player;
    }

    public void SetPlayer(Transform player) => _overridePlayerTransform = player;

    public bool IsTargetClose(float distance)
    {
        var target = TargetTransform;
        return target != null && _settings != null && GetPlanarSqrDistanceTo(target) < distance * distance;
    }

    public bool IsTargetFarEnough(float distance)
    {
        var target = TargetTransform;
        return target == null || _settings == null || GetPlanarSqrDistanceTo(target) >= distance * distance;
    }

    public Vector3 GetFleeDirection()
    {
        var target = TargetTransform;
        if (target == null) return -transform.forward;

        Vector3 direction = transform.position - target.position;
        direction.y = 0f;

        return direction.sqrMagnitude < _settings.DirectionEpsilon ? -transform.forward : direction.normalized;
    }

    public void Move(Vector3 direction, float speed)
    {
        if (_controller == null || _settings == null) return;

        if (direction.sqrMagnitude >= _settings.DirectionEpsilon)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _settings.TurnSpeed * Time.deltaTime);
        }

        _verticalVelocity = _controller.isGrounded && _verticalVelocity < 0f
            ? _settings.GroundedStickyVelocity
            : _verticalVelocity + _settings.Gravity * Time.deltaTime;

        Vector3 velocity = direction.normalized * speed + Vector3.up * _verticalVelocity;
        _controller.Move(velocity * Time.deltaTime);
    }

    private float GetPlanarSqrDistanceTo(Transform target)
    {
        Vector3 offset = transform.position - target.position;
        offset.y = 0f;
        return offset.sqrMagnitude;
    }
}