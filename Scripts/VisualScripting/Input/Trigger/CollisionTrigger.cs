using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// IsTrigger 체크 없이, 오브젝트끼리 물리적으로 부딪혔을 때 작동하는 트리거
    /// </summary>
    public class CollisionTrigger : ProcessBase
    {
        [Header("Collision Settings")]
        [Tooltip("충돌을 감지할 대상의 태그를 입력하세요 (예: Player, Enemy)")]
        [SerializeField] private string selectedTag = "Player";

        // 현재 충돌 중인 대상 (원한다면 다른 노드에서 가져다 쓸 수 있음)
        [SerializeField] private GameObject objTarget;

        public GameObject GetTarget() { return objTarget; }

        // --- 1. 물리적 실제 충돌 감지 (OnCollision) ---
        private void OnCollisionEnter(Collision collision)
        {
#if UNITY_EDITOR
            Debug.Log($"<color=white>[CollisionTrigger Debug]</color> <b>OnCollisionEnter</b> on '{gameObject.name}' with '{collision.gameObject.name}' (Tag: '{collision.gameObject.tag}', Collider Tag: '{collision.collider.tag}')");
#endif
            if (collision.collider.CompareTag(selectedTag) || (collision.gameObject != null && collision.gameObject.CompareTag(selectedTag)))
            {
                IsOn = true; // 다음 노드로 신호 전달
                objTarget = collision.gameObject;

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[CollisionTrigger]</color> 물리 충돌 감지 성공: {collision.gameObject.name}");
#endif
            }
        }

        private void OnCollisionExit(Collision collision)
        {
#if UNITY_EDITOR
            Debug.Log($"<color=white>[CollisionTrigger Debug]</color> <b>OnCollisionExit</b> on '{gameObject.name}' with '{collision.gameObject.name}'");
#endif
            if (collision.collider.CompareTag(selectedTag) || (collision.gameObject != null && collision.gameObject.CompareTag(selectedTag)))
            {
                IsOn = true; // 나갈 때도 다음 노드를 실행 (기존 Trigger 방식 유지)

#if UNITY_EDITOR
                Debug.Log($"<color=yellow>[CollisionTrigger]</color> 물리 충돌 해제: {collision.gameObject.name}");
#endif
                if (objTarget != null && collision.gameObject == objTarget)
                {
                    objTarget = null;
                }
            }
        }

        // --- 2. IsTrigger가 켜진 콜라이더 대응 (OnTrigger) ---
        private void OnTriggerEnter(Collider other)
        {
#if UNITY_EDITOR
            Debug.Log($"<color=white>[CollisionTrigger Debug]</color> <b>OnTriggerEnter</b> on '{gameObject.name}' with '{other.gameObject.name}' (Tag: '{other.gameObject.tag}')");
#endif
            if (other.CompareTag(selectedTag) || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(selectedTag)))
            {
                IsOn = true;
                objTarget = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[CollisionTrigger (Trigger)]</color> 트리거 진입 감지 성공: {objTarget.name}");
#endif
            }
        }

        private void OnTriggerExit(Collider other)
        {
#if UNITY_EDITOR
            Debug.Log($"<color=white>[CollisionTrigger Debug]</color> <b>OnTriggerExit</b> on '{gameObject.name}' with '{other.gameObject.name}'");
#endif
            GameObject targetGO = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
            if (other.CompareTag(selectedTag) || (other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(selectedTag)))
            {
                IsOn = true;

#if UNITY_EDITOR
                Debug.Log($"<color=yellow>[CollisionTrigger (Trigger)]</color> 트리거 이탈: {targetGO.name}");
#endif
                if (objTarget != null && objTarget == targetGO)
                {
                    objTarget = null;
                }
            }
        }

        public override void Execute()
        {
            // ProcessBase의 실행 구조에 맞게 상태 토글 (기존 코드 유지)
            IsOn = !IsOn;
        }

        private void OnDrawGizmos()
        {
            // 씬 뷰에서 트리거 영역을 초록색으로 표시
            Gizmos.color = Color.green;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}