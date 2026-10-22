using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class RankingTimerOutput : ProcessBase
    {
        public enum TimerCommand
        {
            [Tooltip("타이머를 0초로 리셋하고 즉시 시작합니다.")]
            StartTimer,

            [Tooltip("타이머를 멈추고 현재 시간을 랭킹 보드에 기록합니다.")]
            StopAndSubmitRecord,

            [Tooltip("단순히 타이머만 멈춥니다.")]
            StopTimerOnly,

            [Tooltip("타이머를 리셋합니다.")]
            ResetTimer
        }

        [Header("─ 실행 명령 ─")]
        [SerializeField] private TimerCommand command = TimerCommand.StartTimer;

        [Header("─ 기록 제출 설정 (StopAndSubmit 시 사용) ─")]
        [SerializeField] private string playerName = "Berry";

        [Tooltip("기록 제출 완료 후 UI 랭킹판을 즉시 갱신할 컴포넌트 (선택사항)")]
        [SerializeField] private RankingBoardUI rankingBoardUI;

        public override void Execute()
        {
            if (LocalRankingManager.Instance == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 씬에 LocalRankingManager가 존재하지 않습니다.");
                IsOn = true;
                return;
            }

            switch (command)
            {
                case TimerCommand.StartTimer:
                    LocalRankingManager.Instance.StartTimer();
                    break;

                case TimerCommand.StopAndSubmitRecord:
                    float clearTime = LocalRankingManager.Instance.StopTimer();
                    int rank = LocalRankingManager.Instance.SubmitRecord(playerName, clearTime);
                    Debug.Log($"<color=cyan>[Clear!]</color> 기록: {LocalRankingManager.FormatTime(clearTime)} / 순위: {(rank > 0 ? $"{rank}위" : "순위권 밖")}");

                    if (rankingBoardUI != null)
                    {
                        rankingBoardUI.RefreshDisplay();
                    }
                    break;

                case TimerCommand.StopTimerOnly:
                    LocalRankingManager.Instance.StopTimer();
                    break;

                case TimerCommand.ResetTimer:
                    LocalRankingManager.Instance.ResetTimer();
                    break;
            }

            IsOn = true;
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}