using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("--- UI Panels ---")]
    public GameObject settingsPanel;           // 설정 UI 패널
    public GameObject menuPanel;
    public GameObject quitConfirmationPanel;   // 게임 종료 확인 팝업 패널

    private bool _isAnyMenuOpen = false;

    void Awake()
    {
        BindQuitConfirmationButtons();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 시작 시 모든 팝업 패널 닫아둔다
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (menuPanel != null) menuPanel.SetActive(false);
        if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);

        BindQuitConfirmationButtons();
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
    }

    void Update()
    {
        // 메뉴 패널(menuPanel, settingsPanel, quitConfirmationPanel)의 활성화 여부를 감지하여
        // 일시정지(Time.timeScale) 및 마우스 커서 상태를 동기화합니다.
        bool isOpen = IsAnyMenuPanelActive();
        if (isOpen != _isAnyMenuOpen)
        {
            _isAnyMenuOpen = isOpen;
            OnMenuStateChanged(_isAnyMenuOpen);
        }
    }

    public bool IsAnyMenuPanelActive()
    {
        return (menuPanel != null && menuPanel.activeSelf) ||
               (settingsPanel != null && settingsPanel.activeSelf) ||
               (quitConfirmationPanel != null && quitConfirmationPanel.activeSelf);
    }

    private void OnMenuStateChanged(bool isOpen)
    {
        UpdateCursorState.isMenuOpen = isOpen;

        if (isOpen)
        {
            // 1. 게임 일시정지
            Time.timeScale = 0f;

            // 2. 마우스 커서 활성화 및 잠금 해제
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // 1. 게임 재개
            Time.timeScale = 1f;

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

    public void OnClickLobby()
    {
        Time.timeScale = 1f;
        UpdateCursorState.isMenuOpen = false;
        SceneManager.LoadScene("2__Lobby");
    }

    // [설정] 버튼
    public void OnClickSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void OnClickExitSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
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
            quitConfirmationPanel.SetActive(true);
            BindQuitConfirmationButtons();
        }
    }

    // [패널 닫기] - 뒤로가기, 닫기창, 또는 종료 팝업 '아니오' 버튼 등
    public void ClosePanel(GameObject targetPanel)
    {
        if (targetPanel != null) targetPanel.SetActive(false);
    }

    private void OnDisable()
    {
        // 비활성화 시 타임스케일 안전 복원
        if (_isAnyMenuOpen)
        {
            Time.timeScale = 1f;
            UpdateCursorState.isMenuOpen = false;
        }
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        UpdateCursorState.isMenuOpen = false;
    }
}