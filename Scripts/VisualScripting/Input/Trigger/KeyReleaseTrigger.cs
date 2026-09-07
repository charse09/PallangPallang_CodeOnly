using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 키를 누르고 있다가 손을 떼는 순간(Key Up) IsOn 상태를 켜는 트리거
    /// </summary>
    public class KeyReleaseTrigger : ProcessBase
    {
        [Header("Key Release Settings")]
        [Tooltip("감지할 상호작용 키를 지정합니다. (예: Space, F)")]
        [SerializeField] private KeyCode targetKey = KeyCode.Space;

        [Tooltip("키를 뗄 때마다 계속해서 재발동할지 여부 (차징 공격 등은 체크)")]
        [SerializeField] private bool isRepeatable = true;

        [Tooltip("최소 유지 시간(초)\n이 시간 이상 꾹 누르고 있다가 떼야만 발동합니다. (0이면 짧게 눌렀다 떼도 즉시 발동)")]
        [SerializeField] private float minHoldTime = 0.5f;

        private float currentHoldTime = 0f;
        private bool isHolding = false;
        private bool hasTriggered = false;

        private void Update()
        {
            // 일회성 기믹인데 이미 실행을 완료했다면 감지 중단
            if (!isRepeatable && hasTriggered) return;

            // 1. 키를 누르는 최초 순간 감지 (타이머 시작)
            if (Input.GetKeyDown(targetKey))
            {
                isHolding = true;
                currentHoldTime = 0f;
#if UNITY_EDITOR
                Debug.Log($"<color=yellow>[{gameObject.name}]</color> {targetKey} 키 입력 시작... (떼면 발동)");
#endif
            }

            // 2. 키를 누르고 있는 동안 시간 누적
            if (isHolding && Input.GetKey(targetKey))
            {
                currentHoldTime += Time.deltaTime;
            }

            // 3. 키에서 손을 떼는 순간 감지 (Key Up)
            if (isHolding && Input.GetKeyUp(targetKey))
            {
                isHolding = false;

                // 최소 유지 시간 조건을 만족했는지 확인
                if (currentHoldTime >= minHoldTime)
                {
                    TriggerAction();
                }
                else
                {
#if UNITY_EDITOR
                    Debug.Log($"<color=grey>[{gameObject.name}]</color> {targetKey} 키 떼기 취소됨. (유지 시간 {currentHoldTime:F2}초 / 최소 요구치 {minHoldTime}초)");
#endif
                }

                currentHoldTime = 0f; // 다음 시도를 위해 초기화
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
                Debug.Log($"<color=green>[{gameObject.name}]</color> {targetKey} 키 떼기 성공! 트리거가 발동되었습니다.");
#endif
            }
        }

        private IEnumerator PulseRoutine()
        {
            IsOn = true;
#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> {targetKey} 키 떼기 성공! (반복) 다음 노드 실행.");
#endif
            yield return null; // 1프레임 대기
            Reset(); // 상태 초기화
            hasTriggered = false; // 락 해제
        }

        public override void Execute()
        {
            // 외부 강제 실행용
            TriggerAction();
        }
    }
}