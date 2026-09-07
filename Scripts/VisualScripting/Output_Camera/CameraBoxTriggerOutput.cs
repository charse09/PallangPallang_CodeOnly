using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 화면 중앙 사각형 영역 안에 대상 오브젝트가 들어오면 자동으로 Output을 실행하는 노드입니다.
    /// 별도의 UI 이미지나 가상 카메라 설정 없이 바로 동작합니다.
    /// </summary>
    public class CameraBoxTriggerOutput : ProcessBase
    {
        [Header("1. Target (감지 대상)")]
        [Tooltip("화면 중앙 박스에 들어와야 할 대상 (Transform 또는 GameObject)")]
        [SerializeField] private Transform target;

        [Tooltip("체크 시 대상의 피벗 대신 Renderer의 중심점(Bounds Center)을 기준으로 판정합니다.")]
        [SerializeField] private bool useRendererBoundsCenter = true;

        [Header("2. Camera & Detection Box (카메라 및 박스 영역)")]
        [Tooltip("감지에 사용할 카메라 (비워두면 Camera.main 자동 사용)")]
        [SerializeField] private Camera targetCamera;

        [Tooltip("화면 중앙 사각형 크기 (뷰포트 비율: 0.1 ~ 1.0)\n예: (0.4, 0.4)는 화면 가로/세로 중앙의 40% 영역")]
        [SerializeField] private Vector2 boxSizeRatio = new Vector2(0.35f, 0.35f);

        [Tooltip("중앙 사각형 안에 머물러야 하는 시간 (초). 0이면 들어오자마자 즉시 발동")]
        [SerializeField] private float holdDuration = 0f;

        [Tooltip("체크 시 카메라와 대상 사이에 장애물(벽)이 없을 때만 포착 인정")]
        [SerializeField] private bool checkLineOfSight = false;
        [SerializeField] private LayerMask obstacleLayer = ~0;

        [Header("3. Output Settings")]
        [Tooltip("포착 성공 시 실행할 ProcessBase Output 목록")]
        [SerializeField] private List<ProcessBase> outputsOnSuccess = new List<ProcessBase>();

        [Tooltip("성공 시 해당 노드를 자동으로 완료(IsOn = true) 처리")]
        [SerializeField] private bool completeProcessOnTrigger = true;

        [Header("4. Debug View")]
        [Tooltip("씬 뷰(Scene View)에서 중앙 사각형 감지 영역을 기즈모로 표시")]
        [SerializeField] private bool showDebugGizmos = true;

        private bool _isMonitoring = false;
        private float _currentHoldTimer = 0f;
        private Renderer _targetRenderer;

        private void Awake()
        {
            InitCamera();
            if (target != null && useRendererBoundsCenter)
            {
                _targetRenderer = target.GetComponentInChildren<Renderer>();
            }
            IsOn = false;
        }

        public override void Reset()
        {
            base.Reset();
            _isMonitoring = false;
            _currentHoldTimer = 0f;
            IsOn = false;
        }

        public override void Execute()
        {
            InitCamera();

            if (target == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 감지 대상(Target)이 비어 있습니다.");
                IsOn = true;
                return;
            }

            IsOn = false;
            _currentHoldTimer = 0f;
            _isMonitoring = true;
        }

        private void Update()
        {
            if (!_isMonitoring || target == null) return;
            if (targetCamera == null && !InitCamera()) return;

            Vector3 targetWorldPos = GetTargetPosition();
            Vector3 viewportPos = targetCamera.WorldToViewportPoint(targetWorldPos);

            // 1. 카메라 앞쪽에 있는지 확인 (Z > 0)
            if (viewportPos.z <= 0f)
            {
                _currentHoldTimer = 0f;
                return;
            }

            // 2. 화면 중앙 사각형 영역 계산 (중심: 0.5, 0.5)
            float halfW = boxSizeRatio.x * 0.5f;
            float halfH = boxSizeRatio.y * 0.5f;

            bool isInCenterBox = (viewportPos.x >= 0.5f - halfW && viewportPos.x <= 0.5f + halfW) &&
                                 (viewportPos.y >= 0.5f - halfH && viewportPos.y <= 0.5f + halfH);

            // 3. 시선 차단(벽) 체크
            if (isInCenterBox && checkLineOfSight)
            {
                Vector3 rayOrigin = targetCamera.transform.position;
                Vector3 rayDir = (targetWorldPos - rayOrigin).normalized;
                float dist = Vector3.Distance(rayOrigin, targetWorldPos);

                if (Physics.Raycast(rayOrigin, rayDir, dist - 0.1f, obstacleLayer))
                {
                    isInCenterBox = false;
                }
            }

            // 4. 포착 타이머 및 트리거 실행
            if (isInCenterBox)
            {
                _currentHoldTimer += Time.deltaTime;
                if (_currentHoldTimer >= holdDuration)
                {
                    TriggerSuccess();
                }
            }
            else
            {
                _currentHoldTimer = 0f;
            }
        }

        private void TriggerSuccess()
        {
            _isMonitoring = false;

            foreach (var output in outputsOnSuccess)
            {
                if (output != null)
                {
                    output.Reset();
                    output.Execute();
                }
            }

            if (completeProcessOnTrigger)
            {
                IsOn = true;
            }
        }

        private Vector3 GetTargetPosition()
        {
            if (useRendererBoundsCenter && _targetRenderer != null)
                return _targetRenderer.bounds.center;

            return target.position;
        }

        private bool InitCamera()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;

            return targetCamera != null;
        }

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos) return;

            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;

            // 씬 뷰에서 카메라 정면 2미터 거리에 사각형 기즈모 시각화
            float depth = 2.0f;
            float halfW = boxSizeRatio.x * 0.5f;
            float halfH = boxSizeRatio.y * 0.5f;

            Vector3 p1 = cam.ViewportToWorldPoint(new Vector3(0.5f - halfW, 0.5f - halfH, depth));
            Vector3 p2 = cam.ViewportToWorldPoint(new Vector3(0.5f + halfW, 0.5f - halfH, depth));
            Vector3 p3 = cam.ViewportToWorldPoint(new Vector3(0.5f + halfW, 0.5f + halfH, depth));
            Vector3 p4 = cam.ViewportToWorldPoint(new Vector3(0.5f - halfW, 0.5f + halfH, depth));

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);
        }
    }
}