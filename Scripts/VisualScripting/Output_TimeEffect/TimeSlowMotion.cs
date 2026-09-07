using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class TimeSlowMotion : ProcessBase
    {
        [Header("슬로우 모션 설정")]
        [Range(0.01f, 1f)]
        [Tooltip("변경할 시간 배속입니다. (0.5 = 절반 속도)")]
        [SerializeField] private float targetTimeScale = 0.3f;

        [Tooltip("슬로우 모션이 유지될 시간입니다. (현실 시간 기준, 초 단위)")]
        [SerializeField] private float duration = 2.0f;

        [Header("옵션")]
        [Tooltip("체크하면 물리 연산 주기도 타임스케일에 맞춰 조정합니다. (권장)")]
        [SerializeField] private bool adjustFixedDeltaTime = true;
        [Tooltip("체크하면 불릿타임도중 카메라셰이크 시, 타임스케일의 영향을 받지 않습니다.")]
        [SerializeField] private bool IgnoreTimeScaleValue = true;

        // 중복 실행 방지용 코루틴 변수
        private Coroutine slowMoCoroutine;

        private void Awake()
        {
            if (Unity.Cinemachine.CinemachineImpulseManager.Instance != null)
            {
                Unity.Cinemachine.CinemachineImpulseManager.Instance.IgnoreTimeScale = IgnoreTimeScaleValue;
            }
        }

        public override void Execute()
        {
            // 만약 이미 슬로우 모션 중인데 트리거가 또 발동했다면, 
            // 기존 타이머를 취소하고 처음부터 다시 시간을 잽니다.
            if (slowMoCoroutine != null)
            {
                StopCoroutine(slowMoCoroutine);
            }

            // 1. 타임스케일 및 물리 연산 시간 변경 (슬로우 모션 진입)
            Time.timeScale = targetTimeScale;
            if (adjustFixedDeltaTime)
            {
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            }

            Debug.Log($"<color=orange>[Output]</color> 슬로우 모션 시작: {targetTimeScale}배속 (유지: {duration}초)");

            // 2. 타이머 코루틴 시작
            slowMoCoroutine = StartCoroutine(SlowMotionRoutine());
        }

        private IEnumerator SlowMotionRoutine()
        {
            // 핵심: 타임스케일의 영향을 받지 않는 '현실 시간' 기준으로 기다립니다!
            yield return new WaitForSecondsRealtime(duration);

            // 3. 시간이 다 되면 원래 속도로 복구
            ResetTimeScale();
        }

        /// <summary>
        /// 안전하게 타임스케일을 정상 속도로 복구하는 메서드
        /// </summary>
        private void ResetTimeScale()
        {
            if (slowMoCoroutine != null)
            {
                StopCoroutine(slowMoCoroutine);
                slowMoCoroutine = null;
            }

            Time.timeScale = 1f;
            if (adjustFixedDeltaTime)
            {
                Time.fixedDeltaTime = 0.02f; // 유니티 기본 물리 연산 주기
            }

            Debug.Log("<color=orange>[Output]</color> 슬로우 모션 종료. 정상 속도로 복구되었습니다.");
        }

        // ★ [버그 방지] 오브젝트가 비활성화되거나 파괴될 때 강제로 타임스케일 복구
        private void OnDisable()
        {
            if (slowMoCoroutine != null)
            {
                ResetTimeScale();
            }
        }

        private void OnDestroy()
        {
            if (slowMoCoroutine != null)
            {
                ResetTimeScale();
            }
        }

        public new void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}