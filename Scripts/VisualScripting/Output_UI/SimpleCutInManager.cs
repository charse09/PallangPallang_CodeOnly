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
        [SerializeField] private GameObject textPanel; // 화자나 대사가 없을 때 숨기기 위함
        [SerializeField] private CanvasGroup textCanvasGroup; // TextPanel 페이드 및 알파 제어용

        private Coroutine sequenceCoroutine;
        private Coroutine effectCoroutine;
        private Coroutine textEffectCoroutine;
        
        private Action onSequenceCompleted;
        private CutInStringTable currentStringTable;

        private CutInStepData currentGuideStep; //0907

        private void Awake()
        {
            EnsureTextCanvasGroup();
            if (rootUI != null) rootUI.SetActive(false);
        }

        //======================================0907==============================
        // SimpleCutInManager.cs 내부에 추가

        /// <summary>
        /// 퀘스트 가이드 UI를 화면에 띄우고 자동으로 닫지 않은 채 유지합니다.
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

            if (!string.IsNullOrEmpty(step.sfxID) && SoundManager.instance != null)
            {
                SoundManager.instance.PlaySFX(step.sfxID);
            }

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

            // 입력이나 타이머 대기 없이 UI를 띄워둔 채로 시퀀스 완료 신호 전달
            onComplete?.Invoke();
        }

        /// <summary>
        /// 화면에 유지 중인 퀘스트 가이드 UI를 FadeOut 연출과 함께 비활성화합니다.
        /// </summary>
        public void HideGuide(bool useFadeOut = true, float fadeDuration = 0.5f, Action onComplete = null)
        {
            if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = StartCoroutine(HideGuideRoutine(useFadeOut, fadeDuration, onComplete));
        }

        private IEnumerator HideGuideRoutine(bool useFadeOut, float fadeDuration, Action onComplete)
        {
            // 1. FadeOut 연출 실행
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

            // 2. 실행 중이던 이펙트 정리 및 UI 비활성화
            if (textEffectCoroutine != null) StopCoroutine(textEffectCoroutine);
            if (effectCoroutine != null) StopCoroutine(effectCoroutine);

            if (rootUI != null) rootUI.SetActive(false);
            currentGuideStep = null;

            onComplete?.Invoke();
        }

        // ==================================================0907==========

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
            // 1. 초기 셋업 (이미지, 텍스트)
            cutInImage.sprite = step.image;
            cutInRect.anchoredPosition = step.imageOffset;
            
            bool hasText = SetupText(step);

            // 2. 효과음 재생
            if (!string.IsNullOrEmpty(step.sfxID))
            {
                // TODO: 실제 프로젝트의 사운드 매니저 연동
                Debug.Log($"[CutIn] Play SFX: {step.sfxID}");
            }

            // 3. 효과 적용 준비
            if (effectCoroutine != null) StopCoroutine(effectCoroutine);
            
            // FadeIn 초기화
            if (step.useFadeIn) cutInCanvasGroup.alpha = 0f;
            else cutInCanvasGroup.alpha = 1f;

            // Slide 초기 위치 설정
            Vector2 targetPos = step.imageOffset;
            if (step.useSlide)
            {
                Vector2 slideStartPos = GetSlideStartPosition(step.slideDirection, targetPos);
                cutInRect.anchoredPosition = slideStartPos;
            }

            // 4. 효과 병렬 실행 (코루틴)
            effectCoroutine = StartCoroutine(EffectRoutine(step, targetPos));

            if (textEffectCoroutine != null) StopCoroutine(textEffectCoroutine);
            if (hasText && step.useTextEnterEffect)
            {
                textEffectCoroutine = StartCoroutine(TextEnterRoutine(step, step.textOffset));
            }

            // 5. 진행 대기 (입력 또는 타이머)
            if (step.progressionMode == ProgressionMode.Timer)
            {
                float timer = step.timerDurationMs;
                if (timer >= 100f)
                {
                    timer /= 1000f;
                }
                yield return new WaitForSeconds(timer);
            }
            else if (step.progressionMode == ProgressionMode.TimerOrInput)
            {
                float timer = step.timerDurationMs;
                if (timer >= 100f)
                {
                    timer /= 1000f;
                }

                // 이전 입력이 남아있어 즉시 넘어가는 현상 방지
                yield return null;
                yield return new WaitForSeconds(0.1f);

                float elapsed = 0f;
                while (elapsed < timer)
                {
                    if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
                        break;
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                // 다음 스텝으로 넘어갈 때 입력이 겹치지 않게 1프레임 대기
                yield return null;
            }
            else // WaitInput
            {
                // 이전 노드를 트리거했던 클릭/키 입력이 남아있어 즉시 넘어가는 현상 방지
                yield return null;
                yield return new WaitForSeconds(0.1f); // 연타 오작동 방지용 짧은 딜레이

                // 입력 대기 (아무 키나 터치) - 프로젝트 입력 시스템에 맞춰 변경 가능
                yield return new WaitUntil(() => Input.GetMouseButtonDown(0) || Input.anyKeyDown);
                
                // 다음 스텝으로 넘어갈 때 입력이 겹치지 않게 1프레임 대기
                yield return null; 
            }

            // 6. 스텝 완료 후 퇴장 효과 실행
            if (hasText && step.useTextExitEffect)
            {
                if (textEffectCoroutine != null) StopCoroutine(textEffectCoroutine);
                yield return StartCoroutine(TextExitRoutine(step, step.textOffset));
            }
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
            // 화면 밖에서 등장하도록 화면 크기 기반으로 임의 계산
            // 실제 캔버스 해상도에 맞춰 조절 필요 (예: 1920x1080)
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
            
            // 효과들의 최대 지속 시간 계산
            float maxDuration = 0f;
            if (step.useFadeIn) maxDuration = Mathf.Max(maxDuration, step.fadeDuration);
            if (step.useShake) maxDuration = Mathf.Max(maxDuration, step.shakeDuration);
            if (step.useSlide) maxDuration = Mathf.Max(maxDuration, step.slideDuration);

            Vector2 slideStartPos = cutInRect.anchoredPosition;

            while (elapsed < maxDuration)
            {
                elapsed += Time.deltaTime;

                // 1. FadeIn
                if (step.useFadeIn && elapsed <= step.fadeDuration)
                {
                    cutInCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / step.fadeDuration);
                }

                // 2. Slide
                Vector2 currentBasePos = targetPos; // Slide 안쓸 때 기본 위치
                if (step.useSlide)
                {
                    float slideProgress = Mathf.Clamp01(elapsed / step.slideDuration);
                    float curveValue = step.slideCurve.Evaluate(slideProgress);
                    currentBasePos = Vector2.LerpUnclamped(slideStartPos, targetPos, curveValue);
                }

                // 3. Shake
                Vector2 shakeOffset = Vector2.zero;
                if (step.useShake && elapsed <= step.shakeDuration)
                {
                    // Random inside unit circle
                    shakeOffset = UnityEngine.Random.insideUnitCircle * step.shakeIntensity;
                    // shake intensity decreases over time
                    float shakeProgress = 1f - (elapsed / step.shakeDuration);
                    shakeOffset *= shakeProgress;
                }

                // 최종 위치 적용
                cutInRect.anchoredPosition = currentBasePos + shakeOffset;

                yield return null;
            }

            // 완료 후 최종 상태 보장
            if (step.useFadeIn) cutInCanvasGroup.alpha = 1f;
            cutInRect.anchoredPosition = targetPos;
        }
    }
}
