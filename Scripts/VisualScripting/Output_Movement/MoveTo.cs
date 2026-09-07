using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class MoveTo : ProcessBase
    {
        [Header("Target Settings")]
        [SerializeField] private GameObject objToMove;
        [SerializeField] private Transform destination;

        [Header("Movement Settings")]
        [SerializeField] private float duration = 1.0f;
        [SerializeField] private AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);

        private Coroutine _moveCoroutine;

        private void Awake()
        {
            if (!objToMove) objToMove = this.gameObject;
        }

        public override void Execute()
        {
            // [수정] 실행 시작 시 대기 상태로 만듦
            IsOn = false;

            if (objToMove != null && destination != null)
            {
                StartMove();
            }
            else
            {
                Debug.LogWarning($"{name}: 이동할 오브젝트나 목적지가 설정되지 않았습니다.");
                IsOn = true; // 에러 시에는 다음으로 넘기기
            }
        }

        private void StartMove()
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(MoveRoutine());
        }

        private IEnumerator MoveRoutine()
        {
            Vector3 startPosition = objToMove.transform.position;
            Vector3 targetPosition = destination.position;
            float elapsedTime = 0f;

            Rigidbody rb = objToMove.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                float curveValue = movementCurve.Evaluate(t);
                objToMove.transform.position = Vector3.Lerp(startPosition, targetPosition, curveValue);
                yield return null;
            }

            objToMove.transform.position = targetPosition;
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            _moveCoroutine = null;

            // [수정] 이동이 완벽하게 끝난 이 시점에 완료 신호 발송!
            IsOn = true;
        }

        private void OnDrawGizmos()
        {
            if (destination != null)
            {
                Vector3 startPos = (objToMove != null) ? objToMove.transform.position : transform.position;
                Vector3 endPos = destination.position;
                Gizmos.color = Color.green;
                Gizmos.DrawLine(startPos, endPos);
                Gizmos.DrawWireSphere(endPos, 0.5f);
            }
        }
    }
}