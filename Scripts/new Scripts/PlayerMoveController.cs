using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using System.Collections.Generic;

public class PlayerMoveController : MonoBehaviour
{
    private Animator anim;
    private CharacterController controller;
    public CinemachineCamera cam;
    public GameObject PlayerObject; // 몸을 돌릴 대상 (자식 오브젝트)

    [Header("Movement Settings")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 5.0f;
    public float rotationSpeed = 10.0f;

    [Header("Slim Movement Settings")]
    public float slimWalkSpeed = 2.0f;
    public float slimRunSpeed = 3.0f;
   

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -15.0f;
    public float groundDistance = 0.3f;
    public float sphereRadius = 0.4f;
    public LayerMask groundMask;
    public LayerMask platformMask;

    [Header("Special State Settings")]
    public float duringPaper = 2.0f; // 종이 지속 시간
    private Coroutine currentCoroutine;
    private float originalHeight;
    private Vector3 originalCenter;
    private float originalRadius;
    private bool isPaper = false;
    private bool isSlim = false;
    private float beforeX, beforeZ;

    private Vector3 playerVelocity;
    private bool isGrounded;

    // --- 줄타기(Rope) 상태 ---
    [Header("Rope Walk State (자동 제어 - 건드리지 마세요)")]
    public bool isOnRope = false;               // 현재 Rope 위인가
    private float ropeWalkSpeedLimit = 1.5f;    // Rope 위에서의 속도 제한
    private RopeBalanceGimmick activeRopeGimmick; // 현재 활성화된 기믹 참조

    void Start()
    {
        anim = GetComponent<Animator>();
        controller = this.GetComponent<CharacterController>();

        originalHeight = controller.height;
        originalCenter = controller.center;
        originalRadius = controller.radius;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (groundMask == 0) groundMask = ~0;
    }

    void Update()
    {
        // 페이퍼 상태 중이면 Update의 일반 이동 / 타기 처리 생략
        if (isPaper) return;

        isGrounded = GroundCheck();

        if (isGrounded && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
            anim.SetBool("Jump", false);
        }

        HandleMovement();

        // 줄타기 중에는 점프/변신 불가
        if (!isOnRope)
        {
            if(isSlim == false)
            {
                HandleJump();
            }
        
        
            if (Input.GetKeyDown(KeyCode.F))
            {
                DoPaper();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (isSlim == false)
                {
                    SlimPlayer();
                }
                else
                {
                    UnSlimPlayer();
                }
            }
        } // end if (!isOnRope)

        // 수직 속도 적용 및 최종 이동
        playerVelocity.y += gravity * Time.deltaTime;
        // 수정 후 (안전 장치 추가)
        if (controller.enabled)
        {
            controller.Move(playerVelocity * Time.deltaTime);
        }
    }

    void HandleMovement()
    {
        // [줄타기 모드] Rope 위에서는 전진만 허용, 속도 제한
        if (isOnRope)
        {
            HandleRopeMovement();
            return;
        }

        // [일반 모드] Slim 상태가 아닐 때만 카메라 방면으로 캐릭터(transform)를 회전을 돌림
        if (!isSlim)
        {
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0;

            if (cameraForward.sqrMagnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
            }
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v);

        if (inputDir.magnitude >= 0.1f)
        {
            if (inputDir.magnitude > 1) inputDir.Normalize();
            bool isRunning = Input.GetKey(KeyCode.LeftShift);

            // Slim 여부에 따른 속도 결정
            float currentSpeed = isSlim ? (isRunning ? slimRunSpeed : slimWalkSpeed) : (isRunning ? runSpeed : walkSpeed);

            Vector3 moveDir;

            // [수정] 이동 방향 계산: Slim 상태일 때는 옆으로만 이동하는 등의 제약이 필요할 수 있으나 
            // 일단 기본 이동 방향을 적용하되, 애니메이션 파라미터 전달 방식을 유지합니다.
            if(isSlim == true)
            {
                moveDir = (transform.forward * v ).normalized;
            }
            else
            {
                moveDir = (transform.forward * v + transform.right * h).normalized;
            }

                controller.Move(moveDir * currentSpeed * Time.deltaTime);

            float speedMultiplier = isRunning ? 1.0f : 0.5f;
            anim.SetFloat("MoveX", h * speedMultiplier, 0.1f, Time.deltaTime);
            anim.SetFloat("MoveZ", v * speedMultiplier, 0.1f, Time.deltaTime);
        }
        else
        {
            StopCharacter();
        }
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
            anim.SetBool("Jump", true);
        }
    }

    // --- 종이(Paper) 관련 로직 ---
    public void DoPaper()
    {
        if (currentCoroutine != null) return;
        currentCoroutine = StartCoroutine(PaperActive());
        anim.SetBool("SideStep", true);
    }

    IEnumerator PaperActive()
    {
        isPaper = true;
        beforeX = transform.localEulerAngles.x;
        beforeZ = transform.localEulerAngles.z;

        float currentGravity = -1.0f; // 팔랑거리는 중력
        float verticalVel = 0f;

        controller.height = 0.05f;
        controller.center = new Vector3(0, 0.05f, 0);

        float elasp = 0.0f;
        float startY = transform.localEulerAngles.y;
        Vector3 paperVelocity = Vector3.zero;

        if (anim != null) anim.speed = 0f;

        // 땅에 닿거나 3초가 지날 때까지 팔랑거림
        while (elasp < 3 && !GroundCheck())
        {
            elasp += Time.deltaTime;
            verticalVel += currentGravity * Time.deltaTime;

            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 inputDir = new Vector3(h, 0, v);

            if (inputDir.sqrMagnitude > 0.01f)
            {
                float targetRotation = Mathf.Atan2(h, v) * Mathf.Rad2Deg + Camera.main.transform.eulerAngles.y;
                Vector3 targetDir = Quaternion.Euler(0.0f, targetRotation, 0.0f) * Vector3.forward;
                paperVelocity = Vector3.Lerp(paperVelocity, targetDir * (walkSpeed * 2f), Time.deltaTime * 3.0f);
            }
            else
            {
                paperVelocity = Vector3.Lerp(paperVelocity, Vector3.zero, Time.deltaTime * 1.5f);
            }

            // 회전 시각화 (팔랑거림)
            float flutter = Mathf.Sin(Time.time * 5f) * 7f;
            transform.localEulerAngles = new Vector3(-90f + flutter + (v * 25f), startY, beforeZ - flutter + (h * 25f));

            Vector3 motion = (paperVelocity + new Vector3(0, verticalVel, 0)) * Time.deltaTime;
            controller.Move(motion);

            yield return null;
        }

        PaperInActive();
    }

    public void PaperInActive()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(WakeUpRoutine());
        anim.SetBool("SideStep", false);
    }

    IEnumerator WakeUpRoutine()
    {
        yield return new WaitForSeconds(duringPaper);

        controller.enabled = false;
        transform.localEulerAngles = new Vector3(beforeX, transform.localEulerAngles.y, beforeZ);
        controller.height = originalHeight;
        controller.center = originalCenter;
        transform.position += Vector3.up * 0.1f;
        controller.enabled = true;

        isPaper = false;
        playerVelocity.y = -2f;
        if (anim != null) anim.speed = 1f;

        currentCoroutine = null;
    }

    // --- 슬림(Slim) 관련 로직 ---
    public void SlimPlayer()
    {
        if (currentCoroutine == null && isGrounded && !isSlim)
        {
            currentCoroutine = StartCoroutine(SlimActivate(1f));
        }
    }

    IEnumerator SlimActivate(float duration)
    {
        float elasp = 0.0f;
        Quaternion startRotation = PlayerObject.transform.localRotation;
        Quaternion targetRotation = Quaternion.Euler(0, 90, 0);

        while (elasp < duration)
        {
            elasp += Time.deltaTime;
            float smoothPercent = Mathf.SmoothStep(0, 1, elasp / duration);
            PlayerObject.transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, smoothPercent);
            yield return null;
        }

        controller.radius = 0.1f;
        isSlim = true;
        currentCoroutine = null;
    }

    public void UnSlimPlayer()
    {
        if (currentCoroutine == null && isGrounded && isSlim)
        {
            if (CanUnSlim()) currentCoroutine = StartCoroutine(SlimUnActivate(1f));
        }
    }

    IEnumerator SlimUnActivate(float duration)
    {
        float elasp = 0.0f;
        Quaternion startRotation = PlayerObject.transform.localRotation;
        Quaternion targetRotation = Quaternion.Euler(0, 0, 0);

        while (elasp < duration)
        {
            elasp += Time.deltaTime;
            float smoothPercent = Mathf.SmoothStep(0, 1, elasp / duration);
            PlayerObject.transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, smoothPercent);
            yield return null;
        }

        controller.radius = originalRadius;
        isSlim = false;
        currentCoroutine = null;
    }

    private bool CanUnSlim()
    {
        float checkRadius = originalRadius * 0.9f;
        Vector3 bottom = transform.position + Vector3.up * (checkRadius + 0.2f);
        Vector3 top = transform.position + Vector3.up * (originalHeight - checkRadius);
        Collider[] colliders = Physics.OverlapCapsule(bottom, top, checkRadius, groundMask);

        foreach (var hit in colliders)
        {
            if (hit.gameObject != gameObject && hit.transform.root != transform) return false;
        }
        return true;
    }

    bool GroundCheck()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * (sphereRadius);
        return Physics.SphereCast(rayOrigin, sphereRadius, Vector3.down, out _, groundDistance, groundMask | platformMask);
    }

    void StopCharacter()
    {
        anim.SetFloat("MoveX", 0, 0.1f, Time.deltaTime);
        anim.SetFloat("MoveZ", 0, 0.1f, Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 rayOrigin = transform.position + Vector3.up * (sphereRadius);
        Gizmos.DrawWireSphere(rayOrigin + Vector3.down * groundDistance, sphereRadius);
    }

    // ─────────────────────────────────────────────
    //  줄타기(Rope) 전용 이동 처리
    // ─────────────────────────────────────────────

    /// <summary>
    /// Rope 위에서의 이동 처리.
    /// - 전진(V축)만 허용, 속도를 ropeWalkSpeedLimit으로 제한
    /// - 좌우(H축)는 RopeBalanceGimmick이 균형 보정으로 사용
    /// - 캐릭터는 항상 Rope 방향을 바라봄
    /// </summary>
    private void HandleRopeMovement()
    {
        float v = Input.GetAxis("Vertical");

        // 전진/후진만 허용 (좌우 이동 없음)
        Vector3 moveDir = transform.forward * v;

        if (Mathf.Abs(v) > 0.1f)
        {
            controller.Move(moveDir.normalized * ropeWalkSpeedLimit * Time.deltaTime);
            // 애니메이션: 전진만 표시
            anim.SetFloat("MoveX", 0, 0.1f, Time.deltaTime);
            anim.SetFloat("MoveZ", v * 0.5f, 0.1f, Time.deltaTime);
        }
        else
        {
            StopCharacter();
        }
    }

    /// <summary>
    /// RopeBalanceGimmick이 호출 - 줄타기 모드 진입
    /// </summary>
    public void EnterRopeWalk(float speedLimit, RopeBalanceGimmick gimmick)
    {
        isOnRope = true;
        ropeWalkSpeedLimit = speedLimit;
        activeRopeGimmick = gimmick;
        Debug.Log("[PlayerMoveController] 줄타기 모드 진입");
    }

    /// <summary>
    /// RopeBalanceGimmick이 호출 - 줄타기 모드 해제
    /// </summary>
    public void ExitRopeWalk()
    {
        isOnRope = false;
        activeRopeGimmick = null;
        Debug.Log("[PlayerMoveController] 줄타기 모드 해제");
    }
}