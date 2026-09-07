using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 키를 N초 동안 끊기지 않고 꾹 누르고 있어야 IsOn 상태를 켜는 트리거
    /// </summary>
    public class KeyHoldTimerTrigger : ProcessBase
    {
        [Header("Key Hold Settings")]
        [Tooltip("누르고 있을 상호작용 키를 지정합니다. (예: F, E, Space)")]
        [SerializeField] private KeyCode targetKey = KeyCode.F;

        [Tooltip("트리거가 발동하기 위해 키를 유지해야 하는 시간(초)입니다.")]
        [SerializeField] private float requiredHoldTime = 2.0f;

        [Tooltip("발동 완료 후, 나중에 다시 키를 꾹 눌러서 재발동할 수 있는지 여부")]
        [SerializeField] private bool isRepeatable = false;

        private float currentHoldTime = 0f;
        private bool isHolding = false;
        private bool hasTriggered = false;

        private void Update()
        {
            // 일회성 기믹인데 이미 발동을 완료했다면 입력 감지 중단
            if (!isRepeatable && hasTriggered) return;

            // 타겟 키를 누르고 있는 동안 매 프레임 실행
            if (Input.GetKey(targetKey))
            {
                if (!isHolding)
                {
                    isHolding = true;
#if UNITY_EDITOR
                    Debug.Log($"<color=yellow>[{gameObject.name}]</color> {targetKey} 키 홀드 시작...");
#endif
                }

                currentHoldTime += Time.deltaTime; // 누른 시간 누적

                // 요구된 목표 시간을 채웠을 때
                if (currentHoldTime >= requiredHoldTime)
                {
                    TriggerAction();
                }
            }
            // 도중에 키에서 손을 뗐을 때 (진행도 초기화)
            else
            {
                if (isHolding)
                {
                    isHolding = false;

                    // 게이지를 다 채우지 못하고 취소된 경우 로그 출력
                    if (currentHoldTime > 0f && currentHoldTime < requiredHoldTime)
                    {
#if UNITY_EDITOR
                        Debug.Log($"<color=grey>[{gameObject.name}]</color> {targetKey} 키 입력 취소. (유지 시간: {currentHoldTime:F1}초)");
#endif
                    }

                    // 타이머 초기화 (처음부터 다시 눌러야 함)
                    currentHoldTime = 0f;
                }
            }
        }

        private void TriggerAction()
        {
            hasTriggered = true;
            currentHoldTime = 0f;
            isHolding = false;

            if (isRepeatable)
            {
                // 반복형: 시스템 감지를 위해 펄스(Pulse) 발생
                StartCoroutine(PulseRoutine());
            }
            else
            {
                // 일회성: 상태 영구 켬
                IsOn = true;
#if UNITY_EDITOR
                Debug.Log($"<color=green>[{gameObject.name}]</color> {requiredHoldTime}초 동안 키 홀드 완료! 트리거가 발동되었습니다.");
#endif
            }
        }

        private IEnumerator PulseRoutine()
        {
            IsOn = true;
#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> {requiredHoldTime}초 동안 키 홀드 완료! (반복) 트리거 발동.");
#endif
            yield return null; // 1프레임 대기
            Reset(); // 프레임워크 초기화
            hasTriggered = false; // 락 해제
        }

        public override void Execute()
        {
            // 외부(다른 노드)에서 신호를 받아 강제로 완료 처리할 때 사용
            TriggerAction();
        }
    }
}