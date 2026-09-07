using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    [System.Serializable]
    public struct LockAxis
    {
        [Tooltip("X축 회전을 고정합니다 (위/아래 끄덕임 방지)")]
        public bool lockX;
        [Tooltip("Y축 회전을 고정합니다 (좌/우 회전 방지)")]
        public bool lockY;
        [Tooltip("Z축 회전을 고정합니다 (기울어짐 방지)")]
        public bool lockZ;
    }

    /// <summary>
    /// 주체(Subject) 오브젝트가 타겟(Target) 오브젝트를 바라보도록 
    /// 커스텀 속도 그래프(AnimationCurve)에 맞춰 부드럽게 회전시키는 Output 모듈
    /// </summary>
    public class LookAtObject : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("회전시킬 주체 오브젝트 (미지정 시 본인 오브젝트 사용)")]
        [SerializeField] private GameObject subjectObject;

        [Tooltip("바라볼 목표 오브젝트")]
        [SerializeField] private Transform targetTransform;

        [Tooltip("회전에서 제외할 축 (예: Y축 평면 회전만 원할 경우 Lock X, Lock Z 체크)")]
        [SerializeField] private LockAxis lockAxis;

        [Tooltip("회전하는 데 걸리는 총 시간")]
        [SerializeField] private float timeToRotate = 1f;

        [Header("Juice Settings (가감속 연출)")]
        [Tooltip("회전 속도의 변화를 제어하는 그래프입니다.\n" +
                 "- 기본값은 일정한 등속도 직선입니다.")]
        [SerializeField] private AnimationCurve rotationEase = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Tooltip("1회 회전 완료 후 노드를 완료 상태로 고정할지 여부")]
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

            StartCoroutine(C_LookAt());
        }

        private IEnumerator C_LookAt()
        {
            _isRotating = true;
            IsOn = false; // 회전 시작 시 완료 신호를 off 처리

            // subjectObject가 지정되어 있지 않으면 스크립트 붙은 자신을 주체로 설정
            GameObject subject = subjectObject != null ? subjectObject : gameObject;
            Transform tf = subject.transform;

            // 1) 시작 회전값 및 목표 방향/회전값 계산
            Quaternion startRotation = tf.rotation; // World 회전 기준

            Vector3 directionToTarget = targetTransform.position - tf.position;

            // 방향 벡터가 거의 0일 경우 (동일 위치) 예외 처리
            if (directionToTarget.sqrMagnitude < 0.0001f)
            {
                _isRotating = false;
                IsOn = rotateOnce;
                yield break;
            }

            // 목표 바라보기 Quaternion 생성
            Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
            Vector3 targetEuler = lookRotation.eulerAngles;
            Vector3 startEuler = startRotation.eulerAngles;

            // 축 고정(Lock Axis) 적용: 고정된 축은 기존(시작) euler 각도 유지
            if (lockAxis.lockX) targetEuler.x = startEuler.x;
            if (lockAxis.lockY) targetEuler.y = startEuler.y;
            if (lockAxis.lockZ) targetEuler.z = startEuler.z;

            Quaternion targetRotation = Quaternion.Euler(targetEuler);

            float elapsedTime = 0f;

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> [{targetTransform.name}] 바라보기 회전 가동 (소요 시간: {timeToRotate}초)");
#endif

            // 2) 지정된 시간 동안 회전 실행
            while (elapsedTime < timeToRotate)
            {
                elapsedTime += Time.deltaTime;

                // 진행 비율 (0.0 ~ 1.0)
                float timeRatio = Mathf.Clamp01(elapsedTime / timeToRotate);

                // AnimationCurve 그래프에서 진행도 적용
                float evaluatedT = rotationEase.Evaluate(timeRatio);

                // 구면 선형 보간(Slerp)으로 회전 적용
                tf.rotation = Quaternion.Slerp(startRotation, targetRotation, evaluatedT);

                yield return null;
            }

            // 3) 오차 보정 및 스냅 고정
            tf.rotation = targetRotation;

            _isRotating = false;
            IsOn = rotateOnce;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> [{targetTransform.name}] 바라보기 회전 완료.");
#endif
        }

        private bool CheckNull()
        {
            if (targetTransform == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> Target Transform이 비어있습니다.");
#endif
                return true;
            }

            if (timeToRotate <= 0f) return true;

            return false;
        }
    }
}