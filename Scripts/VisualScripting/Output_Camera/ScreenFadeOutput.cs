using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    public enum FadeType
    {
        FadeOut, // 화면이 점차 설정된 색상으로 완전히 덮임 (어두워짐)
        FadeIn   // 화면을 덮고 있던 색상이 점차 투명해짐 (밝아짐)
    }

    /// <summary>
    /// 화면을 지정된 색상으로 페이드 인/아웃 하는 화면 전환 연출용 Output
    /// UI Canvas를 자동으로 생성하고 관리하므로 별도의 UI 셋팅이 필요 없습니다.
    /// </summary>
    public class ScreenFadeOutput : ProcessBase
    {
        [Header("Fade Settings")]
        [Tooltip("Fade Out: 화면 가리기 / Fade In: 화면 보이기")]
        [SerializeField] private FadeType fadeType = FadeType.FadeOut;

        [Tooltip("페이드 효과에 사용할 색상 (알파값은 코드가 알아서 제어하므로 RGB 색상만 고르시면 됩니다)")]
        [SerializeField] private Color fadeColor = Color.black;

        [Tooltip("페이드 효과가 진행되는 시간 (초)")]
        [SerializeField] private float duration = 1.0f;

        [Tooltip("화면이 가려져 있는 동안 플레이어의 UI 터치나 클릭을 막을지 여부")]
        [SerializeField] private bool blockRaycasts = true;

        private bool isFading = false;

        // 씬 내에서 페이드 캔버스를 고유하게 식별하기 위한 이름
        private const string CanvasName = "GOS_GlobalFadeCanvas";

        public override void Execute()
        {
            if (!isFading)
            {
                StartCoroutine(FadeRoutine());
            }
        }

        private IEnumerator FadeRoutine()
        {
            isFading = true;

            // 1. 페이드용 UI 이미지 가져오기 (없으면 자동 생성)
            Image fadeImage = SetupFadeCanvas();

            // 2. 시작/끝 알파값 설정
            float startAlpha = fadeType == FadeType.FadeOut ? 0f : 1f;
            float endAlpha = fadeType == FadeType.FadeOut ? 1f : 0f;

            Color startColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, startAlpha);
            Color endColor = new Color(fadeColor.r, fadeColor.g, fadeColor.b, endAlpha);

            fadeImage.color = startColor;

            // 페이드 아웃 시작 시 이미지 활성화 및 클릭 차단 설정
            fadeImage.gameObject.SetActive(true);
            fadeImage.raycastTarget = blockRaycasts;

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> {fadeType} 시작! ({duration}초, 색상: {fadeColor})");
#endif

            // 3. 시간에 따른 보간(Lerp) 처리
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // 자연스러운 전환을 위해 SmoothStep 적용
                t = Mathf.SmoothStep(0f, 1f, t);

                fadeImage.color = Color.Lerp(startColor, endColor, t);

                yield return null; // 1프레임 대기
            }

            // 오차 보정 (정확히 목표 색상으로 맞춤)
            fadeImage.color = endColor;

            // Fade In(화면이 다시 보임)이 끝났다면, 화면을 가리지 않도록 이미지를 비활성화하여 성능 최적화
            if (fadeType == FadeType.FadeIn)
            {
                fadeImage.gameObject.SetActive(false);
            }

            isFading = false;

            // 4. 다음 노드 실행 신호
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> {fadeType} 완료.");
#endif
        }

        /// <summary>
        /// 씬에 페이드용 캔버스가 없으면 생성하고, Image 컴포넌트를 반환합니다.
        /// </summary>
        private Image SetupFadeCanvas()
        {
            GameObject canvasObj = GameObject.Find(CanvasName);
            Image fadeImage = null;

            // 캔버스가 이미 존재하면 이미지 컴포넌트만 찾아서 반환
            if (canvasObj != null)
            {
                fadeImage = canvasObj.GetComponentInChildren<Image>(true);
                return fadeImage;
            }

            // 존재하지 않으면 최상단(SortingOrder 999)에 새로 생성
            canvasObj = new GameObject(CanvasName);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // 모든 UI보다 무조건 위에 렌더링되도록 높은 값 부여

            // 해상도 대응을 위해 CanvasScaler 추가
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            // UI 터치 방어용
            canvasObj.AddComponent<GraphicRaycaster>();

            // 캔버스 자식으로 페이드 이미지 생성
            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvasObj.transform, false);

            fadeImage = imageObj.AddComponent<Image>();

            // 화면에 꽉 차도록 앵커(Anchor) 설정
            RectTransform rect = fadeImage.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // 이 씬 전환용 캔버스는 씬이 넘어가도 유지되어야 할 수 있으므로 DontDestroyOnLoad 처리
            DontDestroyOnLoad(canvasObj);

            return fadeImage;
        }
    }
}