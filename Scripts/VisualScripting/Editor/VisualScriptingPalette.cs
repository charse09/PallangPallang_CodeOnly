/*using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using _Project.Scripts.VisualScripting;

namespace _Project.Scripts.VisualScripting.Editor
{
    public class VisualScriptingPalette : EditorWindow
    {
        private Dictionary<string, List<Type>> _categorizedNodes = new Dictionary<string, List<Type>>();
        private Vector2 _scrollPosition;

        [MenuItem("Visual Scripting/GameObject/Pallete Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<VisualScriptingPalette>("Visual Scripting Pallete Window");
            window.Show();
        }

        private void OnEnable()
        {
            RefreshNodeList();
        }

        private void RefreshNodeList()
        {
            _categorizedNodes.Clear();

            var nodeTypes = TypeCache.GetTypesDerivedFrom<ProcessBase>()
                .Where(t => !t.IsAbstract && !t.IsInterface);

            foreach (Type type in nodeTypes)
            {
                string[] guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");

                // 기본값
                string categoryName = "ETC (미분류)";

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (path.EndsWith($"/{type.Name}.cs"))
                    {
                        // [핵심 변경점] 하위 폴더 깊이에 상관없이, 경로에 포함된 대분류 키워드만 찾습니다.
                        if (path.Contains("/Input/")) categoryName = "Input";
                        else if (path.Contains("/Logic/")) categoryName = "Logic";
                        else if (path.Contains("/Output/")) categoryName = "Output";
                        else if (path.Contains("/Output_Camera/")) categoryName = "Output_Camera";
                        else if (path.Contains("/Output_Choice/")) categoryName = "Output_Choice";
                        else if (path.Contains("/Output_Damage/")) categoryName = "Output_Damage";
                        else if (path.Contains("/Output_Effect/")) categoryName = "Output_Effect";
                        else if (path.Contains("/Output_Input/")) categoryName = "Output_Input";
                        else if (path.Contains("/Output_Light/")) categoryName = "Output_Light";
                        else if (path.Contains("/Output_Movement/")) categoryName = "Output_Movement";
                        else if (path.Contains("/Output_Object/")) categoryName = "Output_Object";
                        else if (path.Contains("/Output_Physics/")) categoryName = "Output_Physics";
                        else if (path.Contains("/Output_Sound/")) categoryName = "Output_Sound";
                        else if (path.Contains("/Output_Spawn/")) categoryName = "Output_Spawn";
                        else if (path.Contains("/Output_Spawn_Physics/")) categoryName = "Output_Spawn_Physics";
                        else if (path.Contains("/Output_StatusEffect/")) categoryName = "Output_StatusEffect";
                        else if (path.Contains("/Output_TimeEffect/")) categoryName = "Output_TimeEffect";
                        else if (path.Contains("/Output_UI/")) categoryName = "Output_UI";
                        else if (path.Contains("/Output_Variable/")) categoryName = "Output_Variable";
                        break;
                    }
                }

                if (!_categorizedNodes.ContainsKey(categoryName))
                {
                    _categorizedNodes[categoryName] = new List<Type>();
                }
                _categorizedNodes[categoryName].Add(type);
            }
        }

        // 카테고리 정렬 순서를 지정하는 헬퍼 함수
        private int GetCategoryOrder(string category)
        {
            if (category == "Input") return 1;
            if (category == "Logic") return 2;
            if (category == "Output") return 3;
            if (category == "Output_Camera") return 4;
            if (category == "Output_Choice") return 5;
            if (category == "Output_Damage") return 6;
            if (category == "Output_Effect") return 7;
            if (category == "Output_Input") return 8;
            if (category == "Output_Light") return 9;
            if (category == "Output_Movement") return 10;
            if (category == "Output_Object") return 11;
            if (category == "Output_Physics") return 12;
            if (category == "Output_Sound") return 13;
            if (category == "Output_Spawn") return 14;
            if (category == "Output_Spawn_Physics") return 15;
            if (category == "Output_StatusEffect") return 16;
            if (category == "Output_TimeEffect") return 17;
            if (category == "Output_UI") return 18;
            if (category == "Output_Variable") return 19;
            return 20; // ETC 등 나머지는 맨 아래로
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            if (GUILayout.Button("🔄 리스트 새로고침 (Refresh)", GUILayout.Height(30)))
            {
                RefreshNodeList();
            }
            GUILayout.Space(10);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // 알파벳 순서가 아닌, GetCategoryOrder에 정의된 논리적 흐름(Input -> Process -> Output) 순으로 정렬
            var orderedCategories = _categorizedNodes.OrderBy(k => GetCategoryOrder(k.Key));

            foreach (var category in orderedCategories)
            {
                EditorGUILayout.LabelField($"📂 {category.Key}", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                foreach (Type nodeType in category.Value.OrderBy(t => t.Name))
                {
                    if (GUILayout.Button($"➕ {nodeType.Name}", GUILayout.Height(25)))
                    {
                        CreateNode(nodeType, category.Key);
                    }
                }

                EditorGUI.indentLevel--;
                GUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();
        }

        private void CreateNode(Type nodeType, string category)
        {
            GameObject go = new GameObject(nodeType.Name);
            go.AddComponent(nodeType);

            // Input 그룹에 속해있거나 이름에 Trigger가 있다면 자동으로 설정
            if (nodeType.Name.Contains("Trigger"))
            {
                BoxCollider col = go.AddComponent<BoxCollider>();
                col.isTrigger = true;
            }

            GameObject activeObj = Selection.activeGameObject;
            if (activeObj != null)
            {
                GameObjectUtility.SetParentAndAlign(go, activeObj);
            }

            Undo.RegisterCreatedObjectUndo(go, $"Create {nodeType.Name}");
            Selection.activeObject = go;
        }
    }
}*/

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.VisualScripting.Editor
{
    public class VisualScriptingPalette : EditorWindow
    {
        private Dictionary<string, List<Type>> _categorizedNodes = new Dictionary<string, List<Type>>();
        private Vector2 _scrollPosition;

        // 검색 기능을 위한 변수 추가
        private string _searchQuery = "";

        [MenuItem("Visual Scripting/GameObject/Pallete Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<VisualScriptingPalette>("Visual Scripting Pallete Window");
            window.Show();
        }

        private void OnEnable()
        {
            RefreshNodeList();
        }

        private void RefreshNodeList()
        {
            _categorizedNodes.Clear();

            var nodeTypes = TypeCache.GetTypesDerivedFrom<ProcessBase>()
                .Where(t => !t.IsAbstract && !t.IsInterface);

            foreach (Type type in nodeTypes)
            {
                string[] guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");
                string categoryName = "ETC (미분류)";

                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);

                    if (path.EndsWith($"/{type.Name}.cs"))
                    {
                        // 폴더 구조를 기반으로 카테고리 파싱 (예: VisualScripting/Output_Camera/.. 이면 Output_Camera)
                        string[] parts = path.Split('/');
                        if (parts.Length > 2)
                        {
                            categoryName = parts[parts.Length - 2];
                        }
                        break;
                    }
                }

                if (!_categorizedNodes.ContainsKey(categoryName))
                {
                    _categorizedNodes[categoryName] = new List<Type>();
                }
                _categorizedNodes[categoryName].Add(type);
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Visual Scripting Node Palette", EditorStyles.boldLabel);
            GUILayout.Space(5);

            // --- 🔍 검색창 UI 추가 ---
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("🔍 검색:", GUILayout.Width(50));

            // 텍스트가 바뀔 때마다 OnGUI가 실시간으로 갱신되며 리스트를 필터링합니다.
            string prevSearch = _searchQuery;
            _searchQuery = EditorGUILayout.TextField(_searchQuery);

            // 검색어를 지우는 'X' 버튼
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                _searchQuery = "";
                GUI.FocusControl(null); // 검색창 포커스 해제
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(10);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // 카테고리 순서 정렬
            var orderedCategories = _categorizedNodes.OrderBy(k => GetCategoryOrder(k.Key));

            foreach (var category in orderedCategories)
            {
                // 현재 검색어 조건에 맞는 노드들만 필터링
                var filteredNodes = category.Value
                    .Where(t => string.IsNullOrEmpty(_searchQuery) || t.Name.IndexOf(_searchQuery, StringComparison.OrdinalIgnoreCase) >= 0)
                    .OrderBy(t => t.Name)
                    .ToList();

                // 이 카테고리 안에 검색 조건에 맞는 노드가 하나도 없다면 카테고리 타이틀 자체를 그리지 않음
                if (filteredNodes.Count == 0) continue;

                EditorGUILayout.LabelField($"📂 {category.Key}", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;

                foreach (Type nodeType in filteredNodes)
                {
                    if (GUILayout.Button($"➕ {nodeType.Name}", GUILayout.Height(25)))
                    {
                        CreateNode(nodeType, category.Key);
                    }
                }

                EditorGUI.indentLevel--;
                GUILayout.Space(10);
            }

            EditorGUILayout.EndScrollView();
        }

        // 기존 카테고리 정렬 순서 보장용 헬퍼 함수 (구조에 맞게 순서 수치 커스텀 가능)
        private int GetCategoryOrder(string categoryName)
        {
            if (categoryName.StartsWith("Input")) return 0;
            if (categoryName.StartsWith("Logic")) return 1;
            if (categoryName.StartsWith("Output")) return 2;
            return 99;
        }

        private void CreateNode(Type nodeType, string category)
        {
            GameObject go = new GameObject(nodeType.Name);
            go.AddComponent(nodeType);

            if (nodeType.Name.Contains("Trigger"))
            {
                BoxCollider col = go.AddComponent<BoxCollider>();
                col.isTrigger = true;
            }

            GameObject activeObj = Selection.activeGameObject;
            if (activeObj != null)
            {
                GameObjectUtility.SetParentAndAlign(go, activeObj);
            }

            Undo.RegisterCreatedObjectUndo(go, $"Create {nodeType.Name}");
            Selection.activeObject = go;
        }
    }
}