using UnityEngine;
using Zenject;

public class Player : MonoBehaviour
{
    public IPlayerMovement Movement => _movement;
    private IEntityRegistry<Player> _registry;

    private IPlayerMovement _movement;

    

    [Inject]
    private void Construct(IEntityRegistry<Player> registry)
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