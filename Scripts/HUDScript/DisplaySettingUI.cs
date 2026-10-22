using UnityEngine;
using UnityEngine.UI;

public class DisplaySettingUI : MonoBehaviour
{
    [Header("--- Brightness Slider (0.0 ~ 1.0) ---")]
    [SerializeField] private Slider brightnessSlider;

    [Header("--- Mode Comparison (인스펙터에서 비교 테스트) ---")]
    [Tooltip("InGameOnly: 3D 씬만 조절 / FullScreen: UI 포함 전체 화면 조절")]
    [SerializeField] private BrightnessMode mode = BrightnessMode.InGameOnly;

    [Header("--- Debug Comparison Hotkey ---")]
    [Tooltip("플레이 도중 단축키(기본 F1)를 눌러 두 모드를 실시간으로 전환하며 체감 비교할 수 있습니다.")]
    [SerializeField] private bool enableDebugToggleKey = true;
    [SerializeField] private KeyCode debugToggleKey = KeyCode.F1;

    private void Awake()
    {
        if (brightnessSlider == null)
        {
            Slider[] sliders = GetComponentsInChildren<Slider>(true);
            foreach (var s in sliders)
            {
                if (s.name.Contains("Brightness") || s.name.Contains("brightness"))
                {
                    brightnessSlider = s;
                    break;
                }
            }
            if (brightnessSlider == null && sliders.Length > 0)
            {
                brightnessSlider = sliders[0];
            }
        }
    }

    private void Start()
    {
        InitSlider();
        SyncModeFromManager();
    }

    private void OnEnable()
    {
        RefreshSlider();
        SyncModeFromManager();
    }

    private void Update()
    {
        // F1 키 등을 눌러 두 모드를 즉각 전환하며 실시간 비교 가능
        if (enableDebugToggleKey && Input.GetKeyDown(debugToggleKey))
        {
            if (BrightnessManager.Instance != null)
            {
                BrightnessManager.Instance.ToggleBrightnessMode();
                SyncModeFromManager();
                Debug.Log($"[DisplaySettingUI] 밝기 모드 전환됨: {mode} (F1 키로 전환 가능)");
            }
        }
    }

    private void InitSlider()
    {
        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 0f;
            brightnessSlider.maxValue = 1f;
            brightnessSlider.onValueChanged.RemoveListener(OnBrightnessSliderChanged);
            brightnessSlider.onValueChanged.AddListener(OnBrightnessSliderChanged);
        }

        RefreshSlider();
    }

    private void RefreshSlider()
    {
        if (brightnessSlider != null && BrightnessManager.Instance != null)
        {
            brightnessSlider.value = BrightnessManager.Instance.GetBrightness();
        }
    }

    private void SyncModeFromManager()
    {
        if (BrightnessManager.Instance != null)
        {
            mode = BrightnessManager.Instance.CurrentMode;
        }
    }

    private void OnBrightnessSliderChanged(float value)
    {
        if (BrightnessManager.Instance != null)
        {
            BrightnessManager.Instance.SetBrightness(value);
        }
    }

    /// <summary>
    /// 외부 UI(예: 드롭다운이나 버튼)에서 모드를 변경할 때 호출할 수 있는 메서드
    /// </summary>
    public void SetMode(BrightnessMode newMode)
    {
        mode = newMode;
        if (BrightnessManager.Instance != null)
        {
            BrightnessManager.Instance.SetBrightnessMode(newMode);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying && BrightnessManager.Instance != null)
        {
            BrightnessManager.Instance.SetBrightnessMode(mode);
        }
    }
#endif
}
