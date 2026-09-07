using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// ?�레?�어가 ?�역??진입?�면 ?�프?�에 밀�?고정?�어 말림 ?�전???�고,
    /// ?�페?�스�?차징??0.0~1.0)???�라 Target 1 ~ Target 2 ?�이???�시�??�물??궤적??그리�?
    /// 최고??2�?�?최�???2�??�서 머무르며 ?�복 충전?�는 ?�프 컴포?�트?�니??
    /// </summary>
    public class AttachToShaftOutput : ProcessBase
    {
        private enum ChargePhase
        {
            Idle,
            ChargingUp,     // 0% -> 100% (Target 1 -> Target 2 증�?)
            HoldingMax,     // 100% 최�?치에??2�?머무�?
            ReturningDown,  // 100% -> 0% (Target 2 -> Target 1 복�?)
            HoldingMin      // 0% 최소치에??2�?머무�?-> ?�후 ?�충??
        }

        [Header("Shaft & Attachment Settings")]
        [Tooltip("?�전?�는 ?�프???�통) Transform")]
        [SerializeField] private Transform shaftTransform;

        [Tooltip("?�레?�어가 �?밀착하??말릴 고정 ?�치 (?�프???�식?�로 배치??Transform)")]
        [SerializeField] private Transform attachPoint;

        [Tooltip("?�프?�의 길이/?�전 �?(보통 Local Right ?�는 Forward)")]
        [SerializeField] private Vector3 shaftLocalAxis = Vector3.right;

        [Header("Positioning & Snap Settings")]
        [Tooltip("진입 ??attachPoint ?�치�??�식간에 보정?��? ?��?")]
        [SerializeField] private bool snapToAttachPoint = true;

        [Tooltip("snapToAttachPoint가 false????attachPoint ?�치�?부?�럽�??�동?�는 ?�간(�?")]
        [SerializeField] private float snapDuration = 0.15f;

        [Header("Slide Along Shaft Settings")]
        [Tooltip("?�프?�에 매달�??�태?�서 �????�동 ?�용 ?��?")]
        [SerializeField] private bool allowAxisSliding = true;

        [Tooltip("�???조작 반�? ?��? (체크 ??A/D ??조작??반�?�???��??")]
        [SerializeField] private bool invertControls = false;

        [Tooltip("�????�라?�딩 ?�동 ?�도")]
        [SerializeField] private float slideSpeed = 6.0f;

        [Tooltip("AttachPoint 기�? ?�쪽(-)?�로 ?�동 가?�한 최�? 거리")]
        [SerializeField] private float leftSlideDistance = 3.0f;

        [Tooltip("AttachPoint 기�? ?�른�?+)?�로 ?�동 가?�한 최�? 거리")]
        [SerializeField] private float rightSlideDistance = 3.0f;

        [Header("Animation & Rotation Settings")]
        [Tooltip("말림 ?�전 ?�니메이???�도 배율 (기본�? 1.0)")]
        [SerializeField] private float rollAnimSpeed = 1.5f;

        [Tooltip("Animator?�서 말림 ?�니메이???�어???�용??Bool ?�라미터 ?�름")]
        [SerializeField] private string rollAnimParamName = "IsWindRolling";

        [Header("Key & Charge Settings")]
        [Tooltip("?�프/차징???�용????(기본�? Space)")]
        [SerializeField] private KeyCode dismountKey = KeyCode.Space;

        [Tooltip("Target 1?�서 Target 2(최�?�?까�? ?�달?�는 차징 ?�간(�?")]
        [SerializeField] private float chargeTimeThreshold = 1.0f;

        [Tooltip("최�?�?Target 2)???�달?�을 ??최고?�에??머무르는 ?�간(�?")]
        [SerializeField] private float maxHoldDuration = 2.0f;

        [Tooltip("최�?치에???�시 Target 1(0%)?�로 ?�르�??�돌?�오???�간(�?")]
        [SerializeField] private float returnDuration = 0.75f;

        [Tooltip("최소�?Target 1)�??�돌?�왔????최�??�에??머무르는 ?�간(�?")]
        [SerializeField] private float minHoldDuration = 2.0f;

        [Tooltip("최�????��???계속 ?�르�??�을 ???�시 충전??반복(Loop)?��? ?��?")]
        [SerializeField] private bool loopCharge = true;

        [Header("Target 1 Settings (Tap / Min - 최소 ?�치)")]
        [Tooltip("차징 0%?????�아�?1�?기본 목표 ?�치")]
        [SerializeField] private Transform target1Transform;

        [Tooltip("1�?목표 ?�프 최고 ?�이")]
        [SerializeField] private float target1JumpHeight = 3.0f;

        [Tooltip("1�?목표 ?�치까�? ?�아가???�요 ?�간(�?")]
        [SerializeField] private float target1JumpDuration = 0.8f;

        [Header("Target 2 Settings (Max - 최�? ?�치)")]
        [Tooltip("차징 100%?????�아�?2�?최�? 목표 ?�치")]
        [SerializeField] private Transform target2Transform;

        [Tooltip("2�?목표 ?�프 최고 ?�이")]
        [SerializeField] private float target2JumpHeight = 5.0f;

        [Tooltip("2�?목표 ?�치까�? ?�아가???�요 ?�간(�?")]
        [SerializeField] private float target2JumpDuration = 1.0f;

        [Header("Trajectory Parabola Line Settings")]
        [Tooltip("?�동 ?�산???�물??궤적??그려�?LineRenderer 컴포?�트")]
        [SerializeField] private LineRenderer trajectoryLine;

        [Tooltip("?�물??궤적 가?�드?�인 ?�성???��?")]
        [SerializeField] private bool showTrajectory = true;

        [Tooltip("?�물??궤적 곡선???�상??(??개수)")]
        [SerializeField] private int trajectoryResolution = 25;

        [Header("Jump Particle Settings (Optional)")]
        [Tooltip("?�프 발사 ???�폰???�티???�리??(?? DustExplosion 1)")]
        [SerializeField] private GameObject jumpParticlePrefab;

        [Header("Charge Visual Effect Settings (Optional)")]
        [Tooltip("차징 ??성?될 ?펙??게임?브?트")]
        [SerializeField] private GameObject chargeEffectObject;

        [Tooltip("차징 중 재생할 파티클 시스템")]
        [SerializeField] private ParticleSystem chargeParticleSystem;

        [Tooltip("?레?어 ?브?트 (비워??경우 ?동 ?색)")]
        [SerializeField] private PlayerLocomotion locomotion;

        private Coroutine _rollCoroutine;
        private PlayerLocomotion _lastTriggeredPlayer;

        private bool _isAttached = false;
        private bool _isCharging = false;
        private bool _wasJumpRequested = false;
        private float _currentSlideOffset = 0f;

        // 차징 ?�이???�어 변??
        private ChargePhase _chargePhase = ChargePhase.Idle;
        private float _phaseTimer = 0f;
        private float _currentChargeRatio = 0f;
        private float _finalJumpRatio = 0f;

        private void Update()
        {
            if (!_isAttached) return;

            // 1. 차징 ?�작
            if (Input.GetKeyDown(dismountKey))
            {
                _isCharging = true;
                _chargePhase = ChargePhase.ChargingUp;
                _phaseTimer = 0f;
                _currentChargeRatio = 0f;
                SetChargeEffectState(true);
            }

            // 2. 차징 ?�이??(증�? -> 2�?최고???��?-> 감소 -> 2�?최�????��?-> 루프)
            if (_isCharging && Input.GetKey(dismountKey))
            {
                UpdateChargeCycle();
            }

            // 3. ?�을 ?� ?�간???�확??비율�?발사
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
                    // 0% -> 100% 증�?
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
                    // 최고??Target 2)?�서 2.0�??��?
                    _currentChargeRatio = 1.0f;

                    if (_phaseTimer >= maxHoldDuration)
                    {
                        _phaseTimer = 0f;
                        _chargePhase = ChargePhase.ReturningDown;
                    }
                    break;

                case ChargePhase.ReturningDown:
                    // 100% -> 0%�?복�?
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
                    // 최�???Target 1)?�서 2.0�??��?
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

        // ★ 추가: MoveTo가 끝나면서 isRidingRail이 뒤늦게 false가 되어도 즉시 감지
        private void OnTriggerStay(Collider other)
        {
            CheckAndStartRoll(other);
        }

        private void CheckAndStartRoll(Collider other)
        {
            // 이미 붙어있거나 동작 코루틴이 도는 중이면 무시
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

            if (visuals != null) visuals.SetWindRollAnim(true, rollAnimSpeed);
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

            Vector3 worldAxis = shaftTransform != null
                ? shaftTransform.TransformDirection(shaftLocalAxis).normalized
                : Vector3.right;

            while (true)
            {
                if (_wasJumpRequested)
                {
                    _wasJumpRequested = false;
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
