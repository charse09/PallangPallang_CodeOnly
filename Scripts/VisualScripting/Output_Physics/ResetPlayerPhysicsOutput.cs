using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 플레이어의 Rigidbody 물리량(속도, 각속도, 힘 등)을 초기화하고
    /// 필요 시 특정 위치로 텔레포트하거나 물리 옵션을 재설정하는 아웃풋 컴포넌트입니다.
    /// </summary>
    public class ResetPlayerPhysicsOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("물리를 초기화할 플레이어 (비워둘 경우 자동 탐색)")]
        [SerializeField] private PlayerLocomotion locomotion;

        [Header("Velocity Reset Settings")]
        [Tooltip("선택 시 이동 속도(Velocity)를 Vector3.zero로 초기화합니다.")]
        [SerializeField] private bool resetVelocity = true;

        [Tooltip("선택 시 회전 속도(Angular Velocity)를 Vector3.zero로 초기화합니다.")]
        [SerializeField] private bool resetAngularVelocity = true;

        [Tooltip("선택 시 누적된 물리 관성 및 외력을 완전히 제거합니다 (Sleep 후 WakeUp).")]
        [SerializeField] private bool clearInertiaAndForces = true;

        [Header("Physics State Settings")]
        [Tooltip("선택 시 isKinematic 상태를 강제로 변경합니다.")]
        [SerializeField] private bool modifyIsKinematic = false;

        [Tooltip("modifyIsKinematic이 true일 때 적용할 isKinematic 값")]
        [SerializeField] private bool targetIsKinematic = false;

        [Tooltip("선택 시 useGravity 상태를 강제로 변경합니다.")]
        [SerializeField] private bool modifyUseGravity = false;

        [Tooltip("modifyUseGravity가 true일 때 적용할 useGravity 값")]
        [SerializeField] private bool targetUseGravity = true;

        [Header("Position Reset Settings (Optional)")]
        [Tooltip("선택 시 특정 위치로 플레이어를 이동시킵니다.")]
        [SerializeField] private bool resetPosition = false;

        [Tooltip("resetPosition이 true일 때 이동시킬 목표 Transform")]
        [SerializeField] private Transform teleportTarget;

        public override void Execute()
        {
            IsOn = false;

            // 1. 플레이어 탐색
            PlayerLocomotion player = locomotion != null
                ? locomotion
                : FindFirstObjectByType<PlayerLocomotion>();

            if (player == null)
            {
                Debug.LogWarning("[ResetPlayerPhysicsOutput] 대상 PlayerLocomotion을 찾지 못했습니다.");
                IsOn = true;
                return;
            }

            Rigidbody rb = player.GetComponent<Rigidbody>();

            if (rb != null)
            {
                // 2. 위치 및 회전 초기화 (텔레포트)
                if (resetPosition && teleportTarget != null)
                {
                    rb.position = teleportTarget.position;
                    rb.rotation = teleportTarget.rotation;
                    player.transform.position = teleportTarget.position;
                    player.transform.rotation = teleportTarget.rotation;
                }

                // 3. 속도 및 각속도 초기화 (유니티 버전에 맞춘 속도 프로퍼티 적용)
                if (resetVelocity)
                {
#if UNITY_6000_0_OR_NEWER
                    rb.linearVelocity = Vector3.zero;
#else
                    rb.velocity = Vector3.zero;
#endif
                }

                if (resetAngularVelocity)
                {
                    rb.angularVelocity = Vector3.zero;
                }

                // 4. 누적된 외력 및 관성 완전 정지
                if (clearInertiaAndForces)
                {
                    rb.Sleep();
                    rb.WakeUp();
                }

                // 5. 물리 옵션 재설정
                if (modifyIsKinematic)
                {
                    rb.isKinematic = targetIsKinematic;
                }

                if (modifyUseGravity)
                {
                    rb.useGravity = targetUseGravity;
                }
            }

            IsOn = true;
        }
    }
}