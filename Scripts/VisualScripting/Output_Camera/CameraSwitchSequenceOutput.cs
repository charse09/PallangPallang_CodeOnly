using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    [System.Serializable]
    public struct CameraStep
    {
        [Tooltip("활성화할 카메라 오브젝트")]
        public GameObject cameraObject;

        [Tooltip("이 카메라를 유지할 시간 (초)")]
        public float stayDuration;

        [Tooltip("다음 카메라로 넘어갈 때 페이드 전환을 사용할지 여부")]
        public bool useFadeTransition;
    }

    /// <summary>
    /// 여러 대의 카메라를 순차적으로 전환하며 연출을 만드는 Output 모듈
    /// </summary>
    public class CameraSwitchSequenceOutput : ProcessBase
    {
        [Header("Sequence Settings")]
        [SerializeField] private List<CameraStep> cameraSteps = new List<CameraStep>();

        [Header("Fade Options")]
        [SerializeField] private Color fadeColor = Color.black;
        [SerializeField] private float fadeDuration = 0.5f;

        private bool isPlaying = false;
        private Image fadeImage;

        public override void Execute()
        {
            if (!isPlaying && cameraSteps.Count > 0)
            {
                StartCoroutine(PlaySequence());
            }
        }

        private IEnumerator PlaySequence()
        {
            isPlaying = true;

            for (int i = 0; i < cameraSteps.Count; i++)
            {
                CameraStep step = cameraSteps[i];

                if (step.cameraObject == null) continue;

                // 1. 카메라 전환
                SwitchToCamera(i);

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[{gameObject.name}]</color> 카메라 전환: {step.cameraObject.name} ({step.stayDuration}초 유지)");
#endif

                // 2. 유지 시간만큼 대기
                yield return new WaitForSeconds(step.stayDuration);

                // 3. 페이드 전환이 설정되어 있고, 다음 카메라가 있다면 페이드 연출
                if (step.useFadeTransition && i < cameraSteps.Count - 1)
                {
                    yield return StartCoroutine(FadeRoutine(0, 1)); // 암전
                    // 암전된 상태에서 루프가 다음으로 넘어가며 카메라를 바꿈
                    yield return StartCoroutine(FadeRoutine(1, 0)); // 다시 밝아짐
                }
            }

            isPlaying = false;
            IsOn = true; // 시퀀스 완료 후 다음 노드 실행
        }

        private void SwitchToCamera(int index)
        {
            for (int i = 0; i < cameraSteps.Count; i++)
            {
                if (cameraSteps[i].cameraObject != null)
                {
                    cameraSteps[i].cameraObject.SetActive(i == index);
                }
            }
        }

        private IEnumerator FadeRoutine(float startAlpha, float endAlpha)
        {
            if (fadeImage == null) fadeImage = SetupFadeImage();

            fadeImage.gameObject.SetActive(true);
            float elapsed = 0;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float a = Mathf.Lerp(startAlpha, endAlpha, elapsed / fadeDuration);
                fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, a);
                yield return null;
            }
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, endAlpha);

            if (endAlpha <= 0) fadeImage.gameObject.SetActive(false);
        }

        private Image SetupFadeImage()
        {
            // 이전에 만든 ScreenFadeOutput의 캔버스를 찾거나 생성
            GameObject canvasObj = GameObject.Find("GOS_GlobalFadeCanvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("GOS_GlobalFadeCanvas");
                Canvas c = canvasObj.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 999;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            }

            Transform imgT = canvasObj.transform.Find("FadeImage");
            if (imgT != null) return imgT.GetComponent<Image>();

            GameObject imgObj = new GameObject("FadeImage");
            imgObj.transform.SetParent(canvasObj.transform, false);
            Image img = imgObj.AddComponent<Image>();
            RectTransform rt = img.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return img;
        }
    }
}