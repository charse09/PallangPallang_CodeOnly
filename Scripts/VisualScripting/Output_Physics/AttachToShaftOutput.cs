using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 플레이어가 영역에 진입하면 샤프트에 밀착 고정되어 말림 회전을 하고,
    /// 스페이스바 차징(0.0~1.0) 또는 외부 DismountTrigger 신호에 따라 
    /// Target 1 ~ Target 2 사이의 실시간 포물선 궤적을 그리며 탈출하는 샤프트 컴포넌트입니다.
    /// </summary>
    public class AttachToShaftOutput : ProcessBase
    {
        private enum ChargePhase
        {
            Idle,
            ChargingUp,     // 0% -> 100% (Target 1 -> Target 2 증가)
            HoldingMax,     // 100% 최대치에서 2초 머무름
            ReturningDown,  // 100% -> 0% (Target 2 -> Target 1 복귀)
            HoldingMin      // 0% 최소치에서 2초 머무름 -> 이후 재충전
        }

        [Header("Shaft & Attachment Settings")]
        [Tooltip("회전하는 샤프트(원통) Transform")]
        [SerializeField] private Transform shaftTransform;

        [Tooltip("플레이어가 밀착하여 말릴 고정 위치 (샤프트의 자식으로 배치할 Transform)")]
        [SerializeField] private Transform attachPoint;

        [Tooltip("샤프트의 길이/슬라이딩 축 (보통 Local Right 또는 Forward)")]
        [SerializeField] private Vector3 shaftLocalAxis = Vector3.right;

        [Header("Positioning & Snap Settings")]
        [Tooltip("진입 시 attachPoint 위치로 즉시 보정(순간 이동)할지 여부")]
        [SerializeField] private bool snapToAttachPoint = true;

        [Tooltip("snapToAttachPoint가 false일 때 attachPoint 위치로 부드럽게 이동하는 시간(초)")]
        [SerializeField] private float snapDuration = 0.15f;

        [Header("Slide Along Shaft Settings")]
        [Tooltip("샤프트에 매달린 상태에서 축을 따른 좌우 이동 허용 여부")]
        [SerializeField] private bool allowAxisSliding = true;

        [Tooltip("축 이동 조작 반전 여부 (체크 시 A/D 등 조작이 반대로 적용됨)")]
        [SerializeField] private bool invertControls = false;

        [Tooltip("축을 따른 슬라이딩 이동 속도")]
        [SerializeField] private float slideSpeed = 6.0f;

        [Tooltip("AttachPoint 기준 왼쪽(-)으로 이동 가능한 최대 거리")]
        [SerializeField] private float leftSlideDistance = 3.0f;

        [Tooltip("AttachPoint 기준 오른쪽(+)으로 이동 가능한 최대 거리")]
        [SerializeField] private float rightSlideDistance = 3.0f;

        [Header("Animation & Rotation Settings")]
        [Tooltip("말림 회전 애니메이션 속도 배율 (기본값 1.0)")]
        [SerializeField] private float rollAnimSpeed = 1.5f;

        [Tooltip("Animator에서 말림 애니메이션 제어에 사용할 Bool 파라미터 이름")]
        [SerializeField] private string rollAnimParamName = "IsWindRolling";

        [Header("Key & Charge Settings")]
        [Tooltip("점프/차징에 사용할 키 (기본값 Space)")]
        [SerializeField] private KeyCode dismountKey = KeyCode.Space;

        [Tooltip("Target 1에서 Target 2(최대치)까지 도달하는 차징 시간(초)")]
        [SerializeField] private float chargeTimeThreshold = 1.0f;

        [Tooltip("최대치(Target 2)에 도달했을 때 최고점에서 머무르는 시간(초)")]
        [SerializeField] private float maxHoldDuration = 2.0f;

        [Tooltip("최대치에서 다시 Target 1(0%)으로 빠르게 되돌아오는 시간(초)")]
        [SerializeField] private float returnDuration = 0.75f;

        [Tooltip("최소치(Target 1)로 되돌아왔을 때 최저점에서 머무르는 시간(초)")]
        [SerializeField] private float minHoldDuration = 2.0f;

        [Tooltip("최저점에 도달한 뒤에도 키를 계속 누르고 있을 때 충전을 반복(Loop)할지 여부")]
        [SerializeField] private bool loopCharge = true;

        [Header("Audio Settings (SFX)")]
        [Tooltip("Roll(말림) 상태일 때 루프로 재생할 SO_SFXTable 키값")]
        [SerializeField] private string rollLoopSfxKey = "";

        [Tooltip("DismountKey를 누른 순간(차징 시작) 재생할 SO_SFXTable 키값")]
        [SerializeField] private string dismountKeyDownSfxKey = "";

        [Tooltip("차징 종료 후 점프 발사 시 재생할 SO_SFXTable 키값 (선택 사항)")]
        [SerializeField] private string dismountLaunchSfxKey = "";

        [Header("Dismount Trigger Settings")]
        [Tooltip("지정한 외부 ProcessBase(트리거/Output 노드)의 IsOn이 true가 되면 자동으로 점프/탈출합니다.")]
        [SerializeField] private ProcessBase dismountTrigger;

        [Tooltip("트리거 신호로 탈출할 때 적용할 차징 비율 (0.0 = Target 1, 1.0 = Target 2 / 차징 중 트리거 도달 시 현재 차징값 우선)")]
        [Range(0f, 1f)]
        [SerializeField] private float triggerDismountRatio = 0f;

        [Header("Target 1 Settings (Tap / Min - 최소 위치)")]
        [Tooltip("차징 0%일 때 날아갈 1차 기본 목표 위치")]
        [SerializeField] private Transform target1Transform;

        [Tooltip("1차 목표 점프 최고 높이")]
        [SerializeField] private float target1JumpHeight = 3.0f;

        [Tooltip("1차 목표 위치까지 날아가는 데 소요되는 시간(초)")]
        [SerializeField] private float target1JumpDuration = 0.8f;

        [Header("Target 2 Settings (Max - 최대 위치)")]
        [Tooltip("차징 100%일 때 날아갈 2차 최대 목표 위치")]
        [SerializeField] private Transform target2Transform;

        [Tooltip("2차 목표 점프 최고 높이")]
        [SerializeField] private float target2JumpHeight = 5.0f;

        [Tooltip("2차 목표 위치까지 날아가는 데 소요되는 시간(초)")]
        [SerializeField] private float target2JumpDuration = 1.0f;

        [Header("Trajectory Parabola Line Settings")]
        [Tooltip("실시간 계산된 포물선 궤적을 그려줄 LineRenderer 컴포넌트")]
        [SerializeField] private LineRenderer trajectoryLine;

        [Tooltip("포물선 궤적 가이드라인 활성화 여부")]
        [SerializeField] private bool showTrajectory = true;

        [Tooltip("포물선 궤적 곡선의 해상도 (점 개수)")]
        [SerializeField] private int trajectoryResolution = 25;

        [Header("Jump Particle Settings (Optional)")]
        [Tooltip("점프 발사 시 스폰할 파티클 프리팹 (예: DustExplosion 1)")]
        [SerializeField] private GameObject jumpParticlePrefab;

        [Header("Charge Visual Effect Settings (Optional)")]
        [Tooltip("차징 중 활성화될 이펙트 게임오브젝트")]
        [SerializeField] private GameObject chargeEffectObject;

        [Tooltip("차징 중 재생할 파티클 시스템")]
        [SerializeField] private ParticleSystem chargeParticleSystem;

        [Tooltip("플레이어 오브젝트 (비워둘 경우 자동 검색)")]
        [SerializeField] private PlayerLocomotion locomotion;

        private Coroutine _rollCoroutine;
        private PlayerLocomotion _lastTriggeredPlayer;

        private bool _isAttached = false;
        private bool _isCharging = false;
        private bool _wasJumpRequested = false;
        private float _currentSlideOffset = 0f;

        // 차징 사이클 제어 변수
        private ChargePhase _chargePhase = ChargePhase.Idle;
        private float _phaseTimer = 0f;
        private float _currentChargeRatio = 0f;
        private float _finalJumpRatio = 0f;

        // Roll 지속 사운드 전용 AudioSource
        private AudioSource _rollLoopAudioSource;

        private void Awake()
        {
            InitRollAudioSource();
        }

        private void InitRollAudioSource()
        {
            if (_rollLoopAudioSource == null)
            {
                _rollLoopAudioSource = gameObject.AddComponent<AudioSource>();
                _rollLoopAudioSource.playOnAwake = false;
                _rollLoopAudioSource.loop = true;
                _rollLoopAudioSource.rolloffMode = AudioRolloffMode.Linear;
                _rollLoopAudioSource.minDistance = 5.0f;
                _rollLoopAudioSource.maxDistance = 50.0f;
                _rollLoopAudioSource.dopplerLevel = 0f;

                if (AudioManager.Instance != null)
                {
                    _rollLoopAudioSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
                }
            }
        }

        private void Update()
        {
            if (!_isAttached) return;

            // 1. 차징 시작 (DismountKey를 누른 시점)
            if (Input.GetKeyDown(dismountKey))
            {
                _isCharging = true;
                _chargePhase = ChargePhase.ChargingUp;
                _phaseTimer = 0f;
                _currentChargeRatio = 0f;
                SetChargeEffectState(true);

                // DismountKey 입력 사운드 출력
                if (!string.IsNullOrEmpty(dismountKeyDownSfxKey) && SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX(dismountKeyDownSfxKey, transform.position);
                }
            }

            // 2. 차징 사이클 (증가 -> 2초 최고점 유지 -> 감소 -> 2초 최저점 유지 -> 루프)
            if (_isCharging && Input.GetKey(dismountKey))
            {
                UpdateChargeCycle();
            }

            // 3. 키를 뗐을 때 해당 시점의 정확한 비율로 발사
            if (_isCharging && Input.GetKeyUp(dismountKey))
            {
                _isCharging = false;
                _finalJumpRatio = _currentChargeRatio;
                SetChargeEffectState(false);
                _chargePhase = ChargePhase.Idle;

                _wasJumpRequested = true;
            }
        }

        private void UpdateChargeCycle()
        {
            _phaseTimer += Time.deltaTime;

            switch (_chargePhase)
            {
                case ChargePhase.ChargingUp:
                    float upDuration = Mathf.Max(0.01f, chargeTimeThreshold);
                    _currentChargeRatio = Mathf.Clamp01(_phaseTimer / upDuration);

                    if (_phaseTimer >= upDuration)
                    {
                        _currentChargeRatio = 1.0f;
                        _phaseTimer = 0f;
                        _chargePhase = ChargePhase.HoldingMax;
                    }
                    break;

                case ChargePhase.HoldingMax:
                    _currentChargeRatio = 1.0f;

                    if (_phaseTimer >= maxHoldDuration)
                    {
                        _phaseTimer = 0f;
                        _chargePhase = ChargePhase.ReturningDown;
                    }
                    break;

                case ChargePhase.ReturningDown:
                    float downDuration = Mathf.Max(0.01f, returnDuration);
                    _currentChargeRatio = Mathf.Clamp01(1.0f - (_phaseTimer / downDuration));

                    if (_phaseTimer >= downDuration)
                    {
                        _currentChargeRatio = 0f;
                        _phaseTimer = 0f;
                        _chargePhase = ChargePhase.HoldingMin;
                    }
                    break;

                case ChargePhase.HoldingMin:
                    _currentChargeRatio = 0f;

                    if (_phaseTimer >= minHoldDuration)
                    {
                        _phaseTimer = 0f;
                        if (loopCharge)
                        {
                            _chargePhase = ChargePhase.ChargingUp;
                        }
                    }
                    break;
            }
        }

        private void SetChargeEffectState(bool isActive)
        {
            if (chargeEffectObject != null)
            {
                chargeEffectObject.SetActive(isActive);
            }

            if (chargeParticleSystem != null)
            {
                if (isActive) chargeParticleSystem.Play();
                else chargeParticleSystem.Stop();
            }
        }

        public override void Execute()
        {
            IsOn = false;

            PlayerLocomotion player = _lastTriggeredPlayer != null ? _lastTriggeredPlayer : (locomotion != null ? locomotion : FindFirstObjectByType<PlayerLocomotion>());

            if (player == null || player.isRidingRail)
            {
                IsOn = true;
                return;
            }

            StartRoll(player);
        }

        private void OnTriggerEnter(Collider other)
        {
            CheckAndStartRoll(other);
        }

        private void OnTriggerStay(Collider other)
        {
            CheckAndStartRoll(other);
        }

        private void CheckAndStartRoll(Collider other)
        {
            if (_isAttached || _rollCoroutine != null) return;

            PlayerLocomotion player = other.GetComponentInParent<PlayerLocomotion>();
            if (player != null && !player.isRidingRail)
            {
                _lastTriggeredPlayer = player;
                StartRoll(player);
            }
        }

        private void StartRoll(PlayerLocomotion player)
        {
            if (_rollCoroutine != null) StopCoroutine(_rollCoroutine);
            _rollCoroutine = StartCoroutine(RollRoutine(player));
        }

        private IEnumerator RollRoutine(PlayerLocomotion player)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            PlayerVisuals visuals = player.GetComponent<PlayerVisuals>();
            if (visuals == null) visuals = player.GetComponentInChildren<PlayerVisuals>();
            Animator animator = player.GetComponentInChildren<Animator>();

            if (shaftTransform != null)
            {
                Collider shaftCollider = shaftTransform.GetComponent<Collider>();
                Collider playerCollider = player.GetComponent<Collider>();
                if (shaftCollider != null && playerCollider != null)
                {
                    Physics.IgnoreCollision(shaftCollider, playerCollider, true);
                }
            }

            player.SetRailMode(true, playAnim: false);

            Vector3 targetAttachPos = attachPoint != null ? attachPoint.position : (shaftTransform != null ? shaftTransform.position : player.transform.position);
            Quaternion targetAttachRot = attachPoint != null ? attachPoint.rotation : (shaftTransform != null ? shaftTransform.rotation : player.transform.rotation);

            if (snapToAttachPoint || snapDuration <= 0f)
            {
                player.transform.position = targetAttachPos;
                player.transform.rotation = targetAttachRot;
            }
            else
            {
                Vector3 startPos = player.transform.position;
                Quaternion startRot = player.transform.rotation;
                float elapsed = 0f;

                while (elapsed < snapDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / snapDuration);

                    player.transform.position = Vector3.Lerp(startPos, targetAttachPos, t);
                    player.transform.rotation = Quaternion.Slerp(startRot, targetAttachRot, t);
                    yield return null;
                }
            }

            if (visuals != null) visuals.SetWindRollAnim(true);
            if (animator != null && !string.IsNullOrEmpty(rollAnimParamName))
            {
                animator.SetBool(rollAnimParamName, true);
                animator.applyRootMotion = false;
            }

            _isAttached = true;
            _isCharging = false;
            _wasJumpRequested = false;
            _currentSlideOffset = 0f;
            _currentChargeRatio = 0f;
            _finalJumpRatio = 0f;

            // ★ Roll 상태 진입 시 SFX 루프 시작
            PlayRollLoopAudio();

            Vector3 worldAxis = shaftTransform != null
                ? shaftTransform.TransformDirection(shaftLocalAxis).normalized
                : Vector3.right;

            // 진입 시 외부 트리거의 잔여 IsOn 값으로 인해 즉각 발사되는 현상을 방지
            yield return new WaitForFixedUpdate();

            while (true)
            {
                // 키 릴리즈로 인한 탈출 요청 확인
                if (_wasJumpRequested)
                {
                    _wasJumpRequested = false;
                    break;
                }

                // 외부 ProcessBase 트리거 신호 확인
                if (dismountTrigger != null && dismountTrigger.IsOn)
                {
                    _finalJumpRatio = _isCharging ? _currentChargeRatio : triggerDismountRatio;
                    _isCharging = false;
                    _chargePhase = ChargePhase.Idle;
                    break;
                }

                yield return new WaitForFixedUpdate();

                if (allowAxisSliding)
                {
                    float inputX = Input.GetAxisRaw("Horizontal");
                    if (invertControls) inputX = -inputX;

                    _currentSlideOffset += inputX * slideSpeed * Time.fixedDeltaTime;
                    _currentSlideOffset = Mathf.Clamp(_currentSlideOffset, -leftSlideDistance, rightSlideDistance);
                }

                Vector3 basePos = attachPoint != null ? attachPoint.position : targetAttachPos;
                Vector3 currentFixedPos = basePos + (worldAxis * _currentSlideOffset);
                Quaternion currentFixedRot = attachPoint != null ? attachPoint.rotation : targetAttachRot;

                player.transform.position = currentFixedPos;
                player.transform.rotation = currentFixedRot;

                if (rb != null)
                {
                    rb.position = currentFixedPos;
                    rb.rotation = currentFixedRot;
                }

                UpdateTrajectoryPreview(player.transform.position);
            }

            _isAttached = false;
            SetChargeEffectState(false);
            DisableTrajectoryPreview();

            // ★ Roll 상태 해제 시 SFX 루프 정지
            StopRollLoopAudio();

            if (shaftTransform != null)
            {
                Collider shaftCollider = shaftTransform.GetComponent<Collider>();
                Collider playerCollider = player.GetComponent<Collider>();
                if (shaftCollider != null && playerCollider != null)
                {
                    Physics.IgnoreCollision(shaftCollider, playerCollider, false);
                }
            }

            yield return StartCoroutine(DismountRoutine(player, rb, visuals, animator, _finalJumpRatio));

            _rollCoroutine = null;
            IsOn = true;
        }

        #region --- Audio Helper Methods ---

        private void PlayRollLoopAudio()
        {
            if (string.IsNullOrEmpty(rollLoopSfxKey) || SoundManager.Instance == null) return;

            InitRollAudioSource();

            // SoundManager의 sfxTable에 등록된 데이터 조회
            if (SoundManager.Instance.TryGetSFXData(rollLoopSfxKey, out SFXData sfxData))
            {
                if (sfxData.clip == null) return;

                if (AudioManager.Instance != null)
                {
                    _rollLoopAudioSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
                }

                _rollLoopAudioSource.clip = sfxData.clip;
                _rollLoopAudioSource.volume = sfxData.volume;
                _rollLoopAudioSource.pitch = sfxData.pitch;
                _rollLoopAudioSource.spatialBlend = sfxData.is3D ? 1f : 0f;
                _rollLoopAudioSource.loop = true;
                _rollLoopAudioSource.Play();
            }
            else
            {
                Debug.LogWarning($"[AttachToShaftOutput] Roll 루프 SFX 키 '{rollLoopSfxKey}'를 SFX 테이블에서 찾을 수 없습니다.");
            }
        }

        private void StopRollLoopAudio()
        {
            if (_rollLoopAudioSource != null && _rollLoopAudioSource.isPlaying)
            {
                _rollLoopAudioSource.Stop();
                _rollLoopAudioSource.clip = null;
            }
        }

        private void OnDisable()
        {
            StopRollLoopAudio();
        }

        #endregion

        /// <summary>
        /// 외부 스크립트 또는 이벤트에서 임의로 샤프트에서 강제 발사/탈출시킬 때 호출합니다.
        /// </summary>
        public void Detach(float customRatio = -1f)
        {
            if (!_isAttached) return;

            if (customRatio >= 0f)
            {
                _finalJumpRatio = Mathf.Clamp01(customRatio);
            }
            else
            {
                _finalJumpRatio = _isCharging ? _currentChargeRatio : triggerDismountRatio;
            }

            _isCharging = false;
            _chargePhase = ChargePhase.Idle;
            SetChargeEffectState(false);
            _wasJumpRequested = true;
        }

        private void UpdateTrajectoryPreview(Vector3 startPos)
        {
            if (!showTrajectory || trajectoryLine == null) return;

            Transform t1 = target1Transform != null ? target1Transform : target2Transform;
            Transform t2 = target2Transform != null ? target2Transform : target1Transform;

            if (t1 == null && t2 == null)
            {
                trajectoryLine.enabled = false;
                return;
            }

            trajectoryLine.enabled = true;

            Vector3 targetPos = Vector3.Lerp(t1.position, t2.position, _currentChargeRatio);
            float targetHeight = Mathf.Lerp(target1JumpHeight, target2JumpHeight, _currentChargeRatio);

            int count = Mathf.Max(2, trajectoryResolution);
            trajectoryLine.positionCount = count;

            for (int i = 0; i < count; i++)
            {
                float rawT = (float)i / (count - 1);
                float smoothT = Mathf.SmoothStep(0f, 1f, rawT);
                float heightOffset = 4f * targetHeight * rawT * (1f - rawT);

                Vector3 currentPos = Vector3.Lerp(startPos, targetPos, smoothT);
                currentPos.y += heightOffset;

                trajectoryLine.SetPosition(i, currentPos);
            }
        }

        private void DisableTrajectoryPreview()
        {
            if (trajectoryLine != null)
            {
                trajectoryLine.enabled = false;
            }
        }

        private IEnumerator DismountRoutine(PlayerLocomotion player, Rigidbody rb, PlayerVisuals visuals, Animator animator, float jumpRatio)
        {
            if (visuals != null) visuals.SetWindRollAnim(false);
            if (animator != null && !string.IsNullOrEmpty(rollAnimParamName))
            {
                animator.SetBool(rollAnimParamName, false);
            }

            player.SetRailMode(false);
            DisableTrajectoryPreview();

            Transform t1 = target1Transform != null ? target1Transform : target2Transform;
            Transform t2 = target2Transform != null ? target2Transform : target1Transform;

            if (t1 != null || t2 != null)
            {
                Vector3 p1 = t1 != null ? t1.position : t2.position;
                Vector3 p2 = t2 != null ? t2.position : t1.position;

                Vector3 startPos = player.transform.position;
                Vector3 endPos = Vector3.Lerp(p1, p2, jumpRatio);
                float selectedHeight = Mathf.Lerp(target1JumpHeight, target2JumpHeight, jumpRatio);
                float selectedDuration = Mathf.Lerp(target1JumpDuration, target2JumpDuration, jumpRatio);

                // 발사 효과음 출력 (지정된 경우)
                if (!string.IsNullOrEmpty(dismountLaunchSfxKey) && SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlaySFX(dismountLaunchSfxKey, startPos);
                }

                if (jumpParticlePrefab != null)
                {
                    Instantiate(jumpParticlePrefab, startPos, Quaternion.identity);
                }

                Vector3 jumpDirection = new Vector3(endPos.x - startPos.x, 0f, endPos.z - startPos.z);
                if (jumpDirection.sqrMagnitude > 0.01f)
                {
                    player.SetBaseRotation(jumpDirection.normalized);
                }

                bool wasKinematic = rb != null ? rb.isKinematic : false;
                if (rb != null) rb.isKinematic = true;

                float timer = 0f;

                while (timer < selectedDuration)
                {
                    yield return new WaitForFixedUpdate();

                    timer += Time.fixedDeltaTime;
                    float rawT = Mathf.Clamp01(timer / selectedDuration);

                    float smoothT = Mathf.SmoothStep(0f, 1f, rawT);
                    float heightOffset = 4f * selectedHeight * rawT * (1f - rawT);

                    Vector3 currentPos = Vector3.Lerp(startPos, endPos, smoothT);
                    currentPos.y += heightOffset;

                    if (rb != null) rb.MovePosition(currentPos);
                    else player.transform.position = currentPos;
                }

                if (rb != null)
                {
                    rb.MovePosition(endPos);
                    rb.isKinematic = wasKinematic;
                }
                else player.transform.position = endPos;

                Vector3 landingForwardImpulse = jumpDirection.normalized * 3.0f + Vector3.down * 2.0f;
                player.ExternalLaunch(landingForwardImpulse);
            }

            yield return null;
        }
    }
}