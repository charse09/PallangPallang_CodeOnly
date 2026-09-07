using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _Project.Scripts.VisualScripting
{
    public class Conditional : ProcessBase
    {
        [SerializeField] private ProcessData inputData;
        [SerializeField] private List<ProcessData> outputData;
        private bool _isRunning;
        [SerializeField] private bool isLoop;

        // TODO: inputData 가 ture 일때 한번만 실행된다.
        // Input Data에 True 신호가 들어올 때 마다 한번 실행한다.
        private void Update()
        {
            IsOn = CheckInputProcessStatus(inputData);

            if (IsOn)
            {
                if (_isRunning) return;
                Debug.Log($"{inputData.GetType()}.Update()");
                Execute();
                _isRunning = true;
                if (isLoop)
                {
                    inputData.process.Reset();
                    foreach (var output in outputData)
                        output.process.Reset();
                }
            }
            else
            {
                _isRunning = false;
            }
        }
        
        public override void Execute()
        {
            int i = 0;
            foreach (var output in outputData)
            {
                output.process.Execute();
                Debug.Log($"{outputData.GetType()}.Execute({i})");
                i++;
            }
        }

#if UNITY_EDITOR
        // 인스펙터 값이 변경될 때마다 유니티가 호출해주는 콜백
        private void OnValidate()
        {
            if (outputData == null) return;

            for (int i = 0; i < outputData.Count; i++)
            {
                // 자기 자신이 연결되었는지 감시
                if (outputData[i].process == this)
                {
                    // 1. 팝업창(Alert) 띄우기
                    EditorUtility.DisplayDialog(
                        "⚠️ 치명적 오류: 순환 참조 감지",
                        $"'{gameObject.name}' 오브젝트가 자기 자신을 Output으로 가리키고 있습니다.\n\n" +
                        "이대로 실행하면 'StackOverflow(무한 루프)'가 발생합니다.\n" +
                        "안전을 위해 연결을 자동으로 해제했습니다.",
                        "확인 (알겠습니다)"
                    );

                    // 2. 문제의 연결을 즉시 끊어버리기 (초기화)
                    // (ProcessData가 구조체라면 new, 클래스라면 null 등으로 처리)
                    outputData[i] = new ProcessData();

                    // 3. 변경사항을 저장하라고 에디터에게 알림 (Dirty Flag)
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}