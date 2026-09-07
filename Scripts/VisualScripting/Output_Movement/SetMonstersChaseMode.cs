using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class SetMonstersChaseMode : ProcessBase
    {
        [Header("Targets Settings")]
        [Tooltip("추적 모드로 변경할 몬스터(분신)들을 등록해주세요. (+/- 버튼으로 갯수 조절)")]
        [SerializeField] private List<ShabulkabunshinLocomotion> monsters = new List<ShabulkabunshinLocomotion>();

        [Tooltip("추적할 플레이어 타겟")]
        [SerializeField] private Transform playerTarget;

        public override void Execute()
        {
            // 노드 실행 시작
            IsOn = false;

            if (playerTarget == null)
            {
                Debug.LogWarning("SetMonstersChaseMode: 플레이어 타겟이 할당되지 않았습니다!");
                IsOn = true;
                return;
            }

            // 등록된 모든 몬스터를 순회하며 Chase 모드로 전환
            foreach (var monster in monsters)
            {
                if (monster != null)
                {
                    // ShabulkabunshinLocomotion에 내장된 추적 함수 실행
                    monster.SetTarget(playerTarget);
                }
            }

            // 애니메이션 대기나 강제 이동 없이 '상태 변경'만 지시하는 노드이므로,
            // 즉시 다음 노드로 제어권을 넘깁니다.
            IsOn = true;
        }
    }
}