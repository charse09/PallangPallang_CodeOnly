using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 화면을 지정된 색상과 강도로 번쩍이게(Flash) 만드는 연출용 Output
    /// </summary>
    public class ScreenFlashOutput : ProcessBase
    {
        [Header("Flash Settings")]
        [Tooltip("점멸할 때의 색상을 지정합니다. (기본값: 흰색)")]
        [SerializeField] private Color flashColor = Color.white;

        [Tooltip("점멸의 최대 강도 (0.0 = 투명, 1.0 = 완전히 불투명하게 덮임)")]
        [Range(0f, 1f)]
        [SerializeField] private float intensity = 0.8f;

        [Tooltip("번쩍이는 총 횟수 (1이면 단발성 점멸)")]
        [SerializeField] private int flashCount = 1;

        [Tooltip("한 번 번쩍일 때 걸리는 시간(초) - 켜졌다 꺼지는 전체 시간")]
        [SerializeField] private float flashDuration = 0.1f;

        [Tooltip("여러 번 번쩍일 경우, 점멸과 점멸 사이의 대기 시간(초)")]
        [SerializeField] private float intervalDuration = 0.05f;

        private bool isFlashing = false;
        private const string CanvasName = "GOS_GlobalEffectCanvas";

        public override void Execute()
        {
            if (!isFlashing)
            {
                StartCoroutine(FlashRoutine());
            }
        }

        private IEnumerator FlashRoutine()
        {
            isFlashing = true;

            // 페이드/플래시용 캔버스와 이미지 가져오기
            Image flashImage = SetupFlashCanvas();

            // 초기 투명한 색상과 최고 강도 색상 설정
            Color clearColor = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
            Color targetColor = new Color(flashColor.r, flashColor.g, flashColor.b, intensity);

            flashImage.color = clearColor;
            flashImage.gameObject.SetActive(true);

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 화면 점멸 시작! (횟수: {flashCount}, 강도: {intensity})");
#endif

            // 지정된 횟수만큼 점멸 반복
            for (int i = 0; i < flashCount; i++)
            {
                float halfDuration = flashDuration / 2f;
                float elapsed = 0f;

                // 1. 순식간에 밝아짐 (Fade In)
                while (elapsed < halfDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / halfDuration);
                    flashImage.color = Color.Lerp(clearColor, targetColor, t);
                    yield return null;
                }
                flashImage.color = targetColor;

                // 2. 순식간에 어두워짐 (Fade Out)
                elapsed = 0f;
                while (elapsed < halfDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / halfDuration);
                    flashImage.color = Color.Lerp(targetColor, clearColor, t);
                    yield return null;
                }
                flashImage.color = clearColor;

                // 3. 점멸 간격 대기 (마지막 점멸이 아닐 경우에만)
                if (i < flashCount - 1 && intervalDuration > 0f)
                {
                    yield return new WaitForSeconds(intervalDuration);
                }
            }

            // 모든 점멸이 끝나면 비활성화하여 리소스 절약
            flashImage.gameObject.SetActive(false);
            isFlashing = false;

            // 4. 다음 노드 실행
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 화면 점멸 완료.");
#endif
        }

        /// <summary>
        /// 씬에 글로벌 캔버스가 없으면 생성하고, 플래시용 Image 컴포넌트를 반환합니다.
        /// </summary>
        private Image SetupFlashCanvas()
        {
            GameObject canvasObj = GameObject.Find(CanvasName);

            // 캔버스가 아예 없으면 새로 생성
            if (canvasObj == null)
            {
                canvasObj = new GameObject(CanvasName);
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 998; // 화면을 가려야 하므로 높은 숫자 배정

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

                DontDestroyOnLoad(canvasObj);
            }

            // 해당 캔버스 안에 "FlashImage"라는 오브젝트가 있는지 확인
            Transform imageTransform = canvasObj.transform.Find("FlashImage");
            Image flashImage = null;

            if (imageTransform != null)
            {
                flashImage = imageTransform.GetComponent<Image>();
            }
            else
            {
                // 없으면 새로 생성
                GameObject imageObj = new GameObject("FlashImage");
                imageObj.transform.SetParent(canvasObj.transform, false);

                flashImage = imageObj.AddComponent<Image>();

                // 화면 전체를 덮도록 앵커 설정
                RectTransform rect = flashImage.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                // ★ 중요: 점멸하는 동안 화면 클릭이나 터치를 방해하지 않도록 Raycast 차단
                flashImage.raycastTarget = false;
            }

            return flashImage;
        }
    }
}