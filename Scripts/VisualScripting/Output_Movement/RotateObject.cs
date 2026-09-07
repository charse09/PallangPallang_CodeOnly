using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    [System.Serializable]
    public struct RotateAxis
    {
        public bool x;
        public bool y;
        public bool z;
    }

    /// <summary>
    /// 지정된 오브젝트를 커스텀 속도 그래프(AnimationCurve)에 맞춰 
    /// 부드러운 가속/감속과 함께 회전시키는 Output 모듈
    /// </summary>
    public class RotateObject : ProcessBase
    {
        [Tooltip("회전 대상")][SerializeField] private GameObject objectToRotate;
        [Tooltip("회전할 총 각도")][Range(-360f, 360f)][SerializeField] private float rotateAngle;
        [Tooltip("회전 축")][SerializeField] private RotateAxis axis;
        [Tooltip("회전하는 데 걸리는 총 시간")][SerializeField] private float timeToRotate = 1f;

        [Header("Juice Settings (가감속 연출)")]
        [Tooltip("회전 속도의 변화를 제어하는 그래프입니다.\n" +
                 "- 시작할 때 천천히, 끝날 때 빠르게 쿵 닫히게 하려면: 완만하다가 마지막에 급격히 올라가는 곡선을 그리세요.\n" +
                 "- 기본값은 일정한 등속도 직선입니다.")]
        [SerializeField] private AnimationCurve rotationEase = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("1회 회전 후 노드를 완료 상태로 고정할지 여부")]
        [SerializeField] private bool rotateOnce = true;

        private bool _isRotating = false;

        public override void Execute()
        {
            // 중복 실행 방지 및 예외 처리
            if (_isRotating) return;
            if (CheckNull())
            {
                IsOn = true;
                return;
            }

            StartCoroutine(C_Rotate());
        }

        private IEnumerator C_Rotate()
        {
            _isRotating = true;
            IsOn = false; // 회전이 시작될 때는 완료 신호를 잠시 끕니다.

            Transform tf = objectToRotate.transform;

            // Rigidbody가 있으면 MoveRotation으로 구동 (자식 Kinematic Rigidbody가 따라올 수 있도록)
            // 없으면 기존대로 Transform.localRotation 직접 조작
            Rigidbody rb = objectToRotate.GetComponent<Rigidbody>();
            bool originalKinematic = false;
            RigidbodyInterpolation originalInterpolation = RigidbodyInterpolation.None;

            if (rb != null)
            {
                originalKinematic = rb.isKinematic;
                originalInterpolation = rb.interpolation;

                rb.isKinematic = true; // 회전 중 MoveRotation을 위해 Kinematic 활성화
                rb.interpolation = RigidbodyInterpolation.Interpolate; // 부드러운 회전 보간
            }

            // 1) 시작 회전값 저장 및 목표 회전값 계산
            // Rigidbody가 있으면 worldRotation 기준으로 계산, 없으면 localRotation 기준
            Quaternion startRotation = (rb != null) ? tf.rotation : tf.localRotation;

            // 다중 축 선택 처리 (선택 안 하면 Y축이 기본값)
            bool anyAxis = axis.x || axis.y || axis.z;
            Vector3 eulerDelta = anyAxis
                ? new Vector3(axis.x ? rotateAngle : 0f, axis.y ? rotateAngle : 0f, axis.z ? rotateAngle : 0f)
                : new Vector3(0f, rotateAngle, 0f);

            Quaternion targetRotation = startRotation * Quaternion.Euler(eulerDelta);

            float elapsedTime = 0f;

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 커스텀 회전 연출 가동: {rotateAngle}도, 목표 시간: {timeToRotate}초 (Rigidbody 사용: {rb != null})");
#endif

            // 2) 지정된 시간 동안 루프 구동
            while (elapsedTime < timeToRotate)
            {
                elapsedTime += Time.deltaTime;

                // 시간 흐름 비율 계산 (0.0 ~ 1.0)
                float timeRatio = Mathf.Clamp01(elapsedTime / timeToRotate);

                // 기획자가 인스펙터에 그린 그래프에서 현재 시점의 진행도 값을 치환해 옵니다.
                float evaluatedT = rotationEase.Evaluate(timeRatio);

                Quaternion currentRot = Quaternion.Slerp(startRotation, targetRotation, evaluatedT);

                if (rb != null)
                {
                    // ★ Rigidbody.MoveRotation: 물리 엔진을 통해 회전을 전달하므로
                    //   자식으로 붙은 Kinematic Rigidbody(플레이어 등)가 올바르게 따라옵니다.
                    //   WaitForFixedUpdate를 써야 물리 타임스텝과 싱크가 맞습니다.
                    rb.MoveRotation(currentRot);
                }
                else
                {
                    tf.localRotation = currentRot;
                }

                // Rigidbody MoveRotation은 FixedUpdate 타이밍에 처리되므로 FixedUpdate를 기다림
                if (rb != null)
                    yield return new WaitForFixedUpdate();
                else
                    yield return null;
            }

            // 3) 마지막 프레임 부동소수점 오차 차단 및 스냅 고정
            if (rb != null)
            {
                rb.MoveRotation(targetRotation);
                yield return new WaitForFixedUpdate();

                // 회전 완료 후 원래 상태(isKinematic, interpolation)로 복원
                rb.isKinematic = originalKinematic;
                rb.interpolation = originalInterpolation;
            }
            else
            {
                tf.localRotation = targetRotation;
            }

            _isRotating = false;

            // [프레임워크 규칙] 작업이 완료되면 다음 스크립팅 레고 블록으로 신호 토스
            IsOn = rotateOnce;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 회전 연출 완료.");
#endif
        }

        private bool CheckNull() => (objectToRotate is null || timeToRotate <= 0f);
    }
}