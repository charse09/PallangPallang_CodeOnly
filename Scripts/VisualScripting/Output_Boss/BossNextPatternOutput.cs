using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 스턴 상태의 보스(Boss2Controller)를 깨우고, 다음 페이즈/지정 페이즈로 전환시키거나 사망 처리하는 노드 모듈
    /// </summary>
    public class BossNextPatternOutput : ProcessBase
    {
        [Header("Boss Target Settings")]
        [Tooltip("신호를 보낼 보스 오브젝트 (비어있을 경우 씬 내 Boss2Controller를 자동 탐색합니다)")]
        [SerializeField] private Boss2Controller bossTarget;

        [Header("Defeat Options")]
        [Tooltip("True일 경우 페이즈 전환 대신 보스를 즉시 사망(Defeated) 상태로 만듭니다.")]
        [SerializeField] private bool triggerDefeat = false;

        [Header("Phase Transition Options")]
        [Tooltip("True: 현재 페이즈 기준 다음 페이즈로 순차 전환 (P1->P2->P3)\nFalse: 아래 targetPhase에서 지정한 페이즈로 직접 전환")]
        [SerializeField] private bool useNextSequentialPhase = true;

        [Tooltip("useNextSequentialPhase가 False일 때 강제로 전환시킬 목표 페이즈")]
        [SerializeField] private BossPhase targetPhase = BossPhase.Phase2;

        public override void Execute()
        {
            IsOn = false;

            if (bossTarget == null)
            {
                bossTarget = FindFirstObjectByType<Boss2Controller>();
            }

            if (bossTarget == null)
            {
                Debug.LogError($"[{gameObject.name}] 씬에서 Boss2Controller를 찾을 수 없습니다!");
                IsOn = true;
                return;
            }

            if (triggerDefeat)
            {
                bossTarget.SetDefeated();
#if UNITY_EDITOR
                Debug.Log($"<color=red>[{gameObject.name}]</color> 보스를 사망(Defeated) 상태로 전환했습니다.");
#endif
            }
            else
            {
                // 페이즈 결정 및 전환 신호 발송
                TriggerBossNextPhase();
            }

            IsOn = true;
        }

        private void TriggerBossNextPhase()
        {
            BossPhase nextPhase;

            if (useNextSequentialPhase)
            {
                // 현재 페이즈 기준 다음 페이즈 순차 계산
                switch (bossTarget.currentPhase)
                {
                    case BossPhase.Phase1:
                        nextPhase = BossPhase.Phase2;
                        break;
                    case BossPhase.Phase2:
                    case BossPhase.Phase2_5:
                        nextPhase = BossPhase.Phase3;
                        break;
                    default:
                        nextPhase = BossPhase.Phase3;
                        break;
                }
            }
            else
            {
                // 수동 지정 페이즈
                nextPhase = targetPhase;
            }

            // 보스의 스턴 타이머를 깨고 지정된 페이즈로 전환 및 리셋 실행
            bossTarget.ForceRecoverFromStun(nextPhase);

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> 보스의 스턴을 해제하고 [{nextPhase}] 페이즈로 전환했습니다.");
#endif
        }
    }
}