using UnityEngine;
using Zenject;

public class CameraController : MonoBehaviour
{
    private IEntityRegistry<Player> _playerRegistry;
    private CameraSettings _settings;

    private float _yaw;
    private float _pitch;

    public Vector3 Forward => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
    public Vector3 Right => Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

    [Inject]
    public void Construct(IEntityRegistry<Player> playerRegistry, CameraSettings settings)
    {
        _playerRegistry = playerRegistry;
        _settings = settings;
    }

    private void LateUpdate()
    {
        if (!_playerRegistry.HasTarget) return;

        Vector3 center = _playerRegistry.Current.transform.position + _settings.TargetOffset;

        _yaw += Input.GetAxis("Mouse X") * _settings.Sensitivity;
        _pitch -= Input.GetAxis("Mouse Y") * _settings.Sensitivity;
        _pitch = Mathf.Clamp(_pitch, _settings.VerticalLimits.x, _settings.VerticalLimits.y);

        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        transform.position = center + (rotation * new Vector3(0f, 0f, -_settings.Distance));
        transform.LookAt(center);
    }
}