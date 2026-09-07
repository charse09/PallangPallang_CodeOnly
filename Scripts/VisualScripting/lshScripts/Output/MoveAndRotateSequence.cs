using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 여러 지점을 순차 이동하며 각 지점에서 X축 회전 액션을 수행하는 Output
    /// </summary>
    public class MoveAndRotateSequence : ProcessBase
    {
        [Header("대상 설정")]
        [Tooltip("이동 및 회전시킬 대상 오브젝트")]
        [SerializeField] private Transform actorTransform;

        [Header("경로 설정")]
        [Tooltip("순차적으로 방문할 지점(Transform) 리스트")]
        [SerializeField] private List<Transform> targetPoints = new List<Transform>();

        [Header("이동 및 회전 파라미터")]
        [SerializeField] private float moveSpeed = 5f;

        [Tooltip("각 지점에서 회전할 X축 각도 (예: 45도 숙이기)")]
        [SerializeField] private float rotateAngleX = 45f;

        [Tooltip("회전 액션(숙였다가 돌아오기)에 걸리는 시간")]
        [SerializeField] private float actionDuration = 1.0f;

        private bool isRunning = false;

        public override void Execute()
        {
            if (actorTransform == null || targetPoints == null || targetPoints.Count == 0)
            {
                Debug.LogWarning($"[{gameObject.name}] 설정이 미비하여 실행할 수 없습니다.");
                return;
            }

            if (isRunning) return;

            StartCoroutine(SequenceRoutine());
        }

        private IEnumerator SequenceRoutine()
        {
            isRunning = true;
            IsOn = false;

            foreach (Transform target in targetPoints)
            {
                if (target == null) continue;

                // 1. 목표 지점으로 이동
                while (Vector3.Distance(actorTransform.position, target.position) > 0.01f)
                {
                    actorTransform.position = Vector3.MoveTowards(
                        actorTransform.position,
                        target.position,
                        moveSpeed * Time.deltaTime
                    );
                    yield return null;
                }
                actorTransform.position = target.position; // 위치 보정

                // 2. X축 회전 액션 (숙였다가 돌아오기)
                yield return StartCoroutine(RotateActionRoutine());
            }

            isRunning = false;
            IsOn = true; // 모든 시퀀스 완료 알림
#if UNITY_EDITOR
            Debug.Log($"<color=green>[Sequence]</color> 모든 지점 이동 및 액션 완료.");
#endif
        }

        private IEnumerator RotateActionRoutine()
        {
            Quaternion originalRot = actorTransform.rotation;
            Quaternion targetRot = originalRot * Quaternion.Euler(rotateAngleX, 0, 0);

            float halfDuration = actionDuration * 0.5f;
            float elapsed = 0f;

            // 회전: 숙이기 (Ease In Out)
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
                actorTransform.rotation = Quaternion.Slerp(originalRot, targetRot, t);
                yield return null;
            }

            elapsed = 0f;
            // 회전: 원래대로 (Ease In Out)
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / halfDuration);
                actorTransform.rotation = Quaternion.Slerp(targetRot, originalRot, t);
                yield return null;
            }

            actorTransform.rotation = originalRot; // 회전 보정
        }

        public new void Reset()
        {
            base.Reset();
            isRunning = false;
            StopAllCoroutines();
        }
    }
}