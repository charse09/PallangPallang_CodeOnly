using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

public class CircleSplineGenerator : MonoBehaviour
{
    [Header("Circle Settings")]
    [Tooltip("스플라인 오브젝트가 (0,0,0)일 때 원의 중심이 위치할 좌표")]
    [SerializeField] private Vector3 center = Vector3.zero;
    [SerializeField] private float radius = 10f;
    [SerializeField] private int nodeCount = 4;

    [ContextMenu("Generate Circle Spline")]
    public void GenerateCircle()
    {
        var container = GetComponent<SplineContainer>();
        if (container == null) container = gameObject.AddComponent<SplineContainer>();

        Spline spline = container.Spline;
        spline.Clear();
        spline.Closed = true;

        float handleLength = (4f / 3f) * Mathf.Tan(Mathf.PI / (2f * nodeCount)) * radius;

        for (int i = 0; i < nodeCount; i++)
        {
            float angle = i * (Mathf.PI * 2f / nodeCount);

            // 중심점(center) 오프셋을 더해 지정한 위치에 원 배치
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            Vector3 tangentOut = new Vector3(-Mathf.Sin(angle) * handleLength, 0f, Mathf.Cos(angle) * handleLength);
            Vector3 tangentIn = -tangentOut;

            BezierKnot knot = new BezierKnot(
                (float3)pos,
                (float3)tangentIn,
                (float3)tangentOut,
                quaternion.identity
            );

            spline.Add(knot);
        }
    }
}