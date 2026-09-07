using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class SequenceCollisionTrigger : ProcessBase
    {
        [Header("순서대로 누를 트리거 목록")]
        [SerializeField] private List<CollisionTrigger> sequenceSteps;

        [Header("설정")]
        [SerializeField] private bool resetOnFail = true; // 순서를 틀렸을 때 처음부터 다시 할지 여부

        private int _currentStepIndex = 0; // 현재 진행 중인 단계

        private void Update()
        {
            // 이미 완료되었거나, 리스트가 비어있으면 작동하지 않음
            if (IsOn || sequenceSteps.Count == 0) return;

            // 1. 현재 단계에서 눌러야 할 '정답 트리거' 확인
            CollisionTrigger expectedTrigger = sequenceSteps[_currentStepIndex];

            if (expectedTrigger.IsOn)
            {
                Debug.Log($"[{gameObject.name}] {_currentStepIndex + 1}단계 성공!");
                _currentStepIndex++;

                // 모든 순서를 다 맞췄다면?
                if (_currentStepIndex >= sequenceSteps.Count)
                {
                    IsOn = true;
                    Execute(); // 후속 노드(Conditional 등)로 신호 전달
                    Debug.Log($"[{gameObject.name}] 순서 퍼즐 완료! 최종 트리거 발동!");
                }
                return; // 성공했으면 이번 프레임 검사 종료
            }

            // 2. 순서를 틀렸는지 검사 (resetOnFail이 true일 때만)
            if (resetOnFail)
            {
                // 아직 누를 차례가 아닌 '미래의 스위치'가 눌렸는지 확인합니다.
                for (int i = _currentStepIndex + 1; i < sequenceSteps.Count; i++)
                {
                    if (sequenceSteps[i].IsOn)
                    {
                        FailSequence();
                        break;
                    }
                }
            }
        }

        private void FailSequence()
        {
            Debug.Log($"[{gameObject.name}] 잘못된 순서입니다! 퍼즐이 초기화됩니다.");
            _currentStepIndex = 0; // 진행도 초기화

            // 리스트에 있는 모든 스위치의 상태를 끕니다.
            foreach (var trigger in sequenceSteps)
            {
                trigger.Reset();
            }
        }

        public override void Execute()
        {
            // 이 컴포넌트가 최종적으로 켜졌을 때 실행할 내용.
            // 보통 유니티 에디터에서 Conditional 노드 등에 이 스크립트를 연결해서 씁니다.
        }
    }
}