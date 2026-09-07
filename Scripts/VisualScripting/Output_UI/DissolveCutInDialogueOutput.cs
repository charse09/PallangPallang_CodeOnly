using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    public class DissolveCutInDialogueOutput : ProcessBase
    {
        [Header("Global Setting")]
        [Tooltip("씬에 배치한 DissolveCutInDialogueManager 레퍼런스")]
        [SerializeField] private DissolveCutInDialogueManager manager;

        [Header("Cut-In & Dialogue Sequence")]
        [Tooltip("디졸브 전환 기능이 새로 추가된 대사 시퀀스를 설계합니다.")]
        [SerializeField] private List<DissolveCutInDialogueManager.CutInDialogueStep> sequenceSteps = new List<DissolveCutInDialogueManager.CutInDialogueStep>();

        [Header("Progression Options")]
        [Tooltip("대화 시퀀스가 끝날 때까지 대기하지 않고 즉시 다음 노드를 실행할지 여부")]
        [SerializeField] private bool runInBackground = false;

        public override void Execute()
        {
            if (manager == null)
            {
                manager = FindFirstObjectByType<DissolveCutInDialogueManager>();
                if (manager == null)
                {
                    Debug.LogError($"[{gameObject.name}] DissolveCutInDialogueManager를 찾을 수 없습니다.");
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