using _Project.Scripts.VisualScripting;
using System;
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    namespace _Project.Scripts.VisualScripting
    {
        public enum StatOperation
        {
            Set,      // 값 덮어쓰기 (Target = ActorValue)
            Add,      // 더하기 (Target += ActorValue)
            Subtract, // 빼기 (Target -= ActorValue)
            Multiply, // 곱하기 (Target *= ActorValue)
            Toggle    // 불리언 반전 (Target = !Target)
        }

        public enum EntrySourceType
        {
            Constant,   // 고정 수치 직접 입력 (예: 포션, 고정 함정 데미지)
            Actor       // 다른 클로너의 스탯 참조 (예: 공격자의 공격력)
        }
        // [CustomEditor(typeof(EntryClonnervent))]
        public class EntryClonnervent : ProcessBase
        {
            [Header("Target (The Receiver)")]
            [Tooltip("효과를 받는 대상입니다.")]
            [SerializeField] private EntryCloner targetCloner;
            [Tooltip("변경할 대상의 스탯 키")]
            [SerializeField] private string targetEntryKey = "HP";

            [Header("Source Settings")]
            [SerializeField] private EntrySourceType sourceType = EntrySourceType.Constant;

            [Tooltip("SourceType이 Actor일 때 참조할 액터 클로너입니다.")]
            [SerializeField] private EntryCloner actorCloner;
            [Tooltip("SourceType이 Actor일 때 참조할 액터의 스탯 키입니다.")]
            [SerializeField] private string actorEntryKey = "Atk";
            [Tooltip("SourceType이 Constant일 때 사용할 고정 값입니다.")]
            [SerializeField] private string constantValue = "10";

            [Header("Operation")]
            [SerializeField] private StatOperation operation = StatOperation.Subtract;

            public override void Execute()
            {
                if (targetCloner == null) return;

                // 1. 피격자(Target) 현재 값 확보
                object targetVal = targetCloner.GetStat<object>(targetEntryKey);
                if (targetVal == null)
                {
                    Debug.LogError($"[{gameObject.name}] 타겟 값을 가져오지 못했습니다. TargetEntryKey: {targetEntryKey}");
                    return;
                }

                // 2. 연산에 쓸 소스 값(Actor 스탯 vs 고정값) 결정
                object sourceValue = GetSourceValue();
                if (sourceValue == null)
                {
                    Debug.LogError($"[{gameObject.name}] 소스 값을 가져오지 못했습니다. SourceType: {sourceType}, ActorEntryKey: {actorEntryKey}, ConstantValue: {constantValue}");
                    return;
                }

                // 3. 연산 및 적용
                object result = Calculate(targetVal, sourceValue);
                targetCloner.SetStat(targetEntryKey, result);

                IsOn = true;

                Debug.Log($"[EntryModifier] {targetCloner.targetId}.{targetEntryKey} {operation} by {sourceValue}. Result: {result}");
            }

            private object GetSourceValue()
            {
                if (sourceType == EntrySourceType.Constant)
                {
                    return constantValue;
                }
                else // Actor 모드
                {
                    if (actorCloner == null)
                    {
                        Debug.LogWarning($"[{gameObject.name}] ActorCloner가 설정되지 않았습니다.");
                        return null;
                    }
                    return actorCloner.GetStat<object>(actorEntryKey);
                }
            }

            private object Calculate(object targetValue, object sourceValue)
            {
                float.TryParse(targetValue.ToString(), out float targetFloat);
                float.TryParse(sourceValue.ToString(), out float sourceFloat);

                switch (operation)
                {
                    case StatOperation.Set:
                        return sourceValue;
                    case StatOperation.Add:
                        return targetFloat + sourceFloat;
                    case StatOperation.Subtract:
                        return targetFloat - sourceFloat;
                    case StatOperation.Multiply:
                        return targetFloat * sourceFloat;
                    case StatOperation.Toggle:
                        if (targetValue is bool b) return !b;
                        return targetValue;
                    default:
                        return targetValue;
                }
            }
        }
    }
}