using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class CanLoopConditional : ProcessBase
    {
        [SerializeField] private ProcessData inputData;
        [SerializeField] private List<ProcessData> outputData;
        [SerializeField] private bool isLoop;

        [Header("Cooldown Settings")]
        [Tooltip("한 번 실행된 후 다시 발동될 수 있을 때까지의 대기 시간(초)입니다.")]
        [SerializeField] private float cooldownTime = 0.5f;

        // 마지막 실행 시각
        private float _lastExecuteTime = -999f;

        private void Update()
        {
            IsOn = CheckInputProcessStatus(inputData);

            if (IsOn)
            {
                // 1. 쿨타임 도중에 들어온 연속 충돌 신호는 무시하고 잔여 신호 제거
                if (Time.time - _lastExecuteTime < cooldownTime)
                {
                    if (isLoop && inputData.process != null)
                    {
                        inputData.process.Reset();
                    }
                    return;
                }

                // 2. 쿨타임이 지났으므로 실행 시각 갱신 후 아웃풋 트리거
                _lastExecuteTime = Time.time;
                Debug.Log($"{inputData.GetType()}.Update()");

                Execute();

                // 3. 루프 모드일 경우 Input 신호 초기화
                if (isLoop)
                {
                    if (inputData.process != null)
                    {
                        inputData.process.Reset();
                    }
                }
            }
        }

        public override void Execute()
        {
            if (outputData == null) return;

            int i = 0;
            foreach (var output in outputData)
            {
                if (output.process != null)
                {
                    output.process.Execute();
                    Debug.Log($"{outputData.GetType()}.Execute({i})");
                    i++;
                }
            }
        }
    }
}