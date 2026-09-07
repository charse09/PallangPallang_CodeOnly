using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PaperMoveTrigger : MonoBehaviour
{
    [Header("Paper Move Settings")]
    [Tooltip("이 구역에서의 페이퍼 상태 이동 속도")]
    [SerializeField] private float zoneMoveSpeed = 30f;
    [Tooltip("좌우로 비비적거리는 속도 (주파수)")]
    [SerializeField] private float wiggleFrequency = 15f;
    [Tooltip("좌우로 비비적거리는 폭 (진폭)")]
    [SerializeField] private float wiggleAmplitude = 1.5f;

    [Header("Gizmo Settings")]
    [Tooltip("트리거 영역 내부의 반투명한 색상입니다.")]
    [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);
    [Tooltip("트리거 영역 테두리 선의 색상입니다.")]
    [SerializeField] private Color gizmoWireColor = new Color(0f, 1f, 0f, 1f);

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 프리팹 구조상 스크립트와 콜라이더가 같은 곳에 있으므로 GetComponent로 족합니다.
            PlayerLocomotion locomotion = other.GetComponent<PlayerLocomotion>();
            if (locomotion != null)
            {
                locomotion.canMoveInPaperZone = true;
                locomotion.currentPaperZoneSpeed = zoneMoveSpeed;
                locomotion.currentWiggleFrequency = wiggleFrequency;
                locomotion.currentWiggleAmplitude = wiggleAmplitude;
            }
        }
    }

    // [핵심 해결책] 3개의 콜라이더 중 하나가 빠져나가서 false가 되더라도, 
    // 트리거 안에 남은 다른 콜라이더가 즉시 상태를 true로 살려냅니다.
    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerLocomotion locomotion = other.GetComponent<PlayerLocomotion>();

            // 혹시라도 버그로 인해 false가 되어 있다면 강제로 다시 true로 만듭니다.
            if (locomotion != null && !locomotion.canMoveInPaperZone)
            {
                locomotion.canMoveInPaperZone = true;
                locomotion.currentPaperZoneSpeed = zoneMoveSpeed;
                locomotion.currentWiggleFrequency = wiggleFrequency;
                locomotion.currentWiggleAmplitude = wiggleAmplitude;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 페이퍼 변신 중 콜라이더가 비활성화(꺼짐)되어서 억울하게 발생한 가짜 Exit 이벤트는 무시합니다.
        if (!other.gameObject.activeInHierarchy || !other.enabled)
            return;

        if (other.CompareTag("Player"))
        {
            PlayerLocomotion locomotion = other.GetComponent<PlayerLocomotion>();
            if (locomotion != null)
            {
                locomotion.canMoveInPaperZone = false;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            BoxCollider box = col as BoxCollider;

            if (box != null)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawCube(box.center, box.size);

                Gizmos.color = gizmoWireColor;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else
            {
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.color = gizmoColor;
                Gizmos.DrawCube(col.bounds.center, col.bounds.size);

                Gizmos.color = gizmoWireColor;
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
    }
}