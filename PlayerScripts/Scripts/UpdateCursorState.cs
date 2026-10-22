using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UpdateCursorState : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private bool lockCursorOnStart = true;
    [SerializeField] private bool isCursorLocked = false;

    [Header("Cursor Release Key Settings")]
    [Tooltip("누르고 있는 동안 커서를 완전히 자유롭게 풀어 게임 창 밖으로 뺄 수 있는 키")]
    [SerializeField] private KeyCode cursorReleaseKey = KeyCode.LeftAlt;
    [Tooltip("체크 시 누르고 있을 때뿐만 아니라 한 번 누를 때마다 켜고 끄는 토글 방식으로 작동")]
    [SerializeField] private bool toggleReleaseKey = false;

    private bool _isKeyReleasedActive = false;

    [Header("Custom Cursor Settings")]
    [Tooltip("커서로 사용할 텍스처 (미지정 시 Resources 또는 PlayerSettings의 커서 자동 사용)")]
    [SerializeField] private Texture2D cursorTexture;
    [Tooltip("커서 클릭 기준점 (좌측 상단: 0, 0)")]
    [SerializeField] private Vector2 cursorHotSpot = Vector2.zero;
    [Tooltip("커서 렌더링 모드 (ForceSoftware 적용 시 크기 제한 없이 원본 해상도 유지)")]
    [SerializeField] private CursorMode cursorMode = CursorMode.ForceSoftware;
    [Tooltip("원하는 커서 픽셀 크기 (X: 가로, Y: 세로). 0, 0이면 원본 크기 유지. 임의의 원하는 크기(예: 64, 64 또는 70, 70 등) 자유롭게 입력 가능")]
    [SerializeField] private Vector2Int customCursorSize = new Vector2Int(64, 64);

    private Texture2D _scaledCursorTexture;
    private Vector2Int _lastScaledSize;
    private Texture2D _lastSourceTexture;

    [Header("Scene Exclusion Settings")]
    [Tooltip("커서 잠금 로직을 적용하지 않을 씬 이름 목록 (예: 2__Lobby)")]
    [SerializeField] private List<string> excludedScenes = new List<string> { "2__Lobby" };

    [Header("Cursor Visibility Conditions (Panels)")]
    [Tooltip("메뉴 패널 (미지정 시 자동 탐색)")]
    [SerializeField] private GameObject menuPanel;
    [Tooltip("설정 패널 (미지정 시 자동 탐색)")]
    [SerializeField] private GameObject settingsPanel;
    [Tooltip("게임 종료 확인 패널 (미지정 시 자동 탐색)")]
    [SerializeField] private GameObject quitConfirmationPanel;
    [Tooltip("가이드북 패널 (미지정 시 자동 탐색)")]
    [SerializeField] private GameObject guidePanel;
    [Tooltip("다이어리/회상 패널 (미지정 시 자동 탐색)")]
    [SerializeField] private GameObject diaryPanel;

    [Tooltip("활성화(Active)되어 있을 때 커서를 자동으로 표시하고 잠금 해제할 추가 UI 패널 목록 (인벤토리, 대화창 등 자유롭게 추가 가능)")]
    [SerializeField] private List<GameObject> cursorActivePanels = new List<GameObject>();

    /// <summary>
    /// 외부 스크립트/컴포넌트(CursorActivator 등)에서 커서 표시를 요청한 목록
    /// </summary>
    private static readonly HashSet<object> _cursorRequesters = new HashSet<object>();

    /// <summary>
    /// 코드로 커서를 강제로 표시/해제할 수 있는 전역 플래그
    /// </summary>
    public static bool isCursorForceVisible = false;

    /// <summary>
    /// 메뉴/일시정지 상태일 때 커서가 잠기는 것을 방지하는 전역 플래그
    /// </summary>
    public static bool isMenuOpen = false;

    /// <summary>
    /// 현재 활성화된 씬이 제외 씬(예: 2__Lobby)인지 여부
    /// </summary>
    public bool isCurrentSceneExcluded { get; private set; } = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitGlobalCursor()
    {
        Texture2D cursor = Resources.Load<Texture2D>("cursor");
#if UNITY_EDITOR
        if (cursor == null)
        {
            cursor = UnityEditor.PlayerSettings.defaultCursor;
        }
#endif
        if (cursor != null)
        {
            if (cursor.width > 64 || cursor.height > 64)
            {
                Texture2D scaled = ResizeTexture(cursor, 64, 64);
                Cursor.SetCursor(scaled, Vector2.zero, CursorMode.ForceSoftware);
            }
            else
            {
                Cursor.SetCursor(cursor, Vector2.zero, CursorMode.ForceSoftware);
            }
        }
    }

    private void Awake()
    {
        InitializeCursorTexture();
        ApplyCustomCursor();
        AutoFindPanelsIfNull();
        EvaluateCursorState();
    }

    private void InitializeCursorTexture()
    {
        if (cursorTexture == null)
        {
            cursorTexture = Resources.Load<Texture2D>("cursor");
#if UNITY_EDITOR
            if (cursorTexture == null)
            {
                cursorTexture = UnityEditor.PlayerSettings.defaultCursor;
            }
#endif
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            ApplyCustomCursor();
        }
    }
#endif

    public void ApplyCustomCursor()
    {
        if (cursorTexture != null)
        {
            Texture2D textureToApply = cursorTexture;
            Vector2 hotSpotToApply = cursorHotSpot;

            if (customCursorSize.x > 0 && customCursorSize.y > 0 &&
                (cursorTexture.width != customCursorSize.x || cursorTexture.height != customCursorSize.y))
            {
                if (_scaledCursorTexture == null || _lastScaledSize != customCursorSize || _lastSourceTexture != cursorTexture)
                {
                    DestroyScaledTexture();

                    _scaledCursorTexture = ResizeTexture(cursorTexture, customCursorSize.x, customCursorSize.y);
                    _lastScaledSize = customCursorSize;
                    _lastSourceTexture = cursorTexture;
                }
                textureToApply = _scaledCursorTexture;

                if (cursorTexture.width > 0 && cursorTexture.height > 0)
                {
                    hotSpotToApply = new Vector2(
                        cursorHotSpot.x * ((float)customCursorSize.x / cursorTexture.width),
                        cursorHotSpot.y * ((float)customCursorSize.y / cursorTexture.height)
                    );
                }
            }

            Cursor.SetCursor(textureToApply, hotSpotToApply, cursorMode);
        }
    }

    public static Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        if (source == null || targetWidth <= 0 || targetHeight <= 0)
        {
            return source;
        }

        if (source.isReadable)
        {
            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            result.name = $"{source.name}_scaled_{targetWidth}x{targetHeight}";
            result.filterMode = FilterMode.Bilinear;

            Color[] rcolors = new Color[targetWidth * targetHeight];
            for (int y = 0; y < targetHeight; y++)
            {
                float v = (y + 0.5f) / targetHeight;
                for (int x = 0; x < targetWidth; x++)
                {
                    float u = (x + 0.5f) / targetWidth;
                    rcolors[y * targetWidth + x] = source.GetPixelBilinear(u, v);
                }
            }
            result.SetPixels(rcolors);
            result.Apply();
            return result;
        }
        else
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0, RenderTextureFormat.ARGB32);
            RenderTexture prevActive = RenderTexture.active;
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;

            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
            result.name = $"{source.name}_scaled_{targetWidth}x{targetHeight}";
            result.filterMode = FilterMode.Bilinear;

            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();

            RenderTexture.active = prevActive;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }
    }

    private void DestroyScaledTexture()
    {
        if (_scaledCursorTexture != null)
        {
#if UNITY_EDITOR
            DestroyImmediate(_scaledCursorTexture);
#else
            Destroy(_scaledCursorTexture);
#endif
            _scaledCursorTexture = null;
        }
    }

    private void OnDestroy()
    {
        DestroyScaledTexture();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        CheckCurrentScene(SceneManager.GetActiveScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckCurrentScene(scene);
    }

    private void CheckCurrentScene(Scene scene)
    {
        isCurrentSceneExcluded = IsExcludedScene(scene.name);
        AutoFindPanelsIfNull();
        EvaluateCursorState();
    }

    private bool IsExcludedScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;

        foreach (var excluded in excludedScenes)
        {
            if (string.Equals(sceneName, excluded, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    void Update()
    {
        HandleReleaseKeyInput();
        EvaluateCursorState();
    }

    private void HandleReleaseKeyInput()
    {
        if (cursorReleaseKey == KeyCode.None) return;

        if (toggleReleaseKey)
        {
            if (Input.GetKeyDown(cursorReleaseKey))
            {
                _isKeyReleasedActive = !_isKeyReleasedActive;
            }
        }
        else
        {
            _isKeyReleasedActive = Input.GetKey(cursorReleaseKey);
        }
    }

    /// <summary>
    /// 현재 마우스 커서가 화면에 보여야 하는 상황인지 여부를 평가합니다.
    /// </summary>
    public bool ShouldShowCursor()
    {
        // 1. 제외 씬(2__Lobby 등): 커서 항상 표시
        if (isCurrentSceneExcluded)
        {
            return true;
        }

        // 2. 외부 코드 강제 요청 또는 메뉴 열림 플래그 확인
        if (isMenuOpen || isCursorForceVisible || _cursorRequesters.Count > 0)
        {
            return true;
        }

        // 3. 주요 기본 UI 패널 활성화 확인
        if (IsPanelActive(menuPanel) ||
            IsPanelActive(settingsPanel) ||
            IsPanelActive(quitConfirmationPanel) ||
            IsPanelActive(guidePanel) ||
            IsPanelActive(diaryPanel))
        {
            return true;
        }

        // 4. 추가 등록된 패널 리스트 확인
        if (cursorActivePanels != null)
        {
            for (int i = 0; i < cursorActivePanels.Count; i++)
            {
                if (IsPanelActive(cursorActivePanels[i]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 상황에 따라 마우스 커서의 3가지 상태(Locked / Confined / None)를 갱신합니다.
    /// </summary>
    public void EvaluateCursorState()
    {
        // 1순위: 해제 키(Alt 등) 입력 중일 때 -> 화면 밖으로 자유 이동 (None)
        if (_isKeyReleasedActive)
        {
            ApplyCursorMode(CursorLockMode.None, true);
            return;
        }

        // 2순위: UI 패널/로비 등 커서 조작이 필요한 상황 -> 게임 창 내로 제한 (Confined)
        if (ShouldShowCursor())
        {
            ApplyCursorMode(CursorLockMode.Confined, true);
        }
        // 3순위: 일반 인게임 플레이 상태 -> 커서 숨김 및 중앙 잠금 (Locked)
        else
        {
            ApplyCursorMode(CursorLockMode.Locked, false);
        }
    }

    private void ApplyCursorMode(CursorLockMode targetMode, bool visible)
    {
        // 제외 씬(2__Lobby 등)에서는 잠금(Locked) 상태가 되지 않도록 방어
        if (isCurrentSceneExcluded && targetMode == CursorLockMode.Locked)
        {
            targetMode = _isKeyReleasedActive ? CursorLockMode.None : CursorLockMode.Confined;
            visible = true;
        }

        // 현재 상태와 다를 때만 갱신 (불필요한 반복 호출 방지)
        if (Cursor.lockState != targetMode || Cursor.visible != visible)
        {
            Cursor.lockState = targetMode;
            Cursor.visible = visible;

            if (visible)
            {
                ApplyCustomCursor();
            }
        }

        isCursorLocked = (targetMode == CursorLockMode.Locked);
    }

    private static bool IsPanelActive(GameObject panel)
    {
        return panel != null && panel.activeInHierarchy;
    }

    public void SetCursorLock(bool cursorLock)
    {
        if (cursorLock)
        {
            ApplyCursorMode(CursorLockMode.Locked, false);
        }
        else
        {
            CursorLockMode targetMode = _isKeyReleasedActive ? CursorLockMode.None : CursorLockMode.Confined;
            ApplyCursorMode(targetMode, true);
        }
    }

    /// <summary>
    /// 외부 객체(컴포넌트, 컷씬 등)에서 커서 표시를 요청합니다.
    /// </summary>
    public static void RequestCursor(object requester)
    {
        if (requester != null)
        {
            _cursorRequesters.Add(requester);
            UpdateCursorState instance = Object.FindFirstObjectByType<UpdateCursorState>();
            if (instance != null)
            {
                instance.EvaluateCursorState();
            }
        }
    }

    /// <summary>
    /// 외부 객체의 커서 표시 요청을 해제합니다.
    /// </summary>
    public static void ReleaseCursor(object requester)
    {
        if (requester != null)
        {
            _cursorRequesters.Remove(requester);
            UpdateCursorState instance = Object.FindFirstObjectByType<UpdateCursorState>();
            if (instance != null)
            {
                instance.EvaluateCursorState();
            }
        }
    }

    /// <summary>
    /// 동적으로 생성된 UI 패널을 커서 활성화 감지 목록에 등록합니다.
    /// </summary>
    public void RegisterCursorPanel(GameObject panel)
    {
        if (panel != null && !cursorActivePanels.Contains(panel))
        {
            cursorActivePanels.Add(panel);
            EvaluateCursorState();
        }
    }

    /// <summary>
    /// UI 패널을 커서 활성화 감지 목록에서 제거합니다.
    /// </summary>
    public void UnregisterCursorPanel(GameObject panel)
    {
        if (panel != null && cursorActivePanels.Contains(panel))
        {
            cursorActivePanels.Remove(panel);
            EvaluateCursorState();
        }
    }

    /// <summary>
    /// 인스펙터에 패널 참조가 비어있는 경우, 계층 구조에서 자동 검색하여 바인딩합니다.
    /// </summary>
    public void AutoFindPanelsIfNull()
    {
        if (menuPanel == null)
            menuPanel = FindPanelByName("Menu");

        if (settingsPanel == null)
            settingsPanel = FindPanelByName("SettingPanel");

        if (quitConfirmationPanel == null)
            quitConfirmationPanel = FindPanelByName("QuitConfirmationPanel");

        if (guidePanel == null)
            guidePanel = FindPanelByName("GuideBook");

        if (diaryPanel == null)
            diaryPanel = FindPanelByName("DiaryPanel") ?? FindPanelByName("MemoryPanel");
    }

    private GameObject FindPanelByName(string panelName)
    {
        if (transform.parent != null)
        {
            Transform found = transform.parent.Find(panelName);
            if (found != null) return found.gameObject;
        }

        Transform rootFound = FindChildRecursive(transform.root, panelName);
        if (rootFound != null) return rootFound.gameObject;

        return null;
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase))
                return child;

            Transform found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }
        return null;
    }
}