using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace _Project.Scripts.VisualScripting
{
    public class RankingBoardUI : MonoBehaviour
    {
        [System.Serializable]
        public struct RankSlotUI
        {
            public TextMeshProUGUI rankText;
            public TextMeshProUGUI nameText;
            public TextMeshProUGUI timeText;
            public TextMeshProUGUI dateText;
        }

        [Header("─ 순위별 UI 텍스트 슬롯 (1위 ~ 5위) ─")]
        [SerializeField] private List<RankSlotUI> rankSlots = new List<RankSlotUI>();

        [Header("─ 현재 실시간 타이머 텍스트 (플레이 중 표시용, 선택사항) ─")]
        [SerializeField] private TextMeshProUGUI liveTimerText;

        private void OnEnable()
        {
            RefreshDisplay();
        }

        private void Update()
        {
            if (liveTimerText != null && LocalRankingManager.Instance != null && LocalRankingManager.Instance.IsTimerRunning)
            {
                liveTimerText.text = LocalRankingManager.FormatTime(LocalRankingManager.Instance.CurrentTimer);
            }
        }

        public void RefreshDisplay()
        {
            if (LocalRankingManager.Instance == null) return;

            List<RankingEntry> records = LocalRankingManager.Instance.GetRankings();

            for (int i = 0; i < rankSlots.Count; i++)
            {
                if (i < records.Count)
                {
                    if (rankSlots[i].rankText != null) rankSlots[i].rankText.text = $"{i + 1}";
                    if (rankSlots[i].nameText != null) rankSlots[i].nameText.text = records[i].playerName;
                    if (rankSlots[i].timeText != null) rankSlots[i].timeText.text = LocalRankingManager.FormatTime(records[i].clearTime);
                    if (rankSlots[i].dateText != null) rankSlots[i].dateText.text = records[i].dateString;
                }
                else
                {
                    // 빈 순위 슬롯 처리
                    if (rankSlots[i].rankText != null) rankSlots[i].rankText.text = $"{i + 1}";
                    if (rankSlots[i].nameText != null) rankSlots[i].nameText.text = "-";
                    if (rankSlots[i].timeText != null) rankSlots[i].timeText.text = "--:--.--";
                    if (rankSlots[i].dateText != null) rankSlots[i].dateText.text = "-";
                }
            }
        }
    }
}