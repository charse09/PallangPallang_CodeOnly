using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 오브젝트의 초기 위치를 기억하고, 그 위치로부터 [minDistance ~ maxDistance] 사이로 이동했을 때 발동하는 트리거
    /// </summary>
    public class DisplacementTrigger : ProcessBase
    {
        [Header("Displacement Settings")]
        [Tooltip("최소 이동 거리 (이 거리 '이상' 이동해야 발동)")]
        [SerializeField] private float minDistance = 2.0f;

        [Tooltip("최대 이동 거리 (이 거리 '미만'일 때만 유지/발동)")]
        [SerializeField] private float maxDistance = 5.0f;

        [Tooltip("조건 범위에 다시 들어올 때마다 계속해서 재발동할지 여부")]
        [SerializeField] private bool isRepeatable = false;

        [Tooltip("성능 최적화를 위한 거리 검사 주기(초)")]
        [SerializeField] private float checkInterval = 0.1f;

        private Vector3 startPosition;
        private bool isInsideRange = false;
        private bool hasTriggered = false;

        private void OnEnable()
        {
            // 오브젝트가 활성화(또는 씬 시작)될 때의 현재 위치를 기준점으로 기록합니다.
            startPosition = transform.position;
            StartCoroutine(CheckDisplacementRoutine());
        }

        private IEnumerator CheckDisplacementRoutine()
        {
            while (true)
            {
                // 일회성 기믹인데 이미 한 번 발동했다면 검사 종료
                if (!isRepeatable && hasTriggered)
                    yield break;

                // 시작 위치와 현재 위치 사이의 이동 거리(변위) 계산
                float distance = Vector3.Distance(startPosition, transform.position);

                // 이동 거리가 특정 미터 이상, 특정 미터 미만일 때
                if (distance >= minDistance && distance < maxDistance)
                {
                    if (!isInsideRange)
                    {
                        isInsideRange = true;
                        IsOn = true; // 프레임워크 상태 켜기 (다음 노드 실행)
                        hasTriggered = true;

#if UNITY_EDITOR
                        Debug.Log($"<color=cyan>[{gameObject.name}]</color> 이동 거리({distance:F1}m)가 {minDistance}m~{maxDistance}m 조건을 만족하여 트리거 발동!");
#endif
                    }
                }
                // 거리가 부족하거나, 너무 멀리 밀어버렸을 때
                else
                {
                    if (isInsideRange)
                    {
                        isInsideRange = false;

                        // 반복 가능한 트리거라면 상태를 초기화하여 다음 진입을 준비함
                        if (isRepeatable)
                        {
                            Reset(); // IsOn을 false로 변경
#if UNITY_EDITOR
                            Debug.Log($"<color=grey>[{gameObject.name}]</color> 유효 이동 범위를 벗어남. 트리거 초기화 완료.");
#endif
                        }
                    }
                }

                // 지정된 주기마다 검사 (프레임워크 최적화)
                yield return new WaitForSeconds(checkInterval);
            }
        }

        public override void Execute()
        {
            // 외부 강제 실행 시 기준점을 현재 위치로 재설정(영점 조절)하는 기능으로 활용
            startPosition = transform.position;
            isInsideRange = false;
            Reset();

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 외부 신호로 인해 기준 위치(Start Position)가 현재 위치로 재설정되었습니다.");
#endif
        }

        // ==========================================
        // 에디터 씬 뷰에서 이동 허용 범위를 시각화
        // ==========================================
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 플레이 모드일 때는 기록된 시작 위치를 기준으로, 에디터 모드일 때는 현재 위치를 기준으로 그립니다.
            Vector3 center = Application.isPlaying ? startPosition : transform.position;

            // 최소 거리 표시 (빨간색 선) - 이 원 밖으로 나가야 함
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, minDistance);

            // 최대 거리 표시 (초록색 선) - 이 원 안쪽에 있어야 함
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(center, maxDistance);
        }
#endif
    }
}