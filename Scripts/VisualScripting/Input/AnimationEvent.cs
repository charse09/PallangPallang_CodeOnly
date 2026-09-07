using UnityEngine;
using static _Project.Scripts.VisualScripting.EntryModifierEvent;

namespace _Project.Scripts.VisualScripting
{
    public class AnimationEvent : ProcessBase
    {
        [Header("Target Selection")]
        [Tooltip("타겟을 결정하는 방식입니다.")]
        [SerializeField] private TargetSourceType targetSourceType = TargetSourceType.Fixed;

        [Tooltip("TargetSourceType이 FromTrigger일 때 타겟을 가져올 트리거 노드입니다.")]
        [SerializeField] private ProcessTargeter sourceTrigger;

        [Header("Target Object")]
        [Tooltip("TargetSourceType이 Fixed일 때 사용할 대상(애니메이터)입니다.")]
        [SerializeField] private Animator targetAnimator;

        [Header("Animation Settings")]
        [Tooltip("실행할 트리거 파라미터의 이름")]
        [SerializeField] private string triggerName = "Demaged";

        public override void Execute()
        {
            // 1. 설정된 옵션에 따라 최종 타겟(Animator) 결정
            Animator finalAnimator = GetFinalAnimator();

            // 2. 실행
            if (finalAnimator != null)
            {
                finalAnimator.SetTrigger(triggerName);

                // 실행 여부를 확인하기 위한 처리
                IsOn = true;
                Debug.Log($"[AnimationEvent] {finalAnimator.gameObject.name}의 '{triggerName}' 트리거 실행.");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] 유효한 타겟(Animator)을 찾지 못해 애니메이션을 재생할 수 없습니다.");
            }
        }

        /// <summary>
        /// 인스펙터 설정에 따라 타겟 Animator를 가져옵니다.
        /// </summary>
        private Animator GetFinalAnimator()
        {
            if (targetSourceType == TargetSourceType.Fixed)
            {
                return targetAnimator;
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
                    Debug.LogWarning($"[{gameObject.name}] Trigger타겟이 없음 현재 감지하고 있는 대상이 없습니다.");
                    return null;
                }

                // 기존 툴팁의 내용("자식에 애니메이터가 있는 오브젝트")을 반영하여
                // 트리거 대상의 본체뿐만 아니라 자식 객체들까지 포함해서 Animator를 검색합니다.
                Animator anim = triggerTarget.GetComponentInChildren<Animator>();
                if (anim == null)
                {
                    Debug.LogWarning($"[{gameObject.name}] Trigger 대상({triggerTarget.name}) 및 그 자식에서 Animator를 찾을 수 없습니다.");
                }

                return anim;
            }
        }

        // 외부에서 동적으로 타겟 오브젝트를 바꿀 때 사용 (기존 기능 유지)
        public void SetTarget(Animator newTarget)
        {
            targetAnimator = newTarget;
            // 코드로 타겟을 직접 강제할 경우, 모드를 Fixed로 변경해주는 것이 안전할 수 있습니다.
            targetSourceType = TargetSourceType.Fixed;
        }
    }
}