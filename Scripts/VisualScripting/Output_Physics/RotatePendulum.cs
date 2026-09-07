using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정한 축을 기준으로 진자운동(왔다갔다 1회) 회전을 수행하는 Process/Output 컴포넌트입니다.
    /// 양 끝점으로 갈수록 자연스럽게 감속(Ease In-Out)됩니다.
    /// </summary>
    public class RotatePendulum : ProcessBase
    {
        public enum RotateAxis
        {
            X,
            Y,
            Z
        }

        [Header("Target Settings")]
        [Tooltip("회전시킬 대상 오브젝트 (미지정 시 현재 오브젝트)")]
        [SerializeField] private GameObject targetObj;

        [Header("Pendulum Settings")]
        [Tooltip("회전 중심 축 (X, Y, Z 중 선택)")]
        [SerializeField] private RotateAxis rotateAxis = RotateAxis.Z;

        [Tooltip("최대 회전 각도 (예: 45도 지정 시 +45도까지 갔다가 -45도 거쳐 원위치 복귀)")]
        [SerializeField] private float maxSwingAngle = 45.0f;

        [Tooltip("왕복 1회 전체 동작에 걸리는 시간 (초)")]
        [SerializeField] private float duration = 2.0f;

        private Coroutine _swingCoroutine;
        private Quaternion _initialRotation;

        private void Awake()
        {
            if (!targetObj) targetObj = this.gameObject;
        }

        public override void Execute()
        {
            IsOn = false;

            if (targetObj == null || duration <= 0f)
            {
                IsOn = true;
                return;
            }

            StartSwing();
        }

        private void StartSwing()
        {
            if (_swingCoroutine != null) StopCoroutine(_swingCoroutine);
            _swingCoroutine = StartCoroutine(SwingRoutine());
        }

        private IEnumerator SwingRoutine()
        {
            _initialRotation = targetObj.transform.localRotation;
            float elapsedTime = 0f;

            // 회전 축 방향 설정
            Vector3 axisVector = Vector3.forward;
            switch (rotateAxis)
            {
                case RotateAxis.X: axisVector = Vector3.right; break;
                case RotateAxis.Y: axisVector = Vector3.up; break;
                case RotateAxis.Z: axisVector = Vector3.forward; break;
            }

            while (elapsedTime < duration)
            {
                yield return new WaitForFixedUpdate();
                elapsedTime += Time.fixedDeltaTime;

                // 0.0 ~ 1.0 진행률
                float progress = Mathf.Clamp01(elapsedTime / duration);

                // Sine 파형을 이용한 진자 각도 연산
                // Sin(0) = 0 -> Sin(PI) = 0 -> Sin(2*PI) = 0
                // 0 -> +maxSwingAngle -> -maxSwingAngle -> 0 으로 부드럽게 왕복
                float currentAngle = Mathf.Sin(progress * Mathf.PI * 2.0f) * maxSwingAngle;

                // SmoothStep 효과 추가 (양 끝단에서 감속)
                Quaternion targetRotation = _initialRotation * Quaternion.AngleAxis(currentAngle, axisVector);

                Rigidbody rb = targetObj.GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.MoveRotation(targetRotation);
                }
                else
                {
                    targetObj.transform.localRotation = targetRotation;
                }
            }

            // 정확한 초기 회전 값으로 복원
            targetObj.transform.localRotation = _initialRotation;

            _swingCoroutine = null;
            IsOn = true;
        }

        /// <summary>
        /// 외부에서 강제로 회전을 정지시키고 초기 회전 상태로 복원할 때 사용
        /// </summary>
        public void StopSwing()
        {
            if (_swingCoroutine != null)
            {
                StopCoroutine(_swingCoroutine);
                _swingCoroutine = null;
                if (targetObj != null)
                {
                    targetObj.transform.localRotation = _initialRotation;
                }
                IsOn = true;
            }
        }
    }
}