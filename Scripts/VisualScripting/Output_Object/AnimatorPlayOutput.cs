using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 대상 오브젝트의 Animator를 통해 특정 애니메이션 스테이트를 재생하는 Output 노드.
    /// loopCount를 설정하면 애니메이션이 끝날 때마다 순차적으로 반복 재생됩니다.
    /// ForLoop 안에 넣는 것과 달리, 이 방식은 각 재생이 끝난 후 다음 재생이 시작됩니다.
    /// </summary>
    public class AnimatorPlayOutput : ProcessBase
    {
        [Header("Animator Settings")]
        [Tooltip("애니메이션을 재생할 대상 Animator (비우면 자기 자신의 Animator를 찾습니다)")]
        [SerializeField] private Animator targetAnimator;

        [Tooltip("재생할 애니메이션 스테이트 이름 (Animator 창에 있는 네모 박스 이름입니다. 대소문자 주의!)")]
        [SerializeField] private string stateName = "Idle";

        [Tooltip("애니메이션 재생 속도 (1.0 = 정상 속도, 2.0 = 2배속, 0.5 = 절반)")]
        [SerializeField] private float playSpeed = 1.0f;

        [Tooltip("체크 ON: 애니메이션이 완전히 끝난 후 다음 신호를 넘깁니다. (반복 사용 필수)\n체크 OFF: 해당 애니메이션 재생 즉시 다른 노드로 신호를 넘깁니다. (단발성 이펙트용)")]
        [SerializeField] private bool waitForFinish = true;

        [Tooltip("Execute() 호출 시 이 애니메이션을 반복할 횟수입니다.\n1 = 1번 재생, 3 = 3번 반복 재생.\n※ waitForFinish가 체크되어 있어야 각 반복 사이에 끝날 때까지 기다립니다.")]
        [SerializeField] private int loopCount = 1;

        private bool isPlaying = false;

        private void Start()
        {
            // 타깃이 없으면 자기 자신의 Animator를 자동 탐색
            if (targetAnimator == null)
            {
                targetAnimator = GetComponent<Animator>();
            }
        }

        public override void Execute()
        {
            if (!isPlaying && targetAnimator != null)
            {
                StartCoroutine(PlayAnimationRoutine());
            }
            else if (targetAnimator == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> [AnimatorPlayOutput] 타깃 Animator가 없습니다!");
#endif
            }
        }

        private IEnumerator PlayAnimationRoutine()
        {
            isPlaying = true;

            int clampedLoop = Mathf.Max(1, loopCount);

            for (int i = 0; i < clampedLoop; i++)
            {
                // 재생 속도 설정
                targetAnimator.speed = playSpeed;

                // 스테이트 이름으로 애니메이션 재생 시작 (0번 레이어, 처음부터)
                targetAnimator.Play(stateName, 0, 0f);

                // Animator가 스테이트 전환을 처리할 때까지 1프레임 대기
                yield return null;

                if (!waitForFinish)
                {
                    // [즉시 완료] 기다리지 않고 바로 다음 액션 실행
                    // loopCount > 1이어도 첫 번째 재생 직후 종료됩니다.
                    IsOn = true;
                    isPlaying = false;
#if UNITY_EDITOR
                    Debug.Log($"<color=cyan>[{gameObject.name}]</color> [AnimatorPlayOutput] '{stateName}' 재생 시작. (즉시 완료 처리)");
#endif
                    yield break;
                }

                // 현재 재생 중인 애니메이션 정보 가져오기
                AnimatorStateInfo stateInfo = targetAnimator.GetCurrentAnimatorStateInfo(0);

                // 속도가 바뀌었으므로, 실제 대기 시간 = 클립 길이 ÷ 속도
                float actualWaitTime = stateInfo.length / Mathf.Abs(playSpeed);

#if UNITY_EDITOR
                Debug.Log($"<color=yellow>[{gameObject.name}]</color> [AnimatorPlayOutput] '{stateName}' 재생 중... " +
                          $"({i + 1}/{clampedLoop}회, {actualWaitTime:F1}초 대기)");
#endif

                // 애니메이션이 끝날 때까지 대기
                yield return new WaitForSeconds(actualWaitTime);
            }

            // 모든 반복이 끝난 후 완료 신호
            IsOn = true;
            isPlaying = false;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> [AnimatorPlayOutput] '{stateName}' {clampedLoop}회 반복 완료!");
#endif
        }
    }
}