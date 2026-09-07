using UnityEngine;
using System.Collections;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 실행 시 지정된 시간 또는 외부 트리거가 켜질 때까지 플레이어의 braceState(앉기)를 강제로 활성화하는 아웃풋 노드
    /// </summary>
    public class ForceSitProcess : ProcessBase
    {
        [Header("Target Player")]
        [Tooltip("씬에 있는 PlayerLocomotion 컴포넌트를 연결하세요. 비워두면 자동으로 찾아옵니다.")]
        [SerializeField] private PlayerLocomotion playerComponent;

        [Header("Sit Mode Settings")]
        [Tooltip("true일 경우 시간 제한 대신 계속 앉은 상태를 유지합니다. (기본값: false)")]
        [SerializeField] private bool keepSitting = false;

        [Tooltip("강제로 앉아 있게 만들 유지 시간 (초) (keepSitting이 false일 때 사용)")]
        [SerializeField] private float holdDuration = 3.0f;

        [Header("★ Release Trigger Settings (외부 노드/트리거 연동)")]
        [Tooltip("이 ProcessBase(트리거/Output 노드)의 IsOn이 true가 되면 중간에 강제 앉기가 해제됩니다.")]
        [SerializeField] private ProcessBase releaseTrigger;

        private bool _isProcessing = false;

        private void Start()
        {
            // 인스펙터에 플레이어를 깜빡하고 안 넣었다면 자동으로 씬에서 찾아옴
            if (playerComponent == null)
            {
                playerComponent = Object.FindFirstObjectByType<PlayerLocomotion>();
            }
        }

        public override void Execute()
        {
            if (_isProcessing) return;

            if (playerComponent == null)
            {
                playerComponent = Object.FindFirstObjectByType<PlayerLocomotion>();
                if (playerComponent == null)
                {
                    Debug.LogError($"[{gameObject.name}] 씬에서 PlayerLocomotion을 찾을 수 없습니다!");
                    IsOn = true;
                    return;
                }
            }

            IsOn = false;
            StartCoroutine(ForceSitRoutine());
        }

        private IEnumerator ForceSitRoutine()
        {
            _isProcessing = true;

            // 1. 플레이어의 변수 및 애니메이션을 강제로 "앉음" 상태로 변경!
            playerComponent.SetForceBraceState(true);
            Debug.Log($"[ForceSit] 플레이어를 강제로 앉힙니다. (지속 유지 여부: {keepSitting})");

            // 2. 조건에 따른 대기 처리
            if (keepSitting)
            {
                if (releaseTrigger != null)
                {
                    // releaseTrigger의 IsOn이 true가 될 때까지 대기
                    while (!releaseTrigger.IsOn)
                    {
                        yield return null;
                    }
                }
                else
                {
                    // releaseTrigger가 등록되어 있지 않다면 1초 만에 종료
                    Debug.LogWarning($"[{gameObject.name}] keepSitting이 true이지만 releaseTrigger가 없으므로 1초 후 자동 해제됩니다.");
                    yield return new WaitForSeconds(1.0f);
                }
            }
            else
            {
                // keepSitting이 false일 때는 holdDuration 동안 대기 (중간에 releaseTrigger가 켜져도 해제 가능)
                float timer = 0f;
                while (timer < holdDuration)
                {
                    if (releaseTrigger != null && releaseTrigger.IsOn)
                    {
                        break;
                    }
                    timer += Time.deltaTime;
                    yield return null;
                }
            }

            // 3. 유지 시간이 끝나거나 releaseTrigger가 감지되면 강제 앉기 상태 해제!
            playerComponent.SetForceBraceState(false);
            Debug.Log("[ForceSit] 플레이어 강제 앉기 상태를 해제합니다.");

            _isProcessing = false;
            IsOn = true; // 다음 QTE나 시퀀스 노드로 신호 전달
        }

        public override void Reset()
        {
            base.Reset();
            _isProcessing = false;
            if (playerComponent != null)
            {
                playerComponent.SetForceBraceState(false);
            }
        }
    }
}