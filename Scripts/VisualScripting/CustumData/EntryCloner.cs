using System;
using System.Collections.Generic;
using UnityEngine;
using _Project.Scripts.Data;

public class EntryCloner : MonoBehaviour
{
    private Dictionary<string, object> _runtimeStats = new Dictionary<string, object>();

    [Header("Settings")]
    public string targetTable = "Character";
    public string targetId; // Character ID (예: 1, 2)
    public bool autoInit = true;

    private void Start()
    {
        // if (autoInit && !string.IsNullOrEmpty(targetId))
            // InitFromDB(targetTable, targetId);
    }

    //public void InitFromDB(string tableName, string id)
    //{
    //    if (GameManager.instance == null) return;
    //    var db = GameManager.instance.DatabaseManager;
    //    var row = db.GetTable(tableName)?.GetRow(id);
    //    if (row == null) return;

    //    _runtimeStats.Clear();

    //    foreach (var col in row.GetColumns())
    //    {
    //        string type = row.GetColumnType(col);
    //        object val = row.Get<object>(col);

    //        if (type == "struct")
    //        {
    //            var subTable = db.GetTable(col); // "Class" 테이블
    //            if (subTable != null)
    //            {
    //                string searchVal = val.ToString();
    //                // 1. ID로 먼저 찾아보고, 없으면 'Name' 컬럼에서 찾음
    //                RowEntity subRow = subTable.GetRow(searchVal) ?? subTable.FindRowByColumn("Name", searchVal);

    //                if (subRow != null)
    //                {
    //                    foreach (var subCol in subRow.GetColumns())
    //                        _runtimeStats[subCol] = subRow.Get<object>(subCol);
    //                }
    //                else
    //                {
    //                    Debug.LogWarning($"[EntryCloner] '{searchVal}' 데이터를 {col} 테이블의 ID 또는 Name에서 찾을 수 없습니다.");
    //                }
    //            }
    //        }
    //        else
    //        {
    //            _runtimeStats[col] = val;
    //        }
    //    }
    //}

    public T GetStat<T>(string key)
    {
        if (_runtimeStats.TryGetValue(key, out object val))
            return (T)Convert.ChangeType(val, typeof(T));
        return default;
    }

    public void SetStat(string key, object val)
    {
        if (_runtimeStats.ContainsKey(key)) _runtimeStats[key] = val;
    }

    private void OnGUI()
    {
        if (_runtimeStats.Count == 0) return;
        Camera cam = GameManager.instance.MainRenderCamera;
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 2.2f);
        if (screenPos.z < 0) return;

        Rect rect = new Rect(screenPos.x - 75, Screen.height - screenPos.y, 160, _runtimeStats.Count * 22f);
        GUI.backgroundColor = new Color(0, 0, 0, 0.8f);
        GUILayout.BeginArea(rect, GUI.skin.box);
        foreach (var kvp in _runtimeStats)
        {
            GUILayout.Label($"<color=silver>{kvp.Key}:</color> <color=yellow>{kvp.Value}</color>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });
        }
        GUILayout.EndArea();
    }
}