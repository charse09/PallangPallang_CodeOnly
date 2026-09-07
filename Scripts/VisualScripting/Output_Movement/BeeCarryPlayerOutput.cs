using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// Yenbeol(벌)이 플레이어를 들고 장애물을 피하며 목적지까지 날아가는 연출 Output.
    ///
    /// ──────────────────────────────────────────────────────
    /// [장애물 회피 방법]
    ///
    /// ① 웨이포인트 (Waypoints) — 권장
    ///    Inspector에서 waypoints 리스트에 빈 오브젝트를 추가.
    ///    Yenbeol이 waypoint → waypoint → destination 순서로 이동.
    ///    컷씬처럼 경로가 정해진 경우 가장 안정적.
    ///
    /// ② 자동 스티어링 (Auto Avoidance) — 보조
    ///    레이캐스트로 전방 장애물 감지 → 회피 방향으로 스티어링.
    ///    Enable Auto Avoidance 체크 시 활성화.
    ///    Obstacle Layer에 장애물 레이어를 지정해야 작동.
    ///
    /// 두 방법을 동시에 사용하면 웨이포인트로 큰 경로를 잡고
    /// 자동 스티어링으로 세밀한 장애물을 피할 수 있습니다.
    /// ──────────────────────────────────────────────────────
    /// </summary>
    public class BeeCarryPlayerOutput : ProcessBase
    {
        [Header("Bee Settings")]
        [Tooltip("이동할 Yenbeol(벌) 오브젝트의 Transform")]
        [SerializeField] private Transform yenbeolTransform;

        [Header("Player Settings")]
        [Tooltip("플레이어 루트 게임오브젝트")]
        [SerializeField] private GameObject playerObject;

        [Tooltip("비활성화할 플레이어 스크립트 목록 (PlayerLocomotion 등)")]
        [SerializeField] private List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();

        [Header("Path Settings")]
        [Tooltip("목적지까지의 경유점 목록.\n장애물 앞뒤에 빈 오브젝트를 배치해서 추가하세요.\n비어 있으면 목적지로 직선 이동.")]
        [SerializeField] private List<Transform> waypoints = new List<Transform>();

        [Tooltip("최종 목적지 Transform")]
        [SerializeField] private Transform destination;

        [Header("Flight Settings")]
        [Tooltip("Yenbeol이 날아가는 속도 (m/s)")]
        [SerializeField] private float flySpeed = 5f;

        [Tooltip("각 경유점/목적지 도착으로 판정하는 거리 (m)")]
        [SerializeField] private float arrivalThreshold = 0.15f;

        [Header("Auto Avoidance (Optional)")]
        [Tooltip("자동 장애물 감지 & 스티어링 활성화.\nObstacle Layer를 반드시 지정해야 작동합니다.")]
        [SerializeField] private bool enableAutoAvoidance = false;

        [Tooltip("장애물로 인식할 레이어 (벽, 오브젝트 등)")]
        [SerializeField] private LayerMask obstacleLayer;

        [Tooltip("장애물 감지 레이캐스트 앞 거리 (m)")]
        [SerializeField] private float avoidanceDetectDistance = 2.5f;

        [Tooltip("레이캐스트 탐지 반경 (SphereCast 반지름)")]
        [SerializeField] private float avoidanceSphereRadius = 0.5f;

        [Tooltip("장애물 회피 힘의 세기 (클수록 장애물에서 더 강하게 피함)")]
        [SerializeField] private float avoidanceStrength = 8f;

        // 내부 상태
        private Coroutine _carryCoroutine;
        private Rigidbody _playerRb;
        private bool _wasKinematic;

        // 디버그: 씬 뷰 스티어링 방향 시각화용
        private Vector3 _debugSteerDir;

        public override void Execute()
        {
            if (yenbeolTransform == null)
            {
                Debug.LogWarning($"[{gameObject.name}] yenbeolTransform이 비어 있습니다.");
                IsOn = true; return;
            }
            if (playerObject == null)
            {
                Debug.LogWarning($"[{gameObject.name}] playerObject가 비어 있습니다.");
                IsOn = true; return;
            }
            if (destination == null)
            {
                Debug.LogWarning($"[{gameObject.name}] destination이 비어 있습니다.");
                IsOn = true; return;
            }

            IsOn = false;
            if (_carryCoroutine != null) StopCoroutine(_carryCoroutine);
            _carryCoroutine = StartCoroutine(CarryRoutine());
        }

        private IEnumerator CarryRoutine()
        {
            Transform playerTransform = playerObject.transform;

            // ─────────────────────────────────────────────
            // STEP 1: 플레이어 물리 / 입력 완전 잠금
            // ─────────────────────────────────────────────
            foreach (var script in scriptsToDisable)
                if (script != null) script.enabled = false;

            if (InputManager.Instance != null)
                InputManager.Instance.enabled = false;

            _playerRb = playerObject.GetComponent<Rigidbody>();
            if (_playerRb != null)
            {
                _wasKinematic = _playerRb.isKinematic;
                _playerRb.linearVelocity = Vector3.zero;
                _playerRb.angularVelocity = Vector3.zero;
                _playerRb.isKinematic = true;
            }

            // Locomotion LateUpdate/FixedUpdate 완전히 멈추도록 2프레임 대기
            yield return null;
            yield return null;

            // ─────────────────────────────────────────────
            // STEP 2: grab 시점의 오프셋 기록
            // (HeadPoint는 Player 안에 있으므로 SetParent 불가 → 오프셋 추적 방식)
            // ─────────────────────────────────────────────
            Vector3 carryOffset = playerTransform.position - yenbeolTransform.position;

            // ─────────────────────────────────────────────
            // STEP 3: 경유점 → 목적지 순서로 이동
            // ─────────────────────────────────────────────

            // 방문할 지점 목록 구성 (waypoints + destination)
            List<Vector3> pathTargets = new List<Vector3>();
            foreach (var wp in waypoints)
                if (wp != null) pathTargets.Add(wp.position);
            pathTargets.Add(destination.position);

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[BeeCarryPlayerOutput]</color> 비행 시작. " +
                      $"경유점 {pathTargets.Count - 1}개 → 목적지: {destination.name}");
#endif

            foreach (Vector3 target in pathTargets)
            {
#if UNITY_EDITOR
                Debug.Log($"<color=orange>[BeeCarryPlayerOutput]</color> 다음 목표: {target}");
#endif
                // 각 목표 지점에 도달할 때까지 이동
                while (Vector3.Distance(yenbeolTransform.position, target) > arrivalThreshold)
                {
                    Vector3 steerDir = CalculateSteeringDirection(yenbeolTransform.position, target);

                    // 남은 거리와 이동량 비교 → 오버슈팅 방지
                    float distToTarget = Vector3.Distance(yenbeolTransform.position, target);
                    float stepSize = Mathf.Min(flySpeed * Time.deltaTime, distToTarget);

                    yenbeolTransform.position += steerDir * stepSize;

                    // 플레이어는 Yenbeol + 고정 오프셋으로 따라옴
                    playerTransform.position = yenbeolTransform.position + carryOffset;

                    // 이동 방향으로 Yenbeol 회전 (Y축만, 부드럽게)
                    Vector3 flatDir = new Vector3(steerDir.x, 0f, steerDir.z);
                    if (flatDir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(flatDir);
                        yenbeolTransform.rotation = Quaternion.Slerp(
                            yenbeolTransform.rotation, targetRot, Time.deltaTime * 5f);
                    }

                    yield return null;
                }

                // 목표 지점에 정확히 스냅
                yenbeolTransform.position = target;
                playerTransform.position = target + carryOffset;
            }

#if UNITY_EDITOR
            Debug.Log($"<color=green>[BeeCarryPlayerOutput]</color> 목적지 도착. 플레이어 복구 중...");
#endif

            // ─────────────────────────────────────────────
            // STEP 4: 플레이어 상태 복구
            // ─────────────────────────────────────────────
            yield return null;

            if (_playerRb != null)
            {
                _playerRb.isKinematic = _wasKinematic;
                _playerRb.linearVelocity = Vector3.zero;
                _playerRb.angularVelocity = Vector3.zero;
            }

            for (int i = scriptsToDisable.Count - 1; i >= 0; i--)
                if (scriptsToDisable[i] != null)
                    scriptsToDisable[i].enabled = true;

            if (InputManager.Instance != null)
                InputManager.Instance.enabled = true;

            _carryCoroutine = null;
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[BeeCarryPlayerOutput]</color> 시퀀스 완료. IsOn = true");
#endif
        }

        /// <summary>
        /// 목표 지점으로의 방향을 계산. Auto Avoidance가 켜져 있으면
        /// 전방 장애물 감지 스티어링을 추가로 혼합합니다.
        /// </summary>
        private Vector3 CalculateSteeringDirection(Vector3 currentPos, Vector3 target)
        {
            Vector3 toTarget = (target - currentPos).normalized;

            if (!enableAutoAvoidance)
            {
                _debugSteerDir = toTarget;
                return toTarget;
            }

            // ── 자동 스티어링: 전방 여러 방향 SphereCast ──
            // 전방 + 상하좌우 45도 방향으로 레이를 쏴서 가장 가까운 장애물에서 멀어지는 힘을 계산
            Vector3 avoidance = Vector3.zero;

            Vector3[] probeDirections = new Vector3[]
            {
                toTarget,                                             // 정면
                Quaternion.Euler(0,  45, 0) * toTarget,              // 오른쪽 45°
                Quaternion.Euler(0, -45, 0) * toTarget,              // 왼쪽 45°
                Quaternion.Euler( 30, 0, 0) * toTarget,              // 위 30°
                Quaternion.Euler(-30, 0, 0) * toTarget,              // 아래 30°
                Quaternion.Euler(0,  90, 0) * toTarget,              // 오른쪽 90° (측면)
                Quaternion.Euler(0, -90, 0) * toTarget,              // 왼쪽 90° (측면)
            };

            foreach (Vector3 probeDir in probeDirections)
            {
                if (Physics.SphereCast(
                        currentPos,
                        avoidanceSphereRadius,
                        probeDir,
                        out RaycastHit hit,
                        avoidanceDetectDistance,
                        obstacleLayer))
                {
                    // 장애물에서 멀어지는 방향으로 힘 추가
                    // 가까울수록 힘이 강해짐 (선형 감쇠)
                    Vector3 pushDir = (currentPos - hit.point).normalized;
                    float normalizedDist = 1f - (hit.distance / avoidanceDetectDistance);
                    avoidance += pushDir * (normalizedDist * avoidanceStrength);
                }
            }

            // 목표 방향 + 회피 힘을 합산해서 최종 스티어링 방향 계산
            Vector3 steer = (toTarget * flySpeed + avoidance).normalized;
            _debugSteerDir = steer;
            return steer;
        }

        public new void Reset()
        {
            base.Reset();
            if (_carryCoroutine != null)
            {
                StopCoroutine(_carryCoroutine);
                _carryCoroutine = null;
            }
        }

        // ─────────────────────────────────────────────
        // Gizmos: 씬 뷰에서 경로 시각화
        // ─────────────────────────────────────────────
        private void OnDrawGizmos()
        {
            if (destination == null) return;

            Vector3 prev = (yenbeolTransform != null) ? yenbeolTransform.position : transform.position;

            // 웨이포인트 경로 (노란색)
            Gizmos.color = Color.yellow;
            foreach (var wp in waypoints)
            {
                if (wp == null) continue;
                Gizmos.DrawLine(prev, wp.position);
                Gizmos.DrawWireSphere(wp.position, 0.2f);
                prev = wp.position;
            }

            // 최종 목적지 (초록색)
            Gizmos.color = Color.green;
            Gizmos.DrawLine(prev, destination.position);
            Gizmos.DrawWireSphere(destination.position, 0.35f);

            // 자동 스티어링 감지 범위 (파란색, 활성화 시)
            if (enableAutoAvoidance && yenbeolTransform != null)
            {
                Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.25f);
                Gizmos.DrawWireSphere(yenbeolTransform.position, avoidanceDetectDistance);
            }

            // 현재 스티어링 방향 (플레이 중 흰색 화살표)
            if (Application.isPlaying && yenbeolTransform != null && _debugSteerDir != Vector3.zero)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawRay(yenbeolTransform.position, _debugSteerDir * 1.5f);
            }
        }
    }
}
