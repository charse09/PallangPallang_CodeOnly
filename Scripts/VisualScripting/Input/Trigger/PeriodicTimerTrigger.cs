using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 시간(N초)마다 주기적으로 IsOn 상태를 발동(Pulse)시키는 반복 트리거
    /// </summary>
    public class PeriodicTimerTrigger : ProcessBase
    {
        [Header("Periodic Settings")]
        [Tooltip("반복 주기 (단위: 초)")]
        [SerializeField] private float intervalSeconds = 2.0f;

        [Tooltip("오브젝트가 켜지면(OnEnable) 자동으로 반복을 시작할지 여부")]
        [SerializeField] private bool autoStart = true;

        private Coroutine periodicCoroutine;

        private void OnEnable()
        {
            if (autoStart)
            {
                StartTimer();
            }
        }

        private void OnDisable()
        {
            // 오브젝트가 꺼지면 메모리 누수 방지를 위해 코루틴 강제 정지
            StopTimer();
        }

        /// <summary>
        /// 반복 타이머 시작
        /// </summary>
        public void StartTimer()
        {
            if (periodicCoroutine == null)
            {
                periodicCoroutine = StartCoroutine(TimerRoutine());
            }
        }

        /// <summary>
        /// 반복 타이머 정지
        /// </summary>
        public void StopTimer()
        {
            if (periodicCoroutine != null)
            {
                StopCoroutine(periodicCoroutine);
                periodicCoroutine = null;
                Reset(); // ProcessBase의 상태 초기화 (IsOn = false)
            }
        }

        private IEnumerator TimerRoutine()
        {
            while (true)
            {
                // N초 대기
                yield return new WaitForSeconds(intervalSeconds);

                // 1. 상태를 true로 변경하여 연결된 Logic/Output 노드들이 작동하게 함
                IsOn = true;

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[{gameObject.name}]</color> {intervalSeconds}초 반복 주기 발동!");
#endif

                // 2. 프레임워크가 IsOn = true 상태를 감지할 수 있도록 딱 한 프레임만 대기
                yield return null;

                // 3. 다음 주기에 다시 이벤트를 발생시키기 위해 상태 초기화
                Reset();
            }
        }

        /// <summary>
        /// 다른 트리거(버튼 등)에서 신호를 보낼 경우, 타이머를 켜거나 끄는 토글(Toggle) 역할
        /// </summary>
        public override void Execute()
        {
            if (periodicCoroutine != null)
            {
                StopTimer();
#if UNITY_EDITOR
                Debug.Log($"<color=red>[{gameObject.name}]</color> 외부 신호로 반복 타이머 정지");
#endif
            }
            else
            {
                StartTimer();
#if UNITY_EDITOR
                Debug.Log($"<color=green>[{gameObject.name}]</color> 외부 신호로 반복 타이머 시작");
#endif
            }
        }
    }
}