using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    public class ScreenFadeSequence : ProcessBase
    {
        public enum FadeType
        {
            FadeIn,
            FadeOut
        }

        [Header("Fade Target")]
        [SerializeField] private Image fadeImage;

        [Header("Fade Settings")]
        [SerializeField] private FadeType fadeType = FadeType.FadeOut;
        [SerializeField] private float fadeDuration = 1f;
        [SerializeField] private Color fadeColor = Color.black;

        public override void Execute()
        {
            if (IsOn) return;
            if (fadeImage == null) return;

            StartCoroutine(C_Fade());
        }

        private IEnumerator C_Fade()
        {
            IsOn = true;

            fadeImage.gameObject.SetActive(true);

            float startAlpha = fadeType == FadeType.FadeOut ? 0f : 1f;
            float endAlpha = fadeType == FadeType.FadeOut ? 1f : 0f;

            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;

                float t = elapsedTime / fadeDuration;
                float alpha = Mathf.Lerp(startAlpha, endAlpha, t);

                fadeImage.color = new Color(
                    fadeColor.r,
                    fadeColor.g,
                    fadeColor.b,
                    alpha
                );

                yield return null;
            }

            fadeImage.color = new Color(
                fadeColor.r,
                fadeColor.g,
                fadeColor.b,
                endAlpha
            );

            if (fadeType == FadeType.FadeIn)
            {
                fadeImage.gameObject.SetActive(false);
            }

            IsOn = false;
        }
    }
}