using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 오브젝트(Actor)가 특정 타겟(Target)을 바라보도록 회전시키는 Output 모듈.
    /// 인스펙터에서 원하는 오브젝트들을 직접 드래그 앤 드롭으로 지정할 수 있습니다.
    /// </summary>
    public class LookAtOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("고개를 돌릴 주체 오브젝트를 드래그 앤 드롭 하세요. (비워두면 자동으로 플레이어를 찾습니다)")]
        [SerializeField] private GameObject actorObject;

        [Tooltip("바라볼 대상 오브젝트(NPC, 아이템 등)를 드래그 앤 드롭 하세요.")]
        [SerializeField] private Transform lookAtTarget;

        [Header("Rotation Settings")]
        [Tooltip("체크 시 Y축 위치를 무시하고 수평으로만 바라봅니다. (캐릭터 고개가 위아래로 기괴하게 꺾이는 것 방지)")]
        [SerializeField] private bool ignoreYAxis = true;

        [Tooltip("바라보는 데 걸리는 시간 (0이면 즉시 회전, 값을 주면 부드럽게 회전)")]
        [SerializeField] private float lookDuration = 0.25f;

        public override void Execute()
        {
            IsOn = false;

            // 1. ActorObject를 비워뒀을 경우, 자동으로 플레이어를 탐색하는 편의 기능
            if (actorObject == null)
            {
                var locomotion = FindFirstObjectByType<PlayerLocomotion>();
                if (locomotion != null) actorObject = locomotion.gameObject;
            }

            // 2. 예외 처리 (둘 중 하나라도 없으면 에러 방지를 위해 리턴)
            if (actorObject == null || lookAtTarget == null)
            {
                Debug.LogWarning($"[{gameObject.name}] Actor(회전할 대상) 또는 Target(바라볼 대상)이 지정되지 않았습니다. 인스펙터를 확인해주세요.");
                IsOn = true;
                return;
            }

            // 3. 부드러운 회전 루틴 시작
            StartCoroutine(LookAtRoutine());
        }

        private IEnumerator LookAtRoutine()
        {
            Transform actorTransform = actorObject.transform;
            
            // 목표 방향 계산 (타겟 위치 - 내 위치)
            Vector3 targetDirection = lookAtTarget.position - actorTransform.position;
            
            if (ignoreYAxis)
            {
                targetDirection.y = 0f; // 수평 회전만 남김
            }

            // 거리 벡터가 정상적일 때만 회전 수행 (제자리에 있으면 회전 안 함)
            if (targetDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(targetDirection, Vector3.up);
                Quaternion startRot = actorTransform.rotation;

                // lookDuration 동안 부드럽게 회전
                if (lookDuration > 0f)
                {
                    float elapsed = 0f;
                    while (elapsed < lookDuration)
                    {
                        elapsed += Time.deltaTime;
                        float smoothT = Mathf.SmoothStep(0f, 1f, elapsed / lookDuration);
                        
                        actorTransform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);
                        yield return null;
                    }
                }

                // 최종 목표 회전값 강제 고정
                actorTransform.rotation = targetRot;
            }

            // 비주얼 스크립팅 노드 완료 처리
            IsOn = true;
        }
    }
}