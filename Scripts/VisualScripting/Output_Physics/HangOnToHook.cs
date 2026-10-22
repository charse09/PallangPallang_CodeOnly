
//using System.Collections;
//using UnityEngine;

//namespace _Project.Scripts.VisualScripting
//{
//    public class HangOnToHook : ProcessBase
//    {
//        [Header("Target References")]
//        [Tooltip("고리에 매달릴 플레이어 오브젝트")]
//        [SerializeField] private GameObject playerObj;

//        [Tooltip("플레이어가 매달릴 대상 고리/오브젝트")]
//        [SerializeField] private Transform hookTransform;

//        [Header("Hanging Settings")]
//        [Tooltip("고리 중앙 기준으로 플레이어가 위치할 오프셋 (예: Y = -1.5m 밑)")]
//        [SerializeField] private Vector3 hangOffset = new Vector3(0f, -1.5f, 0f);

//        [Tooltip("고리의 진행 방향을 똑같이 바라볼지 여부")]
//        [SerializeField] private bool followHookRotation = true;

//        [Header("Dismount Key Settings")]
//        [Tooltip("플레이어가 지정한 키를 눌러 중간에 뛰어내릴 수 있는지 여부")]
//        [SerializeField] private bool canDismountWithKey = true;

//        [Tooltip("중간에 내릴 때 사용할 키 (기본값: Space)")]
//        [SerializeField] private KeyCode dismountKey = KeyCode.Space;

//        [Header("★ Dismount Trigger Settings (외부 노드/트리거 연동)")]
//        [Tooltip("이 ProcessBase(트리거/Output 노드)의 IsOn이 true가 되면 중간에 자동으로 떨어집니다.")]
//        [SerializeField] private ProcessBase dismountTrigger;

//        [Header("★ Landing Flight Settings (목표 지점으로 슝 날아가기)")]
//        [Tooltip("이탈 시 관성 튕김 대신 특정 위치로 포물선을 그리며 날아갈지 여부")]
//        [SerializeField] private bool useTargetJumpOnDismount = true;

//        [Tooltip("고리에서 내릴 때 안착할 목표 위치 (빈 GameObject)")]
//        [SerializeField] private Transform landingTarget;

//        [Tooltip("날아갈 때의 포물선 최대 높이")]
//        [SerializeField] private float jumpHeight = 3.0f;

//        [Tooltip("목표 지점까지 날아가는 시간 (초)")]
//        [SerializeField] private float jumpDuration = 0.8f;

//        [Header("Physics Exit Settings (useTargetJumpOnDismount = false 일 때만 사용)")]
//        [Tooltip("고리에서 이탈할 때 고리의 속도를 얼마나 전달받을지 (관성 배수)")]
//        [SerializeField] private float momentumMultiplier = 1.0f;

//        [Tooltip("고리에서 내릴 때 위로 살짝 띄워주는 힘")]
//        [SerializeField] private float upwardForce = 3.0f;

//        private Coroutine _hangCoroutine;
//        private Vector3 _lastHookPosition;
//        private Vector3 _currentHookVelocity;

//        private void Awake()
//        {
//            if (!playerObj) playerObj = this.gameObject;
//        }

//        public override void Execute()
//        {
//            IsOn = false;

//            if (playerObj == null || hookTransform == null)
//            {
//                IsOn = true;
//                return;
//            }

//            PlayerLocomotion locomotion = playerObj.GetComponent<PlayerLocomotion>();

//            if (locomotion != null && locomotion.isRidingRail)
//            {
//                IsOn = true;
//                return;
//            }

//            StartHanging();
//        }

//        private void StartHanging()
//        {
//            if (_hangCoroutine != null) StopCoroutine(_hangCoroutine);
//            _hangCoroutine = StartCoroutine(HangRoutine());
//        }

//        private IEnumerator HangRoutine()
//        {
//            PlayerLocomotion locomotion = playerObj.GetComponent<PlayerLocomotion>();
//            Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();

//            if (locomotion != null) locomotion.SetRailMode(true);
//            else if (playerRb != null) playerRb.isKinematic = true;

//            _lastHookPosition = hookTransform.position;

//            while (hookTransform != null)
//            {
//                // 1. 지정된 키를 눌렀을 때 뛰어내림
//                if (canDismountWithKey && Input.GetKeyDown(dismountKey))
//                {
//                    break;
//                }

//                // 2. ★ 지정한 외부 트리거 / Output 노드의 IsOn이 true가 되었을 때 뛰어내림
//                if (dismountTrigger != null && dismountTrigger.IsOn)
//                {
//                    break;
//                }

//                yield return new WaitForFixedUpdate();

//                _currentHookVelocity = (hookTransform.position - _lastHookPosition) / Time.fixedDeltaTime;
//                _lastHookPosition = hookTransform.position;

//                Vector3 targetPos = hookTransform.position + (hookTransform.rotation * hangOffset);
//                Quaternion targetRot = followHookRotation ? hookTransform.rotation : playerObj.transform.rotation;

//                if (playerRb != null)
//                {
//                    playerRb.MovePosition(targetPos);
//                    playerRb.MoveRotation(targetRot);
//                }
//                else
//                {
//                    playerObj.transform.position = targetPos;
//                    playerObj.transform.rotation = targetRot;
//                }
//            }

//            // 루프 종료 시 (키 입력 / 외부 트리거 감지 / 고리 이동 끝) 이탈 처리
//            yield return StartCoroutine(DismountSequence(locomotion, playerRb, landingTarget));

//            _hangCoroutine = null;
//            IsOn = true;
//        }

//        /// <summary>
//        /// 외부 스크립트에서 직접 강제로 고리를 끊고 떨어뜨릴 때 호출하는 함수
//        /// </summary>
//        public void ForceDrop()
//        {
//            ForceDropAndFlyTo(landingTarget);
//        }

//        /// <summary>
//        /// 외부에서 강제로 고리를 끊으면서 '동적으로 지정한 특정 위치'로 날려 보낼 때 호출하는 함수
//        /// </summary>
//        public void ForceDropAndFlyTo(Transform overrideTarget)
//        {
//            if (_hangCoroutine != null)
//            {
//                StopCoroutine(_hangCoroutine);
//                _hangCoroutine = StartCoroutine(ForceDropRoutine(overrideTarget));
//            }
//        }

//        private IEnumerator ForceDropRoutine(Transform customLandingTarget)
//        {
//            PlayerLocomotion locomotion = playerObj.GetComponent<PlayerLocomotion>();
//            Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();

//            yield return StartCoroutine(DismountSequence(locomotion, playerRb, customLandingTarget));

//            _hangCoroutine = null;
//            IsOn = true;
//        }

//        private IEnumerator DismountSequence(PlayerLocomotion locomotion, Rigidbody playerRb, Transform targetDestination)
//        {
//            // 레일 모드 해제
//            if (locomotion != null)
//            {
//                locomotion.SetRailMode(false);
//            }

//            // 1. 목표 지점으로 포물선을 그리며 날아가는 방식[cite: 4]
//            if (useTargetJumpOnDismount && targetDestination != null)
//            {
//                bool wasKinematic = (playerRb != null) ? playerRb.isKinematic : false;
//                if (playerRb != null) playerRb.isKinematic = true;

//                Vector3 startPos = playerObj.transform.position;
//                Vector3 endPos = targetDestination.position;
//                float timer = 0f;

//                while (timer < jumpDuration)
//                {
//                    timer += Time.deltaTime;
//                    float t = Mathf.Clamp01(timer / jumpDuration);

//                    // 포물선 Y 오프셋 연산[cite: 4]
//                    float heightOffset = 4f * jumpHeight * t * (1f - t);

//                    Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
//                    currentPos.y += heightOffset;

//                    playerObj.transform.position = currentPos;
//                    yield return null;
//                }

//                playerObj.transform.position = endPos;

//                if (playerRb != null)
//                {
//                    playerRb.isKinematic = wasKinematic;
//                    playerRb.linearVelocity = Vector3.zero;
//                }
//            }
//            // 2. 목표 지점이 없거나 단순 물리 관성 발사인 경우[cite: 3]
//            else
//            {
//                Vector3 launchVelocity = (_currentHookVelocity * momentumMultiplier) + (Vector3.up * upwardForce);

//                if (locomotion != null)
//                {
//                    if (_currentHookVelocity.sqrMagnitude > 0.1f)
//                    {
//                        locomotion.SetBaseRotation(_currentHookVelocity.normalized);
//                    }
//                    locomotion.ExternalLaunch(launchVelocity);
//                }
//                else if (playerRb != null)
//                {
//                    playerRb.isKinematic = false;
//                    playerRb.linearVelocity = launchVelocity;
//                }
//            }
//        }
//    }
//}

using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// PlayerFaceMaterial과 연동할 표정 타입 열거형
    /// </summary>
    public enum FaceType
    {
        None,           // 표정을 변경하지 않고 유지
        Normal,
        Surprize,
        Kawaii,
        Stare,
        Sad,
        MouthSmile,
        Embarassed,
        Smile
    }

    public class HangOnToHook : ProcessBase
    {
        [Header("Target References")]
        [Tooltip("고리에 매달릴 플레이어 오브젝트")]
        [SerializeField] private GameObject playerObj;

        [Tooltip("플레이어가 매달릴 대상 고리/오브젝트")]
        [SerializeField] private Transform hookTransform;

        [Header("Hanging Settings")]
        [Tooltip("고리 중앙 기준으로 플레이어가 위치할 오프셋 (예: Y = -1.5m 밑)")]
        [SerializeField] private Vector3 hangOffset = new Vector3(0f, -1.5f, 0f);

        [Tooltip("고리의 진행 방향을 똑같이 바라볼지 여부")]
        [SerializeField] private bool followHookRotation = true;

        [Header("Dismount Key Settings")]
        [Tooltip("플레이어가 지정한 키를 눌러 중간에 뛰어내릴 수 있는지 여부")]
        [SerializeField] private bool canDismountWithKey = true;

        [Tooltip("중간에 내릴 때 사용할 키 (기본값: Space)")]
        [SerializeField] private KeyCode dismountKey = KeyCode.Space;

        [Header("★ Dismount Trigger Settings (외부 노드/트리거 연동)")]
        [Tooltip("이 ProcessBase(트리거/Output 노드)의 IsOn이 true가 되면 중간에 자동으로 떨어집니다.")]
        [SerializeField] private ProcessBase dismountTrigger;

        [Header("★ Face Expression Settings (표정 연동)")]
        [Tooltip("플레이어 표정 제어 컴포넌트 (비워둘 시 playerObj 및 자식에서 자동 탐색)")]
        [SerializeField] private PlayerFaceMaterial playerFaceMaterial;

        [Tooltip("고리에 매달렸을 때 변경할 표정 (None이면 변경 안 함)")]
        [SerializeField] private FaceType hangFace = FaceType.Kawaii;

        [Tooltip("고리에서 이탈하여 날아가는 동안 변경할 표정 (포물선 비행 시 적용)")]
        [SerializeField] private FaceType flightFace = FaceType.Embarassed;

        [Tooltip("착지 및 고리 액션이 완전히 끝난 후 복귀할 표정")]
        [SerializeField] private FaceType exitFace = FaceType.Normal;

        [Header("★ Landing Flight Settings (목표 지점으로 슝 날아가기)")]
        [Tooltip("이탈 시 관성 튕김 대신 특정 위치로 포물선을 그리며 날아갈지 여부")]
        [SerializeField] private bool useTargetJumpOnDismount = true;

        [Tooltip("고리에서 내릴 때 안착할 목표 위치 (빈 GameObject)")]
        [SerializeField] private Transform landingTarget;

        [Tooltip("날아갈 때의 포물선 최대 높이")]
        [SerializeField] private float jumpHeight = 3.0f;

        [Tooltip("목표 지점까지 날아가는 시간 (초)")]
        [SerializeField] private float jumpDuration = 0.8f;

        [Header("Physics Exit Settings (useTargetJumpOnDismount = false 일 때만 사용)")]
        [Tooltip("고리에서 이탈할 때 고리의 속도를 얼마나 전달받을지 (관성 배수)")]
        [SerializeField] private float momentumMultiplier = 1.0f;

        [Tooltip("고리에서 내릴 때 위로 살짝 띄워주는 힘")]
        [SerializeField] private float upwardForce = 3.0f;

        private Coroutine _hangCoroutine;
        private Vector3 _lastHookPosition;
        private Vector3 _currentHookVelocity;

        private void Awake()
        {
            if (!playerObj) playerObj = this.gameObject;

            // PlayerFaceMaterial 자동 캐싱 (본체 또는 자식 오브젝트)
            if (playerFaceMaterial == null && playerObj != null)
            {
                playerFaceMaterial = playerObj.GetComponentInChildren<PlayerFaceMaterial>();
            }
        }

        public override void Execute()
        {
            IsOn = false;

            if (playerObj == null || hookTransform == null)
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

            StartHanging();
        }

        private void StartHanging()
        {
            if (_hangCoroutine != null) StopCoroutine(_hangCoroutine);
            _hangCoroutine = StartCoroutine(HangRoutine());
        }

        private IEnumerator HangRoutine()
        {
            PlayerLocomotion locomotion = playerObj.GetComponent<PlayerLocomotion>();
            Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();

            if (locomotion != null) locomotion.SetRailMode(true);
            else if (playerRb != null) playerRb.isKinematic = true;

            // 1. 매달리기 시작할 때 표정 적용
            ApplyFace(hangFace);

            _lastHookPosition = hookTransform.position;

            while (hookTransform != null)
            {
                // 지정된 키 입력 시 이탈
                if (canDismountWithKey && Input.GetKeyDown(dismountKey))
                {
                    break;
                }

                // 외부 트리거 IsOn 감지 시 이탈
                if (dismountTrigger != null && dismountTrigger.IsOn)
                {
                    break;
                }

                yield return new WaitForFixedUpdate();

                _currentHookVelocity = (hookTransform.position - _lastHookPosition) / Time.fixedDeltaTime;
                _lastHookPosition = hookTransform.position;

                Vector3 targetPos = hookTransform.position + (hookTransform.rotation * hangOffset);
                Quaternion targetRot = followHookRotation ? hookTransform.rotation : playerObj.transform.rotation;

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

            // 루프 종료 시 (키 입력 / 외부 트리거 감지 / 고리 이동 끝) 이탈 시퀀스 실행
            yield return StartCoroutine(DismountSequence(locomotion, playerRb, landingTarget));

            _hangCoroutine = null;
            IsOn = true;
        }

        public void ForceDrop()
        {
            ForceDropAndFlyTo(landingTarget);
        }

        public void ForceDropAndFlyTo(Transform overrideTarget)
        {
            if (_hangCoroutine != null)
            {
                StopCoroutine(_hangCoroutine);
                _hangCoroutine = StartCoroutine(ForceDropRoutine(overrideTarget));
            }
        }

        private IEnumerator ForceDropRoutine(Transform customLandingTarget)
        {
            PlayerLocomotion locomotion = playerObj.GetComponent<PlayerLocomotion>();
            Rigidbody playerRb = playerObj.GetComponent<Rigidbody>();

            yield return StartCoroutine(DismountSequence(locomotion, playerRb, customLandingTarget));

            _hangCoroutine = null;
            IsOn = true;
        }

        private IEnumerator DismountSequence(PlayerLocomotion locomotion, Rigidbody playerRb, Transform targetDestination)
        {
            // 레일 모드 해제
            if (locomotion != null)
            {
                locomotion.SetRailMode(false);
            }

            // 1. 목표 지점으로 포물선을 그리며 날아가는 방식
            if (useTargetJumpOnDismount && targetDestination != null)
            {
                // 비행 중 표정 적용 (예: 놀란 표정)
                ApplyFace(flightFace);

                bool wasKinematic = (playerRb != null) ? playerRb.isKinematic : false;
                if (playerRb != null) playerRb.isKinematic = true;

                Vector3 startPos = playerObj.transform.position;
                Vector3 endPos = targetDestination.position;
                float timer = 0f;

                while (timer < jumpDuration)
                {
                    timer += Time.deltaTime;
                    float t = Mathf.Clamp01(timer / jumpDuration);

                    // 포물선 Y 오프셋 연산
                    float heightOffset = 4f * jumpHeight * t * (1f - t);

                    Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
                    currentPos.y += heightOffset;

                    playerObj.transform.position = currentPos;
                    yield return null;
                }

                playerObj.transform.position = endPos;

                if (playerRb != null)
                {
                    playerRb.isKinematic = wasKinematic;
                    playerRb.linearVelocity = Vector3.zero;
                }
            }
            // 2. 목표 지점이 없거나 단순 물리 관성 발사인 경우
            else
            {
                ApplyFace(flightFace);

                Vector3 launchVelocity = (_currentHookVelocity * momentumMultiplier) + (Vector3.up * upwardForce);

                if (locomotion != null)
                {
                    if (_currentHookVelocity.sqrMagnitude > 0.1f)
                    {
                        locomotion.SetBaseRotation(_currentHookVelocity.normalized);
                    }
                    locomotion.ExternalLaunch(launchVelocity);
                }
                else if (playerRb != null)
                {
                    playerRb.isKinematic = false;
                    playerRb.linearVelocity = launchVelocity;
                }
            }

            // 3. 착지/이탈 동작 완료 후 원래 표정으로 복귀
            ApplyFace(exitFace);
        }

        /// <summary>
        /// 외부 피격/넉백 노드에서 고리 매달림을 즉시 강제 중단시킬 때 호출
        /// </summary>
        public void InterruptHang()
        {
            if (_hangCoroutine != null)
            {
                StopCoroutine(_hangCoroutine);
                _hangCoroutine = null;
            }

            // HangOnToHook 내부이므로 protected 접근자여도 문제없이 대입 가능
            IsOn = true;
        }

 

        /// <summary>
        /// FaceType 열거형에 맞춰 PlayerFaceMaterial 메서드를 호출합니다.
        /// </summary>
        private void ApplyFace(FaceType faceType)
        {
            if (playerFaceMaterial == null || faceType == FaceType.None) return;

            switch (faceType)
            {
                case FaceType.Normal:
                    playerFaceMaterial.SetNormalFace();
                    break;
                case FaceType.Surprize:
                    playerFaceMaterial.SetSurprizeFace();
                    break;
                case FaceType.Kawaii:
                    playerFaceMaterial.SetKawaiiFace();
                    break;
                case FaceType.Smile:
                    playerFaceMaterial.SetSmileFace();
                    break;
                case FaceType.Stare:
                    playerFaceMaterial.SetStareFace();
                    break;
                case FaceType.Sad:
                    playerFaceMaterial.SetSadFace();
                    break;
                case FaceType.MouthSmile:
                    playerFaceMaterial.SetMouthSmileFace();
                    break;
                case FaceType.Embarassed:
                    playerFaceMaterial.SetEmbarassedFace();
                    break;
            }
        }
    }
}