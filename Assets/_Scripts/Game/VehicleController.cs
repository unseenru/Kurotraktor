using UnityEngine;

public class VehicleController : MonoBehaviour
{
    public enum VehicleState
    {
        Idle,
        Accelerating,
        Reversing,
        Braking,
        Handbraking
    }

    [System.Serializable]
    public struct WheelPair
    {
        public WheelCollider collider;
        public Transform mesh;
    }

    [Header("Wheel Bindings")]
    [SerializeField] private WheelPair frontLeft;
    [SerializeField] private WheelPair frontRight;
    [SerializeField] private WheelPair rearLeft;
    [SerializeField] private WheelPair rearRight;

    [Header("Vehicle Specs")]
    [SerializeField] private float maxMotorTorque = 2000f;
    [SerializeField] private float maxBrakeTorque = 3000f;
    [SerializeField] private float handbrakeTorque = 8000f;
    [SerializeField] private float maxSteerAngle = 35f;
    [SerializeField] private float airPitchTorque = 1500f; // Для наклона Вверх / Вниз в воздухе

    [Header("Commands (FSM Input)")]
    [SerializeField] private bool commandGas;
    [SerializeField] private bool commandBrake;
    [SerializeField] private bool commandHandbrake;
    [SerializeField] private bool commandLeft;
    [SerializeField] private bool commandRight;
    [SerializeField] private bool commandUp;
    [SerializeField] private bool commandDown;

    [Header("Current State")]
    [SerializeField] private VehicleState currentState = VehicleState.Idle;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Понижаем центр масс, чтобы машина не переворачивалась на каждом повороте
        rb.centerOfMass += new Vector3(0, -0.5f, 0);
    }

    private void Update()
    {
        UpdateFSM();
        UpdateWheelVisuals();
    }

    private void FixedUpdate()
    {
        ApplyControlAlgorithm();
    }

    // 1. Конечный автомат: определяем текущий режим движения
    private void UpdateFSM()
    {
        if (commandHandbrake)
        {
            currentState = VehicleState.Handbraking;
        }
        else if (commandBrake)
        {
            currentState = VehicleState.Braking;
        }
        else if (commandGas)
        {
            // Если зажат Down и Gas — движение назад, иначе вперед
            currentState = commandDown ? VehicleState.Reversing : VehicleState.Accelerating;
        }
        else
        {
            currentState = VehicleState.Idle;
        }
    }

    // 2. Алгоритм управления физикой и колесами
    private void ApplyControlAlgorithm()
    {
        // Расчет поворота (Влево / Вправо)
        float steerInput = 0f;
        if (commandLeft) steerInput -= 1f;
        if (commandRight) steerInput += 1f;

        float currentSteerAngle = steerInput * maxSteerAngle;
        frontLeft.collider.steerAngle = currentSteerAngle;
        frontRight.collider.steerAngle = currentSteerAngle;

        // Расчет газов и тормозов по состоянию FSM
        float currentMotor = 0f;
        float currentBrake = 0f;

        switch (currentState)
        {
            case VehicleState.Accelerating:
                currentMotor = maxMotorTorque;
                break;

            case VehicleState.Reversing:
                currentMotor = -maxMotorTorque * 0.5f;
                break;

            case VehicleState.Braking:
                currentBrake = maxBrakeTorque;
                break;

            case VehicleState.Handbraking:
                // Ручник бьет по задней оси
                rearLeft.collider.brakeTorque = handbrakeTorque;
                rearRight.collider.brakeTorque = handbrakeTorque;
                break;

            case VehicleState.Idle:
                // Минимальное сопротивление
                currentBrake = 10f;
                break;
        }

        // Применяем крутящий момент на ведущую ось (задний привод)
        rearLeft.collider.motorTorque = currentMotor;
        rearRight.collider.motorTorque = currentMotor;

        // Применяем рабочий тормоз на все 4 колеса
        if (currentState != VehicleState.Handbraking)
        {
            frontLeft.collider.brakeTorque = currentBrake;
            frontRight.collider.brakeTorque = currentBrake;
            rearLeft.collider.brakeTorque = currentBrake;
            rearRight.collider.brakeTorque = currentBrake;
        }

        // Обработка команд Вверх / Вниз (для наклонов в воздухе)
        HandleAirControl();
    }

    // Вспомогательная логика для Вверх/Вниз вне контакта с землей
    private void HandleAirControl()
    {
        bool isGrounded = frontLeft.collider.isGrounded || frontRight.collider.isGrounded ||
                           rearLeft.collider.isGrounded || rearRight.collider.isGrounded;

        if (!isGrounded)
        {
            float pitchInput = 0f;
            if (commandUp) pitchInput += 1f;    // Задираем нос
            if (commandDown) pitchInput -= 1f;  // Опускаем нос

            if (Mathf.Abs(pitchInput) > 0.01f)
            {
                rb.AddRelativeTorque(Vector3.right * pitchInput * airPitchTorque, ForceMode.Acceleration);
            }
        }
    }

    // 3. Связка трансформа 3D-модели колеса с физическим WheelCollider
    private void UpdateWheelVisuals()
    {
        SyncWheelMesh(frontLeft);
        SyncWheelMesh(frontRight);
        SyncWheelMesh(rearLeft);
        SyncWheelMesh(rearRight);
    }

    private void SyncWheelMesh(WheelPair pair)
    {
        if (pair.collider == null || pair.mesh == null) return;

        pair.collider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        pair.mesh.position = pos;
        pair.mesh.rotation = rot;
    }
}