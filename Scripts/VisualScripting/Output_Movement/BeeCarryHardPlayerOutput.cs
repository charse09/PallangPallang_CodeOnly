using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// Yenbeol(벌)이 무거운 플레이어를 들고 힘에 부쳐 고도를 높이거나 떨어뜨리며 
    /// 위태롭게 목적지까지 날아가는 연출 Output.
    /// </summary>
    public class BeeCarryHardPlayerOutput : ProcessBase
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
        [Tooltip("목적지까지의 경유점 목록.\n비어 있으면 목적지로 직선 이동.")]
        [SerializeField] private List<Transform> waypoints = new List<Transform>();

        [Tooltip("최종 목적지 Transform")]
        [SerializeField] private Transform destination;

        [Header("Flight Settings")]
        [Tooltip("Yenbeol이 날아가는 기본 속도 (m/s)")]
        [SerializeField] private float flySpeed = 4f;

        [Tooltip("각 경유점/목적지 도착으로 판정하는 수평 거리 (m)")]
        [SerializeField] private float arrivalThreshold = 0.2f;

        [Header("Flight Trajectory Curve (고도 곡선 연출)")]
        [Tooltip("전체 비행 구간 동안 Y축 고도 오프셋(m)을 직접 제어할 곡선 그래프 (0~1 시간 기준)")]
        [SerializeField] private AnimationCurve flightHeightCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

        [Header("Heavy & Unstable Motion (위태로운 연출)")]
        [Tooltip("무거워서 아래로 쳐지는 Y축 미세 진동 폭 (m)")]
        [SerializeField] private float heavyDipAmount = 0.3f;

        [Tooltip("고도 미세 진동 속도")]
        [SerializeField] private float heavyDipSpeed = 8.0f;

        [Tooltip("비틀거리는 좌우 흔들림 폭 (m)")]
        [SerializeField] private float wobbleAmount = 0.3f;

        [Tooltip("비틀거리는 좌우 흔들림 속도")]
        [SerializeField] private float wobbleSpeed = 8.0f;

        [Tooltip("몸체가 기울어지는 최대 각도 (Roll/Pitch)")]
        [SerializeField] private float maxTiltAngle = 15.0f;

        [Header("Auto Avoidance (Optional)")]
        [Tooltip("자동 장애물 감지 & 스티어링 활성화.")]
        [SerializeField] private bool enableAutoAvoidance = false;

        [Tooltip("장애물로 인식할 레이어")]
        [SerializeField] private LayerMask obstacleLayer;

        [Tooltip("장애물 감지 레이캐스트 거리 (m)")]
        [SerializeField] private float avoidanceDetectDistance = 2.5f;

        [Tooltip("SphereCast 반지름")]
        [SerializeField] private float avoidanceSphereRadius = 0.5f;

        [Tooltip("장애물 회피 힘의 세기")]
        [SerializeField] private float avoidanceStrength = 8f;

        // 내부 상태
        private Coroutine _carryCoroutine;
        private Rigidbody _playerRb;
        private bool _wasKinematic;

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

            // STEP 1: 플레이어 물리 / 입력 완전 잠금
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

            yield return null;
            yield return null;

            // STEP 2: grab 시점 오프셋 기록
            Vector3 carryOffset = playerTransform.position - yenbeolTransform.position;

            // STEP 3: 전체 비행 경로 포인트 구성
            List<Vector3> pathTargets = new List<Vector3>();
            foreach (var wp in waypoints)
                if (wp != null) pathTargets.Add(wp.position);
            pathTargets.Add(destination.position);

            Vector3 flightStartPos = yenbeolTransform.position;
            float totalFlightDistanceXZ = CalculateTotalPathDistanceXZ(flightStartPos, pathTargets);
            float travelledDistanceXZ = 0f;

            float totalFlightTime = 0f;

            foreach (Vector3 target in pathTargets)
            {
                Vector3 segmentStartPos = yenbeolTransform.position;

                // [핵심 변경] Y축 고도를 제외한 pure XZ 수평 거리가 도착 범위 내로 들어올 때까지 이동
                while (GetHorizontalDistance(yenbeolTransform.position, target) > arrivalThreshold)
                {
                    totalFlightTime += Time.deltaTime;

                    float currentFlatDistToTarget = GetHorizontalDistance(yenbeolTransform.position, target);

                    // A. 수평(XZ) 거리 기반의 정확한 전체 진행률 (0.0 ~ 1.0) 연산
                    float currentSegmentTravelledXZ = GetHorizontalDistance(segmentStartPos, yenbeolTransform.position);
                    float currentProgress = Mathf.Clamp01((travelledDistanceXZ + currentSegmentTravelledXZ) / Mathf.Max(totalFlightDistanceXZ, 0.001f));

                    // B. 기본 진행 방향 연산
                    Vector3 steerDir = CalculateSteeringDirection(yenbeolTransform.position, target);
                    Vector3 rightVec = Vector3.Cross(Vector3.up, steerDir).normalized;
                    if (rightVec == Vector3.zero) rightVec = Vector3.right;

                    // C. 수평 비행 기본 고도 계산 (시작점 Y와 도착점 Y를 진행률에 맞춰 선형 보정)
                    float baseHeight = Mathf.Lerp(segmentStartPos.y, target.y, currentSegmentTravelledXZ / Mathf.Max(GetHorizontalDistance(segmentStartPos, target), 0.001f));

                    // D. 커브 고도 오프셋 및 비틀거림 연산 (목적지에 다가갈수록 오프셋을 자연스럽게 0으로 감쇠)
                    float arrivalFade = Mathf.Clamp01(currentFlatDistToTarget / 1.0f);

                    float curveHeightOffset = flightHeightCurve.Evaluate(currentProgress);
                    float verticalDip = -Mathf.Abs(Mathf.Sin(totalFlightTime * heavyDipSpeed)) * heavyDipAmount * arrivalFade;
                    float horizontalWobble = Mathf.Sin(totalFlightTime * wobbleSpeed) * wobbleAmount * arrivalFade;

                    // E. 위치 연산: 수평 이동(XZ) + 선형 Y 고도 + 커브/노이즈 오프셋
                    float stepSize = Mathf.Min(flySpeed * Time.deltaTime, currentFlatDistToTarget);
                    Vector3 nextBasePos = yenbeolTransform.position + (steerDir * stepSize);

                    nextBasePos.y = baseHeight + curveHeightOffset + verticalDip;

                    Vector3 finalPos = nextBasePos + (rightVec * horizontalWobble * Time.deltaTime * 5f);
                    yenbeolTransform.position = finalPos;

                    // 플레이어 위치 동기화
                    playerTransform.position = yenbeolTransform.position + carryOffset;

                    // F. 회전 및 비틀림 처리 (0.3m 이내에서는 공회전 방지)
                    if (currentFlatDistToTarget > 0.3f)
                    {
                        Vector3 flatDir = new Vector3(steerDir.x, 0f, steerDir.z);
                        if (flatDir.sqrMagnitude > 0.01f)
                        {
                            Quaternion baseRot = Quaternion.LookRotation(flatDir);

                            float tiltRoll = Mathf.Sin(totalFlightTime * wobbleSpeed) * maxTiltAngle * arrivalFade;
                            float tiltPitch = Mathf.Cos(totalFlightTime * heavyDipSpeed) * (maxTiltAngle * 0.5f) * arrivalFade;
                            Quaternion tiltRot = Quaternion.Euler(tiltPitch, 0f, tiltRoll);

                            yenbeolTransform.rotation = Quaternion.Slerp(
                                yenbeolTransform.rotation, baseRot * tiltRot, Time.deltaTime * 6f);
                        }
                    }

                    yield return null;
                }

                // 해당 경유점 누적 수평 거리 업데이트 및 정확하게 목표 위치에 스냅
                travelledDistanceXZ += GetHorizontalDistance(segmentStartPos, target);
                yenbeolTransform.position = target;
                playerTransform.position = target + carryOffset;
            }

            // STEP 4: 플레이어 상태 복구
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
        }

        // --- 헬퍼 함수: Y축을 제외한 순수 수평 거리(XZ) 연산 ---
        private float GetHorizontalDistance(Vector3 p1, Vector3 p2)
        {
            Vector2 v1 = new Vector2(p1.x, p1.z);
            Vector2 v2 = new Vector2(p2.x, p2.z);
            return Vector2.Distance(v1, v2);
        }

        private float CalculateTotalPathDistanceXZ(Vector3 startPos, List<Vector3> targets)
        {
            float totalDist = 0f;
            Vector3 current = startPos;
            foreach (var target in targets)
            {
                totalDist += GetHorizontalDistance(current, target);
                current = target;
            }
            return totalDist;
        }

        private Vector3 CalculateSteeringDirection(Vector3 currentPos, Vector3 target)
        {
            Vector3 toTarget = (target - currentPos).normalized;

            if (!enableAutoAvoidance)
            {
                _debugSteerDir = toTarget;
                return toTarget;
            }

            Vector3 avoidance = Vector3.zero;
            Vector3[] probeDirections = new Vector3[]
            {
                toTarget,
                Quaternion.Euler(0,  45, 0) * toTarget,
                Quaternion.Euler(0, -45, 0) * toTarget,
                Quaternion.Euler( 30, 0, 0) * toTarget,
                Quaternion.Euler(-30, 0, 0) * toTarget,
                Quaternion.Euler(0,  90, 0) * toTarget,
                Quaternion.Euler(0, -90, 0) * toTarget,
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
                    Vector3 pushDir = (currentPos - hit.point).normalized;
                    float normalizedDist = 1f - (hit.distance / avoidanceDetectDistance);
                    avoidance += pushDir * (normalizedDist * avoidanceStrength);
                }
            }

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

        private void OnDrawGizmos()
        {
            if (destination == null) return;

            Vector3 prev = (yenbeolTransform != null) ? yenbeolTransform.position : transform.position;

            Gizmos.color = Color.yellow;
            foreach (var wp in waypoints)
            {
                if (wp == null) continue;
                Gizmos.DrawLine(prev, wp.position);
                Gizmos.DrawWireSphere(wp.position, 0.2f);
                prev = wp.position;
            }

            Gizmos.color = Color.green;
            Gizmos.DrawLine(prev, destination.position);
            Gizmos.DrawWireSphere(destination.position, 0.35f);

            if (enableAutoAvoidance && yenbeolTransform != null)
            {
                Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.25f);
                Gizmos.DrawWireSphere(yenbeolTransform.position, avoidanceDetectDistance);
            }

            if (Application.isPlaying && yenbeolTransform != null && _debugSteerDir != Vector3.zero)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawRay(yenbeolTransform.position, _debugSteerDir * 1.5f);
            }
        }
    }
}