using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    public class UpgradedShowTextOutput : ProcessBase
    {
        [Header("Text Settings")]
        [TextArea(3, 5)]
        [SerializeField] private string message = "출력할 대사를 입력하세요.";

        [SerializeField] private float duration = 3.0f;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private int fontSize = 48;
        [SerializeField] private bool waitForFinish = false;

        [Header("Font Settings")]
        [Tooltip("사용할 폰트를 프로젝트 창에서 끌어다 넣으세요. 비워두면 시스템 기본 폰트를 시도합니다.")]
        [SerializeField] private Font customFont;

        private const string CanvasName = "GOS_GlobalTextCanvas";

        // [추정 해결책] 글자를 참조하여 끌 수 있도록 클래스 멤버 변수로 격상
        private Text _activeUiText;

        public override void Execute()
        {
            StartCoroutine(ShowTextRoutine());
        }

        private IEnumerator ShowTextRoutine()
        {
            _activeUiText = SetupTextUI();

            _activeUiText.text = message;
            _activeUiText.color = textColor;
            _activeUiText.fontSize = fontSize;
            _activeUiText.gameObject.SetActive(true);

            if (!waitForFinish) IsOn = true;

            yield return new WaitForSeconds(duration);

            // 함수화된 ClearText()를 사용해 안전하게 제거
            ClearText();

            if (waitForFinish) IsOn = true;
        }

        /// <summary>
        /// [추가] 텍스트 내용을 지우고 UI를 비활성화하는 안전 함수
        /// </summary>
        private void ClearText()
        {
            if (_activeUiText != null && _activeUiText.text == message)
            {
                _activeUiText.text = "";
                _activeUiText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// [핵심 추가] 이 노드 오브젝트가 파괴될 때(씬 전환 포함) 자동으로 호출됩니다.
        /// </summary>
        private void OnDestroy()
        {
            // 루틴이 중간에 끊기더라도 강제로 글자를 지워 흐름을 보장합니다.
            ClearText();
        }

        /// <summary>
        /// [선택적 추가] 만약 노드가 파괴되지 않고 단순히 비활성화(SetActive(false)) 될 때도 지우고 싶다면 켜둡니다.
        /// </summary>
        private void OnDisable()
        {
            ClearText();
        }

        private Text SetupTextUI()
        {
            GameObject canvasObj = GameObject.Find(CanvasName);

            if (canvasObj == null)
            {
                canvasObj = new GameObject(CanvasName);
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                DontDestroyOnLoad(canvasObj);
            }

            Transform textTransform = canvasObj.transform.Find("SubtitleText");
            Text uiText = null;

            if (textTransform != null)
            {
                uiText = textTransform.GetComponent<Text>();
            }
            else
            {
                GameObject textObj = new GameObject("SubtitleText");
                textObj.transform.SetParent(canvasObj.transform, false);

                uiText = textObj.AddComponent<Text>();

                if (customFont != null)
                {
                    uiText.font = customFont;
                }
                else
                {
                    uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }

                uiText.alignment = TextAnchor.LowerCenter;
                uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
                uiText.verticalOverflow = VerticalWrapMode.Overflow;

                Outline outline = textObj.AddComponent<Outline>();
                outline.effectColor = new Color(0, 0, 0, 0.8f);
                outline.effectDistance = new Vector2(2, -2);

                RectTransform rect = uiText.rectTransform;
                rect.anchorMin = new Vector2(0.1f, 0.1f);
                rect.anchorMax = new Vector2(0.9f, 0.3f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            if (customFont != null) uiText.font = customFont;

            return uiText;
        }
    }
}