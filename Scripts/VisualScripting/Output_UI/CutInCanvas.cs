using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace _Project.Scripts.VisualScripting
{
    [Serializable]
    public class JSONDialogueStep
    {
        public int Step_Index;
        public string Left_String_ID;
        public string Left_Text_Korean;
        public string Right_String_ID;
        public string Right_Text_Korean;
    }

    [Serializable]
    public class JSONCutInData
    {
        public string CutIn_ID;
        public string Comment;
        public List<JSONDialogueStep> Dialogue_Steps;
    }

    [Serializable]
    public class JSONCutInTable
    {
        public List<JSONCutInData> CutIn_Table_Data;
    }
    public class CutInCanvas : MonoBehaviour
    {
        [Header("Common Mask Component")]
        [SerializeField] private GameObject darkMaskBackground;

        [Header("TYPE-A (Comics Slash Layout)")]
        [SerializeField] private GameObject panelTypeA;
        [SerializeField] private Image imgLeftOutlineContainer;
        [SerializeField] private Image imgLeftVisual;
        [SerializeField] private Image imgLeftBorderLine;
        [SerializeField] private GameObject bubbleLeftWindow;
        [SerializeField] private Image imgLeftBubbleBg;
        [SerializeField] private TextMeshProUGUI txtLeftName;
        [SerializeField] private TextMeshProUGUI txtLeftDialogue;

        [SerializeField] private Image imgRightOutlineContainer;
        [SerializeField] private Image imgRightVisual;
        [SerializeField] private Image imgRightBorderLine;
        [SerializeField] private GameObject bubbleRightWindow;
        [SerializeField] private Image imgRightBubbleBg;
        [SerializeField] private TextMeshProUGUI txtRightName;
        [SerializeField] private TextMeshProUGUI txtRightDialogue;

        [Header("TYPE-B (Central Banner Layout)")]
        [SerializeField] private GameObject panelTypeB;
        [SerializeField] private Image imgBannerOutlineContainer; // TYPE-B 자르기용 순수 구멍 마스크 이미지 (Mask 컴포넌트 포함)
        [SerializeField] private Image imgBannerVisual;           // TYPE-B 내부 캐릭터 일러스트
        [SerializeField] private Image imgBannerBorderLine;       // ★ [신규 추가] TYPE-B 최상단 마감용 검은 테두리 배너 이미지
        [SerializeField] private TextMeshProUGUI txtBannerDialogue;

        [Header("Feedback Component")]
        [SerializeField] private GameObject goNextArrow;

        private CutInOutput _ownerNode;
        private CutInLayoutType _currentLayout;
        private bool _isPauseMode;
        private List<CutInDialogueStep> _currentSteps;
        private int _currentStepIndex = 0;
        private float _realtimeDuration;

        private bool _isTyping = false;
        private bool _skipTypingRequest = false;
        private Coroutine _activeAnimationRoutine;

        private void Awake()
        {
            HideAllElements();
        }

        public void PlaySystemCutIn(CutInOutput owner, CutInLayoutType type, bool isPause,
                                    CharacterVisualSet left, CharacterVisualSet right, List<CutInDialogueStep> steps, float durationTime)
        {
            _ownerNode = owner;
            _currentLayout = type;
            _isPauseMode = isPause;
            _currentSteps = steps;
            _currentStepIndex = 0;
            _realtimeDuration = durationTime;

            darkMaskBackground.SetActive(true);
            HideAllElements();

            ApplyVisualData(type, left, right);

            if (type == CutInLayoutType.TYPE_A_ComicsSlash) panelTypeA.SetActive(true);
            else panelTypeB.SetActive(true);

            _activeAnimationRoutine = StartCoroutine(CutInLifecycleRoutine());
        }

        private void ApplyVisualData(CutInLayoutType type, CharacterVisualSet left, CharacterVisualSet right)
        {
            if (type == CutInLayoutType.TYPE_A_ComicsSlash)
            {
                // TYPE-A 처리부
                if (left.innerMaskOnlySprite != null) imgLeftOutlineContainer.sprite = left.innerMaskOnlySprite;
                if (left.outerBorderLineSprite != null) imgLeftBorderLine.sprite = left.outerBorderLineSprite;
                if (left.speechBubbleSprite != null) imgLeftBubbleBg.sprite = left.speechBubbleSprite;
                txtLeftName.text = !string.IsNullOrEmpty(left.characterName) ? left.characterName : "Character L";

                if (left.illustration != null)
                {
                    imgLeftVisual.gameObject.SetActive(true);
                    imgLeftVisual.sprite = left.illustration;
                    imgLeftVisual.rectTransform.anchoredPosition = new Vector2(left.visualPivotOffset.x * 200f, left.visualPivotOffset.y * 200f);
                    imgLeftVisual.transform.localScale = Vector3.one * (left.scaleMultiplier > 0 ? left.scaleMultiplier : 1f);
                }
                else imgLeftVisual.gameObject.SetActive(false);

                if (right.innerMaskOnlySprite != null) imgRightOutlineContainer.sprite = right.innerMaskOnlySprite;
                if (right.outerBorderLineSprite != null) imgRightBorderLine.sprite = right.outerBorderLineSprite;
                if (right.speechBubbleSprite != null) imgRightBubbleBg.sprite = right.speechBubbleSprite;
                txtRightName.text = !string.IsNullOrEmpty(right.characterName) ? right.characterName : "Character R";

                if (right.illustration != null)
                {
                    imgRightVisual.gameObject.SetActive(true);
                    imgRightVisual.sprite = right.illustration;
                    imgRightVisual.rectTransform.anchoredPosition = new Vector2(right.visualPivotOffset.x * 200f, right.visualPivotOffset.y * 200f);
                    imgRightVisual.transform.localScale = Vector3.one * (right.scaleMultiplier > 0 ? right.scaleMultiplier : 1f);
                }
                else imgRightVisual.gameObject.SetActive(false);
            }
            else
            {
                // ★ [핵심 구현] TYPE-B 배너 독립형 마스크 및 테두리 스프라이트 동적 할당
                if (left.innerMaskOnlySprite != null) imgBannerOutlineContainer.sprite = left.innerMaskOnlySprite;
                if (left.outerBorderLineSprite != null) imgBannerBorderLine.sprite = left.outerBorderLineSprite; // 검은 배너 선 테두리 장착

                Sprite bannerSprite = left.illustration != null ? left.illustration : right.illustration;
                if (bannerSprite != null)
                {
                    imgBannerVisual.gameObject.SetActive(true);
                    imgBannerVisual.sprite = bannerSprite;
                    imgBannerVisual.rectTransform.anchoredPosition = new Vector2(left.visualPivotOffset.x * 200f, left.visualPivotOffset.y * 200f);
                    imgBannerVisual.transform.localScale = Vector3.one * (left.scaleMultiplier > 0 ? left.scaleMultiplier : 1f);
                }
                else
                {
                    imgBannerVisual.gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator CutInLifecycleRoutine()
        {
            while (_currentStepIndex < _currentSteps.Count)
            {
                CutInDialogueStep currentStep = _currentSteps[_currentStepIndex];

                string rawLeftText = LocalizationManager.GetText(currentStep.leftStringID);
                string rawRightText = LocalizationManager.GetText(currentStep.rightStringID);

                goNextArrow.SetActive(false);

                yield return StartCoroutine(RenderDialogueStepRoutine(currentStep.outputOrder, rawLeftText, rawRightText));

                goNextArrow.SetActive(true);

                if (_isPauseMode)
                {
                    bool userClicked = false;
                    while (!userClicked)
                    {
                        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                        {
                            userClicked = true;
                        }
                        yield return null;
                    }
                }
                else
                {
                    yield return new WaitForSecondsRealtime(_realtimeDuration / _currentSteps.Count);
                }

                _currentStepIndex++;
            }

            // 퇴장 연출
            float fallElapsed = 0f;
            Vector3 originalPos = transform.localPosition;
            while (fallElapsed < 0.2f)
            {
                fallElapsed += Time.unscaledDeltaTime;
                transform.localPosition = originalPos + Vector3.down * (fallElapsed * 1500f);
                yield return null;
            }
            transform.localPosition = originalPos;

            HideAllElements();
            darkMaskBackground.SetActive(false);
            _ownerNode.NotifySequenceFinished();
        }

        private IEnumerator RenderDialogueStepRoutine(TextOutputOrder order, string leftTxt, string rightTxt)
        {
            _isTyping = true;
            _skipTypingRequest = false;

            bubbleLeftWindow.SetActive(order == TextOutputOrder.LeftFirst || order == TextOutputOrder.Simultaneous);
            bubbleRightWindow.SetActive(order == TextOutputOrder.RightFirst || order == TextOutputOrder.Simultaneous);

            if (_currentLayout == CutInLayoutType.TYPE_A_ComicsSlash)
            {
                txtLeftDialogue.text = "";
                txtRightDialogue.text = "";

                if (order == TextOutputOrder.LeftFirst)
                {
                    yield return StartCoroutine(TypeRoutine(txtLeftDialogue, leftTxt));
                    bubbleRightWindow.SetActive(true);
                    yield return StartCoroutine(TypeRoutine(txtRightDialogue, rightTxt));
                }
                else if (order == TextOutputOrder.RightFirst)
                {
                    yield return StartCoroutine(TypeRoutine(txtRightDialogue, rightTxt));
                    bubbleLeftWindow.SetActive(true);
                    yield return StartCoroutine(TypeRoutine(txtLeftDialogue, leftTxt));
                }
                else
                {
                    yield return StartCoroutine(TypeSimultaneousRoutine(txtLeftDialogue, leftTxt, txtRightDialogue, rightTxt));
                }
            }
            else
            {
                // TYPE-B 중앙 대사 타이핑
                txtBannerDialogue.text = "";
                yield return StartCoroutine(TypeRoutine(txtBannerDialogue, leftTxt + " " + rightTxt));
            }

            _isTyping = false;
        }

        private IEnumerator TypeRoutine(TextMeshProUGUI tmpComponent, string targetText)
        {
            tmpComponent.text = "";
            foreach (char letter in targetText.ToCharArray())
            {
                if (_skipTypingRequest)
                {
                    tmpComponent.text = targetText;
                    break;
                }
                tmpComponent.text += letter;
                yield return new WaitForSecondsRealtime(0.03f);
            }
        }

        private IEnumerator TypeSimultaneousRoutine(TextMeshProUGUI componentA, string textA, TextMeshProUGUI componentB, string textB)
        {
            int maxLen = Mathf.Max(textA.Length, textB.Length);
            for (int i = 0; i <= maxLen; i++)
            {
                if (_skipTypingRequest)
                {
                    componentA.text = textA;
                    componentB.text = textB;
                    break;
                }
                if (i <= textA.Length) componentA.text = textA.Substring(0, i);
                if (i <= textB.Length) componentB.text = textB.Substring(0, i);
                yield return new WaitForSecondsRealtime(0.03f);
            }
        }

        private void Update()
        {
            if (_isTyping && !_skipTypingRequest)
            {
                if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                {
                    _skipTypingRequest = true;
                }
            }
        }

        private void HideAllElements()
        {
            panelTypeA.SetActive(false);
            panelTypeB.SetActive(false);
            goNextArrow.SetActive(false);
        }
    }

    // 텍스트 소팅 깡통 더미 로컬라이징 스태틱 매니저
    /// <summary>
    /// CutInDialogueSheet.json 데이터를 로드하여 실시간으로 
    /// ID에 맞는 텍스트 대사를 반환하는 실무형 로컬라이징 매니저
    /// </summary>
    public static class LocalizationManager
    {
        // ID를 입력하면 실제 대사가 튀어나오도록 딕셔너리 구성
        private static Dictionary<string, string> _textDictionary = new Dictionary<string, string>();
        private static bool _isInitialized = false;

        /// <summary>
        /// JSON 데이터를 메모리에 로드합니다.
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;

            _textDictionary.Clear();

            // [중요] CutInDialogueSheet.json 파일은 반드시 'Assets/Resources/' 폴더 안에 위치해야 합니다.
            // Resources.Load는 확장자(.json)를 붙이지 않고 이름만 적습니다.
            TextAsset jsonFile = Resources.Load<TextAsset>("CutInDialogueSheet");

            if (jsonFile == null)
            {
                Debug.LogError("[LocalizationManager] 'Assets/Resources/CutInDialogueSheet.json' 파일을 찾을 수 없습니다! 컷인 대사가 비어있게 됩니다.");
                _isInitialized = true;
                return;
            }

            try
            {
                // 유니티 내장 JSON 유틸리티로 파싱 수행
                JSONCutInTable tableData = JsonUtility.FromJson<JSONCutInTable>(jsonFile.text);

                if (tableData != null && tableData.CutIn_Table_Data != null)
                {
                    foreach (var cutIn in tableData.CutIn_Table_Data)
                    {
                        foreach (var step in cutIn.Dialogue_Steps)
                        {
                            // 1. 좌측 캐릭터 대사 ID 등록
                            if (!string.IsNullOrEmpty(step.Left_String_ID) && !_textDictionary.ContainsKey(step.Left_String_ID))
                            {
                                _textDictionary.Add(step.Left_String_ID, step.Left_Text_Korean);
                            }

                            // 2. 우측 캐릭터 대사 ID 등록 (TYPE-B 배너의 경우 문구 ID가 됨)
                            if (!string.IsNullOrEmpty(step.Right_String_ID) && !_textDictionary.ContainsKey(step.Right_String_ID))
                            {
                                _textDictionary.Add(step.Right_String_ID, step.Right_Text_Korean);
                            }
                        }
                    }
#if UNITY_EDITOR
                    Debug.Log($"<color=green>[LocalizationManager]</color> JSON 파싱 성공! 총 {_textDictionary.Count}개의 대사 ID를 정상 로드했습니다.");
#endif
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalizationManager] JSON 파싱 중 치명적인 오류 발생: {ex.Message}");
            }

            _isInitialized = true;
        }

        /// <summary>
        /// 비주얼 스크립팅 노드 및 UI 매니저가 특정 ID를 넘기면 파싱된 JSON 실대사를 매칭해주는 핵심 함수
        /// </summary>
        public static string GetText(string key)
        {
            // 초기화가 안 되어 있다면 강제 초기화 진행
            if (!_isInitialized) Initialize();

            if (string.IsNullOrEmpty(key)) return "";

            // 딕셔너리에 기획자가 적은 ID가 존재한다면 한글 대사 반환
            if (_textDictionary.TryGetValue(key, out string translatedText))
            {
                return translatedText;
            }

            // 만약 기획서 오기입으로 테이블에 없는 ID를 넣었다면 오류 방지용으로 ID 자체를 반환
            return key;
        }
    }
}