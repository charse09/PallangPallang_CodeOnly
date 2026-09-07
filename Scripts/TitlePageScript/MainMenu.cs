using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections; // 코루틴(IEnumerator)을 쓰기 위해 필수!
using TMPro; // TextMeshPro를 제어하기 위해 필수!

public class MainMenu : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("깜빡거릴 Image 등록 창 (선택 사항)")]
    public Image pressAnyKeyText;

    [Tooltip("깜빡거릴 TextMeshPro 등록 창 (선택 사항)")]
    public TextMeshProUGUI pressAnyKeyTMP;

    [Header("Flicker Settings")]
    public float flickering = 1.5f;

    private bool isReadyToStart = false; // 아무 키나 누를 수 있는 상태인지 체크

    void Start()
    {
        // Image나 TextMeshPro 중 하나라도 등록되어 있으면 깜빡임 효과 시작
        if (pressAnyKeyText != null || pressAnyKeyTMP != null)
        {
            StartCoroutine(BlinkUI());
        }
    }

    void Update()
    {
        // 준비 상태일 때 + 사용자가 어떤 키든 누르거나 마우스를 클릭했을 때
        if (isReadyToStart && Input.anyKeyDown)
        {
            PlayGame();
        }
    }

    // 부드럽게 깜빡이는 효과를 만드는 코루틴 함수
    IEnumerator BlinkUI()
    {
        // 처음엔 텍스트가 안 보이게 대기
        yield return new WaitForSeconds(1.0f);

        isReadyToStart = true; // 이제 키 입력을 받을 수 있음

        float currentAlpha = 1.0f;

        while (true)
        {
            // 투명도를 서서히 낮춤 (밝아졌다가 어두워짐)
            while (currentAlpha > 0.1f)
            {
                currentAlpha -= Time.deltaTime * flickering;
                SetAlpha(currentAlpha);
                yield return null;
            }

            // 투명도를 서서히 높임 (어두워졌다가 밝아짐)
            while (currentAlpha < 1.0f)
            {
                currentAlpha += Time.deltaTime * flickering;
                SetAlpha(currentAlpha);
                yield return null;
            }
        }
    }

    private void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        // Image가 등록되어 있다면 Image 투명도 조절
        if (pressAnyKeyText != null)
        {
            Color color = pressAnyKeyText.color;
            color.a = alpha;
            pressAnyKeyText.color = color;
        }

        // TextMeshPro가 등록되어 있다면 TextMeshPro 투명도 조절
        if (pressAnyKeyTMP != null)
        {
            Color color = pressAnyKeyTMP.color;
            color.a = alpha;
            pressAnyKeyTMP.color = color;
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("2__Lobby");
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료!");
        Application.Quit();
    }
}