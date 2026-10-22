using System.Collections;
using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Transform characterVisual;
    [SerializeField] private Animator anim;
    [SerializeField] private PlayerFaceMaterial faceStance;
    [SerializeField] private GameObject sphereObject;

    [Header("Visual Controllers")]
    [SerializeField] private PlayerColorController colorController;

    [Header("Paper Sway Settings")]
    [SerializeField] private float swayAmount = 15f;
    [SerializeField] private float swaySpeed = 5f;
    [SerializeField] private Vector3 swayPivot = new Vector3(0f, 0f, 1f);
    [SerializeField] private float visualTiltAngle = 30.0f;

    [Header("Wind Roll Settings")]
    [SerializeField] private GameObject rollingVisualPrefab;
    [SerializeField] private Animator rollingAnim;

    [Header("Flutter Berry Settings")]
    [SerializeField] private GameObject FlutterVisualPrefab;

    private float swayTimer = 0f;
    private float swayWeight = 0f;

    private bool isPaper;
    private bool isSlim;
    private bool isSlimCompletely;
    private bool isWindRolling;

    private bool isGrounded; // ★ 접지 상태 추적 변수 추가
    private bool isSplineMoving = false;
    private float ropeTiltAngle = 0f;

    // ★ 카메라 마주보기 제어 변수
    private bool isFacingCamera = false;
    private Camera mainCam;

    public Transform CharacterVisual => characterVisual;

    private void Start()
    {
        mainCam = Camera.main;
    }

    /// <summary>
    /// 외부(컷씬 노드 등)에서 카메라를 마주보게 하거나 해제할 때 호출합니다.
    /// </summary>
    public void SetFacingCamera(bool enable)
    {
        isFacingCamera = enable;

        // 해제 시 기본 상태로 복구
        if (!enable && characterVisual != null)
        {
            characterVisual.localRotation = Quaternion.identity;
        }
    }

    public void UpdateStates(bool paper, bool slim, bool completelySlim, bool windRolling)
    {
        isPaper = paper;
        isSlim = slim;
        isSlimCompletely = completelySlim;
        isWindRolling = windRolling;
    }

    public void ChangeColorByState(PlayerConditionState state)
    {
        if (colorController != null) colorController.ChangeColorByState(state);
    }

    public void SetWalkAnim(bool isWalk) { if (anim) anim.SetBool("IsWalk", isWalk); }
    public void SetCrouchAnim(bool isCrouch) { if (anim) anim.SetBool("IsCrouch", isCrouch); }
    public void SetHangOnAnim(bool isHangOn) { if (anim) anim.SetBool("IsHangOn", isHangOn); }
    public void SetJumpAnim() { if (anim) anim.SetTrigger("IsJump"); }

    public void SetWindRollAnim(bool isActive)
    {
        if (characterVisual) characterVisual.gameObject.SetActive(!isActive);
        if (rollingVisualPrefab) rollingVisualPrefab.SetActive(isActive);

        if (rollingAnim != null)
        {
            rollingAnim.SetBool("IsRolling", isActive);

            // 만약 Animator Controller에 IsReverseRolling 파라미터가 아직 남아있다면
            // 의도치 않은 전이를 방지하기 위해 항상 false로 고정해두거나 파라미터를 삭제하세요.
            rollingAnim.SetBool("IsReverseRolling", false);
        }
    }

    public void UpdateFace(bool brace, bool sliding, bool grounded, Vector3 velocity)
    {
        if (faceStance == null) return;
        bool isMovingHorizontally = new Vector3(velocity.x, 0f, velocity.z).sqrMagnitude > 0.1f;

        if (brace) faceStance.SetStareFace();
        else if (isPaper && (sliding || isMovingHorizontally)) faceStance.SetSurprizeFace();
        else if (isPaper) faceStance.SetEmbarassedFace();
        else if (isSlim) faceStance.SetStareFace();
        else if (!grounded) faceStance.SetKawaiiFace();
        else faceStance.SetNormalFace();
    }

    public void SetSurpriseFace() { if (faceStance) faceStance.SetSurprizeFace(); }
    public void SetNormalFace() { if (faceStance) faceStance.SetNormalFace(); }

    public void SetSplineMoving(bool moving) { isSplineMoving = moving; }

    public void HandleVisualsLateUpdate(float deltaTime, bool isGrounded, bool isRidingRail)
    {
        this.isGrounded = isGrounded; // ★ 접지 상태 동기화

        if (isRidingRail || isSplineMoving) return;

        // 1. Flutter Visual On/Off 조건 판별
        bool shouldShowFlutter = isPaper && !isGrounded && !isWindRolling;

        if (FlutterVisualPrefab != null)
        {
            if (FlutterVisualPrefab.activeSelf != shouldShowFlutter)
            {
                FlutterVisualPrefab.SetActive(shouldShowFlutter);
            }
        }

        // 2. characterVisual 활성화 관리
        if (characterVisual != null && !isWindRolling)
        {
            bool shouldShowCharacter = !shouldShowFlutter;
            if (characterVisual.gameObject.activeSelf != shouldShowCharacter)
            {
                characterVisual.gameObject.SetActive(shouldShowCharacter);
            }
        }

        // 3. 기존 Sway 및 구체 비주얼 처리
        if (!isGrounded && isPaper) swayWeight = Mathf.MoveTowards(swayWeight, 1f, deltaTime * 3f);
        else swayWeight = Mathf.MoveTowards(swayWeight, 0f, deltaTime * 8f);

        if (isPaper)
        {
            if (sphereObject && !sphereObject.activeSelf) sphereObject.SetActive(true);
            swayTimer += deltaTime * swaySpeed;
        }
        else
        {
            if (sphereObject && sphereObject.activeSelf) sphereObject.SetActive(false);
        }

        ApplyFinalLocalVisuals();
    }

    public void ResetRotationAndVisuals()
    {
        swayWeight = 0f;
        isFacingCamera = false; // 리셋 시 마주보기 해제
        if (characterVisual != null)
        {
            characterVisual.localPosition = Vector3.zero;
            characterVisual.localRotation = Quaternion.identity;
            characterVisual.gameObject.SetActive(true);
        }
        if (sphereObject) sphereObject.SetActive(false);
        if (FlutterVisualPrefab) FlutterVisualPrefab.SetActive(false);
    }

    public void StopRotationTransition() { }
    public void TriggerStateChangeTransition() { }

    public void SetRopeTilt(float angle)
    {
        ropeTiltAngle = angle;
        ApplyFinalLocalVisuals();
    }

    private void ApplyFinalLocalVisuals()
    {
        if (characterVisual == null) return;

        // ★ [수정] isPaper 상태이면서 땅에 닿아있을 때는 카메라 바라보기를 예외 처리
        bool isPaperOnGround = isPaper && isGrounded;

        if (isFacingCamera && !isPaperOnGround)
        {
            if (mainCam == null) mainCam = Camera.main;

            if (mainCam != null)
            {
                Vector3 lookDir = mainCam.transform.position - characterVisual.position;
                lookDir.y = 0f; // 수평 회전만 유지

                if (lookDir.sqrMagnitude > 0.001f)
                {
                    characterVisual.rotation = Quaternion.LookRotation(lookDir);
                }
            }
            return; // 카메라 고정 연산 후 기존 Sway/Tilt 연산 무시
        }

        // 기존 비주얼 회전 연산 (isPaperOnGround일 경우 이쪽 로직 실행)
        float currentTilt = isSlimCompletely ? 0.0f : visualTiltAngle;

        if (isPaper)
        {
            float angle = Mathf.Sin(swayTimer) * swayAmount * swayWeight;
            Quaternion swayRot = Quaternion.Euler(currentTilt, angle, 0f);
            Vector3 swayPos = swayPivot + (swayRot * -swayPivot);

            characterVisual.localPosition = Vector3.Lerp(Vector3.zero, swayPos, swayWeight);
            characterVisual.localRotation = Quaternion.Slerp(Quaternion.Euler(10f, 0f, 0f), swayRot, swayWeight);
        }
        else if (isSlim)
        {
            characterVisual.localPosition = Vector3.zero;
            characterVisual.localRotation = Quaternion.Euler(0, -currentTilt, 0);
        }
        else
        {
            characterVisual.localPosition = Vector3.zero;
            characterVisual.localRotation = Quaternion.Euler(0, 0, ropeTiltAngle);
        }
    }
}