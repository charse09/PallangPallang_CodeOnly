/*
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 시스템에 끼워 넣을 QTE 로직 노드 블록
    /// </summary>
    public class QTEProcess : ProcessBase
    {
        [Header("QTE Key Settings")]
        [SerializeField] private KeyCode targetKey = KeyCode.Space;

        [Header("Fail Branch Input")]
        [Tooltip("QTE 실패 시 실행시켜 줄 다른 블록(Input으로 작동할 노드)을 연결하세요.")]
        [SerializeField] private ProcessData failOutputData;

        private bool _isWaitingInput = false;

        public override void Execute()
        {
            // 중복 실행 방지
            if (_isWaitingInput) return;

            IsOn = false; // 실행 시작 시 판정값 false 초기화
            _isWaitingInput = true;

            if (QTEController.Instance != null)
            {
                // UI 매니저에게 감지 위임
                QTEController.Instance.SetupAndStart(targetKey, OnQTESuccess, OnQTEFail);
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] 씬에 QTEController가 존재하지 않습니다.");
                OnQTEFail();
            }
        }

        // QTE 성공 시 호출
        private void OnQTESuccess()
        {
            _isWaitingInput = false;
            
            // IsOn이 true가 됨으로써 상위 SequenceConditional의 while(!IsOn) 대기가 풀림!
            // 다음 순차 시퀀스 노드가 안전하게 실행됨.
            IsOn = true; 
            
#if UNITY_EDITOR
            Debug.Log($"<color=green>[QTE SUCCESS]</color> 지정된 키 {targetKey} 입력 성공. 다음 시퀀스로 진행합니다.");
#endif
        }

        // QTE 실패 시 호출
        private void OnQTEFail()
        {
            _isWaitingInput = false;
            
            // IsOn을 true로 만들지 않으므로 상위 SequenceConditional은 대기를 풀지 못하고 흐름이 끊김(Block).
            IsOn = false; 

#if UNITY_EDITOR
            Debug.Log($"<color=red>[QTE FAIL]</color> QTE 실패. 현재 시퀀스 라인이 차단되었습니다.");
#endif

            // 대신 실패했을 때 연동된 다른 분기 블록(input 역할)이 있다면 그쪽을 실행시킴
            if (failOutputData.process != null)
            {
#if UNITY_EDITOR
                Debug.Log($"<color=yellow>[QTE FAIL BRANCH]</color> 실패 분기 노드 [{failOutputData.process.name}]를 실행합니다.");
#endif
                failOutputData.process.Reset();
                failOutputData.process.Execute();
            }
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;
            _isWaitingInput = false;
        }
    }
}
*/

using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class QTEProcess : ProcessBase
    {
        [Header("Target Controller Assign")]
        [Tooltip("이 노드가 실행할 때 켜줄 QTE UI 컨트롤러 오브젝트를 연결하세요.")]
        [SerializeField] private QTEController targetController; 

        [Header("QTE Key Settings")]
        [SerializeField] private KeyCode targetKey = KeyCode.Space;

        [Header("Fail Branch Input")]
        [SerializeField] private ProcessData failOutputData;

        private bool _isWaitingInput = false;

        public override void Execute()
        {
            if (_isWaitingInput) return;

            IsOn = false;
            _isWaitingInput = true;

            // 매니저 싱글톤 대신 지정된 타겟 컨트롤러를 찌릅니다.
            if (targetController != null)
            {
                targetController.SetupAndStart(targetKey, OnQTESuccess, OnQTEFail);
            }
            else
            {
                Debug.LogError($"[{gameObject.name}]에 할당된 QTEController가 없습니다! 인스펙터를 확인하세요.");
                OnQTEFail();
            }
        }

        private void OnQTESuccess()
        {
            _isWaitingInput = false;
            IsOn = true; 
        }

        private void OnQTEFail()
        {
            _isWaitingInput = false;
            IsOn = false; 

            if (failOutputData.process != null)
            {
                failOutputData.process.Reset();
                failOutputData.process.Execute();
            }
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;
            _isWaitingInput = false;
        }
    }
}