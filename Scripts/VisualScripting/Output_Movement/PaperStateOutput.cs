using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 플레이어의 PaperState(눕기 상태)를 전환하는 Output 모듈.
    /// targetState 조건에 맞춰 PaperState를 설정하고, 실제 회전 애니메이션이 완료될 때까지 기다린 뒤 다음 노드로 신호를 넘깁니다.
    /// </summary>
    public class PaperStateOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("제어할 플레이어의 Locomotion 스크립트를 할당하세요.")]
        [SerializeField] private PlayerLocomotion player;

        [Tooltip("설정할 목표 상태 (체크 시 눕기, 체크 해제 시 일어서기)")]
        [SerializeField] private bool targetState;

        [Header("Wait Settings")]
        [Tooltip("회전이 완료됐다고 판단할 각도 오차 기준값 (도). 작을수록 더 완전히 기다립니다.")]
        [SerializeField] private float completionAngleThreshold = 5f;

        [Tooltip("회전이 완료된 후 추가로 기다릴 시간 (초). 다음 노드가 너무 빠르게 실행되는 것을 방지합니다.")]
        [SerializeField] private float extraWaitAfterComplete = 0.05f;

        [Tooltip("최대 대기 시간 (초). 이 시간이 지나면 회전이 완료되지 않아도 강제로 다음으로 넘어갑니다.")]
        [SerializeField] private float maxWaitTime = 2f;

        public override void Execute()
        {
            IsOn = false;

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerLocomotion>();
            }

            if (player != null)
            {
                StartCoroutine(SetStateAndWait());
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] PlayerLocomotion 레퍼런스를 찾을 수 없습니다.");
                IsOn = true;
            }
        }

        private IEnumerator SetStateAndWait()
        {
            // 1. 현재 플레이어의 paperState 상태 확인
            bool currentPaper = player.GetPaperState();

            // 조건 비교: 현재 상태와 targetState가 다를 때만 변경
            // - currentPaper == true  & targetState == false -> false로 변경
            // - currentPaper == false & targetState == true  -> true로 변경
            // - currentPaper == true  & targetState == true  -> 그대로 둠
            // - currentPaper == false & targetState == false -> 그대로 둠
            if (currentPaper != targetState)
            {
                player.SetAbsolutePaperState(targetState);
            }

#if UNITY_EDITOR
            string stateName = targetState ? "페이퍼(눕기)" : "일반(서기)";
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> 플레이어 상태 확인 및 적용 (목표: {stateName}), 회전 대기 중...");
#endif

            Rigidbody rb = player.GetComponent<Rigidbody>();
            float elapsed = 0f;

            yield return new WaitForFixedUpdate();

            // 2. 회전 완료까지 대기
            while (elapsed < maxWaitTime)
            {
                elapsed += Time.deltaTime;

                if (rb != null)
                {
                    // 머리 위 방향 벡터(Up Vector)로 기울기 측정 (Y축 바라보는 방향 상관없이 정확)
                    Vector3 characterUp = rb.rotation * Vector3.up;
                    float currentTiltAngle = Vector3.Angle(characterUp, Vector3.up);

                    // 목표 기울기 각도: 눕기(true) = 90도, 서기(false) = 0도
                    float targetTiltAngle = targetState ? 90f : 0f;
                    float diff = Mathf.Abs(currentTiltAngle - targetTiltAngle);

                    if (diff <= completionAngleThreshold)
                    {
                        break; // 회전 완료
                    }
                }
                else
                {
                    break;
                }

                yield return null;
            }

            // 3. 추가 대기 (버퍼)
            if (extraWaitAfterComplete > 0f)
            {
                yield return new WaitForSeconds(extraWaitAfterComplete);
            }

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> 회전 완료! 다음 노드로 신호를 전달합니다. (경과: {elapsed:F2}초)");
#endif

            IsOn = true;
        }
    }
}