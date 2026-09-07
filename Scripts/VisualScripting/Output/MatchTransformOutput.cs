using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 오브젝트(Actor)의 위치와 회전값을 타겟 오브젝트(Target)와 동일하게 일치시키는 Output 모듈.
    /// 인스펙터에서 대상 오브젝트들을 직접 드래그 앤 드롭으로 지정할 수 있습니다.
    /// </summary>
    public class MatchTransformOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("위치/회전을 변경할 주체 오브젝트를 드래그 앤 드롭 하세요. (비워두면 자동으로 플레이어를 찾습니다)")]
        [SerializeField] private GameObject actorObject;

        [Tooltip("일치시키고자 하는 목표 위치/회전값을 가진 타겟 오브젝트를 드래그 앤 드롭 하세요.")]
        [SerializeField] private Transform matchTarget;

        [Header("Match Settings")]
        [Tooltip("타겟의 위치(Position)를 맞출지 여부")]
        [SerializeField] private bool matchPosition = true;

        [Tooltip("타겟의 회전(Rotation)을 맞출지 여부")]
        [SerializeField] private bool matchRotation = true;

        [Tooltip("이동 및 회전에 걸리는 시간 (0이면 즉시 순간이동, 값을 주면 부드럽게 이동)")]
        [SerializeField] private float matchDuration = 0.25f;

        public override void Execute()
        {
            IsOn = false;

            // 1. ActorObject를 비워뒀을 경우, 자동으로 플레이어를 탐색하는 편의 기능
            if (actorObject == null)
            {
                var locomotion = FindFirstObjectByType<PlayerLocomotion>();
                if (locomotion != null) actorObject = locomotion.gameObject;
            }

            // 2. 예외 처리
            if (actorObject == null || matchTarget == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Actor(변경할 대상) 또는 Target(기준 대상)이 지정되지 않았습니다. 인스펙터를 확인해주세요.");
                IsOn = true;
                return;
            }

            // 3. 변환 루틴 시작
            StartCoroutine(MatchTransformRoutine());
        }

        private IEnumerator MatchTransformRoutine()
        {
            Transform actorTransform = actorObject.transform;

            // 시작 시점의 위치와 회전 저장
            Vector3 startPos = actorTransform.position;
            Quaternion startRot = actorTransform.rotation;

            // 목표로 하는 위치와 회전 (설정에 따라 현재 값 유지)
            Vector3 targetPos = matchPosition ? matchTarget.position : startPos;
            Quaternion targetRot = matchRotation ? matchTarget.rotation : startRot;

            // matchDuration 동안 부드럽게 이동 및 회전
            if (matchDuration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < matchDuration)
                {
                    elapsed += Time.deltaTime;
                    float smoothT = Mathf.SmoothStep(0f, 1f, elapsed / matchDuration);

                    if (matchPosition)
                    {
                        actorTransform.position = Vector3.Lerp(startPos, targetPos, smoothT);
                    }
                    
                    if (matchRotation)
                    {
                        actorTransform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
                    }

                    yield return null;
                }
            }

            // 최종 목표 값 강제 고정
            if (matchPosition) actorTransform.position = targetPos;
            if (matchRotation) actorTransform.rotation = targetRot;

            // 비주얼 스크립팅 노드 완료 처리
            IsOn = true;
        }
    }
}