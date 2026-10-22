using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public enum PetPostArrivalAction
    {
        [Tooltip("목표 지점에 그대로 대기 (FloatingPet 꺼둠)")]
        KeepAtDestination,

        [Tooltip("목표 지점에서 제자리 호버링 대기")]
        StayState,

        [Tooltip("도착 즉시 일반 플레이어 추적(Follow)으로 전환")]
        ResumeFollow,

        [Tooltip("목표 지점 도달 후, 플레이어의 특정 오프셋(CustomOffset) 위치로 날아와 복귀")]
        ReturnToPlayerOffset
    }

    /// <summary>
    /// 플레이어 해머 회전 -> 서쪽(-90도) 도달 시 투척 -> 
    /// 던져진 시점부터 목표 도달 시간(throwDuration) 측정 -> 목표 도착 및 복귀 제어 노드입니다.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Output/Hammer Throw Output")]
    public class HammerThrowOutput : ProcessBase
    {
        [Header("─ 대상 설정 ─")]
        [Tooltip("회전의 중심축이 될 플레이어 Transform")]
        [SerializeField] private Transform playerTransform;

        [Tooltip("던져질 펫 Transform")]
        [SerializeField] private Transform petTransform;

        [Header("─ 1. 회전 및 가속(Spin) 설정 ─")]
        [Tooltip("처음 회전을 시작할 때의 초기 속도 (도/초)")]
        [SerializeField] private float initialSpinSpeed = 180f;

        [Tooltip("최종 최대 회전 속도 (도/초, 예: 720 = 1초에 2바퀴)")]
        [SerializeField] private float spinSpeed = 720f;

        [Tooltip("최대 속도(풀 차징) 도달 시간 (초)")]
        [SerializeField] private float readyDuration = 1.2f;

        [Tooltip("플레이어 기준 펫이 잡혀있을 로컬 위치")]
        [SerializeField] private Vector3 petLocalOffset = new Vector3(0f, 0.5f, 1.5f);

        [Tooltip("던지기를 발동시킬 조건 노드 (IsOn 감지 시 투척 시퀀스 진입)")]
        [SerializeField] private ProcessBase exitConditionProcess;

        [Tooltip("exitConditionProcess 없이 최대 시간 경과 시 자동 투척할지 여부")]
        [SerializeField] private bool autoThrowAfterMaxDuration = false;
        [SerializeField] private float maxSpinDuration = 5.0f;

        [Header("─ 2. ★ 투척(비행) 도달 시간 설정 ─")]
        [Tooltip("★ 던져진 순간(회전 끝난 순간)부터 목표 지점에 쿵 찍힐 때까지 걸리는 기본 도달 시간 (초)")]
        [SerializeField] private float throwDuration = 0.6f;

        [Tooltip("더 많이 회전(차징)했을 때 도달 시간을 단축시켜 더 빨리 도착하게 할지 여부")]
        [SerializeField] private bool speedUpByCharge = true;

        [Tooltip("풀 차징 시 기본 도달 시간에서 최대 몇 초를 깎아줄 것인가 (예: 0.2초 설정 시 0.6초 -> 0.4초로 단축)")]
        [SerializeField] private float maxTimeReduction = 0.2f;

        [Tooltip("포물선의 최고 정점 높이 (0이면 직선 투척)")]
        [SerializeField] private float arcHeight = 2.0f;

        [Tooltip("비행 중 궤적 방향을 바라보게 회전")]
        [SerializeField] private bool alignWithTrajectory = true;

        [Header("─ 3. 타겟 및 릴리즈 각도 ─")]
        [Tooltip("펫이 날아갈 목표 Transform (비어있으면 플레이어 전방 15m 자동 설정)")]
        [SerializeField] private Transform targetDestination;

        [Tooltip("targetDestination이 비어있을 때 사용할 절대 월드 좌표 (선택사항)")]
        [SerializeField] private Vector3 customWorldDestination = Vector3.zero;

        [Tooltip("릴리즈 각도 미세 조정 (0이면 서쪽 -90도 정석 접선)")]
        [SerializeField] private float releaseAngleOffset = 0f;

        [Header("─ 4. 투척 선행 연출 (카메라 등) ─")]
        [Tooltip("던지기 신호 발생 시 즉시 먼저 실행할 Output (예: 카메라 전환 노드)")]
        [SerializeField] private ProcessBase preThrowOutput;

        [Tooltip("카메라 앵글 전환 등을 기다려줄 딜레이 시간 (초)")]
        [SerializeField] private float preThrowDelay = 0.3f;

        [Header("─ 5. 단계별 Output 노드 ─")]
        [Tooltip("★ 펫이 손에서 정확히 떨어져 날아가는 순간 실행할 Output (효과음, 발사 이펙트)")]
        [SerializeField] private ProcessBase onThrowOutput;

        [Tooltip("펫이 목표 지점에 도달한 순간 즉시 실행할 Output (타격 이펙트, 기믹 발동 등)")]
        [SerializeField] private ProcessBase onArrivedOutput;

        [Header("─ 6. 착지 후 복귀(Return) 설정 ─")]
        [Tooltip("목표 도달 후 펫의 행동 방식")]
        [SerializeField] private PetPostArrivalAction postArrivalAction = PetPostArrivalAction.ReturnToPlayerOffset;

        [Tooltip("도착 지점에 머무르는 시간 (초, 타격 연출 대기용)")]
        [SerializeField] private float returnDelay = 0.4f;

        [Tooltip("복귀할 플레이어 기준 로컬 오프셋 (X: 좌우, Y: 높이, Z: 전후)")]
        [SerializeField] private Vector3 returnCustomOffset = new Vector3(0.5f, 1.2f, -0.5f);

        [Tooltip("복귀 비행 속도 (0이면 FloatingPet 기본 속도 사용)")]
        [SerializeField] private float returnMoveSpeed = 16f;

        [Tooltip("플레이어 오프셋에 도착할 때까지 대기한 뒤 다음 신호를 발생시킬지 여부")]
        [SerializeField] private bool waitForReturnArrival = true;

        [Tooltip("복귀 인정 반경")]
        [SerializeField] private float returnArrivalDistance = 0.35f;

        [Tooltip("복귀 완료 후 자동으로 평상시 Follow 상태로 전환")]
        [SerializeField] private bool changeToFollowAfterReturn = true;

        [Tooltip("플레이어 오프셋으로 완전히 돌아왔을 때 실행할 Output 노드 (선택사항)")]
        [SerializeField] private ProcessBase onReturnCompleteOutput;

        [Header("─ 플레이어 복구 ─")]
        [Tooltip("던진 직후 플레이어의 회전각을 원래 방향으로 복원할 시간")]
        [SerializeField] private float playerRestoreDuration = 0.15f;

        private Transform _originalPetParent;
        private Quaternion _originalPlayerRotation;
        private Quaternion _originalPetRotation;
        private FloatingPet _floatingPetComponent;

        private Coroutine _actionCoroutine;
        private bool _isActionRunning = false;
        private float _spinTimer = 0f;

        public override void Execute()
        {
            if (_isActionRunning) return;

            if (playerTransform == null || petTransform == null)
            {
                Debug.LogError($"[{gameObject.name}] Player 또는 Pet Transform이 누락되었습니다!");
                IsOn = true;
                return;
            }

            _originalPlayerRotation = playerTransform.rotation;
            _originalPetRotation = petTransform.rotation;
            _originalPetParent = petTransform.parent;

            _floatingPetComponent = petTransform.GetComponent<FloatingPet>();
            if (_floatingPetComponent != null)
            {
                _floatingPetComponent.enabled = false;
            }

            petTransform.SetParent(playerTransform);
            petTransform.localPosition = petLocalOffset;
            petTransform.localRotation = Quaternion.identity;

            _isActionRunning = true;
            _spinTimer = 0f;
            IsOn = false;

            if (_actionCoroutine != null) StopCoroutine(_actionCoroutine);
            _actionCoroutine = StartCoroutine(MainSequenceRoutine());
        }

        private IEnumerator MainSequenceRoutine()
        {
            // ──────────────────────────────────────────
            // Phase 1. 가속 회전 (원하는 만큼 회전 유지)
            // ──────────────────────────────────────────
            while (true)
            {
                float dt = Time.deltaTime;
                _spinTimer += dt;

                float progress = (readyDuration > 0f) ? Mathf.Clamp01(_spinTimer / readyDuration) : 1f;
                float currentSpeed = Mathf.Lerp(initialSpinSpeed, spinSpeed, progress * progress);

                playerTransform.Rotate(Vector3.up, currentSpeed * dt, Space.World);

                if (!IsOn && progress >= 1.0f)
                {
                    IsOn = true;
                }

                bool triggerThrow = false;
                if (exitConditionProcess != null && exitConditionProcess.IsOn)
                {
                    triggerThrow = true;
                }
                else if (autoThrowAfterMaxDuration && (_spinTimer >= maxSpinDuration))
                {
                    triggerThrow = true;
                }

                if (triggerThrow) break;

                yield return null;
            }

            // ──────────────────────────────────────────
            // Phase 2. 선행 연출 (카메라 전환)
            // ──────────────────────────────────────────
            if (preThrowOutput != null)
            {
                preThrowOutput.Execute();
            }

            if (preThrowDelay > 0f)
            {
                float delayTimer = 0f;
                while (delayTimer < preThrowDelay)
                {
                    float dt = Time.deltaTime;
                    delayTimer += dt;
                    _spinTimer += dt;

                    if (playerTransform != null)
                    {
                        playerTransform.Rotate(Vector3.up, spinSpeed * dt, Space.World);
                    }

                    yield return null;
                }
            }

            // ──────────────────────────────────────────
            // Phase 3. ★ 목표 기준 서쪽(-90도) 도달할 때까지 회전 유지
            // ──────────────────────────────────────────
            Vector3 destination = GetCurrentDestination();

            if (playerTransform != null)
            {
                Vector3 dirToDest = destination - playerTransform.position;
                dirToDest.y = 0f;
                if (dirToDest.sqrMagnitude < 0.001f) dirToDest = _originalPlayerRotation * Vector3.forward;
                else dirToDest.Normalize();

                float baseAngle = (spinSpeed >= 0f) ? -90f : 90f;
                Vector3 idealReleaseDir = Quaternion.Euler(0f, baseAngle + releaseAngleOffset, 0f) * dirToDest;

                while (true)
                {
                    float dt = Time.deltaTime;
                    _spinTimer += dt;

                    float step = Mathf.Abs(spinSpeed) * dt;

                    Vector3 petDir = petTransform.position - playerTransform.position;
                    petDir.y = 0f;
                    petDir.Normalize();

                    float remaining = Vector3.SignedAngle(petDir, idealReleaseDir, Vector3.up);

                    if (spinSpeed >= 0f)
                    {
                        if (remaining < 0f) remaining += 360f;
                    }
                    else
                    {
                        if (remaining > 0f) remaining -= 360f;
                    }

                    if (remaining <= step || remaining < 1.0f)
                    {
                        playerTransform.Rotate(Vector3.up, remaining, Space.World);
                        break;
                    }

                    float rotDir = (spinSpeed >= 0f) ? 1f : -1f;
                    playerTransform.Rotate(Vector3.up, rotDir * step, Space.World);
                    yield return null;
                }
            }

            // ──────────────────────────────────────────
            // Phase 4. ★ 투척 시작! (이 순간부터 도달 시간 측정 시작)
            // ──────────────────────────────────────────
            Vector3 startPos = petTransform.position;
            petTransform.SetParent(null, true);

            if (onThrowOutput != null)
            {
                onThrowOutput.Execute();
            }

            StartCoroutine(RestorePlayerRotationRoutine());

            // 비행 시간 계산: 던져진 시점부터 throwDuration을 기준으로 측정
            float actualThrowDuration = throwDuration;

            if (speedUpByCharge && readyDuration > 0f)
            {
                float chargeRatio = Mathf.Clamp01(_spinTimer / readyDuration);
                actualThrowDuration = Mathf.Max(0.1f, throwDuration - (chargeRatio * maxTimeReduction));
            }

            float flyElapsed = 0f;
            Vector3 lastPos = startPos;

            // ★ 회전 끝난 순간부터 actualThrowDuration 동안 비행
            while (flyElapsed < actualThrowDuration)
            {
                flyElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(flyElapsed / actualThrowDuration);

                destination = GetCurrentDestination();

                Vector3 currentPos = Vector3.Lerp(startPos, destination, t);
                currentPos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;

                petTransform.position = currentPos;

                if (alignWithTrajectory)
                {
                    Vector3 moveDir = currentPos - lastPos;
                    if (moveDir.sqrMagnitude > 0.0001f)
                    {
                        petTransform.rotation = Quaternion.LookRotation(moveDir);
                    }
                }

                lastPos = currentPos;
                yield return null;
            }

            petTransform.position = destination;

            // ──────────────────────────────────────────
            // Phase 5. 목표 지점 도착 (OnArrived)
            // ──────────────────────────────────────────
            if (onArrivedOutput != null)
            {
                onArrivedOutput.Execute();
            }

            // ──────────────────────────────────────────
            // Phase 6. 복귀 처리
            // ──────────────────────────────────────────
            yield return StartCoroutine(HandlePostArrivalRoutine());

            _isActionRunning = false;
            _actionCoroutine = null;
        }

        private Vector3 GetCurrentDestination()
        {
            if (targetDestination != null)
            {
                return targetDestination.position;
            }

            if (customWorldDestination != Vector3.zero)
            {
                return customWorldDestination;
            }

            if (playerTransform != null)
            {
                return playerTransform.position + (_originalPlayerRotation * Vector3.forward * 15f);
            }

            return transform.position + Vector3.forward * 15f;
        }

        private IEnumerator HandlePostArrivalRoutine()
        {
            switch (postArrivalAction)
            {
                case PetPostArrivalAction.KeepAtDestination:
                    if (_floatingPetComponent != null) _floatingPetComponent.enabled = false;
                    break;

                case PetPostArrivalAction.StayState:
                    petTransform.SetParent(_originalPetParent, true);
                    if (_floatingPetComponent != null)
                    {
                        _floatingPetComponent.enabled = true;
                        _floatingPetComponent.SetStateStay();
                    }
                    break;

                case PetPostArrivalAction.ResumeFollow:
                    petTransform.SetParent(_originalPetParent, true);
                    if (_floatingPetComponent != null)
                    {
                        _floatingPetComponent.enabled = true;
                        _floatingPetComponent.SetStateFollow();
                    }
                    break;

                case PetPostArrivalAction.ReturnToPlayerOffset:
                    if (returnDelay > 0f)
                    {
                        yield return new WaitForSeconds(returnDelay);
                    }

                    petTransform.SetParent(_originalPetParent, true);

                    if (_floatingPetComponent != null)
                    {
                        _floatingPetComponent.enabled = true;
                        _floatingPetComponent.MoveToCustomOffset(returnCustomOffset, returnMoveSpeed);
                    }

                    if (waitForReturnArrival)
                    {
                        float timeout = 8.0f;
                        float elapsed = 0f;
                        yield return null;

                        while (elapsed < timeout)
                        {
                            elapsed += Time.deltaTime;

                            Vector3 targetPos = (playerTransform != null)
                                ? playerTransform.TransformPoint(returnCustomOffset)
                                : petTransform.position;

                            float dist = Vector3.Distance(petTransform.position, targetPos);
                            if (dist <= returnArrivalDistance)
                            {
                                break;
                            }

                            yield return null;
                        }

                        if (changeToFollowAfterReturn && _floatingPetComponent != null)
                        {
                            _floatingPetComponent.SetStateFollow();
                        }

                        if (onReturnCompleteOutput != null)
                        {
                            onReturnCompleteOutput.Execute();
                        }
                    }
                    break;
            }
        }

        private IEnumerator RestorePlayerRotationRoutine()
        {
            if (playerTransform == null) yield break;

            if (playerRestoreDuration > 0f)
            {
                Quaternion startRot = playerTransform.rotation;
                float elapsed = 0f;

                while (elapsed < playerRestoreDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / playerRestoreDuration);
                    playerTransform.rotation = Quaternion.Slerp(startRot, _originalPlayerRotation, t);
                    yield return null;
                }
            }

            playerTransform.rotation = _originalPlayerRotation;
        }

        public override void Reset()
        {
            base.Reset();

            if (_actionCoroutine != null)
            {
                StopCoroutine(_actionCoroutine);
                _actionCoroutine = null;
            }

            if (_isActionRunning)
            {
                if (petTransform != null)
                {
                    petTransform.SetParent(_originalPetParent, true);
                    petTransform.rotation = _originalPetRotation;
                }

                if (playerTransform != null)
                {
                    playerTransform.rotation = _originalPlayerRotation;
                }

                if (_floatingPetComponent != null)
                {
                    _floatingPetComponent.enabled = true;
                }

                _isActionRunning = false;
            }

            IsOn = false;
        }

        private void OnDisable()
        {
            Reset();
        }
    }
}