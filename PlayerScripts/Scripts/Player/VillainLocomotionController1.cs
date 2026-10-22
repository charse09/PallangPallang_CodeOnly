using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VillainLocomotion : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private BoxCollider bodyCollider;
    [SerializeField] private Transform characterVisual;
    [SerializeField] private Camera cam1;
    [SerializeField] private Animator anim;
    [SerializeField] private VillainFaceMaterial faceStance;

    [Header("Ground Check Colliders")]
    [SerializeField] private BoxCollider feetGroundCollider;
    [SerializeField] private BoxCollider backGroundCollider;

    [Header("Movement Settings")]
    [SerializeField] private float slimStateSpeed = 4.0f;
    [SerializeField] private float moveSpeed = 16f;
    [SerializeField] private float sprintSpeed = 24f;
    [SerializeField] private float jumpSpeed = 15f;
    [SerializeField] private float gravity = -30f;
    [SerializeField] private float paperGravity = -5f;
    [SerializeField] private float groundSharpness = 15f;
    [SerializeField] private float airSharpness = 2f;
    [SerializeField] private float airSpeedMultiplier = 0.5f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Brace Settings")]
    [Tooltip("체크 시 정해진 시간 동안만 버틸 수 있습니다.")]
    [SerializeField] private bool useBraceTimeLimit = true;
    [Tooltip("버티기 최대 시간 (초)")]
    [SerializeField] private float maxBraceTime = 2.0f;
    [Tooltip("버티기를 푼 후 피로도가 회복되기 시작할 때까지의 딜레이 (초)")]
    [SerializeField] private float normalRecoveryDelay = 1.0f;
    [Tooltip("탈진(2초 초과) 시 피로도 회복이 시작될 때까지의 긴 페널티 딜레이 (초)")]
    [SerializeField] private float exhaustedRecoveryDelay = 3.0f;
    [Tooltip("피로도 회복 속도 (높을수록 빨리 0으로 초기화됨)")]
    [SerializeField] private float braceRecoverySpeed = 2.0f;

    // 내부 타이머 및 과열 방지용 변수
    private float braceTimer = 0f; // 0에서 시작해서 maxBraceTime까지 차오르는 피로도
    private bool braceExhausted = false;
    private float currentRecoveryWaitTimer = 0f; // 회복을 시작하기 전 대기하는 타이머


    [Header("Slide Settings")]
    [SerializeField] private float slideSpeed = 35f;
    [SerializeField] private float slideDuration = 1.0f;
    private bool isSliding = false;
    private float currentSlideTimer = 0f;
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
    // [수정] bool에서 float로 변경 (바람을 맞은 뒤 유지할 시간)
    private float windBufferTimer = 0f;

    private Vector3 inputMoveDir;
    private Vector3 lerpMoveDir;
    private float verticalVelocity;

    private Coroutine currentCoroutine;
    private Vector3 currentGroundNormal = Vector3.up;

    private float swayTimer = 0f;
    private Quaternion baseMoveRotation = Quaternion.identity;

    // 0이면 바닥, 1이면 완전한 공중 상태를 나타내는 부드러운 전환 값
    private float swayWeight = 0f;



    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (bodyCollider == null) bodyCollider = GetComponent<BoxCollider>();

        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        baseMoveRotation = transform.rotation;
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

        lastPaperState = paperState;
        lastSlimState = slimState;

        UpdateGroundColliders();
    }


    void Update()
    {
        HandleInputs();

        if (paperState != lastPaperState)
        {
            lastPaperState = paperState;
            OnPaperStateChanged();
        }

        if (slimState != lastSlimState)
        {
            lastSlimState = slimState;
            OnSlimStateChanged();
        }

        // --- [수정된 팔랑거림 로직 (가중치 보간)] ---
        if (!isGrounded && paperState)
        {
            // 공중에 뜨면 가중치를 1로 부드럽게 올림 (약 0.3초 소요)
            swayWeight = Mathf.MoveTowards(swayWeight, 1f, Time.deltaTime * 3f);
        }
        else
        {
            // 땅에 닿으면 가중치를 0으로 부드럽고 빠르게 내림 (찰싹 달라붙는 느낌)
            swayWeight = Mathf.MoveTowards(swayWeight, 0f, Time.deltaTime * 8f);
        }

        // 타이머는 파도가 끊기지 않게 계속 돌려줍니다.
        // (어차피 땅에 닿아서 swayWeight가 0이 되면 흔들림이 화면에 안 보입니다)
        if (paperState)
        {
            swayTimer += Time.deltaTime * swaySpeed;
        }
        PlayerFaceControl();
    }



        void FixedUpdate()
    {
        ApplyCustomGravity();
        CalculateMovement();

        // [핵심 해결 1] 매 물리 연산마다 바닥 상태를 초기화합니다.
        // 유니티는 FixedUpdate 직후에 충돌(OnCollision)을 계산하므로,
        // 바닥에 닿아있다면 바로 이어서 실행되는 OnCollisionStay에서 다시 true로 만들어 줍니다.
        isGrounded = false;
        currentGroundNormal = Vector3.up;
    }

    void LateUpdate()
    {
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

        if (!paperState)
        {
            isSliding = false;
        }
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
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint contact = collision.GetContact(i);

                if (contact.thisCollider == feetGroundCollider || contact.thisCollider == backGroundCollider)
                {
                    // [핵심 해결 2] 부딪힌 표면의 각도를 검사합니다.
                    // 위쪽(Vector3.up)을 기준으로 45도 이하의 각도를 가질 때만 바닥으로 인정합니다.
                    // 수직에 가까운 절벽 면을 긁을 때는 조건에 맞지 않아 isGrounded가 켜지지 않습니다.
                    if (Vector3.Angle(Vector3.up, contact.normal) <= 45f)
                    {
                        isGrounded = true;
                        isSlideFalling = false;
                        currentGroundNormal = contact.normal;

                        if (verticalVelocity <= 0)
                            verticalVelocity = -1f;

                        return;
                    }
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateGroundCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateGroundCollision(collision);
    }

    // [수정됨] 매 FixedUpdate에서 isGrounded를 false로 만들므로 OnCollisionExit은 더 이상 필요 없습니다!
    // 유니티의 OnCollisionExit 버그에서 완벽하게 자유로워집니다.

    public void AddContinuousForce(Vector3 force)
    {
        // [수정] 0.5초 동안은 바람을 맞고 있는 것으로 간주
        windBufferTimer = 0.5f;

        externalHorizontalVelocity += new Vector3(force.x, 0f, force.z);

        if (Mathf.Abs(force.y) > 0.1f)
        {
            if (force.y > 0 && isGrounded)
            {
                isGrounded = false;
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
        isSliding = false;

        if (rb != null) rb.linearVelocity = launchVelocity;
    }

    private void ApplyExternalForceDamping()
    {
        // [수정] 윈드 버퍼 타이머 감소
        if (windBufferTimer > 0f) windBufferTimer -= Time.fixedDeltaTime;

        // [수정] 바람을 맞고 있거나(windBufferTimer > 0), 바람을 맞은 지 얼마 안 됐다면 Damping을 낮게 유지
        float currentDamping = (windBufferTimer > 0f) ? externalForceDamping : externalForceDamping * 3f;

        externalHorizontalVelocity = Vector3.Lerp(externalHorizontalVelocity, Vector3.zero, Time.fixedDeltaTime * currentDamping);
    }

    void HandleInputs()
    {
        Vector2 input = InputManager.Instance.Movement;
        Vector3 forward = cam1.transform.forward;
        Vector3 right = cam1.transform.right;
        forward.y = 0; right.y = 0;
        forward.Normalize(); right.Normalize();

        inputMoveDir = (forward * input.y + right * input.x);

        // --- [수정된 고퀄리티 버티기(Brace) 시스템] ---
        bool braceInput = Input.GetKey(KeyCode.C);

        // 1. 버티기 모드 발동 조건 검사 (탈진하지 않았고, 종이나 슬라임이 아닐 때만!)
        if (braceInput && isGrounded && !slimState && !paperState && !braceExhausted)
        {
            braceState = true;

            if (useBraceTimeLimit)
            {
                // [수정] 이제 바람이 끊겨도 0.5초간은 버티기 타이머가 유지됩니다!
                if (windBufferTimer > 0f)
                {
                    braceTimer += Time.deltaTime;
                }

                // 놓는 순간부터 이 시간(1초)만큼 기다려야 회복이 시작되도록 딜레이 세팅
                currentRecoveryWaitTimer = normalRecoveryDelay;

                if (braceTimer >= maxBraceTime)
                {
                    // 피로도가 꽉 차면 강제로 풀리고 탈진 상태 돌입!
                    braceState = false;
                    braceExhausted = true;
                    // 탈진했으므로 훨씬 긴 페널티 대기시간(3초) 세팅
                    currentRecoveryWaitTimer = exhaustedRecoveryDelay;
                }
            }
        }
        else
        {
            braceState = false;

            // 2. 버티기를 풀었을 때 (false 상태일 때)의 회복 및 초기화 로직
            if (useBraceTimeLimit)
            {
                // C키를 확실히 떼어야만 회복 쿨타임이 돌아가게 함 (계속 누르는 꼼수 방지)
                if (!braceInput)
                {
                    if (currentRecoveryWaitTimer > 0f)
                    {
                        // 일단 지정된 딜레이(1초 or 3초)를 기다립니다.
                        currentRecoveryWaitTimer -= Time.deltaTime;
                    }
                    else if (braceTimer > 0f)
                    {
                        // 딜레이가 끝나면 타이머(피로도)가 부드럽게 초기화 시작!
                        braceTimer -= Time.deltaTime * braceRecoverySpeed;

                        if (braceTimer <= 0f)
                        {
                            braceTimer = 0f;
                            braceExhausted = false; // 완전히 0으로 초기화되면 탈진 상태도 해제!
                        }
                    }
                }
            }
        }

        // 버티기 모드이거나, 종이 상태에서 땅에 가만히 있을 때는 이동 방향을 0으로 잠금
        if (braceState || (paperState && isGrounded && !isSliding))
        {
            inputMoveDir = Vector3.zero;
        }
        // ------------------------------------------


        if (InputManager.Instance.JumpPressed && isGrounded && !slimState && !paperState)
        {
            isGrounded = false;
            verticalVelocity = jumpSpeed;
            rb.position += Vector3.up * 0.1f;
            isSliding = false;
        }

        // 종이 상태 변환 트리거 (버티는 중이 아닐 때만 가능하게)
        if (InputManager.Instance.PaperStateKeyTrigger && !slimState && !braceState)
        {
            // [방어 로직] 공중(!isGrounded)인데 종이 상태(paperState)라면 끄지 못하게 막습니다!
            if (!isGrounded && paperState)
            {
                // 아무 일도 일어나지 않음 (무시)
            }
            else
            {
                paperState = !paperState; // 땅에 있거나, 일반 상태일 때만 정상적으로 전환

                if (paperState && isGrounded && InputManager.Instance.SprintHeld && inputMoveDir.sqrMagnitude > 0.1f)
                {
                    isSliding = true;
                    currentSlideTimer = slideDuration;
                    slideDirection = inputMoveDir.normalized;
                }
            }
        }

        if (InputManager.Instance.SlimStateKeyTrigger && isGrounded && !paperState && !braceState)
        {
            slimState = !slimState;
        }

        if (anim != null)
        {
            anim.SetBool("IsWalk", inputMoveDir.sqrMagnitude > 0.01f);
            // [수정됨] GetKeyDown 대신 우리가 정교하게 설계한 braceState 변수를 직접 연결합니다!
            anim.SetBool("IsCrouch", braceState);
        }
            
    }

    void UpdateMovementRotation()
    {
        if (isSliding)
        {
            baseMoveRotation = Quaternion.LookRotation(slideDirection);
            if (currentCoroutine == null) ApplyFinalRotation();
            return;
        }

        if (inputMoveDir.sqrMagnitude > 0.01f)
        {
            baseMoveRotation = Quaternion.LookRotation(inputMoveDir);
            if (currentCoroutine == null) ApplyFinalRotation();
        }
        else if (currentCoroutine == null)
        {
            ApplyFinalRotation();
        }
    }

    Vector3 Vectransform = Vector3.zero;
    private Vector3 debugPivotWorldPos;
    private Vector3 debugVisualWorldPos;

    void ApplyFinalRotation()
    {
        Quaternion stateOffset = Quaternion.identity;
        if (paperState) stateOffset = Quaternion.Euler(-90f, 0, 0);
        else if (slimState) stateOffset = Quaternion.Euler(0, 90f, 0);
        transform.rotation = baseMoveRotation * stateOffset;

        if (characterVisual != null)
        {
            float currentTilt = slimCompletelyState ? 0.0f : 15.0f;

            //if (paperState)
            //{
            //    if (!isGrounded)
            //    {
            //        float angle = Mathf.Sin(swayTimer) * swayAmount;
            //        Quaternion swayRot = Quaternion.Euler(currentTilt, angle, 0f);
            //        characterVisual.localPosition = swayPivot + (swayRot * -swayPivot);
            //        characterVisual.localRotation = swayRot;

            //        Vectransform = characterVisual.localPosition;
            //        debugPivotWorldPos = transform.position + (transform.rotation * swayPivot);
            //        debugVisualWorldPos = characterVisual.position;
            //    }
            //    else
            //    {
            //        characterVisual.localPosition = Vector3.zero;
            //        characterVisual.localRotation = Quaternion.Euler(10f, 0f, 0f);
            //    }
            //}
            if (paperState)
            {
                // [수정] !isGrounded 체크를 없애고 swayWeight를 곱해줍니다!
                float angle = Mathf.Sin(swayTimer) * swayAmount * swayWeight;
                Quaternion swayRot = Quaternion.Euler(currentTilt, angle, 0f);
                Vector3 swayPos = swayPivot + (swayRot * -swayPivot);

                // 바닥에 있을 때의 기본 위치/회전
                Quaternion groundRot = Quaternion.Euler(10f, 0f, 0f);
                Vector3 groundPos = Vector3.zero;

                // 두 상태를 swayWeight(0~1)에 따라 스무스하게 섞어줍니다
                characterVisual.localPosition = Vector3.Lerp(groundPos, swayPos, swayWeight);
                characterVisual.localRotation = Quaternion.Slerp(groundRot, swayRot, swayWeight);

                // (디버그 변수 업데이트는 필요하시면 남겨두세요)
                Vectransform = characterVisual.localPosition;
                debugPivotWorldPos = transform.position + (transform.rotation * swayPivot);
                debugVisualWorldPos = characterVisual.position;
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
                //// [수정] 코루틴 회전 중에도 0.1초 유예를 줍니다.
                //float swayY = (paperState) ? Mathf.Sin(swayTimer) * swayAmount : 0f;
                // [수정] 코루틴 회전 중에도 가중치(swayWeight)를 곱해줍니다!
                float swayY = paperState ? Mathf.Sin(swayTimer) * swayAmount * swayWeight : 0f;

                Quaternion targetVisualRot = paperState ? Quaternion.Euler(currentTilt, swayY, 0) :
                                            (slimState ? Quaternion.Euler(0, -currentTilt, 0) : Quaternion.identity);

                characterVisual.localRotation = Quaternion.Slerp(startVisualRot, targetVisualRot, t);
            }
            yield return null;
        }

        currentCoroutine = null;
    }

    void CalculateMovement()
    {
        ApplyExternalForceDamping();

        if (isSliding)
        {
            currentSlideTimer -= Time.fixedDeltaTime;

            if (currentSlideTimer <= 0f || !isGrounded || !paperState)
            {
                if (!isGrounded && paperState)
                {
                    isSlideFalling = true;
                }

                isSliding = false;

                float endRatio = Mathf.Max(0f, currentSlideTimer / slideDuration);
                float endSpeed = Mathf.SmoothStep(0f, slideSpeed, endRatio);

                float nextTargetSpeed = InputManager.Instance.SprintHeld ? sprintSpeed : moveSpeed;
                if (!isGrounded) nextTargetSpeed *= airSpeedMultiplier;

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
            if (slopeAngle > 0 && slopeAngle <= 45f)
                movementVelocity = Vector3.ProjectOnPlane(slideDirection, currentGroundNormal).normalized * currentSpeed;
            else
                movementVelocity = slideDirection * currentSpeed;

            if (verticalVelocity <= 0) verticalVelocity = -1f;
        }
        else
        {
            float targetSpeed = InputManager.Instance.SprintHeld ? sprintSpeed : moveSpeed;
            float currentSharpness = isGrounded ? groundSharpness : airSharpness;

            if (lerpMoveDir.magnitude > 1.2f)
            {
                currentSharpness = isGrounded ? 3f : 0.5f;
            }

            if (!isGrounded) targetSpeed *= airSpeedMultiplier;

            lerpMoveDir = Vector3.Lerp(lerpMoveDir, inputMoveDir, currentSharpness * Time.fixedDeltaTime);

            if (isGrounded)
            {
                float slopeAngle = Vector3.Angle(Vector3.up, currentGroundNormal);
                if (slopeAngle > 0 && slopeAngle <= 45f)
                    movementVelocity = Vector3.ProjectOnPlane(lerpMoveDir, currentGroundNormal).normalized * (lerpMoveDir.magnitude * targetSpeed);
                else
                    movementVelocity = lerpMoveDir * targetSpeed;

                if (verticalVelocity <= 0) verticalVelocity = -1f;
            }
            else
            {
                movementVelocity = lerpMoveDir * targetSpeed;
            }

            if (slimState == true)
            {
                movementVelocity = lerpMoveDir * slimStateSpeed;
            }
        }

        // [수정됨] 버티기 로직 적용
        Vector3 finalVelocity = movementVelocity;

        if (braceState)
        {
            // 바닥을 꽉 잡고 있으므로 외부 힘을 상쇄시킵니다. (미끄러지지 않음)
            // (나중에 힘이 너무 세면 조금씩 밀려나게 할 수도 있도록 곱하기 연산으로 둡니다. 현재는 0 곱해서 완벽 방어)
            finalVelocity += externalHorizontalVelocity * 0.0f;
        }
        else
        {
            // 평소에는 외부 힘을 그대로 받아서 밀려납니다.
            finalVelocity += externalHorizontalVelocity;
        }

        finalVelocity.y = verticalVelocity;

        rb.linearVelocity = finalVelocity;
    }

        void ApplyCustomGravity()
    {
        float targetGravity = (paperState && verticalVelocity < 0 && !isSlideFalling) ? paperGravity : gravity;
        verticalVelocity += targetGravity * Time.fixedDeltaTime;

        float maxFallSpeed = (paperState && !isSlideFalling) ? paperGravity : gravity;

        if (verticalVelocity < maxFallSpeed)
        {
            verticalVelocity = Mathf.Lerp(verticalVelocity, maxFallSpeed, Time.fixedDeltaTime * 6f);
        }
    }

    void PlayerFaceControl()
    {
        // faceStance가 인스펙터에 안 들어가 있으면 에러가 날 수 있으니 안전장치
        if (faceStance == null) return;

        bool isMovingHorizontally = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).sqrMagnitude > 0.1f;
        if (braceState)
        {
            faceStance.SetHurtFace(); // 읏챠! 하고 버티는 표정 (원하시는 표정으로 변경)
        }
        // 1순위: Paper 상태이면서 (슬라이딩 중이거나 OR X, Z축으로 이동 중일 때)
        else if (paperState && (isSliding || isMovingHorizontally))
        {
            faceStance.SetSmileFace();
        }
        // 2순위: Paper 상태이긴 한데 속도 없이 가만히 있을 때
        else if (paperState)
        {
            faceStance.SetEmbarassedFace();
        }
        // 3순위: Slim 상태 (벽에 붙은 상태)
        else if (slimState)
        {
            faceStance.SetJigglingFace();
        }
        // 4순위: 일반 상태에서 공중에 떠 있을 때 (점프, 추락)
        else if (!isGrounded)
        {
            faceStance.SetTalkFace();
        }
        // 5순위: 땅에 있고 아무것도 안 하는 기본 상태
        else
        {
            faceStance.SetNormalFace();
        }
    }
    public bool SlimCompletely
    {
        get { return slimCompletelyState; }
        set
        {
            if (slimCompletelyState != value)
            {
                slimCompletelyState = value;
                if (currentCoroutine == null) ApplyFinalRotation();
            }
        }
    }

    public bool PaperState
    {
        set
        {
            // [방어 로직] 공중(!isGrounded)인데 종이 상태(paperState)를 끄려고 하면 바로 입구컷!
            if (!isGrounded && paperState) return;

            paperState = !paperState;
            if (currentCoroutine != null) StopCoroutine(currentCoroutine);
            currentCoroutine = StartCoroutine(RotationRoutine());
        }
    }

    // ─── SyncLocomotionStateOutput / 외부 제어용 공개 API ─────────────

    /// <summary>
    /// baseMoveRotation을 지정한 수평 forward 방향으로 동기화합니다.
    /// SyncLocomotionStateOutput 노드에서 Locomotion 재활성화 직전에 호출합니다.
    /// </summary>
    public void SetBaseRotation(Vector3 horizontalForward)
    {
        if (horizontalForward.sqrMagnitude < 0.001f) return;
        baseMoveRotation = Quaternion.LookRotation(horizontalForward.normalized);
    }

    /// <summary>
    /// 내부 속도 변수(verticalVelocity, externalHorizontalVelocity, lerpMoveDir, inputMoveDir)를
    /// 0으로 초기화합니다. Locomotion 재활성화 직전에 호출하면 잔여 관성으로 인한 튕김을 방지합니다.
    /// </summary>
    public void ResetInternalVelocities()
    {
        verticalVelocity            = 0f;
        externalHorizontalVelocity  = Vector3.zero;
        lerpMoveDir                 = Vector3.zero;
        inputMoveDir                = Vector3.zero;
        windBufferTimer             = 0f;
    }

    /// <summary>
    /// braceState를 강제로 설정합니다.
    /// false를 전달하면 버티기 상태를 즉시 해제하고 피로도 타이머도 초기화합니다.
    /// </summary>
    public void SetForceBraceState(bool value)
    {
        braceState = value;
        if (!value)
        {
            braceTimer              = 0f;
            braceExhausted          = false;
            currentRecoveryWaitTimer = 0f;
        }
    }

    /// <summary>
    /// braceState를 외부에서 직접 get/set합니다.
    /// LookNetDrone 등 외부 노드에서 빌런을 braceState로 전환할 때 사용합니다.
    /// </summary>
    public bool ForceBraceState
    {
        get => braceState;
        set => SetForceBraceState(value);
    }

    /// <summary>
    /// paperState, slimState, braceState를 모두 해제합니다.
    /// SyncLocomotionStateOutput의 resetBraceState 옵션에서 호출됩니다.
    /// </summary>
    public void CancelSpecialStatesForCondition()
    {
        if (paperState) paperState = false;
        if (slimState)  slimState  = false;
        braceState     = false;
        isSliding      = false;
        slimCompletelyState = false;
    }
}