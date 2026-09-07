/*
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    [RequireComponent(typeof(AudioSource))]
    public class DissolveCutInDialogueManager : MonoBehaviour
    {
        public enum SlideDirection { Top, Bottom, Left, Right }
        public enum DialogueProgressionMode { Timer, WaitInput, TimerOrInput }
        public enum CutInSlot { Left, Right, Center }

        public enum CutInExitMode
        {
            KeepOn,     // 계속 유지
            FadeOut,    // 지정 시간 동안 서서히 사라짐
            InstantOff  // 즉시 끔
        }

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
            [Tooltip("디졸브(Crossfade) 연출을 위한 보조 이미지 (비워두면 자동 복제 생성)")]
            public Image dissolveImage;
            public CanvasGroup cutInCanvasGroup;
            public RectTransform cutInRect;
        }

        [System.Serializable]
        public class CutInDialogueStep
        {
            [Header("--- [1. Step Delay Settings] ---")]
            [Tooltip("이 스텝(대사/연출)이 시작되기 전 전체 대기 시간 (초)")]
            public float startDelay = 0f;

            [Header("--- [2. Cut-In Image Settings] ---")]
            public Sprite cutInSprite;
            [Tooltip("이 캐릭터를 어느 위치(슬롯)에 띄울 것인가?")]
            public CutInSlot targetSlot = CutInSlot.Center;
            public Vector2 cutInOffset;

            [Space(5)]
            [Tooltip("대사가 시작된 후 컷인이 등장할 때까지의 개별 딜레이 시간 (초)\n예: 1.0 지정 시 대사 출력 후 1초 뒤에 컷인 등장")]
            public float cutInStartDelay = 0f;

            public bool useFadeIn;
            public float fadeDuration = 0.5f;

            [Tooltip("이전 이미지에서 새 이미지로 스르륵 녹아들며 바뀌는 디졸브 연출 사용")]
            public bool useDissolve;
            public float dissolveDuration = 0.5f;

            public bool useSlide;
            public SlideDirection slideDirection;
            public float slideDuration = 0.5f;
            public AnimationCurve slideCurve = AnimationCurve.Linear(0, 0, 1, 1);

            public bool useShake;
            public float shakeDuration = 0.3f;
            public float shakeIntensity = 10f;

            [Header("--- [3. Cut-In Independent Timer Settings] ---")]
            [Tooltip("체크 시 대사 진행/클릭 여부와 상관없이 컷인이 지정한 유지 시간 후 독립적으로 꺼집니다.")]
            public bool useCutInTimer = false;

            [Tooltip("useCutInTimer 체크 시: 컷인 이미지가 화면에 유지될 시간 (초)")]
            public float cutInDisplayDuration = 2.0f;

            [Tooltip("컷인이 꺼질 때의 퇴장 방식\n• KeepOn: 계속 유지\n• FadeOut: 서서히 사라짐\n• InstantOff: 즉시 끔")]
            public CutInExitMode cutInExitMode = CutInExitMode.KeepOn;

            [Tooltip("cutInExitMode가 FadeOut일 때 페이드아웃 소요 시간 (초)")]
            public float exitFadeDuration = 0.3f;

            [Tooltip("이 대사가 시작될 때, 다른 위치(슬롯)에 떠 있는 모든 캐릭터를 지울 것인가?")]
            public bool clearOtherSlots = false;

            [Header("--- [4. Dialogue Text Settings] ---")]
            public string speakerName;
            [TextArea(3, 5)]
            public string dialogueText;

            [Tooltip("체크 시 대화창을 화면에 보입니다.")]
            public bool showDialogue = true;
            [Tooltip("체크 시 이 대사 스텝이 끝나는 순간 대화창만 끕니다.")]
            public bool hideDialogueOnStepEnd = false;

            [Header("--- [5. Audio / Dubbing Settings] ---")]
            public AudioClip dubbingClip;
            public string sfxID;

            [Header("--- [6. Progression Settings] ---")]
            public DialogueProgressionMode progressionMode;
            public float duration = 2f;
        }

        [Header("Root UI Panel")]
        [SerializeField] private GameObject rootUI;

        [Header("Global Auto-Close Setting")]
        [Tooltip("체크 시 전체 대화 시퀀스가 종료되면 화면 전체(대화창, 컷인)를 자동으로 끕니다.")]
        [SerializeField] private bool autoCloseAtSequenceEnd = true;

        [Header("Cut-In Multi-Slot UI List (Left, Right, Center 등록)")]
        [SerializeField] private List<CutInSlotUI> slots = new List<CutInSlotUI>();

        [Header("Dialogue UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private RectTransform dialogueRect;
        [SerializeField] private Image dialogueBgImage;
        [SerializeField] private TextMeshProUGUI dialogueText;

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
                if (slot.dissolveImage != null) slot.dissolveImage.gameObject.SetActive(false);
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

            if (autoCloseAtSequenceEnd)
            {
                foreach (var slot in slots)
                {
                    StartCoroutine(FadeOutSlotRoutine(slot, 0.3f));
                }

                if (dialoguePanel != null) dialoguePanel.SetActive(false);

                yield return new WaitForSeconds(0.35f);

                if (rootUI != null) rootUI.SetActive(false);
            }

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
                // 기존 슬롯에서 실행 중인 컷인 애니메이션이 있다면 중단
                if (slotCoroutines.TryGetValue(step.targetSlot, out Coroutine activeCoroutine))
                {
                    if (activeCoroutine != null) StopCoroutine(activeCoroutine);
                }

                // 컷인 전용 독립 제어 코루틴 실행
                slotCoroutines[step.targetSlot] = StartCoroutine(CutInLifecycleRoutine(slotUI, step));
            }

            if (step.showDialogue && dialoguePanel != null)
            {
                StartCoroutine(DialogueBounceRoutine());
            }

            // 대사 진행 대기 (Timer / WaitInput / TimerOrInput)
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

            // 스텝 종료 시 대화창 제어
            if (step.hideDialogueOnStepEnd && dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            // useCutInTimer가 꺼져 있는 경우에만 대사 스텝이 끝날 때 컷인 퇴장 처리
            if (!step.useCutInTimer && slotUI != null)
            {
                HandleCutInExit(slotUI, step);
            }
        }

        /// <summary>
        /// 컷인의 독립적인 라이프사이클 (등장 딜레이 -> 등장 -> 유지 시간 -> 퇴장)
        /// </summary>
        private IEnumerator CutInLifecycleRoutine(CutInSlotUI slotUI, CutInDialogueStep step)
        {
            // 1. 컷인 개별 등장 딜레이
            if (step.cutInStartDelay > 0f)
            {
                yield return new WaitForSeconds(step.cutInStartDelay);
            }

            // 2. 컷인 등장 연출 (Dissolve 또는 일반 연출)
            if (step.useDissolve && slotUI.cutInImage != null && slotUI.cutInImage.sprite != null && slotUI.cutInImage.gameObject.activeSelf)
            {
                yield return StartCoroutine(DissolveEffectRoutine(slotUI, step));
            }
            else
            {
                SetupCutIn(slotUI, step);
                yield return StartCoroutine(CutInEffectRoutine(slotUI, step, step.cutInOffset));
            }

            // 3. 독립 타이머가 켜져 있으면 대사 진행과 무관하게 지정한 유지 시간 후 자동으로 퇴장
            if (step.useCutInTimer)
            {
                yield return new WaitForSeconds(step.cutInDisplayDuration);
                HandleCutInExit(slotUI, step);
            }
        }

        private void HandleCutInExit(CutInSlotUI slotUI, CutInDialogueStep step)
        {
            switch (step.cutInExitMode)
            {
                case CutInExitMode.FadeOut:
                    StartCoroutine(FadeOutSlotRoutine(slotUI, step.exitFadeDuration));
                    break;

                case CutInExitMode.InstantOff:
                    TurnOffSlotInstantly(slotUI);
                    break;

                case CutInExitMode.KeepOn:
                default:
                    break;
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

        private void TurnOffSlotInstantly(CutInSlotUI slot)
        {
            if (slot == null) return;
            if (slot.cutInCanvasGroup != null) slot.cutInCanvasGroup.alpha = 0f;
            if (slot.cutInImage != null) slot.cutInImage.gameObject.SetActive(false);
            if (slot.dissolveImage != null) slot.dissolveImage.gameObject.SetActive(false);
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
            if (slot.dissolveImage != null) slot.dissolveImage.gameObject.SetActive(false);
        }

        private void SetupDialogue(CutInDialogueStep step)
        {
            if (!step.showDialogue)
            {
                if (dialoguePanel != null) dialoguePanel.SetActive(false);
                return;
            }

            if (dialoguePanel != null) dialoguePanel.SetActive(true);

            if (dialogueText != null) dialogueText.text = step.dialogueText;

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
                Debug.Log($"[Dissolve CutIn] Play SFX: {step.sfxID}");
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

        private IEnumerator DissolveEffectRoutine(CutInSlotUI slot, CutInDialogueStep step)
        {
            if (slot == null || slot.cutInImage == null) yield break;

            if (slot.dissolveImage == null)
            {
                GameObject copyObj = Instantiate(slot.cutInImage.gameObject, slot.cutInImage.transform.parent);
                copyObj.name = slot.cutInImage.name + "_DissolveTemp";
                slot.dissolveImage = copyObj.GetComponent<Image>();
            }

            slot.dissolveImage.transform.SetAsLastSibling();

            slot.dissolveImage.sprite = slot.cutInImage.sprite;
            slot.dissolveImage.rectTransform.anchoredPosition = slot.cutInImage.rectTransform.anchoredPosition;
            slot.dissolveImage.rectTransform.sizeDelta = slot.cutInImage.rectTransform.sizeDelta;
            slot.dissolveImage.rectTransform.localScale = slot.cutInImage.rectTransform.localScale;
            slot.dissolveImage.gameObject.SetActive(true);

            CanvasGroup dissolveCG = slot.dissolveImage.GetComponent<CanvasGroup>();
            if (dissolveCG != null) dissolveCG.alpha = 1f;

            Color dissolveColor = slot.dissolveImage.color;
            dissolveColor.a = 1f;
            slot.dissolveImage.color = dissolveColor;

            slot.cutInImage.sprite = step.cutInSprite;
            slot.cutInImage.rectTransform.anchoredPosition = step.cutInOffset;
            slot.cutInImage.gameObject.SetActive(true);

            CanvasGroup mainCG = slot.cutInImage.GetComponent<CanvasGroup>();
            if (mainCG != null) mainCG.alpha = 1f;

            Color mainColor = slot.cutInImage.color;
            mainColor.a = 0f;
            slot.cutInImage.color = mainColor;

            if (slot.cutInCanvasGroup != null) slot.cutInCanvasGroup.alpha = 1f;

            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, step.dissolveDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                mainColor.a = t;
                slot.cutInImage.color = mainColor;

                dissolveColor.a = 1f - t;
                slot.dissolveImage.color = dissolveColor;
                if (dissolveCG != null) dissolveCG.alpha = 1f - t;

                yield return null;
            }

            mainColor.a = 1f;
            slot.cutInImage.color = mainColor;
            slot.dissolveImage.gameObject.SetActive(false);
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

*/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    [RequireComponent(typeof(AudioSource))]
    public class DissolveCutInDialogueManager : MonoBehaviour
    {
        public enum SlideDirection { Top, Bottom, Left, Right }
        public enum DialogueProgressionMode { Timer, WaitInput, TimerOrInput }

        // ★ 7개 슬롯으로 확장된 CutInSlot Enum
        public enum CutInSlot
        {
            Left,
            Right,
            Center,
            Up,
            Down,
            Ji,
            Min
        }

        public enum CutInExitMode
        {
            KeepOn,     // 계속 유지
            FadeOut,    // 지정 시간 동안 서서히 사라짐
            InstantOff  // 즉시 끔
        }

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
            [Tooltip("디졸브(Crossfade) 연출을 위한 보조 이미지 (비워두면 자동 복제 생성)")]
            public Image dissolveImage;
            public CanvasGroup cutInCanvasGroup;
            public RectTransform cutInRect;
        }

        [System.Serializable]
        public class CutInDialogueStep
        {
            [Header("--- [1. Step Delay Settings] ---")]
            [Tooltip("이 스텝(대사/연출)이 시작되기 전 전체 대기 시간 (초)")]
            public float startDelay = 0f;

            [Header("--- [2. Cut-In Image Settings] ---")]
            public Sprite cutInSprite;
            [Tooltip("이 캐릭터를 어느 위치(슬롯)에 띄울 것인가?")]
            public CutInSlot targetSlot = CutInSlot.Center;
            public Vector2 cutInOffset;

            [Space(5)]
            [Tooltip("대사가 시작된 후 컷인이 등장할 때까지의 개별 딜레이 시간 (초)\n예: 1.0 지정 시 대사 출력 후 1초 뒤에 컷인 등장")]
            public float cutInStartDelay = 0f;

            public bool useFadeIn;
            public float fadeDuration = 0.5f;

            [Tooltip("이전 이미지에서 새 이미지로 스르륵 녹아들며 바뀌는 디졸브 연출 사용")]
            public bool useDissolve;
            public float dissolveDuration = 0.5f;

            public bool useSlide;
            public SlideDirection slideDirection;
            public float slideDuration = 0.5f;
            public AnimationCurve slideCurve = AnimationCurve.Linear(0, 0, 1, 1);

            public bool useShake;
            public float shakeDuration = 0.3f;
            public float shakeIntensity = 10f;

            [Header("--- [3. Cut-In Independent Timer Settings] ---")]
            [Tooltip("체크 시 대사 진행/클릭 여부와 상관없이 컷인이 지정한 유지 시간 후 독립적으로 꺼집니다.")]
            public bool useCutInTimer = false;

            [Tooltip("useCutInTimer 체크 시: 컷인 이미지가 화면에 유지될 시간 (초)")]
            public float cutInDisplayDuration = 2.0f;

            [Tooltip("컷인이 꺼질 때의 퇴장 방식\n• KeepOn: 계속 유지\n• FadeOut: 서서히 사라짐\n• InstantOff: 즉시 끔")]
            public CutInExitMode cutInExitMode = CutInExitMode.KeepOn;

            [Tooltip("cutInExitMode가 FadeOut일 때 페이드아웃 소요 시간 (초)")]
            public float exitFadeDuration = 0.3f;

            [Tooltip("이 대사가 시작될 때, 다른 위치(슬롯)에 떠 있는 모든 캐릭터를 지울 것인가?")]
            public bool clearOtherSlots = false;

            [Header("--- [4. Dialogue Text Settings] ---")]
            public string speakerName;
            [TextArea(3, 5)]
            public string dialogueText;

            [Tooltip("체크 시 대화창을 화면에 보입니다.")]
            public bool showDialogue = true;
            [Tooltip("체크 시 이 대사 스텝이 끝나는 순간 대화창만 끕니다.")]
            public bool hideDialogueOnStepEnd = false;

            [Header("--- [5. Audio / Dubbing Settings] ---")]
            public AudioClip dubbingClip;
            public string sfxID;

            [Header("--- [6. Progression Settings] ---")]
            public DialogueProgressionMode progressionMode;
            public float duration = 2f;
        }

        [Header("Root UI Panel")]
        [SerializeField] private GameObject rootUI;

        [Header("Global Auto-Close Setting")]
        [Tooltip("체크 시 전체 대화 시퀀스가 종료되면 화면 전체(대화창, 컷인)를 자동으로 끕니다.")]
        [SerializeField] private bool autoCloseAtSequenceEnd = true;

        [Header("Cut-In Multi-Slot UI List (Left, Right, Center, Up, Down, Ji, Min 등록)")]
        [SerializeField] private List<CutInSlotUI> slots = new List<CutInSlotUI>();

        [Header("Dialogue UI References")]
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private RectTransform dialogueRect;
        [SerializeField] private Image dialogueBgImage;
        [SerializeField] private TextMeshProUGUI dialogueText;

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
                if (slot.dissolveImage != null) slot.dissolveImage.gameObject.SetActive(false);
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

            if (autoCloseAtSequenceEnd)
            {
                foreach (var slot in slots)
                {
                    StartCoroutine(FadeOutSlotRoutine(slot, 0.3f));
                }

                if (dialoguePanel != null) dialoguePanel.SetActive(false);

                yield return new WaitForSeconds(0.35f);

                if (rootUI != null) rootUI.SetActive(false);
            }

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
                if (slotCoroutines.TryGetValue(step.targetSlot, out Coroutine activeCoroutine))
                {
                    if (activeCoroutine != null) StopCoroutine(activeCoroutine);
                }

                slotCoroutines[step.targetSlot] = StartCoroutine(CutInLifecycleRoutine(slotUI, step));
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

            if (step.hideDialogueOnStepEnd && dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            if (!step.useCutInTimer && slotUI != null)
            {
                HandleCutInExit(slotUI, step);
            }
        }

        private IEnumerator CutInLifecycleRoutine(CutInSlotUI slotUI, CutInDialogueStep step)
        {
            if (step.cutInStartDelay > 0f)
            {
                yield return new WaitForSeconds(step.cutInStartDelay);
            }

            if (step.useDissolve && slotUI.cutInImage != null && slotUI.cutInImage.sprite != null && slotUI.cutInImage.gameObject.activeSelf)
            {
                yield return StartCoroutine(DissolveEffectRoutine(slotUI, step));
            }
            else
            {
                SetupCutIn(slotUI, step);
                yield return StartCoroutine(CutInEffectRoutine(slotUI, step, step.cutInOffset));
            }

            if (step.useCutInTimer)
            {
                yield return new WaitForSeconds(step.cutInDisplayDuration);
                HandleCutInExit(slotUI, step);
            }
        }

        private void HandleCutInExit(CutInSlotUI slotUI, CutInDialogueStep step)
        {
            switch (step.cutInExitMode)
            {
                case CutInExitMode.FadeOut:
                    StartCoroutine(FadeOutSlotRoutine(slotUI, step.exitFadeDuration));
                    break;

                case CutInExitMode.InstantOff:
                    TurnOffSlotInstantly(slotUI);
                    break;

                case CutInExitMode.KeepOn:
                default:
                    break;
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

        private void TurnOffSlotInstantly(CutInSlotUI slot)
        {
            if (slot == null) return;
            if (slot.cutInCanvasGroup != null) slot.cutInCanvasGroup.alpha = 0f;
            if (slot.cutInImage != null) slot.cutInImage.gameObject.SetActive(false);
            if (slot.dissolveImage != null) slot.dissolveImage.gameObject.SetActive(false);
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
            if (slot.dissolveImage != null) slot.dissolveImage.gameObject.SetActive(false);
        }

        private void SetupDialogue(CutInDialogueStep step)
        {
            if (!step.showDialogue)
            {
                if (dialoguePanel != null) dialoguePanel.SetActive(false);
                return;
            }

            if (dialoguePanel != null) dialoguePanel.SetActive(true);

            if (dialogueText != null) dialogueText.text = step.dialogueText;

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
                Debug.Log($"[Dissolve CutIn] Play SFX: {step.sfxID}");
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

        private IEnumerator DissolveEffectRoutine(CutInSlotUI slot, CutInDialogueStep step)
        {
            if (slot == null || slot.cutInImage == null) yield break;

            if (slot.dissolveImage == null)
            {
                GameObject copyObj = Instantiate(slot.cutInImage.gameObject, slot.cutInImage.transform.parent);
                copyObj.name = slot.cutInImage.name + "_DissolveTemp";
                slot.dissolveImage = copyObj.GetComponent<Image>();
            }

            slot.dissolveImage.transform.SetAsLastSibling();

            slot.dissolveImage.sprite = slot.cutInImage.sprite;
            slot.dissolveImage.rectTransform.anchoredPosition = slot.cutInImage.rectTransform.anchoredPosition;
            slot.dissolveImage.rectTransform.sizeDelta = slot.cutInImage.rectTransform.sizeDelta;
            slot.dissolveImage.rectTransform.localScale = slot.cutInImage.rectTransform.localScale;
            slot.dissolveImage.gameObject.SetActive(true);

            CanvasGroup dissolveCG = slot.dissolveImage.GetComponent<CanvasGroup>();
            if (dissolveCG != null) dissolveCG.alpha = 1f;

            Color dissolveColor = slot.dissolveImage.color;
            dissolveColor.a = 1f;
            slot.dissolveImage.color = dissolveColor;

            slot.cutInImage.sprite = step.cutInSprite;
            slot.cutInImage.rectTransform.anchoredPosition = step.cutInOffset;
            slot.cutInImage.gameObject.SetActive(true);

            CanvasGroup mainCG = slot.cutInImage.GetComponent<CanvasGroup>();
            if (mainCG != null) mainCG.alpha = 1f;

            Color mainColor = slot.cutInImage.color;
            mainColor.a = 0f;
            slot.cutInImage.color = mainColor;

            if (slot.cutInCanvasGroup != null) slot.cutInCanvasGroup.alpha = 1f;

            float elapsed = 0f;
            float duration = Mathf.Max(0.05f, step.dissolveDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                mainColor.a = t;
                slot.cutInImage.color = mainColor;

                dissolveColor.a = 1f - t;
                slot.dissolveImage.color = dissolveColor;
                if (dissolveCG != null) dissolveCG.alpha = 1f - t;

                yield return null;
            }

            mainColor.a = 1f;
            slot.cutInImage.color = mainColor;
            slot.dissolveImage.gameObject.SetActive(false);
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