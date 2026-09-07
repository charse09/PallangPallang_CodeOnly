using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 오브젝트(Rigidbody)에 특정 방향으로 물리적인 힘을 가하는 Output 모듈
    /// </summary>
    public class ApplyForceOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("힘을 적용할 대상 오브젝트를 넣으세요. (반드시 Rigidbody 컴포넌트가 있어야 합니다)")]
        [SerializeField] private Rigidbody targetRigidbody;

        [Header("Force Settings")]
        [Tooltip("힘을 가할 방향 (예: Y=1 이면 위로 점프, Z=1 이면 앞쪽으로 밀기)")]
        [SerializeField] private Vector3 forceDirection = new Vector3(0f, 1f, 0f);

        [Tooltip("힘의 세기")]
        [SerializeField] private float forceStrength = 10f;

        [Tooltip("힘을 가할 시간(초).\n0으로 설정하면 대포나 점프처럼 '즉발성 충격(Impulse)'을 가하고,\n0보다 크면 바람이나 부스터처럼 '지속성 힘(Continuous Force)'을 가합니다.")]
        [SerializeField] private float duration = 0f;

        private bool isApplyingForce = false;

        public override void Execute()
        {
            if (targetRigidbody == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> 힘을 가할 타겟 Rigidbody가 지정되지 않았습니다!");
#endif
                return;
            }

            // 이미 지속적인 힘을 받고 있는 중이라면 중복 실행 방지
            if (isApplyingForce) return;

            // 방향 벡터를 정규화(길이를 1로 맞춤)하여 순수한 방향만 남김
            Vector3 normalizedDirection = forceDirection.normalized;
            Vector3 finalForce = normalizedDirection * forceStrength;

            if (duration <= 0f)
            {
                // 1. 즉발성 힘 (적용 시간이 0일 때)
                // ForceMode.Impulse: 질량을 고려하여 순간적인 충격량을 가함
                targetRigidbody.AddForce(finalForce, ForceMode.Impulse);

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[{gameObject.name}]</color> 즉각적인 힘(Impulse) 적용 완료! (세기: {forceStrength})");
#endif
                IsOn = true; // 액션 완료
            }
            else
            {
                // 2. 지속성 힘 (적용 시간이 있을 때)
                StartCoroutine(ApplyContinuousForceRoutine(finalForce));
            }
        }

        private IEnumerator ApplyContinuousForceRoutine(Vector3 forceToApply)
        {
            isApplyingForce = true;
            float elapsed = 0f;

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> {duration}초 동안 지속적인 힘 적용 시작...");
#endif

            while (elapsed < duration)
            {
                // 물리 연산은 Update가 아닌 FixedUpdate 주기에 맞춰야 정확하게 작동합니다.
                // ForceMode.Force: 질량을 고려하여 지속적인 힘을 가함
                targetRigidbody.AddForce(forceToApply, ForceMode.Force);

                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            isApplyingForce = false;
            IsOn = true; // 액션 완료

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 지속적인 힘 적용 완료.");
#endif
        }
    }
}