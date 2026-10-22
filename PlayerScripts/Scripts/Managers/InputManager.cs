/*using UnityEngine;
using UnityEngine.InputSystem;

// [Flags] 속성: 인스펙터에서 다중 선택(Everything, Nothing 등) 가능
[System.Flags]
public enum InputControlType
{
    None = 0,               // 아무것도 선택 안 함
    Movement = 1 << 0,      // 1 (이동)
    Jump = 1 << 1,          // 2 (점프)
    Slide = 1 << 2,         // 4 (슬라이딩 / LShift) [기존 Sprint 대체]
    Crouch = 1 << 3,        // 8 (앉기)
    Click = 1 << 4,         // 16 (클릭)
    Escape = 1 << 5,        // 32 (일시정지)
    PaperState = 1 << 6,    // 64 (종이 변신)
    SlimState = 1 << 7,     // 128 (슬림 변신)
    All = ~0                // 모든 비트 선택 (Everything)
}

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    private PlayerInputSystem playerinput;

    [Header("Input Flags")]
    public bool canMove = true;
    public bool canJump = true;
    public bool canSlide = true;
    public bool canCrouch = true;
    public bool canClick = true;
    public bool canEscape = true;
    public bool canPaperState = true;
    public bool canSlimState = true;

    public Vector2 Movement => canMove ? playerinput.Player.Move.ReadValue<Vector2>() : Vector2.zero;
    public bool JumpPressed => canJump && playerinput.Player.Jump.IsPressed();
    public bool JumpReleased => canJump && playerinput.Player.Jump.WasReleasedThisFrame();
    /// <summary>키를 누른 첫 프레임에만 true. 연타 감지에 사용합니다.</summary>
    public bool JumpTriggered => canJump && playerinput.Player.Jump.WasPressedThisFrame();

    // 슬라이딩 입력 (New Input System의 Sprint 바인딩 또는 레거시 LShift 모두 지원)
    public bool SlideKeyTrigger => canSlide && (playerinput.Player.Sprint.triggered || Input.GetKeyDown(KeyCode.LeftShift));
    public bool SlidePressed => SlideKeyTrigger;
    public bool SlideHeld => canSlide && (playerinput.Player.Sprint.IsPressed() || Input.GetKey(KeyCode.LeftShift));

    // [호환용] 기존 스크립트(VillainLocomotionController1 등)에서 참조 시 에러 방지
    public bool SprintHeld => SlideHeld;

    public bool CrouchTogglePressed => canCrouch && playerinput.Player.Crouch.triggered;
    public bool LeftClickTrigger => canClick && playerinput.Player.Click.triggered;
    public bool EscapeKeyTrigger => canEscape && playerinput.Player.Escape.triggered;
    public bool PaperStateKeyTrigger => canPaperState && playerinput.Player.PaperState.triggered;
    public bool SlimStateKeyTrigger => canSlimState && playerinput.Player.SlimState.triggered;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            playerinput = new PlayerInputSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable() => playerinput.Enable();
    private void OnDisable() => playerinput.Disable();

    public void SetInputEnable(bool isEnabled)
    {
        if (isEnabled) playerinput.Enable();
        else playerinput.Disable();
    }

    // 다중 선택된 항목들을 HasFlag로 검사하여 끄거나 켭니다.
    public void SetSpecificInput(InputControlType inputTypes, bool isEnabled)
    {
        if (inputTypes.HasFlag(InputControlType.Movement)) canMove = isEnabled;
        if (inputTypes.HasFlag(InputControlType.Jump)) canJump = isEnabled;
        if (inputTypes.HasFlag(InputControlType.Slide)) canSlide = isEnabled;
        if (inputTypes.HasFlag(InputControlType.Crouch)) canCrouch = isEnabled;
        if (inputTypes.HasFlag(InputControlType.Click)) canClick = isEnabled;
        if (inputTypes.HasFlag(InputControlType.Escape)) canEscape = isEnabled;
        if (inputTypes.HasFlag(InputControlType.PaperState)) canPaperState = isEnabled;
        if (inputTypes.HasFlag(InputControlType.SlimState)) canSlimState = isEnabled;
    }
}*/

using UnityEngine;
using UnityEngine.InputSystem;

// [Flags] 속성: 인스펙터에서 다중 선택(Everything, Nothing 등) 가능
[System.Flags]
public enum InputControlType
{
    None = 0,               // 아무것도 선택 안 함
    Movement = 1 << 0,      // 1 (이동)
    Jump = 1 << 1,          // 2 (점프)
    Slide = 1 << 2,         // 4 (슬라이딩 / LShift) [기존 Sprint 대체]
    Crouch = 1 << 3,        // 8 (앉기)
    Click = 1 << 4,         // 16 (클릭)
    Escape = 1 << 5,        // 32 (일시정지)
    PaperState = 1 << 6,    // 64 (종이 변신)
    SlimState = 1 << 7,     // 128 (슬림 변신)
    All = ~0                // 모든 비트 선택 (Everything)
}

public class InputManager : MonoBehaviour
{
    public static InputManager Instance;

    private PlayerInputSystem playerinput;

    [Header("Input Flags")]
    public bool canMove = true;
    public bool canJump = true;
    public bool canSlide = true;
    public bool canCrouch = true;
    public bool canClick = true;
    public bool canEscape = true;
    public bool canPaperState = true;
    public bool canSlimState = true;

    [Header("Directional Movement Settings")]
    [Tooltip("W 키 (전진 / Up) 허용")]
    public bool allowW = true;
    [Tooltip("A 키 (좌측 / Left) 허용")]
    public bool allowA = true;
    [Tooltip("S 키 (후진 / Down) 허용")]
    public bool allowS = true;
    [Tooltip("D 키 (우측 / Right) 허용")]
    public bool allowD = true;

    /// <summary>
    /// canMove가 true일 때, 허용된 방향(W, A, S, D)의 입력만 필터링하여 반환합니다.
    /// </summary>
    public Vector2 Movement
    {
        get
        {
            if (!canMove) return Vector2.zero;

            Vector2 raw = playerinput.Player.Move.ReadValue<Vector2>();

            // 좌우 판별 (X축: A / D)
            if (raw.x > 0f && !allowD) raw.x = 0f;
            else if (raw.x < 0f && !allowA) raw.x = 0f;

            // 전후 판별 (Y축: W / S)
            if (raw.y > 0f && !allowW) raw.y = 0f;
            else if (raw.y < 0f && !allowS) raw.y = 0f;

            return raw;
        }
    }

    public bool JumpPressed => canJump && playerinput.Player.Jump.IsPressed();
    public bool JumpReleased => canJump && playerinput.Player.Jump.WasReleasedThisFrame();
    /// <summary>키를 누른 첫 프레임에만 true. 연타 감지에 사용합니다.</summary>
    public bool JumpTriggered => canJump && playerinput.Player.Jump.WasPressedThisFrame();

    // 슬라이딩 입력 (New Input System의 Sprint 바인딩 또는 레거시 LShift 모두 지원)
    public bool SlideKeyTrigger => canSlide && (playerinput.Player.Sprint.triggered || Input.GetKeyDown(KeyCode.LeftShift));
    public bool SlidePressed => SlideKeyTrigger;
    public bool SlideHeld => canSlide && (playerinput.Player.Sprint.IsPressed() || Input.GetKey(KeyCode.LeftShift));

    // 앉기/버티기 입력 (canCrouch가 true일 때만 New Input System 바인딩 또는 KeyCode.C 감지)
    public bool CrouchHeld => canCrouch && (playerinput.Player.Crouch.IsPressed() || Input.GetKey(KeyCode.C));

    // [호환용] 기존 스크립트(VillainLocomotionController1 등)에서 참조 시 에러 방지
    public bool SprintHeld => SlideHeld;

    public bool CrouchTogglePressed => canCrouch && playerinput.Player.Crouch.triggered;
    public bool LeftClickTrigger => canClick && playerinput.Player.Click.triggered;
    public bool EscapeKeyTrigger => canEscape && playerinput.Player.Escape.triggered;
    public bool PaperStateKeyTrigger => canPaperState && playerinput.Player.PaperState.triggered;
    public bool SlimStateKeyTrigger => canSlimState && playerinput.Player.SlimState.triggered;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            playerinput = new PlayerInputSystem();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable() => playerinput.Enable();
    private void OnDisable() => playerinput.Disable();

    public void SetInputEnable(bool isEnabled)
    {
        if (isEnabled) playerinput.Enable();
        else playerinput.Disable();
    }

    // 다중 선택된 항목들을 HasFlag로 검사하여 끄거나 켭니다.
    public void SetSpecificInput(InputControlType inputTypes, bool isEnabled)
    {
        if (inputTypes.HasFlag(InputControlType.Movement)) canMove = isEnabled;
        if (inputTypes.HasFlag(InputControlType.Jump)) canJump = isEnabled;
        if (inputTypes.HasFlag(InputControlType.Slide)) canSlide = isEnabled;
        if (inputTypes.HasFlag(InputControlType.Crouch)) canCrouch = isEnabled;
        if (inputTypes.HasFlag(InputControlType.Click)) canClick = isEnabled;
        if (inputTypes.HasFlag(InputControlType.Escape)) canEscape = isEnabled;
        if (inputTypes.HasFlag(InputControlType.PaperState)) canPaperState = isEnabled;
        if (inputTypes.HasFlag(InputControlType.SlimState)) canSlimState = isEnabled;
    }

    /// <summary>
    /// W, A, S, D 이동 입력을 개별적으로 허용하거나 차단합니다.
    /// </summary>
    public void SetDirectionalInput(bool w, bool a, bool s, bool d)
    {
        allowW = w;
        allowA = a;
        allowS = s;
        allowD = d;
    }

    /// <summary>
    /// 모든 방향(W, A, S, D) 입력을 다시 기본 상태(허용)로 초기화합니다.
    /// </summary>
    public void ResetDirectionalInput()
    {
        allowW = true;
        allowA = true;
        allowS = true;
        allowD = true;
    }
}