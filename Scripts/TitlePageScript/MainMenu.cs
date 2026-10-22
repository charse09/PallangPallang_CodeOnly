using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using System.Collections;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("깜빡거릴 Image 등록 창 (선택 사항)")]
    public Image pressAnyKeyText;

    [Tooltip("깜빡거릴 TextMeshPro 등록 창 (선택 사항)")]
    public TextMeshProUGUI pressAnyKeyTMP;

    [Header("Flicker Settings")]
    public float flickering = 1.5f;

    [Header("Transition Video Settings")]
    [Tooltip("다음으로 전환할 씬 이름")]
    public string nextSceneName = "2__Lobby";

    [Tooltip("씬에 존재하는 VideoPlayer (비워둘 경우 자동 탐색)")]
    public VideoPlayer videoPlayer;

    [Tooltip("클릭/키 입력 시 재생할 전환 비디오 클립 (예: 로고 접기.mp4)")]
    public VideoClip transitionVideoClip;

    [Tooltip("비디오 재생 시작 시 함께 숨길 추가 오브젝트들")]
    public GameObject[] hideOnTransition;

    [Tooltip("비디오 재생 중 다시 클릭하면 즉시 스킵할지 여부")]
    public bool allowSkip = false;

    private bool isReadyToStart = false; // 아무 키나 누를 수 있는 상태인지 체크
    private bool isTransitioning = false; // 비디오 재생 및 씬 전환 진행 여부
    private Coroutine blinkCoroutine;

    void Start()
    {
        // VideoPlayer가 인스펙터에 지정되지 않은 경우 씬에서 자동 탐색
        if (videoPlayer == null)
        {
            videoPlayer = FindFirstObjectByType<VideoPlayer>();
        }

        // Image나 TextMeshPro 중 하나라도 등록되어 있으면 깜빡임 효과 시작
        if (pressAnyKeyText != null || pressAnyKeyTMP != null)
        {
            blinkCoroutine = StartCoroutine(BlinkUI());
        }
        else
        {
            isReadyToStart = true;
        }
    }

    void Update()
    {
        if (isTransitioning)
        {
            // 스킵 허용 시 추가 입력이 들어오면 바로 다음 씬으로 전환
            if (allowSkip && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
            {
                LoadNextScene();
            }
            return;
        }

        // 준비 상태일 때 + 사용자가 어떤 키든 누르거나 마우스를 클릭했을 때
        if (isReadyToStart && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
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
        if (isTransitioning) return;
        isTransitioning = true;
        isReadyToStart = false;

        // 깜빡임 코루틴 중지 및 UI 숨김 처리
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }

        HideUIElements();

        // 비디오 클립과 플레이어가 준비되어 있다면 비디오 재생 후 씬 전환
        if (videoPlayer != null && transitionVideoClip != null)
        {
            StartCoroutine(PlayTransitionVideoRoutine());
        }
        else
        {
            LoadNextScene();
        }
    }

    private void HideUIElements()
    {
        if (pressAnyKeyText != null) pressAnyKeyText.gameObject.SetActive(false);
        if (pressAnyKeyTMP != null) pressAnyKeyTMP.gameObject.SetActive(false);

        if (hideOnTransition != null)
        {
            foreach (var obj in hideOnTransition)
            {
                if (obj != null) obj.SetActive(false);
            }
        }
    }

    private IEnumerator PlayTransitionVideoRoutine()
    {
        videoPlayer.isLooping = false;
        videoPlayer.clip = transitionVideoClip;
        videoPlayer.Prepare();

        // 비디오 버퍼링/디코딩 준비 대기 (최대 2초 타임아웃)
        float prepareTimeout = 2.0f;
        float elapsedPrepare = 0f;
        while (!videoPlayer.isPrepared && elapsedPrepare < prepareTimeout)
        {
            elapsedPrepare += Time.deltaTime;
            yield return null;
        }

        videoPlayer.Play();

        // 비디오 끝 도달 여부 체크
        bool isFinished = false;
        void OnVideoFinished(VideoPlayer vp)
        {
            isFinished = true;
        }

        videoPlayer.loopPointReached += OnVideoFinished;

        // 비디오 길이 기반 타임아웃 계산 (영상 종료 이벤트 누락 방지 안전장치)
        float duration = (float)videoPlayer.length;
        if (duration <= 0f && transitionVideoClip != null)
        {
            duration = (float)transitionVideoClip.length;
        }
        float timeout = Mathf.Max(duration, 0.5f) + 1.0f;
        float elapsed = 0f;

        while (!isFinished && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        videoPlayer.loopPointReached -= OnVideoFinished;
        LoadNextScene();
    }

    private void LoadNextScene()
    {
        string sceneToLoad = string.IsNullOrEmpty(nextSceneName) ? "2__Lobby" : nextSceneName;
        SceneManager.LoadScene(sceneToLoad);
    }

    public void QuitGame()
    {
        Debug.Log("게임 종료!");
        Application.Quit();
    }
}