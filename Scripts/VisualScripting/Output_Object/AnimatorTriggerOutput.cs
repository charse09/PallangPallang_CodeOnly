using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// Animator의 SetTrigger()를 호출하여 애니메이션을 재생하는 Output 노드.
    ///
    /// [AnimatorPlayOutput과의 차이]
    /// - AnimatorPlayOutput : animator.Play("상태명") → Animator Controller의 전환 흐름을 무시하고 강제 진입.
    ///   ExitTime이나 Transition 조건이 무시되어 예상치 못한 동작이 발생할 수 있음.
    ///
    /// - AnimatorTriggerOutput : animator.SetTrigger("트리거명") → Controller에 정의된 전환 흐름을 그대로 따름.
    ///   BossAnim.controller처럼 Trigger 파라미터로 설계된 컨트롤러에 올바른 방식.
    ///
    /// [BossAnim.controller 기준 파라미터]
    ///   - "ElectricDischarge" → BossElectric 상태
    ///   - "SparkStrike"       → BossSparkStrike 상태
    ///   - "GimmikPrepare"     → BossHandEarthquake 상태
    /// </summary>
    public class AnimatorTriggerOutput : ProcessBase
    {
        [Header("Animator Settings")]
        [Tooltip("Trigger를 보낼 대상 Animator (비우면 자기 자신에서 자동 탐색)")]
        [SerializeField] private Animator targetAnimator;

        [Tooltip("호출할 Animator Trigger 파라미터 이름 (Animator 창의 Parameters 탭에 있는 이름)\n" +
                 "예: ElectricDischarge / SparkStrike / GimmikPrepare")]
        [SerializeField] private string triggerName = "ElectricDischarge";

        [Tooltip("이 Trigger로 진입하는 Animator State의 이름 (반복 재생 대기에 사용)\n" +
                 "예: BossElectric / BossSparkStrike / BossHandEarthquake\n" +
                 "※ loopCount > 1일 때, 이 상태가 끝날 때까지 기다렸다가 다음 Trigger를 보냅니다.")]
        [SerializeField] private string triggeredStateName = "BossElectric";

        [Tooltip("Execute() 호출 시 이 Trigger를 반복할 횟수.\n" +
                 "1 = 1회 재생. 3 = 애니메이션이 끝날 때마다 3번 반복.\n" +
                 "※ triggeredStateName이 올바르게 입력되어야 반복이 정확하게 동작합니다.")]
        [SerializeField] private int loopCount = 1;

        [Tooltip("상태 감지 타임아웃 (초). 이 시간 안에 상태 전환이 감지되지 않으면 다음으로 넘어갑니다.\n" +
                 "애니메이션 길이보다 충분히 크게 설정하세요.")]
        [SerializeField] private float stateTimeout = 10f;

        private bool isPlaying = false;

        private void Start()
        {
            if (targetAnimator == null)
                targetAnimator = GetComponent<Animator>();

            if (targetAnimator == null)
                targetAnimator = GetComponentInParent<Animator>();

#if UNITY_EDITOR
            if (targetAnimator == null)
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> [AnimatorTriggerOutput] " +
                                 "Animator를 찾지 못했습니다! Inspector에서 직접 할당해주세요.");
#endif
        }

        public override void Execute()
        {
            if (isPlaying)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=orange>[{gameObject.name}]</color> [AnimatorTriggerOutput] " +
                                 "이미 재생 중입니다. 중복 Execute() 무시.");
#endif
                return;
            }

            if (targetAnimator == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> [AnimatorTriggerOutput] " +
                                 "Animator가 없어 Trigger를 보낼 수 없습니다!");
#endif
                return;
            }

            StartCoroutine(TriggerRoutine());
        }

        private IEnumerator TriggerRoutine()
        {
            isPlaying = true;
            int clampedLoop = Mathf.Max(1, loopCount);

            for (int i = 0; i < clampedLoop; i++)
            {
#if UNITY_EDITOR
                Debug.Log($"<color=yellow>[{gameObject.name}]</color> [AnimatorTriggerOutput] " +
                          $"SetTrigger(\"{triggerName}\") 호출 ({i + 1}/{clampedLoop}회)");
#endif
                // 1. Trigger 발사 → Controller의 전환 흐름을 따라 상태 진입
                targetAnimator.SetTrigger(triggerName);

                // loopCount가 1이면 대기 없이 즉시 완료
                if (clampedLoop == 1)
                    break;

                // 2. Animator가 전환을 처리할 때까지 1프레임 대기
                yield return null;
                yield return null; // 전환 블렌딩 안정화를 위해 1프레임 추가 대기

                // 3. triggeredStateName 상태에 진입할 때까지 대기
                float elapsed = 0f;
                while (elapsed < stateTimeout)
                {
                    if (targetAnimator.GetCurrentAnimatorStateInfo(0).IsName(triggeredStateName))
                        break;
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (elapsed >= stateTimeout)
                {
#if UNITY_EDITOR
                    Debug.LogWarning($"<color=orange>[{gameObject.name}]</color> [AnimatorTriggerOutput] " +
                                     $"'{triggeredStateName}' 상태 진입 대기 타임아웃. " +
                                     $"triggeredStateName이 올바른지 확인하세요.");
#endif
                }

                // 4. triggeredStateName 상태가 끝날 때까지 대기 (Exit로 빠져나갈 때까지)
                elapsed = 0f;
                while (elapsed < stateTimeout)
                {
                    if (!targetAnimator.GetCurrentAnimatorStateInfo(0).IsName(triggeredStateName)
                        && !targetAnimator.IsInTransition(0))
                        break;
                    elapsed += Time.deltaTime;
                    yield return null;
                }

#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[{gameObject.name}]</color> [AnimatorTriggerOutput] " +
                          $"'{triggeredStateName}' {i + 1}회 완료. 다음 반복 준비...");
#endif
            }

            IsOn = true;
            isPlaying = false;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> [AnimatorTriggerOutput] " +
                      $"\"{triggerName}\" {clampedLoop}회 반복 완료!");
#endif
        }

        public override void Reset()
        {
            base.Reset();
            isPlaying = false;
        }
    }
}
