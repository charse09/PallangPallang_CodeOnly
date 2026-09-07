/*

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// Yenbeol(벌)이 플레이어를 들고 이동할 때, 
    /// 플레이어가 Space바를 연타해야 고도를 유지/상승시키며 목적지까지 날아가는 QTE Output.
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

        [Header("Flight Settings")]
        [Tooltip("Yenbeol이 날아가는 기본 수평 속도 (m/s)")]
        [SerializeField] private float flySpeed = 4f;

        [Tooltip("도착 판정 수평 거리 (m)")]
        [SerializeField] private float arrivalThreshold = 0.2f;

        [Header("★ Space Mash QTE Settings (스페이스바 연타 설정)")]
        [Tooltip("QTE에 사용할 키 (기본: Space)")]
        [SerializeField] private KeyCode qteKey = KeyCode.Space;

        [Tooltip("스페이스바 1회 연타 시 상승하는 Y축 고도 (m)")]
        [SerializeField] private float spaceBoostAmount = 0.35f;

        [Tooltip("연타를 안 할 때 초당 떨어지는 속도 (m/s)")]
        [SerializeField] private float gravityFallSpeed = 1.5f;

        [Tooltip("스페이스바로 올릴 수 있는 최대 고도 오프셋 (m)")]
        [SerializeField] private float maxQteHeightOffset = 2.0f;

        [Tooltip("연타 부족으로 떨어질 수 있는 최저 고도 오프셋 (m)")]
        [SerializeField] private float minQteHeightOffset = -2.5f;

        [Tooltip("고도 변화가 적용되는 부드러움 (Lerp 속도)")]
        [SerializeField] private float heightSmoothSpeed = 10f;

        [Header("Heavy & Unstable Motion (위태로운 연출)")]
        [Tooltip("무거워서 아래로 쳐지는 Y축 미세 진동 폭")]
        [SerializeField] private float heavyDipAmount = 0.2f;

        [Tooltip("고도 미세 진동 속도")]
        [SerializeField] private float heavyDipSpeed = 8.0f;

        [Tooltip("비틀거리는 좌우 흔들림 폭")]
        [SerializeField] private float wobbleAmount = 0.25f;

        [Tooltip("비틀거리는 좌우 흔들림 속도")]
        [SerializeField] private float wobbleSpeed = 8.0f;

        [Tooltip("몸체가 기울어지는 최대 각도")]
        [SerializeField] private float maxTiltAngle = 15.0f;

        // 내부 상태
        private Coroutine _carryCoroutine;
        private Rigidbody _playerRb;
        private bool _wasKinematic;
        private float _currentQteOffset = 0f;
        private float _targetQteOffset = 0f;

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

            // STEP 1: 다른 플레이어 입력 및 이동 스크립트 전역 잠금
            foreach (var script in scriptsToDisable)
            {
                if (script != null) script.enabled = false;
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

            yield return null;
            yield return null;

            // STEP 2: 플레이어 잡은 위치 오프셋 고정
            Vector3 carryOffset = playerTransform.position - yenbeolTransform.position;

            // STEP 3: 경로 구성
            List<Vector3> pathTargets = new List<Vector3>();
            foreach (var wp in waypoints)
            {
                if (wp != null) pathTargets.Add(wp.position);
            }
            pathTargets.Add(destination.position);

            float totalFlightTime = 0f;
            _targetQteOffset = 0f;
            _currentQteOffset = 0f;

            foreach (Vector3 target in pathTargets)
            {
                Vector3 segmentStartPos = yenbeolTransform.position;

                while (GetHorizontalDistance(yenbeolTransform.position, target) > arrivalThreshold)
                {
                    totalFlightTime += Time.deltaTime;

                    // ---------------------------------------------------------
                    // ★ 핵심 QTE 로직: Space 연타 감지 및 중력 가감산
                    // ---------------------------------------------------------
                    // InputManager가 꺼져있어도 유니티 자체 Input.GetKeyDown은 정상 동작함!
                    if (Input.GetKeyDown(qteKey))
                    {
                        _targetQteOffset += spaceBoostAmount;
                    }

                    // 시간 흐름에 따른 고도 수직 하강 (연타 늦으면 밑으로 떨어짐)
                    _targetQteOffset -= gravityFallSpeed * Time.deltaTime;

                    // 고도 범위 제한 (최대/최저 오프셋)
                    _targetQteOffset = Mathf.Clamp(_targetQteOffset, minQteHeightOffset, maxQteHeightOffset);

                    // 고도 보간 (갑자기 툭툭 튀지 않고 부드럽게 끙차 올라가고 떨어지도록 처리)
                    _currentQteOffset = Mathf.Lerp(_currentQteOffset, _targetQteOffset, Time.deltaTime * heightSmoothSpeed);

                    // ---------------------------------------------------------
                    // 위치 및 흔들림 연산
                    // ---------------------------------------------------------
                    float currentFlatDistToTarget = GetHorizontalDistance(yenbeolTransform.position, target);
                    float currentSegmentTravelledXZ = GetHorizontalDistance(segmentStartPos, yenbeolTransform.position);

                    Vector3 steerDir = (target - yenbeolTransform.position);
                    steerDir.y = 0;
                    steerDir.Normalize();

                    Vector3 rightVec = Vector3.Cross(Vector3.up, steerDir).normalized;
                    if (rightVec == Vector3.zero) rightVec = Vector3.right;

                    // 기본 선형 Y 고도
                    float baseHeight = Mathf.Lerp(segmentStartPos.y, target.y, currentSegmentTravelledXZ / Mathf.Max(GetHorizontalDistance(segmentStartPos, target), 0.001f));

                    // 비틀거림/무게감 노이즈
                    float arrivalFade = Mathf.Clamp01(currentFlatDistToTarget / 1.0f);
                    float verticalDip = -Mathf.Abs(Mathf.Sin(totalFlightTime * heavyDipSpeed)) * heavyDipAmount * arrivalFade;
                    float horizontalWobble = Mathf.Sin(totalFlightTime * wobbleSpeed) * wobbleAmount * arrivalFade;

                    // 최종 좌표 = 수평 이동 + 기본 Y + QTE 고도 오프셋 + 미세 진동
                    float stepSize = Mathf.Min(flySpeed * Time.deltaTime, currentFlatDistToTarget);
                    Vector3 nextBasePos = yenbeolTransform.position + (steerDir * stepSize);

                    nextBasePos.y = baseHeight + _currentQteOffset + verticalDip;

                    Vector3 finalPos = nextBasePos + (rightVec * horizontalWobble * Time.deltaTime * 5f);
                    yenbeolTransform.position = finalPos;

                    // 플레이어 동기화
                    playerTransform.position = yenbeolTransform.position + carryOffset;

                    // 회전 연출
                    if (currentFlatDistToTarget > 0.3f)
                    {
                        Quaternion baseRot = Quaternion.LookRotation(steerDir);
                        float tiltRoll = Mathf.Sin(totalFlightTime * wobbleSpeed) * maxTiltAngle * arrivalFade;
                        float tiltPitch = Mathf.Cos(totalFlightTime * heavyDipSpeed) * (maxTiltAngle * 0.5f) * arrivalFade;
                        Quaternion tiltRot = Quaternion.Euler(tiltPitch, 0f, tiltRoll);

                        yenbeolTransform.rotation = Quaternion.Slerp(
                            yenbeolTransform.rotation, baseRot * tiltRot, Time.deltaTime * 6f);
                    }

                    yield return null;
                }

                yenbeolTransform.position = target;
                playerTransform.position = target + carryOffset;
            }

            // STEP 4: 복구
            yield return null;

            if (_playerRb != null)
            {
                _playerRb.isKinematic = _wasKinematic;
                _playerRb.linearVelocity = Vector3.zero;
                _playerRb.angularVelocity = Vector3.zero;
            }

            for (int i = scriptsToDisable.Count - 1; i >= 0; i--)
            {
                if (scriptsToDisable[i] != null)
                    scriptsToDisable[i].enabled = true;
            }

            if (InputManager.Instance != null)
            {
                InputManager.Instance.enabled = true;
            }

            _carryCoroutine = null;
            IsOn = true;
        }

        private float GetHorizontalDistance(Vector3 p1, Vector3 p2)
        {
            Vector2 v1 = new Vector2(p1.x, p1.z);
            Vector2 v2 = new Vector2(p2.x, p2.z);
            return Vector2.Distance(v1, v2);
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
    }
}


*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// Yenbeol(벌)이 무거운 플레이어를 들고 이동할 때, 
    /// 스페이스바를 누르면 위로 솟구치고, 안 누르면 부드럽게 낙하하며
    /// 절대 바닥을 뚫지 않고 질질 끌려가는 QTE Output.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Outputs/벌 플레이어 운반 (부드러운 낙하 & 바닥 뚫림 완벽 방지 QTE)")]
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

        [Header("Flight Settings")]
        [Tooltip("Yenbeol이 날아가는 기본 수평 속도 (m/s)")]
        [SerializeField] private float flySpeed = 4f;

        [Tooltip("도착 판정 수평 거리 (m)")]
        [SerializeField] private float arrivalThreshold = 0.2f;

        [Header("★ Space & Soft Gravity (부드러운 추락 & 솟구침)")]
        [Tooltip("QTE에 사용할 키 (기본: Space)")]
        [SerializeField] private KeyCode qteKey = KeyCode.Space;

        [Tooltip("스페이스바 1회 누를 때 위로 솟구치는 힘 (Impulse)")]
        [SerializeField] private float spaceImpulse = 4.0f;

        [Tooltip("기본 중력 세기 (자연스러운 낙하감: 9.8 전후)")]
        [SerializeField] private float gravity = 9.8f;

        [Tooltip("하강 중일 때 적용할 추가 중력 배율 (1.2 ~ 1.4 추천)")]
        [SerializeField] private float fallMultiplier = 1.3f;

        [Tooltip("★ 최대 낙하 속도 제한 (이 수치 이상으로 확 떨어지지 않음)")]
        [SerializeField] private float maxFallSpeed = 3.5f;

        [Tooltip("스페이스바로 도달할 수 있는 최고 높이 오프셋 (m)")]
        [SerializeField] private float maxQteHeightOffset = 2.0f;

        [Header("★ Perfect Ground Protection (바닥 뚫림 100% 방지)")]
        [Tooltip("체크 시 바닥 지형을 감지하여 플레이어가 땅을 뚫지 않도록 합니다.")]
        [SerializeField] private bool preventGroundClipping = true;

        [Tooltip("바닥으로 인식할 지형 LayerMask (Default, Ground 등)")]
        [SerializeField] private LayerMask groundLayer;

        [Tooltip("플레이어 발바닥 기준 추가 Y 오프셋 (0이면 발바닥이 바닥에 맞닿음)")]
        [SerializeField] private float playerBottomOffset = 0.0f;

        [Header("Heavy & Unstable Motion (위태로운 연출)")]
        [Tooltip("무거워서 아래로 쳐지는 Y축 미세 진동 폭")]
        [SerializeField] private float heavyDipAmount = 0.15f;

        [Tooltip("고도 미세 진동 속도")]
        [SerializeField] private float heavyDipSpeed = 8.0f;

        [Tooltip("비틀거리는 좌우 흔들림 폭")]
        [SerializeField] private float wobbleAmount = 0.2f;

        [Tooltip("비틀거리는 좌우 흔들림 속도")]
        [SerializeField] private float wobbleSpeed = 8.0f;

        [Tooltip("몸체가 기울어지는 최대 각도")]
        [SerializeField] private float maxTiltAngle = 12.0f;

        // 내부 물리 상태
        private Coroutine _carryCoroutine;
        private Rigidbody _playerRb;
        private bool _wasKinematic;
        
        private float _verticalVelocity = 0f;
        private float _qteHeightOffset = 0f;

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
            foreach (var script in scriptsToDisable)
            {
                if (script != null) script.enabled = false;
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

            yield return null;
            yield return null;

            // STEP 2: 잡은 시점 오프셋 고정
            Vector3 carryOffset = playerTransform.position - yenbeolTransform.position;

            // STEP 3: 이동 경로 포인트 구성
            List<Vector3> pathTargets = new List<Vector3>();
            foreach (var wp in waypoints)
            {
                if (wp != null) pathTargets.Add(wp.position);
            }
            pathTargets.Add(destination.position);

            float totalFlightTime = 0f;
            _verticalVelocity = 0f;
            _qteHeightOffset = 0f;

            foreach (Vector3 target in pathTargets)
            {
                Vector3 segmentStartPos = yenbeolTransform.position;

                while (GetHorizontalDistance(yenbeolTransform.position, target) > arrivalThreshold)
                {
                    totalFlightTime += Time.deltaTime;

                    // ---------------------------------------------------------
                    // 1. 중력 하강 & 부드러운 최대 낙하 속도 제한
                    // ---------------------------------------------------------
                    if (Input.GetKeyDown(qteKey))
                    {
                        _verticalVelocity = spaceImpulse;
                    }
                    else
                    {
                        float currentGravity = (_verticalVelocity < 0f) ? (gravity * fallMultiplier) : gravity;
                        _verticalVelocity -= currentGravity * Time.deltaTime;

                        // ★ [핵심] 낙하 속도가 maxFallSpeed를 넘지 않도록 제한 (너무 팍 떨어지는 현상 방지)
                        _verticalVelocity = Mathf.Max(_verticalVelocity, -maxFallSpeed);
                    }

                    // 수직 위치 갱신
                    _qteHeightOffset += _verticalVelocity * Time.deltaTime;

                    // 천장(최고 높이) 제한
                    if (_qteHeightOffset > maxQteHeightOffset)
                    {
                        _qteHeightOffset = maxQteHeightOffset;
                        if (_verticalVelocity > 0f) _verticalVelocity = 0f;
                    }

                    // ---------------------------------------------------------
                    // 2. 수평 전진 및 기본 고도 연산
                    // ---------------------------------------------------------
                    float currentFlatDistToTarget = GetHorizontalDistance(yenbeolTransform.position, target);
                    float currentSegmentTravelledXZ = GetHorizontalDistance(segmentStartPos, yenbeolTransform.position);

                    Vector3 steerDir = (target - yenbeolTransform.position);
                    steerDir.y = 0;
                    steerDir.Normalize();

                    Vector3 rightVec = Vector3.Cross(Vector3.up, steerDir).normalized;
                    if (rightVec == Vector3.zero) rightVec = Vector3.right;

                    float baseHeight = Mathf.Lerp(segmentStartPos.y, target.y, currentSegmentTravelledXZ / Mathf.Max(GetHorizontalDistance(segmentStartPos, target), 0.001f));

                    float arrivalFade = Mathf.Clamp01(currentFlatDistToTarget / 1.0f);
                    float verticalDip = -Mathf.Abs(Mathf.Sin(totalFlightTime * heavyDipSpeed)) * heavyDipAmount * arrivalFade;
                    float horizontalWobble = Mathf.Sin(totalFlightTime * wobbleSpeed) * wobbleAmount * arrivalFade;

                    float stepSize = Mathf.Min(flySpeed * Time.deltaTime, currentFlatDistToTarget);
                    Vector3 nextBasePos = yenbeolTransform.position + (steerDir * stepSize);

                    // 벌 및 플레이어 예상 고도
                    nextBasePos.y = baseHeight + _qteHeightOffset + verticalDip;
                    float expectedPlayerY = nextBasePos.y + carryOffset.y;

                    // ---------------------------------------------------------
                    // ★ 3. 절대 안전 상공 레이캐스트 (바닥 뚫림 100% 방지)
                    // ---------------------------------------------------------
                    if (preventGroundClipping)
                    {
                        // 플레이어 위치가 아닌 '안전한 기본 상공 고도(baseHeight + 5m)'에서 항상 아래로 레이를 쏨
                        Vector3 rayStart = new Vector3(nextBasePos.x, baseHeight + 5.0f, nextBasePos.z);
                        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hitInfo, 15.0f, groundLayer))
                        {
                            float groundSurfaceY = hitInfo.point.y + playerBottomOffset;

                            // 플레이어 발/몸통이 바닥보다 아래로 파묻히려고 하면
                            if (expectedPlayerY < groundSurfaceY)
                            {
                                // 바닥 표면에 완벽하게 고정
                                expectedPlayerY = groundSurfaceY;
                                nextBasePos.y = expectedPlayerY - carryOffset.y;

                                // 현재 바닥 고도 기준으로 오프셋 역계산 & 하강 속도 0 리셋
                                _qteHeightOffset = nextBasePos.y - (baseHeight + verticalDip);
                                if (_verticalVelocity < 0f)
                                {
                                    _verticalVelocity = 0f;
                                }
                            }
                        }
                    }

                    // 위치 반영
                    Vector3 finalPos = nextBasePos + (rightVec * horizontalWobble * Time.deltaTime * 5f);
                    yenbeolTransform.position = finalPos;

                    // 플레이어 위치 동기화 (바닥에 딱 맞닿은 채로 전진)
                    playerTransform.position = yenbeolTransform.position + carryOffset;

                    // 비틀거림 회전 연출
                    if (currentFlatDistToTarget > 0.3f)
                    {
                        Quaternion baseRot = Quaternion.LookRotation(steerDir);
                        float tiltRoll = Mathf.Sin(totalFlightTime * wobbleSpeed) * maxTiltAngle * arrivalFade;
                        float tiltPitch = Mathf.Cos(totalFlightTime * heavyDipSpeed) * (maxTiltAngle * 0.5f) * arrivalFade;
                        Quaternion tiltRot = Quaternion.Euler(tiltPitch, 0f, tiltRoll);

                        yenbeolTransform.rotation = Quaternion.Slerp(
                            yenbeolTransform.rotation, baseRot * tiltRot, Time.deltaTime * 6f);
                    }

                    yield return null;
                }

                yenbeolTransform.position = target;
                playerTransform.position = target + carryOffset;
            }

            // STEP 4: 복구
            yield return null;

            if (_playerRb != null)
            {
                _playerRb.isKinematic = _wasKinematic;
                _playerRb.linearVelocity = Vector3.zero;
                _playerRb.angularVelocity = Vector3.zero;
            }

            for (int i = scriptsToDisable.Count - 1; i >= 0; i--)
            {
                if (scriptsToDisable[i] != null)
                    scriptsToDisable[i].enabled = true;
            }

            if (InputManager.Instance != null)
            {
                InputManager.Instance.enabled = true;
            }

            _carryCoroutine = null;
            IsOn = true;
        }

        private float GetHorizontalDistance(Vector3 p1, Vector3 p2)
        {
            Vector2 v1 = new Vector2(p1.x, p1.z);
            Vector2 v2 = new Vector2(p2.x, p2.z);
            return Vector2.Distance(v1, v2);
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
    }
}