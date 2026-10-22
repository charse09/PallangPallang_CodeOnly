using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerLocomotions : MonoBehaviour
{
    // 기존 변수 선언부에 추가
    [Header("Respawn State")]
    public bool isRespawning = false;

    /// <summary>
    /// true면 FaceDownFallControl이 물리를 직접 제어합니다.
    /// Update / FixedUpdate / LateUpdate의 로코모션 연산이 모두 차단됩니다.
    /// </summary>
    [Header("External Fall Control")]
    public bool isFallingControl = false;

    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private BoxCollider bodyCollider;
    [SerializeField] private Transform characterVisual;
    [SerializeField] private Camera cam1;
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerFaceMaterial faceStance;
    [SerializeField] private GameObject sphereObject;

    [Header("Ground Check Colliders")]
    [SerializeField] private BoxCollider feetGroundCollider;
    [SerializeField] private BoxCollider backGroundCollider;

    [Header("Visual Controllers")]
    [SerializeField] private PlayerColorController colorController;

    [Header("Movement Settings")]
    [SerializeField] private float slimStateSpeed = 4.0f;
    [SerializeField] private float moveSpeed = 16f;
    [SerializeField] private float jumpSpeed = 15f;
    [SerializeField] private float gravity = -30f;
    [SerializeField] private float paperGravity = -5f;
    [SerializeField] private float groundSharpness = 15f;
    [SerializeField] private float airSharpness = 2f;
    [SerializeField] private float airSpeedMultiplier = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Brace Settings")]
    [SerializeField] private bool useBraceTimeLimit = true;
    [SerializeField] private float maxBraceTime = 2.0f;
    [SerializeField] private float normalRecoveryDelay = 1.0f;
    [SerializeField] private float exhaustedRecoveryDelay = 3.0f;
    [SerializeField] private float braceRecoverySpeed = 2.0f;

    [Header("Rail System")]
    public bool isRidingRail = false;

    [Header("Slide Zone Settings")]
    [SerializeField] private bool isInsideSlideZone = false;
    private Vector3 fixedSlideDirection = Vector3.forward;
    private Vector3 fixedSlideRight = Vector3.right;
    private float slideZoneForwardSpeed = 16f;
    private float slideZoneSharpness = 1.5f; // 낮을수록 더 미끌거림

    private float braceTimer = 0f;
    private bool braceExhausted = false;
    private float currentRecoveryWaitTimer = 0f;

    [Header("Slide Settings")]
    [SerializeField] private float slideSpeed = 35f;
    [SerializeField] private float slideDuration = 1.0f;
    [SerializeField] private float slideCooldown = 3.0f;

    private bool isSliding = false;
    private float currentSlideTimer = 0f;
    private float slideCooldownTimer = 0f;
    private Vector3 slideDirection;
    private bool isSlideFalling = false;

    [Header("Player State")]
    [SerializeField] private bool paperReadyState = false;
    [SerializeField] private bool paperState = false;
    [SerializeField] private bool slimState = false;
    [SerializeField] private bool slimCompletelyState = false;
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool braceState = false;
    [SerializeField] private float visualTiltAngle = 30.0f;

    private bool lastPaperState = false;
    private bool lastSlimState = false;

    [Header("Trigger")]
    [SerializeField] private Collider slimTriggerCollider;

    [Header("Paper Sway Settings")]
    [SerializeField] private float swayAmount = 15f;
    [SerializeField] private float swaySpeed = 5f;
    [SerializeField] private Vector3 swayPivot = new Vector3(0f, 0f, 1f);

    [Header("External Physics")]
    private Vector3 externalHorizontalVelocity;
    [SerializeField] private float externalForceDamping = 5f;
    private float windBufferTimer = 0f;

    [Header("Condition Modifiers (FSM)")]
    public float speedModifier = 1.0f;
    public float jumpModifier = 1.0f;
    public float slideDurationModifier = 1.0f;
    public bool isWet2Restricted = false; // true면 이동/점프 외 스킬 사용 불가

    [Header("Wind Roll Settings")]
    [SerializeField] private GameObject rollingVisualPrefab; // 구르기 상태일 때 켜질 프리팹
    [SerializeField] private Animator rollingAnim; // 구르기 프리팹의 애니메이터
    [SerializeField] private float rollSideSpeed = 10f; // 구르는 동안 좌우 이동 속도
    [SerializeField] private float windRollCooldown = 1.0f;
    // --- [추가] 인스펙터에서 조절할 구르기 전용 점프력 ---
    [SerializeField] private float windRollJumpSpeed = 12f;

    private Vector3 inputMoveDir;
    private Vector3 lerpMoveDir;
    private float verticalVelocity;

    private Coroutine currentCoroutine;
    private Vector3 currentGroundNormal = Vector3.up;

    private float swayTimer = 0f;
    private Quaternion baseMoveRotation = Quaternion.identity;
    private float swayWeight = 0f;

    // --- 플랫폼 추적 변수 ---
    private Rigidbody currentPlatformRB;
    private bool isOnPlatform = false;
    private float groundCheckTimer = 0f;
    private float platformCheckTimer = 0f;

    // --- 추가 플랫폼 동기화 변수 ---
    private Rigidbody lastPlatformRB;
    private Vector3 prevPlatformPos;
    private Quaternion prevPlatformRot;
    private Vector3 platformMovementVelocity;

    public bool isWindRolling = false;
    private Vector3 currentWindDir;
    private float windRollCooldownTimer = 0f;
    private float rollBounceTimer = 0f;

    // -- 승태 추가 변수 -- : 강제로 앉히기 위한 변수
    private bool isForcedCrouch = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (bodyCollider == null) bodyCollider = GetComponent<BoxCollider>();

        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        baseMoveRotation = transform.rotation;

        //if (GetComponent<PlayerTelekinesisController>() == null)
        //{
        //    gameObject.AddComponent<PlayerTelekinesisController>();
        //}
    }

    // FSM에서 페널티를 주입받는 메서드
    public void ApplyConditionModifiers(float speedMod, float jumpMod, float slideMod, bool restricted)
    {
        speedModifier = speedMod;
        jumpModifier = jumpMod;
        slideDurationModifier = slideMod;
        isWet2Restricted = restricted;
    }

    // Wet2 상태 진입 시 강제로 슬라임/페이퍼 상태를 해제하는 메서드
    public void CancelSpecialStatesForCondition()
    {
        // 콜라이더를 건드리지 않고, 기존 로직의 OnPaperStateChanged/OnSlimStateChanged를 
        // 자연스럽게 태우기 위해 플래그만 변경합니다. 
        // 이렇게 해야 시각적 효과를 담당하는 코루틴(RotationRoutine)이 정상적으로 
        // Y축 90도 회전을 원래대로 부드럽게 되돌려놓습니다.
        if (paperState) paperState = false;
        if (slimState) slimState = false;
        braceState = false;
        isSliding = false;
        slimCompletelyState = false;
    }

    public void UpdatePlayerVisuals(PlayerConditionState state)
    {
        if (colorController != null)
        {
            colorController.ChangeColorByState(state);
        }
    }

    private void OnEnable()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        verticalVelocity = 0f;
        inputMoveDir = Vector3.zero;
        lerpMoveDir = Vector3.zero;

        isSliding = false;
        isGrounded = false;
        isSlideFalling = false;
        slideCooldownTimer = 0f;

        currentPlatformRB = null;
        isOnPlatform = false;
        groundCheckTimer = 0f;
        platformCheckTimer = 0f;

        lastPlatformRB = null;
        prevPlatformPos = Vector3.zero;
        prevPlatformRot = Quaternion.identity;
        platformMovementVelocity = Vector3.zero;

        lastPaperState = paperState;
        lastSlimState = slimState;

        UpdateGroundColliders();
    }

    void Update()
    {
        if (isRidingRail || isRespawning || isFallingControl) return; // 리스폰/낙하 제어 중 차단

        if (slideCooldownTimer > 0f) slideCooldownTimer -= Time.deltaTime;

        if (windRollCooldownTimer > 0f) windRollCooldownTimer -= Time.deltaTime;

        HandleInputs();

        if (paperState != lastPaperState) { lastPaperState = paperState; OnPaperStateChanged(); }
        if (slimState != lastSlimState) { lastSlimState = slimState; OnSlimStateChanged(); }

        if (!isGrounded && paperState) swayWeight = Mathf.MoveTowards(swayWeight, 1f, Time.deltaTime * 3f);
        else swayWeight = Mathf.MoveTowards(swayWeight, 0f, Time.deltaTime * 8f);

        if (paperState)
        {
            sphereObject.gameObject.SetActive(true);
            swayTimer += Time.deltaTime * swaySpeed;
        }
        else sphereObject.gameObject.SetActive(false);

        PlayerFaceControl();
    }

    void FixedUpdate()
    {
        if (isRidingRail || isRespawning || isFallingControl) return; // 리스폰/낙하 제어 중 물리 연산 차단

        if (groundCheckTimer > 0f) groundCheckTimer -= Time.fixedDeltaTime;
        if (platformCheckTimer > 0f) platformCheckTimer -= Time.fixedDeltaTime;

        isGrounded = groundCheckTimer > 0f;
        isOnPlatform = platformCheckTimer > 0f;

        if (!isOnPlatform) currentPlatformRB = null;

        // --- 플랫폼 동기화 연산 ---
        if (isOnPlatform && currentPlatformRB != null)
        {
            if (currentPlatformRB == lastPlatformRB)
            {
                Vector3 platformPos = currentPlatformRB.position;
                Quaternion platformRot = currentPlatformRB.rotation;

                // 플랫폼의 선속도와 각속도 회전을 모두 고려한 변위 계산
                Vector3 localPos = rb.position - prevPlatformPos;
                Quaternion deltaRot = platformRot * Quaternion.Inverse(prevPlatformRot);
                Vector3 targetGlobalPos = deltaRot * localPos + platformPos;
                Vector3 platformDeltaPos = targetGlobalPos - rb.position;

                platformMovementVelocity = platformDeltaPos / Time.fixedDeltaTime;

                // 플레이어가 멈춰있을 때 플랫폼 회전에 맞춰 baseMoveRotation 회전시킴
                if (inputMoveDir.sqrMagnitude <= 0.01f)
                {
                    Vector3 EulerDelta = deltaRot.eulerAngles;
                    float deltaY = EulerDelta.y;
                    if (deltaY > 180f) deltaY -= 360f;
                    baseMoveRotation = Quaternion.Euler(0, deltaY, 0) * baseMoveRotation;
                }
            }
            else
            {
                // 플랫폼이 새로 변경된 첫 프레임에는 GetPointVelocity로 선속도 즉시 보간하여 1프레임 미끄러짐 방지
                platformMovementVelocity = currentPlatformRB.GetPointVelocity(rb.position);
            }

            lastPlatformRB = currentPlatformRB;
            prevPlatformPos = currentPlatformRB.position;
            prevPlatformRot = currentPlatformRB.rotation;
        }
        else
        {
            platformMovementVelocity = Vector3.zero;
            lastPlatformRB = null;
        }

        ApplyCustomGravity();
        CalculateMovement();
    }

    void LateUpdate()
    {
        if (isRidingRail || isFallingControl) return; // 레일/낙하 제어 중 기본 회전 연산 정지

        if (currentCoroutine == null)
        {
            UpdateMovementRotation();
            ApplyFinalRotation();
        }
    }

    private void OnPaperStateChanged()
    {
        UpdateGroundColliders();
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(RotationRoutine());
        if (!paperState) isSliding = false;
    }

    private void OnSlimStateChanged()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(RotationRoutine());
    }

    private void UpdateGroundColliders()
    {
        if (backGroundCollider != null) backGroundCollider.enabled = paperState;
        if (feetGroundCollider != null) feetGroundCollider.enabled = !paperState;
    }

    private void EvaluateGroundCollision(Collision collision)
    {
        bool isCurrentPlatform = currentPlatformRB != null && collision.rigidbody == currentPlatformRB;

        if (((1 << collision.gameObject.layer) & groundLayer) != 0 || isCurrentPlatform)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                if (contact.thisCollider == feetGroundCollider || contact.thisCollider == backGroundCollider)
                {
                    if (Vector3.Angle(Vector3.up, contact.normal) <= 45f)
                    {
                        groundCheckTimer = 0.25f;
                        isGrounded = true;
                        isSlideFalling = false;
                        currentGroundNormal = contact.normal;

                        Rigidbody hitRB = contact.otherCollider.attachedRigidbody;
                        if (hitRB != null)
                        {
                            currentPlatformRB = hitRB;
                            platformCheckTimer = 0.25f;
                            isOnPlatform = true;
                        }
                        return;
                    }
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision) => EvaluateGroundCollision(collision);
    private void OnCollisionStay(Collision collision) => EvaluateGroundCollision(collision);

    public void AddContinuousForce(Vector3 force)
    {
        windBufferTimer = 0.5f;
        externalHorizontalVelocity += new Vector3(force.x, 0f, force.z);
        if (Mathf.Abs(force.y) > 0.1f)
        {
            if (force.y > 0 && isGrounded)
            {
                isGrounded = false;
                groundCheckTimer = 0f;
                rb.position += Vector3.up * 0.1f;
            }
            verticalVelocity += force.y;
        }
    }

    public void ExternalLaunch(Vector3 launchVelocity)
    {
        verticalVelocity = launchVelocity.y;
        externalHorizontalVelocity = new Vector3(launchVelocity.x, 0, launchVelocity.z);

        isGrounded = false;
        isOnPlatform = false;
        groundCheckTimer = 0f;
        platformCheckTimer = 0f;
        isSliding = false;

        // 날아가는 동안 3배 강한 감쇠(Damping)가 즉시 들어가는 것을 방지
        windBufferTimer = 0.5f;

        // [핵심 추가 코드] 바닥 충돌 판정(OnCollisionStay)이 다시 발동하지 못하도록
        // 캐릭터를 물리적으로 바닥에서 살짝 띄워버립니다.
        if (launchVelocity.y > 0)
        {
            rb.position += Vector3.up * 0.2f;
        }

        if (rb != null) rb.linearVelocity = launchVelocity;
    }

    private void ApplyExternalForceDamping()
    {
        if (windBufferTimer > 0f) windBufferTimer -= Time.fixedDeltaTime;
        float currentDamping = (windBufferTimer > 0f) ? externalForceDamping : externalForceDamping * 3f;
        externalHorizontalVelocity = Vector3.Lerp(externalHorizontalVelocity, Vector3.zero, Time.fixedDeltaTime * currentDamping);
    }

    void HandleInputs()
    {
        // 1. 공통으로 사용하는 입력값은 함수 맨 위에서 한 번만 받습니다.
        Vector2 input = InputManager.Instance.Movement;
        bool braceInput = Input.GetKey(KeyCode.C);

        // ==========================================
        // [특수 상태] 바람 구역 구르기 모드
        // ==========================================
        if (isWindRolling)
        {
            // 바람의 수직 방향(좌우) 계산
            Vector3 windRight = Vector3.Cross(Vector3.up, currentWindDir).normalized;

            // 전진/후진 입력 무시하고, 좌우(input.x) 조작만 허용
            inputMoveDir = windRight * input.x;

            if (InputManager.Instance.JumpPressed && isGrounded)
            {
                JumpOutFromRoll();
            }


            // 점프, 눕기 등 기타 모든 입력 무시
            return;
        }

        // ==========================================
        // [특수 상태] 강제 슬라이드 구역 (미끄럼틀/얼음판)
        // ==========================================
        if (isInsideSlideZone)
        {
            // A. [수정됨] 방향 강제 및 Paper 상태 시 좌우 이동 차단
            // paperState가 true면 좌우 입력을 0으로 무시, 아니면 원래 입력(input.x) 사용
            float currentInputX = paperState ? 0f : input.x;
            inputMoveDir = (fixedSlideDirection + fixedSlideRight * currentInputX).normalized;

            // B. 눕기(Brace) 로직 차단: 슬라이드 중에는 엎드릴 수 없음
            braceState = false;
            // paperState = false;

            // C. 점프 로직 (기존과 동일)
            if (InputManager.Instance.JumpPressed && isGrounded && !slimState && !paperState)
            {
                isGrounded = false;
                isOnPlatform = false;
                groundCheckTimer = 0f;
                platformCheckTimer = 0f;
                verticalVelocity = jumpSpeed;
                rb.position += Vector3.up * 0.1f;
                isSliding = false;
            }

            // D. Paper 상태 전환 허용
            if (InputManager.Instance.PaperStateKeyTrigger && !slimState && !braceState)
            {
                // 공중에서 종이 상태일 때 실수로 풀리는 것을 방지
                if (!(!isGrounded && paperState)) paperState = !paperState;
            }

            // E. 애니메이션
            if (anim != null)
            {
                anim.SetBool("IsWalk", true);
                anim.SetBool("IsCrouch", braceState);
            }

            // [핵심] 일반 로직을 씹고 여기서 끝냄
            return;
        }

        // ==========================================
        // [일반 상태] 평상시 자유 이동 로직
        // ==========================================
        Vector3 forward = cam1.transform.forward;
        Vector3 right = cam1.transform.right;
        forward.y = 0; right.y = 0; forward.Normalize(); right.Normalize();

        inputMoveDir = (forward * input.y + right * input.x);

        if (isForcedCrouch)
        {
            braceState = true;
            if (anim != null) anim.SetBool("IsCrouch", true);
        }
        else
        {
            // 일반 상태 눕기(Brace) 기존 순수 로직
            if (braceInput && isGrounded && !slimState && !paperState && !braceExhausted && !isWet2Restricted)
            {
                braceState = true;
                if (useBraceTimeLimit)
                {
                    if (windBufferTimer > 0f) braceTimer += Time.deltaTime;
                    currentRecoveryWaitTimer = normalRecoveryDelay;
                    if (braceTimer >= maxBraceTime) { braceState = false; braceExhausted = true; currentRecoveryWaitTimer = exhaustedRecoveryDelay; }
                }
            }
            else
            {
                braceState = false;
                if (useBraceTimeLimit && !braceInput)
                {
                    if (currentRecoveryWaitTimer > 0f) currentRecoveryWaitTimer -= Time.deltaTime;
                    else if (braceTimer > 0f) { braceTimer -= Time.deltaTime * braceRecoverySpeed; if (braceTimer <= 0f) { braceTimer = 0f; braceExhausted = false; } }
                }
            }
        }


        // 일반 상태에서는 눕거나 종이 상태로 바닥에 붙으면 이동(inputMoveDir)을 0으로 멈춥니다.
        if (braceState || (paperState && isGrounded && !isSliding)) inputMoveDir = Vector3.zero;

        // 일반 상태 점프
        if (InputManager.Instance.JumpPressed && isGrounded && !slimState && !paperState && !isWet2Restricted)
        {
            isGrounded = false;
            isOnPlatform = false;
            groundCheckTimer = 0f;
            platformCheckTimer = 0f;

            if (currentPlatformRB != null)
            {
                Vector3 platformVel = currentPlatformRB.GetPointVelocity(rb.position);
                externalHorizontalVelocity += new Vector3(platformVel.x, 0, platformVel.z);
            }

            // 점프력에 Modifier를 곱해서 점프 높이를 낮춥니다.
            verticalVelocity = jumpSpeed * jumpModifier;
            rb.position += Vector3.up * 0.1f;
            isSliding = false;
        }

        // 폼 체인지 및 기타 스킬 입력 (슬라이드 구역 밖에서만 가능)
        if (InputManager.Instance.PaperStateKeyTrigger && !slimState && !braceState && !isWet2Restricted)
        {
            if (!(!isGrounded && paperState)) paperState = !paperState;
        }

        // 슬라이드 입력 수정
        if (Input.GetKeyDown(KeyCode.LeftShift) && slideCooldownTimer <= 0f && isGrounded && !slimState && !braceState && !isWet2Restricted)
        {
            if (!paperState) paperState = true;
            if (inputMoveDir.sqrMagnitude > 0.1f)
            {
                isSliding = true;
                currentSlideTimer = slideDuration * slideDurationModifier; // 거리 페널티 적용
                slideDirection = inputMoveDir.normalized;
                slideCooldownTimer = slideCooldown;
            }
        }

        if (InputManager.Instance.SlimStateKeyTrigger && !paperState && !braceState && !isWet2Restricted)
        {
            slimState = !slimState;
        }

        if (anim != null)
        {
            anim.SetBool("IsWalk", inputMoveDir.sqrMagnitude > 0.01f);
            anim.SetBool("IsCrouch", braceState);
        }

        
    }

    void UpdateMovementRotation()
    {
        // [추가] 바람 구역 구르기 중일 때는 조이스틱(좌우) 방향으로 회전하지 않음 (바라보던 방향 유지)
        if (isWindRolling)
        {
            if (currentCoroutine == null) ApplyFinalRotation();
            return;
        }

        if (isInsideSlideZone)
        {
            baseMoveRotation = Quaternion.LookRotation(fixedSlideDirection);
            if (currentCoroutine == null) ApplyFinalRotation();
            return;
        }

        if (isSliding) { baseMoveRotation = Quaternion.LookRotation(slideDirection); if (currentCoroutine == null) ApplyFinalRotation(); return; }

        if (inputMoveDir.sqrMagnitude > 0.01f)
        {
            baseMoveRotation = Quaternion.LookRotation(inputMoveDir);
        }

        if (currentCoroutine == null) ApplyFinalRotation();
    }

    void ApplyFinalRotation()
    {
        Quaternion stateOffset = Quaternion.identity;
        if (paperState) stateOffset = Quaternion.Euler(-90f, 0, 0);
        else if (slimState) stateOffset = Quaternion.Euler(0, 90f, 0);
        transform.rotation = baseMoveRotation * stateOffset;

        if (characterVisual != null)
        {
            float currentTilt = slimCompletelyState ? 0.0f : 15.0f;
            if (paperState)
            {
                float angle = Mathf.Sin(swayTimer) * swayAmount * swayWeight;
                Quaternion swayRot = Quaternion.Euler(currentTilt, angle, 0f);
                Vector3 swayPos = swayPivot + (swayRot * -swayPivot);

                characterVisual.localPosition = Vector3.Lerp(Vector3.zero, swayPos, swayWeight);
                characterVisual.localRotation = Quaternion.Slerp(Quaternion.Euler(10f, 0f, 0f), swayRot, swayWeight);
            }
            else if (slimState)
            {
                characterVisual.localPosition = Vector3.zero;
                characterVisual.localRotation = Quaternion.Euler(0, -currentTilt, 0);
            }
            else
            {
                characterVisual.localPosition = Vector3.zero;
                characterVisual.localRotation = Quaternion.identity;
            }
        }
    }

    IEnumerator RotationRoutine()
    {
        float t = 0;
        Quaternion startRot = transform.rotation;
        Quaternion startVisualRot = (characterVisual != null) ? characterVisual.localRotation : Quaternion.identity;

        while (t < 1.0f)
        {
            t += Time.deltaTime * 8f;
            Quaternion stateOffset = paperState ? Quaternion.Euler(-90f, 0, 0) : (slimState ? Quaternion.Euler(0, 90f, 0) : Quaternion.identity);
            transform.rotation = Quaternion.Slerp(startRot, baseMoveRotation * stateOffset, t);

            if (characterVisual != null)
            {
                float currentTilt = slimCompletelyState ? 0f : visualTiltAngle;
                float swayY = paperState ? Mathf.Sin(swayTimer) * swayAmount * swayWeight : 0f;
                Quaternion targetVisualRot = paperState ? Quaternion.Euler(currentTilt, swayY, 0) :
                                            (slimState ? Quaternion.Euler(0, -currentTilt, 0) : Quaternion.identity);
                characterVisual.localRotation = Quaternion.Slerp(startVisualRot, targetVisualRot, t);
            }
            yield return null;
        }
        currentCoroutine = null;
    }

    void ApplyCustomGravity()
    {
        // [해결책 1] 플랫폼 위에 안착해 있을 때는 대폭발을 막기 위해 무거운 중력을 끕니다.
        if (isOnPlatform && isGrounded && !isSlideFalling)
        {
            // 단, 미세하게 뜨면서 덜덜거리는 현상을 막기 위해 아주 약한 자석 힘(-2f)만 바닥으로 가합니다.
            verticalVelocity = -2f;
            return;
        }

        if (!isGrounded || isSlideFalling)
        {
            float targetGravity = (paperState && verticalVelocity < 0 && !isSlideFalling) ? paperGravity : gravity;
            verticalVelocity += targetGravity * Time.fixedDeltaTime;

            float maxFallSpeed = (paperState && !isSlideFalling) ? paperGravity : gravity;
            if (verticalVelocity < maxFallSpeed) verticalVelocity = Mathf.Lerp(verticalVelocity, maxFallSpeed, Time.fixedDeltaTime * 6f);
        }
    }

    void CalculateMovement()
    {
        ApplyExternalForceDamping();

        if (isSliding)
        {
            currentSlideTimer -= Time.fixedDeltaTime;
            if (currentSlideTimer <= 0f || !isGrounded || !paperState)
            {
                if (!isGrounded && paperState) isSlideFalling = true;
                isSliding = false;

                float endRatio = Mathf.Max(0f, currentSlideTimer / slideDuration);
                float endSpeed = Mathf.SmoothStep(0f, slideSpeed, endRatio);
                float nextTargetSpeed = !isGrounded ? moveSpeed * airSpeedMultiplier : moveSpeed;
                float baseSpeed = Mathf.Max(nextTargetSpeed, 0.1f);
                lerpMoveDir = slideDirection * (endSpeed / baseSpeed);
            }
        }

        Vector3 movementVelocity;

        if (isSliding)
        {
            float speedRatio = currentSlideTimer / slideDuration;
            float currentSpeed = Mathf.SmoothStep(0f, slideSpeed, speedRatio);
            float slopeAngle = Vector3.Angle(Vector3.up, currentGroundNormal);

            if (slopeAngle > 0 && slopeAngle <= 45f) movementVelocity = Vector3.ProjectOnPlane(slideDirection, currentGroundNormal).normalized * currentSpeed;
            else movementVelocity = slideDirection * currentSpeed;

            if (verticalVelocity <= 0 && !isOnPlatform) verticalVelocity = -1f;
        }
        else
        {
            // 목표 이동 속도에 Modifier를 곱해 이동 속도를 느리게 만듭니다.
            float targetSpeed = moveSpeed * speedModifier;
            float currentSharpness = isGrounded ? groundSharpness : airSharpness;

            if (isInsideSlideZone)
            {
                targetSpeed = slideZoneForwardSpeed;
                currentSharpness = isGrounded ? slideZoneSharpness : airSharpness;
            }
            // [추가된 부분] 구르기 상태일 때의 속도 및 마찰력 설정
            else if (isWindRolling)
            {
                targetSpeed = rollSideSpeed;
                currentSharpness = isGrounded ? 5f : airSharpness; // 구를 때 좌우 조향감 조정
            }

            
            if (lerpMoveDir.magnitude > 1.2f) currentSharpness = isGrounded ? 3f : 0.5f;
            if (!isGrounded) targetSpeed *= airSpeedMultiplier;

            lerpMoveDir = Vector3.Lerp(lerpMoveDir, inputMoveDir, currentSharpness * Time.fixedDeltaTime);

            if (isGrounded)
            {
                float slopeAngle = Vector3.Angle(Vector3.up, currentGroundNormal);
                if (slopeAngle > 0 && slopeAngle <= 45f) movementVelocity = Vector3.ProjectOnPlane(lerpMoveDir, currentGroundNormal).normalized * (lerpMoveDir.magnitude * targetSpeed);
                else movementVelocity = lerpMoveDir * targetSpeed;

                if (verticalVelocity <= 0 && !isOnPlatform) verticalVelocity = -1f;
            }
            else movementVelocity = lerpMoveDir * targetSpeed;

            if (slimState) movementVelocity = lerpMoveDir * slimStateSpeed;
        }

        Vector3 finalVelocity = movementVelocity;
        finalVelocity += braceState ? externalHorizontalVelocity * 0.0f : externalHorizontalVelocity;

        // --- [해결책 2: Y축 개입 차단] ---
        if (isOnPlatform && currentPlatformRB != null)
        {
            // 플랫폼의 선속도 및 회전을 포함한 정밀 동기화 속도 적용
            finalVelocity.x += platformMovementVelocity.x;
            finalVelocity.z += platformMovementVelocity.z;

            // Y축은 플랫폼 수직 속도와 캐릭터의 수직 속도를 조합하여 대입
            finalVelocity.y = verticalVelocity + platformMovementVelocity.y;
        }
        else
        {
            if (isGrounded && !isSlideFalling)
            {
                finalVelocity.y = -1f;
                verticalVelocity = -1f;
            }
            else
            {
                finalVelocity.y = verticalVelocity;
            }
        }

        rb.linearVelocity = finalVelocity;
    }

    void PlayerFaceControl()
    {
        if (faceStance == null) return;
        bool isMovingHorizontally = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).sqrMagnitude > 0.1f;
        if (braceState) faceStance.SetStareFace();
        else if (paperState && (isSliding || isMovingHorizontally)) faceStance.SetSurprizeFace();
        else if (paperState) faceStance.SetEmbarassedFace();
        else if (slimState) faceStance.SetStareFace();
        else if (!isGrounded) faceStance.SetKawaiiFace();
        else faceStance.SetNormalFace();
    }

    public bool SlimCompletely
    {
        get { return slimCompletelyState; }
        set { if (slimCompletelyState != value) { slimCompletelyState = value; if (currentCoroutine == null) ApplyFinalRotation(); } }
    }

    public bool PaperState
    {
        set { if (!isGrounded && paperState) return; paperState = !paperState; if (currentCoroutine != null) StopCoroutine(currentCoroutine); currentCoroutine = StartCoroutine(RotationRoutine()); }
    }

    public float SlideCooldownTimer => slideCooldownTimer;
    public float SlideCooldown => slideCooldown;
    public void SetRailMode(bool isEntering)
    {
        isRidingRail = isEntering;

        // [추가된 부분] 레일 탑승 여부에 따라 애니메이터의 IsHangOn 상태를 자동으로 켜고 끕니다.
        if (anim != null)
        {
            anim.SetBool("IsHangOn", isEntering);
        }

        if (isEntering)
        {
            // 1. 상태 강제 해제
            paperState = false;
            lastPaperState = false;
            slimState = false;
            lastSlimState = false;
            braceState = false;
            isSliding = false;
            slimCompletelyState = false;
            swayWeight = 0f;

            // 2. 물리 끄기 (관성 및 중력 완벽 차단)
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // 3. 코루틴 중지 및 시각적 로테이션 강제 초기화 (Y축 90도 고정 버그 완벽 차단)
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
                currentCoroutine = null;
            }

            baseMoveRotation = Quaternion.identity;
            transform.rotation = Quaternion.identity;

            if (characterVisual != null)
            {
                characterVisual.localPosition = Vector3.zero;
                characterVisual.localRotation = Quaternion.identity;
            }

            UpdateGroundColliders();
            if (sphereObject != null) sphereObject.gameObject.SetActive(false);
        }
        else
        {
            // 레일 종료 시 물리 다시 켜기
            rb.isKinematic = false;

            // 공중에서 레일이 끝날 경우를 대비해 수직 속도 초기화
            verticalVelocity = 0f;
            externalHorizontalVelocity = Vector3.zero;
        }
    }

    public void EnterSlideZone(Vector3 direction, float speed, float sharpness)
    {
        isInsideSlideZone = true;
        fixedSlideDirection = direction.normalized;
        // 진행 방향의 수직인 오른쪽 축을 계산해 좌우 이동 축으로 삼음
        fixedSlideRight = Vector3.Cross(Vector3.up, fixedSlideDirection).normalized;
        slideZoneForwardSpeed = speed;
        slideZoneSharpness = sharpness;

        // 진입 시 기존 변신 상태들 강제 해제 및 초기화
        paperState = false;
        slimState = false;
        isSliding = false;
        braceState = false;
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
        UpdateGroundColliders();
    }

    public void ExitSlideZone()
    {
        isInsideSlideZone = false;
    }

    public void SetPlatform(Rigidbody platformRB)
    {
        if (platformRB != null)
        {
            currentPlatformRB = platformRB;
            platformCheckTimer = 0.25f;
            isOnPlatform = true;
            isGrounded = true;
            groundCheckTimer = 0.25f;

            // 즉시 추적 데이터 초기화하여 1프레임 렉 완벽 방지
            lastPlatformRB = platformRB;
            prevPlatformPos = platformRB.position;
            prevPlatformRot = platformRB.rotation;
            if (rb != null)
            {
                platformMovementVelocity = platformRB.GetPointVelocity(rb.position);
                rb.linearVelocity = Vector3.zero;
            }
            else
            {
                platformMovementVelocity = Vector3.zero;
            }
        }
    }

    // 스크립트 하단 쯤에 제어 메서드 추가
    public void SetRespawnMode(bool isEntering)
    {
        isRespawning = isEntering;

        if (isEntering)
        {
            // SetRailMode와 동일하게 기존 상태들 강제 해제 및 코루틴 정지
            paperState = false;
            slimState = false;
            braceState = false;
            isSliding = false;
            slimCompletelyState = false;

            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
                currentCoroutine = null;
            }

            // 물리 연산 완벽 차단
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            // 연출 종료 시 물리 원상 복구 및 수직 속도 초기화
            rb.isKinematic = false;
            verticalVelocity = 0f;
            externalHorizontalVelocity = Vector3.zero;

            // 정상 표정 복구
            if (faceStance != null) faceStance.SetNormalFace();
        }
    }

    public void ForceRespawnAction()
    {
        // 낙하 연출 진행 중 강제로 보여줄 모습
        // 기획에 맞게 paperState를 true로 만들어서 종이처럼 팔랑거리게 하거나, 
        // 작성해두신 PlayerFaceMaterial을 이용해 표정을 바꿀 수 있습니다.

        if (faceStance != null)
        {
            faceStance.SetSurprizeFace(); // 떨어지는 동안 놀란 표정 유지
        }

        // 필요하다면 여기서 애니메이터 트리거를 호출해도 됩니다.
        // if (anim != null) anim.SetTrigger("Fall"); 
    }


    //
    //
    public void SetForceBraceState(bool isForced)
    {
        isForcedCrouch = isForced; // 매 프레임 업데이트가 덮어쓰지 못하도록 방어막 가동
        braceState = isForced;

        if (anim != null)
        {
            anim.SetBool("IsCrouch", isForced);
        }
    }

    public void SetBaseRotation(Vector3 lookForward)
    {
        // 공중으로 튀어 오를 때 몸이 위아래로 눕지 않고 좌우 회전(Y축)만 하도록 평탄화
        lookForward.y = 0;

        if (lookForward.sqrMagnitude > 0.01f)
        {
            baseMoveRotation = Quaternion.LookRotation(lookForward.normalized);
            transform.rotation = baseMoveRotation;
        }
    }

    /// <summary>
    /// 컴포넌트가 비활성화된 동안 누적된 내부 속도 변수를 초기화합니다.
    /// ExternalLaunch와 달리 isGrounded 등 접지 상태를 건드리지 않으므로
    /// 재활성화 직전 상태 동기화 용도로 안전하게 사용할 수 있습니다.
    /// </summary>
    public void ResetInternalVelocities()
    {
        verticalVelocity = 0f;
        externalHorizontalVelocity = Vector3.zero;
        lerpMoveDir = Vector3.zero;
        inputMoveDir = Vector3.zero;
    }

    public bool EnterWindRollMode(Vector3 windDir)
    {
        // 쿨타임 중이거나 이미 구르고 있다면 진입 거부
        if (isWindRolling || windRollCooldownTimer > 0f || !isGrounded) return false;

        isWindRolling = true;
        currentWindDir = windDir.normalized;

        // 기존 비주얼 비활성화, 구르기 프리팹 활성화
        if (characterVisual != null) characterVisual.gameObject.SetActive(false);
        if (rollingVisualPrefab != null) rollingVisualPrefab.SetActive(true);

        // 바람 방향과 플레이어 방향을 비교하여 앞/뒤 판별
        // dot > 0 이면 뒤에서 바람을 맞아 앞으로 날아가는 상태
        float dot = Vector3.Dot(transform.forward, currentWindDir);

        if (rollingAnim != null)
        {
            rollingAnim.SetBool("IsRolling", dot > 0);
            rollingAnim.SetBool("IsReverseRolling", dot <= 0);
        }

        // 기존 상태들 강제 해제 (SlideZone 진입과 유사한 처리)
        paperState = false;
        slimState = false;
        braceState = false;
        isSliding = false;
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
            currentCoroutine = null;
        }
        UpdateGroundColliders();

        return true;
    }

    public void ExitWindRollMode()
    {
        if (!isWindRolling) return;

        isWindRolling = false;

        windRollCooldownTimer = windRollCooldown;

        // 비주얼 원상 복구
        if (characterVisual != null) characterVisual.gameObject.SetActive(true);
        if (rollingVisualPrefab != null) rollingVisualPrefab.SetActive(false);

        if (rollingAnim != null)
        {
            rollingAnim.SetBool("IsRolling", false);
            rollingAnim.SetBool("IsReverseRolling", false);
        }

        // [수정됨] 플레이어의 정면(transform.forward)이 아니라, 바람이 밀던 방향(currentWindDir)으로 튕겨나감
        Vector3 popOutVelocity = (currentWindDir * 12f) + (Vector3.up * 4f);
        ExternalLaunch(popOutVelocity);

        // 누운 상태(Paper)로 펄럭이며 착지하도록 상태 강제 전환
        paperState = true;

        // 튀어나올 때 놀란 표정으로 변경
        if (faceStance != null) faceStance.SetSurprizeFace();
    }

    private void JumpOutFromRoll()
    {
        // 1. 구르기 상태 해제 및 쿨타임 적용 (바닥에 닿자마자 0.1초만에 말리는 것 방지)
        isWindRolling = false;
        windRollCooldownTimer = 0.5f; // 착지 시 부드럽게 다시 말리도록 짧은 쿨타임 부여

        // 2. 비주얼 원상 복구 (몸이 펴짐)
        if (characterVisual != null) characterVisual.gameObject.SetActive(true);
        if (rollingVisualPrefab != null) rollingVisualPrefab.SetActive(false);

        if (rollingAnim != null)
        {
            rollingAnim.SetBool("IsRolling", false);
            rollingAnim.SetBool("IsReverseRolling", false);
        }

        // 3. 점프 물리 적용
        isGrounded = false;
        isOnPlatform = false;
        groundCheckTimer = 0f;
        platformCheckTimer = 0f;

        verticalVelocity = windRollJumpSpeed;
        rb.position += Vector3.up * 0.1f;

        // 4. 공중에서 얇아지기(Slim) 등의 다른 스킬을 즉각 취할 수 있도록 기본 상태로 초기화
        paperState = false;
        slimState = false;
        braceState = false;
    }

    public void SetAbsolutePaperState(bool isPaper)
    {
        paperState = isPaper;
    }
}