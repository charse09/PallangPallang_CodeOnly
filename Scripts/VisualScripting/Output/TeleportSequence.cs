using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class TeleportSequence : ProcessBase
    {
        [Header("Target")]
        [SerializeField] private Transform objectToMove;

        [Header("Destination")]
        [SerializeField] private Transform targetPoint;

        [Header("Options")]
        [Tooltip("텔레포트 후 회전도 맞출지")]
        [SerializeField] private bool applyRotation = true;

        public override void Execute()
        {
            if (IsOn) return;
            if (objectToMove == null || targetPoint == null) return;

            Teleport();
        }

        private void Teleport()
        {
            // IsOn = true; // (기존 코드 제거)

            // 위치 이동
            objectToMove.position = targetPoint.position;

            // 회전
            if (applyRotation)
            {
                objectToMove.rotation = targetPoint.rotation;
            }

            IsOn = true; // 수정: 이동이 완료되었음을 알리기 위해 true로 설정!
        }
    }
}