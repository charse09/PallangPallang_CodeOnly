using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 플레이어의 특정 조작들을 다중 선택하여 개별적으로 차단하거나 허용하는 모듈
    /// </summary>
    public class SetSpecificInputOutput : ProcessBase
    {
        [Header("Control Settings")]
        [Tooltip("제어하고 싶은 조작들을 체크하세요. (여러 개 다중 선택 가능)")]
        [SerializeField] private InputControlType targetInput = InputControlType.None;

        [Tooltip("체크 시: 선택한 조작 허용\n체크 해제 시: 선택한 조작 차단")]
        [SerializeField] private bool enableInput = true;

        [Header("Physics Settings")]
        [Tooltip("입력을 막을 때 이동(Movement)이 포함되어 있다면, 관성을 즉시 없애고 멈출지 여부")]
        [SerializeField] private bool stopPhysicsImmediately = true;

        public override void Execute()
        {
            IsOn = false;

            if (InputManager.Instance != null)
            {
                // 선택한 입력들(다중 선택됨)을 모두 끄거나 켭니다.
                InputManager.Instance.SetSpecificInput(targetInput, enableInput);
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] InputManager.Instance를 찾을 수 없습니다.");
            }

            // 2. 물리 정지 처리 (조작을 막았고 물리 정지 옵션이 켜져 있으며, 선택 항목 중 'Movement'가 포함되어 있을 때)
            if (!enableInput && stopPhysicsImmediately)
            {
                if (targetInput.HasFlag(InputControlType.Movement))
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
            }

#if UNITY_EDITOR
            string stateStr = enableInput ? "<color=green>허용</color>" : "<color=red>차단</color>";
            Debug.Log($"<color=white>[{gameObject.name}]</color> 조작 변경 | 항목: {targetInput} | 상태: {stateStr}");
#endif

            IsOn = true;
        }
    }
}