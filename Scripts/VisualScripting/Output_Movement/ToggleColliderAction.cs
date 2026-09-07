using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class ToggleColliderAction : ProcessBase
    {
        [Header("콜라이더 제어 설정")]
        [Tooltip("제어할 대상의 콜라이더를 드래그해서 넣으세요.")]
        [SerializeField] private Collider targetCollider;

        [Tooltip("체크하면 켭니다(활성화). 체크를 해제하면 끕니다(비활성화).")]
        [SerializeField] private bool enableCollider = false;

        public override void Execute()
        {
            // 대상 콜라이더가 제대로 연결되어 있는지 확인
            if (targetCollider != null)
            {
                // 인스펙터에서 설정한 값(true/false)대로 콜라이더 상태를 변경
                targetCollider.enabled = enableCollider;

                string stateName = enableCollider ? "활성화" : "비활성화";
                Debug.Log($"<color=yellow>[Action]</color> {targetCollider.gameObject.name}의 콜라이더가 {stateName} 되었습니다.");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] 타겟 콜라이더가 지정되지 않았습니다!");
            }
        }

        public new void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}