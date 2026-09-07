/*
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 일반 이동 오브젝트(발판, 고리 등)에 플레이어를 붙여 이동시키고,
    /// 지정한 내리기 키를 누르면 관성 탈출 또는 점프 포탄 궤적(Lerp)으로 날아가는 통합 Process/Output 컴포넌트입니다.
    /// </summary>
    public class AttachToMovingObject : ProcessBase
    {
        public enum DismountType
        {
            [InspectorName("이동 관성 반영 (Momentum)")]
            Momentum,
            [InspectorName("지정된 위치로 점프 (Jump To Target)")]
            JumpToTarget
        }

        [Header("Target References")]
        [Tooltip("매달릴 플레이어 오브젝트 (미지정 시 현재 오브젝트)")]
        [SerializeField] private GameObject playerObj;

        [Tooltip("플레이어가 탑승/매달릴 이동 대상 (발판, 고리 등)")]
        [SerializeField] private Transform movingTarget;

        [Header("Attachment Settings")]
        [Tooltip("체크 시 스크립트 실행 순간의 플레이어와 대상 간의 위치/각도 오프셋을 자동으로 측정하여 유지합니다.")]
        [SerializeField] private bool autoCalculateInitialOffset = true;

        [Tooltip("autoCalculateInitialOffset이 false일 때 사용되는 고정 로컬 오프셋")]
        [SerializeField] private Vector3 localOffset = Vector3.zero;

        [Tooltip("이동 대상의 회전도 같이 따라갈지 여부")]
        [SerializeField] private bool followTargetRotation = true;

        [Header("Dismount Key Settings")]
        [Tooltip("플레이어가 지정한 키를 눌러 중간에 내릴 수 있는지 여부")]
        [SerializeField] private bool canDismountWithKey = true;

        [Tooltip("내릴 때 사용할 키 (기본값: Space)")]
        [SerializeField] private KeyCode dismountKey = KeyCode.Space;

        [Header("Dismount Mode Settings")]
        [Tooltip("내릴 때 탈출 방식 선택 (관성 유지 / 지정 위치 점프)")]
        [SerializeField] private DismountType dismountMode = DismountType.Momentum;

        [Header("Mode 1: Momentum Settings")]
        [Tooltip("내릴 때 이동 대상의 속도를 얼마나 전달받을지 (관성 배수)")]
        [SerializeField] private float momentumMultiplier = 1.0f;

        [Tooltip("내릴 때 위로 살짝 점프/띄워주는 힘")]
        [SerializeField] private float upwardForce = 3.0f;

        [Header("Mode 2: Jump To Target Settings")]
        [Tooltip("점프해 도착할 목표 Transform (우선 적용)")]
        [SerializeField] private Transform jumpTargetTransform;

        [Tooltip("jumpTargetTransform이 없을 때 사용할 고정 월드 좌표")]
        [SerializeField] private Vector3 jumpTargetPosition;

        [Tooltip("점프 시 최고 높이 (포탄 궤적 곡선 오프셋)")]
        [SerializeField] private float jumpHeight = 3.0f;

        [Tooltip("목표 지점까지 날아가는 데 걸리는 총 시간 (초)")]
        [SerializeField] private float jumpDuration = 1.0f;

        [Tooltip("점프하는 동안 진행 방향을 바라볼지 여부")]
        [SerializeField] private bool lookAtDestination = true;

        private Coroutine _attachCoroutine;
        private Vector3 _lastTargetPosition;
        private Vector3 _currentTargetVelocity;
        private Vector3 _dynamicLocalOffset = Vector3.zero;

        private void Awake()
        {
            if (!playerObj) playerObj = this.gameObject;
        }

        public override void Execute()
        {
            IsOn = false;

            if (playerObj == null || movingTarget == null)
            {
                IsOn = true;
                return;
            }

            PlayerLocomotion locomotion = playerObj.GetComponent<PlayerLocomotion>();

            if (locomotion != null && locomotion.isRidingRail)
            {
                IsOn = true;
                return;
            }

            StartAttachment();
        }

        private void StartAttachment()
        {
            if (_attachCoroutine != null) StopCoroutine(_attachCoroutine);
            _attachCoroutine = StartCoroutine(AttachRoutine());
        }

        private IEnumerator AttachRoutine()
        {
            PlayerLocomotion locomotion = playerObj.GetComponent<PlayerLocomotion>();
            Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();

            // 1. 실행 당시 상대적 위치 오프셋 측정
            if (autoCalculateInitialOffset)
            {
                _dynamicLocalOffset = movingTarget.InverseTransformPoint(playerObj.transform.position);
            }
            else
            {
                _dynamicLocalOffset = localOffset;
            }

            // 2. 레일 모드 활성화 (조작/중력 잠금)
            if (locomotion != null) locomotion.SetRailMode(true);
            else if (playerRb != null) playerRb.isKinematic = true;

            _lastTargetPosition = movingTarget.position;

            while (movingTarget != null)
            {
                // 내리기 키 감지
                if (canDismountWithKey && Input.GetKeyDown(dismountKey))
                {
                    break;
                }

                yield return new WaitForFixedUpdate();

                // 3. 실시간 이동 속도 연산 (관성 탈출용)
                _currentTargetVelocity = (movingTarget.position - _lastTargetPosition) / Time.fixedDeltaTime;
                _lastTargetPosition = movingTarget.position;

                // 4. 오프셋 위치/회전 동기화
                Vector3 targetPos = movingTarget.TransformPoint(_dynamicLocalOffset);
                Quaternion targetRot = followTargetRotation ? movingTarget.rotation : playerObj.transform.rotation;

                if (playerRb != null)
                {
                    playerRb.MovePosition(targetPos);
                    playerRb.MoveRotation(targetRot);
                }
                else
                {
                    playerObj.transform.position = targetPos;
                    playerObj.transform.rotation = targetRot;
                }
            }

            // 5. 탈출 처리 (관성 발사 or 보내주신 Lerp 점프 루틴 실행)
            yield return StartCoroutine(DismountRoutine(locomotion, playerRb));

            _attachCoroutine = null;
            IsOn = true;
        }

        public void Detach()
        {
            if (_attachCoroutine != null)
            {
                StopCoroutine(_attachCoroutine);
                _attachCoroutine = null;

                PlayerLocomotion locomotion = playerObj.GetComponent<PlayerLocomotion>();
                Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();
                
                StartCoroutine(DismountRoutine(locomotion, playerRb));
                IsOn = true;
            }
        }

        private IEnumerator DismountRoutine(PlayerLocomotion locomotion, Rigidbody playerRb)
        {
            // 1. 레일 모드 안전하게 해제
            if (locomotion != null)
            {
                locomotion.SetRailMode(false);
            }

            // Mode 1: 관성 반영하여 튀어나가기
            if (dismountMode == DismountType.Momentum)
            {
                Vector3 launchVelocity = (_currentTargetVelocity * momentumMultiplier) + (Vector3.up * upwardForce);

                if (locomotion != null)
                {
                    if (_currentTargetVelocity.sqrMagnitude > 0.1f)
                    {
                        locomotion.SetBaseRotation(_currentTargetVelocity.normalized);
                    }
                    locomotion.ExternalLaunch(launchVelocity);
                }
                else if (playerRb != null)
                {
                    playerRb.isKinematic = false;
                    playerRb.linearVelocity = launchVelocity;
                }
            }
            // Mode 2: 보내주신 검증된 '포탄 궤적 Jump Routine' 실행!
            else if (dismountMode == DismountType.JumpToTarget)
            {
                Vector3 startPos = playerObj.transform.position;
                Vector3 endPos = jumpTargetTransform != null ? jumpTargetTransform.position : jumpTargetPosition;

                // 시선 회전 적용
                Vector3 jumpDirection = new Vector3(endPos.x - startPos.x, 0f, endPos.z - startPos.z);
                if (lookAtDestination && jumpDirection.sqrMagnitude > 0.01f)
                {
                    if (locomotion != null)
                    {
                        locomotion.SetBaseRotation(jumpDirection.normalized);
                    }
                    else
                    {
                        playerObj.transform.rotation = Quaternion.LookRotation(jumpDirection.normalized);
                    }
                }

                bool wasKinematic = playerRb != null ? playerRb.isKinematic : false;
                if (playerRb != null) playerRb.isKinematic = true;

                float timer = 0f;

                while (timer < jumpDuration)
                {
                    yield return new WaitForFixedUpdate();

                    timer += Time.fixedDeltaTime;
                    float t = Mathf.Clamp01(timer / jumpDuration);

                    // 포탄 궤적 곡선 공식 (t = 0.5일 때 최고점 jumpHeight)
                    float heightOffset = 4f * jumpHeight * t * (1f - t);

                    Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
                    currentPos.y += heightOffset;

                    if (playerRb != null)
                    {
                        playerRb.MovePosition(currentPos);
                    }
                    else
                    {
                        playerObj.transform.position = currentPos;
                    }
                }

                // 정확한 목표지점 도달 보장 및 Kinematic 복원
                if (playerRb != null)
                {
                    playerRb.MovePosition(endPos);
                    playerRb.isKinematic = wasKinematic;
                }
                else
                {
                    playerObj.transform.position = endPos;
                }

                // 착지 후 지면 인식 및 속도 보정
                if (locomotion != null)
                {
                    locomotion.ExternalLaunch(Vector3.down * 2.0f);
                }
            }
        }

        /// <summary>
        /// 외부에서 목표 지점을 넘겨주며 즉시 점프 탈출시킬 때 사용하는 메서드
        /// </summary>
        public void DismountToTarget(Transform targetTransform, float height = 3.0f, float duration = 1.0f)
        {
            dismountMode = DismountType.JumpToTarget;
            jumpTargetTransform = targetTransform;
            jumpHeight = height;
            jumpDuration = duration;
            Detach();
        }
    }
}

*/


using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 일반 이동 오브젝트(발판, 고리 등)에 플레이어를 붙여 이동시키고,
    /// 지정한 내리기 키를 누르면 관성 탈출 또는 점프 포탄 궤적(Lerp)으로 날아가는 통합 Process/Output 컴포넌트입니다.
    /// </summary>
    public class AttachToMovingObject : ProcessBase
    {
        public enum DismountType
        {
            [InspectorName("이동 관성 반영 (Momentum)")]
            Momentum,
            [InspectorName("지정된 위치로 점프 (Jump To Target)")]
            JumpToTarget
        }

        [Header("Target References")]
        [Tooltip("매달릴 플레이어 오브젝트 (미지정 시 현재 오브젝트)")]
        [SerializeField] private GameObject playerObj;

        [Tooltip("플레이어가 탑승/매달릴 이동 대상 (발판, 고리 등)")]
        [SerializeField] private Transform movingTarget;

        [Header("Attachment Settings")]
        [Tooltip("체크 시 스크립트 실행 순간의 플레이어와 대상 간의 위치/각도 오프셋을 자동으로 측정하여 유지합니다.")]
        [SerializeField] private bool autoCalculateInitialOffset = true;

        [Tooltip("autoCalculateInitialOffset이 false일 때 사용되는 고정 로컬 오프셋")]
        [SerializeField] private Vector3 localOffset = Vector3.zero;

        [Tooltip("이동 대상의 회전도 같이 따라갈지 여부")]
        [SerializeField] private bool followTargetRotation = true;

        [Header("Dismount Key Settings")]
        [Tooltip("플레이어가 지정한 키를 눌러 중간에 내릴 수 있는지 여부")]
        [SerializeField] private bool canDismountWithKey = true;

        [Tooltip("내릴 때 사용할 키 (기본값: Space)")]
        [SerializeField] private KeyCode dismountKey = KeyCode.Space;

        [Header("Dismount Mode Settings")]
        [Tooltip("내릴 때 탈출 방식 선택 (관성 유지 / 지정 위치 점프)")]
        [SerializeField] private DismountType dismountMode = DismountType.Momentum;

        [Header("Mode 1: Momentum Settings")]
        [Tooltip("내릴 때 이동 대상의 속도를 얼마나 전달받을지 (관성 배수)")]
        [SerializeField] private float momentumMultiplier = 1.0f;

        [Tooltip("내릴 때 위로 살짝 점프/띄워주는 힘")]
        [SerializeField] private float upwardForce = 3.0f;

        [Header("Mode 2: Jump To Target Settings")]
        [Tooltip("점프해 도착할 목표 Transform (우선 적용)")]
        [SerializeField] private Transform jumpTargetTransform;

        [Tooltip("jumpTargetTransform이 없을 때 사용할 고정 월드 좌표")]
        [SerializeField] private Vector3 jumpTargetPosition;

        [Tooltip("점프 시 최고 높이 (포탄 궤적 곡선 오프셋)")]
        [SerializeField] private float jumpHeight = 3.0f;

        [Tooltip("목표 지점까지 날아가는 데 걸리는 총 시간 (초)")]
        [SerializeField] private float jumpDuration = 1.0f;

        [Tooltip("점프하는 동안 진행 방향을 바라볼지 여부")]
        [SerializeField] private bool lookAtDestination = true;

        private Coroutine _attachCoroutine;
        private Vector3 _lastTargetPosition;
        private Vector3 _currentTargetVelocity;
        private Vector3 _dynamicLocalOffset = Vector3.zero;

        // 키 입력을 절대 놓치지 않는 플래그 변수
        private bool _wasDismountRequested = false;

        private void Awake()
        {
            if (!playerObj) playerObj = this.gameObject;
        }

        // 매 프레임 키 입력을 체크하여 씹힘을 방지
        private void Update()
        {
            if (canDismountWithKey && Input.GetKeyDown(dismountKey))
            {
                _wasDismountRequested = true;
            }
        }

        public override void Execute()
        {
            IsOn = false;

            if (playerObj == null || movingTarget == null)
            {
                IsOn = true;
                return;
            }

            PlayerLocomotion locomotion = playerObj.GetComponent<PlayerLocomotion>();

            if (locomotion != null && locomotion.isRidingRail)
            {
                IsOn = true;
                return;
            }

            StartAttachment();
        }

        private void StartAttachment()
        {
            if (_attachCoroutine != null) StopCoroutine(_attachCoroutine);
            _attachCoroutine = StartCoroutine(AttachRoutine());
        }

        private IEnumerator AttachRoutine()
        {
            PlayerLocomotion locomotion = playerObj.GetComponent<PlayerLocomotion>();
            Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();

            _wasDismountRequested = false;

            if (autoCalculateInitialOffset)
            {
                _dynamicLocalOffset = movingTarget.InverseTransformPoint(playerObj.transform.position);
            }
            else
            {
                _dynamicLocalOffset = localOffset;
            }

            if (locomotion != null) locomotion.SetRailMode(true);
            else if (playerRb != null) playerRb.isKinematic = true;

            _lastTargetPosition = movingTarget.position;

            while (movingTarget != null)
            {
                // Update에서 감지된 키 입력 플래그 확인
                if (_wasDismountRequested)
                {
                    _wasDismountRequested = false;
                    break;
                }

                yield return new WaitForFixedUpdate();

                _currentTargetVelocity = (movingTarget.position - _lastTargetPosition) / Time.fixedDeltaTime;
                _lastTargetPosition = movingTarget.position;

                Vector3 targetPos = movingTarget.TransformPoint(_dynamicLocalOffset);
                Quaternion targetRot = followTargetRotation ? movingTarget.rotation : playerObj.transform.rotation;

                if (playerRb != null)
                {
                    playerRb.MovePosition(targetPos);
                    playerRb.MoveRotation(targetRot);
                }
                else
                {
                    playerObj.transform.position = targetPos;
                    playerObj.transform.rotation = targetRot;
                }
            }

            yield return StartCoroutine(DismountRoutine(locomotion, playerRb));

            _attachCoroutine = null;
            IsOn = true;
        }

        public void Detach()
        {
            if (_attachCoroutine != null)
            {
                StopCoroutine(_attachCoroutine);
                _attachCoroutine = null;

                PlayerLocomotion locomotion = playerObj.GetComponent<PlayerLocomotion>();
                Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();
                
                StartCoroutine(DismountRoutine(locomotion, playerRb));
                IsOn = true;
            }
        }

        private IEnumerator DismountRoutine(PlayerLocomotion locomotion, Rigidbody playerRb)
        {
            if (locomotion != null)
            {
                locomotion.SetRailMode(false);
            }

            if (dismountMode == DismountType.Momentum)
            {
                Vector3 launchVelocity = (_currentTargetVelocity * momentumMultiplier) + (Vector3.up * upwardForce);

                if (locomotion != null)
                {
                    if (_currentTargetVelocity.sqrMagnitude > 0.1f)
                    {
                        locomotion.SetBaseRotation(_currentTargetVelocity.normalized);
                    }
                    locomotion.ExternalLaunch(launchVelocity);
                }
                else if (playerRb != null)
                {
                    playerRb.isKinematic = false;
                    playerRb.linearVelocity = launchVelocity;
                }
            }
            else if (dismountMode == DismountType.JumpToTarget)
            {
                Vector3 startPos = playerObj.transform.position;
                Vector3 endPos = jumpTargetTransform != null ? jumpTargetTransform.position : jumpTargetPosition;

                Vector3 jumpDirection = new Vector3(endPos.x - startPos.x, 0f, endPos.z - startPos.z);
                if (lookAtDestination && jumpDirection.sqrMagnitude > 0.01f)
                {
                    if (locomotion != null)
                    {
                        locomotion.SetBaseRotation(jumpDirection.normalized);
                    }
                    else
                    {
                        playerObj.transform.rotation = Quaternion.LookRotation(jumpDirection.normalized);
                    }
                }

                bool wasKinematic = playerRb != null ? playerRb.isKinematic : false;
                if (playerRb != null) playerRb.isKinematic = true;

                float timer = 0f;

                while (timer < jumpDuration)
                {
                    yield return new WaitForFixedUpdate();

                    timer += Time.fixedDeltaTime;
                    float t = Mathf.Clamp01(timer / jumpDuration);

                    float heightOffset = 4f * jumpHeight * t * (1f - t);

                    Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
                    currentPos.y += heightOffset;

                    if (playerRb != null)
                    {
                        playerRb.MovePosition(currentPos);
                    }
                    else
                    {
                        playerObj.transform.position = currentPos;
                    }
                }

                if (playerRb != null)
                {
                    playerRb.MovePosition(endPos);
                    playerRb.isKinematic = wasKinematic;
                }
                else
                {
                    playerObj.transform.position = endPos;
                }

                if (locomotion != null)
                {
                    locomotion.ExternalLaunch(Vector3.down * 2.0f);
                }
            }
        }

        public void DismountToTarget(Transform targetTransform, float height = 3.0f, float duration = 1.0f)
        {
            dismountMode = DismountType.JumpToTarget;
            jumpTargetTransform = targetTransform;
            jumpHeight = height;
            jumpDuration = duration;
            Detach();
        }
    }
}