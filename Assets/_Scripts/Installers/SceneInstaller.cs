using TMPro.Examples;
using UnityEngine;
using Zenject;

public class GameSceneInstaller : MonoInstaller
{
    [Header("Scene References")]
    [SerializeField] private CameraController _cameraController;

    [Header("Prefabs")]
    [SerializeField] private GameObject _playerPrefab;

    public override void InstallBindings()
    {
        BindInfrastructure();
        BindPlayer();
        BindCamera();
        BindControllers();
    }
    private void BindInfrastructure()
    {
        Container.Bind(typeof(IEntityRegistry<>))
                 .To(typeof(EntityRegistry<>))
                 .AsSingle();
    }

    private void BindPlayer()
    {
        Container.Bind<Player>()
                 .FromComponentInNewPrefab(_playerPrefab)
                 .AsTransient();
    }

    private void BindCamera()
    {
        Container.Bind<CameraController>()
                 .FromInstance(_cameraController)
                 .AsSingle();
    }

    private void BindControllers()
    {
        
        Container.BindInterfacesAndSelfTo<PlayerInputController>()
                 .AsSingle();
    }
}