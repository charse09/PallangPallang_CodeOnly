using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 외부 노드로부터 신호(Execute)를 받을 때마다 카운트를 누적하고,
    /// 목표 카운트에 도달하면 IsOn 상태를 true로 변경하는 카운터 트리거
    /// </summary>
    public class CounterTrigger : ProcessBase
    {
        [Header("Counter Settings")]
        [Tooltip("발동에 필요한 목표 카운트 횟수")]
        [SerializeField] private int targetCount = 3;

        [Tooltip("목표치에 도달하여 발동된 후, 카운트를 자동으로 0으로 초기화할지 여부\n(반복해서 사용해야 하는 기믹일 경우 체크)")]
        [SerializeField] private bool autoResetOnTrigger = false;

        [Header("Debug View")]
        [Tooltip("현재 누적된 카운트 (에디터 확인용)")]
        [SerializeField] private int currentCount = 0;

        /// <summary>
        /// 이전 노드(스위치, 적 처치 이벤트 등)에서 신호가 넘어올 때마다 호출됩니다.
        /// </summary>
        public override void Execute()
        {
            // 이미 목표치에 도달했고, 자동 초기화가 꺼져있다면 더 이상 카운트하지 않음
            if (IsOn && !autoResetOnTrigger) return;

            // 카운트 1 증가
            currentCount++;

#if UNITY_EDITOR
            Debug.Log($"<color=orange>[{gameObject.name}]</color> 카운트 증가: {currentCount} / {targetCount}");
#endif

            // 목표 값에 도달했는지 검사
            if (currentCount >= targetCount)
            {
                if (autoResetOnTrigger)
                {
                    // 반복 사용을 위해 펄스(Pulse) 방식으로 상태를 켰다가 끕니다.
                    StartCoroutine(PulseRoutine());
                }
                else
                {
                    // 일회성 기믹이므로 영구적으로 켜둡니다.
                    IsOn = true;
#if UNITY_EDITOR
                    Debug.Log($"<color=green>[{gameObject.name}]</color> 목표 카운트({targetCount}) 도달! IsOn = true");
#endif
                }
            }
        }

        private IEnumerator PulseRoutine()
        {
            // 프레임워크가 상태 변화를 감지할 수 있도록 true로 변경 후 1프레임 대기
            IsOn = true;
#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 목표 카운트 도달! (Auto Reset) 신호를 전달합니다.");
#endif
            yield return null;

            // 다음번 사용을 위해 카운트와 상태를 즉시 초기화
            currentCount = 0;
            Reset(); // ProcessBase에 구현된 IsOn = false
        }

        /// <summary>
        /// 게임 진행 중 수동으로 카운트를 0으로 초기화해야 할 때 호출 (예: 퍼즐 실패 시)
        /// </summary>
        public void ClearCount()
        {
            currentCount = 0;
            Reset();
#if UNITY_EDITOR
            Debug.Log($"<color=red>[{gameObject.name}]</color> 카운트가 0으로 강제 초기화되었습니다.");
#endif
        }
    }
}