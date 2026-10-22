using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 기믹을 통과하거나 특정 구역에 도달했을 때 활성 리스타트 체크포인트 번호(1~5)를 갱신하는 노드입니다.
    /// </summary>
    public class SetRestartCheckpointOutput : ProcessBase
    {
        [Tooltip("새로 등록할 체크포인트 번호 (1 ~ 5)")]
        [Range(1, 5)]
        [SerializeField] private int newCheckpointIndex = 1;

        public override void Execute()
        {
            SceneRestartState.SetCheckpoint(newCheckpointIndex);
            IsOn = true; // 다음 노드로 완료 신호 전달
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}