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
    [Tooltip("Текущий индекс сплайна в контейнере (0, 1, 2, 3...)")]
    public int splineIndex = 0;

    [Header("Speed Settings")]
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float physicalBrakePower = 15f;

    [Header("Look-Ahead Settings")]
    [SerializeField] private float baseLookAhead = 20f;
    [SerializeField] private float speedLookAheadFactor = 0.18f;

    [Header("Anti-Oscillation")]
    [SerializeField] private float predictionFactor = 0.18f;
    [SerializeField] private float steerDeadzone = 3.0f;

    [Header("AI Decision Timing")]
    [SerializeField] private float minDecisionInterval = 0.4f;
    [SerializeField] private float maxDecisionInterval = 1.0f;

    [Header("Overtake & Lane Change Mechanics")]
    [Range(0f, 100f)]
    [Tooltip("Шанс случайной перестройки на свободной дороге")]
    public float laneChangeProbability = 30f;

    [Tooltip("Дистанция обнаружения транспорта перед капотом")]
    [SerializeField] private float frontDetectionRange = 25f;

    [Tooltip("Множитель скорости при обгоне (Газ в пол!)")]
    [SerializeField] private float aggressiveSpeedMultiplier = 1.4f;

    [Tooltip("Индексы сплайнов, доступные для перестройки")]
    [SerializeField] private List<int> allowedLaneChangeSplines = new List<int>();

    [Header("Cooperative & Yielding System")]
    [Tooltip("Множитель скорости при уступке дороги (притормаживание)")]
    [SerializeField] private float yieldSpeedMultiplier = 0.7f;
    [SerializeField] private float yieldCheckDistance = 12f;

    [Header("Lane Change Intervals")]
    [SerializeField] private float minLaneChangeCooldown = 1.5f;
    [SerializeField] private float maxLaneChangeCooldown = 4.0f;

    [Header("Waypoints (Slow Zones)")]
    [SerializeField] private List<TrackWaypoint> trackWaypoints = new List<TrackWaypoint>();

    private NavMeshAgent agent;
    private PrometeoCarController carController;
    private Rigidbody rb;

    // Таймеры и состояния
    private float decisionTimer = 0f;
    private float currentDecisionInterval = 0.5f;
    private float laneChangeCooldownTimer = 0f;

    // Состояния обгона и уступки
    public bool isOvertakingBoost { get; private set; } = false;
    private bool isDesperateOvertake = false;
    private bool isYielding = false;

    private float overtakeTimer = 0f;
    private int targetSplineAfterOvertake = -1;
    private int originalSplineIndex = 0;

    // Трекинг соперников
    private PrometeoSplineAIController lastCarAhead = null;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        carController = GetComponent<PrometeoCarController>();
        rb = GetComponent<Rigidbody>();

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    private void Start()
    {
        ResetDecisionTimer();
        laneChangeCooldownTimer = UnityEngine.Random.Range(0.1f, minLaneChangeCooldown);
    }

    private void Update()
    {
        if (splineContainer == null || splineContainer.Splines == null) return;

        // Отсчет кулдауна перестройки
        if (laneChangeCooldownTimer > 0f)
        {
            laneChangeCooldownTimer -= Time.deltaTime;
        }

        // --- 1. Таймер решений AI ---
        decisionTimer += Time.deltaTime;
        if (decisionTimer >= currentDecisionInterval)
        {
            ResetDecisionTimer();
            EvaluateAIBehaviors();
        }

        // --- 2. Обработка процесса обгона ---
        if (isOvertakingBoost)
        {
            overtakeTimer -= Time.deltaTime;
            if (overtakeTimer <= 0f)
            {
                if (targetSplineAfterOvertake >= 0 && targetSplineAfterOvertake < splineContainer.Splines.Count)
                {
                    splineIndex = targetSplineAfterOvertake;
                }
                isOvertakingBoost = false;
                isDesperateOvertake = false;
                SetRandomLaneChangeCooldown();
            }
        }

        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            return;
        }

        agent.nextPosition = transform.position;

        // --- 3. Расчет целевой точки движения ---
        Spline currentSpline = splineContainer.Splines[splineIndex];
        float splineLength = currentSpline.GetLength();
        if (splineLength <= 0f) return;

        SplineUtility.GetNearestPoint(
            currentSpline,
            splineContainer.transform.InverseTransformPoint(transform.position),
            out _,
            out float normalizedTime
        );

        float currentSpeed = Mathf.Abs(carController.carSpeed);
        float dynamicLookAhead = baseLookAhead + (currentSpeed * speedLookAheadFactor);

        float targetDistance = ((normalizedTime * splineLength) + dynamicLookAhead) % splineLength;
        float targetNormalizedTime = targetDistance / splineLength;

        float3 localTargetPos = SplineUtility.EvaluatePosition(currentSpline, targetNormalizedTime);
        Vector3 worldTargetPos = splineContainer.transform.TransformPoint((Vector3)localTargetPos);

        agent.SetDestination(worldTargetPos);

        float distanceToSpline = Vector3.Distance(transform.position, worldTargetPos);
        Vector3 finalSteerTarget = (distanceToSpline > 12f) ? agent.steeringTarget : worldTargetPos;

        // --- 4. Управление скоростью и руление ---
        ApplyPredictiveSteering(finalSteerTarget);

        float activeSpeedLimit = GetAllowedSpeedForPosition();

        // Динамическое изменение скорости:
        if (isOvertakingBoost)
        {
            // Обгон — газуем на максимуме!
            activeSpeedLimit *= aggressiveSpeedMultiplier;
        }
        else if (isYielding)
        {
            // Пропускаем новый транспорт / даем обгнать — сбрасываем газ
            activeSpeedLimit *= yieldSpeedMultiplier;
        }

        ApplySpeedControl(currentSpeed, activeSpeedLimit);
    }

    private void FixedUpdate()
    {
        float currentSpeed = Mathf.Abs(carController.carSpeed);
        float allowedSpeed = GetAllowedSpeedForPosition();
        if (isOvertakingBoost) allowedSpeed *= aggressiveSpeedMultiplier;

        if (currentSpeed > allowedSpeed + 3f && rb != null)
        {
            rb.AddForce(-rb.velocity.normalized * physicalBrakePower, ForceMode.Acceleration);
        }
    }

    private void EvaluateAIBehaviors()
    {
        PrometeoSplineAIController currentCarAhead = GetCarAheadOnLane();

        // Проверяем, нужно ли уступить дорогу прямо сейчас
        isYielding = CheckIfShouldYield(currentCarAhead);

        // --- СЦЕНАРИЙ 1: Транспорт прямо впереди на нашей линии ---
        if (currentCarAhead != null)
        {
            // А. Тот же самый соперник блокирует путь ("Вырвался вперед тот же")
            if (currentCarAhead == lastCarAhead)
            {
                // Отчаянный обгон до конца: ищем линию, даем жесткий газ и не обращаем внимания на толпу сбоку
                TryStartOvertake(isDesperate: true);
            }
            // Б. Встал НОВЫЙ транспорт на нашу линию ("Вырвался другой")
            else
            {
                // Сначала срабатывает isYielding (притормаживаем), но если есть куда уйти — планируем обгон
                if (laneChangeCooldownTimer <= 0f)
                {
                    TryStartOvertake(isDesperate: false);
                }
            }
        }
        // --- СЦЕНАРИЙ 2: Линия свободна (Рандомные перестройки) ---
        else
        {
            if (!isOvertakingBoost && laneChangeCooldownTimer <= 0f && allowedLaneChangeSplines.Contains(splineIndex))
            {
                float roll = UnityEngine.Random.Range(0f, 100f);
                if (roll <= laneChangeProbability)
                {
                    TryStartOvertake(isDesperate: false);
                }
            }
        }

        lastCarAhead = currentCarAhead;
    }

    private void TryStartOvertake(bool isDesperate)
    {
        if (isOvertakingBoost) return;
        if (!allowedLaneChangeSplines.Contains(splineIndex)) return;

        int targetLane = GetAdjacentLaneIndex();

        // Если нашли куда перестроиться
        if (targetLane != splineIndex)
        {
            isOvertakingBoost = true;
            isDesperateOvertake = isDesperate;
            originalSplineIndex = splineIndex;
            targetSplineAfterOvertake = targetLane;
            overtakeTimer = 3.0f; // Время перестроения на новую линию
        }
    }

    // Проверка, стоит ли притормозить и пропустить
    private bool CheckIfShouldYield(PrometeoSplineAIController currentCarAhead)
    {
        // 1. Встал НОВЫЙ транспорт прямо перед капотом
        if (currentCarAhead != null && currentCarAhead != lastCarAhead)
        {
            return true;
        }

        // 2. Кто-то рядом отчаянно нас обгоняет и пытается вклиниться
        PrometeoSplineAIController[] allCars = FindObjectsByType<PrometeoSplineAIController>(FindObjectsSortMode.None);
        foreach (var car in allCars)
        {
            if (car == this) continue;

            if (car.isOvertakingBoost)
            {
                Vector3 toCar = car.transform.position - transform.position;
                float distance = toCar.magnitude;

                // Если машина чуть впереди нас и на близкой дистанции
                if (distance <= yieldCheckDistance && Vector3.Dot(transform.forward, toCar.normalized) > 0.2f)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private PrometeoSplineAIController GetCarAheadOnLane()
    {
        PrometeoSplineAIController[] allCars = FindObjectsByType<PrometeoSplineAIController>(FindObjectsSortMode.None);
        PrometeoSplineAIController closestCar = null;
        float minDistance = frontDetectionRange;

        foreach (var car in allCars)
        {
            if (car == this) continue;

            Vector3 dirToCar = car.transform.position - transform.position;
            float distance = dirToCar.magnitude;

            if (distance <= frontDetectionRange)
            {
                // Проверяем сектор спереди
                float dot = Vector3.Dot(transform.forward, dirToCar.normalized);
                if (dot > 0.5f && distance < minDistance)
                {
                    minDistance = distance;
                    closestCar = car;
                }
            }
        }

        return closestCar;
    }

    private int GetAdjacentLaneIndex()
    {
        int totalSplines = splineContainer.Splines.Count;
        List<int> possibleLanes = new List<int>();

        int leftLane = splineIndex - 1;
        if (leftLane >= 0 && allowedLaneChangeSplines.Contains(leftLane))
        {
            possibleLanes.Add(leftLane);
        }

        int rightLane = splineIndex + 1;
        if (rightLane < totalSplines && allowedLaneChangeSplines.Contains(rightLane))
        {
            possibleLanes.Add(rightLane);
        }

        if (possibleLanes.Count > 0)
        {
            return possibleLanes[UnityEngine.Random.Range(0, possibleLanes.Count)];
        }

        return splineIndex;
    }

    private void ResetDecisionTimer()
    {
        decisionTimer = 0f;
        currentDecisionInterval = UnityEngine.Random.Range(minDecisionInterval, maxDecisionInterval);
    }

    private void SetRandomLaneChangeCooldown()
    {
        laneChangeCooldownTimer = UnityEngine.Random.Range(minLaneChangeCooldown, maxLaneChangeCooldown);
    }

    private void ApplyPredictiveSteering(Vector3 targetPosition)
    {
        Vector3 localTarget = transform.InverseTransformPoint(targetPosition);

        if (localTarget.z > -1f)
        {
            float angleToTarget = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
            float turnSpeed = rb.angularVelocity.y * Mathf.Rad2Deg;
            float predictedAngle = angleToTarget - (turnSpeed * predictionFactor);

            if (Mathf.Abs(predictedAngle) < steerDeadzone)
            {
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
        else
        {
            carController.ThrottleOff();
            carController.GoReverse();
            carController.TurnRight();
        }
    }

    private void ApplySpeedControl(float currentSpeed, float targetSpeedLimit)
    {
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