using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 플레이어의 모든 입력(이동, 점프, 상호작용 등)을 전역적으로 차단하거나 허용하는 Output 모듈
    /// </summary>
    public class PlayerInputControlOutput : ProcessBase
    {
        [Header("Control Settings")]
        [Tooltip("체크 시: 모든 입력 허용 (정상 플레이)\n체크 해제 시: 모든 입력 차단 (조작 불가)")]
        [SerializeField] private bool inputEnabled = false;

        [Tooltip("입력을 막을 때 플레이어의 현재 움직임을 즉시 멈출지 여부")]
        [SerializeField] private bool stopPhysicsImmediately = true;

        public override void Execute()
        {
            // 1. InputManager의 전역 상태 제어
            // InputManager에 'IsInputLocked' 같은 속성이 있다고 가정하거나 직접 추가해야 합니다.
            if (InputManager.Instance != null)
            {
                // 입력 시스템 자체를 활성화/비활성화
                InputManager.Instance.enabled = inputEnabled;
            }

            // 2. 물리적인 멈춤 처리 (선택 사항)
            if (!inputEnabled && stopPhysicsImmediately)
            {
                var player = FindFirstObjectByType<PlayerLocomotion>();
                if (player != null)
                {
                    Rigidbody rb = player.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                }
            }

#if UNITY_EDITOR
            Debug.Log($"<color=white>[{gameObject.name}]</color> 플레이어 전역 입력 상태: " +
                      (inputEnabled ? "<color=green>모두 허용</color>" : "<color=red>모두 차단</color>"));
#endif

            IsOn = true;
        }
    }
}