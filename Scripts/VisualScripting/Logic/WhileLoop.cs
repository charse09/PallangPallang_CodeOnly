using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class WhileLoop : ProcessBase
    {
        [SerializeField] private ProcessData inputData;
        [SerializeField] private List<ProcessData> outputData;
        [SerializeField] private float delay;

        private Coroutine _loopCoroutine;

        private void Update()
        {
            if (CheckInputProcessStatus(inputData))
            {
                if (_loopCoroutine == null)
                {
                    IsOn = true;
                    _loopCoroutine = StartCoroutine(RuntimeProcess());
                }
            }
            else
            {
                IsOn = false;
            }
        }

        public override void Execute()
        {
            if (_loopCoroutine != null)
            {
                StopCoroutine(_loopCoroutine);
            }
            IsOn = true;
            _loopCoroutine = StartCoroutine(RuntimeProcess());
        }

        private IEnumerator RuntimeProcess()
        {
            while (CheckInputProcessStatus(inputData))
            {
                IsOn = true;
                foreach (var output in outputData)
                {
                    if (output.process == null) continue;

                    output.process.Execute();

                    // 각 Output 프로세스가 완료(IsOn = true)될 때까지 대기합니다.
                    while (!output.process.IsOn)
                    {
                        yield return null;
                    }
                }

                if (delay > 0f)
                {
                    yield return new WaitForSeconds(delay);
                }
                else
                {
                    yield return null; // 딜레이가 없을 때 무한루프 방지
                }
            }

            IsOn = false;
            _loopCoroutine = null;
        }

        public override void Reset()
        {
            base.Reset();
            if (_loopCoroutine != null)
            {
                StopCoroutine(_loopCoroutine);
                _loopCoroutine = null;
            }
        }
    }
}