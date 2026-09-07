using UnityEngine;
using _Project.Scripts.VisualScripting;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 자동차와 부딪힌 정확한 물리 충돌 지점(Contact Point)에 팝업 연출을 출력하는 컴포넌트
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PropHitPopupTrigger : MonoBehaviour
    {
        [Header("Tag Settings")]
        [Tooltip("충돌을 감지할 자동차 태그 (예: PlayerBumper, Player, Bumper)")]
        [SerializeField] private string targetTag = "PlayerBumper";

        [Header("Output Reference")]
        [Tooltip("실행할 ComicHitPopupBossOutput 노드를 드래그 앤 드롭 하세요")]
        [SerializeField] private ComicHitPopupBossOutput hitPopupOutput;

        [Header("Trigger Options")]
        [Tooltip("한 번 부딪혔을 때 1회만 출력할지 여부")]
        [SerializeField] private bool triggerOnce = true;

        [Tooltip("연속 충돌 방지를 위한 쿨타임 (초)")]
        [SerializeField] private float cooldownTime = 0.5f;

        private bool _hasTriggered = false;
        private float _lastTriggerTime = 0f;

        private void OnCollisionEnter(Collision collision)
        {
            // 쿨타임 및 1회 발동 조건 체크
            if (_hasTriggered && triggerOnce) return;
            if (Time.time - _lastTriggerTime < cooldownTime) return;

            // 자동차 태그 검사
            if (collision.gameObject.CompareTag(targetTag) ||
                collision.gameObject.CompareTag("Player") ||
                collision.gameObject.CompareTag("Bumper"))
            {
                if (hitPopupOutput == null)
                {
                    hitPopupOutput = GetComponent<ComicHitPopupBossOutput>() ?? GetComponentInChildren<ComicHitPopupBossOutput>();
                }

                if (hitPopupOutput == null)
                {
                    Debug.LogWarning($"[{gameObject.name}] PropHitPopupTrigger: 연결된 ComicHitPopupBossOutput이 없습니다.");
                    return;
                }

                _lastTriggerTime = Time.time;
                _hasTriggered = true;

                // 1. 자동차와 프랍이 실제 부딪힌 3D 접촉 지점 좌표 가져오기
                Vector3 hitPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : collision.transform.position;

                // 2. 부딪힌 지점에 임시 타겟 오브젝트 생성
                GameObject hitDummy = new GameObject($"HitPointDummy_{gameObject.name}");
                hitDummy.transform.position = hitPoint;

                // 3. Output에 충돌 지점 타겟 전달 후 실행
                hitPopupOutput.SetTargetTransform(hitDummy.transform);
                hitPopupOutput.Execute();

                // 4. UI 연출 종료 후 임시 오브젝트 자동 파괴 (2초 후)
                Destroy(hitDummy, 2.0f);
            }
        }

        /// <summary>
        /// 프랍이 재사용될 때 트리거 상태를 초기화합니다.
        /// </summary>
        public void ResetTrigger()
        {
            _hasTriggered = false;
        }
    }
}