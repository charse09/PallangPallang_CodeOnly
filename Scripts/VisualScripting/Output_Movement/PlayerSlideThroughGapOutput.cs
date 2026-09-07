using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 플레이어가 목적지를 향해 이동하다가, 장애물 틈새를 감지하면 누운 자세(제비처럼)로 샥 지나간 뒤 착지하는 Output입니다.
    /// </summary>
    public class PlayerSlideThroughGapOutput : ProcessBase
    {
        [Header("Player Settings")]
        [SerializeField] private GameObject playerObject;
        [SerializeField] private List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();

        [Header("Movement Settings")]
        [SerializeField] private Transform destination;
        [Tooltip("평상시 이동 속도")]
        [SerializeField] private float moveSpeed = 5f;
        [Tooltip("목적지 도착 판정 거리")]
        [SerializeField] private float arrivalThreshold = 0.2f;

        [Header("Gap Slide Settings")]
        [Tooltip("장애물 감지 레이어")]
        [SerializeField] private LayerMask obstacleLayer;
        [Tooltip("전방 감지 거리 (이 안에 장애물이 있으면 슬라이드 시작)")]
        [SerializeField] private float gapDetectDistance = 3f;
        [Tooltip("장애물 통과 시 (제비 활강) 직진 속도")]
        [SerializeField] private float slideSpeed = 15f;
        [Tooltip("슬라이드 통과 거리 (감지 후 이 거리만큼 슬라이드 유지)")]
        [SerializeField] private float slideDistance = 4f;
        
        [Header("Finish Options")]
        [Tooltip("목적지 도착 시 누운 상태(PaperState)로 착지할지 여부")]
        [SerializeField] private bool finishLyingDown = true;

        private Coroutine _slideCoroutine;
        private Rigidbody _playerRb;
        private bool _wasKinematic;
        private PlayerLocomotion _playerLocomotion;
        private PlayerVisuals _playerVisuals;

        public override void Execute()
        {
            if (playerObject == null || destination == null)
            {
                IsOn = true; return;
            }

            IsOn = false;
            _playerLocomotion = playerObject.GetComponent<PlayerLocomotion>();
            _playerVisuals = playerObject.GetComponentInChildren<PlayerVisuals>();
            
            if (_slideCoroutine != null) StopCoroutine(_slideCoroutine);
            _slideCoroutine = StartCoroutine(SlideRoutine());
        }

        private IEnumerator SlideRoutine()
        {
            Transform playerTransform = playerObject.transform;

            // 1. 물리 / 스크립트 비활성화
            foreach (var script in scriptsToDisable)
                if (script != null) script.enabled = false;

            if (InputManager.Instance != null)
                InputManager.Instance.enabled = false;

            _playerRb = playerObject.GetComponent<Rigidbody>();
            if (_playerRb != null)
            {
                _wasKinematic = _playerRb.isKinematic;
                _playerRb.linearVelocity = Vector3.zero;
                _playerRb.angularVelocity = Vector3.zero;
                _playerRb.isKinematic = true;
            }

            yield return null;

            bool isSliding = false;
            float remainingSlide = 0f;
            
            // 시작 시 서 있는 상태로 초기화 (Locomotion이 꺼져있으므로 수동 업데이트)
            SetPlayerPaperState(false);

            // 2. 목적지로 이동
            while (Vector3.Distance(playerTransform.position, destination.position) > arrivalThreshold)
            {
                Vector3 currentPos = playerTransform.position;
                Vector3 toTarget = (destination.position - currentPos).normalized;

                // ── 틈새 감지 ──
                if (!isSliding)
                {
                    if (Physics.SphereCast(currentPos, 0.3f, toTarget, out RaycastHit hit, gapDetectDistance, obstacleLayer))
                    {
                        isSliding = true;
                        remainingSlide = slideDistance;
                        
                        // 감지 즉시 누운 상태(PaperState)로 전환 -> 콜라이더 축소 및 비주얼 눕기
                        SetPlayerPaperState(true);
#if UNITY_EDITOR
                        Debug.Log($"<color=orange>[PlayerSlideThroughGapOutput]</color> 장애물 감지! 눕습니다.");
#endif
                    }
                }
                else
                {
                    remainingSlide -= slideSpeed * Time.deltaTime;
                    if (remainingSlide <= 0f)
                    {
                        isSliding = false;
                        // 슬라이드 끝나면 다시 일어남
                        SetPlayerPaperState(false);
#if UNITY_EDITOR
                        Debug.Log($"<color=green>[PlayerSlideThroughGapOutput]</color> 틈새 통과! 일어납니다.");
#endif
                    }
                }

                // ── 이동 및 회전 ──
                float currentSpeed = isSliding ? slideSpeed : moveSpeed;
                playerTransform.position = Vector3.MoveTowards(currentPos, destination.position, currentSpeed * Time.deltaTime);

                if (toTarget.sqrMagnitude > 0.001f)
                {
                    // 루트 오브젝트는 단순히 목표를 향해 회전함.
                    // 눕는 모션(-90도 꺾임)은 SetPlayerPaperState(true)에 의해 PlayerVisuals가 자동으로 처리함!
                    Quaternion targetRot = Quaternion.LookRotation(toTarget);
                    playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetRot, Time.deltaTime * 15f);
                }

                yield return null;
            }

            // 정확히 스냅
            playerTransform.position = destination.position;
            yield return null;

            // 3. 누운 상태로 마무리 및 복구
            SetPlayerPaperState(finishLyingDown);

            if (_playerRb != null)
            {
                _playerRb.isKinematic = _wasKinematic;
                _playerRb.linearVelocity = Vector3.zero;
                _playerRb.angularVelocity = Vector3.zero;
            }

            for (int i = scriptsToDisable.Count - 1; i >= 0; i--)
                if (scriptsToDisable[i] != null)
                    scriptsToDisable[i].enabled = true;

            if (InputManager.Instance != null)
                InputManager.Instance.enabled = true;

            _slideCoroutine = null;
            IsOn = true;
        }

        private void SetPlayerPaperState(bool isPaper)
        {
            if (_playerLocomotion != null)
            {
                _playerLocomotion.PaperState = isPaper;
            }
            if (_playerVisuals != null)
            {
                _playerVisuals.UpdateStates(isPaper, false, false, false);
            }
        }

        public new void Reset()
        {
            base.Reset();
            if (_slideCoroutine != null)
            {
                StopCoroutine(_slideCoroutine);
                _slideCoroutine = null;
            }
        }
    }
}
