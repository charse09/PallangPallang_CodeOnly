using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class SmoothMatchTarget : ProcessBase
    {
        [Header("Target Settings")]
        [SerializeField] private GameObject objToMove;
        [Tooltip("위치와 회전을 맞출 목표 오브젝트")]
        [SerializeField] private GameObject targetObj;

        [Header("Match Settings")]
        [Tooltip("목표 지점까지 도달하는 데 걸리는 시간")]
        [SerializeField] private float matchDuration = 1.0f;

        [Tooltip("시간의 흐름에 따른 보간 곡선 (끝부분을 완만하게 꺾으면 '서서히 맞춰지는' 느낌이 극대화됩니다)")]
        [SerializeField] private AnimationCurve matchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Options")]
        [SerializeField] private bool matchPosition = true;
        [SerializeField] private bool matchRotation = true;

        private Coroutine _matchCoroutine;

        private void Awake()
        {
            if (!objToMove) objToMove = this.gameObject;
        }

        public override void Execute()
        {
            IsOn = false;

            if (objToMove == null || targetObj == null)
            {
                IsOn = true;
                return;
            }

            StartMatching();
        }

        private void StartMatching()
        {
            if (_matchCoroutine != null) StopCoroutine(_matchCoroutine);
            _matchCoroutine = StartCoroutine(MatchRoutine());
        }

        private IEnumerator MatchRoutine()
        {
            Rigidbody rb = objToMove.GetComponent<Rigidbody>();

            // 기존 프레임워크처럼 이동 중 물리 간섭을 막으려면 Kinematic을 켜주는 것이 좋습니다.
            bool wasKinematic = false;
            if (rb != null)
            {
                wasKinematic = rb.isKinematic;
                rb.isKinematic = true;
            }

            Vector3 startPos = objToMove.transform.position;
            Quaternion startRot = objToMove.transform.rotation;

            float elapsedTime = 0f;

            // matchDuration 동안 서서히 목표 위치/회전값으로 보간
            while (elapsedTime < matchDuration)
            {
                yield return new WaitForFixedUpdate();

                elapsedTime += Time.fixedDeltaTime;

                // 0 ~ 1 사이의 진행도
                float t = Mathf.Clamp01(elapsedTime / matchDuration);

                // 커브를 적용하여 러프한 가감속 느낌을 줌
                float curveT = matchCurve.Evaluate(t);

                // 타겟이 움직이고 있을 수 있으므로 매 프레임 타겟의 현재 Transform을 참조합니다.
                Vector3 currentTargetPos = targetObj.transform.position;
                Quaternion currentTargetRot = targetObj.transform.rotation;

                // 위치 보간 (Lerp)
                if (matchPosition)
                {
                    Vector3 nextPos = Vector3.Lerp(startPos, currentTargetPos, curveT);
                    if (rb != null) rb.MovePosition(nextPos);
                    else objToMove.transform.position = nextPos;
                }

                // 회전 보간 (Slerp)
                if (matchRotation)
                {
                    Quaternion nextRot = Quaternion.Slerp(startRot, currentTargetRot, curveT);
                    if (rb != null) rb.MoveRotation(nextRot);
                    else objToMove.transform.rotation = nextRot;
                }
            }

            // --- 루프 종료 후 확실하게 타겟과 100% 일치시키기 (오차 보정) ---
            if (matchPosition)
            {
                if (rb != null) rb.MovePosition(targetObj.transform.position);
                else objToMove.transform.position = targetObj.transform.position;
            }
            if (matchRotation)
            {
                if (rb != null) rb.MoveRotation(targetObj.transform.rotation);
                else objToMove.transform.rotation = targetObj.transform.rotation;
            }

            // 물리 상태 원상복구
            if (rb != null) rb.isKinematic = wasKinematic;

            _matchCoroutine = null;
            IsOn = true; // 다음 Visual Scripting 노드로 제어권 넘김
        }
    }
}