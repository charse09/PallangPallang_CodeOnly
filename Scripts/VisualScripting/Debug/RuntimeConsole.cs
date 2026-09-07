using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace _Project.Scripts.VisualScripting
{
    public class RuntimeConsole : MonoBehaviour
    {
        private class LogEntry
        {
            public string message;
            public Color color;
            public int count = 1;
        }

        [Header("Settings")]
        public KeyCode toggleKey = KeyCode.BackQuote;
        public int maxLogs = 1000;

        // 인스펙터에서 켜고 꺼지는지 확인하실 수 있도록 SerializeField 추가
        [SerializeField] private bool isVisible = false;

        private List<LogEntry> logs = new List<LogEntry>();
        private Vector2 scrollPosition;

        private bool collapse = false;
        private bool autoScroll = true;
        private bool showErrorsOnly = false;

        private void OnEnable()
        {
            // 유니티 엔진의 모든 로그(Debug.Log, 에러, 예외 등)를 캡처하는 핵심 이벤트 구독
            Application.logMessageReceived += HandleUnityLog;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= HandleUnityLog;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                isVisible = !isVisible;
            }
        }

        // 유니티 시스템에서 전달받는 로그 처리 함수
        private void HandleUnityLog(string logString, string stackTrace, LogType type)
        {
            Color logColor = Color.white;
            string finalMessage = logString;

            // LogType에 따른 색상 및 텍스트 분류
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    logColor = Color.red;
                    // 예외(Exception)의 경우 스택 트레이스도 함께 보여주면 디버깅에 유리합니다.
                    if (type == LogType.Exception)
                    {
                        finalMessage = $"{logString}\n<size=11>{stackTrace}</size>";
                    }
                    break;
                case LogType.Warning:
                    logColor = Color.yellow;
                    break;
                case LogType.Log:
                    logColor = Color.white;
                    break;
            }

            logs.Add(new LogEntry { message = finalMessage, color = logColor, count = 1 });

            if (logs.Count > maxLogs) logs.RemoveAt(0);
            if (autoScroll) scrollPosition.y = float.MaxValue;
        }

        private void OnGUI()
        {
            if (!isVisible) return;

            float windowWidth = Screen.width * 0.9f;
            float windowHeight = Screen.height * 0.5f;
            Rect windowRect = new Rect(10, Screen.height - windowHeight - 10, windowWidth, windowHeight);

            GUI.Box(windowRect, "Runtime Console (Press '~' to hide)");

            GUILayout.BeginArea(new Rect(windowRect.x + 10, windowRect.y + 25, windowRect.width - 20, 30));
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Clear", GUILayout.Width(60))) logs.Clear();
            collapse = GUILayout.Toggle(collapse, "Collapse", GUILayout.Width(80));
            autoScroll = GUILayout.Toggle(autoScroll, "Auto Scroll", GUILayout.Width(90));
            showErrorsOnly = GUILayout.Toggle(showErrorsOnly, "Errors Only", GUILayout.Width(90));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Save to Log.txt", GUILayout.Width(120))) SaveLogToFile();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            Rect scrollRect = new Rect(windowRect.x + 10, windowRect.y + 60, windowRect.width - 20, windowRect.height - 70);
            List<LogEntry> displayLogs = GetDisplayLogs();

            // 예외 메시지 등 여러 줄의 텍스트를 지원하기 위해 동적 높이 계산 사용
            GUIContent[] contents = new GUIContent[displayLogs.Count];
            float totalHeight = 0;
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };

            for (int i = 0; i < displayLogs.Count; i++)
            {
                string text = displayLogs[i].message;
                if (collapse && displayLogs[i].count > 1) text = $"[{displayLogs[i].count}] {text}";
                contents[i] = new GUIContent(text);
                totalHeight += labelStyle.CalcHeight(contents[i], scrollRect.width - 20);
            }

            Rect contentRect = new Rect(0, 0, scrollRect.width - 20, totalHeight);
            scrollPosition = GUI.BeginScrollView(scrollRect, scrollPosition, contentRect);

            float yPos = 0;
            for (int i = 0; i < displayLogs.Count; i++)
            {
                Color originalColor = GUI.contentColor;
                GUI.contentColor = displayLogs[i].color;

                float height = labelStyle.CalcHeight(contents[i], scrollRect.width - 20);
                GUI.Label(new Rect(0, yPos, contentRect.width, height), contents[i], labelStyle);

                GUI.contentColor = originalColor;
                yPos += height;
            }

            GUI.EndScrollView();
        }

        private List<LogEntry> GetDisplayLogs()
        {
            List<LogEntry> result = new List<LogEntry>();
            Dictionary<string, LogEntry> collapsedDict = new Dictionary<string, LogEntry>();

            foreach (var log in logs)
            {
                if (showErrorsOnly && log.color != Color.red) continue;

                if (collapse)
                {
                    if (collapsedDict.ContainsKey(log.message))
                    {
                        collapsedDict[log.message].count++;
                    }
                    else
                    {
                        LogEntry newEntry = new LogEntry { message = log.message, color = log.color, count = 1 };
                        collapsedDict[log.message] = newEntry;
                        result.Add(newEntry);
                    }
                }
                else
                {
                    result.Add(log);
                }
            }
            return result;
        }

        private void SaveLogToFile()
        {
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filePath = Path.Combine(Application.persistentDataPath, $"RuntimeLog_{timestamp}.txt");

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false))
                {
                    writer.WriteLine("--- Runtime Log Export ---");
                    writer.WriteLine($"Date: {DateTime.Now}");
                    writer.WriteLine("--------------------------\n");

                    foreach (var log in logs)
                    {
                        writer.WriteLine(log.message);
                    }
                }
                Debug.Log($"Log successfully saved to: {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save log: {e.Message}");
            }
        }
    }
}