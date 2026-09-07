using System.Reflection;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정한 스크립트 내부의 특정 boolean(참/거짓) 변수를 찾아, 꺼져(false)있다면 켜주는(true) Output 모듈
    /// </summary>
    public class ActivateBooleanOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("값을 변경할 대상 스크립트(컴포넌트)를 드래그해서 넣으세요.")]
        [SerializeField] private MonoBehaviour targetScript;

        [Tooltip("변경할 boolean 변수의 정확한 이름을 입력하세요. (대소문자 구분)")]
        [SerializeField] private string variableName = "";

        [Tooltip("값을 true(켜기)로 할지, false(끄기)로 할지 선택합니다.")]
        [SerializeField] private bool setToActivate = true;

        public override void Execute()
        {
            // 타겟이 없거나 변수 이름이 비어있으면 실행 취소
            if (targetScript == null || string.IsNullOrEmpty(variableName))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> Target Script나 Variable Name이 설정되지 않았습니다.");
#endif
                return;
            }

            // 타겟 스크립트의 타입(클래스 정보)을 가져옴
            System.Type type = targetScript.GetType();
            bool valueChanged = false;

            // 탐색 조건: public, private, protected 모두 포함하여 인스턴스 변수 탐색
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            // 1. 일반 변수(Field) 탐색
            FieldInfo field = type.GetField(variableName, flags);
            if (field != null && field.FieldType == typeof(bool))
            {
                bool currentValue = (bool)field.GetValue(targetScript);

                // 현재 값과 바꾸려는 값이 다를 때만 변경 적용
                if (currentValue != setToActivate)
                {
                    field.SetValue(targetScript, setToActivate);
                    valueChanged = true;
                }
            }
            else
            {
                // 2. 프로퍼티(Property, 예: public bool IsOpened { get; set; }) 탐색
                PropertyInfo prop = type.GetProperty(variableName, flags);
                if (prop != null && prop.PropertyType == typeof(bool) && prop.CanWrite)
                {
                    bool currentValue = (bool)prop.GetValue(targetScript);

                    if (currentValue != setToActivate)
                    {
                        prop.SetValue(targetScript, setToActivate);
                        valueChanged = true;
                    }
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"<color=orange>[{gameObject.name}]</color> {targetScript.name}에서 '{variableName}' 이름의 bool 변수/프로퍼티를 찾을 수 없습니다. 철자를 확인하세요.");
#endif
                    return;
                }
            }

            // 변경이 성공적으로 이루어졌다면
            if (valueChanged)
            {
#if UNITY_EDITOR
                Debug.Log($"<color=green>[{gameObject.name}]</color> {targetScript.GetType().Name}의 '{variableName}' 값을 {setToActivate}로 변경했습니다.");
#endif
            }

            // 프레임워크 상태 갱신 (다음 연결된 노드 실행)
            IsOn = true;
        }
    }
}