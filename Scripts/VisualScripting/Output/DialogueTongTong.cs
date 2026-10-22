using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace _Project.Scripts.VisualScripting
{
    public class DialogueTongTong : ProcessBase
    {
        // 화자별 전용 이미지 데이터 구조
        [System.Serializable]
        public struct SpeakerVisual
        {
            public string speakerName;        // 화자 이름 (예: 앤벌, 베리)
            public Sprite dialogueSprite;     // 그 화자의 대화창 배경 이미지
        }

        /// <summary>
        /// 한 줄의 대사가 다음 줄로 넘어가는 방식을 결정합니다.
        /// </summary>
        public enum DialogueProgressionMode
        {
            Timer,        // 기존 방식: duration(초) 동안 자동 대기 후 넘어감
            WaitInput,    // 정지형: 마우스 클릭 또는 아무 키 입력 시 넘어감
            TimerOrInput  // 복합형: duration(초) 타이머와 입력 중 먼저 발생한 것으로 넘어감
        }

        // 하나의 대사 데이터 구조
        [System.Serializable]
        public struct DialogueLine
        {
            public string speakerName;        // 현재 줄을 말할 화자 이름
            [TextArea(3, 5)]
            public string dialogueText;       // 대사 내용

            [Tooltip("Timer / TimerOrInput 모드일 때 대기 시간 (초)\nWaitInput 모드에서는 무시됩니다.")]
            public float duration;            // 대사가 머무를 시간 (초)

            [Tooltip("Timer: duration초 자동 대기\nWaitInput: 클릭/키 입력 대기\nTimerOrInput: 둘 중 먼저 발생한 것으로 넘어감")]
            public DialogueProgressionMode progressionMode; // 진행 방식

            [Tooltip("이 대사 줄에서만 특수하게 재생할 SFX 키 (비워둘 경우 기본 defaultSoundKey 재생)")]
            public string soundKeyOverride;
        }

        [Header("Dialogue UI Reference")]
        [SerializeField] private GameObject panelObject;

        [Header("Speaker Database")]
        [Tooltip("화자별 이미지 세트를 등록하세요 (예: 앤벌 = 앤벌이 킹받 이미지)")]
        [SerializeField] private List<SpeakerVisual> speakerDatabase;

        [Header("Global Font Settings")]
        [SerializeField] private TMP_FontAsset fontAsset;     // 사용할 폰트
        [SerializeField] private float fontSize = 81.4f;      // 폰트 크기

        [Header("Dialogue Lines (대사 목록)")]
        [SerializeField] private List<DialogueLine> dialogueLines;

        [Header("Audio Settings (사운드 연동)")]
        [Tooltip("체크 시 대사창이 통! 튀어오를 때 효과음을 재생합니다.")]
        [SerializeField] private bool playDialogueSound = true;

        [Tooltip("대사창이 통! 튀어오를 때 재생할 기본 SFX 키 (SO_SFXTable 등록 키, 비워두면 무음)")]
        [SerializeField] private string defaultSoundKey = "Dialogue_Tong";

        [Header("Layout Settings")]
        [SerializeField] private Vector2 boxPosition = new Vector2(0f, 255f);
        [SerializeField] private Vector2 boxSize = new Vector2(2048f, 1045f);
        [SerializeField] private Vector3 boxScale = new Vector3(0.5f, 0.5f, 1f);
        [SerializeField] private Vector2 textPosition = new Vector2(240f, -143f);
        [SerializeField] private Vector2 textSize = new Vector2(1300f, 330f);
        [SerializeField] private float bounceAmount = 0.1f;

        private RectTransform _dialogueRect;
        private Image _dialogueImage;
        private TextMeshProUGUI _tmpText;
        private Vector3 _originalBoxScale;

        private void Awake()
        {
            CacheComponents();
            if (panelObject != null) panelObject.SetActive(false);
        }

        private void CacheComponents()
        {
            if (panelObject == null) return;

            Transform dialogueTransform = panelObject.transform.Find("Dialogue");
            if (dialogueTransform != null)
            {
                _dialogueRect = dialogueTransform.GetComponent<RectTransform>();
                _dialogueImage = dialogueTransform.GetComponent<Image>();

                Transform textTransform = dialogueTransform.Find("Text (TMP)");
                if (textTransform != null)
                {
                    _tmpText = textTransform.GetComponent<TextMeshProUGUI>();
                }
            }
        }

        public override void Execute()
        {
            IsOn = false;

            if (panelObject == null) CacheComponents();

            if (_dialogueRect == null || _tmpText == null)
            {
                Debug.LogError($"[{gameObject.name}] UI 구조를 확인해주세요. Panel -> Dialogue -> Text (TMP)");
                IsOn = true;
                return;
            }

            if (dialogueLines == null || dialogueLines.Count == 0)
            {
                Debug.LogWarning($"[{gameObject.name}] Dialogue Lines에 적힌 대사가 없습니다!");
                IsOn = true;
                return;
            }

            panelObject.SetActive(true);
            StartCoroutine(DialoguePlayRoutine());
        }

        private IEnumerator DialoguePlayRoutine()
        {
            foreach (var line in dialogueLines)
            {
                // 1. 화자 이미지 자동 변경
                Sprite targetSprite = null;
                foreach (var db in speakerDatabase)
                {
                    if (db.speakerName == line.speakerName)
                    {
                        targetSprite = db.dialogueSprite;
                        break;
                    }
                }
                if (targetSprite != null && _dialogueImage != null) _dialogueImage.sprite = targetSprite;

                // 2. 레이아웃 리셋
                _dialogueRect.anchoredPosition = boxPosition;
                _dialogueRect.sizeDelta = boxSize;
                _dialogueRect.localScale = boxScale;
                _originalBoxScale = boxScale;

                // 3. 텍스트 및 폰트 강제 적용
                _tmpText.text = line.dialogueText;
                if (fontAsset != null) _tmpText.font = fontAsset;
                _tmpText.fontSize = fontSize;

                RectTransform textRect = _tmpText.rectTransform;
                textRect.anchoredPosition = textPosition;
                textRect.sizeDelta = textSize;

                // 🔊 4. 효과음 재생 (playDialogueSound 체크 여부 확인 후 재생)
                string keyToPlay = !string.IsNullOrEmpty(line.soundKeyOverride) ? line.soundKeyOverride : defaultSoundKey;
                PlaySFX(keyToPlay);

                // 5. 귀엽게 통! 튀는 연출
                float bounceTimer = 0f;
                float bounceDuration = 0.15f;
                while (bounceTimer < bounceDuration)
                {
                    bounceTimer += Time.deltaTime;
                    float t = bounceTimer / bounceDuration;
                    float scaleOffset = Mathf.Sin(t * Mathf.PI) * bounceAmount;
                    _dialogueRect.localScale = _originalBoxScale + new Vector3(scaleOffset, scaleOffset, 0f);
                    yield return null;
                }
                _dialogueRect.localScale = _originalBoxScale;

                // 6. 진행 방식에 따라 대기
                if (line.progressionMode == DialogueProgressionMode.WaitInput)
                {
                    yield return null;
                    yield return new WaitForSeconds(0.1f);
                    yield return new WaitUntil(() => Input.GetMouseButtonDown(0) || Input.anyKeyDown);
                    yield return null;
                }
                else if (line.progressionMode == DialogueProgressionMode.TimerOrInput)
                {
                    yield return null;
                    yield return new WaitForSeconds(0.1f);

                    float elapsed = 0f;
                    while (elapsed < line.duration)
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
                    yield return new WaitForSeconds(line.duration);
                }
            }

            panelObject.SetActive(false);
            IsOn = true;
        }

        private void PlaySFX(string soundKey)
        {
            if (!playDialogueSound) return;

            if (!string.IsNullOrEmpty(soundKey) && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(soundKey);
            }
        }

        public override void Reset()
        {
            base.Reset();
            if (panelObject != null) panelObject.SetActive(false);
            if (_dialogueRect != null) _dialogueRect.localScale = _originalBoxScale;
        }
    }
}