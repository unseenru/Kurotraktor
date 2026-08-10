using FMOD.Studio;
using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TrackAudio : MonoBehaviour
{
    [Header("FMOD Event Link")]
    [SerializeField] private EventReference trackEvent;

    [Header("Настройки питча")]
    [Tooltip("Питч на месте (0 км/ч)")]
    [SerializeField] private float minPitch = 0.8f;

    [Tooltip("Максимальный питч на предельной скорости")]
    [SerializeField] private float maxPitch = 2.0f;

    [Tooltip("Скорость (в м/с), при которой питч станет максимальным")]
    [SerializeField] private float maxSpeed = 15f;

    private Rigidbody _rb;
    private EventInstance _trackInstance;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        if (trackEvent.IsNull) return;

        _trackInstance = RuntimeManager.CreateInstance(trackEvent);
        RuntimeManager.AttachInstanceToGameObject(_trackInstance, transform, _rb);
        _trackInstance.start();
    }

    private void Update()
    {
        if (!_trackInstance.isValid()) return;

        // В Unity 6: linearVelocity (в старых версиях: _rb.velocity)
        float currentSpeed = _rb.velocity.magnitude;

        // Переводим скорость в диапазон от 0 до 1
        float speedNormalized = Mathf.Clamp01(currentSpeed / maxSpeed);

        // Питч прямо пропорционален скорости
        float currentPitch = Mathf.Lerp(minPitch, maxPitch, speedNormalized);

        _trackInstance.setPitch(currentPitch);
    }

    private void OnDestroy()
    {
        if (_trackInstance.isValid())
        {
            RuntimeManager.DetachInstanceFromGameObject(_trackInstance);

            // IMMEDIATE глушит звук мгновенно без 5-секундных хвостов FMOD
            _trackInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _trackInstance.release();
        }
    }
}