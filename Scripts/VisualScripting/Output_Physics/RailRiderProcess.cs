using System.Collections;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace _Project.Scripts.VisualScripting
{
    public class RailRideProcess : ProcessBase
    {
        [Header("Target Settings")]
        [SerializeField] private GameObject playerObject;

        [Header("Rail Spline Settings")]
        [SerializeField] private SplineContainer splineContainer;

        [Header("Movement Settings")]
        [SerializeField] private float forwardSpeed = 25f;

        [Tooltip("체크하면 트리거 접촉 시 무조건 레일의 시작점(0%)을 향해 주행을 시작합니다.")]
        [SerializeField] private bool startAtBeginning = true;

        [Tooltip("처음 레일에 닿았을 때 레일 중앙으로 부드럽게 빨려 들어가는 시간 (초)")]
        [SerializeField] private float entryDuration = 0.25f;

        [Header("Adjacent Rails (옆 레일 연결)")]
        [SerializeField] private RailRideProcess leftAdjacentRail;
        [SerializeField] private RailRideProcess rightAdjacentRail;

        [Header("Transfer Jump Settings")]
        [SerializeField] private float jumpHeight = 3.0f;
        [SerializeField] private float jumpDuration = 0.4f;

        [Header("Transfer Validation")]
        [SerializeField] private float maxTransferDistance = 6.0f;
        [SerializeField] private float forwardAllowDelta = 3.0f;

        private Coroutine _railCoroutine;
        private PlayerLocomotion _playerLocomotion;
        private Rigidbody _playerRb;
        private Animator _playerAnim;

        private float _currentLocalDistance = 0f;
        private float _localSplineLength = 0f;

        private bool _isJumpingToAdjacent = false;
        private float _jumpTimer = 0f;

        private bool _isEnteringRail = false;
        private float _entryTimer = 0f;
        private Vector3 _startLocalOffsetVec = Vector3.zero;

        private bool _originalRootMotionState = false;

        private void Awake()
        {
            if (playerObject == null) playerObject = this.gameObject;
        }

        public override void Execute()
        {
            IsOn = true;

            if (playerObject != null && splineContainer != null)
            {
                _playerLocomotion = playerObject.GetComponent<PlayerLocomotion>();
                _playerRb = playerObject.GetComponent<Rigidbody>();
                _playerAnim = playerObject.GetComponent<Animator>();

                _localSplineLength = splineContainer.Spline.GetLength();
                Vector3 targetWorldPos;

                if (startAtBeginning)
                {
                    _currentLocalDistance = 0f;
                    splineContainer.Evaluate(0f, out float3 localPos, out _, out _);
                    targetWorldPos = splineContainer.transform.TransformPoint(localPos);
                }
                else
                {
                    float3 localPoint = splineContainer.transform.InverseTransformPoint(playerObject.transform.position);
                    SplineUtility.GetNearestPoint(splineContainer.Spline, localPoint, out float3 nearestLocalPos, out float tProgress);

                    // ★수정됨 1: tProgress(0~1)를 실제 곡선상의 물리적 거리(Distance)로 정확하게 변환합니다.
                    _currentLocalDistance = splineContainer.Spline.ConvertIndexUnit(tProgress, PathIndexUnit.Normalized, PathIndexUnit.Distance);

                    targetWorldPos = splineContainer.transform.TransformPoint(nearestLocalPos);
                }

                _startLocalOffsetVec = playerObject.transform.position - targetWorldPos;

                StartRailRide(_currentLocalDistance, true);
            }
            else
            {
                Debug.LogWarning($"{name}: 세팅이 불완전합니다.");
                IsOn = false;
            }
        }

        private void StartRailRide(float startDistance, bool isFirstEntry)
        {
            if (_railCoroutine != null) StopCoroutine(_railCoroutine);

            if (isFirstEntry)
            {
                if (_playerLocomotion != null)
                {
                    _playerLocomotion.SetRailMode(true);
                    _playerLocomotion.enabled = false;
                }

                if (_playerAnim != null)
                {
                    _originalRootMotionState = _playerAnim.applyRootMotion;
                    _playerAnim.applyRootMotion = false;
                }

                if (_playerRb != null) _playerRb.isKinematic = true;

                _isEnteringRail = true;
                _entryTimer = 0f;
            }

            _currentLocalDistance = startDistance;
            _railCoroutine = StartCoroutine(RailRideRoutine());
        }

        private IEnumerator RailRideRoutine()
        {
            float zScale = splineContainer.transform.lossyScale.z;
            float localSpeed = forwardSpeed / (zScale == 0 ? 1f : zScale);

            while (_currentLocalDistance < _localSplineLength)
            {
                _currentLocalDistance += localSpeed * Time.deltaTime;

                // 거리가 전체 길이를 넘지 않도록 제한
                _currentLocalDistance = Mathf.Clamp(_currentLocalDistance, 0f, _localSplineLength);

                // ★수정됨 2: 누적된 '실제 거리'를 다시 Evaluate를 위한 보간값 t(0~1)로 변환합니다.
                // 이렇게 하면 스플라인 점 간격에 상관없이 항상 일정한 속도로 이동합니다.
                float tProgress = splineContainer.Spline.ConvertIndexUnit(_currentLocalDistance, PathIndexUnit.Distance, PathIndexUnit.Normalized);

                splineContainer.Evaluate(tProgress, out float3 localPos, out float3 localTangent, out float3 localUp);

                Vector3 worldCenterPos = splineContainer.transform.TransformPoint(localPos);
                Vector3 worldForward = splineContainer.transform.TransformDirection(localTangent).normalized;
                Vector3 worldUp = splineContainer.transform.TransformDirection(localUp).normalized;

                HandleLaneInput();

                if (!IsOn) yield break;

                Vector3 finalLocalOffset = Vector3.zero;
                float additionalJumpY = 0f;

                if (_isEnteringRail)
                {
                    _entryTimer += Time.deltaTime;
                    float t = Mathf.Clamp01(_entryTimer / entryDuration);
                    finalLocalOffset = Vector3.Lerp(_startLocalOffsetVec, Vector3.zero, t);
                    if (t >= 1f) _isEnteringRail = false;
                }
                else if (_isJumpingToAdjacent)
                {
                    _jumpTimer += Time.deltaTime;
                    float t = Mathf.Clamp01(_jumpTimer / jumpDuration);
                    finalLocalOffset = Vector3.Lerp(_startLocalOffsetVec, Vector3.zero, t);
                    additionalJumpY = 4f * jumpHeight * t * (1f - t);
                    if (t >= 1f) _isJumpingToAdjacent = false;
                }

                playerObject.transform.position = worldCenterPos + finalLocalOffset + (Vector3.up * additionalJumpY);

                if (worldForward != Vector3.zero)
                {
                    playerObject.transform.rotation = Quaternion.LookRotation(worldForward, worldUp);
                }

                yield return null;
            }

            FinishRailRide(true);
        }

        private void HandleLaneInput()
        {
            if (_isJumpingToAdjacent || _isEnteringRail) return;

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                if (leftAdjacentRail != null && IsTargetRailValid(leftAdjacentRail))
                    SwitchToAdjacentRail(leftAdjacentRail);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                if (rightAdjacentRail != null && IsTargetRailValid(rightAdjacentRail))
                    SwitchToAdjacentRail(rightAdjacentRail);
            }
        }

        private bool IsTargetRailValid(RailRideProcess targetRail)
        {
            float3 localPoint = targetRail.splineContainer.transform.InverseTransformPoint(playerObject.transform.position);
            SplineUtility.GetNearestPoint(targetRail.splineContainer.Spline, localPoint, out float3 nearestLocalPos, out float tProgress);
            Vector3 targetWorldPos = targetRail.splineContainer.transform.TransformPoint(nearestLocalPos);

            Vector3 diff = targetWorldPos - playerObject.transform.position;
            float forwardDelta = Vector3.Dot(diff, playerObject.transform.forward);

            if (Mathf.Abs(forwardDelta) > forwardAllowDelta) return false;
            if (Vector3.Distance(playerObject.transform.position, targetWorldPos) > maxTransferDistance) return false;

            return true;
        }

        private void SwitchToAdjacentRail(RailRideProcess nextRail)
        {
            if (_railCoroutine != null) StopCoroutine(_railCoroutine);
            _railCoroutine = null;
            IsOn = false;
            _isJumpingToAdjacent = false;
            _isEnteringRail = false;

            nextRail.ReceivePlayerFromAdjacent(playerObject, _playerLocomotion, playerObject.transform.position);
        }

        public void ReceivePlayerFromAdjacent(GameObject player, PlayerLocomotion locomotion, Vector3 incomingPosition)
        {
            IsOn = true;
            playerObject = player;
            _playerLocomotion = locomotion;
            _playerRb = player.GetComponent<Rigidbody>();
            _playerAnim = player.GetComponent<Animator>();

            _localSplineLength = splineContainer.Spline.GetLength();
            float3 localPoint = splineContainer.transform.InverseTransformPoint(incomingPosition);
            SplineUtility.GetNearestPoint(splineContainer.Spline, localPoint, out float3 nearestLocalPos, out float tProgress);

            // ★수정됨 3: 옆 레일로 옮겨갈 때도 정확한 진입 지점의 거리로 변환합니다.
            _currentLocalDistance = splineContainer.Spline.ConvertIndexUnit(tProgress, PathIndexUnit.Normalized, PathIndexUnit.Distance);

            Vector3 projectedCenterPos = splineContainer.transform.TransformPoint(nearestLocalPos);

            _startLocalOffsetVec = incomingPosition - projectedCenterPos;
            _isJumpingToAdjacent = true;
            _jumpTimer = 0f;

            StartRailRide(_currentLocalDistance, false);
        }

        private void FinishRailRide(bool shouldReleasePlayer)
        {
            _railCoroutine = null;
            _isJumpingToAdjacent = false;
            _isEnteringRail = false;

            if (shouldReleasePlayer)
            {
                if (_playerLocomotion != null)
                {
                    _playerLocomotion.SetRailMode(false);
                    _playerLocomotion.enabled = true;
                }
                if (_playerAnim != null)
                {
                    _playerAnim.applyRootMotion = _originalRootMotionState;
                }
                if (_playerRb != null) _playerRb.isKinematic = false;

                Vector3 flatForward = playerObject.transform.forward;
                flatForward.y = 0;
                if (flatForward != Vector3.zero)
                {
                    playerObject.transform.rotation = Quaternion.LookRotation(flatForward);
                }
            }

            IsOn = false;
        }
    }
}