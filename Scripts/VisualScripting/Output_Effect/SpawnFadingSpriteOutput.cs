using System.Collections;
using UnityEngine;
using UnityEngine.UI; // UGUI Image 컴포넌트 제어용

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 트리거 신호를 받으면 UGUI Canvas 하위에 이미지를 생성하고,
    /// 애니메이션 커브에 맞춰 크기가 줄어들며, 설정에 따라 알파(투명도)도 같이 제어하는 노코드 모듈
    /// </summary>
    public class SpawnFadingSpriteOutput : ProcessBase
    {
        [Header("UGUI Sprite Settings")]
        [Tooltip("화면에 띄울 UI 2D 이미지 스프라이트 소스")]
        [SerializeField] private Sprite displaySprite;

        [Tooltip("생성될 UI 이미지의 픽셀 해상도 크기 (Width, Height)")]
        [SerializeField] private Vector2 imageSize = new Vector2(100f, 100f);

        [Tooltip("생성 시 초기 크기 스케일 비율 (Scale)")]
        [SerializeField] private Vector3 initialScale = Vector3.one;

        [Header("UGUI Canvas / Parent Settings")]
        [Tooltip("UI 이미지가 생성될 부모 UI 패널/Canvas (비워두면 자동으로 씬의 최상위 Canvas를 찾습니다)")]
        [SerializeField] private RectTransform uiParent;

        [Tooltip("부모 UI의 앵커 중심점으로부터 생성될 2D 스크린 좌표 오프셋")]
        [SerializeField] private Vector2 positionOffset = Vector2.zero;

        [Header("Animation Settings")]
        [Tooltip("이미지가 완전히 작아져 사라지기까지 걸리는 시간 (초)")]
        [SerializeField] private float duration = 1.0f;

        [Tooltip("크기가 작아지는 가속도를 제어할 애니메이션 커브")]
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

        // ---  요청하신 알파 제어 토글 스위치 ---
        [Tooltip("체크(True) 시 크기가 줄어들면서 투명도(Alpha)도 부드럽게 0으로 사라집니다.\n체크 해제(False) 시 투명도는 원본을 유지한 채 크기만 줄어듭니다.")]
        [SerializeField] private bool fadeAlpha = true;

        public override void Execute()
        {
            if (displaySprite == null)
            {
                Debug.LogWarning($"<color=yellow>[{gameObject.name}]</color> Display Sprite가 할당되지 않았습니다.");
                IsOn = true;
                return;
            }

            // 1. 부모 UGUI 패널 자동 탐색
            if (uiParent == null)
            {
                Canvas mainCanvas = FindFirstObjectByType<Canvas>();
                if (mainCanvas != null)
                {
                    uiParent = mainCanvas.GetComponent<RectTransform>();
                }
                else
                {
                    Debug.LogError($"<color=red>[{gameObject.name}]</color> 씬에 UGUI Canvas가 없어 이미지를 띄울 수 없습니다.");
                    IsOn = true;
                    return;
                }
            }

            // 2. UI 오브젝트 생성 및 구조 세팅
            GameObject uiObj = new GameObject($"FadingUGUI_{displaySprite.name}");
            RectTransform rectTransform = uiObj.AddComponent<RectTransform>();
            rectTransform.SetParent(uiParent, false);

            // 3. UGUI Image 컴포넌트 추가 및 값 반영
            Image ughuiImage = uiObj.AddComponent<Image>();
            ughuiImage.sprite = displaySprite;

            rectTransform.sizeDelta = imageSize;
            rectTransform.anchoredPosition = positionOffset;
            rectTransform.localScale = initialScale;

            // 4. 연출 코루틴 시작
            StartCoroutine(UGUILifeRoutine(uiObj, rectTransform, ughuiImage));

            // 프레임워크 규칙: 즉시 다음 연결 노드로 신호 전달
            IsOn = true;
        }

        private IEnumerator UGUILifeRoutine(GameObject targetObj, RectTransform rectTransform, Image uiImage)
        {
            float elapsedTime = 0f;
            Color originalColor = uiImage != null ? uiImage.color : Color.white;

            while (elapsedTime < duration)
            {
                if (targetObj == null) yield break;

                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);

                // 1. 커브 기반 스케일 축소 (이 연출은 상시 작동)
                float curveScaleMultiplier = scaleCurve.Evaluate(progress);
                rectTransform.localScale = initialScale * curveScaleMultiplier;

                // 2. [조건부 분기] 인스펙터에서 fadeAlpha가 켜져있을 때만 투명도 연산 수행
                if (fadeAlpha && uiImage != null)
                {
                    // 시간이 지날수록 알파 값을 원본 알파값(originalColor.a)에서 0f까지 부드럽게 줄임
                    float targetAlpha = Mathf.Lerp(originalColor.a, 0f, progress);
                    uiImage.color = new Color(originalColor.r, originalColor.g, originalColor.b, targetAlpha);
                }

                yield return null;
            }

            // 수명 종료 시 오브젝트 완전 파괴
            if (targetObj != null)
            {
                Destroy(targetObj);
            }
        }
    }
}