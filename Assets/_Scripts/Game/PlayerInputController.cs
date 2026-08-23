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
        Player player = _playerRegistry.AllEntities.OfType<Player>().FirstOrDefault();
        if (player == null) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = _cameraController.Forward * vertical + _cameraController.Right * horizontal;

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        // Поворот игрока в сторону, противоположную камере, только по Y
        Vector3 lookDirection = player.transform.position - _cameraController.transform.position;
        lookDirection.y = 0;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            player.transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        player.Movement.Move(moveDirection);
    }
}