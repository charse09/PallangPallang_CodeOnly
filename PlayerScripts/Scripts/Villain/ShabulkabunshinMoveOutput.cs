using System;
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// ShabulkabunshinLocomotion 컴포넌트를 제어하여 지정한 타겟 목적지로 이동시키는 Output 노드
    /// </summary>
    public class ShabulkabunshinMoveOutput : ProcessBase
    {
        [Header("Actor & Target References")]
        [Tooltip("이동시킬 액터 (비워둘 경우 본인 게임오브젝트 또는 씬에서 자동 탐색합니다)")]
        [SerializeField] private ShabulkabunshinLocomotion actor;

        [Tooltip("목적지 Target Transform (비워둘 경우 'Player' 태그 오브젝트를 탐색합니다)")]
        [SerializeField] private Transform targetTransform;

        [Header("Node Flow & Arrival Settings")]
        [Tooltip("체크 시 목적지에 도달하면 속도와 워블 모션을 멈추고 정지합니다.")]
        [SerializeField] private bool stopOnArrival = true;

        [Tooltip("체크 시 목적지에 도착할 때까지 다음 노드로 넘어가지 않고 대기합니다.")]
        [SerializeField] private bool waitUntilArrival = true;

        [Tooltip("목적지 도착 판정 거리 (m)")]
        [SerializeField] private float arrivalTolerance = 0.1f;

        public void SetTargetTransform(Transform target)
        {
            targetTransform = target;
        }

        public void SetActor(ShabulkabunshinLocomotion targetActor)
        {
            actor = targetActor;
        }

        public override void Execute()
        {
            if (actor == null)
            {
                actor = GetComponent<ShabulkabunshinLocomotion>();
                if (actor == null)
                {
                    actor = FindFirstObjectByType<ShabulkabunshinLocomotion>();
                }
            }

            if (actor == null)
            {
                Debug.LogError($"[{gameObject.name}] ShabulkabunshinMoveOutput: ShabulkabunshinLocomotion 컴포넌트를 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            Transform target = targetTransform;
            if (target == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) target = playerObj.transform;
            }

            if (target == null)
            {
                Debug.LogWarning($"[{gameObject.name}] ShabulkabunshinMoveOutput: 이동 목적지(Target Transform)가 지정되지 않았습니다.");
                IsOn = true;
                return;
            }

            IsOn = false;

            // 이동 시작 전 로코모션 활성화 및 목적지 설정
            actor.enabled = true;
            actor.SetTarget(target);

            // 도착 체크 및 정지 루틴
            if (waitUntilArrival)
            {
                StartCoroutine(HandleArrivalRoutine(target, completeNode: true));
            }
            else
            {
                IsOn = true;
                if (stopOnArrival)
                {
                    StartCoroutine(HandleArrivalRoutine(target, completeNode: false));
                }
            }
        }

        private IEnumerator HandleArrivalRoutine(Transform target, bool completeNode)
        {
            Vector3 lastForward = actor.transform.forward;

            while (actor != null && target != null)
            {
                Vector3 actorPos = actor.transform.position;
                Vector3 targetPos = target.position;

                actorPos.y = 0f;
                targetPos.y = 0f;

                Vector3 dirToTarget = targetPos - actorPos;

                // 목적지를 지나쳐 반대로 뒤집히기 전, 진행 중이던 정면 방향을 계속 기억
                if (dirToTarget.sqrMagnitude > 0.001f)
                {
                    lastForward = dirToTarget.normalized;
                }

                if (dirToTarget.magnitude <= arrivalTolerance)
                {
                    break;
                }

                yield return null;
            }

            // 목적지 도착 시 정지 처리
            if (actor != null && stopOnArrival)
            {
                // 1. 지나침(Overshoot)으로 인한 180도 뒤돌기 방지: 이동하던 정면 방향으로 회전 고정
                if (lastForward.sqrMagnitude > 0.001f)
                {
                    actor.transform.rotation = Quaternion.LookRotation(lastForward);
                }

                // 2. 물리 수평 속도 즉시 제거
                Rigidbody rb = actor.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
                    rb.angularVelocity = Vector3.zero;
                }

                // 3. EnemyWobble 이동 크기를 0으로 초기화
                EnemyWobble wobble = actor.GetComponentInChildren<EnemyWobble>();
                if (wobble != null)
                {
                    wobble.SetMovementMagnitude(0f);
                }

                // 4. 로코모션 비활성화
                actor.enabled = false;
            }

            if (completeNode)
            {
                IsOn = true;
            }
        }

        private void Reset()
        {
            IsOn = false;
        }
    }
}