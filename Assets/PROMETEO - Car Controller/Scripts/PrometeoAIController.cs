using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PrometeoCarController))]
[RequireComponent(typeof(Rigidbody))]
public class PrometeoSplineAIController : MonoBehaviour
{
    [Header("Spline Path")]
    [SerializeField] private SplineContainer splineContainer;

    [Header("Speed Settings")]
    [Tooltip("Базовая скорость на прямой (км/ч)")]
    [SerializeField] private float baseMaxSpeed = 100f;

    [Header("Hard Physical Braking (Физический Стоп-Кран)")]
    [Tooltip("Сила принудительного гашения скорости (10-20 = жесткое торможение)")]
    [SerializeField] private float physicalBrakePower = 15f;

    [Header("Speed Zones (TrackWaypoints)")]
    [SerializeField] private List<TrackWaypoint> trackWaypoints = new List<TrackWaypoint>();

    [Header("Look-Ahead Settings")]
    [SerializeField] private float baseLookAhead = 10f;
    [SerializeField] private float speedLookAheadFactor = 0.15f;
    [SerializeField] private float steerDeadzone = 0.05f;

    private NavMeshAgent agent;
    private PrometeoCarController carController;
    private Rigidbody rb;

    private float targetAllowedSpeed;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        carController = GetComponent<PrometeoCarController>();
        rb = GetComponent<Rigidbody>();

        agent.updatePosition = false;
        agent.updateRotation = false;
    }

    private void Update()
    {
        if (splineContainer == null || !agent.isOnNavMesh) return;

        // 1. Привязка физики к NavMesh
        agent.nextPosition = transform.position;

        // 2. Расчет целевой скорости для текущей зоны
        targetAllowedSpeed = GetAllowedSpeedForPosition();

        // 3. Вычисление точки прицела (Look-Ahead)
        SplineUtility.GetNearestPoint(
            splineContainer.Spline,
            splineContainer.transform.InverseTransformPoint(transform.position),
            out float3 nearestPointLocal,
            out float normalizedTime
        );

        float currentSpeed = Mathf.Abs(carController.carSpeed);
        float dynamicLookAhead = baseLookAhead + (currentSpeed * speedLookAheadFactor);

        float splineLength = splineContainer.Spline.GetLength();
        float targetDistance = ((normalizedTime * splineLength) + dynamicLookAhead) % splineLength;

        Vector3 targetWorldPosition = splineContainer.transform.TransformPoint(
            splineContainer.EvaluatePosition(targetDistance / splineLength)
        );

        agent.SetDestination(targetWorldPosition);

        // 4. Рулежка
        Vector3 desiredVelocity = agent.desiredVelocity;
        if (desiredVelocity.sqrMagnitude < 0.01f)
        {
            carController.ThrottleOff();
            carController.ResetSteeringAngle();
            return;
        }

        Vector3 localVel = transform.InverseTransformDirection(desiredVelocity);

        if (localVel.x > steerDeadzone)
        {
            carController.TurnRight();
        }
        else if (localVel.x < -steerDeadzone)
        {
            carController.TurnLeft();
        }
        else
        {
            carController.ResetSteeringAngle();
        }

        // 5. ЛОГИКА ГАЗА И ТОРМОЗА
        if (localVel.z > 0.05f)
        {
            // Если скорость ПРЕВЫШАЕТ лимит — полностью сбрасываем газ!
            if (currentSpeed > targetAllowedSpeed)
            {
                carController.ThrottleOff();
                carController.Brakes();
            }
            else
            {
                carController.CancelInvoke("DecelerateCar");
                carController.GoForward();
            }
        }
        else if (localVel.z < -0.2f)
        {
            carController.CancelInvoke("DecelerateCar");
            carController.GoReverse();
        }
        else
        {
            carController.ThrottleOff();
        }
    }

    private void FixedUpdate()
    {
        // 6. ПРИНУДИТЕЛЬНОЕ ФИЗИЧЕСКОЕ ГАШЕНИЕ ИМПУЛЬСА (Применяется в физическом цикле)
        float currentSpeed = Mathf.Abs(carController.carSpeed);

        if (currentSpeed > targetAllowedSpeed + 2f && rb != null)
        {
            // Применяем физическую силу против вектора движения, сбивая скорость колом
            Vector3 brakeDirection = -rb.velocity.normalized;
            rb.AddForce(brakeDirection * physicalBrakePower, ForceMode.Acceleration);
        }
    }

    // Расчет лимита скорости с учетом высоты и расстояния
    private float GetAllowedSpeedForPosition()
    {
        float minAllowedSpeed = baseMaxSpeed;

        foreach (var wp in trackWaypoints)
        {
            if (wp == null || !wp.isSlowZone) continue;

            // Считаем дистанцию в 2D (игнорируем разницу по высоте Y)
            Vector3 carPos2D = Vector3.ProjectOnPlane(transform.position, Vector3.up);
            Vector3 wpPos2D = Vector3.ProjectOnPlane(wp.transform.position, Vector3.up);

            float distance = Vector3.Distance(carPos2D, wpPos2D);

            // Если машина внутри зоны замедления (или подлетает к ней)
            if (distance <= wp.zoneRadius)
            {
                float zoneLimit = baseMaxSpeed * wp.speedMultiplier;
                if (zoneLimit < minAllowedSpeed)
                {
                    minAllowedSpeed = zoneLimit;
                }
            }
        }

        return minAllowedSpeed;
    }
}