using UnityEngine;
using TMPro;
using Zenject;

public class ChickenCollector : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _scoreText; // Прямая ссылка на TextMeshProUGUI

    [Header("Raycast Settings")]
    [SerializeField] private Transform _rayOrigin;       // Откуда пускать луч (если null, берется transform машины)
    [SerializeField] private float _rayDistance = 1.0f;  // Дистанция срабатывания (1 метр)
    [SerializeField] private LayerMask _chickenLayer;   // Слой, на котором находятся курицы

    private int _collectedCount = 0;
    private IEntityRegistry<IEntity> _entityRegistry;

    public int CollectedCount => _collectedCount;

    [Inject]
    private void Construct(IEntityRegistry<IEntity> entityRegistry)
    {
        _entityRegistry = entityRegistry;
    }

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        CheckForChicken();
    }

    private void CheckForChicken()
    {
        Transform origin = _rayOrigin != null ? _rayOrigin : transform;

        // Пускаем луч из машины вперед
        if (Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, _rayDistance, _chickenLayer))
        {
            // Проверяем, попал ли луч в курицу
            if (hit.collider.TryGetComponent<Chicken>(out var chicken))
            {
                Collect(chicken);
            }
        }
    }

    private void Collect(Chicken chicken)
    {
        // 1. Если курицы зарегистрированы в реестре сущностей — удаляем из него
        if (chicken is IEntity entity)
        {
            _entityRegistry?.Unregister(entity);
        }

        // 2. Увеличиваем счетчик конкретного транспорта
        _collectedCount++;
        UpdateUI();

        // 3. Уничтожаем префаб курицы
        Destroy(chicken.gameObject);
    }

    private void UpdateUI()
    {
        if (_scoreText != null)
        {
            _scoreText.text = $"{_collectedCount}";
        }
    }

    // Отрисовка луча в редакторе (Scene View) для удобной настройки
    private void OnDrawGizmosSelected()
    {
        Transform origin = _rayOrigin != null ? _rayOrigin : transform;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin.position, origin.forward * _rayDistance);
    }
}