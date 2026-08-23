using System.Linq;
using UnityEngine;
using Zenject;

public class PlayerInputController : ITickable
{
    private readonly IEntityRegistry<IEntity> _playerRegistry;
    private readonly CameraController _cameraController;

    [Inject] private Animator _playerAnimator;
    private const string _animKeyWalk = "isWalk";
    [SerializeField] private float _rotationSpeed = 10f; // скорость плавного поворота

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

        bool isMoving = moveDirection.sqrMagnitude > 0.001f;
        _playerAnimator.SetBool(_animKeyWalk, isMoving);

        if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();

        if (isMoving)
        {
            Vector3 lookDirection = moveDirection;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                player.transform.rotation = Quaternion.Slerp(
                    player.transform.rotation,
                    targetRotation,
                    Time.deltaTime * _rotationSpeed // скорость поворота, настраиваемая
                );
            }
        }

        player.Movement.Move(moveDirection);
    }
}