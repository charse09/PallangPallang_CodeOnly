using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 게임 오브젝트를 특정 타겟 위치로 포물선(점프) 궤적을 그리며 이동시키는 모듈
    /// </summary>
    public class ObjectJumpToTargetOutput : ProcessBase
    {
        [Header("Object Settings")]
        [Tooltip("점프시킬 대상 오브젝트입니다. 비워두면 이 스크립트가 붙은 오브젝트가 이동합니다.")]
        [SerializeField] private GameObject objectToJump;

        [Header("Target Settings")]
        [Tooltip("오브젝트가 도착할 목표 지점의 Transform (빈 게임 오브젝트를 배치해서 연결하세요)")]
        [SerializeField] private Transform targetPoint;

        [Header("Jump Settings")]
        [Tooltip("점프가 얼마나 높이 뛸지(최대 높이) 결정합니다.")]
        [SerializeField] private float jumpHeight = 3.0f;

        [Tooltip("점프부터 착지까지 걸리는 총 시간(초)입니다.")]
        [SerializeField] private float jumpDuration = 1.0f;

        public override void Execute()
        {
            // 실행 시작
            IsOn = false;

            // 목표 지점 검사
            if (targetPoint == null)
            {
                Debug.LogError($"[{gameObject.name}] Target Point가 비어있습니다! 목표 지점을 할당해주세요.");
                IsOn = true;
                return;
            }

            // 이동할 대상 지정 (비어있으면 자기 자신을 대상으로 설정)
            if (objectToJump == null)
            {
                objectToJump = this.gameObject;
            }

            // 코루틴을 사용하여 부드러운 점프 이동 시작
            StartCoroutine(JumpRoutine(objectToJump));
        }

        private IEnumerator JumpRoutine(GameObject targetObj)
        {
            // 대상에게 Rigidbody가 있는지 확인
            Rigidbody rb = targetObj.GetComponent<Rigidbody>();
            bool hasRigidbody = rb != null;
            bool wasKinematic = false;

            // [핵심 1] Rigidbody가 존재한다면 물리 연산 잠시 비활성화
            if (hasRigidbody)
            {
                wasKinematic = rb.isKinematic;
                rb.isKinematic = true;
            }

            Vector3 startPos = targetObj.transform.position;
            Vector3 endPos = targetPoint.position;

            float timer = 0f;

            while (timer < jumpDuration)
            {
                timer += Time.deltaTime;

                // 진행도 (0.0 ~ 1.0)
                float t = Mathf.Clamp01(timer / jumpDuration);

                // [핵심 2] 포물선 공식 적용: 시작과 끝은 0, 중간(0.5)일 때 가장 높은 값을 가집니다.
                float heightOffset = 4f * jumpHeight * t * (1f - t);

                // 바닥 경로(선형 보간)를 먼저 계산한 뒤, Y축에 높이 오프셋을 더해줍니다.
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
                currentPos.y += heightOffset;

                // 위치 갱신
                targetObj.transform.position = currentPos;

                yield return null; // 다음 프레임까지 대기
            }

            // [마무리] 시간이 다 되면 정확히 타겟 위치에 안착시킵니다.
            targetObj.transform.position = endPos;

            // 물리가 원래 존재했다면 상태를 복구
            if (hasRigidbody)
            {
                rb.isKinematic = wasKinematic;
            }

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 대상 오브젝트({targetObj.name})가 목표 지점으로 점프 이동을 완료했습니다.");
#endif

            // 작업이 끝났으므로 다음 시퀀스로 신호 전달!
            IsOn = true;
        }
    }
}