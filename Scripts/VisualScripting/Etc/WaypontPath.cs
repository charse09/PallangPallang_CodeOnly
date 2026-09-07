using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [Header("Path Settings")]
    public Transform[] points; // 웨이포인트 지점들
    public bool loop = true;   // 순환 여부

    // 현재 인덱스를 입력받아 다음 웨이포인트를 반환하는 기능
    public Transform GetWaypoint(int index)
    {
        if (points == null || points.Length == 0) return null;
        return points[index % points.Length];
    }

    // 다음 인덱스 번호를 계산 (기존 AIController의 NextWaypoint 로직 이동)
    public int GetNextIndex(int currentIndex)
    {
        if (points == null || points.Length == 0) return 0;

        int nextIndex = currentIndex + 1;
        if (nextIndex >= points.Length)
        {
            return loop ? 0 : points.Length - 1;
        }
        return nextIndex;
    }

    // 에디터 시각화 (기존 기능을 확장하여 경로 선 추가)
    private void OnDrawGizmos()
    {
        if (points == null || points.Length < 2) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < points.Length; i++)
        {
            if (points[i] == null) continue;
            Gizmos.DrawSphere(points[i].position, 0.3f);

            // 다음 지점 연결 선 그리기
            int next = (i + 1) % points.Length;
            if (points[next] != null)
            {
                Gizmos.DrawLine(points[i].position, points[next].position);
            }
        }
    }
}