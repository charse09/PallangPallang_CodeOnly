using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 씬 재로드 시에도 유지되는 1~5번 체크포인트 및 게임오버 상태 관리자입니다.
    /// </summary>
    public static class SceneRestartState
    {
        /// <summary>
        /// 0 = 일반 시작 (첫 플레이)
        /// 1 ~ 5 = 각 체크포인트 구간
        /// </summary>
        public static int ActiveCheckpoint { get; set; } = 0;

        /// <summary>
        /// 방금 씬 로드가 게임오버로 인한 리스타트인지 여부
        /// </summary>
        public static bool IsRestartedFromGameOver { get; set; } = false;

        /// <summary>
        /// 특정 체크포인트 활성화 (1 ~ 5)
        /// </summary>
        public static void SetCheckpoint(int checkpointIndex)
        {
            ActiveCheckpoint = Mathf.Clamp(checkpointIndex, 1, 5);
        }

        /// <summary>
        /// 스테이지 클리어 또는 씬 퇴장 시 모든 상태를 일반 시작(0)으로 완전 초기화
        /// </summary>
        public static void ResetAll()
        {
            ActiveCheckpoint = 0;
            IsRestartedFromGameOver = false;
        }
    }
}