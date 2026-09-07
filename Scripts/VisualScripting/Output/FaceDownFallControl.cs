using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 오브젝트가 얼굴을 바닥으로 향한 엎드린 자세를 유지하면서 아래로 떨어지고,
    /// 플레이어가 WASD(Move 입력)로 낙하 방향(수평 드리프트)을 조종할 수 있는 Output 노드.
    ///
    /// [동작 방식]
    ///   - Execute() 호출 시 PlayerLocomotions.isFallingControl = true 로 설정하여
    ///     PlayerLocomotions의 물리 연산을 완전 차단한 뒤,
    ///     Rigidbody.linearVelocity를 직접 제어합니다.
    ///   - W/S(Move Y축) → 전/후 방향 드리프트 (카메라 기준)
    ///   - A/D(Move X축) → 좌/우 방향 드리프트
    ///   - 종이 휘날림 효과: 랜덤 바람 + 공기 저항 + flutter 회전.
    ///   - 착지 감지: 지정한 Ground Layer에 Raycast가 닿으면 자동 착지(IsOn = true).
    ///   - 착지 또는 Reset() 호출 시 PlayerLocomotions.isFallingControl = false 복원.
    /// </summary>
    public class FaceDownFallControl : ProcessBase
    {
        [Header("References")]
        [Tooltip("떨어뜨릴 오브젝트 Transform.\n비워두면 이 컴포넌트가 붙은 오브젝트 자신을 사용합니다.")]
        [SerializeField] private Transform fallingObject;

        [Tooltip("이동 방향 기준 카메라.\n비워두면 월드 좌표 기준으로 이동합니다.")]
        [SerializeField] private Camera referenceCamera;

        [Header("Fall Settings")]
        [Tooltip("아래(-Y)로 떨어지는 속도 (units/s)")]
        [SerializeField] private float fallSpeed = 5f;

        [Tooltip("WASD로 수평 이동하는 최대 속도 (units/s)")]
        [SerializeField] private float moveSpeed = 4f;

        [Tooltip("수평 이동 시 부드럽게 가속/감속하는 속도.\n0이면 즉시 반응, 클수록 관성이 생깁니다.")]
        [SerializeField] private float moveLerpSpeed = 8f;

        [Header("Face Down Settings")]
        [Tooltip("엎드린 자세를 만드는 Euler Rotation.\n기본값 (90, 0, 0)은 앞으로 눕힘입니다.")]
        [SerializeField] private Vector3 faceDownEulerAngles = new Vector3(90f, 0f, 0f);

        [Tooltip("WASD 이동 시 이동 방향으로 살짝 기울어지는 최대 각도 (도).")]
        [SerializeField] private float tiltAngle = 15f;

        [Header("Ground Detection")]
        [Tooltip("이 Layer에 닿으면 자동으로 착지 처리합니다.")]
        [SerializeField] private LayerMask groundLayer;

        [Tooltip("착지 감지 Raycast 거리 (오브젝트 중심 기준)")]
        [SerializeField] private float groundCheckDistance = 1f;

        [Tooltip("착지 후 위치를 바닥에 정확히 맞출지 여부")]
        [SerializeField] private bool snapToGround = true;

        [Header("Paper Flutter Settings")]
        [Tooltip("랜덤 바람이 주는 최대 수평 속도 (units/s).\nmoveSpeed보다 작게 유지 권장. (예: moveSpeed 12 → windMaxSpeed 3~5)")]
        [SerializeField] private float windMaxSpeed = 4f;

        [Tooltip("바람 방향이 바뀌는 주기 (초).")]
        [SerializeField] private float windChangeInterval = 0.6f;

        [Tooltip("공기 저항 계수. 높을수록 저항이 강하게 감속됩니다. (0.85~0.95 권장)")]
        [Range(0f, 1f)]
        [SerializeField] private float airDrag = 0.90f;

        [Tooltip("종이가 팔랑이듯 회전하는 최대 각도 (도). (15~30 권장)")]
        [SerializeField] private float flutterRotationMax = 20f;

        [Tooltip("flutter 회전이 변하는 속도.")]
        [SerializeField] private float flutterSpeed = 3f;

        [Tooltip("flutter 방향이 바뀌는 주기 (초).")]
        [SerializeField] private float flutterFrequency = 0.8f;

        // ─── 내부 상태 ───────────────────────────────────────────
        private bool               _isActive       = false;
        private Quaternion         _faceDownBase;
        private Rigidbody          _rb;
        private PlayerLocomotions  _locomotion;

        // 입력에 의한 수평 속도 (바람과 분리)
        private Vector3 _inputVelocity  = Vector3.zero;

        // 바람에 의한 수평 속도
        private Vector3 _windVelocity   = Vector3.zero;
        private Vector3 _targetWindVel  = Vector3.zero;
        private float   _windTimer      = 0f;

        // flutter 회전
        private float _flutterTiltX   = 0f;
        private float _flutterTiltZ   = 0f;
        private float _flutterTargetX = 0f;
        private float _flutterTargetZ = 0f;
        private float _flutterTimer   = 0f;

        // ──────────────────────────────────────────────────────────

        private void Awake()
        {
            if (fallingObject == null)
                fallingObject = transform;

            // fallingObject 또는 그 부모에서 Rigidbody + PlayerLocomotions 탐색
            _rb         = fallingObject.GetComponentInParent<Rigidbody>();
            _locomotion = fallingObject.GetComponentInParent<PlayerLocomotions>();
        }

        public override void Execute()
        {
            if (fallingObject == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> [FaceDownFallControl] " +
                                 "fallingObject가 없습니다!");
#endif
                IsOn = true;
                return;
            }

            // ── PlayerLocomotions 물리 차단 ───────────────────────
            if (_locomotion != null)
            {
                _locomotion.isFallingControl = true;
                // 기존 속도 초기화
                if (_rb != null)
                {
                    _rb.linearVelocity  = Vector3.zero;
                    _rb.angularVelocity = Vector3.zero;
                }
            }

            // 엎드린 자세 기준 설정
            _faceDownBase = Quaternion.Euler(faceDownEulerAngles);

            // 즉시 엎드린 자세 적용
            fallingObject.rotation = _faceDownBase;

            // 속도 초기화
            _inputVelocity  = Vector3.zero;
            _windVelocity   = Vector3.zero;
            _targetWindVel  = RandomHorizontalVelocity(windMaxSpeed);
            _windTimer      = 0f;

            // flutter 초기화
            _flutterTiltX = 0f;
            _flutterTiltZ = 0f;
            RandomizeFlutterTarget();

            _isActive = true;

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> [FaceDownFallControl] " +
                      "낙하 시작. WASD로 방향 조종 가능.");
#endif
        }

        private void Update()
        {
            if (!_isActive || fallingObject == null) return;

            // ── 1. 입력 읽기 ──────────────────────────────────────
            Vector2 input = Vector2.zero;
            if (InputManager.Instance != null)
                input = InputManager.Instance.Movement;
            else
                input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

            // ── 2. 카메라 기준 이동 방향 계산 ────────────────────
            // [버그 수정] 카메라가 완전히 수직(위에서 내려다봄)일 때
            // forward.y = 0 후 크기가 거의 0이 되어 W/S 입력이 무시되던 문제 수정.
            Vector3 camForward = Vector3.forward;
            Vector3 camRight   = Vector3.right;

            if (referenceCamera != null)
            {
                Vector3 cf = referenceCamera.transform.forward;
                cf.y = 0f;

                if (cf.sqrMagnitude > 0.001f)
                {
                    camForward = cf.normalized;
                }
                else
                {
                    // 카메라가 정수직: up 벡터의 XZ 성분을 forward 대신 사용
                    // (부호는 카메라가 아래를 향할 때 플레이어 관점의 "앞"과 일치하도록 그대로 사용)
                    Vector3 cu = referenceCamera.transform.up;
                    cu.y = 0f;
                    if (cu.sqrMagnitude > 0.001f)
                        camForward = cu.normalized;
                }

                camRight = referenceCamera.transform.right;
                camRight.y = 0f;
                if (camRight.sqrMagnitude > 0.001f) camRight = camRight.normalized;
            }

            // ── 3. 입력 목표 속도 ─────────────────────────────────
            // [N/S 반전 수정] input.y와 camForward 부호가 일치하도록
            // 이 씬의 카메라는 플레이어를 뒤에서 바라보므로 camForward가 곧 "앞" 방향.
            // 단, W(input.y = +1)를 눌렀을 때 플레이어 기준 "앞(북)"으로 가야 하므로 부호를 유지.
            Vector3 targetInput = (camForward * input.y + camRight * input.x) * moveSpeed;

            _inputVelocity = moveLerpSpeed > 0f
                ? Vector3.Lerp(_inputVelocity, targetInput, Time.deltaTime * moveLerpSpeed)
                : targetInput;

            // ── 4. 바람 속도 업데이트 ────────────────────────────
            _windTimer += Time.deltaTime;
            if (_windTimer >= windChangeInterval)
            {
                _windTimer     = 0f;
                _targetWindVel = RandomHorizontalVelocity(windMaxSpeed);
            }
            _windVelocity = Vector3.Lerp(_windVelocity, _targetWindVel, Time.deltaTime * 1.5f);

            // ── 5. 수평 속도 합산 + 공기 저항 ────────────────────
            Vector3 totalHorizontal = (_inputVelocity + _windVelocity)
                                      * Mathf.Pow(airDrag, Time.deltaTime * 60f);

            // ── 6. 낙하 속도 포함한 최종 velocity 설정 ───────────
            Vector3 finalVelocity = totalHorizontal;
            finalVelocity.y = -fallSpeed; // 일정 속도로 낙하 (가속 없음)

            if (_rb != null)
                _rb.linearVelocity = finalVelocity;
            else
                fallingObject.position += finalVelocity * Time.deltaTime;

            // ── 7. flutter 회전 업데이트 ─────────────────────────
            UpdateFlutter();

            // ── 8. 회전 적용 ─────────────────────────────────────
            ApplyRotation(input);

            // ── 9. 착지 감지 ─────────────────────────────────────
            if (groundLayer != 0)
            {
                Vector3 origin = (_rb != null) ? _rb.position : fallingObject.position;
                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                                    groundCheckDistance, groundLayer))
                {
                    if (snapToGround && _rb != null)
                    {
                        _rb.MovePosition(new Vector3(_rb.position.x, hit.point.y, _rb.position.z));
                    }
                    else if (snapToGround)
                    {
                        Vector3 p = fallingObject.position;
                        p.y = hit.point.y;
                        fallingObject.position = p;
                    }

                    Land();
                }
            }
        }

        private void RandomizeFlutterTarget()
        {
            _flutterTargetX = Random.Range(-flutterRotationMax, flutterRotationMax);
            _flutterTargetZ = Random.Range(-flutterRotationMax, flutterRotationMax);
            _flutterTimer   = Random.Range(flutterFrequency * 0.5f, flutterFrequency * 1.5f);
        }

        private void UpdateFlutter()
        {
            _flutterTimer -= Time.deltaTime;
            if (_flutterTimer <= 0f) RandomizeFlutterTarget();

            _flutterTiltX = Mathf.Lerp(_flutterTiltX, _flutterTargetX, Time.deltaTime * flutterSpeed);
            _flutterTiltZ = Mathf.Lerp(_flutterTiltZ, _flutterTargetZ, Time.deltaTime * flutterSpeed);
        }

        private void ApplyRotation(Vector2 input)
        {
            float totalTiltX = (-input.y * tiltAngle) + _flutterTiltX;
            float totalTiltZ = ( input.x * tiltAngle) + _flutterTiltZ;

            Quaternion tilt = Quaternion.Euler(totalTiltX, 0f, totalTiltZ);

            fallingObject.rotation = Quaternion.Slerp(
                fallingObject.rotation,
                _faceDownBase * tilt,
                Time.deltaTime * flutterSpeed
            );
        }

        private Vector3 RandomHorizontalVelocity(float maxSpeed)
        {
            float angle    = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float strength = Random.Range(maxSpeed * 0.3f, maxSpeed);
            return new Vector3(Mathf.Cos(angle) * strength, 0f, Mathf.Sin(angle) * strength);
        }

        private void Land()
        {
            _isActive = false;

            // PlayerLocomotions 물리 복원
            if (_locomotion != null)
            {
                _locomotion.isFallingControl = false;
                // 착지 시 velocity 초기화해서 급격한 관성 방지
                if (_rb != null) _rb.linearVelocity = Vector3.zero;
            }

            fallingObject.rotation = _faceDownBase;
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> [FaceDownFallControl] 착지 완료!");
#endif
        }

        public override void Reset()
        {
            base.Reset();

            if (_isActive)
            {
                // Reset이 강제 호출된 경우에도 물리 복원
                if (_locomotion != null) _locomotion.isFallingControl = false;
                if (_rb != null)         _rb.linearVelocity = Vector3.zero;
            }

            _isActive      = false;
            _inputVelocity = Vector3.zero;
            _windVelocity  = Vector3.zero;
            _targetWindVel = Vector3.zero;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (fallingObject == null) return;

            Vector3 origin = fallingObject.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckDistance);
            Gizmos.DrawWireSphere(origin + Vector3.down * groundCheckDistance, 0.15f);

            if (Application.isPlaying && _isActive)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(origin, _windVelocity.normalized * 1.5f);
            }
        }
#endif
    }
}
