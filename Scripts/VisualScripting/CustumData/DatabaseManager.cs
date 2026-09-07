using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Data
{
    public class DatabaseManager : MonoBehaviour
    {
        private Dictionary<string, TableData> _tables = new Dictionary<string, TableData>();

        [Header("Debug Settings")]
        [SerializeField] private bool showDebugGUI = true;
        private bool _isMinimized = false;
        private Vector2 _scrollPos;
        private Rect _windowRect = new Rect(20, 20, 450, 550);

        public void LoadTableFromPath(string tableName, string path)
        {
            TextAsset csvFile = Resources.Load<TextAsset>(path);
            if (csvFile == null)
            {
                Debug.LogError($"[DatabaseManager] 파일을 찾을 수 없습니다: {path}");
                return;
            }

            string[] lines = csvFile.text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 3) return;

            string[] headers = lines[0].Trim().Split(',');
            string[] types = lines[1].Trim().Split(','); // 컬럼 타입 행 (struct, ref, int32 등)

            TableData newTable = new TableData();
            for (int i = 2; i < lines.Length; i++)
            {
                string[] values = lines[i].Trim().Split(',');
                if (values.Length < headers.Length) continue;

                RowEntity row = new RowEntity();
                for (int j = 0; j < headers.Length; j++)
                {
                    // 1. 값 파싱
                    object parsedValue = CSVParser.ParseValue(types[j], values[j]);

                    // 2. 값과 함께 '타입 정보'를 RowEntity에 저장 (핵심 변경 사항)
                    row.SetField(headers[j], parsedValue, types[j]);
                }
                newTable.AddRow(values[0], row);
            }

            _tables[tableName] = newTable;
            Debug.Log($"<color=white>[DatabaseManager]</color> 테이블 로드 완료: <b>{tableName}</b>");
        }

        public TableData GetTable(string tableName) => _tables.GetValueOrDefault(tableName);
        public void ClearDatabase() => _tables.Clear();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!showDebugGUI) return;
            _windowRect = GUILayout.Window(1, _windowRect, DrawWindow, "<b>[ Database Runtime Debugger ]</b>");
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_isMinimized ? "Maximize" : "Minimize")) _isMinimized = !_isMinimized;
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("X", GUILayout.Width(25))) showDebugGUI = false;
            GUILayout.EndHorizontal();

            if (_isMinimized) { GUI.DragWindow(); return; }

            if (GUILayout.Button("Reload From Game Manager", GUILayout.Height(30)))
            {
                // GameManager.instance?.ReloadAllData();
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            foreach (var tableKvp in _tables)
            {
                DrawTableGUI(tableKvp.Key, tableKvp.Value);
            }
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }

        private void DrawTableGUI(string name, TableData table)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUI.contentColor = Color.cyan;
            GUILayout.Label($"<b>Table: {name}</b>");
            GUI.contentColor = Color.white;

            foreach (var row in table.AllRows)
            {
                string rowText = "";
                foreach (var col in row.GetColumns())
                {
                    // 타입 정보를 함께 표시하여 디버깅 용이하게 개선
                    string typeInfo = row.GetColumnType(col);
                    rowText += $"<color=silver>{col}({typeInfo}):</color>{row.Get<object>(col)}  ";
                }

                // 해결책: 런타임용 커스텀 스타일을 생성해서 글씨 크기를 줄입니다.
                GUIStyle runtimeMiniLabel = new GUIStyle(GUI.skin.label);
                runtimeMiniLabel.fontSize = 11; // 원하는 작은 글씨 크기로 설정
                runtimeMiniLabel.richText = true; // <color> 태그가 작동하도록 허용

                GUILayout.Label(rowText, runtimeMiniLabel);
            }
            GUILayout.EndVertical();
        }
#endif
    }
}