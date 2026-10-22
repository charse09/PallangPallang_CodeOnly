using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 키보드 키나 마우스 클릭 등 아무 입력이나 감지되었을 때 IsOn 상태를 켜는 트리거
    /// </summary>
    public class AnyKeyInputTrigger : ProcessBase
    {
        [Header("Trigger Settings")]
        [Tooltip("입력이 들어올 때마다 계속해서 재발동할지(반복), 딱 한 번만 발동할지 여부")]
        [SerializeField] private bool isRepeatable = true;

        [Header("Optional Filters")]
        [Tooltip("마우스 커서를 움직이는 것도 입력으로 인정할지 여부 (기본: 키/클릭만 감지)")]
        [SerializeField] private bool includeMouseMove = false;

        [Tooltip("마우스 움직임 감지 기준 거리(픽셀). includeMouseMove가 켜져 있을 때만 동작합니다.")]
        [SerializeField] private float mouseMoveThreshold = 5.0f;

        private bool hasTriggered = false;
        private Vector3 lastMousePosition;

        private void Start()
        {
            lastMousePosition = Input.mousePosition;
        }

        private void Update()
        {
            // 일회성 기믹인데 이미 실행되었다면 감지 중단
            if (!isRepeatable && hasTriggered) return;

            // 1. 키보드 전체 키 및 마우스 클릭(좌/우/휠) 감지
            bool inputDetected = Input.anyKeyDown;

            // 2. (선택 사항) 마우스 움직임 감지
            if (!inputDetected && includeMouseMove)
            {
                if (Vector3.Distance(Input.mousePosition, lastMousePosition) > mouseMoveThreshold)
                {
                    inputDetected = true;
                }
            }

            lastMousePosition = Input.mousePosition;

            if (inputDetected)
            {
                TriggerAction();
            }
        }

        private void TriggerAction()
        {
            hasTriggered = true;

            if (isRepeatable)
            {
                // 반복형: 시스템이 상태 변화를 감지할 수 있도록 1프레임 펄스(Pulse) 발생
                StartCoroutine(PulseRoutine());
            }
            else
            {
                // 일회성: 상태 영구 켬
                IsOn = true;
#if UNITY_EDITOR
                Debug.Log($"<color=green>[{gameObject.name}]</color> 입력 감지! 트리거가 영구 발동되었습니다.");
#endif
            }
        }

        private IEnumerator PulseRoutine()
        {
            // 1. 상태를 true로 변경하여 연결된 Logic/Output 노드들이 작동하게 함
            IsOn = true;
#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> 입력 감지! (반복) 트리거 발동.");
#endif

            // 2. 프레임워크가 IsOn = true 상태를 감지할 수 있도록 딱 한 프레임 대기
            yield return null;

            // 3. 다음 입력을 받기 위해 상태 초기화
            Reset();
            hasTriggered = false;
        }

        public override void Execute()
        {
            // 외부 노드에서 강제로 신호를 보낼 때 사용
            TriggerAction();
        }
    }
}