using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingManager : MonoBehaviour
{
    [Header("--- UI Panels ---")]
               // 설정 UI 패널
    public GameObject displayPanel;
    public GameObject soundPanel;
    public GameObject inputPanel;
    public GameObject languageDataPanel;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 시작 시 모든 팝업 패널은 닫아둔다
        //if (displayPanel != null) displayPanel.SetActive(false);
        if (soundPanel != null) soundPanel.SetActive(false);
        if (inputPanel != null) inputPanel.SetActive(false);
        if (languageDataPanel != null) languageDataPanel.SetActive(false);

    }
    public void OnClickDisplay()
    {
        displayPanel.SetActive(true);
        soundPanel.SetActive(false);
        inputPanel.SetActive(false);
        languageDataPanel.SetActive(false);
    }

    public void OnClickSound()
    {
        displayPanel.SetActive(false);
        soundPanel.SetActive(true);
        inputPanel.SetActive(false);
        languageDataPanel.SetActive(false);
    }

    public void OnClickInput()
    {
        displayPanel.SetActive(false);
        soundPanel.SetActive(false);
        inputPanel.SetActive(true);
        languageDataPanel.SetActive(false);
    }

    public void OnClickLanguageData()
    {
        displayPanel.SetActive(false);
        soundPanel.SetActive(false);
        inputPanel.SetActive(false);
        languageDataPanel.SetActive(true);
    }
}
