using System.Linq;
using UnityEngine;
using Zenject;

public class PlayerInputController : ITickable
{
    private readonly IEntityRegistry<IEntity> _playerRegistry;
    private readonly CameraController _cameraController;
    
    public PlayerInputController(IEntityRegistry<IEntity> playerRegistry, CameraController cameraController)
    {
        _playerRegistry = playerRegistry;
        _cameraController = cameraController;
    }

    public void Tick()
    {
        // Берем игрока из коллекции AllEntities
        Player player = _playerRegistry.AllEntities.OfType<Player>().FirstOrDefault();
        if (player == null) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = _cameraController.Forward * vertical + _cameraController.Right * horizontal;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        player.Movement.Move(moveDirection);
    }
}