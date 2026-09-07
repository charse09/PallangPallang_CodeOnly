using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 외부 노드나 스크립트에서 상태를 자유롭게 켜고(Set) 끌 수 있는(Reset) 래치 노드
    /// </summary>
    public class StateLatch : ProcessBase
    {
        [Header("Node Inputs")]
        [Tooltip("이 입력이 켜지면 상태가 true가 됩니다.")]
        [SerializeField] private ProcessData setInput;   // TriggerEnter 등을 연결
        [Tooltip("이 입력이 켜지면 상태가 false가 됩니다.")]
        [SerializeField] private ProcessData resetInput; // TriggerExit이나 다른 리셋 조건을 연결

        [Header("State")]
        [Tooltip("현재 상태 변수 (인스펙터에서 초기 상태 설정 가능)")]
        [SerializeField] private bool currentState = false;

        private void Start()
        {
            // 시작 시 초기 상태 동기화
            IsOn = currentState;
        }

        private void Update()
        {
            // 1. 켜기(Set) 조건 검사
            // ProcessData는 구조체이므로 내부의 process 참조가 null인지 확인합니다.
            if (setInput.process != null && CheckInputProcessStatus(setInput))
            {
                SetState(true);
            }
            // 2. 끄기(Reset) 조건 검사
            // setInput에 의해 이미 처리되었다면 검사하지 않습니다(Set 우선).
            else if (resetInput.process != null && CheckInputProcessStatus(resetInput))
            {
                SetState(false);
            }
        }

        public override void Execute()
        {
            // 기본 Execute 호출 시 상태를 켜도록 처리 (필요에 따라 Toggle로 변경 가능)
            SetState(true);
        }

        /// <summary>
        /// 외부 스크립트나 UnityEvent에서 상태를 직접 지정할 때 사용합니다.
        /// </summary>
        public void SetState(bool newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                IsOn = currentState;

                // 상태가 켜졌을 때 추가적인 처리가 필요하다면 여기에 작성
                // if (IsOn) { ... }
            }
        }

        // --- 외부에서 직관적으로 호출할 수 있는 편의용 메서드들 ---

        public void TurnOn()
        {
            SetState(true);
        }

        public void TurnOff()
        {
            SetState(false);
        }

        public void Toggle()
        {
            SetState(!currentState);
        }
    }
}