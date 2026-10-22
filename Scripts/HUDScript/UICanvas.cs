using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// UICanvas를 씬 전환 시에도 파괴되지 않도록(DontDestroyOnLoad) 관리하고,
/// 중복 생성을 방지하는 싱글톤 컴포넌트입니다.
/// 씬 이름 기반 HUD 자동 On/Off 및 전역 HUD 제어 API를 제공합니다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/UI Canvas (Singleton)")]
public class UICanvas : ManagerBase<UICanvas>
{
    [Header("Scene Transition Settings")]
    [Tooltip("씬 전환 시 열려있는 메뉴/설정창 등의 패널을 자동으로 닫을지 여부")]
    [SerializeField] private bool closePanelsOnSceneLoaded = true;

    [Tooltip("씬에 EventSystem이 없을 경우 자동으로 생성하여 UI 입력을 보장할지 여부")]
    [SerializeField] private bool ensureEventSystem = true;

    [Header("HUD Settings")]
    [Tooltip("제어할 HUD 게임오브젝트 (비워둘 시 직계/하위의 'HUD' 이름을 가진 오브젝트를 자동 탐색)")]
    [SerializeField] private GameObject hudObject;

    [Tooltip("씬 전환 시 씬 이름 키워드에 따라 HUD를 자동으로 켜고 끌지 여부")]
    [SerializeField] private bool autoManageHUDByScene = true;

    [Tooltip("HUD를 숨겨야(비활성화해야) 하는 씬 이름 키워드 목록 (부분 일치 지원, 대소문자 무시)")]
    [SerializeField] private List<string> hudHiddenScenes = new List<string>
    {
        "Lobby",
        "Tutorial",
        "Prologue",
        "Title"
    };

    [Header("Tab[Stay] Key Prompt Settings")]
    [Tooltip("Tab[Stay]를 활성화할 씬 키워드 (기본값: RenewTutorialKST_2)")]
    [SerializeField] private string tabStayActiveSceneKeyword = "RenewTutorialKST_2";

    private MenuManager _menuManager;

    protected override void Awake()
    {
        base.Awake();

        // 중복 생성되어 파괴될 인스턴스인 경우 추가 초기화 건너뜀
        if (instance != this) return;

        _menuManager = GetComponentInChildren<MenuManager>(true);
        _menuManager?.SetupTopmostPanels();
        GetHUDObject();
    }

    private void Start()
    {
        if (instance != this) return;

        if (_menuManager == null) _menuManager = GetComponentInChildren<MenuManager>(true);
        _menuManager?.SetupTopmostPanels();

        // 게임 시작 시 현재 씬에 맞춰 HUD 및 Tab[Stay] 상태 최초 적용
        string activeScene = SceneManager.GetActiveScene().name;
        UpdateHUDStateByScene(activeScene);
        UpdateTabStayStateByScene(activeScene);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 중복 인스턴스가 아닌 정식 인스턴스만 씬 로드 처리
        if (instance != this) return;

        // 새 씬 진입 시 타임스케일과 커서 잠금 상태를 정상 복원
        Time.timeScale = 1f;
        UpdateCursorState.isMenuOpen = false;

        if (ensureEventSystem)
        {
            EnsureEventSystemExists();
        }

        if (closePanelsOnSceneLoaded)
        {
            CloseAllPanels();
        }

        if (_menuManager == null) _menuManager = GetComponentInChildren<MenuManager>(true);
        _menuManager?.SetupTopmostPanels();

        // 씬 이름에 따라 HUD 자동 켜기/끄기
        UpdateHUDStateByScene(scene.name);

        // 4__RenewTutorialKST_2 씬부터 Tab[Stay] 활성화 관리
        UpdateTabStayStateByScene(scene.name);
    }

    /// <summary>
    /// HUD 게임오브젝트를 가져옵니다. 비어있다면 자동 탐색합니다.
    /// </summary>
    public GameObject GetHUDObject()
    {
        if (hudObject == null)
        {
            Transform hudTransform = transform.Find("HUD");
            if (hudTransform != null)
            {
                hudObject = hudTransform.gameObject;
            }
            else
            {
                Transform found = FindChildRecursive(transform, "HUD");
                if (found != null) hudObject = found.gameObject;
            }
        }
        return hudObject;
    }

    /// <summary>
    /// 현재 씬의 이름에 맞춰 HUD 활성화 여부를 자동 갱신합니다.
    /// </summary>
    public void UpdateHUDStateByScene(string sceneName)
    {
        if (!autoManageHUDByScene) return;

        bool shouldHide = IsSceneMatchingKeyword(sceneName, hudHiddenScenes);
        SetHUDActive(!shouldHide);
    }

    private bool IsSceneMatchingKeyword(string sceneName, List<string> keywords)
    {
        if (string.IsNullOrEmpty(sceneName) || keywords == null) return false;

        foreach (var keyword in keywords)
        {
            if (string.IsNullOrEmpty(keyword)) continue;
            // 씬 이름에 키워드가 포함되어 있는지 확인 (대소문자 무시)
            if (sceneName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 인스턴스 단위로 HUD 오브젝트를 켜거나 끕니다.
    /// </summary>
    public void SetHUDActive(bool active)
    {
        GameObject hud = GetHUDObject();
        if (hud != null)
        {
            if (hud.activeSelf != active)
            {
                hud.SetActive(active);
                Debug.Log($"[UICanvas] HUD 상태 변경: {(active ? "켜짐(ON)" : "꺼짐(OFF)")} (씬: {SceneManager.GetActiveScene().name})");
            }
        }
        else
        {
            Debug.LogWarning("[UICanvas] 'HUD' 게임오브젝트를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 씬 내의 어떤 스크립트에서도 한 줄로 HUD를 켜고 끌 수 있는 정적 메서드입니다.
    /// 예: UICanvas.SetHUD(true); 또는 UICanvas.SetHUD(false);
    /// </summary>
    public static void SetHUD(bool active)
    {
        if (instance != null)
        {
            instance.SetHUDActive(active);
        }
        else
        {
            UICanvas found = Object.FindFirstObjectByType<UICanvas>(FindObjectsInactive.Include);
            if (found != null)
            {
                found.SetHUDActive(active);
            }
            else
            {
                Debug.LogWarning("[UICanvas] 씬에 UICanvas 인스턴스가 존재하지 않아 HUD를 제어할 수 없습니다.");
            }
        }
    }

    /// <summary>
    /// 씬 이름에 맞춰 Tab[Stay] 활성화 여부를 관리합니다.
    /// 4__RenewTutorialKST_2 씬(또는 키워드 포함 씬) 진입 시 활성화되며,
    /// Lobby나 Tutorial 1로 돌아갔을 때는 비활성화합니다.
    /// </summary>
    public void UpdateTabStayStateByScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        // Lobby나 Tutorial 1로 돌아갔을 때는 비활성화
        if (sceneName.IndexOf("Lobby", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            sceneName.IndexOf("RenewTutorialKST_1", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            SetTabStayActive(false);
            return;
        }

        // 4__RenewTutorialKST_2 씬부터 활성화
        if (!string.IsNullOrEmpty(tabStayActiveSceneKeyword) &&
            sceneName.IndexOf(tabStayActiveSceneKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            SetTabStayActive(true);
        }
    }

    /// <summary>
    /// HUD 하위의 Tab[Stay] 오브젝트를 활성화하거나 비활성화합니다.
    /// </summary>
    public void SetTabStayActive(bool active)
    {
        GameObject hud = GetHUDObject();
        if (hud == null) return;

        Transform tabStay = hud.transform.Find("Tab[Stay]");
        if (tabStay == null)
        {
            tabStay = FindChildRecursive(hud.transform, "Tab[Stay]");
        }

        if (tabStay != null)
        {
            if (tabStay.gameObject.activeSelf != active)
            {
                tabStay.gameObject.SetActive(active);
                Debug.Log($"[UICanvas] Tab[Stay] {(active ? "활성화(ON)" : "비활성화(OFF)")} (씬: {SceneManager.GetActiveScene().name})");
            }
        }
    }

    /// <summary>
    /// 어디서든 한 줄로 Tab[Stay]를 켜고 끌 수 있는 정적 메서드
    /// 예: UICanvas.SetTabStay(true); 또는 UICanvas.SetTabStay(false);
    /// </summary>
    public static void SetTabStay(bool active)
    {
        if (instance != null)
        {
            instance.SetTabStayActive(active);
        }
        else
        {
            UICanvas found = Object.FindFirstObjectByType<UICanvas>(FindObjectsInactive.Include);
            if (found != null)
            {
                found.SetTabStayActive(active);
            }
            else
            {
                Debug.LogWarning("[UICanvas] 씬에 UICanvas 인스턴스가 존재하지 않아 Tab[Stay]를 제어할 수 없습니다.");
            }
        }
    }

    /// <summary>
    /// 씬에 EventSystem이 없으면 생성하여 UI 클릭 및 내비게이션 입력을 보장합니다.
    /// </summary>
    private void EnsureEventSystemExists()
    {
        if (EventSystem.current == null)
        {
            EventSystem existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem (AutoGenerated)");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
                Debug.Log("[UICanvas] 씬에 EventSystem이 없어 자동으로 생성했습니다.");
            }
        }
    }

    /// <summary>
    /// 열려있는 메뉴, 설정창, 확인 팝업 패널을 모두 비활성화합니다.
    /// </summary>
    public void CloseAllPanels()
    {
        if (_menuManager == null)
        {
            _menuManager = GetComponentInChildren<MenuManager>(true);
        }

        if (_menuManager != null)
        {
            if (_menuManager.settingsPanel != null && _menuManager.settingsPanel.activeSelf)
            {
                _menuManager.settingsPanel.SetActive(false);
            }

            if (_menuManager.menuPanel != null && _menuManager.menuPanel.activeSelf)
            {
                _menuManager.menuPanel.SetActive(false);
            }

            if (_menuManager.quitConfirmationPanel != null && _menuManager.quitConfirmationPanel.activeSelf)
            {
                _menuManager.quitConfirmationPanel.SetActive(false);
            }

            if (_menuManager.guidePanel != null && _menuManager.guidePanel.activeSelf)
            {
                _menuManager.guidePanel.SetActive(false);
            }

            if (_menuManager.diaryPanel != null && _menuManager.diaryPanel.activeSelf)
            {
                _menuManager.diaryPanel.SetActive(false);
            }
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
}