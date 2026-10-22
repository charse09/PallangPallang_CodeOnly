using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// 외부 매니저 스크립트 없이 단독으로 동작하는 버튼 컴포넌트입니다.
/// 버튼 포커스(호버/선택) 동기화 및 게임 종료(Quit) 또는 타이틀 씬 이동을 자체적으로 처리합니다.
/// </summary>
[RequireComponent(typeof(Button))]
public class QuitButtonHandler : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [Header("--- Focus (Hover / Select) Settings ---")]
    [Tooltip("마우스 호버 시 유니티 EventSystem의 선택 상태로 자동 동기화할지 여부")]
    [SerializeField] private bool syncEventSystemSelection = true;

    [Tooltip("포커스(호버/선택) 시 재생할 효과음 (선택 사항 - 없으면 비워둠)")]
    [SerializeField] private AudioClip focusSound;

    [Tooltip("포커스 시 실행할 추가 동작이 있다면 인스펙터에서 연결 (선택 사항)")]
    [SerializeField] private UnityEvent onFocused;

    [Header("--- Button Action & Scene Settings ---")]
    [Tooltip("체크 시 Awake에서 게임 종료(ConfirmQuit) 함수를 버튼에 자동 등록합니다. (타이틀 이동 버튼으로 쓸 때는 체크 해제 가능)")]
    [SerializeField] private bool autoBindQuitOnAwake = true;

    [Tooltip("돌아갈 타이틀 씬의 이름 (프로젝트의 실제 타이틀 씬 이름과 일치시켜 주세요)")]
    [SerializeField] private string titleSceneName = "1__Title";

    private Button _button;

    private void Awake()
    {
        // 1. Button 컴포넌트 가져오기
        _button = GetComponent<Button>();

        // 2. 클릭 시 실행될 게임 종료 함수를 코드로 자동 등록 (인스펙터 연결 불필요)
        if (_button != null && autoBindQuitOnAwake)
        {
            _button.onClick.RemoveListener(ConfirmQuit);
            _button.onClick.AddListener(ConfirmQuit);
        }
    }

    /// <summary>
    /// 버튼에 포커스(호버/선택)가 들어왔을 때 실행되는 핵심 함수
    /// </summary>
    public void OnButtonFocused()
    {
        // 1. EventSystem의 현재 선택된 오브젝트를 이 버튼으로 동기화 (키보드/패드 내비게이션 연동)
        if (syncEventSystemSelection && EventSystem.current != null)
        {
            if (EventSystem.current.currentSelectedGameObject != gameObject)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }

        // 2. 포커스 사운드가 등록되어 있다면 재생
        if (focusSound != null)
        {
            AudioSource.PlayClipAtPoint(focusSound, Camera.main != null ? Camera.main.transform.position : transform.position);
        }

        // 3. 인스펙터에 지정된 추가 UnityEvent가 있다면 실행
        onFocused?.Invoke();
    }

    /// <summary>
    /// Event Trigger 컴포넌트에서 Dynamic 이벤트로 연결할 때를 위한 오버로드
    /// </summary>
    public void OnButtonFocused(BaseEventData eventData)
    {
        OnButtonFocused();
    }

    // =========================================================================
    // Unity EventSystems 인터페이스
    // (인스펙터의 Event Trigger 컴포넌트를 붙이지 않아도 마우스/패드 포커스를 자동 감지합니다)
    // =========================================================================

    public void OnPointerEnter(PointerEventData eventData)
    {
        OnButtonFocused();
    }

    public void OnSelect(BaseEventData eventData)
    {
        OnButtonFocused();
    }

    // =========================================================================
    // 씬 전환 및 게임 종료 기능
    // =========================================================================

    /// <summary>
    /// 타이틀 씬으로 돌아가는 함수 (버튼 OnClick 이벤트 등에 연결하여 사용)
    /// </summary>
    public void ReturnToTitle()
    {
        Debug.Log($"[QuitButtonHandler] 타이틀 씬({titleSceneName})으로 돌아갑니다.");

        Time.timeScale = 1f;
        UpdateCursorState.isMenuOpen = false;

        // ★ 타이틀 진입 시 마우스 커서가 잠기는 현상을 방지하기 위한 안전장치
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 인게임의 모든 사운드(BGM/환경음/보이스) 즉시 완벽 초기화
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.ResetAllAudioForSceneTransition();
        }

        StartCoroutine(TransitionToTitleRoutine());
    }

    private IEnumerator TransitionToTitleRoutine()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.enabled = false;
        }

        // 인게임 에셋 메모리 언로드
        AsyncOperation unloadOp = Resources.UnloadUnusedAssets();
        while (!unloadOp.isDone) yield return null;

        System.GC.Collect();

        // 타이틀 씬 비동기 로드
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(titleSceneName);
        while (!loadOp.isDone) yield return null;
    }

    /// <summary>
    /// 버튼 클릭 시 실행되는 실제 게임 종료 함수
    /// </summary>
    public void ConfirmQuit()
    {
        Debug.Log("[QuitButtonHandler] 게임을 종료합니다.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 에디터 플레이 모드 정지
#else
        Application.Quit(); // 빌드된 실제 게임 종료
#endif
    }
}