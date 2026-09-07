using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 카메라는 고정한 채로 화면 위의 원을 마우스/WASD로 움직여서
    /// 목표 오브젝트를 원 안에 담으면 Output을 실행하는 조준 시스템.
    ///
    /// ──────────────────────────────────────────────────────────────────
    /// [CameraScopeAimOutput 과의 차이]
    ///  - CameraScopeAimOutput : 원은 화면 중앙 고정, 카메라를 움직임
    ///  - CircleScopeAimOutput : 카메라는 고정,       원을 화면 위에서 움직임
    ///
    /// [원 이동 방식]
    ///  ① Enable Mouse Control + Mouse Follows Cursor = true
    ///     → 마우스 커서 위치에 원이 직접 따라옴 (커서 표시됨)
    ///  ② Enable Mouse Control + Mouse Follows Cursor = false
    ///     → 마우스 델타(이동량)로 원을 밀어냄 (커서 잠금)
    ///  ③ Enable WASD Control
    ///     → WASD로 원을 상하좌우 이동, E/Q로 위아래 (화면 2D이므로 E/Q 무시)
    ///  ④ 둘 다 켜면 동시에 사용 가능
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class CircleScopeAimOutput : ProcessBase
    {
        // ══════════════════════════════════════════════════════
        // Inspector 필드
        // ══════════════════════════════════════════════════════

        [Header("─ 원 이동 조작 ─")]
        [Tooltip("마우스로 원을 조작합니다.")]
        [SerializeField] private bool enableMouseControl = true;

        [Tooltip("true  → 마우스 커서 위치에 원이 직접 따라옴 (커서가 화면에 보임)\n" +
                 "false → 마우스 이동량(델타)으로 원을 밀어냄 (커서 잠금)")]
        [SerializeField] private bool mouseFollowsCursor = false;

        [Tooltip("Mouse Follows Cursor = false 일 때 델타 감도")]
        [SerializeField] private float mouseSensitivity = 5f;

        [Tooltip("WASD 키보드로 원을 조작합니다.")]
        [SerializeField] private bool enableWASDControl = true;

        [Tooltip("WASD 이동 속도 (pixels/sec)")]
        [SerializeField] private float wasdSpeed = 400f;

        [Tooltip("원이 화면 경계 밖으로 나가지 못하게 제한합니다.")]
        [SerializeField] private bool clampToScreen = true;

        [Header("─ UI : 스코프 원 ─")]
        [Tooltip("화면에 표시할 원형 이미지 (Image 컴포넌트).\n" +
                 "Canvas에 배치한 뒤 여기에 드래그하세요.")]
        [SerializeField] private Image scopeCircleImage;

        [Tooltip("평상시 원 색상")]
        [SerializeField] private Color colorDefault = Color.white;

        [Tooltip("목표물이 원 안에 들어왔을 때 원 색상")]
        [SerializeField] private Color colorOnTarget = new Color(0.2f, 1f, 0.3f, 1f);

        [Header("─ UI : 포착 진행 표시 (선택) ─")]
        [Tooltip("포착 유지 진행률을 보여줄 Image.\n" +
                 "Image Type을 Filled + Radial 360으로 설정하세요.\n비워두면 사용 안 함.")]
        [SerializeField] private Image holdProgressImage;

        [Header("─ 목표 오브젝트 ─")]
        [Tooltip("원 안에 담아야 할 오브젝트의 Renderer.\n" +
                 "바운딩박스 8개 꼭짓점이 전부 원 안에 들어와야 포착 성공.")]
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("포착 유지 시간 (초). 0이면 들어오자마자 즉시 발동.")]
        [SerializeField] private float holdDuration = 1f;

        [Header("─ 성공 시 실행할 Output ─")]
        [Tooltip("포착 성공 시 실행할 ProcessBase Output 목록.\n비워두면 IsOn = true만 설정합니다.")]
        [SerializeField] private List<ProcessBase> outputsOnSuccess = new List<ProcessBase>();

        [Header("─ 취소 설정 ─")]
        [Tooltip("스코프 모드 취소 키. None으로 설정하면 취소 불가.")]
        [SerializeField] private KeyCode cancelKey = KeyCode.Escape;

        [Tooltip("취소 시 IsOn = true로 설정할지 여부.\ntrue: 시퀀스 계속 / false: 시퀀스 멈춤.")]
        [SerializeField] private bool isOnWhenCancelled = false;

        // ══════════════════════════════════════════════════════
        // 내부 상태
        // ══════════════════════════════════════════════════════
        private bool    _isActive     = false;
        private float   _holdTimer    = 0f;
        private Vector2 _circlePosScreen; // 현재 원의 스크린 좌표
        private Canvas  _parentCanvas;
        private Camera  _mainCamera;

        // ──────────────────────────────────────────────────────
        private void Awake()
        {
            if (scopeCircleImage != null)
            {
                scopeCircleImage.gameObject.SetActive(false);
                _parentCanvas = scopeCircleImage.GetComponentInParent<Canvas>();
            }

            if (holdProgressImage != null)
            {
                holdProgressImage.fillAmount = 0f;
                holdProgressImage.gameObject.SetActive(false);
            }
        }

        // ──────────────────────────────────────────────────────
        public override void Execute()
        {
            if (scopeCircleImage == null)
            {
                Debug.LogWarning($"[{gameObject.name}] scopeCircleImage가 비어 있습니다.");
                IsOn = true; return;
            }
            if (targetRenderer == null)
            {
                Debug.LogWarning($"[{gameObject.name}] targetRenderer가 비어 있습니다.");
                IsOn = true; return;
            }

            IsOn = false;
            _isActive  = true;
            _holdTimer = 0f;

            _mainCamera = Camera.main;

            // 원을 화면 중앙에서 시작
            _circlePosScreen = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            ApplyCircleScreenPosition();

            // UI 표시
            scopeCircleImage.color = colorDefault;
            scopeCircleImage.gameObject.SetActive(true);

            if (holdProgressImage != null)
            {
                holdProgressImage.fillAmount = 0f;
                holdProgressImage.gameObject.SetActive(true);
            }

            // 플레이어 입력 잠금 (카메라는 고정, 원만 움직임)
            if (InputManager.Instance != null)
                InputManager.Instance.SetInputEnable(false);

            // 커서 모드 설정
            if (enableMouseControl && mouseFollowsCursor)
            {
                // 커서 위치로 원이 직접 따라가는 모드: 커서를 보여줌
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible   = true;
            }
            else
            {
                // 델타 모드 or WASD 전용: 커서 잠금
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[CircleScopeAimOutput]</color> 원 조준 모드 시작. 목표: {targetRenderer.name}");
#endif
        }

        // ──────────────────────────────────────────────────────
        private void Update()
        {
            if (!_isActive) return;

            // ── 취소 키 ──
            if (cancelKey != KeyCode.None && Input.GetKeyDown(cancelKey))
            {
                ExitScope(success: false);
                return;
            }

            // ── 원 위치 업데이트 ──
            MoveCircle();

            // 원 위치 화면에 적용
            ApplyCircleScreenPosition();

            // ── 포착 판정 ──
            bool onTarget = IsTargetFullyInCircle();
            scopeCircleImage.color = onTarget ? colorOnTarget : colorDefault;

            if (onTarget)
            {
                _holdTimer += Time.deltaTime;

                if (holdProgressImage != null)
                    holdProgressImage.fillAmount = Mathf.Clamp01(_holdTimer / Mathf.Max(holdDuration, 0.001f));

                if (_holdTimer >= holdDuration)
                    ExitScope(success: true);
            }
            else
            {
                _holdTimer = 0f;
                if (holdProgressImage != null)
                    holdProgressImage.fillAmount = 0f;
            }
        }

        // ──────────────────────────────────────────────────────
        /// <summary>
        /// 마우스 / WASD 입력으로 원의 스크린 좌표를 갱신합니다.
        /// </summary>
        private void MoveCircle()
        {
            Vector2 delta = Vector2.zero;

            // ── 마우스 조작 ──
            if (enableMouseControl)
            {
                if (mouseFollowsCursor)
                {
                    // 커서 위치에 원을 직접 맞춤 (델타 누적 없음)
                    _circlePosScreen = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                }
                else
                {
                    // 마우스 이동량으로 밀기
                    delta.x += Input.GetAxis("Mouse X") * mouseSensitivity;
                    delta.y += Input.GetAxis("Mouse Y") * mouseSensitivity;
                }
            }

            // ── WASD 조작 (레거시 Input: InputManager 잠금과 무관하게 동작) ──
            if (enableWASDControl)
            {
                delta.x += Input.GetAxis("Horizontal") * wasdSpeed * Time.deltaTime;
                delta.y += Input.GetAxis("Vertical")   * wasdSpeed * Time.deltaTime;
            }

            // MouseFollowsCursor 모드가 아닐 때만 delta를 누적
            if (!(enableMouseControl && mouseFollowsCursor))
                _circlePosScreen += delta;

            // ── 화면 경계 클램프 ──
            if (clampToScreen)
            {
                float canvasScale  = (_parentCanvas != null) ? _parentCanvas.scaleFactor : 1f;
                float halfW = scopeCircleImage.rectTransform.rect.width  * 0.5f * canvasScale;
                float halfH = scopeCircleImage.rectTransform.rect.height * 0.5f * canvasScale;

                _circlePosScreen.x = Mathf.Clamp(_circlePosScreen.x, halfW, Screen.width  - halfW);
                _circlePosScreen.y = Mathf.Clamp(_circlePosScreen.y, halfH, Screen.height - halfH);
            }
        }

        // ──────────────────────────────────────────────────────
        /// <summary>
        /// 스크린 좌표를 RectTransform 위치에 적용합니다.
        /// Screen Space – Overlay 캔버스에서 position.xy == 스크린 픽셀 좌표.
        /// </summary>
        private void ApplyCircleScreenPosition()
        {
            scopeCircleImage.rectTransform.position = new Vector3(
                _circlePosScreen.x, _circlePosScreen.y, 0f);

            // holdProgressImage가 있으면 같은 위치로 이동
            if (holdProgressImage != null)
                holdProgressImage.rectTransform.position = scopeCircleImage.rectTransform.position;
        }

        // ──────────────────────────────────────────────────────
        private void ExitScope(bool success)
        {
            _isActive = false;

            // UI 숨김
            scopeCircleImage.gameObject.SetActive(false);
            if (holdProgressImage != null)
                holdProgressImage.gameObject.SetActive(false);

            // 플레이어 입력 복구
            if (InputManager.Instance != null)
                InputManager.Instance.SetInputEnable(true);

            // 커서 복구 (게임 기본값)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;

            if (success)
            {
                foreach (var output in outputsOnSuccess)
                    if (output != null) output.Execute();

                IsOn = true;

#if UNITY_EDITOR
                Debug.Log($"<color=green>[CircleScopeAimOutput]</color> 포착 성공! Output 실행.");
#endif
            }
            else
            {
                if (isOnWhenCancelled) IsOn = true;

#if UNITY_EDITOR
                Debug.Log($"<color=orange>[CircleScopeAimOutput]</color> 취소됨.");
#endif
            }
        }

        // ──────────────────────────────────────────────────────
        /// <summary>
        /// 목표 Renderer의 바운딩박스 8개 꼭짓점이
        /// 현재 원 위치와 반지름 기준으로 모두 원 안에 있는지 검사합니다.
        /// </summary>
        private bool IsTargetFullyInCircle()
        {
            if (_mainCamera == null || targetRenderer == null) return false;

            // 원의 스크린 좌표 & 반지름
            float canvasScale    = (_parentCanvas != null) ? _parentCanvas.scaleFactor : 1f;
            float circleRadiusPx = scopeCircleImage.rectTransform.rect.width * 0.5f * canvasScale;
            Vector2 circleCenter = _circlePosScreen;

            Vector3[] corners = GetBoundsCorners(targetRenderer.bounds);

            foreach (Vector3 corner in corners)
            {
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(corner);

                // 카메라 뒤에 있으면 즉시 false
                if (screenPos.z < 0f) return false;

                float dist = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), circleCenter);
                if (dist > circleRadiusPx) return false;
            }

            return true;
        }

        private static Vector3[] GetBoundsCorners(Bounds b)
        {
            Vector3 c = b.center;
            Vector3 e = b.extents;
            return new Vector3[]
            {
                c + new Vector3(-e.x, -e.y, -e.z),
                c + new Vector3(-e.x, -e.y,  e.z),
                c + new Vector3(-e.x,  e.y, -e.z),
                c + new Vector3(-e.x,  e.y,  e.z),
                c + new Vector3( e.x, -e.y, -e.z),
                c + new Vector3( e.x, -e.y,  e.z),
                c + new Vector3( e.x,  e.y, -e.z),
                c + new Vector3( e.x,  e.y,  e.z),
            };
        }

        // ──────────────────────────────────────────────────────
        public new void Reset()
        {
            base.Reset();
            _isActive = false;
        }

        // ──────────────────────────────────────────────────────
        // Gizmos: 에디터에서 목표물 바운딩박스 시각화
        // ──────────────────────────────────────────────────────
        private void OnDrawGizmos()
        {
            if (targetRenderer == null) return;
            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.35f);
            Gizmos.DrawWireCube(targetRenderer.bounds.center, targetRenderer.bounds.size);
        }
    }
}
