using UnityEngine;
using System.Collections.Generic;

namespace _Project.Scripts.VisualScripting
{
    public class HoldQTEProcess : ProcessBase
    {
        [Header("Target Controller Assign")]
        [SerializeField] private HoldQTEController targetController;

        [Header("QTE Key Settings")]
        [SerializeField] private List<KeyCode> targetKeys = new List<KeyCode> { KeyCode.A };

        // 어떤 이미지를 띄울지 할당하는 슬롯 추가
        [Header("QTE Visual Settings")]
        [Tooltip("이 노드가 실행될 때 컨트롤러 패널에 띄워줄 이미지를 할당하세요. (A키 이미지, D키 이미지 등)")]
        [SerializeField] private Sprite visualCueSprite;

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
                // 시각적 이미지 데이터(visualCueSprite)를 함께 전달!
                targetController.SetupAndStart(targetKeys, visualCueSprite, OnQTESuccess, OnQTEFail);
            }
            else
            {
                Debug.LogError($"[{gameObject.name}]에 할당된 HoldQTEController가 없습니다!");
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