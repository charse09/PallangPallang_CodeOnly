using System;
using UnityEditor;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class EntryModifierEvent : ProcessBase
    {
        public enum TargetSourceType
        {
            Fixed,      // 인스펙터에 할당된 targetCloner 사용 (예: 맵의 고정된 보스)
            FromTrigger // 연결된 Trigger가 감지한 대상 사용 (예: 함정을 밟은 플레이어/적)
        }

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

        [Header("Target Selection")]
        [Tooltip("타겟을 결정하는 방식입니다.")]
        [SerializeField] private TargetSourceType targetSourceType = TargetSourceType.Fixed;

        [Tooltip("TargetSourceType이 FromTrigger일 때 타겟을 가져올 트리거 노드입니다.")]
        [SerializeField] private ProcessTargeter sourceTrigger;

        [Header("Target (The Receiver)")]
        [Tooltip("TargetSourceType이 Fixed일 때 사용할 대상입니다.")]
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
            // 1. 설정된 옵션에 따라 최종 타겟(EntryCloner) 결정
            EntryCloner finalTarget = GetFinalTargetCloner();

            if (finalTarget == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 유효한 타겟(EntryCloner)을 찾지 못했습니다. 이벤트가 무시됩니다.");
                return;
            }

            // 2. 피격자(Target) 현재 값 확보
            object targetVal = finalTarget.GetStat<object>(targetEntryKey);
            if (targetVal == null)
            {
                Debug.LogError($"[{gameObject.name}] 타겟 값을 가져오지 못했습니다. TargetEntryKey: {targetEntryKey}");
                return;
            }

            // 3. 연산에 쓸 소스 값(Actor 스탯 vs 고정값) 결정
            object sourceValue = GetSourceValue();
            if (sourceValue == null)
            {
                Debug.LogError($"[{gameObject.name}] 소스 값을 가져오지 못했습니다. SourceType: {sourceType}, ActorEntryKey: {actorEntryKey}, ConstantValue: {constantValue}");
                return;
            }

            // 4. 연산 및 적용
            object result = Calculate(targetVal, sourceValue);
            finalTarget.SetStat(targetEntryKey, result);

            IsOn = true;

            // 로그 출력 시 targetId가 없다면 gameObject.name으로 대체하여 에러 방지
            Debug.Log($"[EntryModifier] {finalTarget.gameObject.name}.{targetEntryKey} {operation} by {sourceValue}. Result: {result}");
        }

        /// <summary>
        /// 인스펙터 설정에 따라 타겟 EntryCloner를 가져옵니다.
        /// </summary>
        private EntryCloner GetFinalTargetCloner()
        {
            if (targetSourceType == TargetSourceType.Fixed)
            {
                return targetCloner;
            }
            else // TargetSourceType.FromTrigger
            {
                if (sourceTrigger == null)
                {
                    Debug.LogError($"[{gameObject.name}] 참조할 SourceTrigger가 할당되지 않았습니다.");
                    return null;
                }

                GameObject triggerTarget = sourceTrigger.GetTarget();
                if (triggerTarget == null)
                {
                    // 트리거가 비어있을 수 있으므로 에러보다는 경고 처리
                    Debug.LogWarning($"[{gameObject.name}] Trigger()가 현재 감지하고 있는 대상이 없습니다.");
                    return null;
                }

                // 트리거가 감지한 게임 오브젝트에서 EntryCloner 추출
                return triggerTarget.GetComponent<EntryCloner>();
            }
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