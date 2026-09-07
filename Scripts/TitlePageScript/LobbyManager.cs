using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LobbyManager : MonoBehaviour
{
    [Header("--- UI Panels ---")]
    public GameObject memoryPanel;             // 회상 UI 패널
    public GameObject settingsPanel;           // 설정 UI 패널
    public GameObject quitConfirmationPanel;   // 게임 종료 확인 팝업 패널

    [Header("--- Main Buttons ---")]
    public TextMeshProUGUI startButtonText;   // 시작 버튼 텍스트
    public string gameStartScene;

    void Awake()
    {
        RefreshPanelReferences();
    }

    void Start()
    {
        RefreshPanelReferences();

        // 시작 시 모든 팝업 패널 닫아둔다
        if (memoryPanel != null) memoryPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (quitConfirmationPanel != null) quitConfirmationPanel.SetActive(false);

        // 세이브 데이터가 있는지 확인
        CheckSaveData();
    }

    /// <summary>
    /// UICanvas가 DontDestroyOnLoad로 관리됨에 따라, 
    /// 씬이 다시 로드될 때 씬 내의 중복 UICanvas가 파괴되면서 인스펙터 참조가 Missing(Destroyed)될 수 있습니다.
    /// 따라서 현재 살아있는 UICanvas / MenuManager에서 유효한 패널 참조를 자동으로 재연결합니다.
    /// </summary>
    public void RefreshPanelReferences()
    {
        // 1. 현재 DontDestroyOnLoad로 살아있는 MenuManager에서 유효한 패널 참조 획득
        MenuManager menuManager = Object.FindFirstObjectByType<MenuManager>(FindObjectsInactive.Include);
        if (menuManager != null)
        {
            if (settingsPanel == null && menuManager.settingsPanel != null)
            {
                settingsPanel = menuManager.settingsPanel;
            }

            if (quitConfirmationPanel == null && menuManager.quitConfirmationPanel != null)
            {
                quitConfirmationPanel = menuManager.quitConfirmationPanel;
            }
        }

        // 2. UICanvas 싱글톤 또는 씬 내 오브젝트에서 추가 검색 (보조)
        if (settingsPanel == null || quitConfirmationPanel == null)
        {
            UICanvas uiCanvas = UICanvas.instance ?? Object.FindFirstObjectByType<UICanvas>(FindObjectsInactive.Include);
            if (uiCanvas != null)
            {
                if (settingsPanel == null)
                {
                    Transform t = FindChildRecursive(uiCanvas.transform, "SettingPanel") ?? 
                                  FindChildRecursive(uiCanvas.transform, "SettingsPanel");
                    if (t != null) settingsPanel = t.gameObject;
                }

                if (quitConfirmationPanel == null)
                {
                    Transform t = FindChildRecursive(uiCanvas.transform, "QuitConfirmationPanel");
                    if (t != null) quitConfirmationPanel = t.gameObject;
                }
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

    void CheckSaveData()
    {
        if (startButtonText == null) return;

        if (PlayerPrefs.GetInt("HasSaveData", 0) == 1)
        {
            startButtonText.text = "이어하기";
        }
        else
        {
            startButtonText.text = "새 게임";
        }
    }

    // [게임 시작 / 이어하기] 버튼
    public void OnClickStart()
    {
        if (!string.IsNullOrEmpty(gameStartScene))
        {
            SceneManager.LoadScene(gameStartScene);
        }
    }

    // [회상 룸] 버튼
    public void OnClickMemory()
    {
        if (memoryPanel == null) RefreshPanelReferences();

        if (memoryPanel != null)
        {
            memoryPanel.SetActive(true);
        }
    }

    // [설정] 버튼
    public void OnClickSettings()
    {
        if (settingsPanel == null) RefreshPanelReferences();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[LobbyManager] settingsPanel 참조를 찾을 수 없습니다.");
        }
    }

    public void OnClickExitSettings()
    {
        if (settingsPanel == null) RefreshPanelReferences();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // 메인 로비 화면에서 [게임 종료] 버튼을 눌렀을 때 (팝업창을 띄움)
    public void OnClickQuitButton()
    {
        if (quitConfirmationPanel == null) RefreshPanelReferences();

        if (quitConfirmationPanel != null)
        {
            quitConfirmationPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[LobbyManager] quitConfirmationPanel 참조를 찾을 수 없습니다.");
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

    // [패널 닫기] - 뒤로가기, 닫기창, 또는 종료 팝업 '아니오' 버튼 등
    public void ClosePanel(GameObject targetPanel)
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(false);
        }
    }
}