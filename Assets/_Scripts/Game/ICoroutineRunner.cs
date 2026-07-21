using UnityEngine;
using Zenject;

public class PlayerInputController : ITickable
{
    private readonly IEntityRegistry<Player> _playerRegistry;
  
    public PlayerInputController(IEntityRegistry<Player> playerRegistry)
    {
        _playerRegistry = playerRegistry;
    }

    public void Tick()
    {
        if (!_playerRegistry.HasTarget) return;

        Player player = _playerRegistry.Current;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        player.Movement.Move(new Vector2(horizontal, vertical));
    }
}