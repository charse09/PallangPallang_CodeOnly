using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 플레이어를 특정 타겟 위치(오브젝트)로 포물선 궤적으로 점프 이동시킵니다.
    /// 점프 중에는 물리 간섭과 중력 누적을 방지하기 위해 isKinematic 설정 및 PlayerLocomotion 연산을 일시 중단합니다.
    /// </summary>
    public class PlayerJumpToTargetOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("점프시킬 플레이어 (비워둘 경우 자동으로 FindFirstObjectByType 검색)")]
        [SerializeField] private PlayerLocomotion playerLocomotion;

        [Tooltip("플레이어가 착지할 목표 Transform (비워둘 경우 에러)")]
        [SerializeField] private Transform targetPoint;

        [Header("Jump Settings")]
        [Tooltip("점프 최고 높이 (시작점과 끝점 사이의 추가 높이)")]
        [SerializeField] private float jumpHeight = 3.0f;

        [Tooltip("점프에 걸리는 시간(초)")]
        [SerializeField] private float jumpDuration = 1.0f;

        [Header("Landing Settings")]
        [Tooltip("착지 시 바닥 콜라이더 끼임/튕김을 방지하기 위해 목표 위치보다 살짝 높게 배치할 오프셋(Y)")]
        [SerializeField] private float landingHeightOffset = 0.05f;

        // 전역 점프 중복 방지 플래그
        private static bool isAnyJumping = false;
        private static Rigidbody activePlayerRb = null;
        private static PlayerLocomotion activePlayerLocomotion = null;

        public override void Execute()
        {
            // 이미 다른 점프가 실행 중인 경우
            if (isAnyJumping)
            {
                IsOn = true;
                return;
            }

            IsOn = false;

            if (targetPoint == null)
            {
                Debug.LogError($"[{gameObject.name}] Target Point가 비어있습니다! 목표를 지정해주세요.");
                IsOn = true;
                return;
            }

            // 플레이어 검색 (인스펙터 등록 우선 -> 미등록 시 자동 검색)
            PlayerLocomotion player = playerLocomotion != null
                ? playerLocomotion
                : FindFirstObjectByType<PlayerLocomotion>();

            if (player != null)
            {
                StartCoroutine(JumpRoutine(player));
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] PlayerLocomotion을 찾을 수 없습니다.");
                IsOn = true;
            }
        }

        private IEnumerator JumpRoutine(PlayerLocomotion player)
        {
            isAnyJumping = true;
            activePlayerLocomotion = player;

            Rigidbody rb = player.GetComponent<Rigidbody>();
            activePlayerRb = rb;

            // 1. 플레이어 로코모션 연산 일시 정지 (공중에서 중력 가속도 누적 및 물리 충돌 방지)
            if (player != null)
            {
                player.ResetAllVelocities();
                player.enabled = false;
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true; // 물리 간섭 차단
            }

            Vector3 startPos = player.transform.position;
            Vector3 endPos = targetPoint.position + (Vector3.up * landingHeightOffset);

            float timer = 0f;

            try
            {
                while (timer < jumpDuration)
                {
                    timer += Time.deltaTime;

                    // 진행도 (0.0 ~ 1.0)
                    float t = Mathf.Clamp01(timer / jumpDuration);

                    // 포물선 높이: 양 끝은 0, 중앙(0.5)에서 최대 높이가 됩니다.
                    float heightOffset = 4f * jumpHeight * t * (1f - t);

                    // 시작점과 끝점을 보간하고, Y축에 포물선 높이를 더합니다.
                    Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
                    currentPos.y += heightOffset;

                    // 플레이어 위치 이동
                    player.transform.position = currentPos;

                    yield return null;
                }

                // [착지] 목표 위치 도달
                player.transform.position = endPos;
            }
            finally
            {
                // [종료] 점프가 완료되거나 중단될 때 100% 실행되는 복구 로직
                if (rb != null)
                {
                    if (player == null || !player.isRidingRail)
                    {
                        rb.isKinematic = false;
                    }
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                if (player != null)
                {
                    player.ResetAllVelocities();
                    player.enabled = true;
                    player.ResetAllVelocities();
                }

                isAnyJumping = false;
                activePlayerRb = null;
                activePlayerLocomotion = null;
                IsOn = true;

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[{gameObject.name}]</color> 점프 완료. 플레이어 물리/제어 복구 완료.");
#endif
            }
        }

        private void OnDisable()
        {
            try
            {
                // 활성화 중 씬이 전환되거나 삭제될 때 대비
                if (activePlayerLocomotion != null)
                {
                    if (!activePlayerLocomotion.isRidingRail)
                    {
                        activePlayerLocomotion.enabled = true;
                    }
                    activePlayerLocomotion.ResetAllVelocities();
                    activePlayerLocomotion = null;
                }

                if (activePlayerRb != null)
                {
                    activePlayerRb.isKinematic = false;
                    activePlayerRb.linearVelocity = Vector3.zero;
                    activePlayerRb.angularVelocity = Vector3.zero;
                    activePlayerRb = null;
                }
            }
            catch (System.Exception)
            {
                // 씬 재시작 시 객체가 이미 파괴되었을 수 있으므로 무시
            }
            finally
            {
                isAnyJumping = false;
            }
        }
    }
}