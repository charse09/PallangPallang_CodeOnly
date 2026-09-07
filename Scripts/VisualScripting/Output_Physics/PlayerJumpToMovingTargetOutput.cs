using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 플레이어가 여러 후보지 중 가장 가까운 움직이는 타겟(범퍼카 위 등)으로 포물선 점프하여 착지하는 비주얼 스크립팅 Output 컴포넌트입니다.
    /// </summary>
    public class PlayerJumpToMovingTargetOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("착지할 가능성이 있는 후보 위치(Transform)들의 리스트입니다.")]
        [SerializeField] private Transform[] landingPositions;

        [Tooltip("현재 플레이어 위치와 너무 가까운 타겟은 이미 서 있는 곳으로 판단하여 후보에서 제외할 거리 기준입니다.")]
        [SerializeField] private float excludeDistanceThreshold = 1.5f;

        [Header("Jump Settings")]
        [Tooltip("점프 최고 높이(포물선 최대 y 오프셋)입니다.")]
        [SerializeField] private float jumpHeight = 3.0f;

        [Tooltip("점프에 소요되는 시간(초)입니다.")]
        [SerializeField] private float jumpDuration = 1.0f;

        public override void Execute()
        {
            IsOn = false;

            if (landingPositions == null || landingPositions.Length == 0)
            {
                Debug.LogError($"[{gameObject.name}] Landing Positions 리스트가 비어 있습니다!");
                IsOn = true;
                return;
            }

            // 씬에서 플레이어 컴포넌트 찾기
            var player = Object.FindAnyObjectByType<PlayerLocomotion>();
            if (player != null)
            {
                // 플레이어와 가장 가까운 착지 후보 위치 선정
                Transform closestTarget = GetClosestLandingPosition(player.transform.position);
                if (closestTarget != null)
                {
                    // 점프 도중 이 스크립트가 붙은 오브젝트가 비활성화되더라도 끝까지 실행될 수 있도록 플레이어 오브젝트 상에서 코루틴을 돌립니다.
                    player.StartCoroutine(JumpRoutine(player, closestTarget));
                }
                else
                {
                    Debug.LogError($"[{gameObject.name}] 유효한 착지 타겟을 선정할 수 없습니다.");
                    IsOn = true;
                }
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] PlayerLocomotion 컴포넌트를 찾을 수 없습니다.");
                IsOn = true;
            }
        }

        private Transform GetClosestLandingPosition(Vector3 playerPos)
        {
            Transform closest = null;
            float minDistance = float.MaxValue;

            foreach (var target in landingPositions)
            {
                if (target == null) continue;

                float distance = Vector3.Distance(playerPos, target.position);
                
                // 이미 서 있는 타겟(너무 가까운 타겟)은 제외
                if (distance < excludeDistanceThreshold) continue;

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = target;
                }
            }

            return closest;
        }

        private IEnumerator JumpRoutine(PlayerLocomotion player, Transform targetPoint)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();

            // 점프 도중 물리 엔진 영향 비활성화
            bool wasKinematic = rb.isKinematic;
            rb.isKinematic = true;

            Vector3 startPos = player.transform.position;
            float timer = 0f;

            while (timer < jumpDuration)
            {
                timer += Time.deltaTime;

                // 0.0 ~ 1.0 비율 계산
                float t = Mathf.Clamp01(timer / jumpDuration);

                // 포물선 궤적 높이 오프셋
                float heightOffset = 4f * jumpHeight * t * (1f - t);

                // 타겟이 움직이므로 매 프레임 타겟의 최신 위치를 받아와 보간 계산
                Vector3 currentTargetPos = targetPoint.position;
                Vector3 currentPos = Vector3.Lerp(startPos, currentTargetPos, t);
                currentPos.y += heightOffset;

                // 플레이어 위치 갱신
                player.transform.position = currentPos;

                yield return null;
            }

            // 정확한 착지 위치 탐색 (타겟 포인트가 약간 공중에 떠있을 경우를 대비해 아래로 Raycast)
            Vector3 finalLandingPos = targetPoint.position;
            Rigidbody targetRb = targetPoint.GetComponentInParent<Rigidbody>();

            if (Physics.Raycast(targetPoint.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2.0f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                finalLandingPos = hit.point;
                if (hit.collider.attachedRigidbody != null)
                {
                    targetRb = hit.collider.attachedRigidbody;
                }
            }

            // 플레이어가 타겟에 파묻혀서 물리 엔진의 반발력으로 인해 튕겨나가는 것을 방지하기 위해 약간 위로 띄웁니다.
            player.transform.position = finalLandingPos + (Vector3.up * 0.1f);

            // 물리 엔진 원래대로 복구
            rb.isKinematic = wasKinematic;
            
            // 착지 즉시 움직이는 플랫폼(범퍼카)으로 등록하여 미끄러짐 방지 및 동기화 활성화
            if (targetRb != null)
            {
                // 타겟의 속도를 즉시 플레이어에게 적용하여, 플랫폼이 이동하면서 플레이어를 치고 지나가는 현상(밀려남)을 방지
                rb.linearVelocity = targetRb.GetPointVelocity(player.transform.position);
                player.SetPlatform(targetRb);
            }
            else
            {
                rb.linearVelocity = Vector3.zero;
            }

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> 플레이어가 움직이는 타겟 '{targetPoint.name}' 위로 점프하여 착지 완료했습니다.");
#endif

            IsOn = true;
        }
    }
}
