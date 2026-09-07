using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정한 TriggerEnter(또는 ProcessBase) 오브젝트 1개의 IsOn 상태를 false(체크 해제)로 리셋하는 Output 노드.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Outputs/트리거 IsOn 해제 (Output)")]
    public class ResetTriggerIsOnOutput : ProcessBase
    {
        [Header("Target Setup")]
        [Tooltip("IsOn을 false로 끌 대상 (TriggerEnter 등 ProcessBase 스크립트가 붙은 오브젝트)")]
        [SerializeField] private ProcessBase targetProcess;

        public override void Execute()
        {
            IsOn = false;

            if (targetProcess != null)
            {
                // ProcessBase에 추가한 SetIsOn 메서드를 호출하여 해당 오브젝트의 IsOn만 false로 변경
                targetProcess.SetIsOn(false);
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] targetProcess가 지정되지 않았습니다.");
            }

            // 노드 자체의 실행이 완료되었으므로 다음 노드로 바톤 터치
            IsOn = true;
        }
    }
}