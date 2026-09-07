using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    public class SimpleCutInOutput : ProcessBase
    {
        [Header("Global Settings")]
        [Tooltip("CutIn 진행을 담당할 매니저 (비워둘 경우 씬 내 비활성화된 매니저까지 자동 탐색합니다)")]
        [SerializeField] private SimpleCutInManager manager;

        [Tooltip("텍스트 데이터 테이블")]
        [SerializeField] private CutInStringTable stringTable;

        [Header("Sequence Steps")]
        [SerializeField] private List<CutInStepData> sequenceSteps = new List<CutInStepData>();

        [Header("Progression Settings")]
        [Tooltip("시퀀스 완료를 기다리지 않고 즉시 다음 노드를 실행할지 여부")]
        [SerializeField] private bool runInBackground = false;

        private void Awake()
        {
            // 씬 로드 시 미리 매니저 탐색
            FindManagerIfNull();
        }

        public override void Execute()
        {
            FindManagerIfNull();

            if (manager == null)
            {
                Debug.LogError($"[{gameObject.name}] SimpleCutInManager를 찾을 수 없습니다. 씬에 SimpleCutInManager가 존재하는지 확인하세요.");
                IsOn = true;
                return;
            }

            if (sequenceSteps == null || sequenceSteps.Count == 0)
            {
                Debug.LogWarning($"[{gameObject.name}] 컷인 시퀀스 스텝이 없습니다.");
                IsOn = true;
                return;
            }

            IsOn = false;
            manager.PlaySequence(sequenceSteps, stringTable, OnSequenceCompleted);

            if (runInBackground)
            {
                IsOn = true;
            }
        }

        /// <summary>
        /// manager가 null일 경우, 씬의 비활성화(Hide)된 Canvas/GameObject까지 포함하여 매니저를 탐색합니다.
        /// </summary>
        private void FindManagerIfNull()
        {
            if (manager == null)
            {
                manager = FindFirstObjectByType<SimpleCutInManager>(FindObjectsInactive.Include);
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