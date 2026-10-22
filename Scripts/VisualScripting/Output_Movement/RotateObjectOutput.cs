using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class RotateObjectOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("회전시킬 대상 오브젝트 (비워두면 이 스크립트가 붙은 오브젝트가 회전합니다)")]
        [SerializeField] private Transform targetTransform;

        [Header("Rotation Settings")]
        [Tooltip("회전의 기준이 될 축")]
        [SerializeField] private Vector3 rotationAxis = Vector3.up;

        [Tooltip("최종적으로 회전할 목표 각도(도 단위)")]
        [SerializeField] private float targetAngle = 90f;

        [Tooltip("초당 회전 속도 (도/초)")]
        [SerializeField] private float rotationSpeed = 90f;

        private Coroutine _rotateCoroutine;

        private void Awake()
        {
            // Start() 대신 Awake()에서 초기화하여 첫 프레임 호출 시 null 방지
            if (targetTransform == null)
            {
                targetTransform = transform;
            }
        }

        public override void Execute()
        {
            IsOn = false;

            if (targetTransform != null)
            {
                if (_rotateCoroutine != null)
                {
                    StopCoroutine(_rotateCoroutine);
                }
                _rotateCoroutine = StartCoroutine(RotateRoutine());
            }
            else
            {
                Debug.LogWarning($"{name}: 회전할 대상(Target Transform)이 없습니다.");
                IsOn = true;
            }
        }

        private IEnumerator RotateRoutine()
        {
            Quaternion startRotation = targetTransform.rotation;
            Quaternion endRotation = startRotation * Quaternion.AngleAxis(targetAngle, rotationAxis);

            while (Quaternion.Angle(targetTransform.rotation, endRotation) > 0.1f)
            {
                float step = rotationSpeed * Time.deltaTime;
                targetTransform.rotation = Quaternion.RotateTowards(targetTransform.rotation, endRotation, step);
                yield return null;
            }

            targetTransform.rotation = endRotation;
            _rotateCoroutine = null;

            // 회전 완료 알림
            IsOn = true;
        }

        public override void Reset()
        {
            base.Reset();
            if (_rotateCoroutine != null)
            {
                StopCoroutine(_rotateCoroutine);
                _rotateCoroutine = null;
            }
            IsOn = false;
        }
    }
}