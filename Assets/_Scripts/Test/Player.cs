using UnityEngine;
using Zenject;

public class Player : MonoBehaviour
{
    private IEntityRegistry<Player> _registry;

    private IPlayerMovement _movement;

    public IPlayerMovement Movement => _movement;

    [Inject]
    public void Construct(IEntityRegistry<Player> registry)
    {
        _registry = registry;
    }
    private void Awake()
    {
        _movement = GetComponent<IPlayerMovement>();
    }

    private void Start()
    {
        _registry?.Register(this);
    }

    private void OnDestroy()
    {
        _registry?.Unregister(this);
    }
}