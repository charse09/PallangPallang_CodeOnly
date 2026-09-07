using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 주변의 Interactable 물체를 감지하여 플레이어 머리 위나 주변에 띄우고 실시간으로 복사해 들고 다니게 만드는 Output 모듈
    /// </summary>
    public class TelekinesisCatchOutput : ProcessBase
    {
        [Header("Detection Settings")]
        [Tooltip("염력으로 물체를 탐색할 주변 반경 범위")]
        [SerializeField] private float scanRadius = 5f;

        [Tooltip("끌어당길 대상 오브젝트의 태그")]
        [SerializeField] private string targetTag = "Interactable";

        [Header("Hold Settings")]
        [Tooltip("플레이어 위치를 기준으로 얼마나 높이 띄울 것인가 (Y축 오프셋)")]
        [SerializeField] private float holdHeight = 2.5f;

        [Tooltip("물체가 플레이어를 따라오는 속도 가중치 (높을수록 찰딱 달라붙음)")]
        [SerializeField] private float followSharpness = 10f;

        public override void Execute()
        {
            IsOn = false;

            // 이미 무언가 들고 있다면 중복 실행 방지
            if (TelekinesisSharedData.IsHolding)
            {
                IsOn = true;
                return;
            }

            // 1. 플레이어 중심 좌표 확보
            var locomotion = FindFirstObjectByType<PlayerLocomotion>();
            if (locomotion == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 플레이어(PlayerLocomotion)를 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            Transform playerTransform = locomotion.transform;

            // 2. 주변 오버랩 저항을 통해 targetTag를 가진 물체 탐색
            Collider[] hitColliders = Physics.OverlapSphere(playerTransform.position, scanRadius);
            Transform closestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (var col in hitColliders)
            {
                if (col.CompareTag(targetTag))
                {
                    float dist = Vector3.Distance(playerTransform.position, col.transform.position);
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        closestTarget = col.transform;
                    }
                }
            }

            // 3. 물체를 찾았다면 염력 상태 돌입
            if (closestTarget != null)
            {
                // 공유 데이터에 정보 기록
                TelekinesisSharedData.TargetObject = closestTarget;
                TelekinesisSharedData.IsHolding = true;

                // 물리 간섭 차단 처리 (플레이어와 부딪혀서 튀는 버그 방지)
                Rigidbody rb = closestTarget.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }

                // 실시간 추적 코루틴 가동 (주체는 전역 매니저 역할을 하는 이 노드 혹은 별도 루틴)
                // 씬에 상주하는 컴포넌트 특성을 이용해 코루틴을 돌립니다.
                StartCoroutine(FollowPlayerRoutine(playerTransform, closestTarget));

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[{gameObject.name}]</color> 염력 발동! 잡힌 물체: {closestTarget.name}");
#endif
            }

            IsOn = true;
        }

        private IEnumerator FollowPlayerRoutine(Transform player, Transform target)
        {
            // 공유 데이터가 켜져 있고, 물체가 살아있는 동안 무한 추적
            while (TelekinesisSharedData.IsHolding && target != null && player != null)
            {
                // 플레이어의 머리 위 목표 좌표 계산
                Vector3 targetPosition = player.position + (Vector3.up * holdHeight);

                // 물체를 목표 좌표로 부드럽게 보간 이동 (Lerp)
                target.position = Vector3.Lerp(target.position, targetPosition, Time.deltaTime * followSharpness);

                // 물체가 회전하지 않고 똑바로 서있게 유지하고 싶다면 고정 (기획에 따라 삭제 가능)
                target.rotation = Quaternion.Lerp(target.rotation, Quaternion.identity, Time.deltaTime * followSharpness);

                yield return null;
            }
        }
    }
}