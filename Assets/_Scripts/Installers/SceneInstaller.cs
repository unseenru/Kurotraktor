using UnityEngine;
using Zenject;

public class SceneInstaller : MonoInstaller
{
    [Header("Configs")]
    [SerializeField] private GameSettingsSO _gameSettings;

    [Header("Scene References")]
    [SerializeField] private CameraController _cameraController;

    [Header("Prefabs")]
    [SerializeField] private GameObject _playerPrefab;

    public override void InstallBindings()
    {
        BindSettings();
        BindInfrastructure();
        BindEntities();
        BindControllers();
    }

    private void BindSettings()
    {

        Container.BindInstance(_gameSettings).AsSingle();
        Container.BindInstance(_gameSettings.Camera).AsSingle();
        Container.BindInstance(_gameSettings.Player).AsSingle();
    }

    private void BindInfrastructure()
    {
        Container.Bind(typeof(IEntityRegistry<>)).To(typeof(EntityRegistry<>)).AsSingle();
    }

    private void BindEntities()
    {
        Container.Bind<Player>().FromComponentInNewPrefab(_playerPrefab).AsTransient();
        Container.Bind<CameraController>().FromInstance(_cameraController).AsSingle();
    }

    private void BindControllers()
    {
        Container.BindInterfacesAndSelfTo<PlayerInputController>().AsSingle();
    }
}