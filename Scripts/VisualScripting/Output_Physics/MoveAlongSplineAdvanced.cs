/*
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace _Project.Scripts.VisualScripting
{
    public class MoveAlongSplineAdvanced : ProcessBase
    {
        public enum SplineDirectionMode
        {
            [InspectorName("플레이어 시선 기준 (자동)")]
            LookDirection,
            [InspectorName("노드 순서대로 (0 -> 1)")]
            NodeOrder,
            [InspectorName("노드 역순대로 (1 -> 0)")]
            NodeReverse
        }

        [Header("Target Settings")]
        [SerializeField] private GameObject objToMove;
        [SerializeField] private SplineContainer splineContainer;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 20.0f;
        [Tooltip("레일 이동 방향 모드 (시선 기준 / 노드 정순 / 노드 역순)")]
        [SerializeField] private SplineDirectionMode directionMode = SplineDirectionMode.NodeOrder;

        [Header("Hanging & Attachment Settings")]
        [Tooltip("체크 시 씬 뷰에 미리 배치해 둔 고리/플레이어의 초기 상대 위치와 각도를 자동으로 측정하여 딱 걸린 채로 이동합니다.")]
        [SerializeField] private bool autoCalculateInitialOffset = true;
        [Tooltip("autoCalculateInitialOffset이 false일 때 적용될 고정 오프셋")]
        [SerializeField] private Vector3 hangingOffset = new Vector3(0.0f, -1.6f, 0.0f);
        [SerializeField] private bool lookForward = true;
        [SerializeField] private bool keepUpright = true;

        [Header("Swing Control Settings (A/D 좌우 흔들림)")]
        [Tooltip("A/D 키 입력 시 최대로 기울어질 각도 (예: 45도)")]
        [SerializeField] private float maxSwingAngle = 45.0f;

        [Tooltip("A/D 키를 누를 때 기울어지는 속도")]
        [SerializeField] private float swingSpeed = 4.0f;

        [Tooltip("키를 뗐을 때 중앙으로 다시 돌아오는 복귀 속도")]
        [SerializeField] private float returnSpeed = 3.0f;

        [Header("Exit Settings")]
        [Tooltip("레일이 끝났을 때 앞으로 튕겨나가는 힘의 배수 (기본 1.0 = 레일 속도 유지)")]
        [SerializeField] private float exitMomentumMultiplier = 1.0f;
        [Tooltip("레일에서 내릴 때 위로 살짝 띄워주는 힘")]
        [SerializeField] private float exitUpwardForce = 3.0f;

        [Header("Trigger Protection")]
        [SerializeField] private float retriggerCooldown = 0.5f;

        private int currentSplineIndex = 0;
        private Coroutine _moveCoroutine;
        private float _travelDirection = 1.0f;
        private float _nextAllowedTime = 0f;

        // 진입 시 자동 측정되는 오프셋 변수
        private Vector3 _dynamicLocalOffset = Vector3.zero;

        // 실시간 A/D 입력 및 흔들림 각도 변수
        private float _inputSwingDirection = 0f;
        private float _currentSwingAngle = 0f;

        private void Awake()
        {
            if (!objToMove) objToMove = this.gameObject;
        }

        private void Update()
        {
            if (_moveCoroutine != null)
            {
                // A/D 키 입력 실시간 체크 (-1: 왼쪽/반시계, +1: 오른쪽/시계)
                float moveInput = 0f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput += 1f;

                _inputSwingDirection = moveInput;
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

            // 방향 설정 모드 연산
            switch (directionMode)
            {
                case SplineDirectionMode.NodeOrder:
                    _travelDirection = 1.0f;
                    break;
                case SplineDirectionMode.NodeReverse:
                    _travelDirection = -1.0f;
                    break;
                case SplineDirectionMode.LookDirection:
                default:
                    float dotDir = Vector3.Dot(entryForward, worldTangent);
                    _travelDirection = (dotDir >= 0f) ? 1.0f : -1.0f;
                    break;
            }

            // 진입 시점 오프셋 역산
            if (autoCalculateInitialOffset)
            {
                Vector3 startWorldPos = splineContainer.transform.TransformPoint(startPos);
                Vector3 travelTangent = worldTangent * _travelDirection;
                
                Vector3 r, u;
                if (keepUpright)
                {
                    r = Vector3.Cross(Vector3.up, travelTangent).normalized;
                    u = Vector3.Cross(travelTangent, r).normalized;
                }
                else
                {
                    u = splineContainer.transform.TransformDirection(startUp).normalized;
                    r = Vector3.Cross(u, travelTangent).normalized;
                }

                Vector3 worldDiff = objToMove.transform.position - startWorldPos;

                _dynamicLocalOffset.x = Vector3.Dot(worldDiff, r);
                _dynamicLocalOffset.y = Vector3.Dot(worldDiff, u);
                _dynamicLocalOffset.z = Vector3.Dot(worldDiff, travelTangent);
            }
            else
            {
                _dynamicLocalOffset = hangingOffset;
            }

            _currentSwingAngle = 0f;
            float currentSplineLength = splineContainer.CalculateLength(currentSplineIndex);
            Vector3 lastWorldForward = objToMove.transform.forward;

            while (true)
            {
                yield return new WaitForFixedUpdate();

                float tStep = (_travelDirection * moveSpeed / currentSplineLength) * Time.fixedDeltaTime;
                currentT += tStep;

                bool isEndOfRail = (_travelDirection > 0 && currentT >= 1.0f) || (_travelDirection < 0 && currentT <= 0.0f);
                if (isEndOfRail)
                {
                    break;
                }

                float clampedT = Mathf.Clamp01(currentT);
                splineContainer.Evaluate(currentSplineIndex, clampedT, out var currentLocalPos, out var currentLocalTangent, out var currentLocalUp);

                Vector3 travelLocalTangent = (Vector3)currentLocalTangent * _travelDirection;
                Vector3 worldPos = splineContainer.transform.TransformPoint(currentLocalPos);
                Vector3 worldForward = splineContainer.transform.TransformDirection(travelLocalTangent).normalized;

                Vector3 worldRight, worldUp;
                if (keepUpright)
                {
                    worldRight = Vector3.Cross(Vector3.up, worldForward).normalized;
                    worldUp = Vector3.Cross(worldForward, worldRight).normalized;
                }
                else
                {
                    worldUp = splineContainer.transform.TransformDirection(currentLocalUp).normalized;
                    worldRight = Vector3.Cross(worldUp, worldForward).normalized;
                }

                lastWorldForward = worldForward;

                // ★ [A/D 좌우 흔들림 각도 연산]
                float targetAngle = _inputSwingDirection * maxSwingAngle;
                float currentSpeed = (_inputSwingDirection != 0f) ? swingSpeed : returnSpeed;

                // 목표 각도로 부드럽게 보정 (MoveTowards / Lerp)
                _currentSwingAngle = Mathf.MoveTowards(_currentSwingAngle, targetAngle, currentSpeed * maxSwingAngle * Time.fixedDeltaTime);

                // 진행 방향(worldForward) 축을 중심으로 진자 회전 쿼터니언 생성
                Quaternion swingRotation = Quaternion.AngleAxis(_currentSwingAngle, worldForward);

                // 기준 매달리기 오프셋 벡터 계산
                Vector3 baseOffsetVec = (worldRight * _dynamicLocalOffset.x)
                                      + (worldUp * _dynamicLocalOffset.y)
                                      + (worldForward * _dynamicLocalOffset.z);

                // 오프셋을 진행 방향 축 기준으로 회전시켜 실제 진자 위치 계산
                Vector3 rotatedOffsetVec = swingRotation * baseOffsetVec;
                Vector3 finalPos = worldPos + rotatedOffsetVec;

                // 최종 회전 (진자 회전과 레일 진행 방향 회전 합성)
                Quaternion baseRotation = Quaternion.LookRotation(worldForward, worldUp);
                Quaternion finalRotation = swingRotation * baseRotation;

                if (rb != null)
                {
                    rb.MovePosition(finalPos);
                    if (lookForward && worldForward != Vector3.zero)
                        rb.MoveRotation(finalRotation);
                }
                else
                {
                    objToMove.transform.position = finalPos;
                    if (lookForward && worldForward != Vector3.zero)
                        objToMove.transform.rotation = finalRotation;
                }
            }

            // 관성 적용 및 레일 이탈
            if (locomotion != null)
            {
                locomotion.SetRailMode(false);
                locomotion.SetBaseRotation(lastWorldForward);

                Vector3 launchVelocity = lastWorldForward * (moveSpeed * exitMomentumMultiplier) + (Vector3.up * exitUpwardForce);
                locomotion.ExternalLaunch(launchVelocity);
            }
            else if (rb != null)
            {
                rb.isKinematic = false;

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

*/

using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace _Project.Scripts.VisualScripting
{
    public class MoveAlongSplineAdvanced : ProcessBase
    {
        public enum SplineDirectionMode
        {
            [InspectorName("플레이어 시선 기준 (자동)")]
            LookDirection,
            [InspectorName("노드 순서대로 (0 -> 1)")]
            NodeOrder,
            [InspectorName("노드 역순대로 (1 -> 0)")]
            NodeReverse
        }

        [Header("Target Settings")]
        [SerializeField] private GameObject objToMove;
        [SerializeField] private SplineContainer splineContainer;

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 20.0f;
        [Tooltip("레일 이동 방향 모드 (시선 기준 / 노드 정순 / 노드 역순)")]
        [SerializeField] private SplineDirectionMode directionMode = SplineDirectionMode.NodeOrder;

        [Header("Hanging & Attachment Settings")]
        [Tooltip("체크 시 씬 뷰에 미리 배치해 둔 고리/플레이어의 초기 상대 위치와 각도를 자동으로 측정하여 딱 걸린 채로 이동합니다.")]
        [SerializeField] private bool autoCalculateInitialOffset = true;
        [Tooltip("autoCalculateInitialOffset이 false일 때 적용될 고정 오프셋")]
        [SerializeField] private Vector3 hangingOffset = new Vector3(0.0f, -1.6f, 0.0f);
        [SerializeField] private bool lookForward = true;
        [SerializeField] private bool keepUpright = true;

        [Header("Swing Control Settings (A/D 좌우 흔들림)")]
        [Tooltip("A/D 키 입력 시 최대로 기울어질 각도 (예: 45도)")]
        [SerializeField] private float maxSwingAngle = 45.0f;

        [Tooltip("A/D 키를 누를 때 기울어지는 속도")]
        [SerializeField] private float swingSpeed = 4.0f;

        [Tooltip("키를 뗐을 때 중앙으로 다시 돌아오는 복귀 속도")]
        [SerializeField] private float returnSpeed = 3.0f;

        [Header("Exit Settings")]
        [Tooltip("레일이 끝났을 때 앞으로 튕겨나가는 힘의 배수 (기본 1.0 = 레일 속도 유지)")]
        [SerializeField] private float exitMomentumMultiplier = 1.0f;
        [Tooltip("레일에서 내릴 때 위로 살짝 띄워주는 힘")]
        [SerializeField] private float exitUpwardForce = 3.0f;

        [Header("Trigger Protection")]
        [SerializeField] private float retriggerCooldown = 0.5f;

        private int currentSplineIndex = 0;
        private Coroutine _moveCoroutine;
        private float _travelDirection = 1.0f;
        private float _nextAllowedTime = 0f;

        // 진입 시 자동 측정되는 오프셋 변수
        private Vector3 _dynamicLocalOffset = Vector3.zero;

        // 실시간 A/D 입력 및 흔들림 각도 변수
        private float _inputSwingDirection = 0f;
        private float _currentSwingAngle = 0f;

        private void Awake()
        {
            if (!objToMove) objToMove = this.gameObject;
        }

        private void Update()
        {
            if (_moveCoroutine != null)
            {
                // A/D 키 입력 실시간 체크 (-1: 왼쪽/반시계, +1: 오른쪽/시계)
                float moveInput = 0f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput += 1f;

                _inputSwingDirection = moveInput;
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

            // 방향 설정 모드 연산
            switch (directionMode)
            {
                case SplineDirectionMode.NodeOrder:
                    _travelDirection = 1.0f;
                    break;
                case SplineDirectionMode.NodeReverse:
                    _travelDirection = -1.0f;
                    break;
                case SplineDirectionMode.LookDirection:
                default:
                    float dotDir = Vector3.Dot(entryForward, worldTangent);
                    _travelDirection = (dotDir >= 0f) ? 1.0f : -1.0f;
                    break;
            }

            // 진입 시점 오프셋 역산
            if (autoCalculateInitialOffset)
            {
                Vector3 startWorldPos = splineContainer.transform.TransformPoint(startPos);
                Vector3 travelTangent = worldTangent * _travelDirection;

                Vector3 r, u;
                if (keepUpright)
                {
                    r = Vector3.Cross(Vector3.up, travelTangent).normalized;
                    u = Vector3.Cross(travelTangent, r).normalized;
                }
                else
                {
                    u = splineContainer.transform.TransformDirection(startUp).normalized;
                    r = Vector3.Cross(u, travelTangent).normalized;
                }

                Vector3 worldDiff = objToMove.transform.position - startWorldPos;

                _dynamicLocalOffset.x = Vector3.Dot(worldDiff, r);
                _dynamicLocalOffset.y = Vector3.Dot(worldDiff, u);
                _dynamicLocalOffset.z = Vector3.Dot(worldDiff, travelTangent);
            }
            else
            {
                _dynamicLocalOffset = hangingOffset;
            }

            _currentSwingAngle = 0f;
            float currentSplineLength = splineContainer.CalculateLength(currentSplineIndex);
            Vector3 lastWorldForward = objToMove.transform.forward;

            while (true)
            {
                // ★ 프레임(Update) 주기에 맞춰 동기화 (끊김 방지 핵심!)
                yield return null;

                float tStep = (_travelDirection * moveSpeed / currentSplineLength) * Time.deltaTime;
                currentT += tStep;

                bool isEndOfRail = (_travelDirection > 0 && currentT >= 1.0f) || (_travelDirection < 0 && currentT <= 0.0f);
                if (isEndOfRail)
                {
                    break;
                }

                float clampedT = Mathf.Clamp01(currentT);
                splineContainer.Evaluate(currentSplineIndex, clampedT, out var currentLocalPos, out var currentLocalTangent, out var currentLocalUp);

                Vector3 travelLocalTangent = (Vector3)currentLocalTangent * _travelDirection;
                Vector3 worldPos = splineContainer.transform.TransformPoint(currentLocalPos);
                Vector3 worldForward = splineContainer.transform.TransformDirection(travelLocalTangent).normalized;

                Vector3 worldRight, worldUp;
                if (keepUpright)
                {
                    worldRight = Vector3.Cross(Vector3.up, worldForward).normalized;
                    worldUp = Vector3.Cross(worldForward, worldRight).normalized;
                }
                else
                {
                    worldUp = splineContainer.transform.TransformDirection(currentLocalUp).normalized;
                    worldRight = Vector3.Cross(worldUp, worldForward).normalized;
                }

                lastWorldForward = worldForward;

                // A/D 좌우 흔들림 각도 연산
                float targetAngle = _inputSwingDirection * maxSwingAngle;
                float currentSpeed = (_inputSwingDirection != 0f) ? swingSpeed : returnSpeed;

                _currentSwingAngle = Mathf.MoveTowards(_currentSwingAngle, targetAngle, currentSpeed * maxSwingAngle * Time.deltaTime);

                Quaternion swingRotation = Quaternion.AngleAxis(_currentSwingAngle, worldForward);

                Vector3 baseOffsetVec = (worldRight * _dynamicLocalOffset.x)
                                      + (worldUp * _dynamicLocalOffset.y)
                                      + (worldForward * _dynamicLocalOffset.z);

                Vector3 rotatedOffsetVec = swingRotation * baseOffsetVec;
                Vector3 finalPos = worldPos + rotatedOffsetVec;

                Quaternion baseRotation = Quaternion.LookRotation(worldForward, worldUp);
                Quaternion finalRotation = swingRotation * baseRotation;

                // ★ 모니터 화면 프레임과 칼같이 동기화되도록 Transform 직접 이동
                objToMove.transform.position = finalPos;
                if (lookForward && worldForward != Vector3.zero)
                    objToMove.transform.rotation = finalRotation;

                if (rb != null)
                {
                    rb.position = finalPos;
                    rb.rotation = finalRotation;
                }
            }

            // 관성 적용 및 레일 이탈
            if (locomotion != null)
            {
                locomotion.SetRailMode(false);
                locomotion.SetBaseRotation(lastWorldForward);

                Vector3 launchVelocity = lastWorldForward * (moveSpeed * exitMomentumMultiplier) + (Vector3.up * exitUpwardForce);
                locomotion.ExternalLaunch(launchVelocity);
            }
            else if (rb != null)
            {
                rb.isKinematic = false;

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