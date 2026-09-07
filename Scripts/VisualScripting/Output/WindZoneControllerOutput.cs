using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class WindZoneControllerOutput : ProcessBase
    {
        public enum WindControlMode
        {
            DeactivateAndExitRoll,  // 바람 구역을 끄면서 플레이어 구르기도 해제 (원하셨던 첫 번째 방법)
            ExitRollOnly            // 바람 구역은 놔두고, 플레이어 구르기만 원래대로 복구 (원하셨던 두 번째 방법)
        }

        [Header("--- [Target Settings] ---")]
        [Tooltip("제어할 RollingWindZone 오브젝트를 연결하세요.")]
        [SerializeField] private RollingWindZone targetWindZone;

        [Header("--- [Control Settings] ---")]
        [Tooltip("DeactivateAndExitRoll: 구역을 끄면서 구르기 해제\nExitRollOnly: 구역은 유지하고 구르기만 해제")]
        [SerializeField] private WindControlMode controlMode = WindControlMode.DeactivateAndExitRoll;

        public override void Execute()
        {
            IsOn = false;

            if (targetWindZone == null)
            {
                Debug.LogError($"[{gameObject.name}] Target Wind Zone이 지정되지 않았습니다!");
                IsOn = true;
                return;
            }

            // 현재 씬에 존재하는 플레이어(PlayerLocomotion)를 찾습니다.
            PlayerLocomotion player = FindFirstObjectByType<PlayerLocomotion>();

            if (controlMode == WindControlMode.DeactivateAndExitRoll)
            {
                // 1. 바람 구역 오브젝트 비활성화
                targetWindZone.gameObject.SetActive(false);

                // 2. 플레이어가 구르고 있다면 구르기 모드 강제 해제
                if (player != null && player.isWindRolling)
                {
                    player.ExitWindRollMode();
                }
            }
            else if (controlMode == WindControlMode.ExitRollOnly)
            {
                // 바람 구역은 건드리지 않고, 플레이어만 구르기 모드에서 탈출시킵니다.
                if (player != null && player.isWindRolling)
                {
                    player.ExitWindRollMode();
                }
            }

            // 처리가 끝나면 비주얼 스크립팅 플로우를 다음 노드로 넘깁니다.
            IsOn = true;
        }
    }
}