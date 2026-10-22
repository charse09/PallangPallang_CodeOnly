//using System.Collections;
//using System.Reflection;
//using UnityEngine;
//using _Project.Scripts.VisualScripting;

//namespace _Project.Scripts.VisualScripting
//{
//    /// <summary>
//    /// 종이(PaperState) 상태에서 글라이더처럼 전방으로 비행하는 Output 노드.
//    /// - 지상/공중/점프 중 어느 상태에서 실행되어도 안정적인 글라이딩 보장
//    /// - 점프 코루틴 잔여 실행 차단 및 Space 키 조기 캔슬 방지 유예 시간 적용
//    /// - 무입력 시: 지정된 속도로 전방 직진 활공
//    /// - 좌우 입력 시: 90도로 꺾이지 않고 설정한 뱅킹 각도만큼만 기울어지며 비행 방향 선회
//    /// - 벽 충돌 시: 벽면을 타고 미끄러지며(Wall Slide) 전진 유지
//    /// </summary>
//    [AddComponentMenu("Visual Scripting/Outputs/Movement/Paper Gliding Flight (Output)")]
//    public class PaperGlidingFlightOutput : ProcessBase
//    {
//        public enum SteerMode
//        {
//            [Tooltip("A/D 입력 중일 때 계속해서 해당 방향으로 회전합니다.")]
//            ContinuousTurn,
//            [Tooltip("기준 방향으로부터 지정된 최대 편향 각도 범위(±MaxTurnAngle) 내에서만 기울어집니다.")]
//            ClampedAngle
//        }

//        [Header("--- [Target Settings] ---")]
//        [Tooltip("비행을 적용할 플레이어 (비어있으면 자동 탐색)")]
//        [SerializeField] private PlayerLocomotion playerLocomotion;
//        [Tooltip("비행 시작 기준 방향 Transform (비어있으면 카메라/플레이어 시선 방향 사용)")]
//        [SerializeField] private Transform initialDirectionTransform;

//        [Header("--- [Paper State Control] ---")]
//        [Tooltip("Output 실행 시 플레이어를 즉시 PaperState로 강제 변환합니다.")]
//        [SerializeField] private bool activatePaperStateOnExecute = true;
//        [Tooltip("PaperState 변환 시 슬림(Slim), 웅크리기(Brace), 슬라이딩 등 상충되는 상태를 즉시 해제합니다.")]
//        [SerializeField] private bool resetConflictingStates = true;
//        [Tooltip("비행 종료(착지 또는 캔슬) 후에도 PaperState 상태를 유지할지 여부 (false면 일반 상태로 복귀)")]
//        [SerializeField] private bool keepPaperStateOnExit = true;


//        [Header("--- [Flight Speed & Glide] ---")]
//        [Tooltip("전방 비행 추진 속도 (m/s)")]
//        [SerializeField] private float flightSpeed = 18.0f;
//        [Tooltip("활공 시 초당 자연 하강 속도 (0이면 수평 고도 유지)")]
//        [SerializeField] private float descentSpeed = 1.2f;

//        [Header("--- [Steering & Bank Angle] ---")]
//        [Tooltip("선회 조작 모드")]
//        [SerializeField] private SteerMode steerMode = SteerMode.ContinuousTurn;
//        [Tooltip("좌/우 입력 시 기체가 기울어지는 최대 롤(Roll) 각도 (단위: 도)")]
//        [Range(5f, 60f)]
//        [SerializeField] private float maxBankAngle = 30.0f;
//        [Tooltip("좌/우 선회 회전 속도 (ContinuousTurn 모드 시 초당 회전각, Clamped 모드 시 편향 한계각)")]
//        [Range(10f, 180f)]
//        [SerializeField] private float turnRate = 65.0f;
//        [Tooltip("기울기(Bank) 전환 반응 속도")]
//        [SerializeField] private float bankSharpness = 6.0f;
//        [Tooltip("키를 뗐을 때 수평으로 복귀하는 속도")]
//        [SerializeField] private float bankReturnSharpness = 8.0f;

//        [Header("--- [Wall Sliding Settings] ---")]
//        [Tooltip("벽면 및 장애물 판정용 레이어")]
//        [SerializeField] private LayerMask obstacleLayer = ~0;
//        [Tooltip("벽 충돌 감지용 구체 반경 (플레이어 두께)")]
//        [SerializeField] private float collisionRadius = 0.35f;
//        [Tooltip("벽면에 비벼질 때 유지되는 전진 속도 비율 (0.8 = 80% 속도로 감속 미끄러짐)")]
//        [Range(0.2f, 1.0f)]
//        [SerializeField] private float wallSlideFrictionMultiplier = 0.85f;
//        [Tooltip("벽면에 닿았을 때 진행 방향 벡터를 벽면 방향으로 부드럽게 유도(Deflection)할지 여부")]
//        [SerializeField] private bool deflectDirectionAlongWall = true;

//        [Header("--- [Paper Visual Flutter Settings] ---")]
//        [Tooltip("종이 비행 중 미세한 공기 저항 흔들림 연출 사용")]
//        [SerializeField] private bool useFlutterWobble = true;
//        [Tooltip("흔들림 주기")]
//        [SerializeField] private float flutterFrequency = 12.0f;
//        [Tooltip("흔들림 진폭 (각도)")]
//        [SerializeField] private float flutterAmplitude = 3.5f;

//        [Header("--- [Flight Termination Settings] ---")]
//        [Tooltip("지면에 닿았을 때 비행 자동 종료")]
//        [SerializeField] private bool exitOnGround = true;
//        [Tooltip("비행 시작 후 지면 감지를 무시할 유예 시간 (점프 직후 바닥 오감지 방지)")]
//        [SerializeField] private float groundCheckGraceDuration = 0.45f;
//        [Tooltip("지면 감지 거리")]
//        [SerializeField] private float groundCheckDistance = 0.4f;
//        [Tooltip("지면 감지 레이어")]
//        [SerializeField] private LayerMask groundLayer = ~0;
//        [Tooltip("점프 키(Space)를 눌러 비행 수동 캔슬 가능 여부")]
//        [SerializeField] private bool allowManualDismount = true;
//        [Tooltip("비행 시작 후 점프 캔슬 입력을 무시할 안전 시간 (점프하면서 비행 진입 시 즉시 캔슬 방지)")]
//        [SerializeField] private float dismountGraceDuration = 0.4f;
//        [Tooltip("비행 시간 제한 (0이면 착지 또는 캔슬 시까지 무제한)")]
//        [SerializeField] private float flightDuration = 0f;
//        [Tooltip("외부 종료 트리거 (해당 ProcessBase가 IsOn이 되면 즉시 착지)")]
//        [SerializeField] private ProcessBase exitTrigger;

//        [Header("--- [Audio (Optional)] ---")]
//        [SerializeField] private AudioClip flightWindLoopClip;
//        [SerializeField] private AudioSource audioSource;

//        private Coroutine _flightCoroutine;
//        private Rigidbody _playerRb;
//        private PlayerVisuals _playerVisuals;
//        private Animator _playerAnim;
//        private GameObject _flutterVisualObj;
//        private Transform _characterVisualTr;

//        private Vector3 _baseForward;
//        private Vector3 _currentFlightDirection;
//        private float _currentBankAngle = 0f;
//        private float _targetClampedYaw = 0f;
//        private bool _isFlying = false;

//        // PlayerLocomotion 내부 private 필드 캐싱 (점프 및 지면 상태 완벽 리셋용)
//        private static readonly FieldInfo FieldIsGrounded = typeof(PlayerLocomotion).GetField("isGrounded", BindingFlags.Instance | BindingFlags.NonPublic);
//        private static readonly FieldInfo FieldGroundTimer = typeof(PlayerLocomotion).GetField("groundCheckTimer", BindingFlags.Instance | BindingFlags.NonPublic);
//        private static readonly FieldInfo FieldCoyoteTimer = typeof(PlayerLocomotion).GetField("coyoteTimer", BindingFlags.Instance | BindingFlags.NonPublic);
//        private static readonly FieldInfo FieldJumpPending = typeof(PlayerLocomotion).GetField("isJumpingPending", BindingFlags.Instance | BindingFlags.NonPublic);
//        private static readonly FieldInfo FieldTargetRot = typeof(PlayerLocomotion).GetField("targetBaseRotation", BindingFlags.Instance | BindingFlags.NonPublic);

//        private void Awake()
//        {
//            FindPlayerIfNull();
//        }

//        private void FindPlayerIfNull()
//        {
//            if (playerLocomotion == null)
//            {
//                playerLocomotion = FindFirstObjectByType<PlayerLocomotion>();
//            }

//            if (playerLocomotion != null)
//            {
//                _playerRb = playerLocomotion.GetComponent<Rigidbody>();
//                _playerVisuals = playerLocomotion.GetComponentInChildren<PlayerVisuals>();
//                _playerAnim = playerLocomotion.GetComponentInChildren<Animator>();

//                if (_playerVisuals != null)
//                {
//                    _characterVisualTr = _playerVisuals.CharacterVisual;
//                    FieldInfo flutterField = typeof(PlayerVisuals).GetField("FlutterVisualPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
//                    if (flutterField != null)
//                    {
//                        _flutterVisualObj = flutterField.GetValue(_playerVisuals) as GameObject;
//                    }
//                }
//            }
//        }

//        public override void Execute()
//        {
//            FindPlayerIfNull();

//            if (playerLocomotion == null || _playerRb == null)
//            {
//#if UNITY_EDITOR
//                Debug.LogError($"<color=red>[{gameObject.name}]</color> PlayerLocomotion 또는 Rigidbody를 찾을 수 없습니다.");
//#endif
//                IsOn = true;
//                return;
//            }

//            IsOn = false;

//            // 1. 점프 상태 및 로코모션 잔여 로직 완벽 초기화
//            PreparePlayerForFlight();

//            // 2. 비행 루틴 시작
//            if (_flightCoroutine != null)
//            {
//                StopCoroutine(_flightCoroutine);
//            }

//            _flightCoroutine = StartCoroutine(FlightRoutine());
//        }

//        /// <summary>
//        /// 점프 중이든 지상 중이든 상관없이 모든 잔여 모션/코루틴을 청소하고 비행 상태로 설정합니다.
//        /// </summary>
//        private void PreparePlayerForFlight()
//        {
//            if (playerLocomotion == null) return;

//            // 진행 중이던 점프 코루틴(JumpRoutine 등) 완전 차단
//            playerLocomotion.StopAllCoroutines();

//            if (resetConflictingStates)
//            {
//                playerLocomotion.CancelSpecialStatesForCondition();
//            }

//            if (activatePaperStateOnExecute)
//            {
//                playerLocomotion.SetAbsolutePaperState(true);
//            }

//            // 점프 보류 플래그 및 지면 상태 강제 초기화
//            if (FieldJumpPending != null) FieldJumpPending.SetValue(playerLocomotion, false);
//            if (FieldIsGrounded != null) FieldIsGrounded.SetValue(playerLocomotion, false);
//            if (FieldGroundTimer != null) FieldGroundTimer.SetValue(playerLocomotion, 0f);
//            if (FieldCoyoteTimer != null) FieldCoyoteTimer.SetValue(playerLocomotion, 0f);

//            // 로코모션 속도 초기화
//            playerLocomotion.ResetAllVelocities();

//            // 애니메이터 파라미터 초기화
//            if (_playerAnim != null)
//            {
//                _playerAnim.SetBool("IsWalk", false);
//                _playerAnim.SetBool("IsCrouch", false);
//                _playerAnim.SetBool("IsHangOn", false);
//                _playerAnim.ResetTrigger("IsJump");
//            }

//            if (_playerVisuals != null)
//            {
//                _playerVisuals.SetWalkAnim(false);
//                _playerVisuals.SetCrouchAnim(false);
//                _playerVisuals.UpdateStates(paper: true, slim: false, completelySlim: false, windRolling: false);
//                _playerVisuals.SetNormalFace();
//            }
//        }

//        private void SetFlightVisualsActive(bool isFlying)
//        {
//            if (isFlying)
//            {
//                if (_flutterVisualObj != null)
//                {
//                    _flutterVisualObj.SetActive(true);
//                    if (_characterVisualTr != null) _characterVisualTr.gameObject.SetActive(false);
//                }
//                else if (_characterVisualTr != null)
//                {
//                    _characterVisualTr.gameObject.SetActive(true);
//                    _characterVisualTr.localPosition = Vector3.zero;
//                    _characterVisualTr.localRotation = Quaternion.identity;
//                }
//            }
//            else
//            {
//                if (_flutterVisualObj != null)
//                {
//                    _flutterVisualObj.SetActive(false);
//                }
//                if (_characterVisualTr != null)
//                {
//                    _characterVisualTr.gameObject.SetActive(true);
//                }
//            }
//        }

//        /// <summary>
//        /// 종이 상태의 회전 왜곡에 영향을 받지 않는 올바른 수평 전방 벡터를 추출합니다.
//        /// </summary>
//        private Vector3 CalculateReliableForwardDirection()
//        {
//            if (initialDirectionTransform != null)
//            {
//                Vector3 fwd = Vector3.ProjectOnPlane(initialDirectionTransform.forward, Vector3.up).normalized;
//                if (fwd.sqrMagnitude > 0.01f) return fwd;
//            }

//            // 1. 카메라 정면 방향 우선 (가장 직관적인 조준 방향)
//            Camera cam = Camera.main;
//            if (cam != null)
//            {
//                Vector3 camFwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
//                if (camFwd.sqrMagnitude > 0.01f) return camFwd;
//            }

//            // 2. 플레이어의 targetBaseRotation 추출 (로코모션이 바라보던 기준 방향)
//            if (playerLocomotion != null && FieldTargetRot != null)
//            {
//                Quaternion targetRot = (Quaternion)FieldTargetRot.GetValue(playerLocomotion);
//                Vector3 baseFwd = Vector3.ProjectOnPlane(targetRot * Vector3.forward, Vector3.up).normalized;
//                if (baseFwd.sqrMagnitude > 0.01f) return baseFwd;
//            }

//            return Vector3.forward;
//        }

//        private IEnumerator FlightRoutine()
//        {
//            _isFlying = true;

//            // 1. 비주얼 교체 및 로코모션 비활성화
//            SetFlightVisualsActive(true);
//            playerLocomotion.enabled = false;

//            if (_playerRb != null)
//            {
//                _playerRb.isKinematic = true;
//                _playerRb.linearVelocity = Vector3.zero;
//                _playerRb.angularVelocity = Vector3.zero;
//            }

//            // 2. 비행 기준 전방 벡터 확정
//            _baseForward = CalculateReliableForwardDirection();
//            _currentFlightDirection = _baseForward;
//            _currentBankAngle = 0f;
//            _targetClampedYaw = 0f;

//            if (audioSource != null && flightWindLoopClip != null)
//            {
//                audioSource.clip = flightWindLoopClip;
//                audioSource.loop = true;
//                audioSource.Play();
//            }

//#if UNITY_EDITOR
//            Debug.Log($"<color=cyan>[PaperGlidingFlight]</color> 글라이딩 비행 개시! (기준 방향: {_baseForward})");
//#endif

//            float timer = 0f;
//            yield return new WaitForFixedUpdate();

//            while (_isFlying)
//            {
//                timer += Time.fixedDeltaTime;

//                // 시간 초과 체크
//                if (flightDuration > 0f && timer >= flightDuration)
//                {
//#if UNITY_EDITOR
//                    Debug.Log("<color=yellow>[PaperGlidingFlight]</color> 비행 제한 시간 도달로 착지합니다.");
//#endif
//                    break;
//                }

//                // 외부 트리거 체크
//                if (exitTrigger != null && exitTrigger.IsOn)
//                {
//#if UNITY_EDITOR
//                    Debug.Log("<color=yellow>[PaperGlidingFlight]</color> 외부 트리거에 의해 비행을 종료합니다.");
//#endif
//                    break;
//                }

//                // 점프 키(Space) 수동 캔슬 (★ dismountGraceDuration 이후에만, 그리고 누른 '첫 프레임'만 체크)
//                if (allowManualDismount && timer >= dismountGraceDuration)
//                {
//                    bool jumpTriggered = (InputManager.Instance != null && InputManager.Instance.JumpTriggered) || Input.GetKeyDown(KeyCode.Space);
//                    if (jumpTriggered)
//                    {
//#if UNITY_EDITOR
//                        Debug.Log("<color=yellow>[PaperGlidingFlight]</color> 점프 키(Space) 입력으로 비행을 수동 캔슬합니다.");
//#endif
//                        break;
//                    }
//                }

//                // 좌우 이동 입력 수신 (A / D)
//                float inputX = 0f;
//                if (InputManager.Instance != null)
//                {
//                    inputX = InputManager.Instance.Movement.x;
//                }
//                if (Mathf.Abs(inputX) < 0.01f)
//                {
//                    inputX = Input.GetAxisRaw("Horizontal");
//                }

//                // 뱅킹(Roll) 계산 (무입력 시 수평 복귀)
//                float targetBank = -inputX * maxBankAngle;
//                float bankSharp = (Mathf.Abs(inputX) > 0.01f) ? bankSharpness : bankReturnSharpness;
//                _currentBankAngle = Mathf.Lerp(_currentBankAngle, targetBank, Time.fixedDeltaTime * bankSharp);

//                // 진행 방향 선회(Yaw) 계산
//                if (steerMode == SteerMode.ContinuousTurn)
//                {
//                    float yawDelta = inputX * turnRate * Time.fixedDeltaTime;
//                    _currentFlightDirection = Quaternion.Euler(0f, yawDelta, 0f) * _currentFlightDirection;
//                }
//                else
//                {
//                    _targetClampedYaw = Mathf.Lerp(_targetClampedYaw, inputX * turnRate, Time.fixedDeltaTime * bankSharpness);
//                    _currentFlightDirection = Quaternion.Euler(0f, _targetClampedYaw, 0f) * _baseForward;
//                }

//                // 종이 미세 흔들림(Flutter)
//                float flutterRoll = 0f;
//                if (useFlutterWobble)
//                {
//                    flutterRoll = Mathf.Sin(Time.time * flutterFrequency) * flutterAmplitude;
//                }

//                // 회전값 적용
//                Quaternion yawRotation = Quaternion.LookRotation(_currentFlightDirection, Vector3.up);
//                Quaternion bankRotation = Quaternion.Euler(0f, 0f, _currentBankAngle + flutterRoll);
//                Quaternion paperPosture = Quaternion.Euler(-90f, 0f, 0f);

//                Quaternion finalRotation = yawRotation * bankRotation * paperPosture;
//                if (_playerRb != null)
//                {
//                    _playerRb.MoveRotation(finalRotation);

//                    // =========================================================
//                    // 벽면 충돌 검출 및 미끄러짐(Wall Slide) 연산
//                    // =========================================================
//                    Vector3 moveVelocity = (_currentFlightDirection * flightSpeed) + (Vector3.down * descentSpeed);
//                    Vector3 moveDelta = moveVelocity * Time.fixedDeltaTime;
//                    float checkDistance = moveDelta.magnitude;

//                    Transform playerTr = playerLocomotion.transform;
//                    if (Physics.SphereCast(_playerRb.position, collisionRadius, moveDelta.normalized, out RaycastHit wallHit, checkDistance + 0.05f, obstacleLayer, QueryTriggerInteraction.Ignore))
//                    {
//                        if (!wallHit.collider.transform.IsChildOf(playerTr))
//                        {
//                            Vector3 wallNormal = wallHit.normal;
//                            float normalDot = Vector3.Dot(_currentFlightDirection, wallNormal);

//                            if (normalDot < 0f)
//                            {
//                                Vector3 flatNormal = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
//                                if (flatNormal.sqrMagnitude < 0.01f) flatNormal = wallNormal;

//                                Vector3 slidingVelocity = Vector3.ProjectOnPlane(moveVelocity, flatNormal) * wallSlideFrictionMultiplier;
//                                Vector3 remainingMove = slidingVelocity * Time.fixedDeltaTime;

//                                if (deflectDirectionAlongWall)
//                                {
//                                    Vector3 projectedFwd = Vector3.ProjectOnPlane(_currentFlightDirection, flatNormal).normalized;
//                                    if (projectedFwd.sqrMagnitude > 0.01f)
//                                    {
//                                        _currentFlightDirection = Vector3.Slerp(_currentFlightDirection, projectedFwd, Time.fixedDeltaTime * 10f);
//                                    }
//                                }

//                                Vector3 safeContactPoint = wallHit.point + (wallNormal * (collisionRadius + 0.02f));

//                                if (remainingMove.sqrMagnitude > 0.0001f && Physics.SphereCast(safeContactPoint, collisionRadius, remainingMove.normalized, out RaycastHit cornerHit, remainingMove.magnitude, obstacleLayer, QueryTriggerInteraction.Ignore))
//                                {
//                                    _playerRb.MovePosition(cornerHit.point + (cornerHit.normal * (collisionRadius + 0.02f)));
//                                }
//                                else
//                                {
//                                    _playerRb.MovePosition(safeContactPoint + remainingMove);
//                                }
//                            }
//                            else
//                            {
//                                _playerRb.MovePosition(_playerRb.position + moveDelta);
//                            }
//                        }
//                        else
//                        {
//                            _playerRb.MovePosition(_playerRb.position + moveDelta);
//                        }
//                    }
//                    else
//                    {
//                        _playerRb.MovePosition(_playerRb.position + moveDelta);
//                    }
//                }

//                // 바닥 착지 감지 (★ 점프 직후 바닥 오감지 방지를 위해 groundCheckGraceDuration 이후부터 검사)
//                if (exitOnGround && timer >= groundCheckGraceDuration && _playerRb != null)
//                {
//                    Vector3 rayOrigin = _playerRb.position + Vector3.up * 0.2f;
//                    if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, groundCheckDistance + 0.2f, groundLayer, QueryTriggerInteraction.Ignore))
//                    {
//                        if (!groundHit.collider.transform.IsChildOf(playerLocomotion.transform))
//                        {
//#if UNITY_EDITOR
//                            Debug.Log("<color=green>[PaperGlidingFlight]</color> 지면에 착지했습니다.");
//#endif
//                            break;
//                        }
//                    }
//                }

//                yield return new WaitForFixedUpdate();
//            }

//            FinishFlight();
//        }

//        private void FinishFlight()
//        {
//            if (!_isFlying) return;
//            _isFlying = false;

//            if (audioSource != null && audioSource.isPlaying)
//            {
//                audioSource.Stop();
//            }

//            SetFlightVisualsActive(false);

//            if (_playerRb != null)
//            {
//                _playerRb.isKinematic = false;
//            }

//            if (playerLocomotion != null)
//            {
//                playerLocomotion.enabled = true;

//                if (keepPaperStateOnExit)
//                {
//                    playerLocomotion.SetAbsolutePaperState(true);
//                }
//                else
//                {
//                    playerLocomotion.SetAbsolutePaperState(false);
//                }

//                // 비행 해제 시 부드러운 탈출 관성 부여
//                Vector3 exitVelocity = (_currentFlightDirection * (flightSpeed * 0.6f)) + (Vector3.up * 1.5f);
//                playerLocomotion.ExternalLaunch(exitVelocity);
//            }

//            _flightCoroutine = null;
//            IsOn = true;

//#if UNITY_EDITOR
//            Debug.Log($"<color=lime>[{gameObject.name}]</color> 비행 완료 (IsOn = true)");
//#endif
//        }

//        public override void Reset()
//        {
//            base.Reset();

//            if (_flightCoroutine != null)
//            {
//                StopCoroutine(_flightCoroutine);
//                _flightCoroutine = null;
//            }

//            if (_isFlying)
//            {
//                FinishFlight();
//            }

//            IsOn = false;
//        }

//        private void OnDisable()
//        {
//            if (_isFlying)
//            {
//                FinishFlight();
//            }
//        }

//        // =========================================================
//        // 코스 방향 전환 (미로 코너 / 웨이포인트 연동)
//        // =========================================================
//        private Coroutine _redirectCoroutine;

//        /// <summary>
//        /// 미로 코너나 기류 구간 진입 시 비행 기준 전방을 새 방향으로 부드럽게 꺾어줍니다.
//        /// </summary>
//        /// <param name="newDirection">새로 향할 전방 벡터</param>
//        /// <param name="turnDuration">방향 전환에 걸리는 시간 (초)</param>
//        public void RedirectCourse(Vector3 newDirection, float turnDuration = 0.6f)
//        {
//            if (!_isFlying) return;

//            Vector3 targetDir = Vector3.ProjectOnPlane(newDirection, Vector3.up).normalized;
//            if (targetDir.sqrMagnitude < 0.01f) return;

//            if (_redirectCoroutine != null)
//            {
//                StopCoroutine(_redirectCoroutine);
//            }
//            _redirectCoroutine = StartCoroutine(RedirectCourseRoutine(targetDir, Mathf.Max(0.1f, turnDuration)));
//        }

//        private IEnumerator RedirectCourseRoutine(Vector3 targetDir, float duration)
//        {
//            Vector3 startBase = _baseForward;
//            Vector3 startCurrent = _currentFlightDirection;
//            float elapsed = 0f;

//            // 편향각(targetClampedYaw) 서서히 리셋
//            float startYaw = _targetClampedYaw;

//            while (elapsed < duration)
//            {
//                elapsed += Time.fixedDeltaTime;
//                float t = Mathf.Clamp01(elapsed / duration);
//                // SmoothStep으로 부드러운 코너 선회감 연출
//                float smoothT = Mathf.SmoothStep(0f, 1f, t);

//                _baseForward = Vector3.Slerp(startBase, targetDir, smoothT);
//                _currentFlightDirection = Vector3.Slerp(startCurrent, targetDir, smoothT);
//                _targetClampedYaw = Mathf.Lerp(startYaw, 0f, smoothT);

//                yield return new WaitForFixedUpdate();
//            }

//            _baseForward = targetDir;
//            _currentFlightDirection = targetDir;
//            _targetClampedYaw = 0f;
//            _redirectCoroutine = null;

//#if UNITY_EDITOR
//            Debug.Log($"<color=lime>[PaperGlidingFlight]</color> 코너 통과 완료: 새 기준 방향 ({_baseForward})");
//#endif
//        }

//#if UNITY_EDITOR
//        private void OnDrawGizmosSelected()
//        {
//            Transform playerTr = (playerLocomotion != null) ? playerLocomotion.transform : transform;
//            Vector3 startPos = playerTr.position;
//            Vector3 fwd = Application.isPlaying ? _currentFlightDirection : CalculateReliableForwardDirection();

//            Gizmos.color = Color.cyan;
//            Gizmos.DrawRay(startPos, fwd * 10f);

//            Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
//            Gizmos.DrawWireSphere(startPos, collisionRadius);

//            if (steerMode == SteerMode.ClampedAngle)
//            {
//                Gizmos.color = Color.yellow;
//                Vector3 leftDir = Quaternion.Euler(0, -turnRate, 0) * fwd;
//                Vector3 rightDir = Quaternion.Euler(0, turnRate, 0) * fwd;
//                Gizmos.DrawRay(startPos, leftDir * 8f);
//                Gizmos.DrawRay(startPos, rightDir * 8f);
//            }
//        }
//#endif
//    }
//}

using System.Collections;
using System.Reflection;
using UnityEngine;
using _Project.Scripts.VisualScripting;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 종이(PaperState) 상태에서 글라이더처럼 전방으로 비행하는 Output 노드.
    /// - 지상/공중/점프 중 어느 상태에서 실행되어도 안정적인 글라이딩 보장
    /// - 점프 코루틴 잔여 실행 차단 및 Space 키 조기 캔슬 방지 유예 시간 적용
    /// - 무입력 시: 지정된 속도로 전방 직진 활공
    /// - 좌우 입력 시: 90도로 꺾이지 않고 설정한 뱅킹 각도만큼만 기울어지며 비행 방향 선회
    /// - 벽 충돌 시: 벽면을 타고 미끄러지며(Wall Slide) 전진 유지
    /// </summary>
    [AddComponentMenu("Visual Scripting/Outputs/Movement/Paper Gliding Flight (Output)")]
    public class PaperGlidingFlightOutput : ProcessBase
    {
        public enum SteerMode
        {
            [Tooltip("A/D 입력 중일 때 계속해서 해당 방향으로 회전합니다.")]
            ContinuousTurn,
            [Tooltip("기준 방향으로부터 지정된 최대 편향 각도 범위(±MaxTurnAngle) 내에서만 기울어집니다.")]
            ClampedAngle
        }

        [Header("--- [Target Settings] ---")]
        [Tooltip("비행을 적용할 플레이어 (비어있으면 자동 탐색)")]
        [SerializeField] private PlayerLocomotion playerLocomotion;
        [Tooltip("비행 시작 기준 방향 Transform (비어있으면 카메라/플레이어 시선 방향 사용)")]
        [SerializeField] private Transform initialDirectionTransform;

        [Header("--- [Paper State Control] ---")]
        [Tooltip("Output 실행 시 플레이어를 즉시 PaperState로 강제 변환합니다.")]
        [SerializeField] private bool activatePaperStateOnExecute = true;
        [Tooltip("PaperState 변환 시 슬림(Slim), 웅크리기(Brace), 슬라이딩 등 상충되는 상태를 즉시 해제합니다.")]
        [SerializeField] private bool resetConflictingStates = true;
        [Tooltip("비행 종료(착지 또는 캔슬) 후에도 PaperState 상태를 유지할지 여부 (false면 일반 상태로 복귀)")]
        [SerializeField] private bool keepPaperStateOnExit = true;

        [Header("--- [Flight Posture & Orientation] ---")]
        [Tooltip("체크 시 기체를 -90도 회전시켜 바닥과 수평으로 눕힙니다. (false면 눕히지 않고 똑바로 선 자세 유지)")]
        [SerializeField] private bool lieFlatOnFlight = true;

        [Header("--- [Flight Speed & Glide] ---")]
        [Tooltip("전방 비행 추진 속도 (m/s)")]
        [SerializeField] private float flightSpeed = 18.0f;
        [Tooltip("활공 시 초당 자연 하강 속도 (0이면 수평 고도 유지)")]
        [SerializeField] private float descentSpeed = 1.2f;

        [Header("--- [Steering & Bank Angle] ---")]
        [Tooltip("선회 조작 모드")]
        [SerializeField] private SteerMode steerMode = SteerMode.ContinuousTurn;
        [Tooltip("좌/우 입력 시 기체가 기울어지는 최대 롤(Roll) 각도 (단위: 도)")]
        [Range(5f, 60f)]
        [SerializeField] private float maxBankAngle = 30.0f;
        [Tooltip("좌/우 선회 회전 속도 (ContinuousTurn 모드 시 초당 회전각, Clamped 모드 시 편향 한계각)")]
        [Range(10f, 180f)]
        [SerializeField] private float turnRate = 65.0f;
        [Tooltip("기울기(Bank) 전환 반응 속도")]
        [SerializeField] private float bankSharpness = 6.0f;
        [Tooltip("키를 뗐을 때 수평으로 복귀하는 속도")]
        [SerializeField] private float bankReturnSharpness = 8.0f;

        [Header("--- [Wall Sliding Settings] ---")]
        [Tooltip("벽면 및 장애물 판정용 레이어")]
        [SerializeField] private LayerMask obstacleLayer = ~0;
        [Tooltip("벽 충돌 감지용 구체 반경 (플레이어 두께)")]
        [SerializeField] private float collisionRadius = 0.35f;
        [Tooltip("벽면에 비벼질 때 유지되는 전진 속도 비율 (0.8 = 80% 속도로 감속 미끄러짐)")]
        [Range(0.2f, 1.0f)]
        [SerializeField] private float wallSlideFrictionMultiplier = 0.85f;
        [Tooltip("벽면에 닿았을 때 진행 방향 벡터를 벽면 방향으로 부드럽게 유도(Deflection)할지 여부")]
        [SerializeField] private bool deflectDirectionAlongWall = true;

        [Header("--- [Paper Visual Flutter Settings] ---")]
        [Tooltip("종이 비행 중 미세한 공기 저항 흔들림 연출 사용")]
        [SerializeField] private bool useFlutterWobble = true;
        [Tooltip("흔들림 주기")]
        [SerializeField] private float flutterFrequency = 12.0f;
        [Tooltip("흔들림 진폭 (각도)")]
        [SerializeField] private float flutterAmplitude = 3.5f;

        [Header("--- [Flight Termination Settings] ---")]
        [Tooltip("지면에 닿았을 때 비행 자동 종료")]
        [SerializeField] private bool exitOnGround = true;
        [Tooltip("비행 시작 후 지면 감지를 무시할 유예 시간 (점프 직후 바닥 오감지 방지)")]
        [SerializeField] private float groundCheckGraceDuration = 0.45f;
        [Tooltip("지면 감지 거리")]
        [SerializeField] private float groundCheckDistance = 0.4f;
        [Tooltip("지면 감지 레이어")]
        [SerializeField] private LayerMask groundLayer = ~0;
        [Tooltip("점프 키(Space)를 눌러 비행 수동 캔슬 가능 여부")]
        [SerializeField] private bool allowManualDismount = true;
        [Tooltip("비행 시작 후 점프 캔슬 입력을 무시할 안전 시간 (점프하면서 비행 진입 시 즉시 캔슬 방지)")]
        [SerializeField] private float dismountGraceDuration = 0.4f;
        [Tooltip("비행 시간 제한 (0이면 착지 또는 캔슬 시까지 무제한)")]
        [SerializeField] private float flightDuration = 0f;
        [Tooltip("외부 종료 트리거 (해당 ProcessBase가 IsOn이 되면 즉시 착지)")]
        [SerializeField] private ProcessBase exitTrigger;

        [Header("--- [Audio (Optional)] ---")]
        [SerializeField] private AudioClip flightWindLoopClip;
        [SerializeField] private AudioSource audioSource;

        private Coroutine _flightCoroutine;
        private Rigidbody _playerRb;
        private PlayerVisuals _playerVisuals;
        private Animator _playerAnim;
        private GameObject _flutterVisualObj;
        private Transform _characterVisualTr;

        private Vector3 _baseForward;
        private Vector3 _currentFlightDirection;
        private float _currentBankAngle = 0f;
        private float _targetClampedYaw = 0f;
        private bool _isFlying = false;

        // PlayerLocomotion 내부 private 필드 캐싱 (점프 및 지면 상태 완벽 리셋용)
        private static readonly FieldInfo FieldIsGrounded = typeof(PlayerLocomotion).GetField("isGrounded", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldGroundTimer = typeof(PlayerLocomotion).GetField("groundCheckTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldCoyoteTimer = typeof(PlayerLocomotion).GetField("coyoteTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldJumpPending = typeof(PlayerLocomotion).GetField("isJumpingPending", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo FieldTargetRot = typeof(PlayerLocomotion).GetField("targetBaseRotation", BindingFlags.Instance | BindingFlags.NonPublic);

        private void Awake()
        {
            FindPlayerIfNull();
        }

        private void FindPlayerIfNull()
        {
            if (playerLocomotion == null)
            {
                playerLocomotion = FindFirstObjectByType<PlayerLocomotion>();
            }

            if (playerLocomotion != null)
            {
                _playerRb = playerLocomotion.GetComponent<Rigidbody>();
                _playerVisuals = playerLocomotion.GetComponentInChildren<PlayerVisuals>();
                _playerAnim = playerLocomotion.GetComponentInChildren<Animator>();

                if (_playerVisuals != null)
                {
                    _characterVisualTr = _playerVisuals.CharacterVisual;
                    FieldInfo flutterField = typeof(PlayerVisuals).GetField("FlutterVisualPrefab", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (flutterField != null)
                    {
                        _flutterVisualObj = flutterField.GetValue(_playerVisuals) as GameObject;
                    }
                }
            }
        }

        public override void Execute()
        {
            FindPlayerIfNull();

            if (playerLocomotion == null || _playerRb == null)
            {
#if UNITY_EDITOR
                Debug.LogError($"<color=red>[{gameObject.name}]</color> PlayerLocomotion 또는 Rigidbody를 찾을 수 없습니다.");
#endif
                IsOn = true;
                return;
            }

            IsOn = false;

            // 1. 점프 상태 및 로코모션 잔여 로직 완벽 초기화
            PreparePlayerForFlight();

            // 2. 비행 루틴 시작
            if (_flightCoroutine != null)
            {
                StopCoroutine(_flightCoroutine);
            }

            _flightCoroutine = StartCoroutine(FlightRoutine());
        }

        /// <summary>
        /// 점프 중이든 지상 중이든 상관없이 모든 잔여 모션/코루틴을 청소하고 비행 상태로 설정합니다.
        /// </summary>
        private void PreparePlayerForFlight()
        {
            if (playerLocomotion == null) return;

            // 진행 중이던 점프 코루틴(JumpRoutine 등) 완전 차단
            playerLocomotion.StopAllCoroutines();

            if (resetConflictingStates)
            {
                playerLocomotion.CancelSpecialStatesForCondition();
            }

            if (activatePaperStateOnExecute)
            {
                playerLocomotion.SetAbsolutePaperState(true);
            }

            // 점프 보류 플래그 및 지면 상태 강제 초기화
            if (FieldJumpPending != null) FieldJumpPending.SetValue(playerLocomotion, false);
            if (FieldIsGrounded != null) FieldIsGrounded.SetValue(playerLocomotion, false);
            if (FieldGroundTimer != null) FieldGroundTimer.SetValue(playerLocomotion, 0f);
            if (FieldCoyoteTimer != null) FieldCoyoteTimer.SetValue(playerLocomotion, 0f);

            // 로코모션 속도 초기화
            playerLocomotion.ResetAllVelocities();

            // 애니메이터 파라미터 초기화
            if (_playerAnim != null)
            {
                _playerAnim.SetBool("IsWalk", false);
                _playerAnim.SetBool("IsCrouch", false);
                _playerAnim.SetBool("IsHangOn", false);
                _playerAnim.ResetTrigger("IsJump");
            }

            if (_playerVisuals != null)
            {
                _playerVisuals.SetWalkAnim(false);
                _playerVisuals.SetCrouchAnim(false);
                _playerVisuals.UpdateStates(paper: true, slim: false, completelySlim: false, windRolling: false);
                _playerVisuals.SetNormalFace();
            }
        }

        private void SetFlightVisualsActive(bool isFlying)
        {
            if (isFlying)
            {
                if (_flutterVisualObj != null)
                {
                    _flutterVisualObj.SetActive(true);
                    if (_characterVisualTr != null) _characterVisualTr.gameObject.SetActive(false);
                }
                else if (_characterVisualTr != null)
                {
                    _characterVisualTr.gameObject.SetActive(true);
                    _characterVisualTr.localPosition = Vector3.zero;
                    _characterVisualTr.localRotation = Quaternion.identity;
                }
            }
            else
            {
                if (_flutterVisualObj != null)
                {
                    _flutterVisualObj.SetActive(false);
                }
                if (_characterVisualTr != null)
                {
                    _characterVisualTr.gameObject.SetActive(true);
                }
            }
        }

        /// <summary>
        /// 종이 상태의 회전 왜곡에 영향을 받지 않는 올바른 수평 전방 벡터를 추출합니다.
        /// </summary>
        private Vector3 CalculateReliableForwardDirection()
        {
            if (initialDirectionTransform != null)
            {
                Vector3 fwd = Vector3.ProjectOnPlane(initialDirectionTransform.forward, Vector3.up).normalized;
                if (fwd.sqrMagnitude > 0.01f) return fwd;
            }

            // 1. 카메라 정면 방향 우선 (가장 직관적인 조준 방향)
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 camFwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                if (camFwd.sqrMagnitude > 0.01f) return camFwd;
            }

            // 2. 플레이어의 targetBaseRotation 추출 (로코모션이 바라보던 기준 방향)
            if (playerLocomotion != null && FieldTargetRot != null)
            {
                Quaternion targetRot = (Quaternion)FieldTargetRot.GetValue(playerLocomotion);
                Vector3 baseFwd = Vector3.ProjectOnPlane(targetRot * Vector3.forward, Vector3.up).normalized;
                if (baseFwd.sqrMagnitude > 0.01f) return baseFwd;
            }

            return Vector3.forward;
        }

        private IEnumerator FlightRoutine()
        {
            _isFlying = true;

            // 1. 비주얼 교체 및 로코모션 비활성화
            SetFlightVisualsActive(true);
            playerLocomotion.enabled = false;

            if (_playerRb != null)
            {
                _playerRb.isKinematic = true;
                _playerRb.linearVelocity = Vector3.zero;
                _playerRb.angularVelocity = Vector3.zero;
            }

            // 2. 비행 기준 전방 벡터 확정
            _baseForward = CalculateReliableForwardDirection();
            _currentFlightDirection = _baseForward;
            _currentBankAngle = 0f;
            _targetClampedYaw = 0f;

            if (audioSource != null && flightWindLoopClip != null)
            {
                audioSource.clip = flightWindLoopClip;
                audioSource.loop = true;
                audioSource.Play();
            }

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[PaperGlidingFlight]</color> 글라이딩 비행 개시! (기준 방향: {_baseForward})");
#endif

            float timer = 0f;
            yield return new WaitForFixedUpdate();

            while (_isFlying)
            {
                timer += Time.fixedDeltaTime;

                // 시간 초과 체크
                if (flightDuration > 0f && timer >= flightDuration)
                {
#if UNITY_EDITOR
                    Debug.Log("<color=yellow>[PaperGlidingFlight]</color> 비행 제한 시간 도달로 착지합니다.");
#endif
                    break;
                }

                // 외부 트리거 체크
                if (exitTrigger != null && exitTrigger.IsOn)
                {
#if UNITY_EDITOR
                    Debug.Log("<color=yellow>[PaperGlidingFlight]</color> 외부 트리거에 의해 비행을 종료합니다.");
#endif
                    break;
                }

                // 점프 키(Space) 수동 캔슬 (★ dismountGraceDuration 이후에만, 그리고 누른 '첫 프레임'만 체크)
                if (allowManualDismount && timer >= dismountGraceDuration)
                {
                    bool jumpTriggered = (InputManager.Instance != null && InputManager.Instance.JumpTriggered) || Input.GetKeyDown(KeyCode.Space);
                    if (jumpTriggered)
                    {
#if UNITY_EDITOR
                        Debug.Log("<color=yellow>[PaperGlidingFlight]</color> 점프 키(Space) 입력으로 비행을 수동 캔슬합니다.");
#endif
                        break;
                    }
                }

                // 좌우 이동 입력 수신 (A / D)
                float inputX = 0f;
                if (InputManager.Instance != null)
                {
                    inputX = InputManager.Instance.Movement.x;
                }
                if (Mathf.Abs(inputX) < 0.01f)
                {
                    inputX = Input.GetAxisRaw("Horizontal");
                }

                // 뱅킹(Roll) 계산 (무입력 시 수평 복귀)
                float targetBank = -inputX * maxBankAngle;
                float bankSharp = (Mathf.Abs(inputX) > 0.01f) ? bankSharpness : bankReturnSharpness;
                _currentBankAngle = Mathf.Lerp(_currentBankAngle, targetBank, Time.fixedDeltaTime * bankSharp);

                // 진행 방향 선회(Yaw) 계산
                if (steerMode == SteerMode.ContinuousTurn)
                {
                    float yawDelta = inputX * turnRate * Time.fixedDeltaTime;
                    _currentFlightDirection = Quaternion.Euler(0f, yawDelta, 0f) * _currentFlightDirection;
                }
                else
                {
                    _targetClampedYaw = Mathf.Lerp(_targetClampedYaw, inputX * turnRate, Time.fixedDeltaTime * bankSharpness);
                    _currentFlightDirection = Quaternion.Euler(0f, _targetClampedYaw, 0f) * _baseForward;
                }

                // 종이 미세 흔들림(Flutter)
                float flutterRoll = 0f;
                if (useFlutterWobble)
                {
                    flutterRoll = Mathf.Sin(Time.time * flutterFrequency) * flutterAmplitude;
                }

                // 회전값 적용
                Quaternion yawRotation = Quaternion.LookRotation(_currentFlightDirection, Vector3.up);
                Quaternion bankRotation = Quaternion.Euler(0f, 0f, _currentBankAngle + flutterRoll);

                // lieFlatOnFlight가 true면 -90도 회전하여 눕히고, false면 똑바로 선 자세 유지
                Quaternion paperPosture = lieFlatOnFlight ? Quaternion.Euler(-90f, 0f, 0f) : Quaternion.identity;

                Quaternion finalRotation = yawRotation * bankRotation * paperPosture;
                if (_playerRb != null)
                {
                    _playerRb.MoveRotation(finalRotation);

                    // =========================================================
                    // 벽면 충돌 검출 및 미끄러짐(Wall Slide) 연산
                    // =========================================================
                    Vector3 moveVelocity = (_currentFlightDirection * flightSpeed) + (Vector3.down * descentSpeed);
                    Vector3 moveDelta = moveVelocity * Time.fixedDeltaTime;
                    float checkDistance = moveDelta.magnitude;

                    Transform playerTr = playerLocomotion.transform;
                    if (Physics.SphereCast(_playerRb.position, collisionRadius, moveDelta.normalized, out RaycastHit wallHit, checkDistance + 0.05f, obstacleLayer, QueryTriggerInteraction.Ignore))
                    {
                        if (!wallHit.collider.transform.IsChildOf(playerTr))
                        {
                            Vector3 wallNormal = wallHit.normal;
                            float normalDot = Vector3.Dot(_currentFlightDirection, wallNormal);

                            if (normalDot < 0f)
                            {
                                Vector3 flatNormal = new Vector3(wallNormal.x, 0f, wallNormal.z).normalized;
                                if (flatNormal.sqrMagnitude < 0.01f) flatNormal = wallNormal;

                                Vector3 slidingVelocity = Vector3.ProjectOnPlane(moveVelocity, flatNormal) * wallSlideFrictionMultiplier;
                                Vector3 remainingMove = slidingVelocity * Time.fixedDeltaTime;

                                if (deflectDirectionAlongWall)
                                {
                                    Vector3 projectedFwd = Vector3.ProjectOnPlane(_currentFlightDirection, flatNormal).normalized;
                                    if (projectedFwd.sqrMagnitude > 0.01f)
                                    {
                                        _currentFlightDirection = Vector3.Slerp(_currentFlightDirection, projectedFwd, Time.fixedDeltaTime * 10f);
                                    }
                                }

                                Vector3 safeContactPoint = wallHit.point + (wallNormal * (collisionRadius + 0.02f));

                                if (remainingMove.sqrMagnitude > 0.0001f && Physics.SphereCast(safeContactPoint, collisionRadius, remainingMove.normalized, out RaycastHit cornerHit, remainingMove.magnitude, obstacleLayer, QueryTriggerInteraction.Ignore))
                                {
                                    _playerRb.MovePosition(cornerHit.point + (cornerHit.normal * (collisionRadius + 0.02f)));
                                }
                                else
                                {
                                    _playerRb.MovePosition(safeContactPoint + remainingMove);
                                }
                            }
                            else
                            {
                                _playerRb.MovePosition(_playerRb.position + moveDelta);
                            }
                        }
                        else
                        {
                            _playerRb.MovePosition(_playerRb.position + moveDelta);
                        }
                    }
                    else
                    {
                        _playerRb.MovePosition(_playerRb.position + moveDelta);
                    }
                }

                // 바닥 착지 감지 (★ 점프 직후 바닥 오감지 방지를 위해 groundCheckGraceDuration 이후부터 검사)
                if (exitOnGround && timer >= groundCheckGraceDuration && _playerRb != null)
                {
                    Vector3 rayOrigin = _playerRb.position + Vector3.up * 0.2f;
                    if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit groundHit, groundCheckDistance + 0.2f, groundLayer, QueryTriggerInteraction.Ignore))
                    {
                        if (!groundHit.collider.transform.IsChildOf(playerLocomotion.transform))
                        {
#if UNITY_EDITOR
                            Debug.Log("<color=green>[PaperGlidingFlight]</color> 지면에 착지했습니다.");
#endif
                            break;
                        }
                    }
                }

                yield return new WaitForFixedUpdate();
            }

            FinishFlight();
        }

        private void FinishFlight()
        {
            if (!_isFlying) return;
            _isFlying = false;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            SetFlightVisualsActive(false);

            if (_playerRb != null)
            {
                _playerRb.isKinematic = false;
            }

            if (playerLocomotion != null)
            {
                playerLocomotion.enabled = true;

                if (keepPaperStateOnExit)
                {
                    playerLocomotion.SetAbsolutePaperState(true);
                }
                else
                {
                    playerLocomotion.SetAbsolutePaperState(false);
                }

                // 비행 해제 시 부드러운 탈출 관성 부여
                Vector3 exitVelocity = (_currentFlightDirection * (flightSpeed * 0.6f)) + (Vector3.up * 1.5f);
                playerLocomotion.ExternalLaunch(exitVelocity);
            }

            _flightCoroutine = null;
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=lime>[{gameObject.name}]</color> 비행 완료 (IsOn = true)");
#endif
        }

        public override void Reset()
        {
            base.Reset();

            if (_flightCoroutine != null)
            {
                StopCoroutine(_flightCoroutine);
                _flightCoroutine = null;
            }

            if (_isFlying)
            {
                FinishFlight();
            }

            IsOn = false;
        }

        private void OnDisable()
        {
            if (_isFlying)
            {
                FinishFlight();
            }
        }

        // =========================================================
        // 코스 방향 전환 (미로 코너 / 웨이포인트 연동)
        // =========================================================
        private Coroutine _redirectCoroutine;

        /// <summary>
        /// 미로 코너나 기류 구간 진입 시 비행 기준 전방을 새 방향으로 부드럽게 꺾어줍니다.
        /// </summary>
        /// <param name="newDirection">새로 향할 전방 벡터</param>
        /// <param name="turnDuration">방향 전환에 걸리는 시간 (초)</param>
        public void RedirectCourse(Vector3 newDirection, float turnDuration = 0.6f)
        {
            if (!_isFlying) return;

            Vector3 targetDir = Vector3.ProjectOnPlane(newDirection, Vector3.up).normalized;
            if (targetDir.sqrMagnitude < 0.01f) return;

            if (_redirectCoroutine != null)
            {
                StopCoroutine(_redirectCoroutine);
            }
            _redirectCoroutine = StartCoroutine(RedirectCourseRoutine(targetDir, Mathf.Max(0.1f, turnDuration)));
        }

        private IEnumerator RedirectCourseRoutine(Vector3 targetDir, float duration)
        {
            Vector3 startBase = _baseForward;
            Vector3 startCurrent = _currentFlightDirection;
            float elapsed = 0f;

            // 편향각(targetClampedYaw) 서서히 리셋
            float startYaw = _targetClampedYaw;

            while (elapsed < duration)
            {
                elapsed += Time.fixedDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // SmoothStep으로 부드러운 코너 선회감 연출
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                _baseForward = Vector3.Slerp(startBase, targetDir, smoothT);
                _currentFlightDirection = Vector3.Slerp(startCurrent, targetDir, smoothT);
                _targetClampedYaw = Mathf.Lerp(startYaw, 0f, smoothT);

                yield return new WaitForFixedUpdate();
            }

            _baseForward = targetDir;
            _currentFlightDirection = targetDir;
            _targetClampedYaw = 0f;
            _redirectCoroutine = null;

#if UNITY_EDITOR
            Debug.Log($"<color=lime>[PaperGlidingFlight]</color> 코너 통과 완료: 새 기준 방향 ({_baseForward})");
#endif
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Transform playerTr = (playerLocomotion != null) ? playerLocomotion.transform : transform;
            Vector3 startPos = playerTr.position;
            Vector3 fwd = Application.isPlaying ? _currentFlightDirection : CalculateReliableForwardDirection();

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(startPos, fwd * 10f);

            Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
            Gizmos.DrawWireSphere(startPos, collisionRadius);

            if (steerMode == SteerMode.ClampedAngle)
            {
                Gizmos.color = Color.yellow;
                Vector3 leftDir = Quaternion.Euler(0, -turnRate, 0) * fwd;
                Vector3 rightDir = Quaternion.Euler(0, turnRate, 0) * fwd;
                Gizmos.DrawRay(startPos, leftDir * 8f);
                Gizmos.DrawRay(startPos, rightDir * 8f);
            }
        }
#endif
    }
}