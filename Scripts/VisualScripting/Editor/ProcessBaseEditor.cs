using UnityEngine;
using UnityEditor; // 이게 꼭 필요합니다.

namespace _Project.Scripts.VisualScripting
{
    // [CustomEditor] 속성: 이 에디터가 ProcessBase를 담당한다는 뜻입니다.
    // true 파라미터: ProcessBase를 상속받은 '모든 자식 클래스'에도 이 버튼을 띄우겠다는 뜻입니다. (핵심!)
    [CustomEditor(typeof(ProcessBase), true)]
    public class ProcessBaseEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 1. 기존 인스펙터 내용(변수들)을 그대로 그려줍니다.
            base.OnInspectorGUI();

            // 줄 바꿈 (디자인용)
            EditorGUILayout.Space();

            // 2. 타겟(현재 선택된 오브젝트) 가져오기
            ProcessBase process = (ProcessBase)target;

            // 3. 버튼 만들기 (레이아웃 자동 정렬됨)
            // GUIStyle을 사용해 버튼을 조금 더 크고 눈에 띄게 만듭니다.
            if (GUILayout.Button("강제 실행 (Execute)", GUILayout.Height(30)))
            {
                // 에디터 모드에서 실행하는 것이므로 Undo(실행 취소) 기록을 남길 수도 있습니다. (선택사항)
                // Undo.RecordObject(process, "Force Execute");

                process.ForceExecute();
            }
        }
    }
}