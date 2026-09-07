using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    [RequireComponent(typeof(AudioSource))]
    public class UnifiedCutInDialogueManager : MonoBehaviour
    {
        public enum SlideDirection { Top, Bottom, Left, Right }
        public enum DialogueProgressionMode { Timer, WaitInput, TimerOrInput }
        public enum CutInSlot { Left, Right, Center }

        [System.Serializable]
        public struct SpeakerVisual
        {
            public string speakerName;
            public Sprite dialogueSprite;
        }

        [System.Serializable]
        public class CutInSlotUI
        {
            public CutInSlot slotType;
            public Image cutInImage;
            public CanvasGroup cutInCanvasGroup;
            public RectTransform cutInRect;
        }

        [System.Serializable]
        public class CutInDialogueStep
        {
            [Header("--- [1. Time / Delay Settings] ---")]
            [Tooltip("이 대사(스텝)가 시작되기 전 대기할 시간 (초)")]
            public float startDelay = 0f;

            [Header("--- [2. Cut-In Image Settings] ---")]
            public Sprite cutInSprite;
            [Tooltip("이 캐릭터를 어느 위치(슬롯)에 띄울 것인가?")]
            public CutInSlot targetSlot = CutInSlot.Center;
            public Vector2 cutInOffset;
            
            [Space(5)]
            public bool useFadeIn;
            public float fadeDuration = 0.5f;
            public bool useSlide;
            public SlideDirection slideDirection;
            public float slideDuration = 0.5f;
            public AnimationCurve slideCurve = AnimationCurve.Linear(0, 0, 1, 1);
            public bool useShake;
            public float shakeDuration = 0.3f;
            public float shakeIntensity = 10f;

            [Header("--- [3. Multi-Slot & Persistence Settings] ---")]
            [Tooltip("이 대사가 끝난 후에도 현재 슬롯의 캐릭터를 화면에 계속 남겨둘 것인가?")]
            public bool persistCutIn = true;
            [Tooltip("이 대사가 시작될 때, 다른 위치(슬롯)에 떠 있는 모든 캐릭터를 지울 것인가?")]
            public bool clearOtherSlots = false;

            [Header("--- [4. Dialogue Text Settings] ---")]
            [Tooltip("화자 이름 (대사창 스킨 매칭용으로만 사용되며, 화면에 텍스트로 출력되지 않습니다.)")]
            public string speakerName;
            [TextArea(3, 5)]
            public string dialogueText;
            public bool showDialogue = true;

            [Header("--- [5. Audio / Dubbing Settings] ---")]
            [Tooltip("이 대사가 나올 때 재생할 성우 더빙 음성 클립")]
            public AudioClip dubbingClip;
            public string sfxID;

            [Header("--- [6. Progression Settings] ---")]
            public DialogueProgressionMode progressionMode;
            [Tooltip("Timer 또는 TimerOrInput 모드일 때 대기 시간 (초)")]
            public float duration = 2f;
        }

        [Header("Root UI Panel")]
        [SerializeField] private GameObject rootUI;

        [Header("Cut-In Multi-Slot UI List (Left, Right, Center 등록)")]
        [SerializeField] private List<CutInSlotUI> slots = new List<CutInSlotUI>();

        [Header("Dialogue UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private RectTransform dialogueRect;
        [SerializeField] private Image dialogueBgImage;
        [SerializeField] private TextMeshProUGUI dialogueText; // ◀ speakerText 레퍼런스 깔끔하게 삭제 완료!

        [Header("Speaker Database")]
        [SerializeField] private List<SpeakerVisual> speakerDatabase;

        [Header("Layout Settings (Dialogue Bounce)")]
        [SerializeField] private Vector2 boxPosition = new Vector2(0f, -255f);
        [SerializeField] private Vector2 boxSize = new Vector2(2048f, 1045f);
        [SerializeField] private Vector3 boxScale = new Vector3(0.5f, 0.5f, 1f);
        [SerializeField] private float bounceAmount = 0.1f;

        private AudioSource audioSource;
        private Coroutine sequenceCoroutine;
        private Vector3 originalBoxScale;
        private Action onSequenceCompleted;

        private Dictionary<CutInSlot, Coroutine> slotCoroutines = new Dictionary<CutInSlot, Coroutine>();

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            originalBoxScale = boxScale;
            
            foreach (var slot in slots)
            {
                if (slot.cutInImage != null) slot.cutInImage.gameObject.SetActive(false);
                if (slot.cutInCanvasGroup != null) slot.cutInCanvasGroup.alpha = 0f;
            }

            if (rootUI != null) rootUI.SetActive(false);
        }

        public void PlaySequence(List<CutInDialogueStep> steps, Action onComplete)
        {
            onSequenceCompleted = onComplete;

            if (rootUI != null) rootUI.SetActive(true);

            if (sequenceCoroutine != null) StopCoroutine(sequenceCoroutine);
            sequenceCoroutine = StartCoroutine(SequenceRoutine(steps));
        }

        private IEnumerator SequenceRoutine(List<CutInDialogueStep> steps)
        {
            foreach (var step in steps)
            {
                yield return StartCoroutine(PlayStepRoutine(step));
            }

            foreach (var slot in slots)
            {
                StartCoroutine(FadeOutSlotRoutine(slot, 0.3f));
            }

            yield return new WaitForSeconds(0.35f);

            if (rootUI != null) rootUI.SetActive(false);
            onSequenceCompleted?.Invoke();
        }

        private IEnumerator PlayStepRoutine(CutInDialogueStep step)
        {
            if (step.startDelay > 0f)
            {
                yield return new WaitForSeconds(step.startDelay);
            }

            if (step.clearOtherSlots)
            {
                ClearOtherSlots(step.targetSlot);
            }

            SetupDialogue(step);
            PlayStepSound(step);

            CutInSlotUI slotUI = GetSlotUI(step.targetSlot);
            if (slotUI != null && step.cutInSprite != null)
            {
                SetupCutIn(slotUI, step);

                if (slotCoroutines.TryGetValue(step.targetSlot, out Coroutine activeCoroutine))
                {
                    if (activeCoroutine != null) StopCoroutine(activeCoroutine);
                }
                slotCoroutines[step.targetSlot] = StartCoroutine(CutInEffectRoutine(slotUI, step, step.cutInOffset));
            }

            if (step.showDialogue && dialoguePanel != null)
            {
                StartCoroutine(DialogueBounceRoutine());
            }

            if (step.progressionMode == DialogueProgressionMode.Timer)
            {
                yield return new WaitForSeconds(step.duration);
            }
            else if (step.progressionMode == DialogueProgressionMode.TimerOrInput)
            {
                yield return null;
                yield return new WaitForSeconds(0.1f);

                float elapsed = 0f;
                while (elapsed < step.duration)
                {
                    if (Input.GetMouseButtonDown(0) || Input.anyKeyDown)
                        break;
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

            if (!step.persistCutIn && slotUI != null)
            {
                StartCoroutine(FadeOutSlotRoutine(slotUI, 0.3f));
            }
        }

        private CutInSlotUI GetSlotUI(CutInSlot slotType)
        {
            foreach (var slot in slots)
            {
                if (slot.slotType == slotType) return slot;
            }
            return null;
        }

        private void ClearOtherSlots(CutInSlot activeSlot)
        {
            foreach (var slot in slots)
            {
                if (slot.slotType != activeSlot)
                {
                    StartCoroutine(FadeOutSlotRoutine(slot, 0.3f));
                }
            }
        }

        private IEnumerator FadeOutSlotRoutine(CutInSlotUI slot, float duration)
        {
            if (slot == null || slot.cutInCanvasGroup == null) yield break;

            float elapsed = 0f;
            float startAlpha = slot.cutInCanvasGroup.alpha;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                slot.cutInCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                yield return null;
            }
            slot.cutInCanvasGroup.alpha = 0f;
            if (slot.cutInImage != null) slot.cutInImage.gameObject.SetActive(false);
        }

        private void SetupDialogue(CutInDialogueStep step)
        {
            if (!step.showDialogue)
            {
                if (dialoguePanel != null) dialoguePanel.SetActive(false);
                return;
            }

            if (dialoguePanel != null) dialoguePanel.SetActive(true);

            // 텍스트는 이제 대사만 셋업합니다.
            if (dialogueText != null) dialogueText.text = step.dialogueText;

            // [기능 유지] 화면에 이름은 안 나와도, 대화창 스킨을 가려내기 위해 speakerName 비교 연산은 그대로 수행합니다!
            Sprite targetSprite = null;
            foreach (var db in speakerDatabase)
            {
                if (db.speakerName == step.speakerName)
                {
                    targetSprite = db.dialogueSprite;
                    break;
                }
            }
            if (targetSprite != null && dialogueBgImage != null)
            {
                dialogueBgImage.sprite = targetSprite;
            }

            if (dialogueRect != null)
            {
                dialogueRect.anchoredPosition = boxPosition;
                dialogueRect.sizeDelta = boxSize;
                dialogueRect.localScale = boxScale;
            }
        }

        private void SetupCutIn(CutInSlotUI slot, CutInDialogueStep step)
        {
            if (slot == null || slot.cutInImage == null) return;

            slot.cutInImage.gameObject.SetActive(true);
            slot.cutInImage.sprite = step.cutInSprite;

            if (step.useFadeIn && slot.cutInCanvasGroup != null)
            {
                slot.cutInCanvasGroup.alpha = 0f;
            }
            else if (slot.cutInCanvasGroup != null)
            {
                slot.cutInCanvasGroup.alpha = 1f;
            }

            if (step.useSlide && slot.cutInRect != null)
            {
                Vector2 slideStartPos = GetSlideStartPosition(step.slideDirection, step.cutInOffset);
                slot.cutInRect.anchoredPosition = slideStartPos;
            }
            else if (slot.cutInRect != null)
            {
                slot.cutInRect.anchoredPosition = step.cutInOffset;
            }
        }

        private void PlayStepSound(CutInDialogueStep step)
        {
            if (audioSource != null && step.dubbingClip != null)
            {
                audioSource.Stop();
                audioSource.clip = step.dubbingClip;
                audioSource.Play();
            }

            if (!string.IsNullOrEmpty(step.sfxID))
            {
                Debug.Log($"[Unified CutIn] Play SFX: {step.sfxID}");
            }
        }

        private IEnumerator DialogueBounceRoutine()
        {
            if (dialogueRect == null) yield break;

            float bounceTimer = 0f;
            float bounceDuration = 0.15f;
            while (bounceTimer < bounceDuration)
            {
                bounceTimer += Time.deltaTime;
                float t = bounceTimer / bounceDuration;
                float scaleOffset = Mathf.Sin(t * Mathf.PI) * bounceAmount;
                dialogueRect.localScale = originalBoxScale + new Vector3(scaleOffset, scaleOffset, 0f);
                yield return null;
            }
            dialogueRect.localScale = originalBoxScale;
        }

        private IEnumerator CutInEffectRoutine(CutInSlotUI slot, CutInDialogueStep step, Vector2 targetPos)
        {
            if (slot == null || slot.cutInRect == null) yield break;

            float elapsed = 0f;
            float maxDuration = 0f;

            if (step.useFadeIn) maxDuration = Mathf.Max(maxDuration, step.fadeDuration);
            if (step.useShake) maxDuration = Mathf.Max(maxDuration, step.shakeDuration);
            if (step.useSlide) maxDuration = Mathf.Max(maxDuration, step.slideDuration);

            Vector2 slideStartPos = slot.cutInRect.anchoredPosition;

            while (elapsed < maxDuration)
            {
                elapsed += Time.deltaTime;

                if (step.useFadeIn && slot.cutInCanvasGroup != null && elapsed <= step.fadeDuration)
                {
                    slot.cutInCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / step.fadeDuration);
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

                slot.cutInRect.anchoredPosition = currentBasePos + shakeOffset;
                yield return null;
            }

            if (step.useFadeIn && slot.cutInCanvasGroup != null) slot.cutInCanvasGroup.alpha = 1f;
            slot.cutInRect.anchoredPosition = targetPos;
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
    }
}