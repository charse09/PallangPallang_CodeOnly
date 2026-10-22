/*
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

        // QTEProcess.cs 내부
        private bool _isWaitingInput = false;

        // [추가] 외부 노드(SplineWindRollMove)에서 QTE 실행 중 여부를 감지하기 위한 프로퍼티
        public bool IsWaitingInput => _isWaitingInput;

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

        [Header("Grace Period Override (선택 사항)")]
        [Tooltip("체크 시 QTEController의 기본 설정을 무시하고 이 노드만의 유예 시간을 개별 적용합니다.")]
        [SerializeField] private bool overrideGracePeriod = false;

        [Tooltip("체크 시 적용할 유예 시간 활성화 여부")]
        [SerializeField] private bool useGracePeriod = false;

        [Tooltip("초반 입력 유예 시간 (초, 기본값 0초)")]
        [Min(0f)]
        [SerializeField] private float gracePeriodDuration = 0f;

        private bool _isWaitingInput = false;

        public bool IsWaitingInput => _isWaitingInput;

        public override void Execute()
        {
            if (_isWaitingInput) return;

            IsOn = false;
            _isWaitingInput = true;

            if (targetController != null)
            {
                if (overrideGracePeriod)
                {
                    targetController.SetupAndStart(targetKey, OnQTESuccess, OnQTEFail, useGracePeriod, gracePeriodDuration);
                }
                else
                {
                    targetController.SetupAndStart(targetKey, OnQTESuccess, OnQTEFail);
                }
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