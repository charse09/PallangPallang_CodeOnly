using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Cinemachine;
using TMPro;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 마우스 감도를 조절할 수 있는 UI 패널을 생성하고 관리하는 Output 스크립트.
    /// 
    /// 기능:
    /// - 특정 키(기본: Escape)를 눌러 감도 조절 패널을 토글
    /// - 슬라이더로 마우스 감도(Gain)를 실시간 조절
    /// - 설정값을 PlayerPrefs에 저장하여 컴퓨터마다 독립적으로 유지
    /// - CinemachineInputAxisController의 모든 축(X, Y)에 감도 배율 적용
    ///
    /// 사용법:
    /// 1. 빈 오브젝트에 이 컴포넌트를 추가
    /// 2. CinemachineCamera가 붙어있는 오브젝트를 targetCinemachineCamera에 연결
    /// 3. 게임 중 Escape 키로 패널 토글
    /// </summary>
    public class MouseSensitivityOutput : ProcessBase
    {
        [Header("Cinemachine 대상")]
        [Tooltip("마우스 감도를 조절할 대상 Cinemachine Camera 오브젝트.\n" +
                 "이 오브젝트에 CinemachineInputAxisController가 있어야 합니다.")]
        [SerializeField] private CinemachineCamera targetCinemachineCamera;

        [Header("감도 설정")]
        [Tooltip("감도 배율의 최소값")]
        [SerializeField] private float minSensitivity = 0.1f;

        [Tooltip("감도 배율의 최대값")]
        [SerializeField] private float maxSensitivity = 5.0f;

        [Tooltip("기본 감도 배율 (1.0 = Cinemachine 기본값 유지)")]
        [SerializeField] private float defaultSensitivity = 1.0f;

        [Header("UI 토글")]
        [Tooltip("이 키를 누르면 감도 조절 패널이 토글됩니다.")]
        [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

        [Tooltip("패널이 열릴 때 마우스 커서를 표시합니다.")]
        [SerializeField] private bool showCursorWhenOpen = true;

        [Tooltip("패널이 열릴 때 게임 시간을 멈춥니다.")]
        [SerializeField] private bool pauseWhenOpen = false;

        // ── PlayerPrefs 키 ──
        private const string PREF_KEY_SENSITIVITY = "MouseSensitivity";

        // ── 런타임 참조 ──
        private CinemachineInputAxisController inputAxisController;
        private float[] originalGains;         // New Input System Gain 기본값 배열
        private float[] originalLegacyGains;   // Legacy Input Gain 기본값 배열
        private float currentSensitivity;

        // ── UI 요소 ──
        private Canvas settingsCanvas;
        private GameObject panelObject;
        private Slider sensitivitySlider;
        private TextMeshProUGUI valueText;
        private TextMeshProUGUI titleText;
        private bool isPanelOpen = false;

        // ── 커서 상태 복원용 ──
        private bool previousCursorVisible;
        private CursorLockMode previousCursorLockState;

        void Awake()
        {
            // Cinemachine 참조 가져오기
            if (targetCinemachineCamera != null)
            {
                inputAxisController = targetCinemachineCamera.GetComponent<CinemachineInputAxisController>();
            }

            if (inputAxisController == null)
            {
                Debug.LogWarning($"[{nameof(MouseSensitivityOutput)}] " +
                    $"'{gameObject.name}': CinemachineInputAxisController를 찾을 수 없습니다. " +
                    "targetCinemachineCamera를 확인해주세요.");
                enabled = false;
                return;
            }

            // 기본 Gain 값 저장
            StoreOriginalGains();

            // 저장된 감도 불러오기
            currentSensitivity = PlayerPrefs.GetFloat(PREF_KEY_SENSITIVITY, defaultSensitivity);

            // UI 생성
            CreateSettingsUI();

            // 저장된 감도 적용
            ApplySensitivity(currentSensitivity);

            // 시작 시 패널 숨기기
            SetPanelActive(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                TogglePanel();
            }
        }

        /// <summary>
        /// ProcessBase.Execute() 구현. 호출 시 패널을 토글합니다.
        /// </summary>
        public override void Execute()
        {
            TogglePanel();
            IsOn = isPanelOpen;
        }

        // ════════════════════════════════════════════
        //  감도 적용 로직
        // ════════════════════════════════════════════

        /// <summary>
        /// 각 컨트롤러의 원래 Gain 값을 저장합니다.
        /// </summary>
        private void StoreOriginalGains()
        {
            var controllers = inputAxisController.Controllers;
            originalGains = new float[controllers.Count];
            originalLegacyGains = new float[controllers.Count];

            for (int i = 0; i < controllers.Count; i++)
            {
                var reader = controllers[i].Input;

                originalGains[i] = reader.Gain;
                originalLegacyGains[i] = reader.LegacyGain;
            }
        }

        /// <summary>
        /// 감도 배율을 모든 축에 적용합니다.
        /// 원래 Gain × 배율 = 최종 Gain
        /// </summary>
        private void ApplySensitivity(float multiplier)
        {
            var controllers = inputAxisController.Controllers;

            for (int i = 0; i < controllers.Count; i++)
            {
                var reader = controllers[i].Input;

                reader.Gain = originalGains[i] * multiplier;
                reader.LegacyGain = originalLegacyGains[i] * multiplier;
            }
        }

        /// <summary>
        /// 슬라이더 변경 시 호출되는 콜백.
        /// </summary>
        private void OnSensitivityChanged(float value)
        {
            currentSensitivity = value;
            ApplySensitivity(currentSensitivity);

            // 값 텍스트 업데이트
            if (valueText != null)
            {
                valueText.text = $"{value:F2}x";
            }

            // PlayerPrefs에 즉시 저장
            PlayerPrefs.SetFloat(PREF_KEY_SENSITIVITY, currentSensitivity);
            PlayerPrefs.Save();
        }

        // ════════════════════════════════════════════
        //  패널 토글
        // ════════════════════════════════════════════

        private void TogglePanel()
        {
            SetPanelActive(!isPanelOpen);
        }

        private void SetPanelActive(bool active)
        {
            isPanelOpen = active;

            if (panelObject != null)
            {
                panelObject.SetActive(active);
            }

            if (active)
            {
                // 커서 상태 저장 및 변경
                if (showCursorWhenOpen)
                {
                    previousCursorVisible = Cursor.visible;
                    previousCursorLockState = Cursor.lockState;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }

                if (pauseWhenOpen)
                {
                    Time.timeScale = 0f;
                }

                // 슬라이더 값 동기화
                if (sensitivitySlider != null)
                {
                    sensitivitySlider.value = currentSensitivity;
                }
            }
            else
            {
                // 커서 상태 복원
                if (showCursorWhenOpen)
                {
                    Cursor.visible = previousCursorVisible;
                    Cursor.lockState = previousCursorLockState;
                }

                if (pauseWhenOpen)
                {
                    Time.timeScale = 1f;
                }
            }
        }

        // ════════════════════════════════════════════
        //  UI 동적 생성
        // ════════════════════════════════════════════

        private void CreateSettingsUI()
        {
            // ── Canvas ──
            GameObject canvasObj = new GameObject("MouseSensitivityCanvas");
            canvasObj.transform.SetParent(transform);
            settingsCanvas = canvasObj.AddComponent<Canvas>();
            settingsCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            settingsCanvas.sortingOrder = 9999; // 항상 최상단에 표시
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // ── 반투명 배경 오버레이 ──
            GameObject overlayObj = CreateUIElement("Overlay", canvasObj.transform);
            RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
            StretchFull(overlayRect);
            Image overlayImage = overlayObj.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.4f);

            // ── 패널 (중앙) ──
            panelObject = CreateUIElement("SettingsPanel", canvasObj.transform);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(500f, 280f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelBg = panelObject.AddComponent<Image>();
            panelBg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);

            // 패널에 둥근 모서리 효과 (Unity 기본 Sprite 로드 실패로 주석 처리)
            // panelBg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
            // panelBg.type = Image.Type.Sliced;

            // ── EventSystem 확인 및 생성 ──
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            // ── 제목 ──
            GameObject titleObj = CreateUIElement("Title", panelObject.transform);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(0f, 60f);
            titleRect.anchoredPosition = new Vector2(0f, -10f);

            titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Mouse Sensitivity Settings";
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;

            // ── "마우스 감도" 레이블 ──
            GameObject labelObj = CreateUIElement("Label", panelObject.transform);
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.sizeDelta = new Vector2(-60f, 30f);
            labelRect.anchoredPosition = new Vector2(0f, -80f);

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = "Sensitivity (Gain Multiplier)";
            labelText.fontSize = 18;
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.color = new Color(0.8f, 0.8f, 0.85f);

            // ── 슬라이더 ──
            GameObject sliderObj = CreateSlider(panelObject.transform);
            RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0f, 1f);
            sliderRect.anchorMax = new Vector2(1f, 1f);
            sliderRect.pivot = new Vector2(0.5f, 1f);
            sliderRect.sizeDelta = new Vector2(-80f, 30f);
            sliderRect.anchoredPosition = new Vector2(0f, -120f);

            sensitivitySlider = sliderObj.GetComponent<Slider>();
            sensitivitySlider.minValue = minSensitivity;
            sensitivitySlider.maxValue = maxSensitivity;
            sensitivitySlider.value = currentSensitivity;
            sensitivitySlider.wholeNumbers = false;
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

            // ── 현재 값 텍스트 ──
            GameObject valueObj = CreateUIElement("ValueText", panelObject.transform);
            RectTransform valueRect = valueObj.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0f, 1f);
            valueRect.anchorMax = new Vector2(1f, 1f);
            valueRect.pivot = new Vector2(0.5f, 1f);
            valueRect.sizeDelta = new Vector2(-60f, 30f);
            valueRect.anchoredPosition = new Vector2(0f, -155f);

            valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = $"{currentSensitivity:F2}x";
            valueText.fontSize = 22;
            valueText.fontStyle = FontStyles.Bold;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = new Color(0.4f, 0.8f, 1f);

            // ── 하단 안내 텍스트 ──
            GameObject hintObj = CreateUIElement("Hint", panelObject.transform);
            RectTransform hintRect = hintObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(-60f, 40f);
            hintRect.anchoredPosition = new Vector2(0f, 15f);

            TextMeshProUGUI hintText = hintObj.AddComponent<TextMeshProUGUI>();
            hintText.text = $"Press [{toggleKey}] to close  |  Settings are auto-saved";
            hintText.fontSize = 14;
            hintText.alignment = TextAlignmentOptions.Center;
            hintText.color = new Color(0.5f, 0.5f, 0.55f);

            // ── 초기화 버튼 ──
            CreateResetButton(panelObject.transform);
        }

        /// <summary>
        /// Unity UI Slider를 코드로 생성합니다.
        /// </summary>
        private GameObject CreateSlider(Transform parent)
        {
            // 슬라이더 루트
            GameObject sliderObj = CreateUIElement("Slider", parent);
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.direction = Slider.Direction.LeftToRight;

            // 배경 트랙
            GameObject bgObj = CreateUIElement("Background", sliderObj.transform);
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            StretchFull(bgRect);
            bgRect.offsetMin = new Vector2(0, 12f);
            bgRect.offsetMax = new Vector2(0, -12f);
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.25f, 0.25f, 0.3f);
            // bgImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
            // bgImage.type = Image.Type.Sliced;

            // Fill Area
            GameObject fillAreaObj = CreateUIElement("Fill Area", sliderObj.transform);
            RectTransform fillAreaRect = fillAreaObj.GetComponent<RectTransform>();
            StretchFull(fillAreaRect);
            fillAreaRect.offsetMin = new Vector2(5f, 12f);
            fillAreaRect.offsetMax = new Vector2(-15f, -12f);

            // Fill
            GameObject fillObj = CreateUIElement("Fill", fillAreaObj.transform);
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            StretchFull(fillRect);
            fillRect.sizeDelta = new Vector2(10f, 0f);
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 1f);
            // fillImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
            // fillImage.type = Image.Type.Sliced;

            // Handle Slide Area
            GameObject handleAreaObj = CreateUIElement("Handle Slide Area", sliderObj.transform);
            RectTransform handleAreaRect = handleAreaObj.GetComponent<RectTransform>();
            StretchFull(handleAreaRect);
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            // Handle
            GameObject handleObj = CreateUIElement("Handle", handleAreaObj.transform);
            RectTransform handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(24f, 0f);
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = Color.white;
            // handleImage.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");

            // 슬라이더에 UI 요소 연결
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            // 슬라이더의 ColorBlock 설정
            var colors = slider.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.5f, 0.85f, 1f);
            colors.pressedColor = new Color(0.3f, 0.6f, 0.9f);
            slider.colors = colors;

            return sliderObj;
        }

        /// <summary>
        /// 기본값으로 초기화하는 버튼을 생성합니다.
        /// </summary>
        private void CreateResetButton(Transform parent)
        {
            GameObject btnObj = CreateUIElement("ResetButton", parent);
            RectTransform btnRect = btnObj.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.5f, 1f);
            btnRect.anchorMax = new Vector2(0.5f, 1f);
            btnRect.pivot = new Vector2(0.5f, 1f);
            btnRect.sizeDelta = new Vector2(160f, 36f);
            btnRect.anchoredPosition = new Vector2(0f, -195f);

            Image btnBg = btnObj.AddComponent<Image>();
            btnBg.color = new Color(0.3f, 0.3f, 0.35f);
            // btnBg.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
            // btnBg.type = Image.Type.Sliced;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnBg;
            var colors = btn.colors;
            colors.normalColor = new Color(0.3f, 0.3f, 0.35f);
            colors.highlightedColor = new Color(0.4f, 0.4f, 0.45f);
            colors.pressedColor = new Color(0.2f, 0.2f, 0.25f);
            btn.colors = colors;

            btn.onClick.AddListener(OnResetClicked);

            // 버튼 텍스트
            GameObject btnTextObj = CreateUIElement("Text", btnObj.transform);
            RectTransform btnTextRect = btnTextObj.GetComponent<RectTransform>();
            StretchFull(btnTextRect);

            TextMeshProUGUI btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
            btnText.text = "Reset to Default";
            btnText.fontSize = 15;
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.color = new Color(0.85f, 0.85f, 0.9f);
        }

        private void OnResetClicked()
        {
            currentSensitivity = defaultSensitivity;
            ApplySensitivity(currentSensitivity);

            if (sensitivitySlider != null)
            {
                sensitivitySlider.value = currentSensitivity;
            }

            PlayerPrefs.SetFloat(PREF_KEY_SENSITIVITY, currentSensitivity);
            PlayerPrefs.Save();
        }

        // ════════════════════════════════════════════
        //  유틸리티
        // ════════════════════════════════════════════

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        void OnDestroy()
        {
            // 슬라이더 콜백 정리
            if (sensitivitySlider != null)
            {
                sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
            }
        }
    }
}
