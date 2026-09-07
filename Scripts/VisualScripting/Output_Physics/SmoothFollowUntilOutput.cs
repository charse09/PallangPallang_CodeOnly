using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정한 특정 Output(ProcessBase)의 IsOn이 true가 되기 전까지 
    /// 특정 오브젝트가 타겟을 부드럽게 따라가는 Output 컴포넌트.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Outputs/부드러운 추적 (Output)")]
    public class SmoothFollowUntilOutput : ProcessBase
    {
        [Header("Follow Setup")]
        [Tooltip("움직일 오브젝트 (비워두면 이 스크립트가 붙은 오브젝트)")]
        [SerializeField] private Transform followerTransform;

        [Tooltip("따라갈 목표 오브젝트")]
        [SerializeField] private Transform targetTransform;

        [Header("Stop Condition")]
        [Tooltip("이 Output(ProcessBase)의 IsOn이 true가 되면 추적을 멈추고 다음 노드로 진행합니다.")]
        [SerializeField] private ProcessBase stopConditionOutput;

        [Header("Smooth Movement Settings")]
        [Tooltip("추적 속도 (값이 높을수록 팽팽하게 바짝 따라붙습니다)")]
        [SerializeField] private float followSpeed = 5.0f;

        [Tooltip("위치뿐만 아니라 회전 방향도 부드럽게 맞춰갈지 여부")]
        [SerializeField] private bool followRotation = false;

        [Tooltip("회전 추적 속도")]
        [SerializeField] private float rotationSpeed = 5.0f;

        private Coroutine _followCoroutine;

        private void Awake()
        {
            if (followerTransform == null)
            {
                followerTransform = transform;
            }
        }

        public override void Execute()
        {
            IsOn = false; // 추적을 수행하는 동안은 false 유지

            if (followerTransform == null || targetTransform == null)
            {
                Debug.LogWarning($"[{gameObject.name}] followerTransform 또는 targetTransform이 지정되지 않았습니다.");
                IsOn = true; // 오류 시 프레임워크가 막히지 않도록 패스
                return;
            }

            if (_followCoroutine != null)
            {
                StopCoroutine(_followCoroutine);
            }

            _followCoroutine = StartCoroutine(FollowRoutine());
        }

        private IEnumerator FollowRoutine()
        {
            while (true)
            {
                // 1. 중지 조건 체크: 지정한 특정 Output이 IsOn = true가 되었는가?
                if (stopConditionOutput != null && stopConditionOutput.IsOn)
                {
                    break; // 추적 루프 탈출
                }

                // 2. 타겟 오브젝트가 파괴되었거나 사라지면 중지
                if (targetTransform == null)
                {
                    break;
                }

                // 3. 위치 부드럽게 따라가기 (Vector3.Lerp)
                followerTransform.position = Vector3.Lerp(
                    followerTransform.position,
                    targetTransform.position,
                    Time.deltaTime * followSpeed
                );

                // 4. 회전도 부드럽게 따라가는 옵션 처리
                if (followRotation)
                {
                    followerTransform.rotation = Quaternion.Slerp(
                        followerTransform.rotation,
                        targetTransform.rotation,
                        Time.deltaTime * rotationSpeed
                    );
                }

                yield return null; // 다음 프레임까지 대기
            }

            // 추적이 완료(중지)되었으므로 다음 비주얼 노드로 바톤 터치
            IsOn = true;
            _followCoroutine = null;
        }
    }
}