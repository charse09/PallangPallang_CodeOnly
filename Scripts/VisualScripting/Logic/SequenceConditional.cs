using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 여러 Output을 리스트 순서대로 '하나씩' 완료될 때까지 기다리며 실행하는 로직 노드
    /// </summary>
    public class SequenceConditional : ProcessBase
    {
        [SerializeField] private ProcessData inputData;
        [SerializeField] private List<ProcessData> outputData;
        [SerializeField] private bool isLoop;

        private bool _isProcessing = false;
        private bool _wasInputOnLastFrame = false; // 이전 프레임의 입력 상태 기억

        private void Update()
        {
            if (inputData.process == null) return;

            // 현재 입력 상태 확인
            bool currentInputState = CheckInputProcessStatus(inputData);

            // 핵심: 이전 프레임에선 False였는데 이번에 True가 된 '순간'이거나, Loop가 켜져있을 때만 발동
            bool isTriggeredNow = currentInputState && !_wasInputOnLastFrame;

            if ((isTriggeredNow || (isLoop && currentInputState)) && !_isProcessing)
            {
                StartCoroutine(SequenceRoutine());
            }

            // 다음 프레임을 위해 현재 상태 저장
            _wasInputOnLastFrame = currentInputState;
        }

        private IEnumerator SequenceRoutine()
        {
            _isProcessing = true;

#if UNITY_EDITOR
            Debug.Log($"<color=orange>[{gameObject.name}]</color> 순차 시퀀스 시작! (총 {outputData.Count}개)");
#endif

            foreach (var data in outputData)
            {
                if (data.process == null) continue;

                // 실행 전 강제 초기화
                data.process.Reset();

                // 해당 노드 실행
                data.process.Execute();

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[{gameObject.name}]</color> >> [{data.process.name}] 실행 중...");
#endif

                // ★ 핵심: 해당 노드의 IsOn이 true가 될 때까지 무한 대기
                while (!data.process.IsOn)
                {
                    yield return null;
                }

#if UNITY_EDITOR
                Debug.Log($"<color=green>[{gameObject.name}]</color> << [{data.process.name}] 완료됨!");
#endif
            }

            IsOn = true;
            _isProcessing = false;

            // 루프 설정이 되어있다면 입력 노드 초기화
            if (isLoop && inputData.process != null)
            {
                inputData.process.Reset();
            }

#if UNITY_EDITOR
            Debug.Log($"<color=white>[{gameObject.name}]</color> 전체 시퀀스가 성공적으로 종료되었습니다.");
#endif
        }

        public override void Execute()
        {
            if (!_isProcessing) StartCoroutine(SequenceRoutine());
        }

        public override void Reset()
        {
            // 실행 중인 좀비 코루틴을 즉시 강제 종료
            StopAllCoroutines();

            // 내부 상태 완전 초기화 → 다음 TriggerEnter 때 정상 재실행 가능
            _isProcessing = false;
            _wasInputOnLastFrame = false;

            base.Reset(); // IsOn = false
        }
    }
}