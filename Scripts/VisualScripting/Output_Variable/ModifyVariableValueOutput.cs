using System;
using System.Reflection;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정한 스크립트 내부의 특정 숫자형 변수(int, float)를 찾아 값을 증감시키는 Output 모듈
    /// </summary>
    public class ModifyVariableValueOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("값을 변경할 대상 스크립트를 드래그해서 넣으세요.")]
        [SerializeField] private MonoBehaviour targetScript;

        [Tooltip("증감시킬 변수의 정확한 이름을 입력하세요. (int 또는 float 타입 권장)")]
        [SerializeField] private string variableName = "currentGold";

        [Header("Modification Settings")]
        [Tooltip("현재 값에 더하거나 뺄 수치입니다. (음수를 입력하면 감소합니다)")]
        [SerializeField] private float modificationAmount = 10f;

        public override void Execute()
        {
            if (targetScript == null || string.IsNullOrEmpty(variableName))
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> Target Script나 Variable Name이 설정되지 않았습니다.");
#endif
                return;
            }

            System.Type type = targetScript.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            bool success = false;

            try
            {
                // 1. 변수(Field) 탐색
                FieldInfo field = type.GetField(variableName, flags);
                if (field != null)
                {
                    object currentValue = field.GetValue(targetScript);
                    object newValue = CalculateNewValue(currentValue, field.FieldType);
                    field.SetValue(targetScript, newValue);
                    success = true;
                }
                else
                {
                    // 2. 프로퍼티(Property) 탐색
                    PropertyInfo prop = type.GetProperty(variableName, flags);
                    if (prop != null && prop.CanWrite)
                    {
                        object currentValue = prop.GetValue(targetScript);
                        object newValue = CalculateNewValue(currentValue, prop.PropertyType);
                        prop.SetValue(targetScript, newValue);
                        success = true;
                    }
                }
            }
            catch (Exception e)
            {
#if UNITY_EDITOR
                Debug.LogError($"<color=red>[{gameObject.name}]</color> 변수 증감 중 오류 발생: {e.Message}");
#endif
                return;
            }

            if (success)
            {
#if UNITY_EDITOR
                Debug.Log($"<color=green>[{gameObject.name}]</color> {targetScript.GetType().Name}의 '{variableName}' 수치를 {modificationAmount}만큼 변화시켰습니다.");
#endif
                IsOn = true; // 다음 노드 실행
            }
        }

        /// <summary>
        /// 기존 값에 증감 수치를 더해 적절한 타입으로 반환합니다.
        /// </summary>
        private object CalculateNewValue(object current, Type targetType)
        {
            // 모든 숫자형을 float로 변환하여 계산 후 다시 원래 타입으로 복구
            float val = Convert.ToSingle(current);
            val += modificationAmount;

            return Convert.ChangeType(val, targetType);
        }
    }
}