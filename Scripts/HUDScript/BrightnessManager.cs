using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum BrightnessMode
{
    [Tooltip("URP Post Exposure 사용: 설정창/UI는 제외하고 3D 인게임 씬 그래픽만 밝기 조절")]
    InGameOnly,

    [Tooltip("전체 화면 UI Overlay 사용: Canvas UI를 포함한 화면 전체를 밝거나 어둡게 조절")]
    FullScreen
}

public class BrightnessManager : MonoBehaviour
{
    private static BrightnessManager instance;
    public static BrightnessManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<BrightnessManager>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("BrightnessManager");
                    instance = obj.AddComponent<BrightnessManager>();
                }
            }
            return instance;
        }
    }

    [Header("--- Brightness Mode ---")]
    [Tooltip("밝기 조절 모드: InGameOnly (3D 그래픽만) vs FullScreen (UI 포함 전체 화면)")]
    [SerializeField] private BrightnessMode currentMode = BrightnessMode.InGameOnly;

    [Header("--- Option 1: InGameOnly (Post Exposure EV) ---")]
    [Tooltip("슬라이더 0.0일 때의 Post Exposure 값 (어두움)")]
    [SerializeField] private float minExposure = -2.0f;
    [Tooltip("슬라이더 1.0일 때의 Post Exposure 값 (밝음)")]
    [SerializeField] private float maxExposure = 2.0f;

    [Header("--- Option 2: FullScreen (UI Overlay Alpha) ---")]
    [Tooltip("슬라이더 0.0일 때의 검은색 오버레이 최대 불투명도 (어두움)")]
    [Range(0f, 1f)] [SerializeField] private float maxDarkAlpha = 0.85f;
    [Tooltip("슬라이더 1.0일 때의 흰색 오버레이 최대 불투명도 (밝음)")]
    [Range(0f, 1f)] [SerializeField] private float maxBrightAlpha = 0.45f;

    private Volume brightnessVolume;
    private ColorAdjustments colorAdjustments;

    // FullScreen 모드용 전용 오버레이 Canvas & Image
    private GameObject overlayCanvasObj;
    private Image overlayImage;

    public const string BrightnessPrefKey = "Display_Brightness";
    public const string BrightnessModePrefKey = "Display_BrightnessMode";
    public const float DefaultBrightness = 0.5f;

    public BrightnessMode CurrentMode => currentMode;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 저장된 모드 불러오기
            currentMode = (BrightnessMode)PlayerPrefs.GetInt(BrightnessModePrefKey, (int)currentMode);

            InitBrightnessVolume();
            InitFullScreenOverlay();

            SceneManager.sceneLoaded += OnSceneLoaded;

            // 초기 밝기 적용
            ApplyBrightness(GetBrightness());
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureCameraPostProcessing();
    }

    private void InitBrightnessVolume()
    {
        if (brightnessVolume == null)
        {
            brightnessVolume = GetComponent<Volume>();
            if (brightnessVolume == null)
            {
                brightnessVolume = gameObject.AddComponent<Volume>();
            }
            brightnessVolume.isGlobal = true;
            brightnessVolume.priority = 100f; // 최우선 적용
        }

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "BrightnessGlobalProfile";

        if (!profile.TryGet(out colorAdjustments))
        {
            colorAdjustments = profile.Add<ColorAdjustments>(true);
        }

        colorAdjustments.postExposure.overrideState = true;
        brightnessVolume.profile = profile;
    }

    private void InitFullScreenOverlay()
    {
        if (overlayCanvasObj != null) return;

        // 모든 UI 및 씬 위에 렌더링되는 전용 최상위 Overlay Canvas 생성
        overlayCanvasObj = new GameObject("BrightnessOverlayCanvas");
        DontDestroyOnLoad(overlayCanvasObj);

        Canvas canvas = overlayCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767; // 유니티 최상위 Sorting Order

        CanvasScaler scaler = overlayCanvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // 전체 화면을 덮는 Image 오브젝트 생성
        GameObject imageObj = new GameObject("OverlayImage");
        imageObj.transform.SetParent(overlayCanvasObj.transform, false);

        RectTransform rect = imageObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        overlayImage = imageObj.AddComponent<Image>();
        overlayImage.color = new Color(0, 0, 0, 0);
        overlayImage.raycastTarget = false; // 마우스 클릭 차단 방지 (매우 중요)
    }

    /// <summary>
    /// 밝기 조절 모드를 변경합니다.
    /// </summary>
    public void SetBrightnessMode(BrightnessMode mode)
    {
        currentMode = mode;
        PlayerPrefs.SetInt(BrightnessModePrefKey, (int)currentMode);
        PlayerPrefs.Save();

        ApplyBrightness(GetBrightness());
    }

    public void ToggleBrightnessMode()
    {
        SetBrightnessMode(currentMode == BrightnessMode.InGameOnly ? BrightnessMode.FullScreen : BrightnessMode.InGameOnly);
    }

    /// <summary>
    /// 슬라이더 값(0.0 ~ 1.0)을 받아 화면 밝기에 적용하고 PlayerPrefs에 저장합니다.
    /// </summary>
    public void SetBrightness(float sliderValue)
    {
        sliderValue = Mathf.Clamp01(sliderValue);
        PlayerPrefs.SetFloat(BrightnessPrefKey, sliderValue);
        PlayerPrefs.Save();

        ApplyBrightness(sliderValue);
    }

    /// <summary>
    /// 현재 저장된 밝기 슬라이더 값(0.0 ~ 1.0, 기본값 0.5)을 반환합니다.
    /// </summary>
    public float GetBrightness()
    {
        return PlayerPrefs.GetFloat(BrightnessPrefKey, DefaultBrightness);
    }

    private void ApplyBrightness(float sliderValue)
    {
        if (currentMode == BrightnessMode.InGameOnly)
        {
            // Option 1: InGame 3D 씬만 URP Post Exposure로 조절
            if (overlayImage != null)
            {
                overlayImage.color = new Color(0, 0, 0, 0); // 오버레이 끔
            }

            if (colorAdjustments != null)
            {
                float exposure = Mathf.Lerp(minExposure, maxExposure, sliderValue);
                colorAdjustments.postExposure.value = exposure;
            }

            EnsureCameraPostProcessing();
        }
        else // FullScreen
        {
            // Option 2: UI를 포함한 화면 전체를 Overlay로 조절
            if (colorAdjustments != null)
            {
                colorAdjustments.postExposure.value = 0f; // URP 노출은 중립(기본)으로 복원
            }

            if (overlayImage != null)
            {
                if (sliderValue < 0.5f)
                {
                    // 어두움: 검은색 오버레이 알파 증가 (0.5 -> 0, 0.0 -> maxDarkAlpha)
                    float t = (0.5f - sliderValue) / 0.5f;
                    overlayImage.color = new Color(0f, 0f, 0f, t * maxDarkAlpha);
                }
                else if (sliderValue > 0.5f)
                {
                    // 밝음: 흰색 오버레이 알파 증가 (0.5 -> 0, 1.0 -> maxBrightAlpha)
                    float t = (sliderValue - 0.5f) / 0.5f;
                    overlayImage.color = new Color(1f, 1f, 1f, t * maxBrightAlpha);
                }
                else
                {
                    overlayImage.color = new Color(0f, 0f, 0f, 0f);
                }
            }
        }
    }

    public void EnsureCameraPostProcessing()
    {
        Camera cam = Camera.main;
        if (cam == null) cam = Object.FindFirstObjectByType<Camera>();

        if (cam != null && cam.TryGetComponent<UniversalAdditionalCameraData>(out var camData))
        {
            if (!camData.renderPostProcessing)
            {
                camData.renderPostProcessing = true;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyBrightness(GetBrightness());
        }
    }
#endif
}
