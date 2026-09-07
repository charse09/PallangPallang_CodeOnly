using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 플레이어를 거칠게 움직이는 플랫폼에 고정(Attach)하거나 해제(Detach)하는 Output 모듈.
    /// targetState 조건에 맞춰 플랫폼 고정 상태를 설정하고, 처리 완료 후 다음 노드로 신호를 전달합니다.
    /// </summary>
    public class RoughPlatformAttachOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("제어할 플레이어의 Locomotion 스크립트를 할당하세요. (비워둘 시 자동 검색)")]
        [SerializeField] private PlayerLocomotion player;

        [Tooltip("플레이어를 고정할 대상 플랫폼의 Transform (고정 해제 시에는 비워두어도 됩니다).")]
        [SerializeField] private Transform platformTransform;

        [Tooltip("고정 여부 (체크 시 플랫폼에 고정, 체크 해제 시 고정 해제)")]
        [SerializeField] private bool isAttaching = true;

        [Header("Wait Settings")]
        [Tooltip("고정/해제 처리 후 다음 노드로 넘어가기 전 대기할 시간 (초).")]
        [SerializeField] private float extraWaitAfterComplete = 0.05f;

        public override void Execute()
        {
            IsOn = false;

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerLocomotion>();
            }

            if (player != null)
            {
                StartCoroutine(SetAttachModeRoutine());
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] PlayerLocomotion 레퍼런스를 찾을 수 없습니다.");
                IsOn = true;
            }
        }

        private IEnumerator SetAttachModeRoutine()
        {
            // 1. 플랫폼 고정/해제 처리 실행
            player.SetRoughPlatformAttachMode(isAttaching, platformTransform);

#if UNITY_EDITOR
            string platformName = platformTransform != null ? platformTransform.name : "Null";
            string stateName = isAttaching ? $"플랫폼 탑승 및 고정 ({platformName})" : "플랫폼 고정 해제";
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> 플레이어 {stateName} 적용 완료.");
#endif

            // 2. 물리 연산 연동을 위해 1 물리 프레임 대기
            yield return new WaitForFixedUpdate();

            // 3. 추가 대기 (버퍼)
            if (extraWaitAfterComplete > 0f)
            {
                yield return new WaitForSeconds(extraWaitAfterComplete);
            }

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 노드 실행 완료! 다음 노드로 신호를 전달합니다.");
#endif

            IsOn = true;
        }

        /// <summary>
        /// 동적으로 대상 플랫폼을 변경해야 할 때 사용하는 헬퍼 메서드
        /// </summary>
        public void SetTargetPlatform(Transform newPlatform)
        {
            platformTransform = newPlatform;
        }
    }
}