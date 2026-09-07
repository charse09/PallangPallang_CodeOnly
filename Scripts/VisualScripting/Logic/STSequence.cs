using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// SequenceConditional처럼 등록된 Output들을 순차적으로 실행하되,
    /// 외부에서 Output 노드로 직접 호출(Execute)할 수 있고,
    /// 실행 완료 시 IsOn을 자동으로 꺼서(false) 언제든 재호출이 가능하도록 만든 시퀀스 Output 노드입니다.
    /// </summary>
    public class STSequence : ProcessBase
    {
        [Header("Input Trigger (선택사항)")]
        [Tooltip("조건에 의해 자동 실행하고 싶을 때 연결 (직접 Execute()로 호출할 경우 비워두셔도 됩니다)")]
        [SerializeField] private ProcessData inputData;

        [Header("Sequence Outputs")]
        [Tooltip("순서대로 하나씩 완료될 때까지 기다리며 실행할 Output 노드 리스트")]
        [SerializeField] private List<ProcessData> outputData = new List<ProcessData>();

        [Header("Execution Settings")]
        [Tooltip("시퀀스 완료 후 IsOn을 자동으로 다시 false로 끌지 여부 (재호출 및 상위 시퀀스 연동용)")]
        [SerializeField] private bool autoTurnOffIsOn = true;

        [Tooltip("체크 시 inputData가 켜져있는 동안 계속 반복 실행")]
        [SerializeField] private bool isLoop = false;

        private bool _isProcessing = false;
        private bool _wasInputOnLastFrame = false;
        private Coroutine _sequenceCoroutine;

        private void Update()
        {
            if (inputData.process == null) return;

            bool currentInputState = CheckInputProcessStatus(inputData);
            bool isTriggeredNow = currentInputState && !_wasInputOnLastFrame;

            if ((isTriggeredNow || (isLoop && currentInputState)) && !_isProcessing)
            {
                Execute();
            }

            _wasInputOnLastFrame = currentInputState;
        }

        /// <summary>
        /// 외부나 상위 시퀀스에서 이 노드를 Output으로 호출할 때 실행
        /// </summary>
        public override void Execute()
        {
            if (_isProcessing && _sequenceCoroutine != null)
            {
                StopCoroutine(_sequenceCoroutine);
            }

            _sequenceCoroutine = StartCoroutine(SequenceRoutine());
        }

        /// <summary>
        /// 강제 초기화
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            IsOn = false;
            _isProcessing = false;

            if (_sequenceCoroutine != null)
            {
                StopCoroutine(_sequenceCoroutine);
                _sequenceCoroutine = null;
            }
        }

        private IEnumerator SequenceRoutine()
        {
            _isProcessing = true;
            IsOn = false;

#if UNITY_EDITOR
            Debug.Log($"<color=orange>[{gameObject.name} (STSequence)]</color> 시퀀스 시작! (총 {outputData.Count}개)");
#endif

            foreach (var data in outputData)
            {
                if (data.process == null) continue;

                // 1. 하위 노드 초기화 및 실행
                data.process.Reset();
                data.process.Execute();

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[{gameObject.name}]</color> >> [{data.process.name}] 실행 중...");
#endif

                // 2. 하위 노드가 끝날 때(IsOn == true)까지 대기
                while (!data.process.IsOn)
                {
                    yield return null;
                }

#if UNITY_EDITOR
                Debug.Log($"<color=green>[{gameObject.name}]</color> << [{data.process.name}] 완료됨!");
#endif
            }

            // 3. 모든 시퀀스 완주!
            IsOn = true;
            _isProcessing = false;

#if UNITY_EDITOR
            Debug.Log($"<color=white>[{gameObject.name} (STSequence)]</color> 전체 시퀀스 완료!");
#endif

            // 루프 설정 시 입력 노드 초기화
            if (isLoop && inputData.process != null)
            {
                inputData.process.Reset();
            }

            // 4. 상위 SequenceConditional이 IsOn=true를 감지할 수 있도록 1프레임 대기 후 자동 OFF
            if (autoTurnOffIsOn)
            {
                yield return null;
                IsOn = false;

#if UNITY_EDITOR
                Debug.Log($"<color=yellow>[{gameObject.name} (STSequence)]</color> IsOn이 다시 false로 리셋되었습니다. (재호출 준비 완료)");
#endif
            }

            _sequenceCoroutine = null;
        }
    }
}