using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// Yenbeol(벌)이 플레이어를 붙잡고 운반할 때,
    /// 플레이어가 QTE Key(기본: Space)를 연타해야만 목적지까지 조금씩 전진하며,
    /// 아무런 입력이 없으면 입력을 기다리며 제자리에서 호버링(대기)하는 QTE Output.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Outputs/벌 플레이어 운반 (Space 연타 QTE)")]
    public class BeeCarrySpaceMashOutput : ProcessBase
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
        [Tooltip("목적지까지의 경유점 목록")]
        [SerializeField] private List<Transform> waypoints = new List<Transform>();

        [Tooltip("최종 목적지 Transform")]
        [SerializeField] private Transform destination;

        [Header("Flight & Speed Settings")]
        [Tooltip("Yenbeol이 연타에 반응하여 전진하는 속도 (m/s)")]
        [SerializeField] private float flySpeed = 4f;

        [Tooltip("도착 판정 수평 거리 (m)")]
        [SerializeField] private float arrivalThreshold = 0.2f;

        [Header("★ Space Mash QTE Settings (연타 이동 설정)")]
        [Tooltip("QTE에 사용할 키 (기본: Space)")]
        [SerializeField] private KeyCode qteKey = KeyCode.Space;

        [Tooltip("목적지까지 도달하는 데 필요한 총 연타 횟수 (0 이하로 설정 시 advanceDistancePerPress 거리 기반으로 자동 계산)")]
        [SerializeField] private int requiredPressCount = 12;

        [Tooltip("requiredPressCount가 0 이하일 때 적용되는 1회 연타당 전진 거리 (m)")]
        [SerializeField] private float advanceDistancePerPress = 0.6f;

        [Tooltip("연타 시 전진 보간 반응 속도 (높을수록 누르자마자 팍팍 전진)")]
        [SerializeField] private float moveSmoothSpeed = 2.5f;

        [Tooltip("스페이스 1회 연타 시 위로 끙차 들썩이는 Y축 반동 높이 (m)")]
        [SerializeField] private float nudgeHopAmount = 0.25f;

        [Tooltip("스페이스 1회 연타 시 앞으로 끄덕이는 피치 각도 (도)")]
        [SerializeField] private float nudgeTiltAngle = 10f;

        [Tooltip("움찔 반동 회복 속도")]
        [SerializeField] private float nudgeRecoverySpeed = 4.5f;

        [Tooltip("시작 직후 입력 무시 안정화 딜레이 (초)")]
        [SerializeField] private float initialInputDelay = 0.15f;

        [Header("Audio Settings (SoundManager Pooling)")]
        [Tooltip("QTE 키를 누를 때마다 재생할 사운드 키 (SO_SFXTable 등록 키, 비워두면 무음)")]
        [SerializeField] private string qtePressSoundKey = "YenBeol_Carry";

        [Header("★ Ground Protection (바닥 뚫림 방지)")]
        [Tooltip("체크 시 바닥 지형을 감지하여 플레이어가 땅을 뚫지 않도록 합니다.")]
        [SerializeField] private bool preventGroundClipping = true;

        [Tooltip("바닥으로 인식할 지형 LayerMask (Default, Ground 등)")]
        [SerializeField] private LayerMask groundLayer = ~0;

        [Tooltip("플레이어 발바닥 기준 추가 Y 오프셋 (0이면 발바닥이 바닥에 맞닿음)")]
        [SerializeField] private float playerBottomOffset = 0.0f;

        [Header("Heavy & Unstable Motion (위태로운 연출 / 대기 중 호버링)")]
        [Tooltip("무거워서 아래로 쳐지는 Y축 미세 진동 폭 (대기 중 호버링 진폭)")]
        [SerializeField] private float heavyDipAmount = 0.2f;

        [Tooltip("고도 미세 진동 속도 (대기 중 호버링 주기)")]
        [SerializeField] private float heavyDipSpeed = 8.0f;

        [Tooltip("비틀거리는 좌우 흔들림 폭")]
        [SerializeField] private float wobbleAmount = 0.25f;

        [Tooltip("비틀거리는 좌우 흔들림 속도")]
        [SerializeField] private float wobbleSpeed = 8.0f;

        [Tooltip("몸체가 기울어지는 최대 각도")]
        [SerializeField] private float maxTiltAngle = 15.0f;

#pragma warning disable 0414
        [Header("Compatibility Settings (하위 호환 필드)")]
        [SerializeField, HideInInspector] private float spaceImpulse = 5.5f;
        [SerializeField, HideInInspector] private float gravity = 18f;
        [SerializeField, HideInInspector] private float fallMultiplier = 1.8f;
        [SerializeField, HideInInspector] private float maxFallSpeed = 3.5f;
        [SerializeField, HideInInspector] private float maxQteHeightOffset = 2.0f;
#pragma warning restore 0414

        // 내부 상태
        private Coroutine _carryCoroutine;
        private Rigidbody _playerRb;
        private bool _wasKinematic;

        public override void Execute()
        {
            if (yenbeolTransform == null || playerObject == null || destination == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 필수 Transform/GameObject가 비어 있습니다.");
                IsOn = true;
                return;
            }

            IsOn = false;
            if (_carryCoroutine != null) StopCoroutine(_carryCoroutine);
            _carryCoroutine = StartCoroutine(CarryRoutine());
        }

        private IEnumerator CarryRoutine()
        {
            Transform playerTransform = playerObject.transform;

            // STEP 1: 입력 및 플레이어 움직임 잠금
            List<MonoBehaviour> disabledScripts = new List<MonoBehaviour>();
            foreach (var script in scriptsToDisable)
            {
                if (script != null && script.enabled)
                {
                    script.enabled = false;
                    disabledScripts.Add(script);
                }
            }

            // 플레이어의 Locomotion / CharacterController 추가 비활성화
            var locomotion = playerObject.GetComponent<PlayerLocomotion>();
            if (locomotion != null && locomotion.enabled && !disabledScripts.Contains(locomotion))
            {
                locomotion.enabled = false;
                disabledScripts.Add(locomotion);
            }
            CharacterController cc = playerObject.GetComponent<CharacterController>();
            bool wasCcEnabled = cc != null && cc.enabled;
            if (wasCcEnabled)
            {
                cc.enabled = false;
            }

            if (InputManager.Instance != null)
            {
                InputManager.Instance.enabled = false;
            }

            _playerRb = playerObject.GetComponent<Rigidbody>();
            if (_playerRb != null)
            {
                _wasKinematic = _playerRb.isKinematic;
                _playerRb.linearVelocity = Vector3.zero;
                _playerRb.angularVelocity = Vector3.zero;
                _playerRb.isKinematic = true;
            }

            // Locomotion/Physics 프레임 안정화 대기
            yield return null;
            yield return null;

            if (initialInputDelay > 0f)
            {
                yield return new WaitForSeconds(initialInputDelay);
            }

            // STEP 2: 잡은 시점 오프셋 고정
            Vector3 carryOffset = playerTransform.position - yenbeolTransform.position;

            // STEP 3: 이동 경로 포인트 및 누적 길이 구성
            List<Vector3> pathPoints = new List<Vector3>();
            pathPoints.Add(yenbeolTransform.position);
            if (waypoints != null)
            {
                foreach (var wp in waypoints)
                {
                    if (wp != null) pathPoints.Add(wp.position);
                }
            }
            pathPoints.Add(destination.position);

            float totalPathLength = 0f;
            float[] segmentLengths = new float[pathPoints.Count - 1];
            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                segmentLengths[i] = Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
                totalPathLength += segmentLengths[i];
            }
            if (totalPathLength < 0.001f) totalPathLength = 0.001f;

            // 유효 연타 횟수 계산
            int effectivePressCount = requiredPressCount;
            if (effectivePressCount <= 0)
            {
                effectivePressCount = Mathf.Max(1, Mathf.RoundToInt(totalPathLength / Mathf.Max(advanceDistancePerPress, 0.1f)));
            }

            float totalFlightTime = 0f;
            float currentProgress = 0f;
            float targetProgress = 0f;
            int currentPressCount = 0;

            float nudgeImpulse = 0f;
            float tiltSign = 1f;

            Vector3 currentForwardDir = (pathPoints[1] - pathPoints[0]).normalized;
            if (currentForwardDir == Vector3.zero) currentForwardDir = yenbeolTransform.forward;

            // ---------------------------------------------------------
            // ★ 메인 QTE 루프: 연타 입력이 들어와야만 전진, 없으면 제자리 대기
            // ---------------------------------------------------------
            while (currentProgress < 1.0f)
            {
                if (yenbeolTransform == null || playerObject == null || destination == null) yield break;

                totalFlightTime += Time.deltaTime;

                // 1. QTE 연타 입력 감지 (Space 키 등)
                if (IsQtePressed())
                {
                    currentPressCount++;
                    targetProgress = Mathf.Clamp01((float)currentPressCount / effectivePressCount);

                    nudgeImpulse = 1.0f;  // 움찔 반동 트리거
                    tiltSign = -tiltSign; // 좌우 비틀거림 방향 반전

                    PlaySFX(qtePressSoundKey, yenbeolTransform.position);
                }

                // 2. 움찔 반동 감쇠
                if (nudgeImpulse > 0f)
                {
                    nudgeImpulse -= Time.deltaTime * nudgeRecoverySpeed;
                    if (nudgeImpulse < 0f) nudgeImpulse = 0f;
                }

                // 3. 진행도(Progress) 보간 이동
                // flySpeed(m/s)를 경로 전체 길이에 맞춰 속도로 변환
                float progressSpeed = (flySpeed > 0f && totalPathLength > 0.001f)
                    ? (flySpeed / totalPathLength)
                    : 1.0f;

                currentProgress = Mathf.MoveTowards(
                    currentProgress, targetProgress, progressSpeed * moveSmoothSpeed * Time.deltaTime);

                // 4. 경로 좌표 샘플링
                Vector3 basePathPos = GetPointOnPath(pathPoints, segmentLengths, totalPathLength, currentProgress, out currentForwardDir);

                // 5. 모션 연출 (움찔 들썩임 + 대기 중 호버링)
                float arrivalFade = Mathf.Clamp01((1f - currentProgress) / 0.05f);

                // 스페이스 연타 시 위로 끙차 솟구치는 Y 반동
                float nudgeHop = Mathf.Sin(nudgeImpulse * Mathf.PI * 0.5f) * nudgeHopAmount * arrivalFade;

                // 스페이스 연타 시 앞으로 고개 숙이는 피치 반동
                float nudgePitch = nudgeImpulse * nudgeTiltAngle * arrivalFade;

                // 입력 대기 시 제자리 날갯짓 호버링 / 처짐
                float hoverDip = -Mathf.Abs(Mathf.Sin(totalFlightTime * heavyDipSpeed)) * heavyDipAmount * arrivalFade;
                float hoverWobble = Mathf.Sin(totalFlightTime * wobbleSpeed) * wobbleAmount * arrivalFade;

                Vector3 rightVec = Vector3.Cross(Vector3.up, currentForwardDir).normalized;
                if (rightVec == Vector3.zero) rightVec = Vector3.right;

                Vector3 nextPos = basePathPos;
                nextPos.y += (nudgeHop + hoverDip);
                nextPos += rightVec * (hoverWobble * tiltSign * 0.15f);

                // 6. 바닥 충돌 방지 (Prevent Ground Clipping)
                if (preventGroundClipping)
                {
                    float expectedPlayerY = nextPos.y + carryOffset.y;
                    Vector3 rayStart = new Vector3(nextPos.x, basePathPos.y + 5.0f, nextPos.z);
                    if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hitInfo, 15.0f, groundLayer))
                    {
                        float groundSurfaceY = hitInfo.point.y + playerBottomOffset;
                        if (expectedPlayerY < groundSurfaceY)
                        {
                            float diff = groundSurfaceY - expectedPlayerY;
                            nextPos.y += diff;
                        }
                    }
                }

                // 위치 반영
                yenbeolTransform.position = nextPos;
                playerTransform.position = nextPos + carryOffset;

                // 7. 회전 연출
                Vector3 flatForward = currentForwardDir;
                flatForward.y = 0f;
                if (flatForward.sqrMagnitude < 0.001f) flatForward = yenbeolTransform.forward;
                flatForward.Normalize();

                if (flatForward != Vector3.zero)
                {
                    Quaternion baseRot = Quaternion.LookRotation(flatForward);
                    float tiltRoll = hoverWobble * maxTiltAngle * tiltSign;
                    Quaternion dynamicRot = Quaternion.Euler(-nudgePitch, 0f, tiltRoll);

                    yenbeolTransform.rotation = Quaternion.Slerp(
                        yenbeolTransform.rotation, baseRot * dynamicRot, Time.deltaTime * 8.0f);
                }

                // 목표치에 도달하고 이동 보간도 완료되었는지 확인
                float distToDest = Vector3.Distance(yenbeolTransform.position, destination.position);
                if (currentProgress >= 0.999f && currentPressCount >= effectivePressCount && distToDest <= arrivalThreshold)
                {
                    break;
                }

                yield return null;
            }

            // STEP 4: 최종 목적지 안착 및 복구
            yenbeolTransform.position = destination.position;
            playerTransform.position = destination.position + carryOffset;

            yield return null;

            if (_playerRb != null)
            {
                _playerRb.isKinematic = _wasKinematic;
                _playerRb.linearVelocity = Vector3.zero;
                _playerRb.angularVelocity = Vector3.zero;
            }

            foreach (var script in disabledScripts)
            {
                if (script != null) script.enabled = true;
            }

            if (cc != null && wasCcEnabled)
            {
                cc.enabled = true;
            }

            if (InputManager.Instance != null)
            {
                InputManager.Instance.enabled = true;
            }

            _carryCoroutine = null;
            IsOn = true;
        }

        private bool IsQtePressed()
        {
            if (Input.GetKeyDown(qteKey)) return true;

#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (qteKey == KeyCode.Space && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame) return true;
                if (qteKey == KeyCode.Return && UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame) return true;
                if (qteKey == KeyCode.E && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame) return true;
                if (qteKey == KeyCode.F && UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame) return true;
            }
            if (UnityEngine.InputSystem.Gamepad.current != null)
            {
                if (UnityEngine.InputSystem.Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
            }
#endif
            return false;
        }

        private Vector3 GetPointOnPath(List<Vector3> pathPoints, float[] segmentLengths, float totalPathLength, float progress, out Vector3 forwardDir)
        {
            if (pathPoints == null || pathPoints.Count < 2)
            {
                forwardDir = (yenbeolTransform != null) ? yenbeolTransform.forward : Vector3.forward;
                return (pathPoints != null && pathPoints.Count == 1) ? pathPoints[0] : Vector3.zero;
            }

            progress = Mathf.Clamp01(progress);
            float targetDist = progress * totalPathLength;
            float accumulated = 0f;

            for (int i = 0; i < segmentLengths.Length; i++)
            {
                float segLen = segmentLengths[i];
                if (accumulated + segLen >= targetDist || i == segmentLengths.Length - 1)
                {
                    float segProgress = (segLen > 0.0001f) ? Mathf.Clamp01((targetDist - accumulated) / segLen) : 1f;
                    Vector3 diff = pathPoints[i + 1] - pathPoints[i];
                    forwardDir = diff.normalized;
                    if (forwardDir == Vector3.zero)
                    {
                        forwardDir = (yenbeolTransform != null) ? yenbeolTransform.forward : Vector3.forward;
                    }
                    return Vector3.Lerp(pathPoints[i], pathPoints[i + 1], segProgress);
                }
                accumulated += segLen;
            }

            forwardDir = (pathPoints[pathPoints.Count - 1] - pathPoints[pathPoints.Count - 2]).normalized;
            return pathPoints[pathPoints.Count - 1];
        }

        private void PlaySFX(string soundKey, Vector3 position)
        {
            if (string.IsNullOrEmpty(soundKey) || SoundManager.Instance == null) return;
            SoundManager.Instance.PlaySFX(soundKey, position);
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

        private void OnDisable()
        {
            if (_carryCoroutine != null)
            {
                StopCoroutine(_carryCoroutine);
                _carryCoroutine = null;
            }
        }
    }
}