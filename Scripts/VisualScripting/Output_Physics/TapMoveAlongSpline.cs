using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace _Project.Scripts.VisualScripting
{
    public class TapMoveAlongSpline : ProcessBase
    {
        [Header("Target Settings")]
        [SerializeField] private GameObject objToMove;
        [SerializeField] private SplineContainer splineContainer;

        [Header("Animation Settings")]
        [SerializeField] private Animator animator;
        [Tooltip("진입 시 재생할 State 이름 (예: Idle 또는 Base Layer.Idle)")]
        [SerializeField] private string idleStateName = "Idle";
        [Tooltip("State 전환 대신 Trigger를 쓸 경우 입력")]
        [SerializeField] private string idleTriggerName = "";
        [Tooltip("Float 파라미터(Speed 등)를 0으로 만들고 싶을 때 파라미터 이름")]
        [SerializeField] private string speedFloatName = "Speed";
        [Tooltip("Bool 파라미터(IsGrounded 등)를 true로 만들고 싶을 때 파라미터 이름")]
        [SerializeField] private string groundedBoolName = "IsGrounded";

        [Header("Tap Movement Settings")]
        [SerializeField] private KeyCode tapKey = KeyCode.Space;
        [SerializeField] private int targetPressCount = 20;
        [SerializeField] private float moveSmoothSpeed = 3.0f;

        [Header("Tilt (교대 기울기) Settings")]
        [SerializeField] private float jitterAngle = 20.0f;
        [SerializeField] private float jitterDamping = 15.0f;

        [Header("Hanging Settings")]
        [SerializeField] private Vector3 hangingOffset = new Vector3(0.0f, 0.0f, 0.0f);

        [Header("Wall Alignment Settings (벽 정렬)")]
        [SerializeField] private bool alignToWall = true;
        [SerializeField] private float wallDetectDistance = 3.0f;
        [SerializeField] private LayerMask wallLayer = ~0;
        [SerializeField] private Vector3 defaultWallNormal = Vector3.forward;

        [Header("Locomotion & Physics Control")]
        [SerializeField] private bool disableCharacterController = true;
        [SerializeField] private bool setRigidbodyKinematic = true;
        [Tooltip("스플라인 진입 시 이동 스크립트를 꺼서 애니메이션/이동 덮어쓰기를 방지 (필수 체크 권장)")]
        [SerializeField] private bool disableLocomotionScript = true; // [기본값 true로 변경]

        [Header("Exit Jump Target Settings")]
        [SerializeField] private bool enableExitJump = true;
        [SerializeField] private Transform jumpTargetTransform;
        [SerializeField] private float jumpArcHeight = 4.0f;
        [SerializeField] private float jumpDuration = 0.6f;
        [SerializeField] private float fallbackForwardForce = 10.0f;
        [SerializeField] private float fallbackUpwardForce = 5.0f;

        [Header("Trigger Protection")]
        [SerializeField] private float retriggerCooldown = 0.8f;

        private Coroutine _moveCoroutine;
        private int _currentPressCount = 0;
        private float _currentProgress = 0f;
        private float _targetProgress = 0f;

        private float _currentJitterZ = 0f;
        private float _targetJitterZ = 0f;
        private float _tiltDirection = -1f;

        private bool _isTappingActive = false;
        private float _nextAllowedTime = 0f;
        private Vector3 _detectedWallNormal = Vector3.forward;

        private void Awake()
        {
            if (!objToMove) objToMove = this.gameObject;
            if (!animator && objToMove) animator = objToMove.GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (_isTappingActive && _moveCoroutine != null)
            {
                if (Input.GetKeyDown(tapKey))
                {
                    if (_currentPressCount < targetPressCount)
                    {
                        _currentPressCount++;
                        _targetProgress = (float)_currentPressCount / targetPressCount;

                        _tiltDirection *= -1f;
                        _targetJitterZ = _tiltDirection * jitterAngle;
                    }
                }
            }
        }

        public override void Execute()
        {
            IsOn = false;

            if (Time.time < _nextAllowedTime)
            {
                IsOn = true;
                return;
            }

            if (objToMove == null || splineContainer == null)
            {
                IsOn = true;
                return;
            }

            StartTapMove();
        }

        private void StartTapMove()
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(TapMoveRoutine());
        }

        private IEnumerator TapMoveRoutine()
        {
            Rigidbody rb = objToMove.GetComponent<Rigidbody>();
            CharacterController cc = objToMove.GetComponent<CharacterController>();
            PlayerLocomotion locomotion = objToMove.GetComponent<PlayerLocomotion>();

            if (!animator && objToMove) animator = objToMove.GetComponentInChildren<Animator>();

            // --- 1. 스크립트 및 물리 컴포넌트 비활성화 (애니메이션 설정보다 먼저 수행) ---
            bool wasCcEnabled = cc != null && cc.enabled;
            if (cc != null && disableCharacterController) cc.enabled = false;

            bool wasKinematic = rb != null && rb.isKinematic;
            if (rb != null && setRigidbodyKinematic) rb.isKinematic = true;

            if (locomotion != null && disableLocomotionScript) locomotion.enabled = false;

            // --- 2. 애니메이션 제어 (강제 적용) ---
            if (animator != null)
            {
                // 파라미터 리셋
                if (!string.IsNullOrEmpty(speedFloatName)) animator.SetFloat(speedFloatName, 0f);
                if (!string.IsNullOrEmpty(groundedBoolName)) animator.SetBool(groundedBoolName, true);

                // 애니메이션 즉시 강제 실행
                if (!string.IsNullOrEmpty(idleTriggerName))
                {
                    animator.SetTrigger(idleTriggerName);
                }
                else if (!string.IsNullOrEmpty(idleStateName))
                {
                    // Play()로 해당 State를 즉시 0초 시점부터 재생하도록 강제
                    animator.Play(idleStateName, 0, 0f);
                }
            }

            // --- 3. 벽 감지 및 위치 초기화 ---
            DetectWallNormal();

            int splineIndex = 0;

            if (splineContainer.Splines.Count > 0)
            {
                float3 localPlayerPos = splineContainer.transform.InverseTransformPoint(objToMove.transform.position);
                SplineUtility.GetNearestPoint(splineContainer.Splines[splineIndex], localPlayerPos, out float3 _, out float nearestT);

                _currentProgress = nearestT;
                _targetProgress = nearestT;
                _currentPressCount = Mathf.RoundToInt(nearestT * targetPressCount);
            }
            else
            {
                _currentProgress = 0f;
                _targetProgress = 0f;
                _currentPressCount = 0;
            }

            _currentJitterZ = 0f;
            _targetJitterZ = 0f;
            _tiltDirection = -1f;
            _isTappingActive = true;

            splineContainer.Evaluate(splineIndex, _currentProgress, out var initLocalPos, out var initLocalTangent, out var initLocalUp);
            Vector3 initWorldPos = splineContainer.transform.TransformPoint((Vector3)initLocalPos);
            Vector3 initWorldTangent = splineContainer.transform.TransformDirection((Vector3)initLocalTangent).normalized;

            Quaternion initRotation = CalculateCustomAxisRotation(initWorldTangent);

            if (rb != null && rb.isKinematic)
            {
                rb.MovePosition(initWorldPos);
                rb.MoveRotation(initRotation);
            }
            else
            {
                objToMove.transform.position = initWorldPos;
                objToMove.transform.rotation = initRotation;
            }

            Vector3 lastWorldTangent = initWorldTangent;

            // --- 4. 스플라인 이동 루프 ---
            while (true)
            {
                yield return new WaitForFixedUpdate();

                // 이동 동안에도 Speed 파라미터를 지속적으로 0으로 유지
                if (animator != null && !string.IsNullOrEmpty(speedFloatName))
                {
                    animator.SetFloat(speedFloatName, 0f);
                }

                _currentProgress = Mathf.MoveTowards(_currentProgress, _targetProgress, moveSmoothSpeed * Time.fixedDeltaTime);

                splineContainer.Evaluate(splineIndex, _currentProgress, out var localPos, out var localTangent, out var localUp);

                Vector3 worldPos = splineContainer.transform.TransformPoint((Vector3)localPos);
                Vector3 worldTangent = splineContainer.transform.TransformDirection((Vector3)localTangent).normalized;

                if (worldTangent != Vector3.zero) lastWorldTangent = worldTangent;

                Quaternion baseRotation = CalculateCustomAxisRotation(worldTangent);

                _currentJitterZ = Mathf.Lerp(_currentJitterZ, _targetJitterZ, Time.fixedDeltaTime * jitterDamping);

                Quaternion finalRotation = baseRotation * Quaternion.Euler(0f, 0f, _currentJitterZ);

                Vector3 playerY = worldTangent;
                Vector3 playerX = Vector3.Cross(playerY, _detectedWallNormal).normalized;
                Vector3 playerZ = Vector3.Cross(playerX, playerY).normalized;

                Vector3 finalPos = worldPos
                                 + (playerX * hangingOffset.x)
                                 + (playerZ * hangingOffset.y)
                                 + (playerY * hangingOffset.z);

                if (rb != null && rb.isKinematic)
                {
                    rb.MovePosition(finalPos);
                    rb.MoveRotation(finalRotation);
                }
                else
                {
                    objToMove.transform.position = finalPos;
                    objToMove.transform.rotation = finalRotation;
                }

                if (_currentProgress >= 1.0f && _targetProgress >= 1.0f)
                {
                    break;
                }
            }

            _isTappingActive = false;

            // --- 5. 점프 연출 ---
            if (enableExitJump)
            {
                if (jumpTargetTransform != null)
                {
                    Vector3 startJumpPos = objToMove.transform.position;
                    Vector3 targetJumpPos = jumpTargetTransform.position;
                    Quaternion startJumpRot = objToMove.transform.rotation;
                    float elapsed = 0f;

                    while (elapsed < jumpDuration)
                    {
                        yield return new WaitForFixedUpdate();
                        elapsed += Time.fixedDeltaTime;
                        float t = Mathf.Clamp01(elapsed / jumpDuration);

                        Vector3 currentPos = Vector3.Lerp(startJumpPos, targetJumpPos, t);
                        float arc = Mathf.Sin(t * Mathf.PI) * jumpArcHeight;
                        currentPos.y += arc;

                        Vector3 jumpDir = (targetJumpPos - startJumpPos);
                        jumpDir.y = 0;
                        Quaternion targetJumpRot = jumpDir.sqrMagnitude > 0.01f
                            ? Quaternion.LookRotation(jumpDir.normalized, Vector3.up)
                            : startJumpRot;

                        Quaternion currentRot = Quaternion.Slerp(startJumpRot, targetJumpRot, t * 2f);

                        if (rb != null && rb.isKinematic)
                        {
                            rb.MovePosition(currentPos);
                            rb.MoveRotation(currentRot);
                        }
                        else
                        {
                            objToMove.transform.position = currentPos;
                            objToMove.transform.rotation = currentRot;
                        }
                    }

                    Vector3 finalDir = (targetJumpPos - startJumpPos);
                    finalDir.y = 0;
                    Quaternion finalLandRot = finalDir.sqrMagnitude > 0.01f
                        ? Quaternion.LookRotation(finalDir.normalized, Vector3.up)
                        : objToMove.transform.rotation;

                    if (rb != null && rb.isKinematic)
                    {
                        rb.MovePosition(targetJumpPos);
                        rb.MoveRotation(finalLandRot);
                    }
                    else
                    {
                        objToMove.transform.position = targetJumpPos;
                        objToMove.transform.rotation = finalLandRot;
                    }
                }
                else
                {
                    Vector3 jumpVelocity = lastWorldTangent * fallbackForwardForce + Vector3.up * fallbackUpwardForce;
                    if (locomotion != null) locomotion.ExternalLaunch(jumpVelocity);
                    else if (rb != null) rb.linearVelocity = jumpVelocity;
                }
            }

            // --- 6. 상태 복원 ---
            if (locomotion != null && disableLocomotionScript) locomotion.enabled = true;
            if (cc != null && disableCharacterController) cc.enabled = wasCcEnabled;
            if (rb != null && setRigidbodyKinematic) rb.isKinematic = wasKinematic;

            _nextAllowedTime = Time.time + retriggerCooldown;
            _moveCoroutine = null;
            IsOn = true;
        }

        private void DetectWallNormal()
        {
            if (!alignToWall) return;

            Vector3 origin = objToMove.transform.position;
            Vector3[] directions = { objToMove.transform.forward, -objToMove.transform.forward, -objToMove.transform.right, objToMove.transform.right };

            float minDistance = float.MaxValue;
            Vector3 bestNormal = defaultWallNormal;

            foreach (var dir in directions)
            {
                if (Physics.Raycast(origin, dir, out RaycastHit hit, wallDetectDistance, wallLayer))
                {
                    if (hit.distance < minDistance)
                    {
                        minDistance = hit.distance;
                        bestNormal = hit.normal;
                    }
                }
            }

            _detectedWallNormal = bestNormal;
        }

        private Quaternion CalculateCustomAxisRotation(Vector3 splineTangent)
        {
            if (splineTangent == Vector3.zero) return objToMove.transform.rotation;

            Vector3 playerY = splineTangent.normalized;

            if (alignToWall)
            {
                Vector3 wallToWallDirection = -_detectedWallNormal;
                Vector3 playerX = Vector3.Cross(playerY, wallToWallDirection).normalized;

                if (playerX.sqrMagnitude < 0.001f)
                {
                    playerX = Vector3.Cross(playerY, Vector3.up).normalized;
                }

                Vector3 playerZ = Vector3.Cross(playerX, playerY).normalized;
                return Quaternion.LookRotation(playerZ, playerY);
            }
            else
            {
                Vector3 playerX = Vector3.Cross(playerY, Vector3.down).normalized;
                if (playerX.sqrMagnitude < 0.001f) playerX = Vector3.Cross(playerY, Vector3.forward).normalized;
                Vector3 playerZ = Vector3.Cross(playerX, playerY).normalized;
                return Quaternion.LookRotation(playerZ, playerY);
            }
        }
    }
}