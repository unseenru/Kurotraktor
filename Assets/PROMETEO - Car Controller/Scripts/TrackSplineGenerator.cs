using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Splines;
using Unity.Mathematics;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(SplineContainer))]
public class TrackSplineGenerator : MonoBehaviour
{
    [Header("Track Keypoints")]
    [Tooltip("Ключевые точки вдоль трассы для направления движения")]
    [SerializeField] private Transform[] trackGuidePoints;

    [Header("Road Centering (Авто-Центрирование)")]
    [Tooltip("Автоматически смещать точки сплайна строго на центр полотна дороги")]
    [SerializeField] private bool autoCenterOnRoad = true;

    [Tooltip("Максимальная ширина дороги в метрах (для поиска обочин)")]
    [SerializeField] private float maxRoadWidth = 25f;

    [Header("Spline Generation Settings")]
    [SerializeField] private bool loopTrack = true;
    [Tooltip("Минимальное расстояние между узлами сплайна в метрах (4-6м дает отличный результат)")]
    [SerializeField] private float pointMinDistance = 4f;
    [SerializeField] private float sampleRadius = 15f;

    [ContextMenu("Generate Spline From Track Mesh")]
    public void GenerateSpline()
    {
        if (trackGuidePoints == null || trackGuidePoints.Length < 2)
        {
            Debug.LogError("[SplineGen] Укажи минимум 2-3 ориентировочные точки в массив Track Guide Points!");
            return;
        }

        SplineContainer container = GetComponent<SplineContainer>();
        Spline spline = container.Spline;
        spline.Clear();

        List<Vector3> calculatedCorners = new List<Vector3>();
        int count = loopTrack ? trackGuidePoints.Length : trackGuidePoints.Length - 1;

        for (int i = 0; i < count; i++)
        {
            Transform startTransform = trackGuidePoints[i];
            Transform endTransform = trackGuidePoints[(i + 1) % trackGuidePoints.Length];

            if (startTransform == null || endTransform == null) continue;

            if (!NavMesh.SamplePosition(startTransform.position, out NavMeshHit startHit, sampleRadius, NavMesh.AllAreas) ||
                !NavMesh.SamplePosition(endTransform.position, out NavMeshHit endHit, sampleRadius, NavMesh.AllAreas))
            {
                Debug.LogError($"[SplineGen] Точка '{startTransform.name}' или '{endTransform.name}' слишком далеко от NavMesh!");
                return;
            }

            NavMeshPath path = new NavMeshPath();
            if (NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path))
            {
                for (int j = 0; j < path.corners.Length; j++)
                {
                    Vector3 cornerPos = path.corners[j];

                    // АЛГОРИТМ АВТО-ЦЕНТРИРОВАНИЯ ПО ЦЕНТРУ ДОРОГИ
                    if (autoCenterOnRoad)
                    {
                        Vector3 direction = Vector3.forward;
                        if (j < path.corners.Length - 1)
                            direction = (path.corners[j + 1] - cornerPos).normalized;
                        else if (j > 0)
                            direction = (cornerPos - path.corners[j - 1]).normalized;

                        cornerPos = GetRoadCenterPoint(cornerPos, direction, maxRoadWidth);
                    }

                    // Пропускаем точки, если они стоят слишком близко
                    if (calculatedCorners.Count == 0 || Vector3.Distance(calculatedCorners[calculatedCorners.Count - 1], cornerPos) >= pointMinDistance)
                    {
                        calculatedCorners.Add(cornerPos);
                    }
                }
            }
        }

        if (calculatedCorners.Count == 0)
        {
            Debug.LogError("[SplineGen] Не удалось сгенерировать ни одной точки!");
            return;
        }

        // ЗАПИСЬ В СПЛАЙН И АВТО-СГЛАЖИВАНИЕ
        for (int i = 0; i < calculatedCorners.Count; i++)
        {
            Vector3 localPos = container.transform.InverseTransformPoint(calculatedCorners[i]);
            BezierKnot knot = new BezierKnot(new float3(localPos.x, localPos.y, localPos.z));
            spline.Add(knot, TangentMode.AutoSmooth);
        }

        spline.Closed = loopTrack;

#if UNITY_EDITOR
        EditorUtility.SetDirty(container);
        Debug.Log($"[SplineGen] Успех! Сгенерирована отцентрованная траектория. Узлов: {calculatedCorners.Count}");
#endif
    }

    // Метод ищет левый и правый край NavMesh поперек направления дороги и возвращает ровно середину
    private Vector3 GetRoadCenterPoint(Vector3 point, Vector3 direction, float searchWidth)
    {
        Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
        if (right == Vector3.zero) return point;

        Vector3 leftBound = point;
        Vector3 rightBound = point;

        // Выполняем трассировку NavMesh влево и вправо до краев запеченной сетки
        if (NavMesh.Raycast(point, point - right * searchWidth, out NavMeshHit leftHit, NavMesh.AllAreas))
        {
            leftBound = leftHit.position;
        }

        if (NavMesh.Raycast(point, point + right * searchWidth, out NavMeshHit rightHit, NavMesh.AllAreas))
        {
            rightBound = rightHit.position;
        }

        // Середина между левым и правым бордюром
        return (leftBound + rightBound) * 0.5f;
    }
}