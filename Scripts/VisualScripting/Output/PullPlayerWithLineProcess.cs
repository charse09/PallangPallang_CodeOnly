using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// LineRenderer로 두 위치를 타일링 패턴(머티리얼 반복)으로 연결하며 플레이어를 범퍼카로 당겨오는 연출 노드
    /// </summary>
    public class PullPlayerWithLineProcess : ProcessBase
    {
        [Header("Targets")]
        [Tooltip("당겨올 플레이어 오브젝트")]
        [SerializeField] private Transform playerTransform;

        [Tooltip("목표 지점인 범퍼카 오브젝트")]
        [SerializeField] private Transform kartTargetTransform;

        [Header("Line Visual Settings")]
        [Tooltip("직선 연출에 사용할 LineRenderer (비어있으면 자동 생성/추가)")]
        [SerializeField] private LineRenderer lineRenderer;

        [Tooltip("직선의 두께")]
        [SerializeField] private float lineWidth = 0.15f;

        [Tooltip("직선 재질 (반복 적용될 텍스처 머티리얼)")]
        [SerializeField] private Material lineMaterial;

        [Tooltip("직선 색상")]
        [SerializeField] private Color lineColor = Color.cyan;

        [Header("Tiling Settings")]
        [Tooltip("1m당 텍스처가 반복될 횟수 (값이 클수록 머티리얼이 더 촘촘하게 여러 개 들어감)")]
        [SerializeField] private float textureTilingPerMeter = 2.0f;

        [Tooltip("당겨질 때 텍스처가 감기는/흐르는 속도 (0이면 고정 타일링)")]
        [SerializeField] private float textureScrollSpeed = 0.0f;

        [Header("Pull Motion Settings")]
        [Tooltip("당겨지는 데 걸리는 시간(초)")]
        [SerializeField] private float pullDuration = 0.6f;

        [Tooltip("당겨질 때의 속도 곡선")]
        [SerializeField] private AnimationCurve pullCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        private Material instanceMaterial; // 개별 인스턴스 머티리얼 저장용

        private void Awake()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    lineRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            SetupLineRenderer();
            lineRenderer.enabled = false;
        }

        private void SetupLineRenderer()
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.useWorldSpace = true;

            // [핵심 1] 텍스처가 길이에 따라 늘어나지 않고 반복(Tile)되도록 설정
            lineRenderer.textureMode = LineTextureMode.Tile;

            if (lineMaterial != null)
            {
                // 인스턴스 머티리얼을 생성하여 타일링 조작이 다른 오브젝트에 영향을 주지 않도록 함
                instanceMaterial = new Material(lineMaterial);
                lineRenderer.material = instanceMaterial;
            }
            else
            {
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                instanceMaterial = lineRenderer.material;
            }

            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
        }

        public override void Execute()
        {
            IsOn = false;
            StartCoroutine(PullRoutine());
        }

        private IEnumerator PullRoutine()
        {
            if (playerTransform == null || kartTargetTransform == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Target(Player 또는 Kart)이 할당되지 않았습니다!");
                IsOn = true;
                yield break;
            }

            lineRenderer.enabled = true;

            Rigidbody playerRb = playerTransform.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
                playerRb.isKinematic = true;
            }

            Vector3 startPos = playerTransform.position;
            Quaternion startRot = playerTransform.rotation;

            float timer = 0f;

            while (timer < pullDuration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / pullDuration);
                float curvedT = pullCurve.Evaluate(progress);

                // 플레이어 위치 보간 이동
                playerTransform.position = Vector3.Lerp(startPos, kartTargetTransform.position, curvedT);
                playerTransform.rotation = Quaternion.Slerp(startRot, kartTargetTransform.rotation, curvedT);

                // 라인 양 끝 위치 업데이트
                Vector3 pPos = playerTransform.position;
                Vector3 kPos = kartTargetTransform.position;
                lineRenderer.SetPosition(0, pPos);
                lineRenderer.SetPosition(1, kPos);

                // [핵심 2] 현재 두 지점 사이의 거리를 계산하여 텍스처 타일링(Tiling) 수치 실시간 조절
                float currentDistance = Vector3.Distance(pPos, kPos);
                float tilingX = currentDistance * textureTilingPerMeter;

                // 텍스처가 당겨지면서 스크롤(흐르는) 효과 옵션 적용
                float offsetX = (textureScrollSpeed != 0f) ? -Time.time * textureScrollSpeed : 0f;

                if (instanceMaterial != null)
                {
                    instanceMaterial.mainTextureScale = new Vector2(tilingX, 1f);
                    instanceMaterial.mainTextureOffset = new Vector2(offsetX, 0f);
                }

                yield return null;
            }

            // 최종 위치 보정
            playerTransform.position = kartTargetTransform.position;
            playerTransform.rotation = kartTargetTransform.rotation;

            if (playerRb != null)
            {
                playerRb.isKinematic = false;
            }

            lineRenderer.enabled = false;
            IsOn = true;
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
        }

        private void OnDestroy()
        {
            // 동적 생성한 인스턴스 머티리얼 메모리 해제
            if (instanceMaterial != null)
            {
                Destroy(instanceMaterial);
            }
        }
    }
}