using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class FastQTEProcess : ProcessBase
    {
        [Header("Target Controller Assign")]
        [Tooltip("이 노드가 실행할 때 켜줄 Fast QTE UI 컨트롤러 오브젝트를 연결하세요.")]
        [SerializeField] private FastQTEController targetController; 

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

            if (targetController != null)
            {
                targetController.SetupAndStart(targetKey, OnQTESuccess, OnQTEFail);
            }
            else
            {
                Debug.LogError($"[{gameObject.name}]에 할당된 FastQTEController가 없습니다! 인스펙터를 확인하세요.");
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
