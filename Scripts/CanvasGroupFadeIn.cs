using System.Collections;
using UnityEngine;

/// <summary>
/// 오브젝트가 활성화(OnEnable)될 때 CanvasGroup의 Alpha를 서서히 올리고,
/// UpdateCursorState와 연동하여 마우스 커서를 자동으로 표시/해제하는 컴포넌트입니다.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CanvasGroupFadeIn : MonoBehaviour
{
    [Header("--- Cursor Settings ---")]
    [Tooltip("오브젝트 활성화 시 UpdateCursorState에 마우스 커서 표시를 요청할지 여부")]
    [SerializeField] private bool showCursorOnEnable = true;

    [Header("--- Fade Settings ---")]
    [Tooltip("시작할 때의 알파값 (보통 0)")]
    [Range(0f, 1f)]
    [SerializeField] private float startAlpha = 0f;

    [Tooltip("페이드인 완료 시 최종 알파값 (보통 1)")]
    [Range(0f, 1f)]
    [SerializeField] private float targetAlpha = 1f;

    [Tooltip("페이드인에 걸리는 시간 (초)")]
    [SerializeField] private float duration = 0.5f;

    [Tooltip("페이드 진행 곡선 (부드러운 EaseInOut 기본 제공)")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Tooltip("일시정지(Time.timeScale = 0) 상태인 메뉴 창에서도 페이드가 정상 작동할지 여부")]
    [SerializeField] private bool ignoreTimeScale = true;

    [Header("--- Interaction Settings ---")]
    [Tooltip("체크 시 페이드인이 완전히 끝난 후에만 버튼 클릭(터치)을 허용합니다")]
    [SerializeField] private bool interactableOnlyAfterFade = false;

    private CanvasGroup _canvasGroup;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        // 1. UpdateCursorState에 커서 잠금 해제 및 표시 요청
        if (showCursorOnEnable)
        {
            UpdateCursorState.RequestCursor(this);
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        // 2. 켜지는 즉시 시작 알파값으로 초기화
        _canvasGroup.alpha = startAlpha;

        // 3. 페이드인 도중 클릭 허용 여부 설정
        if (interactableOnlyAfterFade)
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
        else
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        // 4. 이전 실행 중이던 코루틴이 있다면 중지하고 새로 시작
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }
        _fadeCoroutine = StartCoroutine(FadeInRoutine());
    }

    private void OnDisable()
    {
        // UI가 닫힐 때 커서 요청 해제 (다른 UI가 켜져 있지 않다면 다시 인게임 커서로 복귀)
        if (showCursorOnEnable)
        {
            UpdateCursorState.ReleaseCursor(this);
        }

        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        // 오브젝트 파괴 시에도 커서 요청이 남아있지 않도록 안전하게 정리
        if (showCursorOnEnable)
        {
            UpdateCursorState.ReleaseCursor(this);
        }
    }

    private IEnumerator FadeInRoutine()
    {
        if (duration <= 0f)
        {
            _canvasGroup.alpha = targetAlpha;
            OnFadeCompleted();
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += ignoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = fadeCurve != null ? fadeCurve.Evaluate(t) : t;

            _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, curveT);
            yield return null;
        }

        _canvasGroup.alpha = targetAlpha;
        OnFadeCompleted();
        _fadeCoroutine = null;
    }

    private void OnFadeCompleted()
    {
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
    }
}