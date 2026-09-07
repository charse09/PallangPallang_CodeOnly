using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class MTEProcess : ProcessBase
    {
        [Header("Target Controller Assign")]
        [SerializeField] private MTEController targetController; 

        [Header("MTE Key & Visual Settings")]
        [SerializeField] private KeyCode targetKey = KeyCode.Space;
        
        
        [Tooltip("연타 버블 중앙에 보여줄 가이드 이미지(예: 스페이스바 아이콘, 마우스 아이콘 등)를 드래그하세요.")]
        [SerializeField] private Sprite keyHintSprite;       // 🆕 노드별 커스텀 가이드 이미지

        [Header("MTE Balance Options")]
        [SerializeField] private float mashPower = 0.08f;
        [SerializeField] private float drainSpeed = 0.2f;
        [SerializeField] private float timeLimit = 4.0f;

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
                // 🆕 컨트롤러를 가동할 때 'keyHintSprite'도 함께 묶어서 전송합니다!
                targetController.SetupAndStart(targetKey, keyHintSprite, mashPower, drainSpeed, timeLimit, OnMTESuccess, OnMTEFail);
            }
            else
            {
                Debug.LogError($"[{gameObject.name}]에 할당된 MTEController가 없습니다!");
                OnMTEFail();
            }
        }

        private void OnMTESuccess()
        {
            _isWaitingInput = false;
            IsOn = true; 
        }

        private void OnMTEFail()
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