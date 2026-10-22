using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    public class SimpleCutInManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject rootUI;
        [SerializeField] private Image cutInImage;
        [SerializeField] private CanvasGroup cutInCanvasGroup;
        [SerializeField] private RectTransform cutInRect;

        [Header("Text References")]
        [SerializeField] private RectTransform textContainer;
        [SerializeField] private TextMeshProUGUI speakerText;
        [SerializeField] private TextMeshProUGUI dialogueText;
        [SerializeField] private GameObject textPanel;
        [SerializeField] private CanvasGroup textCanvasGroup;

        private Coroutine sequenceCoroutine;
        private Coroutine effectCoroutine;
        private Coroutine textEffectCoroutine;

        private Action onSequenceCompleted;
        private CutInStringTable currentStringTable;
        private CutInStepData currentGuideStep;

        private void Awake()
        {
            EnsureTextCanvasGroup();
            if (rootUI != null) rootUI.SetActive(false);
        }

        #region Quest Guide System

        /// <summary>
        /// 퀘스트 가이드 UI를 화면에 띄우고 유지합니다.
        /// </summary>
        public void ShowGuide(CutInStepData step, CutInStringTable stringTable, Action onComplete = null)
        {
            currentStringTable = stringTable;
            currentGuideStep = step;

            if (rootUI != null) rootUI.SetActive(true);

            if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = StartCoroutine(ShowGuideRoutine(step, onComplete));
        }

        private IEnumerator ShowGuideRoutine(CutInStepData step, Action onComplete)
        {
            if (step == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            cutInImage.sprite = step.image;
            cutInRect.anchoredPosition = step.imageOffset;
            bool hasText = SetupText(step);

            if (effectCoroutine != null) StopCoroutine(effectCoroutine);
            if (cutInCanvasGroup != null)
            {
                cutInCanvasGroup.alpha = step.useFadeIn ? 0f : 1f;
            }

            Vector2 targetPos = step.imageOffset;
            if (step.useSlide && cutInRect != null)
            {
                cutInRect.anchoredPosition = GetSlideStartPosition(step.slideDirection, targetPos);
            }

            effectCoroutine = StartCoroutine(EffectRoutine(step, targetPos));
            if (textEffectCoroutine != null) StopCoroutine(textEffectCoroutine);
            if (hasText && step.useTextEnterEffect)
            {
                textEffectCoroutine = StartCoroutine(TextEnterRoutine(step, step.textOffset));
            }

            float enterDuration = 0f;
            if (step.useFadeIn) enterDuration = Mathf.Max(enterDuration, step.fadeDuration);
            if (step.useSlide) enterDuration = Mathf.Max(enterDuration, step.slideDuration);
            if (step.useShake) enterDuration = Mathf.Max(enterDuration, step.shakeDuration);
            if (hasText && step.useTextEnterEffect) enterDuration = Mathf.Max(enterDuration, step.textEnterDuration);

            if (enterDuration > 0f)
            {
                yield return new WaitForSeconds(enterDuration);
            }

            onComplete?.Invoke();
        }

        /// <summary>
        /// 화면에 유지 중인 퀘스트 가이드 UI를 FadeOut 연출과 함께 비활성화합니다.
        /// </summary>
        public void HideGuide(bool useFadeOut = true, float fadeDuration = 0.5f, Action onComplete = null)
        {
            // 이미 오브젝트가 꺼져 있다면 코루틴 실행을 건너뜁니다.
            if (!gameObject.activeInHierarchy)
            {
                onComplete?.Invoke();
                return;
            }

            if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = StartCoroutine(HideGuideRoutine(useFadeOut, fadeDuration, onComplete));
        }

        private IEnumerator HideGuideRoutine(bool useFadeOut, float fadeDuration, Action onComplete)
        {
            if (useFadeOut && fadeDuration > 0f)
            {
                float elapsed = 0f;
                float startCutInAlpha = cutInCanvasGroup != null ? cutInCanvasGroup.alpha : 1f;
                float startTextAlpha = textCanvasGroup != null ? textCanvasGroup.alpha : 1f;

                while (elapsed < fadeDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeDuration);

                    if (cutInCanvasGroup != null) cutInCanvasGroup.alpha = Mathf.Lerp(startCutInAlpha, 0f, t);
                    if (textCanvasGroup != null) textCanvasGroup.alpha = Mathf.Lerp(startTextAlpha, 0f, t);

                    yield return null;
                }

                if (cutInCanvasGroup != null) cutInCanvasGroup.alpha = 0f;
                if (textCanvasGroup != null) textCanvasGroup.alpha = 0f;
            }

            if (textEffectCoroutine != null) StopCoroutine(textEffectCoroutine);
            if (effectCoroutine != null) StopCoroutine(effectCoroutine);

            if (rootUI != null) rootUI.SetActive(false);
            currentGuideStep = null;

            onComplete?.Invoke();
        }

        #endregion

        private void EnsureTextCanvasGroup()
        {
            if (textCanvasGroup == null)
            {
                if (textPanel != null)
                {
                    textCanvasGroup = textPanel.GetComponent<CanvasGroup>();
                    if (textCanvasGroup == null)
                    {
                        textCanvasGroup = textPanel.AddComponent<CanvasGroup>();
                    }
                }
                else if (textContainer != null)
                {
                    textCanvasGroup = textContainer.GetComponent<CanvasGroup>();
                    if (textCanvasGroup == null)
                    {
                        textCanvasGroup = textContainer.gameObject.AddComponent<CanvasGroup>();
                    }
                }
            }
        }

        public void PlaySequence(List<CutInStepData> steps, CutInStringTable stringTable, Action onComplete)
        {
            currentStringTable = stringTable;
            onSequenceCompleted = onComplete;

            if (rootUI != null) rootUI.SetActive(true);

            if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = StartCoroutine(SequenceRoutine(steps));
        }

        private IEnumerator SequenceRoutine(List<CutInStepData> steps)
        {
            foreach (var step in steps)
            {
                yield return StartCoroutine(PlayStepRoutine(step));
            }

            if (textEffectCoroutine != null) StopCoroutine(textEffectCoroutine);
            if (effectCoroutine != null) StopCoroutine(effectCoroutine);

            if (rootUI != null) rootUI.SetActive(false);
            onSequenceCompleted?.Invoke();
        }

        private IEnumerator PlayStepRoutine(CutInStepData step)
        {
            cutInImage.sprite = step.image;
            cutInRect.anchoredPosition = step.imageOffset;

            bool hasText = SetupText(step);

            // 🔊 일반 컷인 스텝 사운드 풀링 재생
            if (!string.IsNullOrEmpty(step.sfxID) && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(step.sfxID);
            }

            if (effectCoroutine != null) StopCoroutine(effectCoroutine);

            if (step.useFadeIn) cutInCanvasGroup.alpha = 0f;
            else cutInCanvasGroup.alpha = 1f;

            Vector2 targetPos = step.imageOffset;
            if (step.useSlide)
            {
                Vector2 slideStartPos = GetSlideStartPosition(step.slideDirection, targetPos);
                cutInRect.anchoredPosition = slideStartPos;
            }

            effectCoroutine = StartCoroutine(EffectRoutine(step, targetPos));

            if (textEffectCoroutine != null) StopCoroutine(textEffectCoroutine);
            if (hasText && step.useTextEnterEffect)
            {
                textEffectCoroutine = StartCoroutine(TextEnterRoutine(step, step.textOffset));
            }

            if (step.progressionMode == ProgressionMode.Timer)
            {
                float timer = step.timerDurationMs;
                if (timer >= 100f) timer /= 1000f;
                yield return new WaitForSeconds(timer);
            }
            else if (step.progressionMode == ProgressionMode.TimerOrInput)
            {
                float timer = step.timerDurationMs;
                if (timer >= 100f) timer /= 1000f;

                yield return null;
                yield return new WaitForSeconds(0.1f);

                float elapsed = 0f;
                while (elapsed < timer)
                {
                    if (Input.GetMouseButtonDown(0) || Input.anyKeyDown) break;
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                yield return null;
            }
            else
            {
                yield return null;
                yield return new WaitForSeconds(0.1f);
                yield return new WaitUntil(() => Input.GetMouseButtonDown(0) || Input.anyKeyDown);
                yield return null;
            }

            Coroutine textExitCo = null;
            if (hasText && step.useTextExitEffect)
            {
                if (textEffectCoroutine != null) StopCoroutine(textEffectCoroutine);
                textExitCo = StartCoroutine(TextExitRoutine(step, step.textOffset));
            }

            Coroutine imageExitCo = null;
            if (step.useFadeOut)
            {
                if (effectCoroutine != null) StopCoroutine(effectCoroutine);
                imageExitCo = StartCoroutine(ImageFadeOutRoutine(step.fadeOutDuration));
            }

            if (textExitCo != null) yield return textExitCo;
            if (imageExitCo != null) yield return imageExitCo;
        }

        private bool SetupText(CutInStepData step)
        {
            EnsureTextCanvasGroup();

            if (string.IsNullOrEmpty(step.stringID) || currentStringTable == null)
            {
                if (textPanel != null) textPanel.SetActive(false);
                return false;
            }

            var stringData = currentStringTable.GetStringData(step.stringID);
            if (stringData != null)
            {
                if (textPanel != null) textPanel.SetActive(true);

                if (speakerText != null) speakerText.text = stringData.speaker;
                if (dialogueText != null) dialogueText.text = stringData.dialogue;

                if (textContainer != null)
                {
                    if (step.useTextEnterEffect)
                    {
                        Vector2 startPos = GetTextOffsetPosition(step.textEnterDirection, step.textOffset, step.textEnterOffset);
                        textContainer.anchoredPosition = startPos;
                        if (textCanvasGroup != null) textCanvasGroup.alpha = 0f;
                    }
                    else
                    {
                        textContainer.anchoredPosition = step.textOffset;
                        if (textCanvasGroup != null) textCanvasGroup.alpha = 1f;
                    }
                }
                return true;
            }
            else
            {
                if (textPanel != null) textPanel.SetActive(false);
                return false;
            }
        }

        private Vector2 GetSlideStartPosition(SlideDirection dir, Vector2 basePos)
        {
            float offsetDistance = 2000f;
            switch (dir)
            {
                case SlideDirection.Top: return basePos + new Vector2(0, offsetDistance);
                case SlideDirection.Bottom: return basePos + new Vector2(0, -offsetDistance);
                case SlideDirection.Left: return basePos + new Vector2(-offsetDistance, 0);
                case SlideDirection.Right: return basePos + new Vector2(offsetDistance, 0);
            }
            return basePos;
        }

        private Vector2 GetTextOffsetPosition(TextPanelDirection dir, Vector2 basePos, float offsetDistance)
        {
            switch (dir)
            {
                case TextPanelDirection.Top: return basePos + new Vector2(0, offsetDistance);
                case TextPanelDirection.Bottom: return basePos + new Vector2(0, -offsetDistance);
                case TextPanelDirection.Left: return basePos + new Vector2(-offsetDistance, 0);
                case TextPanelDirection.Right: return basePos + new Vector2(offsetDistance, 0);
            }
            return basePos;
        }

        private IEnumerator TextEnterRoutine(CutInStepData step, Vector2 targetPos)
        {
            EnsureTextCanvasGroup();
            if (textContainer == null) yield break;

            Vector2 startPos = GetTextOffsetPosition(step.textEnterDirection, targetPos, step.textEnterOffset);
            float duration = Mathf.Max(0.01f, step.textEnterDuration);
            float elapsed = 0f;

            textContainer.anchoredPosition = startPos;
            if (textCanvasGroup != null) textCanvasGroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveVal = step.textEnterCurve != null ? step.textEnterCurve.Evaluate(t) : t;

                textContainer.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, curveVal);
                if (textCanvasGroup != null) textCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

                yield return null;
            }

            textContainer.anchoredPosition = targetPos;
            if (textCanvasGroup != null) textCanvasGroup.alpha = 1f;
        }

        private IEnumerator TextExitRoutine(CutInStepData step, Vector2 currentPos)
        {
            EnsureTextCanvasGroup();
            if (textContainer == null) yield break;

            Vector2 endPos = GetTextOffsetPosition(step.textExitDirection, currentPos, step.textExitOffset);
            float duration = Mathf.Max(0.01f, step.textExitDuration);
            float elapsed = 0f;

            float startAlpha = textCanvasGroup != null ? textCanvasGroup.alpha : 1f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float curveVal = step.textExitCurve != null ? step.textExitCurve.Evaluate(t) : t;

                textContainer.anchoredPosition = Vector2.LerpUnclamped(currentPos, endPos, curveVal);
                if (textCanvasGroup != null) textCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

                yield return null;
            }

            textContainer.anchoredPosition = endPos;
            if (textCanvasGroup != null) textCanvasGroup.alpha = 0f;
            if (textPanel != null) textPanel.SetActive(false);
        }

        private IEnumerator EffectRoutine(CutInStepData step, Vector2 targetPos)
        {
            float elapsed = 0f;
            float maxDuration = 0f;
            if (step.useFadeIn) maxDuration = Mathf.Max(maxDuration, step.fadeDuration);
            if (step.useShake) maxDuration = Mathf.Max(maxDuration, step.shakeDuration);
            if (step.useSlide) maxDuration = Mathf.Max(maxDuration, step.slideDuration);

            Vector2 slideStartPos = cutInRect.anchoredPosition;

            while (elapsed < maxDuration)
            {
                elapsed += Time.deltaTime;

                if (step.useFadeIn && elapsed <= step.fadeDuration)
                {
                    cutInCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / step.fadeDuration);
                }

                Vector2 currentBasePos = targetPos;
                if (step.useSlide)
                {
                    float slideProgress = Mathf.Clamp01(elapsed / step.slideDuration);
                    float curveValue = step.slideCurve.Evaluate(slideProgress);
                    currentBasePos = Vector2.LerpUnclamped(slideStartPos, targetPos, curveValue);
                }

                Vector2 shakeOffset = Vector2.zero;
                if (step.useShake && elapsed <= step.shakeDuration)
                {
                    shakeOffset = UnityEngine.Random.insideUnitCircle * step.shakeIntensity;
                    float shakeProgress = 1f - (elapsed / step.shakeDuration);
                    shakeOffset *= shakeProgress;
                }

                cutInRect.anchoredPosition = currentBasePos + shakeOffset;
                yield return null;
            }

            if (step.useFadeIn) cutInCanvasGroup.alpha = 1f;
            cutInRect.anchoredPosition = targetPos;
        }

        private IEnumerator ImageFadeOutRoutine(float duration)
        {
            if (cutInCanvasGroup == null) yield break;

            float elapsed = 0f;
            float startAlpha = cutInCanvasGroup.alpha;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cutInCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            cutInCanvasGroup.alpha = 0f;
        }
    }
}