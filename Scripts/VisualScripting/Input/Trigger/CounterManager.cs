using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _Project.Scripts.Managers
{
    // 빈 오브젝트에 부착할 수 있도록 MonoBehaviour를 상속받습니다.
    public class CounterManager : MonoBehaviour
    {
        // 핵심: 변수와 함수는 static으로 유지하여, 
        // 맵 어디서든 CounterManager.Increment()를 그대로 쓸 수 있게 합니다.
        private static Dictionary<string, int> counters = new Dictionary<string, int>();

        public static void Increment(string counterName, int amount = 1)
        {
            if (string.IsNullOrEmpty(counterName)) return;

            if (!counters.ContainsKey(counterName))
            {
                counters[counterName] = 0;
            }

            counters[counterName] += amount;
        }

        public static int GetCount(string counterName)
        {
            return counters.ContainsKey(counterName) ? counters[counterName] : 0;
        }

        public static void ResetCounter(string counterName)
        {
            if (counters.ContainsKey(counterName))
            {
                counters[counterName] = 0;
            }
        }

        // --- 에디터(인스펙터)에서 데이터를 읽어오기 위한 전용 함수 ---
        public static Dictionary<string, int> GetAllCounters()
        {
            return counters;
        }
    }

    // ==========================================
    // 전역 카운터 대시보드 (Custom Editor)
    // ==========================================
#if UNITY_EDITOR
    [CustomEditor(typeof(CounterManager))]
    public class CounterManagerEditor : Editor
    {
        // 실시간으로 변하는 숫자를 그리기 위해 매 프레임 인스펙터 새로고침
        public override bool RequiresConstantRepaint()
        {
            return Application.isPlaying;
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(" 전역 카운터 대시보드", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("게임 내에서 생성된 모든 카운터를 실시간으로 추적합니다.");
            EditorGUILayout.Space(5);

            // 게임 실행 중이 아닐 때
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("게임을 실행(Play)하면, 신호가 들어올 때마다 이곳에 카운터가 자동으로 생성되고 기록됩니다.", MessageType.Info);
                return;
            }

            var allCounters = CounterManager.GetAllCounters();

            // 생성된 카운터가 없을 때
            if (allCounters.Count == 0)
            {
                EditorGUILayout.HelpBox("아직 작동한 카운터가 없습니다.", MessageType.Warning);
                return;
            }

            // 딕셔너리에 저장된 모든 카운터를 가로 형태의 표로 그려줍니다.
            foreach (var kvp in allCounters)
            {
                EditorGUILayout.BeginHorizontal("box");

                // 카운터 이름
                EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(150));

                // 누적된 숫자
                EditorGUILayout.LabelField(kvp.Value.ToString(), EditorStyles.boldLabel, GUILayout.Width(50));

                // 테스트용 초기화 버튼
                if (GUILayout.Button("초기화", GUILayout.Width(60)))
                {
                    CounterManager.ResetCounter(kvp.Key);
                }

                EditorGUILayout.EndHorizontal();
            }
        }
    }
#endif
}