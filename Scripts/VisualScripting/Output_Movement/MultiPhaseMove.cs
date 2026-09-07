using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 인스펙터에서 시간과 속도를 설정하기 위한 커스텀 구조체
    /// </summary>
    [System.Serializable]
    public struct MovementPhase
    {
        [Tooltip("해당 속도를 유지할 시간 (초)")]
        public float duration;
        [Tooltip("해당 시간 동안 이동할 속도")]
        public float speed;
    }

    /// <summary>
    /// 설정된 여러 구간의 시간과 속도에 따라 목표 지점으로 순차 이동하는 Output 모듈
    /// </summary>
    public class MultiPhaseMove : ProcessBase
    {
        [Header("Movement Target")]
        [Tooltip("이동시킬 대상 (비워두면 이 스크립트가 붙은 오브젝트가 직접 이동합니다)")]
        [SerializeField] private Transform objectToMove;

        [Tooltip("최종적으로 도달해야 할 목표 지점 (Transform)")]
        [SerializeField] private Transform targetLocation;

        [Header("Phase Settings")]
        [Tooltip("순차적으로 실행될 이동 페이즈(시간, 속도) 목록을 추가하세요.")]
        [SerializeField] private List<MovementPhase> movementPhases = new List<MovementPhase>();

        private bool isMoving = false;

        private void Start()
        {
            // 대상을 지정하지 않았다면 자기 자신을 대상으로 설정
            if (objectToMove == null)
            {
                objectToMove = transform;
            }
        }

        /// <summary>
        /// 이전 노드(Trigger 또는 Logic)의 조건이 충족되어 신호를 받을 때 호출됩니다.
        /// </summary>
        public override void Execute()
        {
            // 이미 이동 중이거나 타겟이 없으면 실행하지 않음
            if (!isMoving && targetLocation != null && objectToMove != null)
            {
                StartCoroutine(MoveRoutine());
            }
        }

        private IEnumerator MoveRoutine()
        {
            isMoving = true;

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 다단계 이동 시작! 총 {movementPhases.Count}개의 페이즈 진행.");
#endif

            int phaseIndex = 1;

            // 등록된 페이즈 목록을 순서대로 꺼내서 실행
            foreach (var phase in movementPhases)
            {
                float timer = 0f;

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[{gameObject.name}]</color> Phase {phaseIndex}: {phase.duration}초 동안 속도 {phase.speed}(으)로 이동.");
#endif

                // 지정된 시간(duration) 동안 반복
                while (timer < phase.duration)
                {
                    timer += Time.deltaTime;

                    // 현재 위치에서 목표 위치 방향으로 이동 (초과 이동 방지를 위해 MoveTowards 사용)
                    objectToMove.position = Vector3.MoveTowards(
                        objectToMove.position,
                        targetLocation.position,
                        phase.speed * Time.deltaTime
                    );

                    // 목표 지점에 완벽히 도달했는지 검사
                    if (Vector3.Distance(objectToMove.position, targetLocation.position) <= 0.001f)
                    {
#if UNITY_EDITOR
                        Debug.Log($"<color=green>[{gameObject.name}]</color> 스케줄이 끝나기 전에 목표 지점에 도달하여 이동을 조기 종료합니다.");
#endif
                        break; // 현재 while 루프 탈출
                    }

                    yield return null; // 1프레임 대기
                }

                // 만약 목표에 도달해서 탈출한 것이라면, 남은 페이즈(foreach)도 전부 취소하고 종료
                if (Vector3.Distance(objectToMove.position, targetLocation.position) <= 0.001f)
                {
                    break;
                }

                phaseIndex++;
            }

            // 모든 이동 페이즈가 끝났거나 목표에 도달함
            isMoving = false;

            // 프레임워크 규칙: 이 노드의 행동이 끝났음을 알리기 위해 상태를 true로 변경
            // (이 노드 뒤에 또 다른 액션이 연결되어 있다면 그 액션이 실행됨)
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 다단계 이동 시퀀스 완료.");
#endif
        }

        private void OnDisable()
        {
            // 오브젝트가 비활성화되면 이동 상태 초기화
            isMoving = false;
            StopAllCoroutines();
        }
    }
}