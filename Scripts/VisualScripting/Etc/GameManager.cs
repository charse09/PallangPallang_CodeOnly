using UnityEngine;
using _Project.Scripts.Data;
using _Project.Scripts.VisualScripting;
using System.Diagnostics;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro; // ★ TextMeshPro 사용을 위한 네임스페이스 추가

public class GameManager : ManagerBase<GameManager>
{
    [Header("Version Settings")]
    [Tooltip("버전을 표시할 TextMeshProUGUI 컴포넌트")]
    [SerializeField] private TextMeshProUGUI versionText;
    [Tooltip("버전 문자열 앞에 붙일 접두사 (예: v -> v1.0.0)")]
    [SerializeField] private string versionPrefix = "v";

    [Header("Respawn Sequence Settings")]
    [SerializeField] private PlayerLocomotion playerLocomotion;
    [SerializeField] private GameObject rescuerPrefab;
    private GameObject activeRescuer;
    private Animator rescuerAnimator;
    [SerializeField] private AnimationCurve dropCurve;
    [SerializeField] private float dropDuration = 1.5f;
    [SerializeField] private Image fadeImage;

    public Transform currentCheckpoint;
    private bool isRespawning = false;

    // ★ [게임 오버 중복 실행 방지 플래그]
    private bool isGameOver = false;

    [Header("Camera Settings")]
    [SerializeField] private Camera mainRenderCamera;

    public Camera MainRenderCamera
    {
        get
        {
            if (mainRenderCamera == null) mainRenderCamera = Camera.main;
            return mainRenderCamera;
        }
    }

    [Header("Cinematic Scope Settings")]
    [Tooltip("상단 검은색 바의 RectTransform (Top-Stretch 앵커 설정 필요)")]
    [SerializeField] private RectTransform topBar;

    [Tooltip("하단 검은색 바의 RectTransform (Bottom-Stretch 앵커 설정 필요)")]
    [SerializeField] private RectTransform bottomBar;

    [Tooltip("시네마틱 바의 목표 높이 (Canvas Scaler 기준 px 높이)")]
    [SerializeField] private float barHeight = 120f;

    [Header("Scene Shortcut Settings")]
    [Tooltip("단축키(F1, F2, F3)를 통한 씬 전환 기능을 활성화할지 여부입니다.")]
    [SerializeField] private bool enableSceneShortcuts = true;
    [SerializeField] private KeyCode previousSceneKey = KeyCode.F1;
    [SerializeField] private KeyCode restartSceneKey = KeyCode.F2;
    [SerializeField] private KeyCode nextSceneKey = KeyCode.F3;

    [Header("Frame Rate Counter Settings")]
    [Tooltip("화면에 FPS 표시 여부")]
    [SerializeField] private bool showFpsGUI = true;

    [Tooltip("FPS 수치 측정/갱신 주기(초)")]
    [SerializeField] private float fpsUpdateInterval = 0.5f;

    [Tooltip("인스펙터 및 GUI에서 실시간으로 확인 가능한 현재 FPS 수치")]
    [SerializeField] private float currentFPS;

    private float _fpsAccumulatedDeltaTime = 0f;
    private int _fpsFrameCount = 0;
    private GUIStyle _guiStyle;

    [Header("Frame Rate Limit Settings")]
    [Tooltip("프레임 제한 기능을 사용할지 여부입니다.")]
    [SerializeField] private bool useFrameRateLimit = true;

    [Tooltip("목표 프레임 제한 수치입니다. (예: 60, 120, 144 / -1 설정 시 제한 없음)")]
    [SerializeField] private int targetFrameRate = 144;

    [Tooltip("수직동기화(VSync) 사용 여부입니다. 체크 시 targetFrameRate 설정은 무시되고 모니터 주사율에 맞춰집니다.")]
    [SerializeField] private bool enableVSync = false;

    // ★ 씬 로드 이벤트 구독
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    protected override void Awake()
    {
        base.Awake();

        // 프레임 제한 적용
        ApplyFrameRateLimit();
    }

    private void Start()
    {
        if (topBar == null || bottomBar == null)
        {
            UnityEngine.Debug.LogWarning("[GameManager] 인스펙터에 Cinematic Bars(Top/Bottom)가 누락되었습니다. 관련 연출이 스킵됩니다.");
        }
        else
        {
            // 초기 상태: 바 높이를 0으로 설정하여 화면 밖으로 숨김
            topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, 0f);
            bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, 0f);
        }

        InitFpsGUIStyle();

        // 초기 버전 표시
        UpdateVersionText();
    }

    // ★ 씬이 로드될 때마다 실행되는 콜백
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSceneReferences();
        UpdateVersionText();
    }

    /// <summary>
    /// 버전 텍스트를 업데이트하고, 참조가 끊어졌다면 자동 재연결을 시도합니다.
    /// </summary>
    private void UpdateVersionText()
    {
        // 1. 참조가 끊어졌거나 null이라면 씬 내에서 "VersionText" 이름을 가진 오브젝트 자동 탐색
        if (versionText == null)
        {
            GameObject verObj = GameObject.Find("VersionText");
            if (verObj != null)
            {
                versionText = verObj.GetComponent<TextMeshProUGUI>();
            }
        }

        // 2. 텍스트 바인딩
        if (versionText != null)
        {
            versionText.text = $"{versionPrefix}{Application.version}";
        }
    }

    private void Update()
    {
        UpdateFrameRateCounter();

        if (enableSceneShortcuts)
        {
            if (Input.GetKeyDown(previousSceneKey))
            {
                LoadPreviousScene();
            }
            else if (Input.GetKeyDown(restartSceneKey))
            {
                RestartCurrentScene();
            }
            else if (Input.GetKeyDown(nextSceneKey))
            {
                LoadNextScene();
            }
        }
    }

    // =========================================================
    // ★ [게임 오버 처리 구문]
    // =========================================================

    public void GameOver()
    {
        if (isGameOver) return; // 중복 실행 방지
        isGameOver = true;

        UnityEngine.Debug.Log("<color=red>[GameManager] GameOver 발동! 암전 후 씬을 재시작합니다.</color>");
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSeconds(1.5f);

        yield return FadeScreen(1f, 1.0f);

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(currentSceneIndex);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        RefreshSceneReferences();
        UpdateVersionText(); // 씬 재로드 후 버전 UI 재갱신

        yield return FadeScreen(0f, 1.0f);

        isGameOver = false;
    }

    private void RefreshSceneReferences()
    {
        if (fadeImage == null)
        {
            GameObject fadeObj = GameObject.Find("FadeImage");
            if (fadeObj != null)
            {
                fadeImage = fadeObj.GetComponent<Image>();
            }
        }
    }

    // =========================================================

    public void ApplyFrameRateLimit()
    {
        if (enableVSync)
        {
            QualitySettings.vSyncCount = 1;
            UnityEngine.Debug.Log("[GameManager] VSync가 활성화되었습니다. (TargetFrameRate 무시됨)");
        }
        else
        {
            QualitySettings.vSyncCount = 0;

            if (useFrameRateLimit)
            {
                Application.targetFrameRate = targetFrameRate;
                UnityEngine.Debug.Log($"[GameManager] 프레임 제한 적용: {targetFrameRate} FPS");
            }
            else
            {
                Application.targetFrameRate = -1;
                UnityEngine.Debug.Log("[GameManager] 프레임 제한이 해제되었습니다.");
            }
        }
    }

    public void SetTargetFrameRate(int fps)
    {
        targetFrameRate = fps;
        useFrameRateLimit = true;
        ApplyFrameRateLimit();
    }

    public void SetVSync(bool useVSync)
    {
        enableVSync = useVSync;
        ApplyFrameRateLimit();
    }

    private void UpdateFrameRateCounter()
    {
        _fpsAccumulatedDeltaTime += Time.unscaledDeltaTime;
        _fpsFrameCount++;

        if (_fpsAccumulatedDeltaTime >= fpsUpdateInterval)
        {
            currentFPS = _fpsFrameCount / _fpsAccumulatedDeltaTime;
            _fpsAccumulatedDeltaTime = 0f;
            _fpsFrameCount = 0;
        }
    }

    private void InitFpsGUIStyle()
    {
        _guiStyle = new GUIStyle();
        _guiStyle.alignment = TextAnchor.UpperLeft;
        _guiStyle.fontSize = 20;
        _guiStyle.fontStyle = FontStyle.Bold;
        _guiStyle.normal.textColor = Color.green;
    }

    private void OnGUI()
    {
        if (!showFpsGUI) return;

        if (_guiStyle == null) InitFpsGUIStyle();

        if (currentFPS >= 60f)
            _guiStyle.normal.textColor = Color.green;
        else if (currentFPS >= 30f)
            _guiStyle.normal.textColor = Color.yellow;
        else
            _guiStyle.normal.textColor = Color.red;

        string text = $"FPS: {currentFPS:F1}";
        GUI.Label(new Rect(15, 15, 150, 40), text, _guiStyle);
    }

    public void StartRespawnSequence(Transform targetCheckpoint = null)
    {
        if (isRespawning) return;

        if (targetCheckpoint != null)
        {
            currentCheckpoint = targetCheckpoint;
        }

        if (currentCheckpoint == null)
        {
            UnityEngine.Debug.LogError("[GameManager] 리스폰할 체크포인트(currentCheckpoint)가 지정되지 않았습니다!");
            return;
        }

        UnityEngine.Debug.Log($"StartRespawnSequence 발동 (목표 체크포인트: {currentCheckpoint.name})");
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;
        playerLocomotion.SetRespawnMode(true);

        // [FIX] 사망 시 물리적 텔레포트로 인해 OnTriggerExit가 호출되지 않아 트리거가 영구적으로 켜진 채 
        // 멈추는 버그(이후 재진입 시 발동 안됨)를 방지하기 위해 씬 내의 모든 TriggerEnter를 리셋합니다.
        TriggerEnter[] triggers = FindObjectsByType<TriggerEnter>(FindObjectsSortMode.None);
        foreach (var t in triggers)
        {
            t.Reset();
        }

        yield return new WaitForSeconds(1.0f);

        yield return FadeScreen(1f, 0.5f);

        Vector3 targetPos = currentCheckpoint.position;
        Vector3 startPos = targetPos + Vector3.up * 10f;

        playerLocomotion.transform.position = startPos;

        if (activeRescuer == null)
        {
            activeRescuer = Instantiate(rescuerPrefab);
            rescuerAnimator = activeRescuer.GetComponent<Animator>();
        }

        activeRescuer.transform.position = startPos + new Vector3(0, 1.8f, -0.2f);
        activeRescuer.transform.rotation = playerLocomotion.transform.rotation;
        activeRescuer.SetActive(true);

        if (rescuerAnimator != null) rescuerAnimator.SetBool("IsHanging", true);

        yield return FadeScreen(0f, 0.5f);

        playerLocomotion.ForceRespawnAction();

        float elapsedTime = 0f;
        while (elapsedTime < dropDuration)
        {
            elapsedTime += Time.deltaTime;
            float curveValue = dropCurve.Evaluate(elapsedTime / dropDuration);

            playerLocomotion.transform.position = Vector3.Lerp(startPos, targetPos, curveValue);
            activeRescuer.transform.position = playerLocomotion.transform.position + new Vector3(0, 2.0f, 0f);

            yield return null;
        }

        playerLocomotion.transform.position = targetPos;

        if (rescuerAnimator != null) rescuerAnimator.SetBool("IsHanging", false);
        activeRescuer.SetActive(false);

        playerLocomotion.SetRespawnMode(false);
        isRespawning = false;
    }

    private IEnumerator FadeScreen(float targetAlpha, float duration)
    {
        if (fadeImage == null) yield break;

        Color c = fadeImage.color;
        float startAlpha = c.a;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = targetAlpha;
        fadeImage.color = c;
    }

    public IEnumerator SetCinematicBars(bool show, float duration)
    {
        if (topBar == null || bottomBar == null)
        {
            UnityEngine.Debug.LogWarning("[GameManager] Cinematic Bars(Top/Bottom)가 할당되지 않았습니다.");
            yield break;
        }

        float time = 0f;
        float startHeight = topBar.sizeDelta.y;
        float endHeight = show ? barHeight : 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float currentHeight = Mathf.Lerp(startHeight, endHeight, smoothT);

            // 상/하단 바의 높이(sizeDelta.y)를 동적으로 변경
            topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, currentHeight);
            bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, currentHeight);

            yield return null;
        }

        topBar.sizeDelta = new Vector2(topBar.sizeDelta.x, endHeight);
        bottomBar.sizeDelta = new Vector2(bottomBar.sizeDelta.x, endHeight);
    }

    public void RestartCurrentScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        UnityEngine.Debug.Log($"[GameManager] [F2] 현재 씬 재시작 (Build Index: {currentSceneIndex})");
        SceneManager.LoadScene(currentSceneIndex);
    }

    public void LoadPreviousScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int previousSceneIndex = currentSceneIndex - 1;

        if (previousSceneIndex >= 0)
        {
            UnityEngine.Debug.Log($"[GameManager] [F1] 이전 씬 로드 (Build Index: {previousSceneIndex})");
            SceneManager.LoadScene(previousSceneIndex);
        }
        else
        {
            UnityEngine.Debug.LogWarning("[GameManager] 이전 씬이 존재하지 않습니다. (현재 씬이 첫 번째 씬입니다.)");
        }
    }

    public void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        int nextSceneIndex = currentSceneIndex + 1;
        int totalSceneCount = SceneManager.sceneCountInBuildSettings;

        if (nextSceneIndex < totalSceneCount)
        {
            UnityEngine.Debug.Log($"[GameManager] [F3] 다음 씬 로드 (Build Index: {nextSceneIndex})");
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            UnityEngine.Debug.LogWarning("[GameManager] 다음 씬이 존재하지 않습니다. (현재 씬이 마지막 씬입니다.)");
        }
    }

    public void LoadSceneByIndex(int sceneIndex)
    {
        if (sceneIndex >= 0 && sceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            UnityEngine.Debug.Log($"[GameManager] 지정 씬 로드 (Build Index: {sceneIndex})");
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            UnityEngine.Debug.LogError($"[GameManager] 존재하지 않는 Scene Index입니다: {sceneIndex}");
        }
    }
}