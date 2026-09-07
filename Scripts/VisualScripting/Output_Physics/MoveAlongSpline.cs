using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace _Project.Scripts.VisualScripting
{
    public class MoveAlongSpline : ProcessBase
    {
        [Header("Target Settings")]
        [SerializeField] private GameObject objToMove;
        [SerializeField] private SplineContainer splineContainer;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 20.0f;

        [Header("Hanging Settings")]
        [SerializeField] private Vector3 hangingOffset = new Vector3(0.0f, -1.6f, 0.0f);
        [SerializeField] private bool lookForward = true;
        [SerializeField] private bool keepUpright = true;

        [Header("Lane Switch Settings")]
        [SerializeField] private float switchDuration = 1.0f;
        [SerializeField] private float maxSwitchDistance = 5.0f;
        [SerializeField] private float switchJumpHeight = 5.0f;

        [Header("Exit Settings")]
        [Tooltip("레일이 끝났을 때 앞으로 튕겨나가는 힘의 배수 (기본 1.0 = 레일 속도 유지)")]
        [SerializeField] private float exitMomentumMultiplier = 1.0f; //  관성 배수
        [Tooltip("레일에서 내릴 때 위로 살짝 띄워주는 힘")]
        [SerializeField] private float exitUpwardForce = 3.0f; //  위로 띄워주는 텐션

        [Header("Trigger Protection")]
        [SerializeField] private float retriggerCooldown = 0.5f;

        private int currentSplineIndex = 0;
        private Coroutine _moveCoroutine;
        private int _inputDirection = 0;
        private float _travelDirection = 1.0f;
        private float _nextAllowedTime = 0f;

        private void Awake()
        {
            if (!objToMove) objToMove = this.gameObject;
        }

        private void Update()
        {
            if (_moveCoroutine != null)
            {
                if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) _inputDirection = -1;
                if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) _inputDirection = 1;
            }
        }

        public override void Execute()
        {
            IsOn = false;

            if (objToMove == null || splineContainer == null)
            {
                IsOn = true;
                return;
            }

            PlayerLocomotion locomotion = objToMove.GetComponent<PlayerLocomotion>();

            if (locomotion != null && locomotion.isRidingRail)
            {
                IsOn = true;
                return;
            }

            if (Time.time < _nextAllowedTime)
            {
                IsOn = true;
                return;
            }

            StartSplineMove();
        }

        private void StartSplineMove()
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(MoveRoutine());
        }

        private IEnumerator MoveRoutine()
        {
            Rigidbody rb = objToMove.GetComponent<Rigidbody>();
            PlayerLocomotion locomotion = objToMove.GetComponent<PlayerLocomotion>();

            // [핵심 해결] SetRailMode로 회전이 (0,0,0)으로 강제 초기화되기 '직전'의 진짜 시선을 미리 저장!
            Vector3 entryForward = objToMove.transform.forward;

            if (locomotion != null) locomotion.SetRailMode(true);
            else if (rb != null) rb.isKinematic = true;

            float currentT = 0f;

            if (splineContainer.Splines.Count > 0)
            {
                float minDistanceSq = float.MaxValue;
                int bestSplineIndex = 0;
                float bestT = 0f;
                float3 localPlayerPos = splineContainer.transform.InverseTransformPoint(objToMove.transform.position);

                for (int i = 0; i < splineContainer.Splines.Count; i++)
                {
                    SplineUtility.GetNearestPoint(splineContainer.Splines[i], localPlayerPos, out float3 nearestPt, out float t);
                    float distSq = math.distancesq(localPlayerPos, nearestPt);
                    if (distSq < minDistanceSq)
                    {
                        minDistanceSq = distSq;
                        bestSplineIndex = i;
                        bestT = t;
                    }
                }
                currentSplineIndex = bestSplineIndex;
                currentT = bestT;
            }

            splineContainer.Evaluate(currentSplineIndex, currentT, out var startPos, out var startTan, out var startUp);
            Vector3 worldTangent = splineContainer.transform.TransformDirection(startTan).normalized;

            // 실시간 forward가 아니라, 아까 저장해둔 '진짜 시선(entryForward)'으로 내적 계산!
            float dotDir = Vector3.Dot(entryForward, worldTangent);
            _travelDirection = (dotDir >= 0f) ? 1.0f : -1.0f;

            int targetSplineIndex = currentSplineIndex;
            float switchProgress = 1f;
            _inputDirection = 0;

            float currentSplineLength = splineContainer.CalculateLength(currentSplineIndex);

            //  플레이어의 마지막 진행 방향을 기억할 변수
            Vector3 lastWorldForward = objToMove.transform.forward;

            while (true)
            {
                yield return new WaitForFixedUpdate();

                float tStep = (_travelDirection * moveSpeed / currentSplineLength) * Time.fixedDeltaTime;
                currentT += tStep;

                bool isEndOfRail = (_travelDirection > 0 && currentT >= 1.0f) || (_travelDirection < 0 && currentT <= 0.0f);
                if (isEndOfRail && switchProgress >= 1f)
                {
                    break; // 레일 끝에서 탈출
                }

                float clampedT = Mathf.Clamp01(currentT);
                splineContainer.Evaluate(currentSplineIndex, clampedT, out var currentLocalPos, out var currentLocalTangent, out var currentLocalUp);

                if (currentT > 1.0f)
                {
                    float overDistance = (currentT - 1.0f) * currentSplineLength;
                    currentLocalPos = (Vector3)currentLocalPos + ((Vector3)currentLocalTangent).normalized * overDistance;
                }
                else if (currentT < 0.0f)
                {
                    float overDistance = (0.0f - currentT) * currentSplineLength;
                    currentLocalPos = (Vector3)currentLocalPos - ((Vector3)currentLocalTangent).normalized * overDistance;
                }

                Vector3 travelLocalTangent = (Vector3)currentLocalTangent * _travelDirection;

                if (_inputDirection != 0 && switchProgress >= 1f)
                {
                    Vector3 currentWorldPos = splineContainer.transform.TransformPoint(currentLocalPos);
                    Vector3 currentWorldForward = splineContainer.transform.TransformDirection(travelLocalTangent).normalized;
                    Vector3 currentWorldRight = Vector3.Cross(Vector3.up, currentWorldForward).normalized;

                    int bestTargetIndex = -1;
                    float minDistance = maxSwitchDistance;

                    for (int i = 0; i < splineContainer.Splines.Count; i++)
                    {
                        if (i == currentSplineIndex) continue;

                        SplineUtility.GetNearestPoint(splineContainer.Splines[i], currentLocalPos, out float3 nearestLocal, out float _);
                        Vector3 targetWorldPos = splineContainer.transform.TransformPoint(nearestLocal);

                        Vector3 directionToTarget = targetWorldPos - currentWorldPos;
                        float distance = directionToTarget.magnitude;

                        if (distance <= maxSwitchDistance)
                        {
                            float dotRight = Vector3.Dot(directionToTarget.normalized, currentWorldRight);
                            bool isRightSide = dotRight > 0.1f;
                            bool isLeftSide = dotRight < -0.1f;

                            if ((_inputDirection == 1 && isRightSide) || (_inputDirection == -1 && isLeftSide))
                            {
                                if (distance < minDistance)
                                {
                                    minDistance = distance;
                                    bestTargetIndex = i;
                                }
                            }
                        }
                    }

                    if (bestTargetIndex != -1)
                    {
                        targetSplineIndex = bestTargetIndex;
                        switchProgress = 0f;
                    }
                    _inputDirection = 0;
                }

                Vector3 finalLocalPos = currentLocalPos;
                Vector3 finalTangent = travelLocalTangent;
                Vector3 finalUp = currentLocalUp;
                float currentJumpOffset = 0f;

                if (switchProgress < 1f)
                {
                    switchProgress += Time.fixedDeltaTime / switchDuration;
                    switchProgress = Mathf.Clamp01(switchProgress);

                    SplineUtility.GetNearestPoint(splineContainer.Splines[targetSplineIndex], currentLocalPos, out float3 _, out float tB);
                    splineContainer.Evaluate(targetSplineIndex, tB, out var localPosB, out var localTangentB, out var localUpB);

                    Vector3 travelLocalTangentB = (Vector3)localTangentB * _travelDirection;

                    finalLocalPos = Vector3.Lerp(currentLocalPos, localPosB, switchProgress);
                    finalTangent = Vector3.Slerp(travelLocalTangent, travelLocalTangentB, switchProgress);
                    finalUp = Vector3.Slerp(currentLocalUp, localUpB, switchProgress);

                    currentJumpOffset = Mathf.Sin(switchProgress * Mathf.PI) * switchJumpHeight;

                    if (switchProgress >= 1f)
                    {
                        currentSplineIndex = targetSplineIndex;
                        currentSplineLength = splineContainer.CalculateLength(currentSplineIndex);
                        currentT = tB;
                    }
                }

                Vector3 worldPos = splineContainer.transform.TransformPoint(finalLocalPos);
                Vector3 worldForward = splineContainer.transform.TransformDirection(finalTangent).normalized;

                Vector3 worldRight, worldUp;
                if (keepUpright)
                {
                    worldRight = Vector3.Cross(Vector3.up, worldForward).normalized;
                    worldUp = Vector3.Cross(worldForward, worldRight).normalized;
                }
                else
                {
                    worldUp = splineContainer.transform.TransformDirection(finalUp).normalized;
                    worldRight = Vector3.Cross(worldUp, worldForward).normalized;
                }

                //  프레임마다 마지막 방향 갱신
                lastWorldForward = worldForward;

                Vector3 finalPos = worldPos
                                 + (worldRight * hangingOffset.x)
                                 + (worldUp * (hangingOffset.y + currentJumpOffset))
                                 + (worldForward * hangingOffset.z);

                if (rb != null)
                {
                    rb.MovePosition(finalPos);
                    if (lookForward && worldForward != Vector3.zero)
                        rb.MoveRotation(Quaternion.LookRotation(worldForward, worldUp));
                }
                else
                {
                    objToMove.transform.position = finalPos;
                    if (lookForward && worldForward != Vector3.zero)
                        objToMove.transform.rotation = Quaternion.LookRotation(worldForward, worldUp);
                }
            }

            // ---  루프 종료 후 관성(Momentum) 적용 ---
            if (locomotion != null)
            {
                locomotion.SetRailMode(false);

                //  [추가된 핵심 코드] 내리기 직전의 마지막 방향을 PlayerLocomotion에 전달!
                locomotion.SetBaseRotation(lastWorldForward);

                // 마지막 방향 * (스플라인 속도 * 배수) + 공중으로 띄우는 힘
                Vector3 launchVelocity = lastWorldForward * (moveSpeed * exitMomentumMultiplier) + (Vector3.up * exitUpwardForce);

                locomotion.ExternalLaunch(launchVelocity);
            }
            else if (rb != null)
            {
                rb.isKinematic = false;

                // NPC나 다른 오브젝트일 경우를 대비한 회전 적용
                Vector3 flatForward = lastWorldForward;
                flatForward.y = 0;
                if (flatForward.sqrMagnitude > 0.01f)
                {
                    rb.rotation = Quaternion.LookRotation(flatForward.normalized);
                }

                rb.linearVelocity = lastWorldForward * (moveSpeed * exitMomentumMultiplier) + (Vector3.up * exitUpwardForce);
            }

            _nextAllowedTime = Time.time + retriggerCooldown;

            _moveCoroutine = null;
            IsOn = true;
        }
    }
}