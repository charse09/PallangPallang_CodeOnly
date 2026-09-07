using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class GameTimeAdject : ProcessBase
    {
        [Header("Time Settings")]
        [Tooltip("목표 시간 속도 (0 = 정지, 1 = 정상, 0.5 = 슬로우)")]
        [SerializeField, Range(0f, 5f)] private float targetScale = 0f;

        [Tooltip("유지 시간\n-1: 무한 (단, 속도가 0일 경우 안전장치 작동)\n양수: 해당 시간 유지")]
        [SerializeField] private float duration = -1f;

        [Tooltip("속도가 0(정지)이면서 무한(-1)일 때 적용할 기본 안전 시간")]
        [SerializeField] private float safetyDuration = 3.0f;

        [Header("Physics Option")]
        [Tooltip("FixedDeltaTime 동기화")]
        [SerializeField] private bool syncFixedDeltaTime = true;

        private float _defaultFixedDeltaTime;
        private Coroutine _activeCoroutine;

        private void Awake()
        {
            _defaultFixedDeltaTime = Time.fixedDeltaTime;
        }

        public override void Execute()
        {
            // 0 입력 시 안전장치 (아무것도 안 함)
            if (duration == 0f) return;

            // 기존 코루틴 정리
            if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);

            // 로직 분기 판단
            bool isStop = Mathf.Approximately(targetScale, 0f); // 부동소수점 비교
            bool isInfinite = Mathf.Approximately(duration, -1f);

            // 1. [예외 케이스] 무한(-1)인데 정지(0)인 경우 -> 안전장치 타이머 발동
            if (isInfinite && isStop)
            {
                Debug.LogWarning($"[GameTimeAdject] 무한 정지는 위험하므로 안전 시간({safetyDuration}초)만큼만 정지합니다.");
                _activeCoroutine = StartCoroutine(Co_TemporaryTimeChange(safetyDuration));
                IsOn = true;
            }
            // 2. [무한 모드] 정지가 아닌 경우 -> 영구 변경 (타이머 X)
            else if (isInfinite)
            {
                ApplyTimeScale(targetScale);
                IsOn = true;
            }
            // 3. [일반 타이머 모드] 양수 시간 입력 시
            else
            {
                _activeCoroutine = StartCoroutine(Co_TemporaryTimeChange(duration));
                IsOn = true;
            }
        }

        private IEnumerator Co_TemporaryTimeChange(float waitTime)
        {
            float prevScale = Time.timeScale;
            ApplyTimeScale(targetScale);

            // 무조건 Realtime을 써야 정지 상태에서도 시간이 흐름
            yield return new WaitForSecondsRealtime(waitTime);

            ApplyTimeScale(prevScale);
            _activeCoroutine = null;
        }

        private void ApplyTimeScale(float scale)
        {
            Time.timeScale = Mathf.Max(0f, scale);
            if (syncFixedDeltaTime)
            {
                Time.fixedDeltaTime = _defaultFixedDeltaTime * Time.timeScale;
            }
        }
    }
}