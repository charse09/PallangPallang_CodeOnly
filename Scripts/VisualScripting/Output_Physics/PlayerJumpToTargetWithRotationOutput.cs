using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public enum JumpLookTargetMode
    {
        [Tooltip("점프 중에도 회전하지 않고 시작 전 바라보던 방향을 그대로 유지합니다.")]
        KeepOriginalRotation,

        [Tooltip("착지할 목표 지점(Target Point)을 수평(Y축)으로 바라봅니다.")]
        LookAtTargetPoint,

        [Tooltip("착지 지점과 별개로 지정한 특정 오브젝트를 바라봅니다.")]
        LookAtSpecificTransform,

        [Tooltip("지정한 특정 월드 방향을 바라봅니다.")]
        CustomDirection
    }

    public enum LandingRotationMode
    {
        [Tooltip("점프 중 바라본 방향을 착지 후에도 계속 유지합니다.")]
        KeepLookRotation,

        [Tooltip("공중에서 목표를 바라보며 날아가더라도 착지 시 원래 방향으로 복귀합니다.")]
        RestoreOriginalRotation
    }

    /// <summary>
    /// 플레이어를 목표 위치로 포물선 점프시키며,
    /// 오직 Y축(수평)만 회전하여 시선을 맞추거나 착지 시 원래 방향으로 복원하는 Output 노드입니다.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Output/Player Jump To Target With Rotation Output")]
    public class PlayerJumpToTargetWithRotationOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("점프시킬 플레이어 (비워둘 경우 자동으로 검색)")]
        [SerializeField] private PlayerLocomotion playerLocomotion;

        [Tooltip("플레이어가 착지할 목표 Transform")]
        [SerializeField] private Transform targetPoint;

        [Header("Rotation Settings (Y-Axis Only)")]
        [Tooltip("점프 공중에서 바라볼 방향 모드")]
        [SerializeField] private JumpLookTargetMode lookMode = JumpLookTargetMode.LookAtTargetPoint;

        [Tooltip("LookMode가 LookAtSpecificTransform일 때 바라볼 대상")]
        [SerializeField] private Transform lookTargetTransform;

        [Tooltip("LookMode가 CustomDirection일 때 바라볼 방향 벡터")]
        [SerializeField] private Vector3 customLookDirection = Vector3.forward;

        [Tooltip("공중 회전 완료 비율 (0.0: 점프 즉시 회전 ~ 1.0: 체공 시간 전체에 걸쳐 회전)")]
        [Range(0f, 1f)]
        [SerializeField] private float rotationTimeRatio = 0.4f;

        [Header("Landing Rotation Settings")]
        [Tooltip("착지 시점의 회전 처리 방식")]
        [SerializeField] private LandingRotationMode landingRotationMode = LandingRotationMode.RestoreOriginalRotation;

        [Tooltip("원래 각도로 부드럽게 복귀하는 시간 (초 단위, 0이면 착지 즉시 복귀)")]
        [SerializeField] private float restoreDuration = 0.15f;

        [Header("Jump Trajectory Settings")]
        [Tooltip("점프 최고 높이 (포물선 정점 추가 높이)")]
        [SerializeField] private float jumpHeight = 3.0f;

        [Tooltip("점프에 걸리는 시간 (초)")]
        [SerializeField] private float jumpDuration = 1.0f;

        [Header("Landing Offset")]
        [Tooltip("착지 시 바닥 끼임을 방지하기 위한 Y축 여유 높이")]
        [SerializeField] private float landingHeightOffset = 0.05f;

        private static bool isAnyJumping = false;
        private static Rigidbody activePlayerRb = null;
        private static PlayerLocomotion activePlayerLocomotion = null;

        public override void Execute()
        {
            if (isAnyJumping)
            {
                IsOn = true;
                return;
            }

            IsOn = false;

            if (targetPoint == null)
            {
                Debug.LogError($"[{gameObject.name}] Target Point가 비어있습니다.");
                IsOn = true;
                return;
            }

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

            // 1. 점프 시작 전 원래 방향 및 위치 저장
            Quaternion originalRotation = player.transform.rotation;
            Vector3 startPos = player.transform.position;
            Vector3 endPos = targetPoint.position + (Vector3.up * landingHeightOffset);

            // 2. 물리 및 플레이어 조작 일시 정지
            player.ResetAllVelocities();
            player.enabled = false;

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            // 3. 점프 중 바라볼 Y축(수평) 전용 목표 각도 계산
            Quaternion airTargetRotation = originalRotation;

            if (lookMode != JumpLookTargetMode.KeepOriginalRotation)
            {
                Vector3 lookDir = GetHorizontalLookDirection(startPos, endPos);
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    airTargetRotation = Quaternion.LookRotation(lookDir, Vector3.up);
                }
            }

            float timer = 0f;
            float rotDuration = Mathf.Max(0.01f, jumpDuration * rotationTimeRatio);

            try
            {
                // 4. 점프 이동 및 공중 Y축 회전
                while (timer < jumpDuration)
                {
                    timer += Time.deltaTime;
                    float t = Mathf.Clamp01(timer / jumpDuration);

                    // 포물선 Y 계산
                    float heightOffset = 4f * jumpHeight * t * (1f - t);
                    Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
                    currentPos.y += heightOffset;
                    player.transform.position = currentPos;

                    // 공중 회전 보간
                    if (lookMode != JumpLookTargetMode.KeepOriginalRotation)
                    {
                        if (rotationTimeRatio <= 0.01f)
                        {
                            player.transform.rotation = airTargetRotation;
                        }
                        else
                        {
                            float rotT = Mathf.Clamp01(timer / rotDuration);
                            player.transform.rotation = Quaternion.Slerp(originalRotation, airTargetRotation, rotT);
                        }
                    }

                    yield return null;
                }

                // 착지 위치 고정
                player.transform.position = endPos;

                // 5. 착지 후 회전 복원 처리
                Quaternion finalRotation = airTargetRotation;

                if (landingRotationMode == LandingRotationMode.RestoreOriginalRotation && lookMode != JumpLookTargetMode.KeepOriginalRotation)
                {
                    if (restoreDuration > 0f)
                    {
                        Quaternion currentAirRot = player.transform.rotation;
                        float restoreTimer = 0f;

                        while (restoreTimer < restoreDuration)
                        {
                            restoreTimer += Time.deltaTime;
                            float restT = Mathf.Clamp01(restoreTimer / restoreDuration);
                            player.transform.rotation = Quaternion.Slerp(currentAirRot, originalRotation, restT);
                            yield return null;
                        }
                    }

                    player.transform.rotation = originalRotation;
                    finalRotation = originalRotation;
                }
                else
                {
                    player.transform.rotation = finalRotation;
                }

                // 6. PlayerLocomotion 내부 기준 각도 동기화 (착지 직후 시선 튐 방지)
                Vector3 finalForward = finalRotation * Vector3.forward;
                finalForward.y = 0f;
                if (finalForward.sqrMagnitude > 0.001f)
                {
                    player.SetBaseRotation(finalForward.normalized);
                }
            }
            finally
            {
                // 7. 물리 및 제어권 복구
                if (rb != null)
                {
                    if (!player.isRidingRail)
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
            }
        }

        private Vector3 GetHorizontalLookDirection(Vector3 currentPos, Vector3 destinationPos)
        {
            Vector3 dir = Vector3.forward;

            switch (lookMode)
            {
                case JumpLookTargetMode.LookAtTargetPoint:
                    dir = destinationPos - currentPos;
                    break;

                case JumpLookTargetMode.LookAtSpecificTransform:
                    dir = (lookTargetTransform != null) ? (lookTargetTransform.position - currentPos) : (destinationPos - currentPos);
                    break;

                case JumpLookTargetMode.CustomDirection:
                    dir = customLookDirection;
                    break;
            }

            // Y 성분을 강제로 0으로 만들어 고개가 위아래로 꺾이는 현상 차단
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
            }
            else
            {
                dir = transform.forward;
                dir.y = 0f;
            }

            return dir;
        }

        private void OnDisable()
        {
            try
            {
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
            }
            finally
            {
                isAnyJumping = false;
            }
        }
    }
}