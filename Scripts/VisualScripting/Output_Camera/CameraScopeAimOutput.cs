using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 화면 중앙의 원 안에 목표 오브젝트를 담으면 Output을 실행하는 스코프 조준 시스템.
    ///
    /// ──────────────────────────────────────────────────────────────────
    /// [동작 방식]
    ///  1. Execute() 호출 시 플레이어 입력 잠금, 스코프 UI 표시
    ///  2. 지정한 Cinemachine 가상 카메라가 우선순위를 받아 활성화
    ///  3. 마우스로 해당 카메라를 자유롭게 회전
    ///  4. 목표 오브젝트(Renderer)의 바운딩박스 8개 꼭짓점이
    ///     전부 원 안에 들어오면 "포착 상태"로 전환
    ///  5. holdDuration 동안 포착 유지 시 → 지정 Output 실행 & IsOn = true
    ///
    /// [설정 방법]
    ///  ① 씬에 빈 GameObject + CinemachineCamera 컴포넌트를 추가 (스코프 전용 카메라)
    ///  ② Canvas에 원형 Image를 배치하고 scopeCircleImage 필드에 연결
    ///  ③ 필요하면 holdProgressImage(채우기 타입 Image)도 연결하면 진행 표시
    ///  ④ 목표 오브젝트의 Renderer를 targetRenderer에 연결
    ///  ⑤ 성공 시 실행할 Output들을 outputsOnSuccess에 추가
    /// ──────────────────────────────────────────────────────────────────
    /// </summary>
    public class CameraScopeAimOutput : ProcessBase
    {
        // ══════════════════════════════════════════════════════
        // Inspector 필드
        // ══════════════════════════════════════════════════════

        [Header("─ 스코프 카메라 ─")]
        [Tooltip("스코프 모드에서 활성화할 Cinemachine 가상 카메라.\n" +
                 "씬에 빈 오브젝트 + CinemachineCamera 컴포넌트를 추가해 연결하세요.")]
        [SerializeField] private CinemachineCamera scopeVirtualCamera;

        [Tooltip("스코프 카메라에 부여할 Priority. 기존 카메라보다 높아야 합니다.")]
        [SerializeField] private int scopeCameraPriority = 100;

        [Tooltip("스코프 종료 후 복귀할 이전 카메라. 비워두면 기존 Priority를 복원합니다.")]
        [SerializeField] private CinemachineCamera previousCamera;

        [Tooltip("마우스 좌우 감도")]
        [SerializeField] private float sensitivityX = 2f;

        [Tooltip("마우스 상하 감도")]
        [SerializeField] private float sensitivityY = 2f;

        [Tooltip("상하 회전 최대 각도 (±)")]
        [SerializeField] private float pitchClamp = 75f;

        [Tooltip("WASD 카메라 이동 속도 (m/s). 0이면 이동 불가.")]
        [SerializeField] private float cameraMoveSpeed = 5f;

        [Header("─ UI : 스코프 원 ─")]
        [Tooltip("화면 중앙에 표시할 원형 이미지 (Image 컴포넌트).\n" +
                 "Canvas에 배치한 뒤 여기에 드래그하세요.")]
        [SerializeField] private Image scopeCircleImage;

        [Tooltip("평상시 원 색상")]
        [SerializeField] private Color colorDefault = Color.white;

        [Tooltip("목표물이 원 안에 들어왔을 때 원 색상")]
        [SerializeField] private Color colorOnTarget = new Color(0.2f, 1f, 0.3f, 1f);

        [Header("─ UI : 포착 진행 표시 (선택) ─")]
        [Tooltip("포착 유지 진행률을 보여줄 Image.\n" +
                 "Image Type을 Filled + Radial 360으로 설정하세요.\n" +
                 "비워두면 사용 안 함.")]
        [SerializeField] private Image holdProgressImage;

        [Header("─ 목표 오브젝트 ─")]
        [Tooltip("원 안에 담아야 할 오브젝트의 Renderer.\n" +
                 "바운딩박스 8개 꼭짓점이 전부 원 안에 들어와야 포착 성공.")]
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("포착 유지 시간 (초). 0이면 들어오자마자 즉시 발동.")]
        [SerializeField] private float holdDuration = 1f;

        [Header("─ 성공 시 실행할 Output ─")]
        [Tooltip("포착 성공 시 실행할 ProcessBase Output 목록.\n" +
                 "비워두면 IsOn = true만 설정합니다.")]
        [SerializeField] private List<ProcessBase> outputsOnSuccess = new List<ProcessBase>();

        [Header("─ 취소 설정 ─")]
        [Tooltip("스코프 모드 취소 키. None으로 설정하면 취소 불가.")]
        [SerializeField] private KeyCode cancelKey = KeyCode.Escape;

        [Tooltip("취소 시 IsOn = true로 설정할지 여부.\n" +
                 "true: 시퀀스가 계속 진행됨 / false: 시퀀스 멈춤.")]
        [SerializeField] private bool isOnWhenCancelled = false;

        // ══════════════════════════════════════════════════════
        // 내부 상태
        // ══════════════════════════════════════════════════════
        private bool _isScopeActive = false;
        private float _holdTimer = 0f;
        private float _yaw;
        private float _pitch;
        private int _previousCameraPriority;
        private Canvas _parentCanvas;
        private Camera _mainCamera;

        // ──────────────────────────────────────────────────────
        private void Awake()
        {
            // 시작 시 스코프 UI 숨김
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
            if (scopeVirtualCamera == null)
            {
                Debug.LogWarning($"[{gameObject.name}] scopeVirtualCamera가 비어 있습니다.");
                IsOn = true; return;
            }
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
            _isScopeActive = true;
            _holdTimer = 0f;

            // ── 카메라 설정 ──
            _mainCamera = Camera.main;
            _previousCameraPriority = scopeVirtualCamera.Priority;

            // 스코프 카메라를 현재 메인 카메라 방향으로 초기화
            if (_mainCamera != null)
            {
                Vector3 euler = _mainCamera.transform.eulerAngles;
                _yaw   = euler.y;
                _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
                scopeVirtualCamera.transform.rotation = _mainCamera.transform.rotation;
            }

            // 스코프 가상 카메라 우선순위 올림 (CinemachinePriorityManager 사용 가능하면 사용)
            ActivateScopeCamera();

            // ── UI 표시 ──
            scopeCircleImage.color = colorDefault;
            scopeCircleImage.gameObject.SetActive(true);

            if (holdProgressImage != null)
            {
                holdProgressImage.fillAmount = 0f;
                holdProgressImage.gameObject.SetActive(true);
            }

            // ── 플레이어 입력 잠금 ──
            if (InputManager.Instance != null)
                InputManager.Instance.SetInputEnable(false);

            // 커서 잠금 (마우스로 카메라 조작)
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[CameraScopeAimOutput]</color> 스코프 모드 시작. 목표: {targetRenderer.name}");
#endif
        }

        // ──────────────────────────────────────────────────────
        private void Update()
        {
            if (!_isScopeActive) return;

            // ── 취소 키 ──
            if (cancelKey != KeyCode.None && Input.GetKeyDown(cancelKey))
            {
                ExitScope(success: false);
                return;
            }

            // ── 스코프 카메라 마우스 회전 ──
            // (레거시 Input.GetAxis 사용 → InputManager 비활성화와 무관하게 동작)
            float mouseX = Input.GetAxis("Mouse X") * sensitivityX;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivityY;

            _yaw   += mouseX;
            _pitch -= mouseY;
            _pitch  = Mathf.Clamp(_pitch, -pitchClamp, pitchClamp);

            scopeVirtualCamera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            // ── 스코프 카메라 WASD 이동 ──
            if (cameraMoveSpeed > 0f)
            {
                float moveX = Input.GetAxis("Horizontal"); // A / D
                float moveZ = Input.GetAxis("Vertical");   // W / S
                float moveY = 0f;
                if (Input.GetKey(KeyCode.E)) moveY =  1f;  // 위
                if (Input.GetKey(KeyCode.Q)) moveY = -1f;  // 아래

                Vector3 moveDir = scopeVirtualCamera.transform.forward * moveZ
                                + scopeVirtualCamera.transform.right   * moveX
                                + Vector3.up                           * moveY;

                scopeVirtualCamera.transform.position += moveDir * cameraMoveSpeed * Time.deltaTime;
            }

            // ── 포착 판정 ──
            bool onTarget = IsTargetFullyInCircle();

            // 색상 피드백
            scopeCircleImage.color = onTarget ? colorOnTarget : colorDefault;

            // 포착 타이머
            if (onTarget)
            {
                _holdTimer += Time.deltaTime;

                if (holdProgressImage != null)
                    holdProgressImage.fillAmount = Mathf.Clamp01(_holdTimer / Mathf.Max(holdDuration, 0.001f));

                if (_holdTimer >= holdDuration)
                {
                    ExitScope(success: true);
                }
            }
            else
            {
                // 원에서 벗어나면 타이머 리셋
                _holdTimer = 0f;
                if (holdProgressImage != null)
                    holdProgressImage.fillAmount = 0f;
            }
        }

        // ──────────────────────────────────────────────────────
        private void ExitScope(bool success)
        {
            _isScopeActive = false;

            // ── UI 숨김 ──
            scopeCircleImage.gameObject.SetActive(false);
            if (holdProgressImage != null)
                holdProgressImage.gameObject.SetActive(false);

            // ── 스코프 카메라 비활성화 (Priority 복원) ──
            DeactivateScopeCamera();

            // ── 플레이어 입력 복구 ──
            if (InputManager.Instance != null)
                InputManager.Instance.SetInputEnable(true);

            // 커서 복구
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (success)
            {
                // 지정 Output 실행
                foreach (var output in outputsOnSuccess)
                    if (output != null) output.Execute();

                IsOn = true;

#if UNITY_EDITOR
                Debug.Log($"<color=green>[CameraScopeAimOutput]</color> 포착 성공! Output 실행.");
#endif
            }
            else
            {
                // 취소
                if (isOnWhenCancelled) IsOn = true;

#if UNITY_EDITOR
                Debug.Log($"<color=orange>[CameraScopeAimOutput]</color> 스코프 취소됨.");
#endif
            }
        }

        // ──────────────────────────────────────────────────────
        // 스코프 카메라 우선순위 활성화 / 비활성화
        // CinemachinePriorityManager가 있으면 사용, 없으면 직접 Priority 조작
        // ──────────────────────────────────────────────────────
        private void ActivateScopeCamera()
        {
            if (CinemachinePriorityManager.Instance != null && previousCamera != null)
            {
                CinemachinePriorityManager.Instance.SwitchActiveCamera(
                    scopeVirtualCamera,
                    CameraBlendStyle.EaseInOut,
                    0.3f);
            }
            else
            {
                // 수동으로 Priority 올리기
                scopeVirtualCamera.Priority = scopeCameraPriority;
            }
        }

        private void DeactivateScopeCamera()
        {
            if (CinemachinePriorityManager.Instance != null && previousCamera != null)
            {
                CinemachinePriorityManager.Instance.SwitchActiveCamera(
                    previousCamera,
                    CameraBlendStyle.EaseInOut,
                    0.3f);
            }
            else
            {
                // 수동으로 Priority 복원
                scopeVirtualCamera.Priority = _previousCameraPriority;
            }
        }

        // ──────────────────────────────────────────────────────
        // 핵심 판정: 목표 오브젝트의 바운딩박스 8개 꼭짓점이
        // 전부 화면 중앙 원 안에 들어와 있는지 확인
        // ──────────────────────────────────────────────────────
        private bool IsTargetFullyInCircle()
        {
            if (_mainCamera == null || targetRenderer == null) return false;

            // 원의 screen-space 반지름 계산 (Canvas scaleFactor 보정)
            float canvasScale = (_parentCanvas != null) ? _parentCanvas.scaleFactor : 1f;
            float circleRadiusPx = (scopeCircleImage.rectTransform.rect.width * 0.5f) * canvasScale;
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            // 바운딩박스 8개 꼭짓점
            Vector3[] corners = GetBoundsCorners(targetRenderer.bounds);

            foreach (Vector3 corner in corners)
            {
                Vector3 screenPos = _mainCamera.WorldToScreenPoint(corner);

                // 카메라 뒤에 있으면 즉시 false
                if (screenPos.z < 0f) return false;

                float dist = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), screenCenter);
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
            _isScopeActive = false;
        }

        // ──────────────────────────────────────────────────────
        // Gizmos: 에디터에서 목표물 바운딩박스 시각화
        // ──────────────────────────────────────────────────────
        private void OnDrawGizmos()
        {
            if (targetRenderer == null) return;

            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.4f);
            Gizmos.DrawWireCube(targetRenderer.bounds.center, targetRenderer.bounds.size);
        }
    }
}
