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
    [Header("Spline Selection")]
    [SerializeField] private SplineContainer splineContainer;
    [Tooltip("Индекс сплайна в контейнере (0, 1, 2, 3...)")]
    [SerializeField] private int splineIndex = 0;

    [Header("Speed Settings")]
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float physicalBrakePower = 15f;

    [Header("Look-Ahead Settings (Дистанция взгляда)")]
    [Tooltip("Чем больше значение (20-30), тем более прямая траектория на скорости")]
    [SerializeField] private float baseLookAhead = 20f;
    [SerializeField] private float speedLookAheadFactor = 0.18f;

    [Header("Anti-Oscillation (Гашение змейки)")]
    [Tooltip("Сила гашения инерции поворота (чем выше, тем раньше отпускается руль)")]
    [SerializeField] private float predictionFactor = 0.18f;
    [Tooltip("Мертвая зона руля в градусах")]
    [SerializeField] private float steerDeadzone = 3.0f;

    [Header("Waypoints (Slow Zones)")]
    [SerializeField] private List<TrackWaypoint> trackWaypoints = new List<TrackWaypoint>();

    private NavMeshAgent agent;
    private PrometeoCarController carController;
    private Rigidbody rb;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        carController = GetComponent<PrometeoCarController>();
        rb = GetComponent<Rigidbody>();

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    private void Update()
    {
        if (splineContainer == null ||
            splineContainer.Splines == null ||
            splineIndex < 0 ||
            splineIndex >= splineContainer.Splines.Count)
            return;

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            return;
        }

        agent.nextPosition = transform.position;

        Spline currentSpline = splineContainer.Splines[splineIndex];
        float splineLength = currentSpline.GetLength();
        if (splineLength <= 0f) return;

        // 1. Поиск ближайшей точки на сплайне
        SplineUtility.GetNearestPoint(
            currentSpline,
            splineContainer.transform.InverseTransformPoint(transform.position),
            out _,
            out float normalizedTime
        );

        // 2. Динамический Look-Ahead (упреждение от скорости)
        float currentSpeed = Mathf.Abs(carController.carSpeed);
        float dynamicLookAhead = baseLookAhead + (currentSpeed * speedLookAheadFactor);

        float targetDistance = ((normalizedTime * splineLength) + dynamicLookAhead) % splineLength;
        float targetNormalizedTime = targetDistance / splineLength;

        float3 localTargetPos = SplineUtility.EvaluatePosition(currentSpline, targetNormalizedTime);
        Vector3 worldTargetPos = splineContainer.transform.TransformPoint((Vector3)localTargetPos);

        agent.SetDestination(worldTargetPos);

        // 3. Выбор целевой точки
        Vector3 finalSteerTarget;
        float distanceToSpline = Vector3.Distance(transform.position, worldTargetPos);

        if (distanceToSpline > 12f)
            finalSteerTarget = agent.steeringTarget;
        else
            finalSteerTarget = worldTargetPos;

        // 4. Предиктивное руление без овершута
        ApplyPredictiveSteering(finalSteerTarget);

        // 5. Контроль скорости
        ApplySpeedControl(currentSpeed);
    }

    private void FixedUpdate()
    {
        float currentSpeed = Mathf.Abs(carController.carSpeed);
        float allowedSpeed = GetAllowedSpeedForPosition();

        if (currentSpeed > allowedSpeed + 2f && rb != null)
        {
            rb.AddForce(-rb.velocity.normalized * physicalBrakePower, ForceMode.Acceleration);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (rb != null)
        {
            // Сбрасываем угловое закручивание при столкновениях
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void ApplyPredictiveSteering(Vector3 targetPosition)
    {
        Vector3 localTarget = transform.InverseTransformPoint(targetPosition);

        if (localTarget.z > -1f)
        {
            // Угол до цели в градусах (-180..180)
            float angleToTarget = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;

            // Текущая угловая скорость вращения кузова вокруг оси Y (в град/сек)
            float turnSpeed = rb.angularVelocity.y * Mathf.Rad2Deg;

            // Прогноз угла с учетом инерции вращения:
            // Если машина уже быстро поворачивает направо, predictedAngle станет близким к 0 еще ДО достижения цели
            float predictedAngle = angleToTarget - (turnSpeed * predictionFactor);

            if (Mathf.Abs(predictedAngle) < steerDeadzone)
            {
                // Заранее выравниваем руль прямо, чтобы не проскочить линию
                carController.ResetSteeringAngle();
            }
            else if (predictedAngle > 0f)
            {
                carController.TurnRight();
            }
            else
            {
                carController.TurnLeft();
            }
        }
        else // Задний ход при полном развороте
        {
            carController.ThrottleOff();
            carController.GoReverse();
            carController.TurnRight();
        }
    }

    private void ApplySpeedControl(float currentSpeed)
    {
        float targetSpeedLimit = GetAllowedSpeedForPosition();

        if (currentSpeed > targetSpeedLimit)
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

    private float GetAllowedSpeedForPosition()
    {
        float minAllowedSpeed = maxSpeed;

        foreach (var wp in trackWaypoints)
        {
            if (wp == null || !wp.isSlowZone) continue;

            float distance = Vector3.Distance(
                Vector3.ProjectOnPlane(transform.position, Vector3.up),
                Vector3.ProjectOnPlane(wp.transform.position, Vector3.up)
            );

            if (distance <= wp.zoneRadius)
            {
                float zoneLimit = maxSpeed * wp.speedMultiplier;
                if (zoneLimit < minAllowedSpeed)
                    minAllowedSpeed = zoneLimit;
            }
        }

        return minAllowedSpeed;
    }

    public void SetSplineIndex(int newIndex)
    {
        if (splineContainer != null && newIndex >= 0 && newIndex < splineContainer.Splines.Count)
        {
            splineIndex = newIndex;
        }
    }
}