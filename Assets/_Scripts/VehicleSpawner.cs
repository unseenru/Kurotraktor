using System.Linq;
using UnityEngine;
using Zenject;

public class VehicleSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _vehicleObject;
    [SerializeField] private GameObject _vehiclePlayerObject;
    [SerializeField] private Component[] _vehiclePlayerComponents;

    [SerializeField] private float _interactionDistance = 2f;
    [SerializeField] private KeyCode _interactionKey = KeyCode.E;

    private IEntityRegistry<IEntity> _entityRegistry;

    [Inject]
    public void Construct(IEntityRegistry<IEntity> entityRegistry)
    {
        _entityRegistry = entityRegistry;
    }

    private void Update()
    {
        if (_vehicleObject == null || _vehiclePlayerObject == null)
            return;

        if (_vehiclePlayerObject.activeInHierarchy)
        {
            TryExitVehicle();
            return;
        }

        TryEnterVehicle();
    }

    private void TryEnterVehicle()
    {
        Player player = GetActivePlayer();

        if (player == null)
            return;

        Vector3 interactionPosition = GetInteractionPosition();

        float distance = GetHorizontalDistance(
            player.transform.position,
            interactionPosition);

        if (distance > _interactionDistance)
            return;

        if (!Input.GetKeyDown(_interactionKey))
            return;

        EnterVehicle(player);
    }

    private void EnterVehicle(Player player)
    {
        if (!_vehicleObject.activeInHierarchy)
        {
            Vector3 spawnPosition = transform.position;
            spawnPosition.y = _vehicleObject.transform.position.y;

            _vehicleObject.transform.SetPositionAndRotation(
                spawnPosition,
                transform.rotation);

            _vehicleObject.SetActive(true);
        }

        player.gameObject.SetActive(false);

        _vehiclePlayerObject.SetActive(true);
        SetVehiclePlayerComponentsActive(true);
    }

    private void TryExitVehicle()
    {
        if (!_vehiclePlayerObject.activeInHierarchy)
            return;

        float distance = GetHorizontalDistance(
            _vehiclePlayerObject.transform.position,
            _vehicleObject.transform.position);

        if (distance > _interactionDistance)
            return;

        if (!Input.GetKeyDown(_interactionKey))
            return;

        ExitVehicle();
    }

    private void ExitVehicle()
    {
        Player player = GetPlayer();

        if (player == null)
            return;

        Vector3 exitPosition =
            _vehicleObject.transform.position +
            _vehicleObject.transform.right * _interactionDistance;

        exitPosition.y = player.transform.position.y;

        SetVehiclePlayerComponentsActive(false);

        _vehiclePlayerObject.SetActive(false);

        player.transform.position = exitPosition;
        player.gameObject.SetActive(true);
    }

    private Vector3 GetInteractionPosition()
    {
        if (_vehicleObject.activeInHierarchy)
            return _vehicleObject.transform.position;

        return transform.position;
    }

    private float GetHorizontalDistance(
        Vector3 firstPosition,
        Vector3 secondPosition)
    {
        firstPosition.y = 0f;
        secondPosition.y = 0f;

        return Vector3.Distance(firstPosition, secondPosition);
    }

    private void SetVehiclePlayerComponentsActive(bool isActive)
    {
        foreach (Component component in _vehiclePlayerComponents)
        {
            if (component == null)
                continue;
            
            switch (component)
            {
                case Behaviour behaviour:
                    behaviour.enabled = isActive;
                    break;

                case Collider collider:
                    collider.enabled = isActive;
                    break;

                case Renderer renderer:
                    renderer.enabled = isActive;
                    break;
            }
        }
    }

    private Player GetActivePlayer()
    {
        return _entityRegistry.AllEntities
            .OfType<Player>()
            .FirstOrDefault(player =>
                player != null &&
                player.gameObject.activeInHierarchy &&
                player.gameObject != _vehiclePlayerObject);
    }

    private Player GetPlayer()
    {
        return _entityRegistry.AllEntities
            .OfType<Player>()
            .FirstOrDefault(player =>
                player != null &&
                player.gameObject != _vehiclePlayerObject);
    }
}