using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MenuManager : MonoBehaviour
{
    [Header("--- UI Panels ---")]
    public GameObject settingsPanel;           // 설정 UI 패널
    public GameObject menuPanel;
    public GameObject quitConfirmationPanel;   // 게임 종료 확인 팝업 패널
    public GameObject guidePanel;              // 가이드북 UI 패널
    public GameObject diaryPanel;              // 다이어리/회상 UI 패널

    [Header("--- Key Input / Menu Control ---")]
    [Tooltip("메뉴를 토글하는 OpenMenuKey 오브젝트 (비어있을 시 자동 탐색)")]
    [SerializeField] private GameObject openMenuKeyObject;

    private bool _isAnyMenuOpen = false;
    private bool _ignoreNextFrameOpenMenuKey = false;

    void Awake()
    {
        FindAllPanelsIfNull();
        SetupTopmostPanels();
        BindQuitConfirmationButtons();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        FindAllPanelsIfNull();
        SetupTopmostPanels();

        // 시작 시 모든 팝업 패널 닫아둔다
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);
        if (guidePanel != null) guidePanel.SetActive(false);
        if (diaryPanel != null) diaryPanel.SetActive(false);

        BindQuitConfirmationButtons();
    }

    private void OnEnable()
    {
        SetupTopmostPanels();
    }

    public void FindOpenMenuKeyObject()
    {
        if (openMenuKeyObject != null) return;

        if (transform.parent != null)
        {
            Transform found = transform.parent.Find("OpenMenuKey");
            if (found != null)
            {
                openMenuKeyObject = found.gameObject;
                return;
            }
        }

        UICanvas canvas = GetComponentInParent<UICanvas>();
        if (canvas != null)
        {
            Transform found = canvas.transform.Find("OpenMenuKey");
            if (found != null)
            {
                openMenuKeyObject = found.gameObject;
                return;
            }
        }

        Transform rootFound = FindChildRecursive(transform.root, "OpenMenuKey");
        if (rootFound != null)
        {
            openMenuKeyObject = rootFound.gameObject;
            return;
        }
    }

    public void FindGuidePanelIfNull()
    {
        if (guidePanel != null) return;

        if (transform.parent != null)
        {
            Transform found = transform.parent.Find("GuideBook");
            if (found != null)
            {
                guidePanel = found.gameObject;
                return;
            }
        }

        Transform rootFound = FindChildRecursive(transform.root, "GuideBook");
        if (rootFound != null)
        {
            guidePanel = rootFound.gameObject;
            return;
        }
    }

    public void FindDiaryPanelIfNull()
    {
        if (diaryPanel != null) return;

        if (transform.parent != null)
        {
            Transform found = transform.parent.Find("DiaryPanel") ?? transform.parent.Find("MemoryPanel");
            if (found != null)
            {
                diaryPanel = found.gameObject;
                return;
            }
        }

        UICanvas canvas = GetComponentInParent<UICanvas>();
        if (canvas != null)
        {
            Transform found = canvas.transform.Find("DiaryPanel") ?? canvas.transform.Find("MemoryPanel");
            if (found != null)
            {
                diaryPanel = found.gameObject;
                return;
            }
        }

        Transform rootFound = FindChildRecursive(transform.root, "DiaryPanel") ?? FindChildRecursive(transform.root, "MemoryPanel");
        if (rootFound != null)
        {
            diaryPanel = rootFound.gameObject;
            return;
        }

        DiaryManager diaryMgr = Object.FindFirstObjectByType<DiaryManager>(FindObjectsInactive.Include);
        if (diaryMgr != null)
        {
            diaryPanel = diaryMgr.gameObject;
            return;
        }
    }

    public void FindMenuPanelIfNull()
    {
        if (menuPanel != null) return;
        menuPanel = FindPanelRecursive("Menu");
    }

    public void FindSettingsPanelIfNull()
    {
        if (settingsPanel != null) return;
        settingsPanel = FindPanelRecursive("SettingPanel") ?? FindPanelRecursive("SettingsPanel");
    }

    public void FindQuitConfirmationPanelIfNull()
    {
        if (quitConfirmationPanel != null) return;
        quitConfirmationPanel = FindPanelRecursive("QuitConfirmationPanel");
    }

    private GameObject FindPanelRecursive(string panelName)
    {
        if (transform.parent != null)
        {
            Transform found = transform.parent.Find(panelName);
            if (found != null) return found.gameObject;
        }

        UICanvas canvas = GetComponentInParent<UICanvas>();
        if (canvas != null)
        {
            Transform found = canvas.transform.Find(panelName);
            if (found != null) return found.gameObject;
        }

        Transform rootFound = FindChildRecursive(transform.root, panelName);
        if (rootFound != null) return rootFound.gameObject;

        return null;
    }

    public void FindAllPanelsIfNull()
    {
        FindOpenMenuKeyObject();
        FindMenuPanelIfNull();
        FindSettingsPanelIfNull();
        FindQuitConfirmationPanelIfNull();
        FindGuidePanelIfNull();
        FindDiaryPanelIfNull();
    }

    /// <summary>
    /// 메뉴 및 관련 팝업 패널들이 씬 전환 FadeOut(ScreenFadeOutput, sortingOrder 999 등)이나
    /// 다른 상위 캔버스보다 항상 위에 렌더링되고 마우스 클릭/인터랙션이 보장되도록
    /// 각 패널에 독립적인 최상위 Sub-Canvas 및 GraphicRaycaster를 설정합니다.
    /// </summary>
    public void SetupTopmostPanels()
    {
        FindAllPanelsIfNull();

        EnsureTopmostCanvas(menuPanel, 20000);
        EnsureTopmostCanvas(guidePanel, 20100);
        EnsureTopmostCanvas(diaryPanel, 20100);
        EnsureTopmostCanvas(settingsPanel, 20200);
        EnsureTopmostCanvas(quitConfirmationPanel, 20300);
    }

    private void EnsureTopmostCanvas(GameObject panel, int sortingOrder)
    {
        if (panel == null) return;

        Canvas canvas = panel.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = panel.AddComponent<Canvas>();
        }
        if (!canvas.overrideSorting || canvas.sortingOrder != sortingOrder)
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;
        }

        Canvas rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null && canvas.additionalShaderChannels != rootCanvas.additionalShaderChannels)
        {
            canvas.additionalShaderChannels = rootCanvas.additionalShaderChannels;
        }

        GraphicRaycaster raycaster = panel.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            raycaster = panel.AddComponent<GraphicRaycaster>();
        }
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

    /// <summary>
    /// QuitConfirmationPanel 하위의 NoButton을 찾아 패널 닫기 리스너를 코드로 안전하게 바인딩합니다.
    /// 씬 전환 후 인스펙터 참조가 유실되더라도 항상 정상 작동하도록 보장합니다.
    /// </summary>
    public void BindQuitConfirmationButtons()
    {
        if (quitConfirmationPanel != null)
        {
            Button[] buttons = quitConfirmationPanel.GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.gameObject.name.Equals("NoButton", System.StringComparison.OrdinalIgnoreCase))
                {
                    btn.onClick.RemoveListener(OnNoButtonClicked);
                    btn.onClick.AddListener(OnNoButtonClicked);
                }
            }
        }
    }

    private void OnNoButtonClicked()
    {
        if (quitConfirmationPanel != null)
        {
            quitConfirmationPanel.SetActive(false);
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    void Update()
    {
        HandleEscapeKey();

        // 메뉴 패널(menuPanel, settingsPanel, quitConfirmationPanel, guidePanel)의 활성화 여부를 감지하여
        // 일시정지(Time.timeScale) 및 마우스 커서 상태를 동기화합니다.
        bool isOpen = IsAnyMenuPanelActive();
        if (isOpen != _isAnyMenuOpen)
        {
            _isAnyMenuOpen = isOpen;
            OnMenuStateChanged(_isAnyMenuOpen);
        }
    }

    private void HandleEscapeKey()
    {
        // 1. SettingPanel이 열려있는 경우
        if (settingsPanel != null && settingsPanel.activeSelf)
        {
            // OpenMenuKey가 켜져 있다면 비활성화하여 Menu 창 토글 방지
            if (openMenuKeyObject != null && openMenuKeyObject.activeSelf)
            {
                openMenuKeyObject.SetActive(false);
            }

            // ESC 키 입력 시 SettingPanel만 닫음
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                OnClickExitSettings();
                // 닫힌 동일 프레임에 OpenMenuKey가 켜져서 메뉴가 즉시 반응하는 현상 방지
                _ignoreNextFrameOpenMenuKey = true;
            }
        }
        // 2. 종료 확인 팝업이 열려있는 경우
        else if (quitConfirmationPanel != null && quitConfirmationPanel.activeSelf)
        {
            if (openMenuKeyObject != null && openMenuKeyObject.activeSelf)
            {
                openMenuKeyObject.SetActive(false);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                quitConfirmationPanel.SetActive(false);
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
                _ignoreNextFrameOpenMenuKey = true;
            }
        }
        // 3. 가이드북이 열려있는 경우
        else if (guidePanel != null && guidePanel.activeSelf)
        {
            if (openMenuKeyObject != null && openMenuKeyObject.activeSelf)
            {
                openMenuKeyObject.SetActive(false);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                guidePanel.SetActive(false);
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
                _ignoreNextFrameOpenMenuKey = true;
            }
        }
        // 4. 다이어리/회상 창이 열려있는 경우
        else if (diaryPanel != null && diaryPanel.activeSelf)
        {
            if (openMenuKeyObject != null && openMenuKeyObject.activeSelf)
            {
                openMenuKeyObject.SetActive(false);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                DiaryManager diaryManager = diaryPanel.GetComponent<DiaryManager>();
                if (diaryManager != null)
                {
                    diaryManager.OnClickBackToLobby();
                }
                else
                {
                    diaryPanel.SetActive(false);
                }
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
                _ignoreNextFrameOpenMenuKey = true;
            }
        }
        else
        {
            // 5. 팝업창들이 닫혀있는 평상시
            if (_ignoreNextFrameOpenMenuKey)
            {
                // ESC로 방금 닫힌 프레임이 지나갈 때까지 1프레임 대기
                _ignoreNextFrameOpenMenuKey = false;
            }
            else
            {
                // 평상시에는 OpenMenuKey를 활성화하여 기본 ESC 메뉴 열기 동작 유지
                if (openMenuKeyObject != null && !openMenuKeyObject.activeSelf)
                {
                    openMenuKeyObject.SetActive(true);
                }
            }
        }
    }

    public bool IsAnyMenuPanelActive()
    {
        return (menuPanel != null && menuPanel.activeSelf) ||
               (settingsPanel != null && settingsPanel.activeSelf) ||
               (quitConfirmationPanel != null && quitConfirmationPanel.activeSelf) ||
               (guidePanel != null && guidePanel.activeSelf) ||
               (diaryPanel != null && diaryPanel.activeSelf);
    }

    private void OnMenuStateChanged(bool isOpen)
    {
        UpdateCursorState.isMenuOpen = isOpen;

        if (isOpen)
        {
            SetupTopmostPanels();
            if (menuPanel != null && menuPanel.activeSelf)
            {
                menuPanel.transform.SetAsLastSibling();
            }

            // 1. 게임 일시정지
            Time.timeScale = 0f;

            // ★ 사운드 일시정지 신호 전달 (affectedByTimeScale이 켜진 음성 일시정지)
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetGamePauseState(true);
            }

            // 2. 마우스 커서 활성화 및 잠금 해제
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // 1. 게임 재개
            Time.timeScale = 1f;

            // ★ 사운드 재개 신호 전달
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetGamePauseState(false);
            }

            // 2. 씬에 UpdateCursorState가 있는 경우(인게임 씬) 마우스 커서를 다시 잠금
            UpdateCursorState cursorState = Object.FindFirstObjectByType<UpdateCursorState>();
            if (cursorState != null)
            {
                cursorState.SetCursorLock(true);
            }
            else
            {
                // 로비 등 마우스 조작이 필요한 씬
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    //public void OnClickLobby()
    //{
    //    Time.timeScale = 1f;
    //    UpdateCursorState.isMenuOpen = false;

    //    // ★ 로비로 갈 때 일시정지 해제 및 기존 음성 정리
    //    if (SoundManager.Instance != null)
    //    {
    //        SoundManager.Instance.SetGamePauseState(false);
    //        SoundManager.Instance.StopVoice();
    //    }

    //    SceneManager.LoadScene("2__Lobby");
    //}

    public void OnClickLobby()
    {
        Time.timeScale = 1f;
        UpdateCursorState.isMenuOpen = false;

        // ★ 로비 진입 시 마우스 커서가 잠기는 현상을 방지하기 위한 안전장치
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 인게임의 모든 사운드(BGM/환경음/보이스) 즉시 완벽 초기화
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.ResetAllAudioForSceneTransition();
        }

        StartCoroutine(TransitionToLobbyRoutine());
    }

    private System.Collections.IEnumerator TransitionToLobbyRoutine()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.enabled = false;
        }

        // 인게임 에셋 메모리 언로드
        AsyncOperation unloadOp = Resources.UnloadUnusedAssets();
        while (!unloadOp.isDone) yield return null;

        System.GC.Collect();

        // 로비 씬 비동기 로드
        AsyncOperation loadOp = SceneManager.LoadSceneAsync("2__Lobby");
        while (!loadOp.isDone) yield return null;
    }

    // [설정] 버튼
    public void OnClickSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.transform.SetAsLastSibling();
            settingsPanel.SetActive(true);
            EnsureTopmostCanvas(settingsPanel, 20200);
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void OnClickExitSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // 게임 종료 팝업창 안에서 [게임 종료(확인)] 버튼 눌렀을 때 (실제 게임 종료)
    public void ConfirmQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(); 
#endif
    }

    public void OnClickQuitButton()
    {
        if (quitConfirmationPanel != null)
        {
            quitConfirmationPanel.transform.SetAsLastSibling();
            quitConfirmationPanel.SetActive(true);
            EnsureTopmostCanvas(quitConfirmationPanel, 20300);
            BindQuitConfirmationButtons();
        }

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    // [패널 닫기] - 뒤로가기, 닫기창, 또는 종료 팝업 '아니오' 버튼 등
    public void ClosePanel(GameObject targetPanel)
    {
        if (targetPanel != null) targetPanel.SetActive(false);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void OnDisable()
    {
        // 비활성화 시 타임스케일 안전 복원
        if (_isAnyMenuOpen)
        {
            Time.timeScale = 1f;
            UpdateCursorState.isMenuOpen = false;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetGamePauseState(false);
            }
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        UpdateCursorState.isMenuOpen = false;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetGamePauseState(false);
        }
    }
}