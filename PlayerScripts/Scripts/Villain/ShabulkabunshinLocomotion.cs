using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShabulkabunshinLocomotion : MonoBehaviour
{
    public enum AIState { Patrol, Chase }

    [Header("Visuals System")]
    [Tooltip("분리된 몬스터 시각 관리 스크립트를 연결해주세요")]
    [SerializeField] private EnemyVisuals visuals;

    [Header("Animation Addons")]
    [SerializeField] private EnemyWobble enemyWobble;

    [Header("Collider System")]
    [Tooltip("비주얼과 동일하게 눕거나 회전할 콜라이더 묶음(빈 오브젝트)을 연결해주세요")]
    [SerializeField] private Transform colliderRoot;

    [Header("Components")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private BoxCollider bodyCollider;

    [Header("Ground Check Colliders")]
    [SerializeField] private BoxCollider feetGroundCollider;
    [SerializeField] private BoxCollider backGroundCollider;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float chaseSpeed = 12f;
    [SerializeField] private float slimStateSpeed = 3.0f;
    [SerializeField] private float paperStateSpeed = 8.0f;
    [SerializeField] private float gravity = -30f;
    [SerializeField] private float paperGravity = -5f;
    [SerializeField] private float rotationSharpness = 10f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Paper Recovery Settings")]
    [Tooltip("땅에 닿은 후 PaperState가 풀리기까지의 시간 (초)")]
    [SerializeField] private float paperGroundedDuration = 3.0f;
    [Tooltip("PaperState가 풀린 후 다시 이동할 때까지의 대기 시간 (초)")]
    [SerializeField] private float moveDelayAfterPaper = 1.0f;

    [Header("AI Settings")]
    [SerializeField] private AIState currentState = AIState.Patrol;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float waypointTolerance = 1.5f;
    private int currentWaypointIndex = 0;

    [Tooltip("추적할 플레이어 타겟")]
    private Transform playerTarget;

    // ==========================================
    // 프로퍼티 기반 상태 변수
    // ==========================================
    [Header("Condition States")]
    [SerializeField] private bool _paperState = false;
    public virtual bool PaperState
    {
        get => _paperState;
        set
        {
            if (_paperState != value)
            {
                _paperState = value;
                OnPaperStateChanged();
            }
        }
    }

    [SerializeField] private bool _slimState = false;
    public virtual bool SlimState
    {
        get => _slimState;
        set
        {
            if (_slimState != value)
            {
                _slimState = value;
                OnSlimStateChanged();
            }
        }
    }

    // 내부 물리 계산용 변수
    private bool isGrounded;
    private float groundCheckTimer = 0f;
    private float verticalVelocity;
    private Vector3 currentGroundNormal = Vector3.up;
    private Vector3 moveDirection;

    // 기믹용 타이머 변수
    private float paperGroundedTimer = 0f;
    private float moveDelayTimer = 0f;
    private bool isRecoveringMove = false;

    // 발사 상태 관련 변수
    private bool isLaunched = false;
    private float launchImmunityTimer = 0f; // 발사 직후 바닥 즉시 판정 방지 타이머
    private Quaternion launchRotation;      // 비행 중 유지할 X축 -90도 회전값

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (visuals == null) Debug.LogWarning("ShabulkabunshinLocomotion에 EnemyVisuals가 연결되지 않았습니다!");
        if (colliderRoot == null) Debug.LogWarning("콜라이더들이 회전하려면 Collider Root가 연결되어야 합니다!");
    }

    protected virtual void OnEnable()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        currentState = AIState.Patrol;
        playerTarget = null;
        verticalVelocity = 0f;

        PaperState = false;
        SlimState = false;

        paperGroundedTimer = 0f;
        moveDelayTimer = 0f;
        isRecoveringMove = false;
        isLaunched = false;
        launchImmunityTimer = 0f;

        UpdateGroundColliders();
    }

    protected virtual void Update()
    {
        if (visuals)
        {
            visuals.UpdateStates(PaperState, SlimState, false);
        }

        if (enemyWobble)
        {
            Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float currentSpeedRatio = horizontalVelocity.magnitude / moveSpeed;
            enemyWobble.SetMovementMagnitude(currentSpeedRatio);
        }
    }

    protected virtual void FixedUpdate()
    {
        // 발사 보호 타이머 차감
        if (launchImmunityTimer > 0f)
        {
            launchImmunityTimer -= Time.fixedDeltaTime;
        }

        if (groundCheckTimer > 0f) groundCheckTimer -= Time.fixedDeltaTime;

        // 발사 보호 시간 동안은 바닥에 닿았어도 isGrounded를 false로 유발
        isGrounded = (launchImmunityTimer <= 0f) && (groundCheckTimer > 0f);

        HandlePaperStateLogic();

        if (!isLaunched)
        {
            ApplyCustomGravity();
            CalculateAI();
            ApplyMovement();
        }
    }

    protected virtual void LateUpdate()
    {
        if (isLaunched)
        {
            // 비행 중일 때는 지정된 X축 -90도 회전값을 강제 고정
            transform.rotation = launchRotation;

            if (colliderRoot != null)
            {
                colliderRoot.rotation = launchRotation;
            }
            return;
        }

        if (visuals)
        {
            // 1. 비주얼 회전 처리
            visuals.UpdateBaseRotation(moveDirection, false, Vector3.zero, false, Vector3.zero);
            visuals.HandleVisualsLateUpdate(Time.deltaTime, isGrounded, false);

            // 2. 비주얼의 최종 회전값을 콜라이더 루트에 동기화
            if (colliderRoot != null)
            {
                colliderRoot.rotation = visuals.transform.rotation;
            }
        }
    }

    private void HandlePaperStateLogic()
    {
        // 발사되어 날아가는 중(isLaunched)일 때는 공중 자동 PaperState 전환 무시
        if (!isLaunched && !isGrounded && verticalVelocity < -0.5f)
        {
            isRecoveringMove = false;
            if (!PaperState)
            {
                PaperState = true;
                paperGroundedTimer = 0f;
            }
        }

        if (isGrounded)
        {
            // 진짜 바닥에 닿았을 때 (발사 보호 시간이 지난 후)
            if (isLaunched)
            {
                isLaunched = false;

                // 기울어졌던 X축 -90도 회전을 원래 상태(세워진 Y축 회전)로 복원
                Vector3 currentEuler = transform.rotation.eulerAngles;
                transform.rotation = Quaternion.Euler(0f, currentEuler.y, 0f);

                // 바닥에 닿았으므로 PaperState로 변환하여 일어나기 타이머 작동
                PaperState = true;
                paperGroundedTimer = 0f;
            }

            if (PaperState)
            {
                paperGroundedTimer += Time.fixedDeltaTime;

                if (paperGroundedTimer >= paperGroundedDuration)
                {
                    PaperState = false;
                    paperGroundedTimer = 0f;

                    isRecoveringMove = true;
                    moveDelayTimer = moveDelayAfterPaper;
                }
            }
            else if (isRecoveringMove)
            {
                moveDelayTimer -= Time.fixedDeltaTime;

                if (moveDelayTimer <= 0f)
                {
                    isRecoveringMove = false;
                }
            }
        }
    }

    protected virtual void OnPaperStateChanged()
    {
        UpdateGroundColliders();
        if (visuals) visuals.TriggerStateChangeTransition();
    }

    protected virtual void OnSlimStateChanged()
    {
        if (visuals) visuals.TriggerStateChangeTransition();
    }

    private void UpdateGroundColliders()
    {
        if (backGroundCollider != null) backGroundCollider.enabled = PaperState;
        if (feetGroundCollider != null) feetGroundCollider.enabled = !PaperState;
    }

    public virtual void SetTarget(Transform target)
    {
        playerTarget = target;
        currentState = AIState.Chase;
    }

    public virtual void LoseTarget()
    {
        playerTarget = null;
        currentState = AIState.Patrol;
    }

    /// <summary>
    /// 보스가 StraightShot처럼 중력 없이 일직선으로 날릴 때 호출
    /// </summary>
    public void Launch(Transform target, Vector3 flyDirection)
    {
        isLaunched = true;
        launchImmunityTimer = 0.2f; // 발사 직후 0.2초간은 바닥 판정 강제 무시
        PaperState = false;

        if (target != null)
        {
            SetTarget(target);
        }

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.angularVelocity = Vector3.zero;
        }

        // 날아가는 방향을 바라보고 X축 -90도 회전 적용 및 저장
        if (flyDirection.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(flyDirection);
            launchRotation = lookRot * Quaternion.Euler(-90f, 0f, 0f);
            transform.rotation = launchRotation;
        }
    }

    private void CalculateAI()
    {
        bool canMove = !(PaperState && isGrounded) && !isRecoveringMove;

        if (!canMove)
        {
            moveDirection = Vector3.zero;
            return;
        }

        if (currentState == AIState.Patrol)
        {
            if (waypoints.Length == 0) return;

            Vector3 targetPos = waypoints[currentWaypointIndex].position;
            Vector3 dirToTarget = targetPos - rb.position;
            dirToTarget.y = 0;

            if (dirToTarget.sqrMagnitude < waypointTolerance * waypointTolerance)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
            }

            moveDirection = dirToTarget.normalized;
        }
        else if (currentState == AIState.Chase)
        {
            if (playerTarget != null)
            {
                Vector3 dirToTarget = playerTarget.position - rb.position;
                dirToTarget.y = 0;
                moveDirection = dirToTarget.normalized;
            }
        }

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDirection);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * rotationSharpness));
        }
    }

    private void ApplyMovement()
    {
        float currentTargetSpeed = (currentState == AIState.Chase) ? chaseSpeed : moveSpeed;
        if (SlimState) currentTargetSpeed = slimStateSpeed;
        if (PaperState) currentTargetSpeed = paperStateSpeed;

        Vector3 movementVelocity = Vector3.zero;

        if (isGrounded)
        {
            float slopeAngle = Vector3.Angle(Vector3.up, currentGroundNormal);
            if (slopeAngle > 0 && slopeAngle <= 45f)
            {
                movementVelocity = Vector3.ProjectOnPlane(moveDirection, currentGroundNormal).normalized * currentTargetSpeed;
            }
            else
            {
                movementVelocity = moveDirection * currentTargetSpeed;
            }
        }
        else
        {
            movementVelocity = moveDirection * currentTargetSpeed;
        }

        Vector3 finalVelocity = movementVelocity;

        if (isGrounded)
        {
            finalVelocity.y = movementVelocity.y - 4f;
        }
        else
        {
            finalVelocity.y = verticalVelocity;
        }

        rb.linearVelocity = finalVelocity;
    }

    private void ApplyCustomGravity()
    {
        if (!isGrounded)
        {
            float targetGravity = (PaperState && verticalVelocity < 0) ? paperGravity : gravity;
            verticalVelocity += targetGravity * Time.fixedDeltaTime;

            float maxFallSpeed = PaperState ? paperGravity : gravity;
            if (verticalVelocity < maxFallSpeed)
            {
                verticalVelocity = Mathf.Lerp(verticalVelocity, maxFallSpeed, Time.fixedDeltaTime * 6f);
            }
        }
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
                    if (Vector3.Angle(Vector3.up, contact.normal) <= 45f)
                    {
                        groundCheckTimer = 0.25f;
                        currentGroundNormal = contact.normal;
                        return;
                    }
                }
            }
        }
    }



    private void OnCollisionEnter(Collision collision) => EvaluateGroundCollision(collision);
    private void OnCollisionStay(Collision collision) => EvaluateGroundCollision(collision);
}