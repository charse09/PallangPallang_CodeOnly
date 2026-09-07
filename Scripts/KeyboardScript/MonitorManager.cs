using UnityEngine;
using TMPro;
using System.Collections;

public class MonitorManager : MonoBehaviour
{
    public static MonitorManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI monitorText;

    [Header("Result Images")]
    [Tooltip("비밀번호가 맞았을 때 켤 이미지 오브젝트")]
    public GameObject successImage;
    [Tooltip("비밀번호가 틀렸을 때 켤 이미지 오브젝트")]
    public GameObject failImage;
    [Tooltip("결과 이미지가 화면에 표시될 시간(초)")]
    public float resultDisplayTime = 2f;

    [Header("Password Settings")]
    [Tooltip("정답 비밀번호를 설정하세요.")]
    public string correctPassword = "1234";
    public bool isPasswordMode = true;
    public int maxCharacters = 8;

    private string currentInput = "";
    private bool isProcessingResult = false; // 결과 연출 중 입력 방지용 플래그

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 시작할 때는 결과 이미지들을 모두 꺼둡니다.
        if (successImage != null) successImage.SetActive(false);
        if (failImage != null) failImage.SetActive(false);

        UpdateDisplay();
    }

    public void AppendCharacter(string character)
    {
        if (isProcessingResult || currentInput.Length >= maxCharacters) return;

        currentInput += character;
        UpdateDisplay();
    }

    public void DeleteLastCharacter()
    {
        if (isProcessingResult) return;

        if (currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateDisplay();
        }
    }

    // [새로 추가] 엔터키를 눌렀을 때 비밀번호를 대조하는 함수
    public void CheckPassword()
    {
        if (isProcessingResult || currentInput.Length == 0) return;

        if (currentInput == correctPassword)
        {
            StartCoroutine(SuccessSequence());
        }
        else
        {
            StartCoroutine(FailSequence());
        }
    }

    // 성공 시 시퀀스
    private IEnumerator SuccessSequence()
    {
        isProcessingResult = true;
        Debug.Log("비밀번호 일치! 잠금 해제.");

        if (successImage != null) successImage.SetActive(true);

        yield return new WaitForSeconds(resultDisplayTime);

        // TODO: 여기에 다음 프로세스로 넘어가는 코드를 작성하세요.
        // 예: 다음 방으로 이동, 문 열림 애니메이션 실행, 씬 전환 등
        Debug.Log("다음 프로세스(Stage) 실행!");

        // 만약 통과 후 UI를 꺼야 한다면:
        // if (successImage != null) successImage.SetActive(false);
        isProcessingResult = false;
    }

    // 실패 시 시퀀스
    private IEnumerator FailSequence()
    {
        isProcessingResult = true;
        Debug.Log("비밀번호 불일치! 다시 시도하세요.");

        if (failImage != null) failImage.SetActive(true);

        yield return new WaitForSeconds(resultDisplayTime);

        if (failImage != null) failImage.SetActive(false);

        // 입력창을 초기화하고 다시 입력받을 준비를 합니다.
        currentInput = "";
        UpdateDisplay();

        isProcessingResult = false;
    }

    private void UpdateDisplay()
    {
        if (monitorText == null) return;

        if (currentInput.Length == 0)
        {
            monitorText.text = "";
            return;
        }

        if (isPasswordMode)
        {
            monitorText.text = new string('*', currentInput.Length);
        }
        else
        {
            monitorText.text = currentInput;
        }
    }
}