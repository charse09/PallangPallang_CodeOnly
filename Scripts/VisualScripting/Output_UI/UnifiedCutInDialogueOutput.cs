using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    public class UnifiedCutInDialogueOutput : ProcessBase
    {
        [Header("Global Setting")]
        [Tooltip("씬에 배치한 UnifiedCutInDialogueManager 레퍼런스")]
        [SerializeField] private UnifiedCutInDialogueManager manager;

        [Header("Cut-In & Dialogue Sequence")]
        [Tooltip("멀티 슬롯, 딜레이, 더빙 보이스가 모두 포함된 통합 시퀀스 단계들을 설계합니다.")]
        [SerializeField] private List<UnifiedCutInDialogueManager.CutInDialogueStep> sequenceSteps = new List<UnifiedCutInDialogueManager.CutInDialogueStep>();

        [Header("Progression Options")]
        [Tooltip("대화 시퀀스가 끝날 때까지 대기하지 않고 즉시 다음 노드를 실행할지 여부")]
        [SerializeField] private bool runInBackground = false;

        public override void Execute()
        {
            // 씬에 매니저가 안 붙어있다면 자동으로 찾아서 연결해주는 안전장치
            if (manager == null)
            {
                manager = FindFirstObjectByType<UnifiedCutInDialogueManager>();
                if (manager == null)
                {
                    Debug.LogError($"[{gameObject.name}] UnifiedCutInDialogueManager를 찾을 수 없습니다.");
                    IsOn = true;
                    return;
                }
            }

            if (sequenceSteps == null || sequenceSteps.Count == 0)
            {
                Debug.LogWarning($"[{gameObject.name}] 실행할 시퀀스 스텝이 존재하지 않습니다.");
                IsOn = true;
                return;
            }

            IsOn = false;
            
            // 매니저에게 업그레이드된 멀티 슬롯 시퀀스를 전달하여 실행합니다.
            manager.PlaySequence(sequenceSteps, OnSequenceCompleted);

            if (runInBackground)
            {
                IsOn = true;
            }
        }

        private void OnSequenceCompleted()
        {
            if (!runInBackground)
            {
                IsOn = true;
            }
        }
    }
}