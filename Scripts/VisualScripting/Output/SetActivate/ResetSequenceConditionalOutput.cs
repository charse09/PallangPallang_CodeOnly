using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정한 SequenceConditional을 완전히 리셋하는 Output 노드.
    /// 실행 중인 좀비 코루틴을 강제 종료하고 _isProcessing, IsOn을 초기화하여
    /// 다음 TriggerEnter 때 SequenceConditional이 정상적으로 재실행되게 한다.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Outputs/SequenceConditional 전체 리셋 (Output)")]
    public class ResetSequenceConditionalOutput : ProcessBase
    {
        [Header("Target Setup")]
        [Tooltip("완전히 리셋할 SequenceConditional 컴포넌트를 연결하세요.")]
        [SerializeField] private SequenceConditional targetSequenceConditional;

        public override void Execute()
        {
            IsOn = false;

            if (targetSequenceConditional != null)
            {
                targetSequenceConditional.Reset();
#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[{gameObject.name}]</color> {targetSequenceConditional.gameObject.name} 리셋 완료 (코루틴 종료, _isProcessing = false)");
#endif
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] targetSequenceConditional이 지정되지 않았습니다.");
            }

            // 노드 실행 완료 → 다음 노드로 바톤 터치
            IsOn = true;
        }
    }
}
