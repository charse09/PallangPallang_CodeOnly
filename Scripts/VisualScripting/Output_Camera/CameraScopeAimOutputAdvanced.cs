using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 화면 중앙의 원 안에 목표 오브젝트가 일정 점유율 이상 담기면 Output을 실행하는 스코프 조준 시스템 (개선판).
    /// 엄격한 8꼭짓점 검사 대신 중심점 및 다중 샘플링 포인트 판정을 사용하여,
    /// 벽 모서리에 살짝 걸치거나 회전해도 안정적으로 포착합니다.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Output/Camera Scope Aim Output Advanced")]
    public class CameraScopeAimOutputAdvanced : ProcessBase
    {
        [Header("─ 스코프 카메라 ─")]
        [Tooltip("스코프 모드에서 활성화할 Cinemachine 가상 카메라")]
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

        [Header("─ UI : 스코프 조준선 ─")]
        [Tooltip("화면 중앙에 표시할 원형 이미지 (Image 컴포넌트)")]
        [SerializeField] private Image scopeCircleImage;

        [Tooltip("평상시 원 색상")]
        [SerializeField] private Color colorDefault = Color.white;

        [Tooltip("목표물이 원 안에 기준치 이상 들어왔을 때 색상")]
        [SerializeField] private Color colorOnTarget = new Color(0.2f, 1f, 0.3f, 1f);

        [Tooltip("포착 유지 진행률을 보여줄 Image (Filled + Radial 360)")]
        [SerializeField] private Image holdProgressImage;

        [Header("─ 목표 오브젝트 & 판정 완화 (핵심) ─")]
        [Tooltip("조준 대상 오브젝트의 Renderer")]
        [SerializeField] private Renderer targetRenderer;

        [Tooltip("★ 최소 요구 점유율 (0.5 = 타겟 샘플링 포인트의 50% 이상이 원에 들어오면 인정)")]
        [Range(0.2f, 1.0f)]
        [SerializeField] private float minOverlapRatio = 0.5f;

        [Tooltip("체크 시: 타겟의 정중앙(Center)은 반드시 원 안에 있어야 유효 판정")]
        [SerializeField] private bool requireCenterInCircle = true;

        [Tooltip("포착 유지 시간 (초)")]
        [SerializeField] private float holdDuration = 1.0f;

        [Tooltip("원 밖으로 나갔을 때 게이지가 줄어드는 속도 (0이면 즉시 0으로 리셋, 1~2 권장)")]
        [Range(0f, 5f)]
        [SerializeField] private float gaugeDecaySpeed = 1.5f;

        [Header("─ 벽 가림(시야 차단) 검사 ─")]
        [Tooltip("체크 시 카메라와 타겟 사이에 실제 벽/장애물이 있으면 조준을 차단합니다.")]
        [SerializeField] private bool checkObstacles = true;

        [Tooltip("벽 및 장애물로 인식할 레이어")]
        [SerializeField] private LayerMask obstacleLayerMask = 1 << 0; // Default

        [Header("─ 성공 시 실행할 Output ─")]
        [Tooltip("포착 성공 시 실행할 ProcessBase Output 목록")]
        [SerializeField] private List<ProcessBase> outputsOnSuccess = new List<ProcessBase>();

        [Header("─ 취소 설정 ─")]
        [Tooltip("스코프 모드 취소 키. None이면 취소 불가.")]
        [SerializeField] private KeyCode cancelKey = KeyCode.Escape;

        [Tooltip("취소 시 IsOn = true로 설정할지 여부")]
        [SerializeField] private bool isOnWhenCancelled = false;

        private bool _isScopeActive = false;
        private float _holdTimer = 0f;
        private float _yaw;
        private float _pitch;
        private int _previousCameraPriority;
        private Canvas _parentCanvas;
        private Camera _mainCamera;

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

        public override void Execute()
        {
            if (scopeVirtualCamera == null || scopeCircleImage == null || targetRenderer == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 필수 레퍼런스(카메라, 조준원, 타겟)가 누락되었습니다.");
                IsOn = true;
                return;
            }

            IsOn = false;
            _isScopeActive = true;
            _holdTimer = 0f;

            _mainCamera = Camera.main;
            _previousCameraPriority = scopeVirtualCamera.Priority;

            if (_mainCamera != null)
            {
                Vector3 euler = _mainCamera.transform.eulerAngles;
                _yaw = euler.y;
                _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
                scopeVirtualCamera.transform.rotation = _mainCamera.transform.rotation;
            }

            ActivateScopeCamera();

            scopeCircleImage.color = colorDefault;
            scopeCircleImage.gameObject.SetActive(true);

            if (holdProgressImage != null)
            {
                holdProgressImage.fillAmount = 0f;
                holdProgressImage.gameObject.SetActive(true);
            }

            if (InputManager.Instance != null)
                InputManager.Instance.SetInputEnable(false);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (!_isScopeActive) return;

            if (cancelKey != KeyCode.None && Input.GetKeyDown(cancelKey))
            {
                ExitScope(success: false);
                return;
            }

            // 카메라 회전 처리
            float mouseX = Input.GetAxis("Mouse X") * sensitivityX;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivityY;

            _yaw += mouseX;
            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, -pitchClamp, pitchClamp);

            scopeVirtualCamera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            // 카메라 위치 이동 (WASD)
            if (cameraMoveSpeed > 0f)
            {
                float moveX = Input.GetAxis("Horizontal");
                float moveZ = Input.GetAxis("Vertical");
                float moveY = 0f;
                if (Input.GetKey(KeyCode.E)) moveY = 1f;
                if (Input.GetKey(KeyCode.Q)) moveY = -1f;

                Vector3 moveDir = scopeVirtualCamera.transform.forward * moveZ
                                + scopeVirtualCamera.transform.right * moveX
                                + Vector3.up * moveY;

                scopeVirtualCamera.transform.position += moveDir * cameraMoveSpeed * Time.deltaTime;
            }
        }

        private void LateUpdate()
        {
            // 시네머신 카메라의 회전/이동 반영이 끝난 직후 화면 좌표 정밀 판정
            if (!_isScopeActive) return;

            bool onTarget = EvaluateTargetOverlap();

            // 색상 피드백
            scopeCircleImage.color = onTarget ? colorOnTarget : colorDefault;

            // 타이머 업데이트 (손떨림 보정 Decay 로직)
            if (onTarget)
            {
                _holdTimer += Time.deltaTime;

                if (_holdTimer >= holdDuration)
                {
                    if (holdProgressImage != null) holdProgressImage.fillAmount = 1f;
                    ExitScope(success: true);
                    return;
                }
            }
            else
            {
                if (gaugeDecaySpeed > 0f)
                {
                    _holdTimer = Mathf.Max(0f, _holdTimer - Time.deltaTime * gaugeDecaySpeed);
                }
                else
                {
                    _holdTimer = 0f;
                }
            }

            if (holdProgressImage != null)
            {
                holdProgressImage.fillAmount = Mathf.Clamp01(_holdTimer / Mathf.Max(holdDuration, 0.001f));
            }
        }

        /// <summary>
        /// 중심점 및 다중 샘플 포인트를 통해 목표물의 점유율(포함 비율)을 검사합니다.
        /// </summary>
        private bool EvaluateTargetOverlap()
        {
            if (_mainCamera == null || targetRenderer == null) return false;

            Bounds b = targetRenderer.bounds;
            Vector3 centerPos = b.center;

            // 1. 실제 벽/장애물 시야 차단 검사
            if (checkObstacles)
            {
                Vector3 camPos = _mainCamera.transform.position;
                Vector3 toTarget = centerPos - camPos;
                float dist = toTarget.magnitude;

                if (Physics.Raycast(camPos, toTarget.normalized, out RaycastHit hit, dist, obstacleLayerMask, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.transform != targetRenderer.transform && !hit.collider.transform.IsChildOf(targetRenderer.transform))
                    {
                        return false;
                    }
                }
            }

            // 2. 조준 원의 Screen Space 반지름 계산
            float canvasScale = (_parentCanvas != null) ? _parentCanvas.scaleFactor : 1f;
            float circleRadiusPx = (scopeCircleImage.rectTransform.rect.width * 0.5f) * canvasScale;
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            // 3. 중심점 필수 포함 검사
            Vector3 centerScreen = _mainCamera.WorldToScreenPoint(centerPos);
            if (centerScreen.z < 0f) return false;

            float centerDist = Vector2.Distance(new Vector2(centerScreen.x, centerScreen.y), screenCenter);
            if (requireCenterInCircle && centerDist > circleRadiusPx)
            {
                return false;
            }

            // 4. 다중 샘플 포인트 검사 (중심 + 6개 외곽 축 포인트)
            Vector3[] samplePoints = GetTargetSamplePoints(b);
            int insideCount = 0;

            for (int i = 0; i < samplePoints.Length; i++)
            {
                Vector3 scr = _mainCamera.WorldToScreenPoint(samplePoints[i]);
                if (scr.z > 0f)
                {
                    float d = Vector2.Distance(new Vector2(scr.x, scr.y), screenCenter);
                    if (d <= circleRadiusPx)
                    {
                        insideCount++;
                    }
                }
            }

            // 5. 점유율 비율 검사
            float currentRatio = (float)insideCount / samplePoints.Length;
            return currentRatio >= minOverlapRatio;
        }

        private Vector3[] GetTargetSamplePoints(Bounds b)
        {
            Vector3 c = b.center;
            Vector3 e = b.extents;

            return new Vector3[]
            {
                c,                               // 중심점
                c + new Vector3(0f, e.y, 0f),    // 머리(상단)
                c - new Vector3(0f, e.y, 0f),    // 발(하단)
                c + new Vector3(e.x, 0f, 0f),    // 우측
                c - new Vector3(e.x, 0f, 0f),    // 좌측
                c + new Vector3(0f, 0f, e.z),    // 전면
                c - new Vector3(0f, 0f, e.z)     // 후면
            };
        }

        private void ExitScope(bool success)
        {
            _isScopeActive = false;

            scopeCircleImage.gameObject.SetActive(false);
            if (holdProgressImage != null)
                holdProgressImage.gameObject.SetActive(false);

            DeactivateScopeCamera();

            if (InputManager.Instance != null)
                InputManager.Instance.SetInputEnable(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (success)
            {
                foreach (var output in outputsOnSuccess)
                {
                    if (output != null) output.Execute();
                }
                IsOn = true;
            }
            else
            {
                if (isOnWhenCancelled) IsOn = true;
            }
        }

        private void ActivateScopeCamera()
        {
            if (CinemachinePriorityManager.Instance != null && previousCamera != null)
            {
                CinemachinePriorityManager.Instance.SwitchActiveCamera(scopeVirtualCamera, CameraBlendStyle.EaseInOut, 0.3f);
            }
            else
            {
                scopeVirtualCamera.Priority = scopeCameraPriority;
            }
        }

        private void DeactivateScopeCamera()
        {
            if (CinemachinePriorityManager.Instance != null && previousCamera != null)
            {
                CinemachinePriorityManager.Instance.SwitchActiveCamera(previousCamera, CameraBlendStyle.EaseInOut, 0.3f);
            }
            else
            {
                scopeVirtualCamera.Priority = _previousCameraPriority;
            }
        }

        public override void Reset()
        {
            base.Reset();
            _isScopeActive = false;
            _holdTimer = 0f;
            IsOn = false;
        }

        private void OnDrawGizmos()
        {
            if (targetRenderer == null) return;

            Bounds b = targetRenderer.bounds;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(b.center, 0.2f);

            Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.3f);
            Gizmos.DrawWireCube(b.center, b.size);
        }
    }
}