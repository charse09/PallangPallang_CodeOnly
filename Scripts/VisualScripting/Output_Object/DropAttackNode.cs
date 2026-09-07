using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    [RequireComponent(typeof(Rigidbody))]
    public class DropAttackNode : ProcessBase
    {
        [Header("Drop Attack Settings")]
        [SerializeField] private float targetSpeed = 125f; // 목표 지점까지 이동 속도
        private float impactForce = 0f; // 착지 시 아래로 가할 충격량 (묵직함 결정)
        [SerializeField] private GameObject warningIndicator;

        [Header("Impact Effects")]
        [SerializeField] private GameObject explosionParticlePrefab; // (선택) 착지 시 바닥 먼지/파편 이펙트

        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true; // 초기: 스크립트 제어
        }

        public override void Execute()
        {
            if (IsOn) return;
            IsOn = true;
            StartCoroutine(HeavyDropRoutine());
        }

        private IEnumerator HeavyDropRoutine()
        {
            if (warningIndicator == null) yield break;

            Vector3 targetPosition = warningIndicator.transform.position;

            // 1. 목표 위치 바로 위까지 이동 (물리 꺼짐 상태)
            // (살짝 띄워야 물리 활성화 후 떨어지는 느낌이 납니다)
            targetPosition.y += 0.01f;

            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    targetSpeed * Time.deltaTime
                );
                yield return null;
            }

            // 2. 경고 표시 비활성화 및 이펙트 생성
            warningIndicator.SetActive(false);
            if (explosionParticlePrefab != null)
            {
                Instantiate(explosionParticlePrefab, transform.position, Quaternion.identity);
            }

            // 3. 묵직한 쇳덩이로 변신: 물리 활성화 및 충격 가하기
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // 관통 방지

            // 아래 방향으로 순간적인 힘을 가해 묵직하게 탁! 꽂히게 함
            rb.AddForce(Vector3.down * impactForce, ForceMode.Impulse);

#if UNITY_EDITOR
            Debug.Log($"[{gameObject.name}] 묵직하게 착지 완료!");
#endif
        }

        private void OnDisable()
        {
            IsOn = false;
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete; // 비활성화 시 기본값으로 복원
            }
        }
    }
}