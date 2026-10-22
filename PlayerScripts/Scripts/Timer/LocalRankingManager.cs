using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    [Serializable]
    public class RankingEntry
    {
        public string playerName;
        public float clearTime;
        public string dateString;
    }

    [Serializable]
    public class RankingDataWrapper
    {
        public List<RankingEntry> entries = new List<RankingEntry>();
    }

    public class LocalRankingManager : MonoBehaviour
    {
        public static LocalRankingManager Instance { get; private set; }

        private const string PREFS_KEY = "LOCAL_SPEEDRUN_RANKING";
        private const int MAX_RANK_COUNT = 5;

        [Header("─ 디버그 / 옵션 ─")]
        [Tooltip("체크 시 별도 트리거 신호가 없어도 씬이 시작되자마자 타이머를 즉시 작동시킵니다.")]
        [SerializeField] private bool autoStartOnAwake = true;

        [Tooltip("현재 실시간으로 흘러가는 시간(초) - 인스펙터 확인용")]
        [SerializeField] private float currentTimer = 0f;

        [SerializeField] private bool isTimerRunning = false;

        private RankingDataWrapper _rankingData = new RankingDataWrapper();

        public float CurrentTimer => currentTimer;
        public bool IsTimerRunning => isTimerRunning;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadRankingData();

                if (autoStartOnAwake)
                {
                    StartTimer();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (isTimerRunning)
            {
                currentTimer += Time.deltaTime;
            }
        }

        public void StartTimer()
        {
            currentTimer = 0f;
            isTimerRunning = true;
            Debug.Log("<color=green>[LocalRankingManager]</color> 타이머 시작됨!");
        }

        public float StopTimer()
        {
            isTimerRunning = false;
            Debug.Log($"<color=yellow>[LocalRankingManager]</color> 타이머 정지됨! 최종 시간: {FormatTime(currentTimer)} ({currentTimer}초)");
            return currentTimer;
        }

        public void ResetTimer()
        {
            currentTimer = 0f;
            isTimerRunning = false;
        }

        public int SubmitRecord(string playerName, float time)
        {
            var newEntry = new RankingEntry
            {
                playerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName,
                clearTime = time,
                dateString = DateTime.Now.ToString("yyyy.MM.dd")
            };

            _rankingData.entries.Add(newEntry);
            _rankingData.entries.Sort((a, b) => a.clearTime.CompareTo(b.clearTime));

            int rank = _rankingData.entries.IndexOf(newEntry) + 1;

            if (_rankingData.entries.Count > MAX_RANK_COUNT)
            {
                _rankingData.entries.RemoveRange(MAX_RANK_COUNT, _rankingData.entries.Count - MAX_RANK_COUNT);
            }

            SaveRankingData();
            return rank <= MAX_RANK_COUNT ? rank : -1;
        }

        public List<RankingEntry> GetRankings()
        {
            return _rankingData.entries;
        }

        public static string FormatTime(float timeInSeconds)
        {
            if (timeInSeconds < 0f) timeInSeconds = 0f;
            int minutes = (int)(timeInSeconds / 60f);
            int seconds = (int)(timeInSeconds % 60f);
            int milliseconds = (int)((timeInSeconds * 100f) % 100f);
            return $"{minutes:00}:{seconds:00}.{milliseconds:00}";
        }

        private void SaveRankingData()
        {
            string json = JsonUtility.ToJson(_rankingData);
            PlayerPrefs.SetString(PREFS_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadRankingData()
        {
            if (PlayerPrefs.HasKey(PREFS_KEY))
            {
                string json = PlayerPrefs.GetString(PREFS_KEY);
                _rankingData = JsonUtility.FromJson<RankingDataWrapper>(json);
            }
            else
            {
                _rankingData = new RankingDataWrapper();
            }
        }
    }
}