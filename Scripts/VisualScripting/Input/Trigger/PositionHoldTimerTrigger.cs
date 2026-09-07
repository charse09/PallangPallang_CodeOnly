using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 위치 반경 내에 타겟이 N초 동안 머무르면 IsOn 상태를 켜는 트리거
    /// </summary>
    public class PositionHoldTimerTrigger : ProcessBase
    {
        [Header("Location & Target Settings")]
        [Tooltip("감지할 기준 위치입니다. (비워두면 이 스크립트가 붙은 오브젝트의 위치를 사용합니다)")]
        [SerializeField] private Transform targetLocation;

        [Tooltip("접근을 감지할 대상의 태그입니다. (예: Player)")]
        [SerializeField] private string targetTag = "";

        [Tooltip("목표 위치로부터 인정되는 허용 반경(미터)")]
        [SerializeField] private float detectRadius = 3.0f;

        [Header("Timer Settings")]
        [Tooltip("해당 범위 내에서 위치를 유지해야 하는 시간(초)")]
        [SerializeField] private float requiredHoldTime = 3.0f;

        [Tooltip("발동 후 범위를 벗어났다가 다시 들어오면 재발동할지 여부")]
        [SerializeField] private bool isRepeatable = false;

        private Transform targetObject;
        private float currentHoldTime = 0f;
        private bool hasTriggered = false;

        private void Start()
        {
            // 타겟을 찾는 코루틴 시작 (최적화를 위해 Update 대신 0.5초마다 검색)
            StartCoroutine(FindTargetRoutine());
        }

        private IEnumerator FindTargetRoutine()
        {
            while (targetObject == null)
            {
                GameObject obj = GameObject.FindGameObjectWithTag(targetTag);
                if (obj != null)
                {
                    targetObject = obj.transform;
                }
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void Update()
        {
            // 일회성 기믹이고 이미 발동했다면 더 이상 검사하지 않음
            if (!isRepeatable && hasTriggered) return;

            // 타겟을 아직 못 찾았다면 대기
            if (targetObject == null) return;

            // 기준 위치 (할당된 Transform이 없으면 자기 자신의 위치 사용)
            Vector3 center = targetLocation != null ? targetLocation.position : transform.position;

            // 거리 계산
            float distance = Vector3.Distance(center, targetObject.position);

            // 범위 안에 있을 때 (타이머 증가)
            if (distance <= detectRadius)
            {
                currentHoldTime += Time.deltaTime;

                // 목표 시간 도달 시
                if (currentHoldTime >= requiredHoldTime)
                {
                    TriggerAction();
                }
            }
            // 범위 밖으로 나갔을 때 (타이머 초기화)
            else
            {
                if (currentHoldTime > 0f)
                {
                    currentHoldTime = 0f;
#if UNITY_EDITOR
                    Debug.Log($"<color=grey>[{gameObject.name}]</color> 타겟이 범위를 벗어나 타이머가 초기화되었습니다.");
#endif
                }
            }
        }

        private void TriggerAction()
        {
            hasTriggered = true;
            currentHoldTime = 0f; // 반복을 위해 타이머 초기화

            if (isRepeatable)
            {
                // 반복형: 시스템이 감지할 수 있도록 펄스(Pulse) 발생
                StartCoroutine(PulseRoutine());
            }
            else
            {
                // 일회성: 상태 영구 켬
                IsOn = true;
#if UNITY_EDITOR
                Debug.Log($"<color=green>[{gameObject.name}]</color> {requiredHoldTime}초 동안 위치 유지 완료! 트리거 발동.");
#endif
            }
        }

        private IEnumerator PulseRoutine()
        {
            IsOn = true;
#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> {requiredHoldTime}초 동안 위치 유지 완료! (반복형) 트리거 발동.");
#endif
            yield return null; // 1프레임 대기
            Reset(); // 상태 초기화
            hasTriggered = false; // 다음 반복을 위해 락 해제
        }

        public override void Execute()
        {
            // 수동 강제 발동
            IsOn = !IsOn;
        }

        // ==========================================
        // 에디터 씬 뷰 시각화 (기즈모)
        // ==========================================
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 center = targetLocation != null ? targetLocation.position : transform.position;

            // 감지 영역을 연한 초록색 구체로 표시
            Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
            Gizmos.DrawSphere(center, detectRadius);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(center, detectRadius);
        }
#endif
    }
}