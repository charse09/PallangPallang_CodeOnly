using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 펫에게 이동/상태 명령을 내리고,
    /// 목적지에 실제로 도착했을 때 IsOn = true 신호를 발생시켜 다음 시퀀스를 이어주는 노드입니다.
    /// </summary>
    public class PetMovementProcess : ProcessBase
    {
        public enum CompletionMode
        {
            [Tooltip("펫이 목표 위치(arrivalDistance 이내)에 실제로 도달할 때까지 대기한 뒤 IsOn = true")]
            WaitForArrival,

            [Tooltip("지정한 시간(waitDuration)만큼 대기한 뒤 IsOn = true")]
            WaitForDuration,

            [Tooltip("명령을 전달하자마자 즉시 IsOn = true")]
            Instant
        }

        [Header("1. Target Pet")]
        [SerializeField] private FloatingPet targetPet;

        [Header("2. Command Settings")]
        [SerializeField] private PetState commandState = PetState.Follow;
        [SerializeField] private Vector3 customOffset = new Vector3(0f, 1.0f, 1.5f);
        [SerializeField] private Transform waypointTransform;

        [Header("3. Movement Speed")]
        [Tooltip("이동 속도 오버라이드 (0으로 설정 시 FloatingPet 기본 속도 사용)")]
        [SerializeField] private float customMoveSpeed = 0f;

        [Header("4. Completion & Sequence Flow")]
        [SerializeField] private CompletionMode completionMode = CompletionMode.WaitForArrival;

        [Tooltip("목적지 도착 인정 반경 (거리 이내 진입 시 완료 판정)")]
        [SerializeField] private float arrivalDistance = 0.25f;

        [Tooltip("WaitForDuration 모드에서 대기할 시간 (초)")]
        [SerializeField] private float waitDuration = 1.0f;

        [Tooltip("장애물 충돌 등으로 펫이 영원히 도달하지 못할 때 시퀀스가 멈추는 것을 방지하는 최대 타임아웃 (초)")]
        [SerializeField] private float arrivalTimeout = 10f;

        private Coroutine _movementCoroutine;

        private void Awake()
        {
            if (targetPet == null) targetPet = GetComponent<FloatingPet>();
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;

            if (_movementCoroutine != null)
            {
                StopCoroutine(_movementCoroutine);
                _movementCoroutine = null;
            }
        }

        public override void Execute()
        {
            IsOn = false;

            if (targetPet == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 제어할 FloatingPet 대상이 할당되지 않았습니다.");
                IsOn = true;
                return;
            }

            // 1. 펫에게 이동 명령 전달
            ExecutePetCommand();

            // 2. 완료 조건에 따라 코루틴 시작
            if (_movementCoroutine != null) StopCoroutine(_movementCoroutine);

            switch (completionMode)
            {
                case CompletionMode.WaitForArrival:
                    _movementCoroutine = StartCoroutine(WaitForArrivalRoutine());
                    break;

                case CompletionMode.WaitForDuration:
                    _movementCoroutine = StartCoroutine(WaitDurationRoutine());
                    break;

                case CompletionMode.Instant:
                    IsOn = true;
                    break;
            }
        }

        private void ExecutePetCommand()
        {
            switch (commandState)
            {
                case PetState.Follow:
                    targetPet.SetStateFollow(customMoveSpeed);
                    break;
                case PetState.Wander:
                    targetPet.SetStateWander(customMoveSpeed);
                    break;
                case PetState.CustomOffset:
                    targetPet.MoveToCustomOffset(customOffset, customMoveSpeed);
                    break;
                case PetState.MoveToTransform:
                    if (waypointTransform != null)
                    {
                        targetPet.MoveToSpecificTransform(waypointTransform, customMoveSpeed);
                    }
                    else
                    {
                        Debug.LogWarning($"[{gameObject.name}] 이동할 waypointTransform이 비어있습니다.");
                    }
                    break;
            }
        }

        private IEnumerator WaitForArrivalRoutine()
        {
            float elapsedTimeout = 0f;

            // 펫의 내부 목적지 갱신을 위해 1프레임 대기
            yield return null;

            while (elapsedTimeout < arrivalTimeout)
            {
                elapsedTimeout += Time.deltaTime;

                Vector3 targetDestination = GetCurrentDestination();
                float dist = Vector3.Distance(targetPet.transform.position, targetDestination);

                // 목표 반경 내 도달 완료
                if (dist <= arrivalDistance)
                {
                    break;
                }

                yield return null;
            }

            IsOn = true;
            _movementCoroutine = null;
        }

        private IEnumerator WaitDurationRoutine()
        {
            yield return new WaitForSeconds(waitDuration);
            IsOn = true;
            _movementCoroutine = null;
        }

        private Vector3 GetCurrentDestination()
        {
            if (commandState == PetState.MoveToTransform && waypointTransform != null)
            {
                return waypointTransform.position;
            }

            if (commandState == PetState.CustomOffset)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    return player.transform.position + customOffset;
                }
            }

            if (targetPet.transform.parent != null)
            {
                return targetPet.transform.parent.position;
            }

            return targetPet.transform.position;
        }
    }
}