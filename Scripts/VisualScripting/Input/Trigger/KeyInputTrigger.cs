using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 특정 키를 눌렀을 때 IsOn 상태를 켜는 트리거
    /// </summary>
    public class KeyInputTrigger : ProcessBase
    {
        [Header("Key Input Settings")]
        [Tooltip("감지할 키보드 키를 선택하세요. (예: F, Space, Return 등)")]
        [SerializeField] private KeyCode targetKey = KeyCode.F;

        [Tooltip("키를 누를 때마다 계속해서 재발동할지(반복), 딱 한 번만 발동할지 여부")]
        [SerializeField] private bool isRepeatable = true;

        private bool hasTriggered = false;

        private void Update()
        {
            // 일회성 기믹인데 이미 실행되었다면 입력 감지 중단
            if (!isRepeatable && hasTriggered) return;

            // 지정된 키를 누르는 '순간'을 감지 (꾹 누르는 것과 무관하게 1회 입력)
            if (Input.GetKeyDown(targetKey))
            {
                TriggerAction();
            }
        }

        private void TriggerAction()
        {
            hasTriggered = true;

            if (isRepeatable)
            {
                // 반복형: 시스템이 상태 변화를 감지할 수 있도록 펄스(Pulse) 발생
                StartCoroutine(PulseRoutine());
            }
            else
            {
                // 일회성: 상태 영구 켬
                IsOn = true;
#if UNITY_EDITOR
                Debug.Log($"<color=green>[{gameObject.name}]</color> {targetKey} 키 입력 감지! 트리거가 영구 발동되었습니다.");
#endif
            }
        }

        private IEnumerator PulseRoutine()
        {
            // 1. 상태를 true로 변경하여 연결된 Logic/Output 노드들이 작동하게 함
            IsOn = true;
#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> {targetKey} 키 입력 감지! (반복) 트리거 발동.");
#endif

            // 2. 프레임워크가 IsOn = true 상태를 감지할 수 있도록 딱 한 프레임만 대기
            yield return null;

            // 3. 다음 입력을 받기 위해 즉시 상태 초기화
            Reset();
            hasTriggered = false;
        }

        public override void Execute()
        {
            // 외부(다른 노드)에서 신호를 보내 강제로 이 트리거를 누른 것과 같은 효과를 낼 때 사용
            TriggerAction();
        }
    }
}