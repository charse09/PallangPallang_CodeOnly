using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// Rigidbody를 가진 오브젝트를 특정 피벗 오프셋(Pivot Offset)을 기준으로 물리(MovePosition/MoveRotation) 회전시키는 Output 모듈
    /// </summary>
    public class RotateRigidObjectOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("회전시킬 대상 오브젝트 (비워두면 이 스크립트가 붙은 오브젝트의 Rigidbody를 사용합니다)")]
        [SerializeField] private Rigidbody targetRigidbody;

        [Header("Pivot Offset Settings")]
        [Tooltip("회전 중심점이 될 오프셋 좌표 (예: 발바닥을 기준으로 회전하려면 Y를 오프셋 크기만큼 음수로 설정 ex: (0, -1, 0))")]
        [SerializeField] private Vector3 pivotOffset = Vector3.zero;

        [Tooltip("오프셋 적용 시 로컬 좌표계 기준 사용 여부 (체크 권장)")]
        [SerializeField] private bool useLocalOffset = true;

        [Header("Rotation Settings")]
        [Tooltip("회전의 기준이 될 축 (예: 앞뒤로 누우려면 X=-1 또는 X=1)")]
        [SerializeField] private Vector3 rotationAxis = Vector3.right;

        [Tooltip("최종적으로 회전할 목표 각도(도 단위). 현재 각도에서 이만큼 더 회전합니다.")]
        [SerializeField] private float targetAngle = -90f;

        [Tooltip("초당 회전 속도 (도/초)")]
        [SerializeField] private float rotationSpeed = 180f;

        private bool isRotating = false;

        private void Awake()
        {
            if (targetRigidbody == null)
            {
                targetRigidbody = GetComponent<Rigidbody>();
                if (targetRigidbody == null)
                {
                    targetRigidbody = GetComponentInParent<Rigidbody>();
                }
            }
        }

        public override void Execute()
        {
            if (!isRotating && targetRigidbody != null)
            {
                StartCoroutine(RotateRoutine());
            }
            else if (targetRigidbody == null)
            {
                Debug.LogError($"[{gameObject.name}] RotateRigidObjectOutput: Rigidbody를 찾을 수 없습니다!");
                IsOn = true;
            }
        }

        private IEnumerator RotateRoutine()
        {
            isRotating = true;
            IsOn = false;

            // 1. 회전 시점에서의 시작 위치와 회전값 캐싱
            Vector3 startPosition = targetRigidbody.position;
            Quaternion startRotation = targetRigidbody.rotation;

            // 2. 피벗(회전 중심점)의 월드 좌표 계산
            Vector3 worldPivotOffset = useLocalOffset ? startRotation * pivotOffset : pivotOffset;
            Vector3 pivotPoint = startPosition + worldPivotOffset;

            // 3. 목표 회전 Delta 계산
            Quaternion deltaRotation = Quaternion.AngleAxis(targetAngle, rotationAxis.normalized);
            Quaternion endRotation = startRotation * deltaRotation;

            // 4. 소요 시간 계산
            float absAngle = Mathf.Abs(targetAngle);
            float duration = (rotationSpeed > 0f) ? (absAngle / rotationSpeed) : 0f;
            float elapsedTime = 0f;

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 피벗 기준 Rigidbody 회전 시작!");
#endif

            if (duration > 0f)
            {
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsedTime / duration);

                    // 현재 단계의 회전 보간
                    Quaternion currentDelta = Quaternion.Slerp(Quaternion.identity, deltaRotation, t);
                    Quaternion currentRotation = startRotation * currentDelta;

                    // 피벗 중심 위치 재계산 (회전에 따라 위치도 함께 원호를 그리며 이동)
                    Vector3 currentPosition = pivotPoint - (currentDelta * worldPivotOffset);

                    // Rigidbody 물리 연산으로 위치와 회전 동시 업데이트
                    if (!targetRigidbody.isKinematic)
                    {
                        targetRigidbody.MoveRotation(currentRotation);
                        targetRigidbody.MovePosition(currentPosition);
                    }
                    else
                    {
                        targetRigidbody.transform.rotation = currentRotation;
                        targetRigidbody.transform.position = currentPosition;
                    }

                    yield return null;
                }
            }

            // 5. 오차 보정: 최종 위치 및 회전 스냅
            Vector3 finalPosition = pivotPoint - (deltaRotation * worldPivotOffset);
            if (!targetRigidbody.isKinematic)
            {
                targetRigidbody.MoveRotation(endRotation);
                targetRigidbody.MovePosition(finalPosition);
            }
            targetRigidbody.transform.rotation = endRotation;
            targetRigidbody.transform.position = finalPosition;

            isRotating = false;
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 피벗 기준 Rigidbody 회전 완료.");
#endif
        }

        private void OnDrawGizmosSelected()
        {
            // 에디터 씬 뷰에서 피벗 위치를 기즈모(분홍색 구체)로 시각화
            if (targetRigidbody != null)
            {
                Vector3 worldPivot = targetRigidbody.position + (useLocalOffset ? targetRigidbody.rotation * pivotOffset : pivotOffset);
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(worldPivot, 0.15f);
                Gizmos.DrawWireSphere(worldPivot, 0.3f);
            }
        }
    }
}