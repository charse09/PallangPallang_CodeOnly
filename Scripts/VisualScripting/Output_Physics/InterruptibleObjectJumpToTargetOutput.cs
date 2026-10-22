using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 오브젝트를 포물선 점프로 날려보내며, 
    /// 비행 도중 재발동되거나 다른 노드에서 가로채면 즉시 이전 점프를 끊고 현재 공중 위치에서 꺾어 날립니다.
    /// 또한 목표 지점(targetPoint)이 움직이는 경우 실시간으로 위치를 추적하여 목표물 위에 정확히 착지합니다.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Output/Interruptible Object Jump To Target Output")]
    public class InterruptibleObjectJumpToTargetOutput : ProcessBase
    {
        [Header("Object Settings")]
        [Tooltip("점프시킬 대상 오브젝트입니다. 비워두면 이 스크립트가 붙은 오브젝트가 이동합니다.")]
        [SerializeField] private GameObject objectToJump;

        [Header("Target Settings")]
        [Tooltip("오브젝트가 도착할 목표 지점의 Transform")]
        [SerializeField] private Transform targetPoint;

        [Tooltip("목표물이 이동 중일 때 실시간으로 위치를 추적하여 착지할지 여부")]
        [SerializeField] private bool trackMovingTarget = true;

        [Header("Jump Settings")]
        [Tooltip("점프가 얼마나 높이 뛸지(최대 높이) 결정합니다.")]
        [SerializeField] private float jumpHeight = 3.0f;

        [Tooltip("점프부터 착지까지 걸리는 총 시간(초)입니다.")]
        [SerializeField] private float jumpDuration = 1.0f;

        [Header("Interruption Settings")]
        [Tooltip("체크 시 이미 날아가는 중이라도 강제로 끊고 현재 공중 위치에서 즉시 새 목표로 궤적을 전환합니다.")]
        [SerializeField] private bool canInterruptOngoingJump = true;

        // 전역 점프 추적기: 동일 오브젝트에 대한 다중 점프 충돌 방지
        private static readonly Dictionary<GameObject, InterruptibleObjectJumpToTargetOutput> ActiveJumpRegistry 
            = new Dictionary<GameObject, InterruptibleObjectJumpToTargetOutput>();

        private Coroutine _jumpCoroutine;
        private Rigidbody _cachedRb;
        private bool _wasKinematic;

        public override void Execute()
        {
            IsOn = false;

            if (targetPoint == null)
            {
                Debug.LogError($"[{gameObject.name}] Target Point가 비어있습니다! 목표 지점을 할당해주세요.");
                IsOn = true;
                return;
            }

            GameObject targetObj = (objectToJump != null) ? objectToJump : this.gameObject;

            // 1. 이미 비행 중인 동일 오브젝트가 있다면 이전 점프 즉시 강제 중단
            if (ActiveJumpRegistry.TryGetValue(targetObj, out var existingJumper))
            {
                if (canInterruptOngoingJump)
                {
                    existingJumper.StopCurrentJump(targetObj);
#if UNITY_EDITOR
                    Debug.Log($"<color=orange>[{gameObject.name}]</color> {targetObj.name}의 이전 비행을 강제 중단하고 새 목표로 즉시 궤적 전환!");
#endif
                }
                else
                {
                    IsOn = true;
                    return;
                }
            }

            // 2. 새 점프 등록 및 현재 공중 위치에서 새 궤적 시작
            ActiveJumpRegistry[targetObj] = this;

            if (_jumpCoroutine != null) StopCoroutine(_jumpCoroutine);
            _jumpCoroutine = StartCoroutine(JumpRoutine(targetObj));
        }

        private IEnumerator JumpRoutine(GameObject targetObj)
        {
            _cachedRb = targetObj.GetComponent<Rigidbody>();
            bool hasRb = _cachedRb != null;

            if (hasRb)
            {
                _wasKinematic = _cachedRb.isKinematic;
                _cachedRb.isKinematic = true;
                _cachedRb.linearVelocity = Vector3.zero;
                _cachedRb.angularVelocity = Vector3.zero;
            }

            // 현재 공중 위치가 새로운 시작점이 됨
            Vector3 startPos = targetObj.transform.position;
            Vector3 currentEndPos = targetPoint.position;

            float timer = 0f;

            try
            {
                while (timer < jumpDuration)
                {
                    timer += Time.deltaTime;
                    float t = Mathf.Clamp01(timer / jumpDuration);

                    // ★ 실시간 이동 타겟 추적
                    if (trackMovingTarget && targetPoint != null)
                    {
                        currentEndPos = targetPoint.position;
                    }

                    // 포물선 Y 높이 오프셋
                    float heightOffset = 4f * jumpHeight * t * (1f - t);
                    Vector3 currentPos = Vector3.Lerp(startPos, currentEndPos, t);
                    currentPos.y += heightOffset;

                    targetObj.transform.position = currentPos;
                    yield return null;
                }

                if (targetPoint != null)
                {
                    currentEndPos = targetPoint.position;
                }
                targetObj.transform.position = currentEndPos;
            }
            finally
            {
                // 정상 착지 또는 다른 점프에 의해 가로채였을 때 정리
                if (ActiveJumpRegistry.TryGetValue(targetObj, out var jumper) && jumper == this)
                {
                    ActiveJumpRegistry.Remove(targetObj);

                    if (hasRb && _cachedRb != null)
                    {
                        _cachedRb.isKinematic = _wasKinematic;
                    }
                }

                _jumpCoroutine = null;
                IsOn = true;
            }
        }

        public void StopCurrentJump(GameObject targetObj)
        {
            if (_jumpCoroutine != null)
            {
                StopCoroutine(_jumpCoroutine);
                _jumpCoroutine = null;
            }

            if (ActiveJumpRegistry.TryGetValue(targetObj, out var jumper) && jumper == this)
            {
                ActiveJumpRegistry.Remove(targetObj);
            }
        }

        public override void Reset()
        {
            base.Reset();

            GameObject targetObj = (objectToJump != null) ? objectToJump : this.gameObject;
            StopCurrentJump(targetObj);

            if (_cachedRb != null)
            {
                _cachedRb.isKinematic = _wasKinematic;
            }

            IsOn = false;
        }

        private void OnDisable()
        {
            GameObject targetObj = (objectToJump != null) ? objectToJump : this.gameObject;
            StopCurrentJump(targetObj);
        }
    }
}