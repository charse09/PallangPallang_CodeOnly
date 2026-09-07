using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 오브젝트를 특정 축을 기준으로 원하는 각도만큼 부드럽게 회전시키는 Output 모듈
    /// </summary>
    public class RotateObjectOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("회전시킬 대상 오브젝트 (비워두면 이 스크립트가 붙은 오브젝트가 회전합니다)")]
        [SerializeField] private Transform targetTransform;

        [Header("Rotation Settings")]
        [Tooltip("회전의 기준이 될 축 (예: Y=1 이면 좌우 회전, X=1 이면 앞뒤 회전)")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [Tooltip("최종적으로 회전할 목표 각도(도 단위). 현재 각도에서 이만큼 더 회전합니다.")]
        [SerializeField] private float targetAngle = 90f;

        [Tooltip("초당 회전 속도 (도/초)")]
        [SerializeField] private float rotationSpeed = 90f;

        private bool isRotating = false;

        private void Start()
        {
            if (targetTransform == null)
            {
                targetTransform = transform;
            }
        }

        public override void Execute()
        {
            if (!isRotating && targetTransform != null)
            {
                StartCoroutine(RotateRoutine());
            }
        }

        private IEnumerator RotateRoutine()
        {
            isRotating = true;

            // 시작 각도와 목표 각도 계산
            Quaternion startRotation = targetTransform.rotation;

            // 현재 회전 상태에서 지정된 축으로 targetAngle만큼 더한 최종 회전값 계산
            Quaternion endRotation = startRotation * Quaternion.AngleAxis(targetAngle, rotationAxis);

            float currentAngle = 0f;
            float totalDistance = targetAngle; // 회전해야 할 총량

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 회전 시작! (목표: {targetAngle}도, 속도: {rotationSpeed})");
#endif

            // 목표 각도에 도달할 때까지 반복
            while (Quaternion.Angle(targetTransform.rotation, endRotation) > 0.1f)
            {
                // 현재 프레임에서 회전할 양 계산
                float step = rotationSpeed * Time.deltaTime;

                // RotateTowards를 사용하여 부드럽게 목표 각도로 근접
                targetTransform.rotation = Quaternion.RotateTowards(targetTransform.rotation, endRotation, step);

                yield return null;
            }

            // 오차 보정: 정확한 목표 각도로 맞춤
            targetTransform.rotation = endRotation;

            isRotating = false;

            // 액션 완료 알림 (다음 노드 실행 가능)
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 회전 완료.");
#endif
        }
    }
}