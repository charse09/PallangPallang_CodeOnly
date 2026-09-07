using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 태그를 가진 오브젝트가 N미터 이내로 접근하면 발동하는 거리 기반 트리거
    /// </summary>
    public class DistanceTrigger : ProcessBase
    {
        [Header("Proximity Settings")]
        [Tooltip("접근을 감지할 대상의 태그입니다. (예: Player)")]
        [SerializeField] private string targetTag = "";

        [Tooltip("감지할 반경(거리)입니다. (단위: 미터)")]
        [SerializeField] private float detectRadius = 5.0f;

        [Tooltip("범위를 벗어났다가 다시 들어오면 계속해서 재발동할지 여부")]
        [SerializeField] private bool isRepeatable = false;

        [Tooltip("성능 최적화를 위한 거리 검사 주기(초). 0.1이면 1초에 10번만 검사합니다.")]
        [SerializeField] private float checkInterval = 0.1f;

        private Transform targetTransform;
        private bool isInside = false;
        private bool hasTriggered = false;

        private void Start()
        {
            // 타겟 찾기 및 감지 루프 시작
            FindTarget();
            StartCoroutine(CheckProximityRoutine());
        }

        private void FindTarget()
        {
            GameObject targetObj = GameObject.FindGameObjectWithTag(targetTag);
            if (targetObj != null)
            {
                targetTransform = targetObj.transform;
            }
        }

        private IEnumerator CheckProximityRoutine()
        {
            while (true)
            {
                // 타겟을 아직 못 찾았거나 파괴되었다면 다시 탐색
                if (targetTransform == null)
                {
                    FindTarget();
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }

                // 일회성 기믹인데 이미 발동했다면 더 이상 검사하지 않음
                if (!isRepeatable && hasTriggered)
                    yield break;

                // 내 위치와 타겟 위치 사이의 거리 계산
                float distance = Vector3.Distance(transform.position, targetTransform.position);

                // 타겟이 반경 N미터 이내로 들어왔을 때
                if (distance <= detectRadius)
                {
                    if (!isInside)
                    {
                        isInside = true;
                        IsOn = true; // 프레임워크 상태 켜기
                        hasTriggered = true;

#if UNITY_EDITOR
                        Debug.Log($"<color=cyan>[{gameObject.name}]</color> {targetTag}이(가) {detectRadius}m 반경 내로 진입! 트리거 발동.");
#endif
                    }
                }
                // 타겟이 반경 밖으로 나갔을 때
                else
                {
                    if (isInside)
                    {
                        isInside = false;

                        // 반복 가능한 트리거라면 상태를 초기화하여 다음 진입을 준비함
                        if (isRepeatable)
                        {
                            Reset(); // IsOn을 false로 변경
#if UNITY_EDITOR
                            Debug.Log($"<color=grey>[{gameObject.name}]</color> {targetTag}이(가) 반경을 벗어남. 트리거 초기화 완료.");
#endif
                        }
                    }
                }

                // 지정된 시간만큼 대기 후 다시 검사 (Update 대신 사용하여 성능 최적화)
                yield return new WaitForSeconds(checkInterval);
            }
        }

        public override void Execute()
        {
            // 외부에서 강제 실행 시 상태 반전
            IsOn = !IsOn;
        }

        // ==========================================
        // 에디터 씬 뷰에서 감지 반경을 시각적으로 보여주는 기능
        // ==========================================
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 오브젝트를 클릭했을 때 반투명한 노란색 구체로 감지 범위를 표시합니다.
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.3f);
            Gizmos.DrawSphere(transform.position, detectRadius);

            // 구체의 테두리 선 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectRadius);
        }
#endif
    }
}