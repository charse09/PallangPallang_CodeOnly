using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class TimingHoldQTEProcess : ProcessBase
    {
        [Header("Target Controller Assign")]
        [Tooltip("이 노드가 실행할 때 켜줄 Timing Hold QTE UI 컨트롤러 오브젝트를 연결하세요.")]
        [SerializeField] private TimingHoldQTEController targetController; 

        [Header("Key & Visual Settings")]
        [SerializeField] private KeyCode targetKey = KeyCode.E;
        [Tooltip("누르고 있어야 할 키의 가이드 이미지 (예: E버튼 아이콘)")]
        [SerializeField] private Sprite keyHintSprite;

        [Header("Wait Limit Settings")]
        [Tooltip("체크하면 입력 대기 시간에 제한을 둡니다. 제한 시간 내에 키를 누르지 않으면 실패합니다.")]
        [SerializeField] private bool hasWaitLimit = false;
        [Tooltip("입력을 기다리는 제한 시간 (초 단위)")]
        [SerializeField] private float waitLimitTime = 5.0f;

        [Header("Timing Balance Options")]
        [Tooltip("화살표가 왼쪽 끝에서 오른쪽 끝까지 도달하는 데 걸리는 시간 (초)")]
        [SerializeField] private float arrowMoveDuration = 2.0f;

        [Header("Fail Branch Input")]
        [Tooltip("실패 시 분기할 노드")]
        [SerializeField] private ProcessData failOutputData;

        private bool _isWaitingInput = false;

        public override void Execute()
        {
            if (_isWaitingInput) return;

            IsOn = false;
            _isWaitingInput = true;

            if (targetController != null)
            {
                targetController.SetupAndStart(
                    targetKey, 
                    keyHintSprite, 
                    hasWaitLimit, 
                    waitLimitTime, 
                    arrowMoveDuration, 
                    OnQTESuccess, 
                    OnQTEFail
                );
            }
            else
            {
                Debug.LogError($"[{gameObject.name}]에 할당된 TimingHoldQTEController가 없습니다!");
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
