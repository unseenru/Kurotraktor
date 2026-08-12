using UnityEngine;
using TMPro;
using Zenject;

public class LookAtCameraController : MonoBehaviour
{
    [Header("Screen Size Settings")]
    [Tooltip("Визуальный размер текста на экране (чем больше значение, тем крупнее текст)")]
    [SerializeField] private float _screenSize = 0.1f;

    [Header("Fade Settings")]
    [Tooltip("Дистанция, ближе которой текст начинает затухать (в метрах)")]
    [SerializeField] private float _fadeDistance = 10f;

    private CameraController _cameraController;
    private Transform _cameraTransform;
    private TMP_Text _text;

    [Inject]
    private void Construct(CameraController cameraController)
    {
        _cameraController = cameraController;
    }

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
        if (_text == null)
            _text = GetComponentInChildren<TMP_Text>();
    }

    private void Start()
    {
        CacheCamera();
    }

    private void LateUpdate()
    {
        if (_cameraTransform == null)
        {
            CacheCamera();
            if (_cameraTransform == null) return;
        }

        // 1. Поворачиваем текст параллельно плоскости камеры
        transform.rotation = _cameraTransform.rotation;

        // 2. Вычисляем дистанцию до камеры
        float distance = Vector3.Distance(transform.position, _cameraTransform.position);

        // 3. Масштабируем текст пропорционально дистанции (постоянный размер на экране)
        transform.localScale = Vector3.one * (distance * _screenSize);

        // 4. Плавное затухание прозрачности при приближении ближе 10м
        if (_text != null)
        {
            float alpha = Mathf.Clamp01(distance / _fadeDistance);
            Color color = _text.color;
            color.a = alpha;
            _text.color = color;
        }
    }

    private void CacheCamera()
    {
        if (_cameraController != null)
        {
            _cameraTransform = _cameraController.transform;
        }
        else
        {
            _cameraController = FindObjectOfType<CameraController>();
            if (_cameraController != null)
            {
                _cameraTransform = _cameraController.transform;
            }
        }
    }
}