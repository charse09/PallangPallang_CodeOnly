using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// Rigidbody를 가진 오브젝트를 특정 피벗 오프셋(Pivot Offset)을 기준으로
    /// 지정한 절대 회전값(Euler Angles)까지 물리(MovePosition/MoveRotation) 회전시키는 Output 모듈
    /// </summary>
    public class SetRigidObjectRotationOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("회전시킬 대상 오브젝트 (비워두면 이 스크립트가 붙은 오브젝트의 Rigidbody를 사용합니다)")]
        [SerializeField] private Rigidbody targetRigidbody;

        [Header("Pivot Offset Settings")]
        [Tooltip("회전 중심점이 될 오프셋 좌표 (예: 발바닥을 기준으로 회전하려면 Y를 오프셋 크기만큼 음수로 설정 ex: (0, -1, 0))")]
        [SerializeField] private Vector3 pivotOffset = Vector3.zero;

        [Tooltip("오프셋 적용 시 로컬 좌표계 기준 사용 여부 (체크 권장)")]
        [SerializeField] private bool useLocalOffset = true;

        [Header("Absolute Rotation Settings")]
        [Tooltip("목표로 하는 절대 회전 각도 (Euler Angles, 예: 0, -90, 0)")]
        [SerializeField] private Vector3 targetEulerAngles = new Vector3(0f, -90f, 0f);

        [Tooltip("목표 회전 각도를 로컬 좌표계(부모 기준)로 적용할지 여부 (체크 해제 시 월드 좌표계 기준)")]
        [SerializeField] private bool useLocalRotation = false;

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
                Debug.LogError($"[{gameObject.name}] SetRigidObjectRotationOutput: Rigidbody를 찾을 수 없습니다!");
                IsOn = true;
            }
        }

        private IEnumerator RotateRoutine()
        {
            isRotating = true;
            IsOn = false;

            // 1. 회전 시작 시점의 위치와 회전값 캐싱
            Vector3 startPosition = targetRigidbody.position;
            Quaternion startRotation = targetRigidbody.rotation;

            // 2. 최종 목표 회전값(Quaternion) 계산
            Quaternion endRotation;
            if (useLocalRotation && targetRigidbody.transform.parent != null)
            {
                endRotation = targetRigidbody.transform.parent.rotation * Quaternion.Euler(targetEulerAngles);
            }
            else
            {
                endRotation = Quaternion.Euler(targetEulerAngles);
            }

            // 3. 피벗(회전 중심점)의 월드 좌표 고정 계산
            Vector3 worldPivotOffset = useLocalOffset ? startRotation * pivotOffset : pivotOffset;
            Vector3 pivotPoint = startPosition + worldPivotOffset;

            // 4. 회전 필요한 총 각도 차이 및 소요 시간 계산
            float totalAngle = Quaternion.Angle(startRotation, endRotation);
            float duration = (rotationSpeed > 0f && totalAngle > 0.01f) ? (totalAngle / rotationSpeed) : 0f;
            float elapsedTime = 0f;

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 피벗 기준 목표 각도({targetEulerAngles})로 Rigidbody 회전 시작!");
#endif

            if (duration > 0f)
            {
                while (elapsedTime < duration)
                {
                    elapsedTime += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsedTime / duration);

                    // 현재 단계의 회전 구면 선형 보간 (Slerp)
                    Quaternion currentRotation = Quaternion.Slerp(startRotation, endRotation, t);

                    // 현재 회전에 맞춘 피벗 중심 위치 재계산
                    Vector3 currentPivotOffset = useLocalOffset ? currentRotation * pivotOffset : pivotOffset;
                    Vector3 currentPosition = pivotPoint - currentPivotOffset;

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
            Vector3 finalPivotOffset = useLocalOffset ? endRotation * pivotOffset : pivotOffset;
            Vector3 finalPosition = pivotPoint - finalPivotOffset;

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
            Debug.Log($"<color=green>[{gameObject.name}]</color> 피벗 기준 목표 각도 회전 완료.");
#endif
        }

        private void OnDrawGizmosSelected()
        {
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