using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    public class ChatboxManager : MonoBehaviour
    {
        public static ChatboxManager Instance { get; private set; }

        [Header("Canvas & Prefab")]
        [SerializeField] private Canvas targetCanvas;
        [Tooltip("Image + TextMeshProUGUI가 포함된 2D 말풍선 UI 프리팹")]
        [SerializeField] private GameObject chatBubblePrefab;

        [Header("HUD Safe Area (여백 픽셀)")]
        [SerializeField] private float marginLeft = 80f;
        [SerializeField] private float marginRight = 80f;
        [SerializeField] private float marginTop = 120f;    // 상단 HUD 영역 보호
        [SerializeField] private float marginBottom = 100f;  // 하단 HUD 영역 보호

        [Header("3D Distance Perspective (원근 거리 계산)")]
        [Tooltip("기준 거리 (이 거리에서 스케일 1.0)")]
        [SerializeField] private float referenceDistance = 6f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float maxDistance = 25f;
        [SerializeField] private float minScale = 0.45f;
        [SerializeField] private float maxScale = 1.15f;

        [Header("Tracking Smoothing")]
        [Tooltip("화면 둘레를 따라 부드럽게 이동하는 추적 속도")]
        [SerializeField] private float trackingSmoothSpeed = 16f;

        [Header("Overlap Prevention (겹침 방지)")]
        [SerializeField] private float bubbleSpacing = 15f;

        private Camera _mainCamera;
        private readonly List<ActiveBubbleData> _activeBubbles = new List<ActiveBubbleData>();

        private class ActiveBubbleData
        {
            public RectTransform rect;
            public TextMeshProUGUI textComp;
            public CanvasGroup canvasGroup;
            public Transform target;
            public Vector3 worldOffset;
            public float remainingTime;
            public Vector2 currentScreenPos;
            public Vector2 targetScreenPos;
            public Vector2 size;
            public bool isInitialized;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            _mainCamera = Camera.main;
            if (targetCanvas == null) targetCanvas = GetComponentInParent<Canvas>();
        }

        public void ShowBubble(Transform target, Vector3 worldOffset, string text, float duration)
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (chatBubblePrefab == null || targetCanvas == null) return;

            GameObject bubbleObj = Instantiate(chatBubblePrefab, targetCanvas.transform);
            RectTransform rect = bubbleObj.GetComponent<RectTransform>();
            TextMeshProUGUI tmp = bubbleObj.GetComponentInChildren<TextMeshProUGUI>();
            CanvasGroup group = bubbleObj.GetComponent<CanvasGroup>();
            if (group == null) group = bubbleObj.AddComponent<CanvasGroup>();

            tmp.text = text;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

            var data = new ActiveBubbleData
            {
                rect = rect,
                textComp = tmp,
                canvasGroup = group,
                target = target,
                worldOffset = worldOffset,
                remainingTime = duration,
                size = rect.sizeDelta,
                isInitialized = false
            };

            _activeBubbles.Add(data);
            StartCoroutine(BubbleLifecycleRoutine(data));
        }

        private IEnumerator BubbleLifecycleRoutine(ActiveBubbleData data)
        {
            data.canvasGroup.alpha = 0f;
            float fadeIn = 0f;
            while (fadeIn < 1f)
            {
                fadeIn += Time.deltaTime * 8f;
                data.canvasGroup.alpha = Mathf.Clamp01(fadeIn);
                yield return null;
            }

            while (data.remainingTime > 0.3f)
            {
                data.remainingTime -= Time.deltaTime;
                yield return null;
            }

            while (data.remainingTime > 0f)
            {
                data.remainingTime -= Time.deltaTime;
                data.canvasGroup.alpha = Mathf.Clamp01(data.remainingTime / 0.3f);
                yield return null;
            }

            _activeBubbles.Remove(data);
            if (data.rect != null) Destroy(data.rect.gameObject);
        }

        private void LateUpdate()
        {
            if (_mainCamera == null || _activeBubbles.Count == 0) return;

            Vector2 screenCenter = new Vector2(
                (marginLeft + (Screen.width - marginRight)) * 0.5f,
                (marginBottom + (Screen.height - marginTop)) * 0.5f
            );

            for (int i = 0; i < _activeBubbles.Count; i++)
            {
                var bubble = _activeBubbles[i];
                if (bubble.target == null) continue;

                Vector3 targetWorldPos = bubble.target.position + bubble.worldOffset;

                // 1. 카메라 기준 실제 3D 상대 벡터 및 거리 연산
                Vector3 relativePos = _mainCamera.transform.InverseTransformPoint(targetWorldPos);
                float distance3D = relativePos.magnitude;

                // 2. 3D 상대 거리에 따른 정밀 원근 스케일링
                float distRatio = Mathf.InverseLerp(minDistance, maxDistance, distance3D);
                float calculatedScale = Mathf.Lerp(maxScale, minScale, distRatio);
                bubble.rect.localScale = Vector3.one * calculatedScale;

                // 스케일이 반영된 말풍선 박스 절반 크기
                float halfWidth = (bubble.size.x * calculatedScale) * 0.5f;
                float halfHeight = (bubble.size.y * calculatedScale) * 0.5f;

                // HUD 마진이 포함된 유효 안전 직사각형 한계선
                float minX = marginLeft + halfWidth;
                float maxX = Screen.width - marginRight - halfWidth;
                float minY = marginBottom + halfHeight;
                float maxY = Screen.height - marginTop - halfHeight;

                Vector2 halfExtents = new Vector2((maxX - minX) * 0.5f, (maxY - minY) * 0.5f);
                Vector2 calculatedPos;

                // 3. 화면 내부 여부 및 360도 상대 방위각 투영 연산
                if (relativePos.z > 0.1f) // 카메라 앞쪽에 위치할 때
                {
                    Vector3 screenPoint = _mainCamera.WorldToScreenPoint(targetWorldPos);

                    // 화면 안쪽에 완벽히 들어오는 경우
                    if (screenPoint.x >= minX && screenPoint.x <= maxX && screenPoint.y >= minY && screenPoint.y <= maxY)
                    {
                        calculatedPos = new Vector2(screenPoint.x, screenPoint.y);
                    }
                    else
                    {
                        // 전방이지만 시야각(FOV)을 벗어난 경우: 중심으로부터의 실제 방향각 계산
                        Vector2 dir = new Vector2(screenPoint.x - screenCenter.x, screenPoint.y - screenCenter.y).normalized;
                        calculatedPos = CalculateRayBoxIntersection(screenCenter, dir, halfExtents);
                    }
                }
                else // 카메라 뒤편에 위치할 때 (Z <= 0)
                {
                    // 카메라 뒤편에 있을 때는 월드 투영 좌표가 반전되므로, 역투영 보정하여 360도 방위각을 정확히 추적
                    Vector3 screenPoint = _mainCamera.WorldToScreenPoint(targetWorldPos);
                    Vector2 dir = new Vector2(-screenPoint.x + screenCenter.x, -screenPoint.y + screenCenter.y).normalized;
                    if (dir.sqrMagnitude < 0.001f) dir = Vector2.down;

                    calculatedPos = CalculateRayBoxIntersection(screenCenter, dir, halfExtents);
                }

                bubble.targetScreenPos = calculatedPos;

                if (!bubble.isInitialized)
                {
                    bubble.currentScreenPos = calculatedPos;
                    bubble.isInitialized = true;
                }
                else
                {
                    // 화면 둘레를 부드럽게 스르륵 이동하도록 보간 (튀는 현상 방지)
                    bubble.currentScreenPos = Vector2.Lerp(bubble.currentScreenPos, bubble.targetScreenPos, Time.deltaTime * trackingSmoothSpeed);
                }
            }

            // 4. 말풍선 간 겹침 방지 (Anti-Overlap)
            ResolveOverlaps();

            // 5. 최종 위치 적용
            for (int i = 0; i < _activeBubbles.Count; i++)
            {
                var bubble = _activeBubbles[i];
                bubble.rect.position = bubble.currentScreenPos;
            }
        }

        // 중심에서 방위각 방향(dir)으로 뻗어나가는 광선과 직사각형 경계선의 정확한 충돌점 계산
        private Vector2 CalculateRayBoxIntersection(Vector2 center, Vector2 dir, Vector2 halfExtents)
        {
            float tX = (Mathf.Abs(dir.x) > 0.0001f) ? halfExtents.x / Mathf.Abs(dir.x) : float.MaxValue;
            float tY = (Mathf.Abs(dir.y) > 0.0001f) ? halfExtents.y / Mathf.Abs(dir.y) : float.MaxValue;
            float t = Mathf.Min(tX, tY);

            return center + (dir * t);
        }

        private void ResolveOverlaps()
        {
            for (int i = 0; i < _activeBubbles.Count; i++)
            {
                for (int j = i + 1; j < _activeBubbles.Count; j++)
                {
                    var a = _activeBubbles[i];
                    var b = _activeBubbles[j];

                    float halfWA = (a.size.x * a.rect.localScale.x) * 0.5f;
                    float halfHA = (a.size.y * a.rect.localScale.y) * 0.5f;
                    float halfWB = (b.size.x * b.rect.localScale.x) * 0.5f;
                    float halfHB = (b.size.y * b.rect.localScale.y) * 0.5f;

                    float deltaX = Mathf.Abs(a.currentScreenPos.x - b.currentScreenPos.x);
                    float deltaY = Mathf.Abs(a.currentScreenPos.y - b.currentScreenPos.y);

                    float overlapX = (halfWA + halfWB + bubbleSpacing) - deltaX;
                    float overlapY = (halfHA + halfHB + bubbleSpacing) - deltaY;

                    if (overlapX > 0 && overlapY > 0)
                    {
                        float push = overlapY * 0.5f;
                        if (a.currentScreenPos.y >= b.currentScreenPos.y)
                        {
                            a.currentScreenPos.y += push;
                            b.currentScreenPos.y -= push;
                        }
                        else
                        {
                            a.currentScreenPos.y -= push;
                            b.currentScreenPos.y += push;
                        }
                    }
                }
            }
        }
    }
}