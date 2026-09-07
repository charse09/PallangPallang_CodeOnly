using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 충돌 없이, 씬이 시작되거나 활성화된 시점부터 N초 후에 IsOn 상태를 true로 변경하는 트리거
    /// </summary>
    public class AutoTimerTrigger : ProcessBase
    {
        [Header("Auto Timer Settings")]
        [Tooltip("대기할 시간(초)을 설정합니다.")]
        [SerializeField] private float delaySeconds = 3.0f;

        [Tooltip("오브젝트가 꺼졌다가 켜질 때마다 타이머를 재시작할지 여부")]
        [SerializeField] private bool restartOnEnable = false;

        private void Start()
        {
            if (!restartOnEnable)
            {
                StartCoroutine(TimerRoutine());
            }
        }

        private void OnEnable()
        {
            // 오브젝트가 활성화될 때마다 초기화 후 재시작
            if (restartOnEnable)
            {
                Reset(); // ProcessBase에 구현된 IsOn = false 초기화 함수
                StartCoroutine(TimerRoutine());
            }
        }

        private IEnumerator TimerRoutine()
        {
            // 설정된 시간만큼 대기
            yield return new WaitForSeconds(delaySeconds);

            // 대기가 끝나면 자신의 상태를 켬
            // -> 이 컴포넌트를 연결해둔 Logic/Output 노드들이 이 상태를 감지하고 알아서 작동함!
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"[{gameObject.name}] {delaySeconds}초 경과. IsOn 상태가 true로 변경되었습니다.");
#endif
        }

        // 수동으로 노드를 작동시켜야 할 경우
        public override void Execute()
        {
            Debug.LogError("AutoTimerTrigger.Execute");
            // 강제 실행 시 즉시 상태 반전 (프레임워크 기본 컨벤션)
            IsOn = !IsOn;

         
        }
    }
}