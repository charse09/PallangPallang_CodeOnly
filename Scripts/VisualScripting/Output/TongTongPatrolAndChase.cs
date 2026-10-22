using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 순찰 경로를 왕복(Ping-Pong)하며 지정한 보폭으로 통통 튀어 분할 이동하다가,
    /// 조건 충족 시 대상을 주시하며 추적을 유지하고, 복귀 조건(OR) 충족 시에만 순찰로 돌아옵니다.
    /// [상태 잠금]: 회전 중에는 플레이어 감지 및 상태 전환이 일체 차단되어 오감지와 와리가리 현상을 방지합니다.
    /// </summary>
    public class TongTongPatrolAndChase : ProcessBase
    {
        private enum AIState
        {
            Idle,
            Patrolling,
            Chasing,
            Returning
        }

        public enum PatrolMode
        {
            [Tooltip("1 -> 2 -> 3 -> 2 -> 1 방식으로 끝에 닿으면 되돌아오는 왕복 방식")]
            PingPong,
            [Tooltip("1 -> 2 -> 3 -> 1 방식으로 순환하는 루프 방식")]
            Loop
        }

        [Header("1. Execution Settings")]
        [Tooltip("체크 시 게임 시작과 동시에 자동으로 순찰을 시작합니다.")]
        [SerializeField] private bool autoStartOnStart = true;

        [Header("2. Patrol Settings (순찰 설정)")]
        [Tooltip("순찰 이동 방식 (기본: PingPong 왕복)")]
        [SerializeField] private PatrolMode patrolMode = PatrolMode.PingPong;

        [Tooltip("순찰할 착지 지점(Waypoint) 리스트")]
        [SerializeField] private List<Transform> patrolPoints = new List<Transform>();

        [Tooltip("순찰 시 1회 도약 최대 이동 거리 (보폭)")]
        [SerializeField] private float patrolStepDistance = 2.0f;

        [Tooltip("순찰 시 1회 도약 포물선 높이")]
        [SerializeField] private float patrolBounceHeight = 1.5f;

        [Tooltip("순찰 시 1회 도약 체공 시간 (초)")]
        [SerializeField] private float patrolBounceDuration = 0.4f;

        [Header("3. Chase Settings (추적 설정)")]
        [Tooltip("추적할 대상 Transform (비워두면 Tag가 'Player'인 대상을 자동 탐색)")]
        [SerializeField] private Transform chaseTarget;

        [Tooltip("추적 시 1회 도약 최대 이동 거리 (보폭)")]
        [SerializeField] private float chaseStepDistance = 2.5f;

        [Tooltip("최대 보폭으로 뛸 때의 포물선 높이")]
        [SerializeField] private float chaseBounceHeight = 2.0f;

        [Tooltip("최대 보폭으로 뛸 때의 체공 시간 (초)")]
        [SerializeField] private float chaseBounceDuration = 0.4f;

        [Tooltip("플레이어에게 완전히 밀착했을 때 제자리 바운드 높이")]
        [SerializeField] private float touchBounceHeight = 0.8f;

        [Header("4. Ground Alignment Settings (바닥 뚫림 방지)")]
        [Tooltip("체크 시 착지할 바닥면을 레이캐스트로 탐지하여 바닥 위에 완벽히 안착시킵니다.")]
        [SerializeField] private bool snapToGround = true;

        [Tooltip("바닥면으로 인식할 레이어")]
        [SerializeField] private LayerMask groundLayerMask = 1 << 0; // Default

        [Tooltip("바닥면으로부터 띄울 높이 (공의 반지름/중심 높이)")]
        [SerializeField] private float groundOffset = 0.5f;

        [Header("5. State Lock & Rotation (★ 상태 잠금 회전)")]
        [Tooltip("Y축 회전만 고정하여 위아래로 기우뚱하지 않도록 합니다.")]
        [SerializeField] private bool lockYAxis = true;

        [Tooltip("메쉬가 반대 방향으로 제작된 경우 체크 (180도 반전)")]
        [SerializeField] private bool reverseFacing = false;

        [Tooltip("회전 속도")]
        [SerializeField] private float rotationSpeed = 14f;

        [Tooltip("★ 회전 중으로 판정할 각도 차이 (도). 목표 방향과 이 각도 이상 벌어져 돌고 있을 때는 상태 전환(추적/복귀)이 완벽히 잠깁니다.")]
        [Range(5f, 45f)]
        [SerializeField] private float turnAngleThreshold = 20f;

        [Tooltip("순찰 중 다음 목표 방향과 각도 차이가 클 때 점프 전 제자리에서 먼저 돌고 출발할지 여부 (추천: 체크)")]
        [SerializeField] private bool rotateInPlaceBeforeStep = true;

        [Header("6. External Trigger Conditions (조건 감지용 Output)")]
        [Tooltip("이 Output의 IsOn이 true가 되는 순간 추적을 시작합니다.")]
        [SerializeField] private ProcessBase chaseTriggerCondition;

        [Tooltip("[복귀 조건 1] 이 Output이 IsOn되면 추적을 멈추고 순찰로 복귀합니다.")]
        [SerializeField] private ProcessBase returnTriggerCondition1;

        [Tooltip("[복귀 조건 2] 이 Output이 IsOn되면 추적을 멈추고 순찰로 복귀합니다.")]
        [SerializeField] private ProcessBase returnTriggerCondition2;

        [Tooltip("접촉 액션 실행용 Output (예: 피격 Trigger)")]
        [SerializeField] private ProcessBase contactTriggerCondition;

        [Header("7. Linked Output Actions (연계 실행용 Output)")]
        [SerializeField] private ProcessBase onChaseStartAction;
        [SerializeField] private ProcessBase onReturnStartAction;
        [SerializeField] private ProcessBase onContactAction;

        [Header("8. Squash & Impact FX")]
        [SerializeField] private Transform ballTransform;
        [SerializeField] private bool useSquashAndStretch = true;
        [SerializeField] private float squashAmount = 0.3f;
        [SerializeField] private GameObject landImpactFx;
        [SerializeField] private AudioClip landSfx;

        private AudioSource _audioSource;
        private Vector3 _originalScale;
        private Coroutine _mainLoopCoroutine;
        private AIState _currentState = AIState.Idle;

        private int _currentPatrolIndex = 0;
        private int _patrolDirection = 1;
        private bool _hasFiredContactAction = false;
        private bool _isTurning = false; // ★ 회전 잠금 플래그

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (ballTransform == null) ballTransform = transform;
            _originalScale = ballTransform.localScale;
        }

        private void Start()
        {
            if (chaseTarget == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) chaseTarget = player.transform;
            }

            if (autoStartOnStart)
            {
                Execute();
            }
        }

        private void OnDisable()
        {
            if (_mainLoopCoroutine != null)
            {
                StopCoroutine(_mainLoopCoroutine);
                _mainLoopCoroutine = null;
            }
            StopAllCoroutines();

            if (ballTransform != null)
            {
                ballTransform.localScale = _originalScale;
            }

            _isTurning = false;
            _currentState = AIState.Idle;
            _hasFiredContactAction = false;
        }

        public override void Reset()
        {
            base.Reset();
            _isTurning = false;
            _currentState = AIState.Idle;
            IsOn = false;
        }

        public override void Execute()
        {
            if (!gameObject.activeInHierarchy || !enabled) return;

            if (_mainLoopCoroutine != null)
            {
                StopCoroutine(_mainLoopCoroutine);
            }

            _currentState = AIState.Patrolling;
            _isTurning = false;
            IsOn = false;
            _mainLoopCoroutine = StartCoroutine(MainAILoopRoutine());
        }

        private IEnumerator MainAILoopRoutine()
        {
            while (gameObject.activeInHierarchy && enabled)
            {
                CheckContactCondition();

                if (!gameObject.activeInHierarchy || !enabled)
                {
                    yield break;
                }

                // ★ 상태 전환 검사 (회전 중일 때는 내부에서 즉시 차단됨)
                CheckStateTransitions();

                switch (_currentState)
                {
                    case AIState.Patrolling:
                        yield return StartCoroutine(PatrolStepRoutine());
                        break;

                    case AIState.Chasing:
                        yield return StartCoroutine(ChaseStepRoutine());
                        break;

                    case AIState.Returning:
                        yield return StartCoroutine(ReturnStepRoutine());
                        break;

                    default:
                        yield return null;
                        break;
                }
            }
        }

        /// <summary>
        /// ★ 회전 상태 잠금(State Lock): 회전 중이거나 방향이 크게 어긋나 있으면 상태 전환을 완전히 무시합니다.
        /// </summary>
        private void CheckStateTransitions()
        {
            // 회전 중 플래그가 켜져 있거나, 순찰 목표 방향과 각도가 크게 벌어져 있으면 절대 감지/전환 불가
            if (_isTurning || IsPatrolDirectionMisaligned())
            {
                return;
            }

            // 1. 순찰/복귀 중 -> 추적 시작
            if (_currentState != AIState.Chasing && chaseTriggerCondition != null && chaseTriggerCondition.IsOn)
            {
                _currentState = AIState.Chasing;
                IsOn = true;

                if (onChaseStartAction != null)
                {
                    onChaseStartAction.Execute();
                }
                return;
            }

            // 2. 추적 중 -> 복귀 조건 충족 시 순찰/복귀로 전환
            if (_currentState == AIState.Chasing)
            {
                bool isReturnTriggered = (returnTriggerCondition1 != null && returnTriggerCondition1.IsOn) ||
                                         (returnTriggerCondition2 != null && returnTriggerCondition2.IsOn);

                if (isReturnTriggered)
                {
                    _currentState = AIState.Returning;
                    IsOn = false;
                    _currentPatrolIndex = GetNearestPatrolIndex();

                    if (onReturnStartAction != null)
                    {
                        onReturnStartAction.Execute();
                    }
                }
            }
        }

        private bool IsPatrolDirectionMisaligned()
        {
            if (_currentState != AIState.Patrolling) return false;
            if (patrolPoints == null || patrolPoints.Count == 0) return false;

            Transform targetPoint = patrolPoints[_currentPatrolIndex];
            if (targetPoint == null) return false;

            Vector3 toTarget = targetPoint.position - ballTransform.position;
            if (lockYAxis) toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f) return false;

            Vector3 targetForward = toTarget.normalized;
            if (reverseFacing) targetForward = -targetForward;

            Vector3 currentForward = ballTransform.forward;
            return Vector3.Angle(currentForward, targetForward) > turnAngleThreshold;
        }

        private void CheckContactCondition()
        {
            if (contactTriggerCondition != null && contactTriggerCondition.IsOn)
            {
                if (!_hasFiredContactAction)
                {
                    _hasFiredContactAction = true;
                    if (onContactAction != null)
                    {
                        onContactAction.Execute();
                    }
                }
            }
            else
            {
                _hasFiredContactAction = false;
            }
        }

        private IEnumerator PatrolStepRoutine()
        {
            if (patrolPoints == null || patrolPoints.Count == 0)
            {
                yield return null;
                yield break;
            }

            Transform targetPoint = patrolPoints[_currentPatrolIndex];
            if (targetPoint == null)
            {
                AdvancePatrolIndex();
                yield break;
            }

            Vector3 startPos = ballTransform.position;
            Vector3 targetPos = targetPoint.position;
            Vector3 diff = targetPos - startPos;
            if (lockYAxis) diff.y = 0f;

            // ★ 각도가 많이 틀어져 있는 경우(예: Ping-Pong 끝 180도 반전 코너),
            // 점프하기 전에 제자리에서 먼저 안전하게 몸을 돌립니다. (이 동안 상태 잠금 유지!)
            if (rotateInPlaceBeforeStep && diff.sqrMagnitude > 0.001f)
            {
                Vector3 desiredForward = diff.normalized;
                if (reverseFacing) desiredForward = -desiredForward;

                float angleDiff = Vector3.Angle(ballTransform.forward, desiredForward);
                if (angleDiff > turnAngleThreshold)
                {
                    yield return StartCoroutine(RotateInPlaceRoutine(desiredForward));
                }
            }

            float horizontalDist = diff.magnitude;
            Vector3 endPos;
            bool reachedTargetWaypoint = false;

            if (horizontalDist <= patrolStepDistance)
            {
                endPos = targetPos;
                reachedTargetWaypoint = true;
            }
            else
            {
                float ratio = patrolStepDistance / horizontalDist;
                endPos = startPos + diff.normalized * patrolStepDistance;
                endPos.y = Mathf.Lerp(startPos.y, targetPos.y, ratio);
            }

            endPos.y = GetGroundAdjustedY(endPos, endPos.y);

            yield return StartCoroutine(BounceMotion(startPos, endPos, patrolBounceHeight, patrolBounceDuration, false));

            OnLandImpact(endPos);

            if (reachedTargetWaypoint)
            {
                AdvancePatrolIndex();
            }
        }

        /// <summary>
        /// ★ 제자리 회전 코루틴: 몸을 돌리는 동안 _isTurning = true로 잠겨 있어 감지가 차단됩니다.
        /// </summary>
        private IEnumerator RotateInPlaceRoutine(Vector3 targetForward)
        {
            _isTurning = true;
            Quaternion targetRot = Quaternion.LookRotation(targetForward);

            while (Quaternion.Angle(ballTransform.rotation, targetRot) > 5f)
            {
                if (!gameObject.activeInHierarchy || !enabled) yield break;

                ballTransform.rotation = Quaternion.Slerp(
                    ballTransform.rotation,
                    targetRot,
                    Time.deltaTime * rotationSpeed
                );
                yield return null;
            }

            ballTransform.rotation = targetRot;
            _isTurning = false; // 회전 정렬 완료 후 잠금 해제
        }

        private void AdvancePatrolIndex()
        {
            if (patrolPoints.Count <= 1) return;

            if (patrolMode == PatrolMode.PingPong)
            {
                if (_currentPatrolIndex >= patrolPoints.Count - 1)
                {
                    _patrolDirection = -1;
                }
                else if (_currentPatrolIndex <= 0)
                {
                    _patrolDirection = 1;
                }

                _currentPatrolIndex += _patrolDirection;
            }
            else
            {
                _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Count;
            }
        }

        private IEnumerator ChaseStepRoutine()
        {
            if (chaseTarget == null)
            {
                yield return null;
                yield break;
            }

            Vector3 startPos = ballTransform.position;
            Vector3 targetPos = chaseTarget.position;

            Vector3 diff = targetPos - startPos;
            diff.y = 0f;
            float horizontalDist = diff.magnitude;

            Vector3 endPos;
            float stepBounceHeight;
            float stepBounceDuration;

            if (horizontalDist <= 0.3f)
            {
                endPos = targetPos;
                stepBounceHeight = touchBounceHeight;
                stepBounceDuration = chaseBounceDuration * 0.75f;
            }
            else if (horizontalDist <= chaseStepDistance)
            {
                endPos = targetPos;
                float distanceRatio = Mathf.Clamp01(horizontalDist / chaseStepDistance);
                stepBounceHeight = Mathf.Lerp(touchBounceHeight, chaseBounceHeight, distanceRatio);
                stepBounceDuration = Mathf.Lerp(chaseBounceDuration * 0.6f, chaseBounceDuration, distanceRatio);
            }
            else
            {
                endPos = startPos + diff.normalized * chaseStepDistance;
                stepBounceHeight = chaseBounceHeight;
                stepBounceDuration = chaseBounceDuration;
            }

            endPos.y = GetGroundAdjustedY(endPos, targetPos.y + groundOffset);

            yield return StartCoroutine(BounceMotion(startPos, endPos, stepBounceHeight, stepBounceDuration, true));

            OnLandImpact(endPos);
        }

        private IEnumerator ReturnStepRoutine()
        {
            if (patrolPoints == null || patrolPoints.Count == 0)
            {
                _currentState = AIState.Patrolling;
                yield break;
            }

            Transform nearestPoint = patrolPoints[_currentPatrolIndex];
            Vector3 startPos = ballTransform.position;
            Vector3 targetPos = nearestPoint.position;

            Vector3 diff = targetPos - startPos;
            diff.y = 0f;
            float dist = diff.magnitude;

            Vector3 endPos;
            if (dist <= chaseStepDistance)
            {
                endPos = targetPos;
                endPos.y = GetGroundAdjustedY(endPos, targetPos.y);
                yield return StartCoroutine(BounceMotion(startPos, endPos, patrolBounceHeight, patrolBounceDuration, false));
                OnLandImpact(endPos);
                _currentState = AIState.Patrolling;
            }
            else
            {
                endPos = startPos + diff.normalized * chaseStepDistance;
                endPos.y = GetGroundAdjustedY(endPos, targetPos.y);
                yield return StartCoroutine(BounceMotion(startPos, endPos, patrolBounceHeight, patrolBounceDuration, false));
                OnLandImpact(endPos);
            }
        }

        private float GetGroundAdjustedY(Vector3 position, float fallbackY)
        {
            if (!snapToGround) return fallbackY;

            Vector3 rayStart = new Vector3(position.x, position.y + 3.0f, position.z);
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 10.0f, groundLayerMask, QueryTriggerInteraction.Ignore))
            {
                return hit.point.y + groundOffset;
            }

            return fallbackY;
        }

        private int GetNearestPatrolIndex()
        {
            int nearestIdx = 0;
            float minDistance = float.MaxValue;
            Vector3 currentPos = ballTransform.position;

            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] == null) continue;
                float dist = Vector3.Distance(currentPos, patrolPoints[i].position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestIdx = i;
                }
            }
            return nearestIdx;
        }

        private IEnumerator BounceMotion(Vector3 start, Vector3 end, float height, float duration, bool lookDirectlyAtTarget)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (!gameObject.activeInHierarchy || !enabled) yield break;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                Vector3 currentPos = Vector3.Lerp(start, end, t);
                currentPos.y += Mathf.Sin(t * Mathf.PI) * height;
                ballTransform.position = currentPos;

                if (useSquashAndStretch)
                {
                    float stretchFactor = Mathf.Sin(t * Mathf.PI) * (squashAmount * 0.5f);
                    ballTransform.localScale = new Vector3(
                        _originalScale.x * (1f - stretchFactor),
                        _originalScale.y * (1f + stretchFactor),
                        _originalScale.z * (1f - stretchFactor)
                    );
                }

                UpdateRotation(lookDirectlyAtTarget, start, end);

                yield return null;
            }

            ballTransform.position = end;
            ballTransform.localScale = _originalScale;
        }

        private void UpdateRotation(bool lookDirectlyAtTarget, Vector3 start, Vector3 end)
        {
            Vector3 dir = Vector3.zero;

            if (lookDirectlyAtTarget && chaseTarget != null)
            {
                dir = chaseTarget.position - ballTransform.position;
            }
            else
            {
                dir = end - start;
            }

            if (lockYAxis) dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            Vector3 targetForward = dir.normalized;
            if (reverseFacing) targetForward = -targetForward;

            Quaternion targetRot = Quaternion.LookRotation(targetForward);
            ballTransform.rotation = Quaternion.Slerp(ballTransform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }

        private void OnLandImpact(Vector3 landPos)
        {
            if (!gameObject.activeInHierarchy || !enabled) return;

            if (landImpactFx != null)
            {
                Instantiate(landImpactFx, landPos, Quaternion.identity);
            }

            if (_audioSource != null && landSfx != null)
            {
                _audioSource.PlayOneShot(landSfx);
            }

            if (useSquashAndStretch && gameObject.activeInHierarchy && enabled)
            {
                StartCoroutine(SquashRoutine());
            }
        }

        private IEnumerator SquashRoutine()
        {
            float elapsed = 0f;
            float duration = 0.1f;

            while (elapsed < duration)
            {
                if (!gameObject.activeInHierarchy || !enabled) yield break;

                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                float squashOffset = Mathf.Sin(t * Mathf.PI) * squashAmount;
                ballTransform.localScale = new Vector3(
                    _originalScale.x * (1f + squashOffset),
                    _originalScale.y * (1f - squashOffset),
                    _originalScale.z * (1f + squashOffset)
                );

                yield return null;
            }

            ballTransform.localScale = _originalScale;
        }
    }
}