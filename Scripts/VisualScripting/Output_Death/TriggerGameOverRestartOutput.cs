using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 게임오버 시 체크포인트 번호를 확정하고 현재 씬을 재로드하는 노드입니다.
    /// </summary>
    public class TriggerGameOverRestartOutput : ProcessBase
    {
        [Header("Checkpoint Settings")]
        [Tooltip("0: 직전에 도달했던 체크포인트 유지\n1 ~ 5: 이 함정/구역에서 사망 시 부활할 체크포인트 번호 강제 지정")]
        [Range(0, 5)]
        [SerializeField] private int assignCheckpointOnDeath = 0;

        [Header("Reload Settings")]
        [Tooltip("사망 연출/사운드 재생 후 씬을 재로드할 대기 시간 (초)")]
        [SerializeField] private float reloadDelay = 0.5f;

        public override void Execute()
        {
            // 1. 체크포인트 갱신 (지정값이 1~5인 경우)
            if (assignCheckpointOnDeath >= 1 && assignCheckpointOnDeath <= 5)
            {
                SceneRestartState.SetCheckpoint(assignCheckpointOnDeath);
            }

            // 2. 게임오버 재시작 플래그 활성화
            SceneRestartState.IsRestartedFromGameOver = true;

            // 3. 씬 재로드
            StartCoroutine(ReloadSceneRoutine());
        }

        private IEnumerator ReloadSceneRoutine()
        {
            if (reloadDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(reloadDelay);
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}