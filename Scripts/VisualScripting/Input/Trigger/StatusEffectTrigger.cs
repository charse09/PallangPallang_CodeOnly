using _Project.Scripts.Systems; // StatusEffectHandler가 있는 네임스페이스
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class StatusEffectTrigger : ProcessBase
    {
        [Header("감지 대상 설정")]
        [Tooltip("감시할 대상의 StatusEffectHandler를 넣으세요. 비워두면 이 스크립트가 붙은 오브젝트에서 찾습니다.")]
        [SerializeField] private StatusEffectHandler targetHandler;

        [Header("감지 조건 설정")]
        [Tooltip("어떤 상태이상을 감지할지 적으세요. (예: Stun, Poison)\n비워두면 '아무 상태이상'이나 모두 감지합니다.")]
        [SerializeField] private string targetEffectName = "Stun";

        private void Awake()
        {
            // 타겟을 안 넣었다면, 자신이 붙어있는 오브젝트에서 자동으로 찾음
            if (targetHandler == null)
            {
                targetHandler = GetComponent<StatusEffectHandler>();
            }
        }

        private void OnEnable()
        {
            // 스크립트가 켜질 때 이벤트 구독 (방송 듣기 시작)
            if (targetHandler != null)
                targetHandler.OnEffectApplied += HandleEffectApplied;
        }

        private void OnDisable()
        {
            // 꺼질 때 이벤트 구독 해제 (매우 중요!)
            if (targetHandler != null)
                targetHandler.OnEffectApplied -= HandleEffectApplied;
        }

        private void HandleEffectApplied(string appliedEffect)
        {
            // 이미 켜져 있다면 무시
            if (IsOn) return;

            // 특정 상태이상을 지정했는데, 방금 걸린 게 그 상태이상이 아니라면 무시
            if (!string.IsNullOrEmpty(targetEffectName) && appliedEffect != targetEffectName)
                return;

            // 조건 통과! 트리거 작동
            IsOn = true;
            Execute();

            Debug.Log($"<color=cyan>[Trigger]</color> {gameObject.name}에서 '{appliedEffect}' 감지 완료! 신호를 보냅니다.");
        }

        public override void Execute() { }

        public new void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}