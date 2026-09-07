using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 순찰 경로를 왕복(Ping-Pong)하며 통통 튀어 이동하다가,
    /// 조건 충족 시 대상을 주시하며 추적을 유지하고, 복귀 조건(OR) 충족 시에만 순찰로 돌아옵니다.
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
        [Tooltip("체크 시 게임 시작과 동시에 자동으로 순찰을 시작합니다. 체크 해제 시 외부에서 Execute()를 호출해야 시작합니다.")]
        [SerializeField] private bool autoStartOnStart = true;

        [Header("2. Patrol Settings")]
        [Tooltip("순찰 이동 방식 (기본: PingPong 왕복)")]
        [SerializeField] private PatrolMode patrolMode = PatrolMode.PingPong;

        [Tooltip("순찰할 착지 지점(Waypoint) 리스트")]
        [SerializeField] private List<Transform> patrolPoints = new List<Transform>();

        [SerializeField] private float patrolBounceHeight = 2.0f;
        [SerializeField] private float patrolBounceDuration = 0.5f;

        [Header("3. Chase Settings")]
        [Tooltip("추적할 대상 Transform (비워두면 Tag가 'Player'인 대상을 자동 탐색)")]
        [SerializeField] private Transform chaseTarget;

        [Tooltip("1회 도약 시 최대 이동 거리 (보폭)")]
        [SerializeField] private float chaseStepDistance = 2.5f;

        [Tooltip("최대 보폭으로 뛸 때의 포물선 높이")]
        [SerializeField] private float chaseBounceHeight = 2.0f;

        [Tooltip("최대 보폭으로 뛸 때의 체공 시간 (초)")]
        [SerializeField] private float chaseBounceDuration = 0.4f;

        [Tooltip("플레이어에게 완전히 밀착했을 때 제자리 바운드 높이")]
        [SerializeField] private float touchBounceHeight = 0.8f;

        [Header("4. External Trigger Conditions (조건 감지용 Output)")]
        [Tooltip("이 Output의 IsOn이 true가 되는 순간 추적을 시작하고, 복귀 조건 전까지 계속 추적을 유지합니다.")]
        [SerializeField] private ProcessBase chaseTriggerCondition;

        [Tooltip("[복귀 조건 1] 이 Output이 IsOn되면 추적을 멈추고 순찰로 복귀합니다 (OR 조건 1).")]
        [SerializeField] private ProcessBase returnTriggerCondition1;

        [Tooltip("[복귀 조건 2] 이 Output이 IsOn되면 추적을 멈추고 순찰로 복귀합니다 (OR 조건 2).")]
        [SerializeField] private ProcessBase returnTriggerCondition2;

        [Tooltip("이 Output의 IsOn이 true가 되면 접촉 액션을 실행합니다. (예: 플레이어 피격 Trigger)")]
        [SerializeField] private ProcessBase contactTriggerCondition;

        [Header("5. Linked Output Actions (연계 실행용 Output)")]
        [Tooltip("추적을 시작하는 순간 1회 실행할 외부 Output")]
        [SerializeField] private ProcessBase onChaseStartAction;

        [Tooltip("추적을 멈추고 다시 순찰/복귀로 돌아갈 때 1회 실행할 외부 Output")]
        [SerializeField] private ProcessBase onReturnStartAction;

        [Tooltip("접촉(contactTriggerCondition)이 활성화되었을 때 즉시 실행할 외부 Output")]
        [SerializeField] private ProcessBase onContactAction;

        [Header("6. Rotation Settings")]
        [Tooltip("Y축 회전만 고정하여 위아래로 기우뚱하지 않도록 합니다.")]
        [SerializeField] private bool lockYAxis = true;

        [Tooltip("메쉬가 반대 방향으로 제작된 경우 체크 (180도 반전)")]
        [SerializeField] private bool reverseFacing = false;

        [Tooltip("부드러운 회전 적용 여부")]
        [SerializeField] private bool useSmoothRotation = true;
        [SerializeField] private float rotationSpeed = 14f;

        [Header("7. Squash & Impact FX")]
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

            _currentState = AIState.Idle;
            _hasFiredContactAction = false;
        }

        public override void Execute()
        {
            if (!gameObject.activeInHierarchy || !enabled) return;

            if (_mainLoopCoroutine != null)
            {
                StopCoroutine(_mainLoopCoroutine);
            }

            _currentState = AIState.Patrolling;
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

        private void CheckStateTransitions()
        {
            // 1. 순찰/복귀 중 -> 진입 트리거가 한 번이라도 On되면 즉시 추적 시작 & 고정
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

            // 2. 추적 중 -> 오직 명시적인 복귀 조건(returnTrigger 1 또는 2)이 켜졌을 때만 복귀
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
            Vector3 endPos = targetPoint.position;

            yield return StartCoroutine(BounceMotion(startPos, endPos, patrolBounceHeight, patrolBounceDuration, false));

            OnLandImpact(endPos);
            AdvancePatrolIndex();
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
                endPos.y = targetPos.y;
                stepBounceHeight = chaseBounceHeight;
                stepBounceDuration = chaseBounceDuration;
            }

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

            if (dist <= chaseStepDistance)
            {
                yield return StartCoroutine(BounceMotion(startPos, targetPos, patrolBounceHeight, patrolBounceDuration, false));
                OnLandImpact(targetPos);
                _currentState = AIState.Patrolling;
            }
            else
            {
                Vector3 nextPos = startPos + diff.normalized * chaseStepDistance;
                nextPos.y = targetPos.y;
                yield return StartCoroutine(BounceMotion(startPos, nextPos, patrolBounceHeight, patrolBounceDuration, false));
                OnLandImpact(nextPos);
            }
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
                currentPos.y += Mathf.Sin(t * Mathf.PI) * height; // 포물선 곡선 연출[cite: 1]
                ballTransform.position = currentPos;

                if (useSquashAndStretch)
                {
                    float stretchFactor = Mathf.Sin(t * Mathf.PI) * (squashAmount * 0.5f);
                    ballTransform.localScale = new Vector3(
                        _originalScale.x * (1f - stretchFactor),
                        _originalScale.y * (1f + stretchFactor),
                        _originalScale.z * (1f - stretchFactor)
                    ); // 탱탱볼 늘어남 효과[cite: 1]
                }

                UpdateRotation(lookDirectlyAtTarget, start, end); // 회전 계산[cite: 2]

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
                dir = chaseTarget.position - ballTransform.position; // 타겟 직접 주시[cite: 2]
            }
            else
            {
                dir = end - start; // 이동 방향 주시[cite: 2]
            }

            if (lockYAxis) dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            Quaternion targetRot = Quaternion.LookRotation(dir.normalized); // 방향 회전값 계산[cite: 2]
            if (reverseFacing) targetRot *= Quaternion.Euler(0f, 180f, 0f);

            if (useSmoothRotation)
            {
                ballTransform.rotation = Quaternion.Slerp(ballTransform.rotation, targetRot, Time.deltaTime * rotationSpeed);
            }
            else
            {
                ballTransform.rotation = targetRot;
            }
        }

        private void OnLandImpact(Vector3 landPos)
        {
            if (!gameObject.activeInHierarchy || !enabled) return;

            if (landImpactFx != null)
            {
                Instantiate(landImpactFx, landPos, Quaternion.identity); // 착지 이펙트 생성[cite: 1]
            }

            if (_audioSource != null && landSfx != null)
            {
                _audioSource.PlayOneShot(landSfx); // 착지 효과음[cite: 1]
            }

            if (useSquashAndStretch && gameObject.activeInHierarchy && enabled)
            {
                StartCoroutine(SquashRoutine()); // 착지 찌그러짐 연출[cite: 1]
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
                ); // 착지 압축 연출[cite: 1]

                yield return null;
            }

            ballTransform.localScale = _originalScale;
        }
    }
}