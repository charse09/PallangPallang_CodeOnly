using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public enum StateCondition
    {
        Ignore,     // 조건 검사하지 않음
        MustBeTrue, // 해당 상태가 true여야 함
        MustBeFalse // 해당 상태가 false여야 함
    }

    public class CheckStateTrigger : ProcessBase
    {
        [SerializeField] private string selectedTag = "Player";

        [Header("Trigger Mode")]
        [Tooltip("체크 시: 설정한 상태가 '아닐 때' IsOn = true를 반환합니다.")]
        [SerializeField] private bool triggerWhenNotMatching = true;

        [Header("Player State Conditions")]
        [Tooltip("PaperState 조건")]
        [SerializeField] private StateCondition paperState = StateCondition.Ignore;

        [Tooltip("SlimState 조건")]
        [SerializeField] private StateCondition slimState = StateCondition.Ignore;

        [Tooltip("IsGrounded 조건")]
        [SerializeField] private StateCondition isGrounded = StateCondition.Ignore;

        private void OnTriggerEnter(Collider other)
        {
            EvaluatePlayerState(other);
        }

        private void OnTriggerStay(Collider other)
        {
            // 트리거 내부에서 상태 변경 시 실시간 반영
            EvaluatePlayerState(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(selectedTag))
            {
                IsOn = false;
            }
        }

        private void EvaluatePlayerState(Collider other)
        {
            if (!other.CompareTag(selectedTag)) return;

            PlayerLocomotion player = other.GetComponentInParent<PlayerLocomotion>();

            if (player != null)
            {
                bool isPaperValid = ValidateCondition(paperState, player.GetPaperState());
                bool isSlimValid = ValidateCondition(slimState, player.GetSlimState());
                bool isGroundedValid = ValidateCondition(isGrounded, player.IsGrounded);

                // 지정된 모든 조건과 일치하는지 여부
                bool isMatched = isPaperValid && isSlimValid && isGroundedValid;

                // triggerWhenNotMatching이 true면 조건을 만족하지 않을 때 IsOn = true
                IsOn = triggerWhenNotMatching ? !isMatched : isMatched;
                return;
            }

            IsOn = false;
        }

        private bool ValidateCondition(StateCondition condition, bool actualValue)
        {
            return condition switch
            {
                StateCondition.MustBeTrue => actualValue,
                StateCondition.MustBeFalse => !actualValue,
                _ => true
            };
        }

        public override void Execute()
        {
            IsOn = true;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}