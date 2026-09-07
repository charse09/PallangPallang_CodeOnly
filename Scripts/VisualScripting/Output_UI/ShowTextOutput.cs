using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    public class ShowTextOutput : ProcessBase
    {
        [Header("Text Settings")]
        [TextArea(3, 5)]
        [SerializeField] private string message = "출력할 대사를 입력하세요.";

        [SerializeField] private float duration = 3.0f;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private int fontSize = 48;
        [SerializeField] private bool waitForFinish = false;

        // ★ 추가된 부분: 인스펙터에서 폰트를 직접 할당받을 수 있게 합니다.
        [Header("Font Settings")]
        [Tooltip("사용할 폰트를 프로젝트 창에서 끌어다 넣으세요. 비워두면 시스템 기본 폰트를 시도합니다.")]
        [SerializeField] private Font customFont;

        private const string CanvasName = "GOS_GlobalTextCanvas";

        public override void Execute()
        {
            StartCoroutine(ShowTextRoutine());
        }

        private IEnumerator ShowTextRoutine()
        {
            Text uiText = SetupTextUI();

            uiText.text = message;
            uiText.color = textColor;
            uiText.fontSize = fontSize;
            uiText.gameObject.SetActive(true);

            if (!waitForFinish) IsOn = true;

            yield return new WaitForSeconds(duration);

            if (uiText.text == message)
            {
                uiText.text = "";
                uiText.gameObject.SetActive(false);
            }

            if (waitForFinish) IsOn = true;
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

                // ★ 수정된 부분: customFont가 있으면 그걸 쓰고, 없으면 Arial을 기본으로 불러옴
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

            // 만약 이미 만들어진 UI라도 폰트가 도중에 바뀌었을 수 있으므로 업데이트 해줌
            if (customFont != null) uiText.font = customFont;

            return uiText;
        }
    }
}