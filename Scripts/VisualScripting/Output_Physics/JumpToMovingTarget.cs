using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 입력 신호를 받으면 지정한 움직이는 타겟의 위치를 실시간으로 추적하며 포물선을 그려 착지하는 Output 컴포넌트.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Outputs/움직이는 타겟 점프 (Output)")]
    public class JumpToMovingTargetOutput : ProcessBase
    {
        [Header("Jumper & Target Setup")]
        [Tooltip("점프를 수행할 오브젝트 (비워두면 이 스크립트가 붙은 오브젝트가 점프합니다)")]
        [SerializeField] private Transform jumperTransform;

        [Tooltip("추적하여 착지할 움직이는 타겟 오브젝트")]
        [SerializeField] private Transform targetTransform;

        [Header("Jump Physics Settings")]
        [Tooltip("점프 최고 높이")]
        [SerializeField] private float jumpHeight = 3.0f;

        [Tooltip("점프 시작부터 착지까지 걸리는 시간 (초)")]
        [SerializeField] private float jumpDuration = 1.0f;

        [Header("Options")]
        [Tooltip("착지 시 타겟의 자식(Child)으로 붙을지 여부 (움직이는 발판 등에 유용)")]
        [SerializeField] private bool attachToTargetOnLand = false;

        private Coroutine _jumpCoroutine;

        private void Awake()
        {
            if (jumperTransform == null)
            {
                jumperTransform = transform;
            }
        }

        public override void Execute()
        {
            IsOn = false; // 점프 동작 시작 (다음 노드 대기)

            if (jumperTransform == null || targetTransform == null)
            {
                Debug.LogWarning($"[{gameObject.name}] jumperTransform 또는 targetTransform이 설정되지 않았습니다.");
                IsOn = true; // 오류 시 막히지 않고 즉시 다음 노드로
                return;
            }

            if (_jumpCoroutine != null)
            {
                StopCoroutine(_jumpCoroutine);
            }

            _jumpCoroutine = StartCoroutine(JumpRoutine());
        }

        private IEnumerator JumpRoutine()
        {
            Vector3 startPos = jumperTransform.position;
            float elapsed = 0f;

            while (elapsed < jumpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / jumpDuration);

                // 1. 매 프레임 타겟의 '현재 위치'를 읽어와 수평 보간 (Target이 움직여도 실시간 추적)
                Vector3 currentTargetPos = targetTransform.position;
                Vector3 currentPos = Vector3.Lerp(startPos, currentTargetPos, t);

                // 2. Y축 높이는 Sin 곡선을 사용하여 포물선 형성 (sin(0 ~ PI))
                float heightOffset = Mathf.Sin(t * Mathf.PI) * jumpHeight;
                currentPos.y += heightOffset;

                jumperTransform.position = currentPos;
                yield return null;
            }

            // 3. 착지 지점 정밀 고정
            jumperTransform.position = targetTransform.position;

            // 4. 움직이는 발판/몬스터일 경우 착지 후 함께 이동하도록 부모 설정
            if (attachToTargetOnLand)
            {
                jumperTransform.SetParent(targetTransform);
            }

            IsOn = true; // ★ 착지 완료! 다음 비주얼 노드로 바톤 터치
            _jumpCoroutine = null;
        }
    }
}