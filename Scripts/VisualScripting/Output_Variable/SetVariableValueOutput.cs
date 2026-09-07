using System;
using System.Reflection;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지원하는 변수 타입 목록
    /// </summary>
    public enum VariableType
    {
        Integer,    // 정수 (int)
        Float,      // 실수 (float)
        String,     // 문자열 (string)
        Boolean     // 참/거짓 (bool)
    }

    /// <summary>
    /// 지정한 스크립트 내부의 특정 변수(Field/Property)를 찾아 원하는 값으로 덮어씌우는 Output 모듈
    /// </summary>
    public class SetVariableValueOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("값을 변경할 대상 스크립트(컴포넌트)를 드래그해서 넣으세요.")]
        [SerializeField] private MonoBehaviour targetScript;

        [Tooltip("변경할 변수의 정확한 이름을 입력하세요. (대소문자 구분)")]
        [SerializeField] private string variableName = "hp";

        [Header("Value Settings")]
        [Tooltip("변경할 변수의 타입을 선택하세요.")]
        [SerializeField] private VariableType targetVariableType = VariableType.Integer;

        // 인스펙터에서 타입에 따라 값을 입력받기 위해 종류별로 변수를 준비합니다.
        [Tooltip("Integer를 선택했을 때 적용될 값")]
        [SerializeField] private int intValue = 0;

        [Tooltip("Float를 선택했을 때 적용될 값")]
        [SerializeField] private float floatValue = 0f;

        [Tooltip("String을 선택했을 때 적용될 값")]
        [SerializeField] private string stringValue = "";

        [Tooltip("Boolean을 선택했을 때 적용될 값")]
        [SerializeField] private bool boolValue = false;

        public override void Execute()
        {
            if (targetScript == null || string.IsNullOrEmpty(variableName))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> Target Script나 Variable Name이 설정되지 않았습니다.");
#endif
                return;
            }

            // 인스펙터에서 선택한 타입에 따라 세팅할 실제 값을 결정합니다.
            object valueToSet = null;
            switch (targetVariableType)
            {
                case VariableType.Integer: valueToSet = intValue; break;
                case VariableType.Float: valueToSet = floatValue; break;
                case VariableType.String: valueToSet = stringValue; break;
                case VariableType.Boolean: valueToSet = boolValue; break;
            }

            System.Type type = targetScript.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            bool success = false;

            try
            {
                // 1. 변수(Field) 탐색 및 값 설정
                FieldInfo field = type.GetField(variableName, flags);
                if (field != null)
                {
                    // Convert.ChangeType을 사용하여 안전하게 형변환 후 대입
                    object convertedValue = Convert.ChangeType(valueToSet, field.FieldType);
                    field.SetValue(targetScript, convertedValue);
                    success = true;
                }
                else
                {
                    // 2. 프로퍼티(Property) 탐색 및 값 설정
                    PropertyInfo prop = type.GetProperty(variableName, flags);
                    if (prop != null && prop.CanWrite)
                    {
                        object convertedValue = Convert.ChangeType(valueToSet, prop.PropertyType);
                        prop.SetValue(targetScript, convertedValue);
                        success = true;
                    }
                }
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"<color=red>[{gameObject.name}]</color> 변수 값을 설정하는 중 오류가 발생했습니다. 타입이 일치하는지 확인하세요. 에러: {e.Message}");
#endif
                return;
            }

            if (success)
            {
#if UNITY_EDITOR
                Debug.Log($"<color=green>[{gameObject.name}]</color> {targetScript.GetType().Name}의 '{variableName}' 값을 [{valueToSet}]로 변경했습니다.");
#endif
                // 프레임워크 룰에 따라 다음 노드 실행
                IsOn = true;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=orange>[{gameObject.name}]</color> {targetScript.name}에서 '{variableName}' 이름의 변수를 찾을 수 없습니다.");
#endif
            }
        }
    }
}