using UnityEngine;
using Zenject;

public class PlayerInputController : ITickable
{
    private readonly IEntityRegistry<Player> _playerRegistry;
    private readonly CameraController _cameraController;

    public PlayerInputController(IEntityRegistry<Player> playerRegistry, CameraController cameraController)
    {
        _playerRegistry = playerRegistry;
        _cameraController = cameraController;
    }

    public void Tick()
    {
        if (!_playerRegistry.HasTarget) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = _cameraController.Forward * vertical + _cameraController.Right * horizontal;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        _playerRegistry.Current.Movement.Move(moveDirection);
    }
}