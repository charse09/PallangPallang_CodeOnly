using UnityEngine;

public class WorldSpaceGizmoDebugger : MonoBehaviour
{
    [Header("Gizmo Settings")]
    [SerializeField] private float radius = 1.0f; // Gizmo의 반지름
    [SerializeField] private Color gizmoColor = Color.cyan; // Gizmo의 색상
    [SerializeField] bool showOnlyWhenSelected = false; // 선택될 때만 보여줄지 여부

    [Header("Position (Optional)")]
    [SerializeField] private bool useManualPosition = false;
    [SerializeField] private Vector3 manualWorldPosition = Vector3.zero;

    // Gizmos는 에디터 상에서 시각적인 디버깅 가이드를 그려주는 함수입니다.
    private void OnDrawGizmos()
    {
        if (showOnlyWhenSelected) return;
        DrawDebugSphere();
    }

    // 오브젝트를 클릭(선택)했을 때만 그려주는 함수입니다.
    private void OnDrawGizmosSelected()
    {
        if (!showOnlyWhenSelected) return;
        DrawDebugSphere();
    }

    private void DrawDebugSphere()
    {
        // 1. Gizmos 색상 설정
        Gizmos.color = gizmoColor;

        // 2. 렌더링할 월드 위치 결정
        // 스크립트가 붙은 오브젝트의 위치를 쓸지, 수동으로 지정한 월드 좌표를 쓸지 결정
        Vector3 targetPosition = useManualPosition ? manualWorldPosition : transform.position;

        // 3. 와이어 구체 그리기 (World Space)
        // Gizmos.DrawWireSphere는 기본적으로 월드 좌표계를 기준으로 그립니다.
        Gizmos.DrawWireSphere(targetPosition, radius);
        
        // (선택 사항) 중심점에 작은 점 하나를 추가하여 위치를 명확히 표시
        Gizmos.DrawSphere(targetPosition, radius * 0.05f);
    }
}