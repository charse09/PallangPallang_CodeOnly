using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// Canvas의 UI Image 알파(투명도) 값을 지정한 시간과 커브에 따라 점진적으로 변화시키는 Output 노드입니다.
    /// 페이드 인/아웃, 화면 암전, 배경 어둡게 깔기 연출 등에 사용됩니다.
    /// </summary>
    public class FadeImageOutput : ProcessBase
    {
        [Header("1. Target Reference")]
        [Tooltip("투명도를 변경할 UI Image (미할당 시 본인 오브젝트에서 탐색)")]
        [SerializeField] private Image targetImage;

        [Header("2. Fade Settings")]
        [Tooltip("목표 알파값 (0.0 = 완전 투명, 1.0 = 완전 불투명 검정)")]
        [Range(0f, 1f)]
        [SerializeField] private float targetAlpha = 1.0f;

        [Tooltip("알파값 변화에 걸리는 총 소요 시간 (초)")]
        [Min(0f)]
        [SerializeField] private float duration = 1.5f;

        [Tooltip("페이드 진행 곡선 (EaseInOut 기본 추천)")]
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("3. Timing & Optimization")]
        [Tooltip("체크 시 Time.timeScale의 영향을 받지 않는 UnscaledTime을 사용하여 게임 일시정지 중에도 페이드가 작동합니다.")]
        [SerializeField] private bool useUnscaledTime = true;

        [Tooltip("페이드 시작 시 대상 게임오브젝트를 자동으로 켤지 여부")]
        [SerializeField] private bool activateOnStart = true;

        [Tooltip("목표 알파가 0(완전 투명)으로 끝났을 때, 마우스 클릭 씹힘 방지 및 최적화를 위해 오브젝트를 자동으로 비활성화(SetActive false)할지 여부")]
        [SerializeField] private bool deactivateWhenFullyTransparent = true;

        private Coroutine _fadeCoroutine;

        private void Awake()
        {
            if (targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }

            IsOn = false;
        }

        public override void Reset()
        {
            base.Reset();

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            IsOn = false;
        }

        public override void Execute()
        {
            if (targetImage == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Target Image가 비어 있어 FadeImageOutput을 실행할 수 없습니다.");
                IsOn = true;
                return;
            }

            IsOn = false;

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(FadeRoutine());
        }

        private IEnumerator FadeRoutine()
        {
            // 1. 오브젝트 활성화 처리
            if (activateOnStart && !targetImage.gameObject.activeSelf)
            {
                targetImage.gameObject.SetActive(true);
            }

            Color currentColor = targetImage.color;
            float startAlpha = currentColor.a;

            // 즉시 전환인 경우 (duration이 0일 때)
            if (duration <= 0f)
            {
                currentColor.a = targetAlpha;
                targetImage.color = currentColor;

                CheckDeactivateCondition();
                _fadeCoroutine = null;
                IsOn = true;
                yield break;
            }

            float elapsed = 0f;

            // 2. 점진적 알파 보간 진행
            while (elapsed < duration)
            {
                float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += deltaTime;

                float progress = Mathf.Clamp01(elapsed / duration);
                float curvedT = fadeCurve.Evaluate(progress);

                currentColor.a = Mathf.Lerp(startAlpha, targetAlpha, curvedT);
                targetImage.color = currentColor;

                yield return null;
            }

            // 3. 최종 목표값 정확히 반영
            currentColor.a = targetAlpha;
            targetImage.color = currentColor;

            // 4. 투명 상태 최적화 및 완료 신호 전달
            CheckDeactivateCondition();

            _fadeCoroutine = null;
            IsOn = true;
        }

        private void CheckDeactivateCondition()
        {
            if (deactivateWhenFullyTransparent && Mathf.Approximately(targetAlpha, 0f))
            {
                targetImage.gameObject.SetActive(false);
            }
        }
    }
}