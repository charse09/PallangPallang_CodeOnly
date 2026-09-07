using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace _Project.Scripts.Systems
{
    // 몬스터나 플레이어에게 붙여서 상태이상을 관리하는 컴포넌트
    public class StatusEffectHandler : MonoBehaviour
    {
        // 대상에게 상태이상이 적용될 때 발생하는 이벤트 (상태이상 이름 전달)
        public event Action<string> OnEffectApplied;

        // 독 화살을 맞거나 스턴 스킬에 당했을 때 이 함수를 호출합니다.
        public void ApplyEffect(string effectName)
        {
            if (string.IsNullOrEmpty(effectName)) return;

            Debug.Log($"<color=magenta>[StatusEffect]</color> {gameObject.name}이(가) '{effectName}' 상태이상에 걸렸습니다!");

            // 이 대상의 상태이상을 감시하고 있는 트리거들에게 알림
            OnEffectApplied?.Invoke(effectName);
        }
    }

    // ==========================================
    // 인스펙터 테스트 버튼 (파일 하단 병합)
    // ==========================================
#if UNITY_EDITOR
    [CustomEditor(typeof(StatusEffectHandler))]
    public class StatusEffectHandlerEditor : Editor
    {
        // 자유 입력 테스트를 위한 임시 문자열 변수
        private string customEffectName = "Burn";

        public override void OnInspectorGUI()
        {
            // 기본 변수들(만약 생긴다면) 표시
            DrawDefaultInspector();

            StatusEffectHandler handler = (StatusEffectHandler)target;

            EditorGUILayout.Space(15);
            EditorGUILayout.LabelField("상태이상 즉시 테스트", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("게임을 실행(Play)한 상태에서 아래 버튼을 눌러 트리거 작동을 테스트하세요.", MessageType.Info);

            // 1. 고정된 상태이상 테스트 버튼들
            EditorGUILayout.BeginHorizontal();

            // GUILayout.Height(30)을 주어 버튼을 큼직하게 만듭니다.
            if (GUILayout.Button("기절 (Stun)", GUILayout.Height(30)))
            {
                handler.ApplyEffect("Stun");
            }
            if (GUILayout.Button("독 (Poison)", GUILayout.Height(30)))
            {
                handler.ApplyEffect("Poison");
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // 2. 자유 입력 상태이상 테스트
            EditorGUILayout.BeginHorizontal("box");

            customEffectName = EditorGUILayout.TextField("직접 입력:", customEffectName);

            if (GUILayout.Button("적용", GUILayout.Width(60)))
            {
                handler.ApplyEffect(customEffectName);
            }

            EditorGUILayout.EndHorizontal();
        }
    }
#endif
}