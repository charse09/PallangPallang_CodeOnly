//using System.Collections;
//using Unity.VisualScripting;
//using UnityEngine;

//[RequireComponent(typeof(Rigidbody))]
//public class PlayerLocomotion : MonoBehaviour
//{
//    [Header("Visuals System")]
//    [Tooltip("분리된 시각 관리 스크립트를 연결해주세요")]
//    [SerializeField] private PlayerVisuals visuals;

//    [Header("Respawn State")]
//    public bool isRespawning = false;

//    private bool isInputLocked = false;
//    private Coroutine launchLockCoroutine;

//    [Header("Components")]
//    [SerializeField] private Rigidbody rb;
//    [SerializeField] private BoxCollider bodyCollider;
//    [SerializeField] private Camera cam1;

//    [Header("Ground Check Colliders")]
//    [SerializeField] private CapsuleCollider feetGroundCollider;
//    [SerializeField] private BoxCollider backGroundCollider;

//    [Header("Movement Settings")]
//    [SerializeField] private float slimStateSpeed = 4.0f;
//    [SerializeField] private float moveSpeed = 16f;
//    [SerializeField] private float jumpSpeed = 15f;
//    [SerializeField] private float jumpDelay = 0.15f;
//    [SerializeField] private float gravity = -30f;
//    [SerializeField] private float paperGravity = -5f;
//    [SerializeField] private float groundSharpness = 15f;
//    [SerializeField] private float airSharpness = 2f;
//    [SerializeField] private float airSpeedMultiplier = 0.5f;
//    [SerializeField] private LayerMask groundLayer;

//    [Header("Brace Settings")]
//    [SerializeField] private bool useBraceTimeLimit = true;
//    [SerializeField] private float maxBraceTime = 2.0f;
//    [SerializeField] private float normalRecoveryDelay = 1.0f;
//    [SerializeField] private float exhaustedRecoveryDelay = 3.0f;
//    [SerializeField] private float braceRecoverySpeed = 2.0f;

//    [Header("Rail System")]
//    public bool isRidingRail = false;
//    [Header("Wind Zone Restrictions")]
//    public bool isInWindZone = false;

//    [Header("Slide Zone Settings")]
//    [SerializeField] private bool isInsideSlideZone = false;
//    private Vector3 fixedSlideDirection = Vector3.forward;
//    private Vector3 fixedSlideRight = Vector3.right;
//    private float slideZoneForwardSpeed = 16f;
//    private float slideZoneSharpness = 1.5f;

//    private float braceTimer = 0f;
//    private bool braceExhausted = false;
//    private float currentRecoveryWaitTimer = 0f;

//    [Header("Slide Settings")]
//    [SerializeField] private float slideSpeed = 35f;
//    [SerializeField] private float slideDuration = 1.0f;
//    [SerializeField] private float slideCooldown = 3.0f;

//    private bool isSliding = false;
//    private float currentSlideTimer = 0f;
//    private float slideCooldownTimer = 0f;
//    private Vector3 slideDirection;
//    private bool isSlideFalling = false;

//    [Header("Player State")]
//    [SerializeField] private bool paperReadyState = false;
//    [SerializeField] private bool paperState = false;
//    [SerializeField] private bool slimState = false;
//    [SerializeField] private bool slimCompletelyState = false;
//    [SerializeField] private bool isGrounded;
//    [SerializeField] private bool braceState = false;

//    private bool isJumpingPending = false;

//    [Header("Paper Move Zone")]
//    public bool canMoveInPaperZone = false;
//    public float currentPaperZoneSpeed = 30f;
//    public float currentWiggleFrequency = 15f;
//    public float currentWiggleAmplitude = 1.5f;
//    public Vector3 wigglePivotOffset = new Vector3(0f, 1f, 0f);

//    private Vector3 savedBaseLocalPos;
//    private Quaternion savedBaseLocalRot;
//    private bool wasTiltedLastFrame = false;

//    private bool lastPaperState = false;
//    private bool lastSlimState = false;

//    [Header("Trigger")]
//    [SerializeField] private Collider slimTriggerCollider;

//    [Header("External Physics")]
//    private Vector3 externalHorizontalVelocity;
//    [SerializeField] private float externalForceDamping = 5f;
//    private float windBufferTimer = 0f;

//    [Header("Rope Walk Settings")]
//    public bool isRidingRope = false;
//    private float ropeWalkSpeed = 1.5f;
//    private RopeBalanceGimmick currentRopeGimmick;

//    [Header("Condition Modifiers (FSM)")]
//    public float speedModifier = 1.0f;
//    public float jumpModifier = 1.0f;
//    public float slideDurationModifier = 1.0f;
//    public bool isWet2Restricted = false;

//    [Header("Wind Roll Settings")]
//    [SerializeField] private float windRollCooldown = 1.0f;
//    [SerializeField] private float windRollJumpSpeed = 12f;
//    [SerializeField] private float railSwitchSpeed = 15f;

//    [Header("Ground Probe & Coyote Settings")]
//    [SerializeField] private float probeRayLength = 0.6f;
//    [SerializeField] private float snapDistance = 0.35f;
//    [Tooltip("절벽에서 벗어난 후 점프 입력이 유효한 시간")]
//    [SerializeField] private float coyoteTime = 0.15f;
//    private float coyoteTimer = 0f;

//    [Header("Rough Platform Attach System")]
//    public bool isAttachedToRoughPlatform = false;

//    private Vector3 inputMoveDir;
//    private Vector3 lerpMoveDir;
//    private float verticalVelocity;

//    private Vector3 currentGroundNormal = Vector3.up;

//    private Rigidbody currentPlatformRB;
//    private bool isOnPlatform = false;
//    private float groundCheckTimer = 0f;
//    private float platformCheckTimer = 0f;

//    private Rigidbody lastPlatformRB;
//    private Vector3 prevPlatformPos;
//    private Quaternion prevPlatformRot;
//    private Vector3 platformVelocity;

//    private Quaternion targetBaseRotation = Quaternion.identity;

//    public bool isWindRolling = false;
//    private Vector3 currentWindDir;
//    private float windRollCooldownTimer = 0f;
//    [HideInInspector] public bool canJumpInWindRoll = false;

//    private int currentRailIndex = 0;
//    private int minRailIndex = -1;
//    private int maxRailIndex = 1;
//    private float currentRailDistance = 3f;

//    private Vector3 windRollAnchorPosition;
//    private float targetRailOffset = 0f;
//    private float currentRailOffset = 0f;

//    private bool isInputReset = true;
//    private bool isForcedCrouch = false;

//    private float jumpCooldownTimer = 0f;

//    void Awake()
//    {
//        rb = GetComponent<Rigidbody>();
//        if (bodyCollider == null) bodyCollider = GetComponent<BoxCollider>();

//        rb.useGravity = false;
//        rb.freezeRotation = true;
//        rb.interpolation = RigidbodyInterpolation.Interpolate;

//        PhysicsMaterial zeroFrictionMat = new PhysicsMaterial("PlayerZeroFriction")
//        {
//            dynamicFriction = 0f,
//            staticFriction = 0f,
//            frictionCombine = PhysicsMaterialCombine.Minimum,
//            bounceCombine = PhysicsMaterialCombine.Minimum
//        };

//        if (bodyCollider) bodyCollider.material = zeroFrictionMat;
//        if (feetGroundCollider) feetGroundCollider.material = zeroFrictionMat;
//        if (backGroundCollider) backGroundCollider.material = zeroFrictionMat;

//        targetBaseRotation = transform.rotation;
//        if (visuals == null) Debug.LogWarning("PlayerLocomotion에 PlayerVisuals가 연결되지 않았습니다.");

//        if (GetComponent<PlayerTelekinesisController>() == null)
//        {
//            gameObject.AddComponent<PlayerTelekinesisController>();
//        }
//    }

//    public void ApplyConditionModifiers(float speedMod, float jumpMod, float slideMod, bool restricted)
//    {
//        speedModifier = speedMod;
//        jumpModifier = jumpMod;
//        slideDurationModifier = slideMod;
//        isWet2Restricted = restricted;
//    }

//    public void CancelSpecialStatesForCondition()
//    {
//        if (paperState) paperState = false;
//        if (slimState) slimState = false;
//        braceState = false;
//        isSliding = false;
//        slimCompletelyState = false;
//    }

//    public void UpdatePlayerVisuals(PlayerConditionState state)
//    {
//        if (visuals) visuals.ChangeColorByState(state);
//    }

//    private void OnEnable()
//    {
//        if (rb != null)
//        {
//            rb.linearVelocity = Vector3.zero;
//            rb.angularVelocity = Vector3.zero;
//            rb.Sleep();
//        }

//        verticalVelocity = 0f;
//        inputMoveDir = Vector3.zero;
//        lerpMoveDir = Vector3.zero;

//        isSliding = false;
//        isGrounded = false;
//        isSlideFalling = false;
//        isInputLocked = false;
//        isJumpingPending = false;
//        slideCooldownTimer = 0f;

//        currentPlatformRB = null;
//        isOnPlatform = false;
//        groundCheckTimer = 0f;
//        platformCheckTimer = 0f;

//        lastPlatformRB = null;
//        prevPlatformPos = Vector3.zero;
//        prevPlatformRot = Quaternion.identity;
//        platformVelocity = Vector3.zero;

//        lastPaperState = paperState;
//        lastSlimState = slimState;

//        UpdateGroundColliders();
//    }

//    void Update()
//    {
//        if (isRidingRail || isRespawning || isAttachedToRoughPlatform) return;

//        if (slideCooldownTimer > 0f) slideCooldownTimer -= Time.deltaTime;
//        if (windRollCooldownTimer > 0f) windRollCooldownTimer -= Time.deltaTime;
//        if (jumpCooldownTimer > 0f) jumpCooldownTimer -= Time.deltaTime;
//        if (coyoteTimer > 0f) coyoteTimer -= Time.deltaTime;

//        if (!isInputLocked) HandleInputs();
//        else inputMoveDir = Vector3.zero;

//        if (paperState != lastPaperState) { lastPaperState = paperState; OnPaperStateChanged(); }
//        if (slimState != lastSlimState) { lastSlimState = slimState; OnSlimStateChanged(); }

//        if (visuals)
//        {
//            visuals.UpdateStates(paperState, slimState, slimCompletelyState, isWindRolling);
//            visuals.UpdateFace(braceState, isSliding, isGrounded, rb.linearVelocity);
//        }
//    }

//    private void UpdateGroundNormalRaycast()
//    {
//        if (!isGrounded)
//        {
//            currentGroundNormal = Vector3.up;
//            return;
//        }

//        float rayStartOffset = paperState ? 0.5f : 0.1f;
//        Vector3 rayOrigin = transform.position + Vector3.up * rayStartOffset;

//        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 0.6f, groundLayer, QueryTriggerInteraction.Ignore))
//        {
//            if (hit.collider.transform.IsChildOf(transform)) return;

//            if (Vector3.Angle(Vector3.up, hit.normal) <= 45f)
//            {
//                Vector3 targetNormal = hit.normal;
//                if (Vector3.Angle(Vector3.up, targetNormal) < 3f) targetNormal = Vector3.up;
//                currentGroundNormal = targetNormal;
//            }
//        }
//    }

//    private void HandleRootPhysicsRotation()
//    {
//        if (isRidingRail || isRespawning) return;

//        Vector3 lookDir = inputMoveDir;
//        if (isInsideSlideZone) lookDir = fixedSlideDirection;
//        else if (isSliding) lookDir = slideDirection;
//        else if (isWindRolling) lookDir = currentWindDir;

//        if (lookDir.sqrMagnitude > 0.01f)
//        {
//            targetBaseRotation = Quaternion.LookRotation(lookDir.normalized);
//        }

//        Quaternion stateOffset = Quaternion.identity;
//        if (paperState) stateOffset = Quaternion.Euler(-90f, 0f, 0f);
//        else if (slimState) stateOffset = Quaternion.Euler(0f, 90f, 0f);

//        Quaternion finalTargetRot = targetBaseRotation * stateOffset;

//        rb.MoveRotation(Quaternion.Slerp(rb.rotation, finalTargetRot, Time.fixedDeltaTime * 15f));
//    }

//    void FixedUpdate()
//    {
//        if (isRidingRail || isRespawning || isAttachedToRoughPlatform) return;

//        ProcessGroundProbe();

//        if (groundCheckTimer > 0f) groundCheckTimer -= Time.fixedDeltaTime;
//        if (platformCheckTimer > 0f) platformCheckTimer -= Time.fixedDeltaTime;

//        isGrounded = groundCheckTimer > 0f;
//        isOnPlatform = platformCheckTimer > 0f;

//        지면에 딛고 있는 동안에는 항상 코요테 타임을 충전
//        if (isGrounded)
//        {
//            coyoteTimer = coyoteTime;
//        }

//        UpdateGroundNormalRaycast();
//        HandleRootPhysicsRotation();

//        if (!isOnPlatform) currentPlatformRB = null;

//        if (isOnPlatform && currentPlatformRB != null)
//        {
//            if (currentPlatformRB == lastPlatformRB)
//            {
//                Quaternion platformDeltaRot = currentPlatformRB.rotation * Quaternion.Inverse(prevPlatformRot);

//                Vector3 relPos = rb.position - prevPlatformPos;
//                Vector3 rotatedRelPos = platformDeltaRot * relPos;
//                Vector3 targetPos = currentPlatformRB.position + rotatedRelPos;

//                platformVelocity = (targetPos - rb.position) / Time.fixedDeltaTime;

//                if (inputMoveDir.sqrMagnitude <= 0.01f)
//                {
//                    Vector3 EulerDelta = platformDeltaRot.eulerAngles;
//                    float deltaY = EulerDelta.y;
//                    if (deltaY > 180f) deltaY -= 360f;

//                    if (Mathf.Abs(deltaY) > 0.001f)
//                        targetBaseRotation = Quaternion.Euler(0, deltaY, 0) * targetBaseRotation;
//                }
//            }
//            else
//            {
//                platformVelocity = currentPlatformRB.GetPointVelocity(rb.position);
//            }

//            lastPlatformRB = currentPlatformRB;
//            prevPlatformPos = currentPlatformRB.position;
//            prevPlatformRot = currentPlatformRB.rotation;
//        }
//        else
//        {
//            platformVelocity = Vector3.zero;
//            lastPlatformRB = null;
//        }

//        ApplyCustomGravity();
//        CalculateMovement();
//    }

//    void LateUpdate()
//    {
//        if (isRidingRail || isAttachedToRoughPlatform) return;

//        if (visuals)
//        {
//            if (wasTiltedLastFrame)
//            {
//                visuals.transform.localRotation = savedBaseLocalRot;
//                wasTiltedLastFrame = false;
//            }

//            visuals.HandleVisualsLateUpdate(Time.deltaTime, isGrounded, isRidingRail);

//            if (paperState && canMoveInPaperZone && inputMoveDir.sqrMagnitude > 0.01f)
//            {
//                savedBaseLocalRot = visuals.transform.localRotation;
//                wasTiltedLastFrame = true;

//                float tiltAngle = Mathf.Sin(Time.time * currentWiggleFrequency) * currentWiggleAmplitude;
//                Quaternion tiltRot = Quaternion.AngleAxis(tiltAngle, Vector3.forward);

//                visuals.transform.localRotation = savedBaseLocalRot * tiltRot;
//            }
//        }
//    }

//    private void OnPaperStateChanged()
//    {
//        UpdateGroundColliders();

//        verticalVelocity = 0f;
//        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

//        if (!paperState) isSliding = false;
//    }

//    private void OnSlimStateChanged() { }

//    private void UpdateGroundColliders()
//    {
//        if (backGroundCollider != null) backGroundCollider.enabled = paperState;
//        if (feetGroundCollider != null) feetGroundCollider.enabled = !paperState;
//    }

//    private void EvaluateGroundCollision(Collision collision)
//    {
//        if (jumpCooldownTimer > 0f) return;

//        bool isCurrentPlatform = currentPlatformRB != null && collision.rigidbody == currentPlatformRB;
//        if (((1 << collision.gameObject.layer) & groundLayer) != 0 || isCurrentPlatform)
//        {
//            Vector3 bestNormal = Vector3.up;
//            float maxUpDot = -1f;
//            bool foundGround = false;
//            Rigidbody hitPlatform = null;

//            for (int i = 0; i < collision.contactCount; i++)
//            {
//                ContactPoint contact = collision.GetContact(i);
//                if (contact.thisCollider == feetGroundCollider || contact.thisCollider == backGroundCollider)
//                {
//                    float upDot = Vector3.Dot(Vector3.up, contact.normal);
//                    if (upDot > maxUpDot && upDot >= 0.707f)
//                    {
//                        maxUpDot = upDot;
//                        bestNormal = contact.normal;

//                        if (Vector3.Angle(Vector3.up, bestNormal) < 3f) bestNormal = Vector3.up;

//                        foundGround = true;
//                        hitPlatform = contact.otherCollider.attachedRigidbody;
//                    }
//                }
//            }

//            if (foundGround)
//            {
//                groundCheckTimer = 0.05f; // 지면 버퍼 시간 축소 (0.25f -> 0.05f)
//                isGrounded = true;
//                isSlideFalling = false;
//                currentGroundNormal = bestNormal;

//                if (hitPlatform != null)
//                {
//                    currentPlatformRB = hitPlatform;
//                    platformCheckTimer = 0.05f;
//                    isOnPlatform = true;
//                }
//            }
//        }
//    }

//    private void OnCollisionEnter(Collision collision) => EvaluateGroundCollision(collision);
//    private void OnCollisionStay(Collision collision) => EvaluateGroundCollision(collision);

//    public void AddContinuousForce(Vector3 force)
//    {
//        windBufferTimer = 0.5f;
//        externalHorizontalVelocity += new Vector3(force.x, 0f, force.z);
//        if (Mathf.Abs(force.y) > 0.1f)
//        {
//            if (force.y > 0 && isGrounded)
//            {
//                isGrounded = false;
//                groundCheckTimer = 0f;
//                rb.position += Vector3.up * 0.1f;
//            }
//            verticalVelocity += force.y;
//        }
//    }

//    public void ExternalLaunch(Vector3 launchVelocity)
//    {
//        verticalVelocity = launchVelocity.y;
//        externalHorizontalVelocity = new Vector3(launchVelocity.x, 0, launchVelocity.z);

//        isGrounded = false;
//        isOnPlatform = false;
//        groundCheckTimer = 0f;
//        platformCheckTimer = 0f;
//        isSliding = false;

//        windBufferTimer = 0.5f;

//        if (launchVelocity.y > 0) rb.position += Vector3.up * 0.2f;
//        if (rb != null) rb.linearVelocity = launchVelocity;
//    }

//    public void LaunchPlayerToPointAndLockInput(Vector3 launchVelocity, Vector3 targetResetPosition)
//    {
//        isGrounded = false;
//        isOnPlatform = false;
//        groundCheckTimer = 0f;
//        platformCheckTimer = 0f;
//        isSliding = false;

//        windBufferTimer = 10f;
//        verticalVelocity = launchVelocity.y;
//        externalHorizontalVelocity = new Vector3(launchVelocity.x, 0f, launchVelocity.z);

//        if (rb != null)
//        {
//            rb.isKinematic = false;
//            rb.WakeUp();
//            rb.linearVelocity = launchVelocity;
//        }

//        isInputLocked = true;
//        paperState = false;

//        if (launchLockCoroutine != null) StopCoroutine(launchLockCoroutine);
//        launchLockCoroutine = StartCoroutine(WaitForTargetPointAndSetPaperRoutine(targetResetPosition));
//    }

//    private IEnumerator WaitForTargetPointAndSetPaperRoutine(Vector3 targetPosition)
//    {
//        float timer = 0f;
//        float maxTimeout = 3.0f;
//        float reachDistance = 4.0f;

//        yield return new WaitForSeconds(0.1f);

//        while (timer < maxTimeout)
//        {
//            timer += Time.deltaTime;
//            windBufferTimer = 2.0f;

//            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
//            if (distanceToTarget <= reachDistance) break;

//            yield return null;
//        }

//        windBufferTimer = 0f;
//        externalHorizontalVelocity = Vector3.zero;

//        isInputLocked = false;
//        SetAbsolutePaperState(true);
//    }

//    private void ApplyExternalForceDamping()
//    {
//        if (windBufferTimer > 0f) windBufferTimer -= Time.fixedDeltaTime;
//        float currentDamping = (windBufferTimer > 0f) ? externalForceDamping : externalForceDamping * 3f;
//        externalHorizontalVelocity = Vector3.Lerp(externalHorizontalVelocity, Vector3.zero, Time.fixedDeltaTime * currentDamping);
//    }

//    private IEnumerator JumpRoutine(float jumpPower)
//    {
//        isJumpingPending = true;

//        if (visuals) visuals.SetJumpAnim();

//        yield return new WaitForSeconds(jumpDelay);

//        isGrounded = false;
//        coyoteTimer = 0f; // 점프 시 코요테 타임 소모
//        isOnPlatform = false;
//        groundCheckTimer = 0f;
//        platformCheckTimer = 0f;

//        jumpCooldownTimer = 0.25f;

//        if (currentPlatformRB != null)
//        {
//            Vector3 platformVel = currentPlatformRB.GetPointVelocity(rb.position);
//            externalHorizontalVelocity += new Vector3(platformVel.x, 0, platformVel.z);
//        }

//        verticalVelocity = jumpPower;
//        rb.position += Vector3.up * 0.1f;
//        isSliding = false;

//        isJumpingPending = false;
//    }

//    void HandleInputs()
//    {
//        Vector2 input = InputManager.Instance.Movement;
//        bool braceInput = InputManager.Instance.CrouchHeld;

//        if (isRidingRope)
//        {
//            Vector3 ropeForward = cam1.transform.forward;
//            ropeForward.y = 0; ropeForward.Normalize();
//            float yInput = input.y > 0f ? input.y : 0f;
//            inputMoveDir = ropeForward * yInput;
//            return;
//        }

//        if (isWindRolling)
//        {
//            inputMoveDir = currentWindDir;
//            SetBaseRotation(currentWindDir);

//            if (Mathf.Abs(input.x) > 0.5f)
//            {
//                if (isInputReset)
//                {
//                    if (input.x < -0.5f && currentRailIndex > minRailIndex) currentRailIndex--;
//                    else if (input.x > 0.5f && currentRailIndex < maxRailIndex) currentRailIndex++;
//                    isInputReset = false;
//                }
//            }
//            else isInputReset = true;

//            targetRailOffset = currentRailIndex * currentRailDistance;

//            if (canJumpInWindRoll && InputManager.Instance.JumpPressed) JumpOutFromRoll();
//            return;
//        }

//        bool canJump = isGrounded || coyoteTimer > 0f; // 코요테 타임 적용

//        if (isInsideSlideZone)
//        {
//            float currentInputX = paperState ? 0f : input.x;
//            inputMoveDir = (fixedSlideDirection + fixedSlideRight * currentInputX).normalized;
//            braceState = false;

//            if (InputManager.Instance.JumpPressed && canJump && !slimState && !paperState && !isJumpingPending)
//            {
//                coyoteTimer = 0f;
//                StartCoroutine(JumpRoutine(jumpSpeed));
//            }

//            if (InputManager.Instance.PaperStateKeyTrigger && !slimState && !braceState)
//            {
//                if (!(!isGrounded && paperState)) paperState = !paperState;
//            }

//            if (visuals)
//            {
//                bool isWalking = isGrounded && !isJumpingPending && (inputMoveDir.sqrMagnitude > 0.01f);
//                visuals.SetWalkAnim(isWalking);
//                visuals.SetCrouchAnim(braceState);
//            }
//            return;
//        }

//        Vector3 forward = cam1.transform.forward;
//        Vector3 right = cam1.transform.right;
//        forward.y = 0; right.y = 0; forward.Normalize(); right.Normalize();

//        inputMoveDir = (forward * input.y + right * input.x);

//        if (isForcedCrouch)
//        {
//            braceState = true;
//            if (visuals) visuals.SetCrouchAnim(true);
//        }
//        else
//        {
//            if (braceInput && isGrounded && !slimState && !paperState && !braceExhausted && !isWet2Restricted)
//            {
//                braceState = true;
//                if (useBraceTimeLimit)
//                {
//                    if (windBufferTimer > 0f) braceTimer += Time.deltaTime;
//                    currentRecoveryWaitTimer = normalRecoveryDelay;
//                    if (braceTimer >= maxBraceTime) { braceState = false; braceExhausted = true; currentRecoveryWaitTimer = exhaustedRecoveryDelay; }
//                }
//            }
//            else
//            {
//                braceState = false;
//                if (useBraceTimeLimit && !braceInput)
//                {
//                    if (currentRecoveryWaitTimer > 0f) currentRecoveryWaitTimer -= Time.deltaTime;
//                    else if (braceTimer > 0f) { braceTimer -= Time.deltaTime * braceRecoverySpeed; if (braceTimer <= 0f) { braceTimer = 0f; braceExhausted = false; } }
//                }
//            }
//        }

//        if (braceState || (paperState && isGrounded && !isSliding && !canMoveInPaperZone))
//            inputMoveDir = Vector3.zero;

//        if (InputManager.Instance.JumpPressed && canJump && !slimState && !paperState && !isWet2Restricted && !isInWindZone && !isJumpingPending)
//        {
//            coyoteTimer = 0f;
//            StartCoroutine(JumpRoutine(jumpSpeed * jumpModifier));
//        }

//        if (InputManager.Instance.PaperStateKeyTrigger && !slimState && !braceState && !isWet2Restricted && !isInWindZone)
//        {
//            if (!(!isGrounded && paperState)) paperState = !paperState;
//        }

//        [수정] 하드코딩된 LShift 대신 InputManager의 SlideKeyTrigger를 사용하도록 변경
//        if (InputManager.Instance.SlideKeyTrigger && isGrounded && !slimState && !braceState && !isWet2Restricted && !isInWindZone)
//        {
//            if (paperState)
//            {
//                paperState = false;
//                isSliding = false;
//            }
//            else if (slideCooldownTimer <= 0f)
//            {
//                paperState = true;
//                if (inputMoveDir.sqrMagnitude > 0.1f)
//                {
//                    isSliding = true;
//                    currentSlideTimer = slideDuration * slideDurationModifier;
//                    slideDirection = inputMoveDir.normalized;
//                    slideCooldownTimer = slideCooldown;
//                }
//            }
//        }

//        if (InputManager.Instance.SlimStateKeyTrigger && !paperState && !braceState && !isWet2Restricted && !isInWindZone)
//        {
//            slimState = !slimState;
//        }

//        if (visuals)
//        {
//            bool isWalking = isGrounded && !isJumpingPending && (inputMoveDir.sqrMagnitude > 0.01f);
//            visuals.SetWalkAnim(isWalking);
//            visuals.SetCrouchAnim(braceState);
//        }
//    }

//    void ApplyCustomGravity()
//    {
//        [추가] paperState일 때 수직 상승 속도(+Y)가 있다면 0으로 깎음
//        if (paperState && verticalVelocity > 0f)
//        {
//            verticalVelocity = 0f;
//        }

//        if (isGrounded && !isSlideFalling && !isJumpingPending)
//        {
//            if (verticalVelocity < 0f) verticalVelocity = -1f;
//            return;
//        }

//        if (!isGrounded || isSlideFalling)
//        {
//            float targetGravity = (paperState && !isSlideFalling) ? paperGravity : gravity;
//            verticalVelocity += targetGravity * Time.fixedDeltaTime;

//            float maxFallSpeed = (paperState && !isSlideFalling) ? paperGravity : gravity;
//            if (verticalVelocity < maxFallSpeed)
//                verticalVelocity = Mathf.Lerp(verticalVelocity, maxFallSpeed, Time.fixedDeltaTime * 6f);
//        }
//    }

//    void CalculateMovement()
//    {
//        ApplyExternalForceDamping();

//        if (isSliding)
//        {
//            currentSlideTimer -= Time.fixedDeltaTime;
//            if (currentSlideTimer <= 0f || !isGrounded || !paperState)
//            {
//                if (!isGrounded && paperState) isSlideFalling = true;
//                isSliding = false;

//                float endRatio = Mathf.Max(0f, currentSlideTimer / slideDuration);
//                float endSpeed = Mathf.SmoothStep(0f, slideSpeed, endRatio);
//                float nextTargetSpeed = !isGrounded ? moveSpeed * airSpeedMultiplier : moveSpeed;
//                float baseSpeed = Mathf.Max(nextTargetSpeed, 0.1f);
//                lerpMoveDir = slideDirection * (endSpeed / baseSpeed);
//            }
//        }

//        Vector3 movementVelocity = Vector3.zero;

//        if (isSliding)
//        {
//            float speedRatio = currentSlideTimer / slideDuration;
//            float currentSpeed = Mathf.SmoothStep(0f, slideSpeed, speedRatio);
//            float slopeAngle = Vector3.Angle(Vector3.up, currentGroundNormal);

//            if (slopeAngle > 0 && slopeAngle <= 45f) movementVelocity = Vector3.ProjectOnPlane(slideDirection, currentGroundNormal).normalized * currentSpeed;
//            else movementVelocity = slideDirection * currentSpeed;

//            if (verticalVelocity <= 0 && !isOnPlatform) verticalVelocity = -1f;
//        }
//        else if (isWindRolling)
//        {
//            currentRailOffset = Mathf.MoveTowards(currentRailOffset, targetRailOffset, railSwitchSpeed * Time.fixedDeltaTime);
//            Vector3 windRight = Vector3.Cross(Vector3.up, currentWindDir).normalized;
//            Vector3 targetLinePos = windRollAnchorPosition + (windRight * currentRailOffset);
//            Vector3 toTargetLine = targetLinePos - rb.position;
//            toTargetLine = Vector3.ProjectOnPlane(toTargetLine, Vector3.up);
//            toTargetLine = Vector3.ProjectOnPlane(toTargetLine, currentWindDir);
//            movementVelocity = toTargetLine / Time.fixedDeltaTime;
//        }
//        else
//        {
//            float targetSpeed = moveSpeed * speedModifier;
//            float currentSharpness = isGrounded ? groundSharpness : airSharpness;

//            if (isInsideSlideZone)
//            {
//                targetSpeed = slideZoneForwardSpeed;
//                currentSharpness = isGrounded ? slideZoneSharpness : airSharpness;
//            }
//            else if (isRidingRope)
//            {
//                targetSpeed = ropeWalkSpeed;
//                currentSharpness = groundSharpness;
//            }

//            if (paperState && canMoveInPaperZone)
//            {
//                targetSpeed = currentPaperZoneSpeed;
//                lerpMoveDir = inputMoveDir;
//            }
//            else
//            {
//                if (inputMoveDir.sqrMagnitude < 0.001f && isGrounded && !isSliding)
//                {
//                    lerpMoveDir = Vector3.zero;
//                }
//                else
//                {
//                    if (lerpMoveDir.magnitude > 1.2f) currentSharpness = isGrounded ? 3f : 0.5f;
//                    lerpMoveDir = Vector3.Lerp(lerpMoveDir, inputMoveDir, currentSharpness * Time.fixedDeltaTime);
//                }
//            }

//            if (!isGrounded) targetSpeed *= airSpeedMultiplier;

//            if (isGrounded)
//            {
//                if (lerpMoveDir.sqrMagnitude > 0.0001f)
//                {
//                    Vector3 projectedDir = Vector3.ProjectOnPlane(lerpMoveDir, currentGroundNormal).normalized;
//                    movementVelocity = projectedDir * (lerpMoveDir.magnitude * targetSpeed);
//                }
//                else movementVelocity = Vector3.zero;
//            }
//            else movementVelocity = lerpMoveDir * targetSpeed;

//            if (slimState) movementVelocity = lerpMoveDir * slimStateSpeed;
//        }

//        Vector3 finalVelocity = movementVelocity;
//        finalVelocity += braceState ? externalHorizontalVelocity * 0.0f : externalHorizontalVelocity;

//        if (isGrounded && !isSlideFalling)
//        {
//            bool hasExternalForce = externalHorizontalVelocity.sqrMagnitude > 0.01f;
//            bool hasMovement = inputMoveDir.sqrMagnitude > 0.01f || lerpMoveDir.sqrMagnitude > 0.01f || isSliding;

//            if (!hasMovement && !hasExternalForce)
//            {
//                finalVelocity = Vector3.zero;
//            }
//        }
//        else
//        {
//            finalVelocity.y += verticalVelocity;
//        }

//        if (isOnPlatform && currentPlatformRB != null)
//        {
//            finalVelocity += platformVelocity;
//        }

//        [추가] paperState일 때 Y축 속도가 양수(+Velocity)라면 0으로 제한(X, Z 수평 속도는 보존)
//        if (paperState)
//        {
//            if (verticalVelocity > 0f) verticalVelocity = 0f;
//            if (finalVelocity.y > 0f) finalVelocity.y = 0f;
//        }

//        rb.linearVelocity = finalVelocity;
//    }

//    public void EnableAndSetPaperState()
//    {
//        this.enabled = true;
//        isInputLocked = false;

//        isGrounded = false;
//        coyoteTimer = 0f;
//        isOnPlatform = false;
//        groundCheckTimer = 0f;
//        verticalVelocity = -2f;
//        externalHorizontalVelocity = Vector3.zero;

//        SetAbsolutePaperState(true);
//    }

//    public bool SlimCompletely
//    {
//        get { return slimCompletelyState; }
//        set
//        {
//            if (slimCompletelyState != value)
//            {
//                slimCompletelyState = value;
//                if (visuals) visuals.UpdateStates(paperState, slimState, slimCompletelyState, isWindRolling);
//            }
//        }
//    }

//    public bool PaperState
//    {
//        set
//        {
//            if (!isGrounded && paperState) return;
//            paperState = !paperState;
//        }
//    }

//    public float SlideCooldownTimer => slideCooldownTimer;
//    public float SlideCooldown => slideCooldown;

//    [추가] 외부 모듈(SetSpecificInputOutput 등)에서 슬라이드를 강제로 멈출 수 있도록 제공
//    public void StopSlide()
//    {
//        isSliding = false;
//    }

//    public void SetRailMode(bool isEntering, bool playAnim = true, bool resetStates = true)
//    {
//        isRidingRail = isEntering;

//        if (visuals && playAnim) visuals.SetHangOnAnim(isEntering);

//        if (isEntering)
//        {
//            if (resetStates)
//            {
//                paperState = false; lastPaperState = false;
//                slimState = false; lastSlimState = false;
//                braceState = false; isSliding = false; slimCompletelyState = false;

//                if (visuals) visuals.ResetRotationAndVisuals();
//            }

//            rb.isKinematic = true;
//            rb.linearVelocity = Vector3.zero;
//            rb.angularVelocity = Vector3.zero;

//            if (resetStates) UpdateGroundColliders();
//        }
//        else
//        {
//            rb.isKinematic = false;
//            verticalVelocity = 0f;
//            externalHorizontalVelocity = Vector3.zero;
//        }
//    }

//    public void EnterSlideZone(Vector3 direction, float speed, float sharpness)
//    {
//        isInsideSlideZone = true;
//        fixedSlideDirection = direction.normalized;
//        fixedSlideRight = Vector3.Cross(Vector3.up, fixedSlideDirection).normalized;
//        slideZoneForwardSpeed = speed;
//        slideZoneSharpness = sharpness;

//        paperState = false; slimState = false; isSliding = false; braceState = false;

//        if (visuals) visuals.StopRotationTransition();
//        UpdateGroundColliders();
//    }

//    public void ExitSlideZone() { isInsideSlideZone = false; }

//    public void SetPlatform(Rigidbody platformRB)
//    {
//        if (platformRB != null)
//        {
//            currentPlatformRB = platformRB; platformCheckTimer = 0.05f;
//            isOnPlatform = true; isGrounded = true; groundCheckTimer = 0.05f;
//            lastPlatformRB = platformRB; prevPlatformPos = platformRB.position;
//            prevPlatformRot = platformRB.rotation;

//            if (rb != null)
//            {
//                platformVelocity = platformRB.GetPointVelocity(rb.position);
//                rb.linearVelocity = Vector3.zero;
//            }
//            else platformVelocity = Vector3.zero;
//        }
//    }

//    public void SetRespawnMode(bool isEntering)
//    {
//        isRespawning = isEntering;

//        if (isEntering)
//        {
//            SetAbsolutePaperState(false);
//            SetSlimState(false);

//            canMoveInPaperZone = false;
//            braceState = false;
//            isSliding = false;
//            slimCompletelyState = false;

//            lastPaperState = false;
//            lastSlimState = false;

//            Fix for rail state persisting after death

//           isRidingRail = false;
//            if (rb != null)
//                {
//                    rb.isKinematic = false;
//                }

//            if (visuals)
//            {
//                visuals.StopRotationTransition();
//                visuals.ResetRotationAndVisuals();
//                visuals.UpdateStates(false, false, false, false);
//            }

//            rb.isKinematic = true;
//            rb.linearVelocity = Vector3.zero;
//            rb.angularVelocity = Vector3.zero;

//            UpdateGroundColliders();
//        }
//        else
//        {
//            rb.isKinematic = false;
//            verticalVelocity = 0f;
//            externalHorizontalVelocity = Vector3.zero;

//            if (visuals) visuals.SetNormalFace();
//        }
//    }

//    public void ForceRespawnAction() { if (visuals) visuals.SetSurpriseFace(); }

//    public void SetForceBraceState(bool isForced)
//    {
//        isForcedCrouch = isForced; braceState = isForced;
//        if (visuals) visuals.SetCrouchAnim(isForced);
//    }

//    public void SetBaseRotation(Vector3 lookForward)
//    {
//        lookForward.y = 0;
//        if (lookForward.sqrMagnitude > 0.01f) targetBaseRotation = Quaternion.LookRotation(lookForward.normalized);
//    }

//    private void ProcessGroundProbe()
//    {
//        if (isJumpingPending || jumpCooldownTimer > 0f || verticalVelocity > 0.1f || isRidingRail || isRespawning) return;

//        float rayStartOffset = paperState ? 0.5f : 0.1f;
//        Vector3 rayOrigin = transform.position + Vector3.up * rayStartOffset;

//        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, probeRayLength, groundLayer, QueryTriggerInteraction.Ignore))
//        {
//            if (hit.collider.transform.IsChildOf(transform)) return;

//            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);

//            if (slopeAngle <= 45f)
//            {
//                currentGroundNormal = hit.normal;

//                float distanceToGround = hit.distance - rayStartOffset;
//                bool hasMovement = inputMoveDir.sqrMagnitude > 0.01f || lerpMoveDir.sqrMagnitude > 0.01f || isSliding;

//                if (hasMovement && distanceToGround > 0.01f && distanceToGround <= snapDistance)
//                {
//                    Vector3 snappedPos = rb.position;
//                    snappedPos.y = hit.point.y;
//                    rb.MovePosition(snappedPos);
//                }

//                if (distanceToGround <= snapDistance)
//                {
//                    verticalVelocity = 0f;
//                    isGrounded = true;
//                    groundCheckTimer = 0.05f; // 지면 버퍼 시간 축소 (0.25f -> 0.05f)

//                    if (hit.rigidbody != null)
//                    {
//                        currentPlatformRB = hit.rigidbody;
//                        isOnPlatform = true;
//                        platformCheckTimer = 0.05f;
//                    }
//                }
//            }
//        }
//        else
//        {
//            currentGroundNormal = Vector3.up;
//        }
//    }

//    public void SetRoughPlatformAttachMode(bool isAttaching, Transform platformTransform = null)
//    {
//        isAttachedToRoughPlatform = isAttaching;

//        if (isAttaching && platformTransform != null)
//        {
//            1.물리 연산 및 충돌 감지 비활성화(튕김 현상 방지)
//            rb.isKinematic = true;
//            rb.detectCollisions = false; // 충돌 연산 자체를 끔
//            rb.linearVelocity = Vector3.zero;
//            rb.angularVelocity = Vector3.zero;

//            2.플랫폼과 플레이어 콜라이더 간 충돌 무시
//            if (platformTransform.TryGetComponent<Collider>(out Collider platformCol))
//            {
//                if (bodyCollider) Physics.IgnoreCollision(bodyCollider, platformCol, true);
//                if (feetGroundCollider) Physics.IgnoreCollision(feetGroundCollider, platformCol, true);
//                if (backGroundCollider) Physics.IgnoreCollision(backGroundCollider, platformCol, true);
//            }

//            3.자식으로 설정
//            transform.SetParent(platformTransform);

//            verticalVelocity = 0f;
//            externalHorizontalVelocity = Vector3.zero;
//            isGrounded = true;
//        }
//        else
//        {
//            Transform prevPlatform = transform.parent;

//            transform.SetParent(null);
//            transform.localScale = Vector3.one;

//            1.물리 및 충돌 감지 재활성화
//            rb.detectCollisions = true;
//            rb.isKinematic = false;

//            2.충돌 무시 해제
//            if (prevPlatform != null && prevPlatform.TryGetComponent<Collider>(out Collider platformCol))
//            {
//                if (bodyCollider) Physics.IgnoreCollision(bodyCollider, platformCol, false);
//                if (feetGroundCollider) Physics.IgnoreCollision(feetGroundCollider, platformCol, false);
//                if (backGroundCollider) Physics.IgnoreCollision(backGroundCollider, platformCol, false);
//            }

//            3.관성 전달
//            if (prevPlatform != null && prevPlatform.TryGetComponent<Rigidbody>(out Rigidbody platformRB))
//            {
//                Vector3 exitVel = platformRB.GetPointVelocity(transform.position);
//                externalHorizontalVelocity = new Vector3(exitVel.x, 0f, exitVel.z);
//                verticalVelocity = exitVel.y;
//                windBufferTimer = 0.5f;
//                rb.linearVelocity = exitVel;
//            }
//        }
//    }

//    public bool EnterWindRollMode(Vector3 windDir, int zoneRailCount, float zoneRailDistance, Vector3 zoneOrigin)
//    {
//        if (isWindRolling || windRollCooldownTimer > 0f || !isGrounded) return false;

//        if (slimState || paperState)
//        {
//            slimState = false;
//            paperState = false;
//            lastSlimState = false;
//            lastPaperState = false;

//            if (visuals)
//            {
//                visuals.StopRotationTransition();
//                visuals.ResetRotationAndVisuals();
//                visuals.UpdateStates(false, false, false, true);
//            }
//        }

//        isWindRolling = true;
//        currentWindDir = windDir.normalized;

//        SetBaseRotation(currentWindDir);
//        rb.rotation = targetBaseRotation;

//        currentRailDistance = zoneRailDistance;
//        maxRailIndex = zoneRailCount / 2;
//        minRailIndex = -maxRailIndex;

//        windRollAnchorPosition = zoneOrigin;

//        Vector3 windRight = Vector3.Cross(Vector3.up, currentWindDir).normalized;
//        Vector3 offsetVector = transform.position - zoneOrigin;
//        float lateralOffset = Vector3.Dot(offsetVector, windRight);

//        int closestIndex = Mathf.RoundToInt(lateralOffset / currentRailDistance);
//        currentRailIndex = Mathf.Clamp(closestIndex, minRailIndex, maxRailIndex);

//        targetRailOffset = currentRailIndex * currentRailDistance;
//        currentRailOffset = lateralOffset;

//        isInputReset = true;

//        [수정] dot 계산 및 역방향 판정 제거 -> 롤링 여부만 넘김
//        if (visuals) visuals.SetWindRollAnim(true);

//        UpdateGroundColliders();
//        return true;
//    }

//    / <summary>
//    / 동일한 바람 구역 내에서 레일 개수만 변경할 때 호출(트리거 통과 시 권장)
//    / 예: newRailCount = 1 -> min:0, max:0이 되어 0번(중앙) 레일로 부드럽게 합류
//    / </summary>
//    public void ChangeRailCount(int newRailCount)
//    {
//        if (!isWindRolling) return;

//        maxRailIndex = newRailCount / 2;
//        minRailIndex = -maxRailIndex;

//        현재 레일이 새 범위(예: 0번)를 벗어나 있다면 안쪽으로 제한
//       currentRailIndex = Mathf.Clamp(currentRailIndex, minRailIndex, maxRailIndex);
//        targetRailOffset = currentRailIndex * currentRailDistance;
//    }



//    public void ExitWindRollMode(bool enterPaperState = true)
//    {
//        if (!isWindRolling) return;

//        isWindRolling = false;
//        canJumpInWindRoll = false; // [추가] 롤링 종료 시 플래그 리셋
//        windRollCooldownTimer = windRollCooldown;

//        if (visuals)
//        {
//            visuals.SetWindRollAnim(false);
//            if (enterPaperState) visuals.SetSurpriseFace();
//        }

//        if (enterPaperState)
//        {
//            [기존 동작] 바람 구역을 벗어날 때 튀어오르며 종이(누운 상태)로 전환
//            Vector3 popOutVelocity = (currentWindDir * 12f) + (Vector3.up * 4f);
//            ExternalLaunch(popOutVelocity);
//            paperState = true;
//        }
//        else
//        {
//            [추가된 분기] 눕지 않고 서 있는 상태로 바람 방향 관성을 유지하며 통과
//            paperState = false;
//            rb.linearVelocity = currentWindDir * moveSpeed;
//        }
//    }

//    private void JumpOutFromRoll()
//    {
//        isWindRolling = false;
//        canJumpInWindRoll = false; // [추가] 탈출 점프 시 플래그 리셋
//        windRollCooldownTimer = 0.5f;

//        if (visuals) visuals.SetWindRollAnim(false);

//        isGrounded = false;
//        coyoteTimer = 0f;
//        isOnPlatform = false;
//        groundCheckTimer = 0f;
//        platformCheckTimer = 0f;

//        Vector3 currentVel = rb.linearVelocity;
//        rb.linearVelocity = new Vector3(currentVel.x, windRollJumpSpeed, currentVel.z);
//        verticalVelocity = windRollJumpSpeed;
//        rb.position += Vector3.up * 0.1f;

//        paperState = false; slimState = false; braceState = false;
//    }

//    public void SetAbsolutePaperState(bool isPaper) { paperState = isPaper; }
//    public bool GetPaperState() => paperState;
//    public bool GetSlimState() => slimState;

//    public void SetSlimState(bool isSlim)
//    {
//        if (slimState != isSlim)
//        {
//            slimState = isSlim;

//            if (visuals) visuals.UpdateStates(paperState, slimState, slimCompletelyState, isWindRolling);
//            OnSlimStateChanged();
//        }
//    }

//    public void ResetAllVelocities()
//    {
//        externalHorizontalVelocity = Vector3.zero;
//        platformVelocity = Vector3.zero;
//        verticalVelocity = 0f;
//        inputMoveDir = Vector3.zero;
//        lerpMoveDir = Vector3.zero;

//        if (rb != null)
//        {
//            rb.linearVelocity = Vector3.zero;
//            rb.angularVelocity = Vector3.zero;
//        }
//    }

//    public void EnterRopeWalk(float speed, RopeBalanceGimmick gimmick)
//    {
//        isRidingRope = true;
//        ropeWalkSpeed = speed;
//        currentRopeGimmick = gimmick;

//        paperState = false;
//        slimState = false;
//        braceState = false;
//        isSliding = false;
//        if (launchLockCoroutine != null)
//        {
//            StopCoroutine(launchLockCoroutine);
//            launchLockCoroutine = null;
//        }
//        UpdateGroundColliders();
//    }

//     ==========================================
//     [추가] Spline Wind Roll 전용 제어 함수
//     ==========================================
//    public void StartSplineRoll()
//    {
//        1.기존 이동 로직 및 물리 연산 차단(스플라인 러너가 직접 좌표 제어)
//        SetRailMode(true, playAnim: false, resetStates: true);

//        2.WindRoll 상태 및 비주얼 활성화
//        isWindRolling = true;
//        if (visuals)
//        {
//            visuals.UpdateStates(false, false, false, true);
//            [수정] 1f 인자 제거
//            visuals.SetWindRollAnim(true);
//        }
//    }

//    public void StopSplineRoll(bool applyExternalLaunch = false, Vector3 exitVelocity = default)
//    {
//        isWindRolling = false;
//        if (visuals)
//        {
//            visuals.SetWindRollAnim(false);
//        }

//        SetRailMode(false);

//        if (applyExternalLaunch)
//        {
//            ExternalLaunch(exitVelocity);
//        }
//    }

//    public void ExitRopeWalk()
//    {
//        isRidingRope = false;
//        currentRopeGimmick = null;
//    }

//    public void SetRopeTilt(float angle)
//    {
//        if (visuals != null) visuals.SetRopeTilt(angle);
//    }

//    public bool IsGrounded => isGrounded;
//    public bool IsSliding => isSliding;
//    public bool IsBracing => braceState;

//    PlayerLocomotion.cs 하단 프로퍼티 구역
//    public bool HasMoveInput => inputMoveDir.sqrMagnitude > 0.01f;
//}

using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerLocomotion : MonoBehaviour
{
    [Header("Visuals System")]
    [Tooltip("분리된 시각 관리 스크립트를 연결해주세요")]
    [SerializeField] private PlayerVisuals visuals;

    [Header("Paper State Target Settings")]
    [Tooltip("Paper 상태일 때 회전(눕기)할 대상 오브젝트 (지정하지 않으면 동작하지 않음)")]
    [SerializeField] private Transform paperTargetTransform;
    [SerializeField] private float paperRotationSpeed = 15f;
    private Quaternion initialPaperTargetLocalRot = Quaternion.identity;

    [Header("Respawn State")]
    public bool isRespawning = false;

    private bool isInputLocked = false;
    private Coroutine launchLockCoroutine;

    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private BoxCollider bodyCollider;
    [SerializeField] private Camera cam1;

    [Header("Ground Check Colliders")]
    [SerializeField] private CapsuleCollider feetGroundCollider;
    [SerializeField] private BoxCollider backGroundCollider;

    [Header("Movement Settings")]
    [SerializeField] private float slimStateSpeed = 4.0f;
    [SerializeField] private float moveSpeed = 16f;
    [SerializeField] private float jumpSpeed = 15f;
    [SerializeField] private float jumpDelay = 0.15f;
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
    [Header("Wind Zone Restrictions")]
    public bool isInWindZone = false;

    [Header("Slide Zone Settings")]
    [SerializeField] private bool isInsideSlideZone = false;
    private Vector3 fixedSlideDirection = Vector3.forward;
    private Vector3 fixedSlideRight = Vector3.right;
    private float slideZoneForwardSpeed = 16f;
    private float slideZoneSharpness = 1.5f;

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

    private bool isJumpingPending = false;

    [Header("Paper Move Zone")]
    public bool canMoveInPaperZone = false;
    public float currentPaperZoneSpeed = 30f;
    public float currentWiggleFrequency = 15f;
    public float currentWiggleAmplitude = 1.5f;
    public Vector3 wigglePivotOffset = new Vector3(0f, 1f, 0f);

    private Vector3 savedBaseLocalPos;
    private Quaternion savedBaseLocalRot;
    private bool wasTiltedLastFrame = false;

    private bool lastPaperState = false;
    private bool lastSlimState = false;

    [Header("Trigger")]
    [SerializeField] private Collider slimTriggerCollider;

    [Header("External Physics")]
    private Vector3 externalHorizontalVelocity;
    [SerializeField] private float externalForceDamping = 5f;
    private float windBufferTimer = 0f;

    [Header("Rope Walk Settings")]
    public bool isRidingRope = false;
    private float ropeWalkSpeed = 1.5f;
    private RopeBalanceGimmick currentRopeGimmick;

    [Header("Condition Modifiers (FSM)")]
    public float speedModifier = 1.0f;
    public float jumpModifier = 1.0f;
    public float slideDurationModifier = 1.0f;
    public bool isWet2Restricted = false;

    [Header("Wind Roll Settings")]
    [SerializeField] private float windRollCooldown = 1.0f;
    [SerializeField] private float windRollJumpSpeed = 12f;
    [SerializeField] private float railSwitchSpeed = 15f;
    private bool isWindRollJumpExited = false;

    [Header("Ground Probe & Coyote Settings")]
    [SerializeField] private float probeRayLength = 0.6f;
    [SerializeField] private float snapDistance = 0.35f;
    [Tooltip("절벽에서 벗어난 후 점프 입력이 유효한 시간")]
    [SerializeField] private float coyoteTime = 0.15f;
    private float coyoteTimer = 0f;

    [Header("Rough Platform Attach System")]
    public bool isAttachedToRoughPlatform = false;

    private Vector3 inputMoveDir;
    private Vector3 lerpMoveDir;
    private float verticalVelocity;

    private Vector3 currentGroundNormal = Vector3.up;

    private Rigidbody currentPlatformRB;
    private bool isOnPlatform = false;
    private float groundCheckTimer = 0f;
    private float platformCheckTimer = 0f;

    private Rigidbody lastPlatformRB;
    private Vector3 prevPlatformPos;
    private Quaternion prevPlatformRot;
    private Vector3 platformVelocity;

    private Quaternion targetBaseRotation = Quaternion.identity;

    public bool isWindRolling = false;
    private Vector3 currentWindDir;
    private float windRollCooldownTimer = 0f;
    [HideInInspector] public bool canJumpInWindRoll = false;

    private int currentRailIndex = 0;
    private int minRailIndex = -1;
    private int maxRailIndex = 1;
    private float currentRailDistance = 3f;

    private Vector3 windRollAnchorPosition;
    private float targetRailOffset = 0f;
    private float currentRailOffset = 0f;

    private bool isInputReset = true;
    private bool isForcedCrouch = false;

    private float jumpCooldownTimer = 0f;

    public event System.Action OnSlideStarted;
    public event System.Action OnSlideEnded;
    public event System.Action OnLieDownTriggered; // 지면에서 눕기(Paper 진입)
    public event System.Action OnGetUpTriggered;
    public event System.Action OnPallangStarted;
    public event System.Action OnRollingStarted; // ★ 롤링 시작 이벤트
    public event System.Action OnRollingEnded;   // ★ 롤링 종료 이벤트
    public event System.Action OnBraceStarted; // ★ 버티기 시작
    public event System.Action OnBraceEnded;   // ★ 버티기 종료
    public event System.Action OnHangStarted; // ★ 매달리기 시작 이벤트
    public event System.Action OnHangEnded;   // ★ 매달리기 종료 이벤트

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (bodyCollider == null) bodyCollider = GetComponent<BoxCollider>();

        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        PhysicsMaterial zeroFrictionMat = new PhysicsMaterial("PlayerZeroFriction")
        {
            dynamicFriction = 0f,
            staticFriction = 0f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };

        if (bodyCollider) bodyCollider.material = zeroFrictionMat;
        if (feetGroundCollider) feetGroundCollider.material = zeroFrictionMat;
        if (backGroundCollider) backGroundCollider.material = zeroFrictionMat;

        targetBaseRotation = transform.rotation;
        if (visuals == null) Debug.LogWarning("PlayerLocomotion에 PlayerVisuals가 연결되지 않았습니다.");

        // 지정된 타깃 트랜스폼의 기본 로컬 회전값 저장
        if (paperTargetTransform != null)
        {
            initialPaperTargetLocalRot = paperTargetTransform.localRotation;
        }
    }

    public void ApplyConditionModifiers(float speedMod, float jumpMod, float slideMod, bool restricted)
    {
        speedModifier = speedMod;
        jumpModifier = jumpMod;
        slideDurationModifier = slideMod;
        isWet2Restricted = restricted;
    }

    public void CancelSpecialStatesForCondition()
    {
        if (paperState) paperState = false;
        if (slimState) slimState = false;
        braceState = false;
        isSliding = false;
        slimCompletelyState = false;
    }

    public void UpdatePlayerVisuals(PlayerConditionState state)
    {
        if (visuals) visuals.ChangeColorByState(state);
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
        isInputLocked = false;
        isJumpingPending = false;
        slideCooldownTimer = 0f;

        currentPlatformRB = null;
        isOnPlatform = false;
        groundCheckTimer = 0f;
        platformCheckTimer = 0f;

        lastPlatformRB = null;
        prevPlatformPos = Vector3.zero;
        prevPlatformRot = Quaternion.identity;
        platformVelocity = Vector3.zero;

        lastPaperState = paperState;
        lastSlimState = slimState;

        isWindRollJumpExited = false;

        UpdateGroundColliders();
    }

    void Update()
    {
        if (isRidingRail || isRespawning || isAttachedToRoughPlatform) return;

        if (slideCooldownTimer > 0f) slideCooldownTimer -= Time.deltaTime;
        if (windRollCooldownTimer > 0f) windRollCooldownTimer -= Time.deltaTime;
        if (jumpCooldownTimer > 0f) jumpCooldownTimer -= Time.deltaTime;
        if (coyoteTimer > 0f) coyoteTimer -= Time.deltaTime;

        if (!isInputLocked) HandleInputs();
        else inputMoveDir = Vector3.zero;

        if (paperState != lastPaperState) { lastPaperState = paperState; OnPaperStateChanged(); }
        if (slimState != lastSlimState) { lastSlimState = slimState; OnSlimStateChanged(); }

        HandlePaperTargetRotation();

        if (visuals)
        {
            visuals.UpdateStates(paperState, slimState, slimCompletelyState, isWindRolling);
            visuals.UpdateFace(braceState, isSliding, isGrounded, rb.linearVelocity);
        }
    }

    private void HandlePaperTargetRotation()
    {
        if (paperTargetTransform == null) return;

        Quaternion targetRot = paperState
            ? initialPaperTargetLocalRot * Quaternion.Euler(-90f, 0f, 0f)
            : initialPaperTargetLocalRot;

        paperTargetTransform.localRotation = Quaternion.Slerp(
            paperTargetTransform.localRotation,
            targetRot,
            Time.deltaTime * paperRotationSpeed
        );
    }

    private void UpdateGroundNormalRaycast()
    {
        if (!isGrounded)
        {
            currentGroundNormal = Vector3.up;
            return;
        }

        float rayStartOffset = paperState ? 0.5f : 0.1f;
        Vector3 rayOrigin = transform.position + Vector3.up * rayStartOffset;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 0.6f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform)) return;

            if (Vector3.Angle(Vector3.up, hit.normal) <= 45f)
            {
                Vector3 targetNormal = hit.normal;
                if (Vector3.Angle(Vector3.up, targetNormal) < 3f) targetNormal = Vector3.up;
                currentGroundNormal = targetNormal;
            }
        }
    }

    private void HandleRootPhysicsRotation()
    {
        if (isRidingRail || isRespawning) return;

        Vector3 lookDir = inputMoveDir;
        if (isInsideSlideZone) lookDir = fixedSlideDirection;
        else if (isSliding) lookDir = slideDirection;
        else if (isWindRolling) lookDir = currentWindDir;

        if (lookDir.sqrMagnitude > 0.01f)
        {
            targetBaseRotation = Quaternion.LookRotation(lookDir.normalized);
        }

        Quaternion stateOffset = Quaternion.identity;
        // [수정] paperState는 지정 오브젝트가 회전하므로 루트 오프셋에서는 제외 (slimState만 유지)
        if (slimState) stateOffset = Quaternion.Euler(0f, 90f, 0f);

        Quaternion finalTargetRot = targetBaseRotation * stateOffset;

        rb.MoveRotation(Quaternion.Slerp(rb.rotation, finalTargetRot, Time.fixedDeltaTime * 15f));
    }

    void FixedUpdate()
    {
        if (isRidingRail || isRespawning || isAttachedToRoughPlatform) return;

        ProcessGroundProbe();

        if (groundCheckTimer > 0f) groundCheckTimer -= Time.fixedDeltaTime;
        if (platformCheckTimer > 0f) platformCheckTimer -= Time.fixedDeltaTime;

        isGrounded = groundCheckTimer > 0f;
        isOnPlatform = platformCheckTimer > 0f;

        if (isGrounded)
        {
            coyoteTimer = coyoteTime;

            if (isWindRollJumpExited && jumpCooldownTimer <= 0f)
            {
                isWindRollJumpExited = false;
            }
        }

        UpdateGroundNormalRaycast();
        HandleRootPhysicsRotation();

        if (!isOnPlatform) currentPlatformRB = null;

        if (isOnPlatform && currentPlatformRB != null)
        {
            if (currentPlatformRB == lastPlatformRB)
            {
                Quaternion platformDeltaRot = currentPlatformRB.rotation * Quaternion.Inverse(prevPlatformRot);

                Vector3 relPos = rb.position - prevPlatformPos;
                Vector3 rotatedRelPos = platformDeltaRot * relPos;
                Vector3 targetPos = currentPlatformRB.position + rotatedRelPos;

                platformVelocity = (targetPos - rb.position) / Time.fixedDeltaTime;

                if (inputMoveDir.sqrMagnitude <= 0.01f)
                {
                    Vector3 EulerDelta = platformDeltaRot.eulerAngles;
                    float deltaY = EulerDelta.y;
                    if (deltaY > 180f) deltaY -= 360f;

                    if (Mathf.Abs(deltaY) > 0.001f)
                        targetBaseRotation = Quaternion.Euler(0, deltaY, 0) * targetBaseRotation;
                }
            }
            else
            {
                platformVelocity = currentPlatformRB.GetPointVelocity(rb.position);
            }

            lastPlatformRB = currentPlatformRB;
            prevPlatformPos = currentPlatformRB.position;
            prevPlatformRot = currentPlatformRB.rotation;
        }
        else
        {
            platformVelocity = Vector3.zero;
            lastPlatformRB = null;
        }

        ApplyCustomGravity();
        CalculateMovement();
    }

    void LateUpdate()
    {
        if (isRidingRail || isAttachedToRoughPlatform) return;

        if (visuals)
        {
            if (wasTiltedLastFrame)
            {
                visuals.transform.localRotation = savedBaseLocalRot;
                wasTiltedLastFrame = false;
            }

            visuals.HandleVisualsLateUpdate(Time.deltaTime, isGrounded, isRidingRail);

            if (paperState && canMoveInPaperZone && inputMoveDir.sqrMagnitude > 0.01f)
            {
                savedBaseLocalRot = visuals.transform.localRotation;
                wasTiltedLastFrame = true;

                float tiltAngle = Mathf.Sin(Time.time * currentWiggleFrequency) * currentWiggleAmplitude;
                Quaternion tiltRot = Quaternion.AngleAxis(tiltAngle, Vector3.forward);

                visuals.transform.localRotation = savedBaseLocalRot * tiltRot;
            }
        }
    }

    private void OnPaperStateChanged()
    {
        UpdateGroundColliders();

        verticalVelocity = 0f;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        if (!paperState) isSliding = false;

        if (paperState && visuals)
        {
            visuals.SetWalkAnim(false);
        }

        if (isGrounded)
        {
            // ★ 슬라이딩 중이 아닐 때만 눕기 피드백 실행
            if (paperState)
            {
                if (!isSliding) OnLieDownTriggered?.Invoke();
            }
            else
            {
                OnGetUpTriggered?.Invoke(); // 일어나기
            }
        }
        else
        {
            OnPallangStarted?.Invoke();
        }
    }

    private void OnSlimStateChanged() { }

    private void UpdateGroundColliders()
    {
        if (backGroundCollider != null) backGroundCollider.enabled = paperState;
        if (feetGroundCollider != null) feetGroundCollider.enabled = !paperState;
    }

    private void EvaluateGroundCollision(Collision collision)
    {
        if (jumpCooldownTimer > 0f) return;

        bool isCurrentPlatform = currentPlatformRB != null && collision.rigidbody == currentPlatformRB;
        if (((1 << collision.gameObject.layer) & groundLayer) != 0 || isCurrentPlatform)
        {
            Vector3 bestNormal = Vector3.up;
            float maxUpDot = -1f;
            bool foundGround = false;
            Rigidbody hitPlatform = null;

            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);
                if (contact.thisCollider == feetGroundCollider || contact.thisCollider == backGroundCollider)
                {
                    float upDot = Vector3.Dot(Vector3.up, contact.normal);
                    if (upDot > maxUpDot && upDot >= 0.707f)
                    {
                        maxUpDot = upDot;
                        bestNormal = contact.normal;

                        if (Vector3.Angle(Vector3.up, bestNormal) < 3f) bestNormal = Vector3.up;

                        foundGround = true;
                        hitPlatform = contact.otherCollider.attachedRigidbody;
                    }
                }
            }

            if (foundGround)
            {
                groundCheckTimer = 0.05f;
                isGrounded = true;
                isSlideFalling = false;
                currentGroundNormal = bestNormal;

                if (isWindRollJumpExited)
                {
                    isWindRollJumpExited = false;
                }

                if (hitPlatform != null)
                {
                    currentPlatformRB = hitPlatform;
                    platformCheckTimer = 0.05f;
                    isOnPlatform = true;
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

        windBufferTimer = 0.5f;

        if (launchVelocity.y > 0) rb.position += Vector3.up * 0.2f;
        if (rb != null) rb.linearVelocity = launchVelocity;
    }

    public void LaunchPlayerToPointAndLockInput(Vector3 launchVelocity, Vector3 targetResetPosition)
    {
        isGrounded = false;
        isOnPlatform = false;
        groundCheckTimer = 0f;
        platformCheckTimer = 0f;
        isSliding = false;

        windBufferTimer = 10f;
        verticalVelocity = launchVelocity.y;
        externalHorizontalVelocity = new Vector3(launchVelocity.x, 0f, launchVelocity.z);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.WakeUp();
            rb.linearVelocity = launchVelocity;
        }

        isInputLocked = true;
        paperState = false;

        if (launchLockCoroutine != null) StopCoroutine(launchLockCoroutine);
        launchLockCoroutine = StartCoroutine(WaitForTargetPointAndSetPaperRoutine(targetResetPosition));
    }

    private IEnumerator WaitForTargetPointAndSetPaperRoutine(Vector3 targetPosition)
    {
        float timer = 0f;
        float maxTimeout = 3.0f;
        float reachDistance = 4.0f;

        yield return new WaitForSeconds(0.1f);

        while (timer < maxTimeout)
        {
            timer += Time.deltaTime;
            windBufferTimer = 2.0f;

            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            if (distanceToTarget <= reachDistance) break;

            yield return null;
        }

        windBufferTimer = 0f;
        externalHorizontalVelocity = Vector3.zero;

        isInputLocked = false;
        SetAbsolutePaperState(true);
    }

    private void ApplyExternalForceDamping()
    {
        if (windBufferTimer > 0f) windBufferTimer -= Time.fixedDeltaTime;
        float currentDamping = (windBufferTimer > 0f) ? externalForceDamping : externalForceDamping * 3f;
        externalHorizontalVelocity = Vector3.Lerp(externalHorizontalVelocity, Vector3.zero, Time.fixedDeltaTime * currentDamping);
    }

    private IEnumerator JumpRoutine(float jumpPower)
    {
        isJumpingPending = true;

        if (visuals) visuals.SetJumpAnim();

        yield return new WaitForSeconds(jumpDelay);

        isGrounded = false;
        coyoteTimer = 0f;
        isOnPlatform = false;
        groundCheckTimer = 0f;
        platformCheckTimer = 0f;

        jumpCooldownTimer = 0.25f;

        if (currentPlatformRB != null)
        {
            Vector3 platformVel = currentPlatformRB.GetPointVelocity(rb.position);
            externalHorizontalVelocity += new Vector3(platformVel.x, 0, platformVel.z);
        }

        verticalVelocity = jumpPower;
        rb.position += Vector3.up * 0.1f;
        isSliding = false;

        isJumpingPending = false;
    }

    void HandleInputs()
    {
        Vector2 input = InputManager.Instance.Movement;
        bool braceInput = InputManager.Instance.CrouchHeld;

        if (isRidingRope)
        {
            Vector3 ropeForward = cam1.transform.forward;
            ropeForward.y = 0; ropeForward.Normalize();
            float yInput = input.y > 0f ? input.y : 0f;
            inputMoveDir = ropeForward * yInput;
            return;
        }

        if (isWindRolling)
        {
            inputMoveDir = currentWindDir;
            SetBaseRotation(currentWindDir);

            if (Mathf.Abs(input.x) > 0.5f)
            {
                if (isInputReset)
                {
                    if (input.x < -0.5f && currentRailIndex > minRailIndex) currentRailIndex--;
                    else if (input.x > 0.5f && currentRailIndex < maxRailIndex) currentRailIndex++;
                    isInputReset = false;
                }
            }
            else isInputReset = true;

            targetRailOffset = currentRailIndex * currentRailDistance;

            if (canJumpInWindRoll && InputManager.Instance.JumpPressed) JumpOutFromRoll();
            return;
        }

        bool canJump = isGrounded || coyoteTimer > 0f;

        if (isInsideSlideZone)
        {
            float currentInputX = paperState ? 0f : input.x;
            inputMoveDir = (fixedSlideDirection + fixedSlideRight * currentInputX).normalized;
            braceState = false;

            if (InputManager.Instance.JumpPressed && canJump && !slimState && !paperState && !isJumpingPending)
            {
                coyoteTimer = 0f;
                StartCoroutine(JumpRoutine(jumpSpeed));
            }

            if (InputManager.Instance.PaperStateKeyTrigger && !slimState && !braceState)
            {
                if (!(!isGrounded && paperState)) paperState = !paperState;
            }

            if (visuals)
            {
                // ★ !isWindRolling 추가: 롤링 중에 방향 키를 눌러도 걷기 애니메이션이 덮어쓰지 않음
                bool isWalking = isGrounded && !isJumpingPending && !isSliding && !isWindRolling && (inputMoveDir.sqrMagnitude > 0.01f);
                visuals.SetWalkAnim(isWalking);
                visuals.SetCrouchAnim(braceState);
            }
            return;
        }

        Vector3 forward = cam1.transform.forward;
        Vector3 right = cam1.transform.right;
        forward.y = 0; right.y = 0; forward.Normalize(); right.Normalize();

        inputMoveDir = (forward * input.y + right * input.x);
        if (isForcedCrouch)
        {
            if (!braceState) OnBraceStarted?.Invoke();
            braceState = true;
            if (visuals) visuals.SetCrouchAnim(true);
        }
        else
        {
            if (braceInput && isGrounded && !slimState && !paperState && !braceExhausted && !isWet2Restricted)
            {
                // ★ 1. 버티기 첫 진입 시 시작 신호
                if (!braceState)
                {
                    braceState = true;
                    OnBraceStarted?.Invoke();
                }

                if (useBraceTimeLimit)
                {
                    if (windBufferTimer > 0f) braceTimer += Time.deltaTime;
                    currentRecoveryWaitTimer = normalRecoveryDelay;

                    // ★ 2. 버티기 시간 초과로 탈진 시 종료 신호
                    if (braceTimer >= maxBraceTime)
                    {
                        braceState = false;
                        braceExhausted = true;
                        currentRecoveryWaitTimer = exhaustedRecoveryDelay;
                        OnBraceEnded?.Invoke();
                    }
                }
            }
            else
            {
                // ★ 3. 키를 뗐을 때 종료 신호
                if (braceState)
                {
                    braceState = false;
                    OnBraceEnded?.Invoke();
                }

                if (useBraceTimeLimit && !braceInput)
                {
                    if (currentRecoveryWaitTimer > 0f) currentRecoveryWaitTimer -= Time.deltaTime;
                    else if (braceTimer > 0f) { braceTimer -= Time.deltaTime * braceRecoverySpeed; if (braceTimer <= 0f) { braceTimer = 0f; braceExhausted = false; } }
                }
            }
        }

        if (braceState || (paperState && isGrounded && !isSliding && !canMoveInPaperZone))
            inputMoveDir = Vector3.zero;

        if (InputManager.Instance.JumpPressed && canJump && !slimState && !paperState && !isWet2Restricted && !isInWindZone && !isJumpingPending)
        {
            coyoteTimer = 0f;
            StartCoroutine(JumpRoutine(jumpSpeed * jumpModifier));
        }

        if (InputManager.Instance.PaperStateKeyTrigger && !slimState && !braceState && !isWet2Restricted && !isInWindZone)
        {
            if (!(!isGrounded && paperState)) paperState = !paperState;
        }

        if (InputManager.Instance.SlideKeyTrigger && isGrounded && !slimState && !braceState && !isWet2Restricted && !isInWindZone)
        {
            if (paperState)
            {
                paperState = false;
                // ★ 슬라이드 중이었다면 종료 이벤트 호출
                if (isSliding)
                {
                    isSliding = false;
                    OnSlideEnded?.Invoke();
                }
            }
            else if (slideCooldownTimer <= 0f)
            {
                paperState = true;
                if (inputMoveDir.sqrMagnitude > 0.1f)
                {
                    isSliding = true;
                    currentSlideTimer = slideDuration * slideDurationModifier;
                    slideDirection = inputMoveDir.normalized;
                    slideCooldownTimer = slideCooldown;

                    OnSlideStarted?.Invoke();
                }
            }
        }

        if (InputManager.Instance.SlimStateKeyTrigger && !paperState && !braceState && !isWet2Restricted && !isInWindZone)
        {
            slimState = !slimState;
        }

        if (visuals)
        {
            // ★ !isWindRolling 추가 (일반 이동 구간)
            bool isWalking = isGrounded && !isJumpingPending && !isSliding && !isWindRolling && (inputMoveDir.sqrMagnitude > 0.01f);
            visuals.SetWalkAnim(isWalking);
            visuals.SetCrouchAnim(braceState);
        }
    }

    void ApplyCustomGravity()
    {
        if (paperState && verticalVelocity > 0f)
        {
            verticalVelocity = 0f;
        }

        if (isGrounded && !isSlideFalling && !isJumpingPending)
        {
            if (verticalVelocity < 0f) verticalVelocity = -1f;
            return;
        }

        if (!isGrounded || isSlideFalling)
        {
            float targetGravity = (paperState && !isSlideFalling) ? paperGravity : gravity;
            verticalVelocity += targetGravity * Time.fixedDeltaTime;

            float maxFallSpeed = (paperState && !isSlideFalling) ? paperGravity : gravity;
            if (verticalVelocity < maxFallSpeed)
                verticalVelocity = Mathf.Lerp(verticalVelocity, maxFallSpeed, Time.fixedDeltaTime * 6f);
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

                OnSlideEnded?.Invoke();

                float endRatio = Mathf.Max(0f, currentSlideTimer / slideDuration);
                float endSpeed = Mathf.SmoothStep(0f, slideSpeed, endRatio);
                float nextTargetSpeed = !isGrounded ? moveSpeed * airSpeedMultiplier : moveSpeed;
                float baseSpeed = Mathf.Max(nextTargetSpeed, 0.1f);
                lerpMoveDir = slideDirection * (endSpeed / baseSpeed);
            }
        }

        Vector3 movementVelocity = Vector3.zero;

        if (isSliding)
        {
            float speedRatio = currentSlideTimer / slideDuration;
            float currentSpeed = Mathf.SmoothStep(0f, slideSpeed, speedRatio);
            float slopeAngle = Vector3.Angle(Vector3.up, currentGroundNormal);

            if (slopeAngle > 0 && slopeAngle <= 45f) movementVelocity = Vector3.ProjectOnPlane(slideDirection, currentGroundNormal).normalized * currentSpeed;
            else movementVelocity = slideDirection * currentSpeed;

            if (verticalVelocity <= 0 && !isOnPlatform) verticalVelocity = -1f;
        }
        else if (isWindRolling)
        {
            currentRailOffset = Mathf.MoveTowards(currentRailOffset, targetRailOffset, railSwitchSpeed * Time.fixedDeltaTime);
            Vector3 windRight = Vector3.Cross(Vector3.up, currentWindDir).normalized;
            Vector3 targetLinePos = windRollAnchorPosition + (windRight * currentRailOffset);
            Vector3 toTargetLine = targetLinePos - rb.position;
            toTargetLine = Vector3.ProjectOnPlane(toTargetLine, Vector3.up);
            toTargetLine = Vector3.ProjectOnPlane(toTargetLine, currentWindDir);
            movementVelocity = toTargetLine / Time.fixedDeltaTime;
        }
        else
        {
            float targetSpeed = moveSpeed * speedModifier;
            float currentSharpness = isGrounded ? groundSharpness : airSharpness;

            if (isInsideSlideZone)
            {
                targetSpeed = slideZoneForwardSpeed;
                currentSharpness = isGrounded ? slideZoneSharpness : airSharpness;
            }
            else if (isRidingRope)
            {
                targetSpeed = ropeWalkSpeed;
                currentSharpness = groundSharpness;
            }

            if (paperState && canMoveInPaperZone)
            {
                targetSpeed = currentPaperZoneSpeed;
                lerpMoveDir = inputMoveDir;
            }
            else
            {
                if (inputMoveDir.sqrMagnitude < 0.001f && isGrounded && !isSliding)
                {
                    lerpMoveDir = Vector3.zero;
                }
                else
                {
                    if (lerpMoveDir.magnitude > 1.2f) currentSharpness = isGrounded ? 3f : 0.5f;
                    lerpMoveDir = Vector3.Lerp(lerpMoveDir, inputMoveDir, currentSharpness * Time.fixedDeltaTime);
                }
            }

            if (!isGrounded) targetSpeed *= airSpeedMultiplier;

            if (isGrounded)
            {
                if (lerpMoveDir.sqrMagnitude > 0.0001f)
                {
                    Vector3 projectedDir = Vector3.ProjectOnPlane(lerpMoveDir, currentGroundNormal).normalized;
                    movementVelocity = projectedDir * (lerpMoveDir.magnitude * targetSpeed);
                }
                else movementVelocity = Vector3.zero;
            }
            else movementVelocity = lerpMoveDir * targetSpeed;

            if (slimState) movementVelocity = lerpMoveDir * slimStateSpeed;
        }

        Vector3 finalVelocity = movementVelocity;
        finalVelocity += braceState ? externalHorizontalVelocity * 0.0f : externalHorizontalVelocity;

        if (isGrounded && !isSlideFalling)
        {
            bool hasExternalForce = externalHorizontalVelocity.sqrMagnitude > 0.01f;
            bool hasMovement = inputMoveDir.sqrMagnitude > 0.01f || lerpMoveDir.sqrMagnitude > 0.01f || isSliding;

            if (!hasMovement && !hasExternalForce)
            {
                finalVelocity = Vector3.zero;
            }
        }
        else
        {
            finalVelocity.y += verticalVelocity;
        }

        if (isOnPlatform && currentPlatformRB != null)
        {
            finalVelocity += platformVelocity;
        }

        if (paperState)
        {
            if (verticalVelocity > 0f) verticalVelocity = 0f;
            if (finalVelocity.y > 0f) finalVelocity.y = 0f;
        }

        rb.linearVelocity = finalVelocity;
    }

    public void EnableAndSetPaperState()
    {
        this.enabled = true;
        isInputLocked = false;

        isGrounded = false;
        coyoteTimer = 0f;
        isOnPlatform = false;
        groundCheckTimer = 0f;
        verticalVelocity = -2f;
        externalHorizontalVelocity = Vector3.zero;

        SetAbsolutePaperState(true);
    }

    public bool SlimCompletely
    {
        get { return slimCompletelyState; }
        set
        {
            if (slimCompletelyState != value)
            {
                slimCompletelyState = value;
                if (visuals) visuals.UpdateStates(paperState, slimState, slimCompletelyState, isWindRolling);
            }
        }
    }

    public bool PaperState
    {
        set
        {
            if (!isGrounded && paperState) return;
            paperState = !paperState;
        }
    }

    public float SlideCooldownTimer => slideCooldownTimer;
    public float SlideCooldown => slideCooldown;

    public void StopSlide()
    {
        if (isSliding)
        {
            isSliding = false;
            OnSlideEnded?.Invoke(); // ★ 강제 정지 시에도 종료 신호 발행
        }
    }

    public void SetRailMode(bool isEntering, bool playAnim = true, bool resetStates = true)
    {
        isRidingRail = isEntering;

        if (visuals && playAnim) visuals.SetHangOnAnim(isEntering);

        if (isEntering)
        {
            if (resetStates)
            {
                paperState = false; lastPaperState = false;
                slimState = false; lastSlimState = false;
                braceState = false; isSliding = false; slimCompletelyState = false;

                if (visuals) visuals.ResetRotationAndVisuals();
                if (paperTargetTransform) paperTargetTransform.localRotation = initialPaperTargetLocalRot;
            }

            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (resetStates) UpdateGroundColliders();

            // ★ 매달리기 시작 이벤트 발행 (애니메이션이 켜질 때만)
            if (playAnim) OnHangStarted?.Invoke();
        }
        else
        {
            rb.isKinematic = false;
            verticalVelocity = 0f;
            externalHorizontalVelocity = Vector3.zero;

            // ★ 매달리기 종료 이벤트 발행
            OnHangEnded?.Invoke();
        }
    }

    public void EnterSlideZone(Vector3 direction, float speed, float sharpness)
    {
        isInsideSlideZone = true;
        fixedSlideDirection = direction.normalized;
        fixedSlideRight = Vector3.Cross(Vector3.up, fixedSlideDirection).normalized;
        slideZoneForwardSpeed = speed;
        slideZoneSharpness = sharpness;

        paperState = false; slimState = false; isSliding = false; braceState = false;

        if (visuals) visuals.StopRotationTransition();
        if (paperTargetTransform) paperTargetTransform.localRotation = initialPaperTargetLocalRot;
        UpdateGroundColliders();
    }

    public void ExitSlideZone() { isInsideSlideZone = false; }

    public void SetPlatform(Rigidbody platformRB)
    {
        if (platformRB != null)
        {
            currentPlatformRB = platformRB; platformCheckTimer = 0.05f;
            isOnPlatform = true; isGrounded = true; groundCheckTimer = 0.05f;
            lastPlatformRB = platformRB; prevPlatformPos = platformRB.position;
            prevPlatformRot = platformRB.rotation;

            if (rb != null)
            {
                platformVelocity = platformRB.GetPointVelocity(rb.position);
                rb.linearVelocity = Vector3.zero;
            }
            else platformVelocity = Vector3.zero;
        }
    }

    public void SetRespawnMode(bool isEntering)
    {
        isRespawning = isEntering;

        if (isEntering)
        {
            SetAbsolutePaperState(false);
            SetSlimState(false);

            canMoveInPaperZone = false;
            braceState = false;
            isSliding = false;
            slimCompletelyState = false;

            lastPaperState = false;
            lastSlimState = false;

            isWindRollJumpExited = false;

            isRidingRail = false;
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            if (visuals)
            {
                visuals.StopRotationTransition();
                visuals.ResetRotationAndVisuals();
                visuals.UpdateStates(false, false, false, false);
            }

            if (paperTargetTransform)
            {
                paperTargetTransform.localRotation = initialPaperTargetLocalRot;
            }

            if (isWindRolling)
            {
                isWindRolling = false;
                OnRollingEnded?.Invoke(); // ★ 사망 시 롤링 연출 정리
            }

            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            UpdateGroundColliders();
        }
        else
        {
            rb.isKinematic = false;
            verticalVelocity = 0f;
            externalHorizontalVelocity = Vector3.zero;

            if (visuals) visuals.SetNormalFace();
        }
    }

    public void ForceRespawnAction() { if (visuals) visuals.SetSurpriseFace(); }

    public void SetForceBraceState(bool isForced)
    {
        // ★ 강제 버티기 상태 변경 시 이벤트 호출
        if (braceState != isForced)
        {
            if (isForced) OnBraceStarted?.Invoke();
            else OnBraceEnded?.Invoke();
        }

        isForcedCrouch = isForced;
        braceState = isForced;
        if (visuals) visuals.SetCrouchAnim(isForced);
    }

    public void SetBaseRotation(Vector3 lookForward)
    {
        lookForward.y = 0;
        if (lookForward.sqrMagnitude > 0.01f) targetBaseRotation = Quaternion.LookRotation(lookForward.normalized);
    }

    private void ProcessGroundProbe()
    {
        if (isJumpingPending || jumpCooldownTimer > 0f || verticalVelocity > 0.1f || isRidingRail || isRespawning) return;

        float rayStartOffset = paperState ? 0.5f : 0.1f;
        Vector3 rayOrigin = transform.position + Vector3.up * rayStartOffset;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, probeRayLength, groundLayer, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform)) return;

            float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);

            if (slopeAngle <= 45f)
            {
                currentGroundNormal = hit.normal;

                float distanceToGround = hit.distance - rayStartOffset;
                bool hasMovement = inputMoveDir.sqrMagnitude > 0.01f || lerpMoveDir.sqrMagnitude > 0.01f || isSliding;

                if (hasMovement && distanceToGround > 0.01f && distanceToGround <= snapDistance)
                {
                    Vector3 snappedPos = rb.position;
                    snappedPos.y = hit.point.y;
                    rb.MovePosition(snappedPos);
                }

                if (distanceToGround <= snapDistance)
                {
                    verticalVelocity = 0f;
                    isGrounded = true;
                    groundCheckTimer = 0.05f;

                    // ★ [추가] 레이캐스트로 바닥에 붙었을 때도 점프 탈출 플래그 즉시 해제
                    if (isWindRollJumpExited)
                    {
                        isWindRollJumpExited = false;
                    }

                    if (hit.rigidbody != null)
                    {
                        currentPlatformRB = hit.rigidbody;
                        isOnPlatform = true;
                        platformCheckTimer = 0.05f;
                    }
                }
            }
        }
        else
        {
            currentGroundNormal = Vector3.up;
        }
    }

    public void SetRoughPlatformAttachMode(bool isAttaching, Transform platformTransform = null)
    {
        isAttachedToRoughPlatform = isAttaching;

        if (isAttaching && platformTransform != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (platformTransform.TryGetComponent<Collider>(out Collider platformCol))
            {
                if (bodyCollider) Physics.IgnoreCollision(bodyCollider, platformCol, true);
                if (feetGroundCollider) Physics.IgnoreCollision(feetGroundCollider, platformCol, true);
                if (backGroundCollider) Physics.IgnoreCollision(backGroundCollider, platformCol, true);
            }

            transform.SetParent(platformTransform);

            verticalVelocity = 0f;
            externalHorizontalVelocity = Vector3.zero;
            isGrounded = true;
        }
        else
        {
            Transform prevPlatform = transform.parent;

            transform.SetParent(null);
            transform.localScale = Vector3.one;

            rb.detectCollisions = true;
            rb.isKinematic = false;

            if (prevPlatform != null && prevPlatform.TryGetComponent<Collider>(out Collider platformCol))
            {
                if (bodyCollider) Physics.IgnoreCollision(bodyCollider, platformCol, false);
                if (feetGroundCollider) Physics.IgnoreCollision(feetGroundCollider, platformCol, false);
                if (backGroundCollider) Physics.IgnoreCollision(backGroundCollider, platformCol, false);
            }

            if (prevPlatform != null && prevPlatform.TryGetComponent<Rigidbody>(out Rigidbody platformRB))
            {
                Vector3 exitVel = platformRB.GetPointVelocity(transform.position);
                externalHorizontalVelocity = new Vector3(exitVel.x, 0f, exitVel.z);
                verticalVelocity = exitVel.y;
                windBufferTimer = 0.5f;
                rb.linearVelocity = exitVel;
            }
        }
    }

    public bool EnterWindRollMode(Vector3 windDir, int zoneRailCount, float zoneRailDistance, Vector3 zoneOrigin)
    {
        // ★ 점프 탈출 후 아직 착지하지 않았으면 진입 차단
        if (isWindRollJumpExited) return false;

        if (isWindRolling || windRollCooldownTimer > 0f || !isGrounded) return false;

        if (slimState || paperState)
        {
            slimState = false;
            paperState = false;
            lastSlimState = false;
            lastPaperState = false;

            if (visuals)
            {
                visuals.StopRotationTransition();
                visuals.ResetRotationAndVisuals();
                visuals.UpdateStates(false, false, false, true);
            }
            if (paperTargetTransform) paperTargetTransform.localRotation = initialPaperTargetLocalRot;
        }

        isWindRolling = true;
        currentWindDir = windDir.normalized;

        SetBaseRotation(currentWindDir);
        rb.rotation = targetBaseRotation;

        currentRailDistance = zoneRailDistance;
        maxRailIndex = zoneRailCount / 2;
        minRailIndex = -maxRailIndex;

        windRollAnchorPosition = zoneOrigin;

        Vector3 windRight = Vector3.Cross(Vector3.up, currentWindDir).normalized;
        Vector3 offsetVector = transform.position - zoneOrigin;
        float lateralOffset = Vector3.Dot(offsetVector, windRight);

        int closestIndex = Mathf.RoundToInt(lateralOffset / currentRailDistance);
        currentRailIndex = Mathf.Clamp(closestIndex, minRailIndex, maxRailIndex);

        targetRailOffset = currentRailIndex * currentRailDistance;
        currentRailOffset = lateralOffset;

        isInputReset = true;

        // ★ 롤링 진입 시 이전 비주얼의 걷기를 끄고(Idle 복귀) 롤링 애니메이션 활성화
        if (visuals)
        {
            visuals.SetWalkAnim(false);
            visuals.SetCrouchAnim(false);
            visuals.SetWindRollAnim(true);
        }

        UpdateGroundColliders();

        OnRollingStarted?.Invoke();
        return true;
    }

    public void ChangeRailCount(int newRailCount)
    {
        if (!isWindRolling) return;

        maxRailIndex = newRailCount / 2;
        minRailIndex = -maxRailIndex;

        currentRailIndex = Mathf.Clamp(currentRailIndex, minRailIndex, maxRailIndex);
        targetRailOffset = currentRailIndex * currentRailDistance;
    }

    public void ExitWindRollMode(bool enterPaperState = true)
    {
        if (!isWindRolling) return;

        isWindRolling = false;
        canJumpInWindRoll = false;
        windRollCooldownTimer = windRollCooldown;

        // ★ 롤링 종료 신호 발행
        OnRollingEnded?.Invoke();

        if (visuals)
        {
            visuals.SetWindRollAnim(false);
            if (enterPaperState) visuals.SetSurpriseFace();
        }

        if (enterPaperState)
        {
            Vector3 popOutVelocity = (currentWindDir * 12f) + (Vector3.up * 4f);
            ExternalLaunch(popOutVelocity);
            paperState = true;
        }
        else
        {
            paperState = false;
            rb.linearVelocity = currentWindDir * moveSpeed;
        }
    }

    private void JumpOutFromRoll()
    {
        isWindRolling = false;
        canJumpInWindRoll = false;
        isWindRollJumpExited = true; // ★ 점프 탈출 플래그 ON (착지 전까지 재진입 불가)
        windRollCooldownTimer = 0f;
        jumpCooldownTimer = 0.15f;   // ★ 점프 직후 바닥 충돌 재감지 방지 (드득거림 원인 해결)

        // ★ 롤링 종료 신호 발행
        OnRollingEnded?.Invoke();

        if (visuals)
        {
            visuals.SetWindRollAnim(false);
            visuals.SetJumpAnim(); // 점프 비주얼 출력
        }

        isGrounded = false;
        coyoteTimer = 0f;
        isOnPlatform = false;
        groundCheckTimer = 0f;
        platformCheckTimer = 0f;

        Vector3 currentVel = rb.linearVelocity;
        rb.linearVelocity = new Vector3(currentVel.x, windRollJumpSpeed, currentVel.z);
        verticalVelocity = windRollJumpSpeed;
        rb.position += Vector3.up * 0.1f;

        paperState = false; slimState = false; braceState = false;
    }

    public void SetAbsolutePaperState(bool isPaper) { paperState = isPaper; }
    public bool GetPaperState() => paperState;
    public bool GetSlimState() => slimState;

    public void SetSlimState(bool isSlim)
    {
        if (slimState != isSlim)
        {
            slimState = isSlim;

            if (visuals) visuals.UpdateStates(paperState, slimState, slimCompletelyState, isWindRolling);
            OnSlimStateChanged();
        }
    }

    public void ResetAllVelocities()
    {
        externalHorizontalVelocity = Vector3.zero;
        platformVelocity = Vector3.zero;
        verticalVelocity = 0f;
        inputMoveDir = Vector3.zero;
        lerpMoveDir = Vector3.zero;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public void EnterRopeWalk(float speed, RopeBalanceGimmick gimmick)
    {
        isRidingRope = true;
        ropeWalkSpeed = speed;
        currentRopeGimmick = gimmick;

        paperState = false;
        slimState = false;
        braceState = false;
        isSliding = false;
        if (launchLockCoroutine != null)
        {
            StopCoroutine(launchLockCoroutine);
            launchLockCoroutine = null;
        }
        UpdateGroundColliders();
    }

    public void StartSplineRoll()
    {
        SetRailMode(true, playAnim: false, resetStates: true);

        isWindRolling = true;
        if (visuals)
        {
            // ★ 스플라인 롤링 진입 시에도 걷기 종료
            visuals.SetWalkAnim(false);
            visuals.UpdateStates(false, false, false, true);
            visuals.SetWindRollAnim(true);
        }

        OnRollingStarted?.Invoke();
    }

    public void StopSplineRoll(bool applyExternalLaunch = false, Vector3 exitVelocity = default)
    {
        isWindRolling = false;

        // ★ 롤링 종료 신호 발행
        OnRollingEnded?.Invoke();


        if (visuals)
        {
            visuals.SetWindRollAnim(false);
        }

        SetRailMode(false);

        if (applyExternalLaunch)
        {
            ExternalLaunch(exitVelocity);
        }
    }

    public void ExitRopeWalk()
    {
        isRidingRope = false;
        currentRopeGimmick = null;
    }

    public void SetRopeTilt(float angle)
    {
        if (visuals != null) visuals.SetRopeTilt(angle);
    }

    public bool IsGrounded => isGrounded;
    public bool IsSliding => isSliding;
    public bool IsBracing => braceState;
    public bool HasMoveInput => inputMoveDir.sqrMagnitude > 0.01f;
}