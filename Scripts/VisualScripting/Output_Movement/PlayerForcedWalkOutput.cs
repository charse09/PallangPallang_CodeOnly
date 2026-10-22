using System.Reflection;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 외부 정지 신호가 올 때까지 플레이어의 이동/점프/슬라이딩 조작만 잠그고,
    /// 마우스 클릭(Mouse0 등)은 허용한 채 지정한 방향으로 강제 걷기를 수행하는 Output 노드입니다.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [AddComponentMenu("Visual Scripting/Output/Player Forced Walk Output")]
    public class PlayerForcedWalkOutput : ProcessBase
    {
        public enum DirectionMode
        {
            [Tooltip("강제 걷기가 시작되는 순간 플레이어가 바라보고 있던 방향으로 직진")]
            CurrentPlayerForward,

            [Tooltip("지정한 Transform의 정면(Forward) 방향으로 직진")]
            TransformForward,

            [Tooltip("지정한 Target Transform의 위치를 향해 이동 (실시간 추적)")]
            TargetPosition,

            [Tooltip("지정한 고정 월드 방향 벡터(X, Z)")]
            CustomWorldDirection
        }

        [Header("─ 대상 플레이어 ─")]
        [Tooltip("조종할 PlayerLocomotion. 비워두면 'Player' 태그를 가진 오브젝트를 자동 탐색합니다.")]
        [SerializeField] private PlayerLocomotion targetLocomotion;

        [Header("─ 종료 신호 (Stop Signal) ─")]
        [Tooltip("★ 이 Output(ProcessBase)의 IsOn이 true가 되면 강제 걷기를 멈추고 조작을 복원합니다.")]
        [SerializeField] private ProcessBase stopSignalCondition;

        [Tooltip("최대 지속 시간(초). 0이면 시간 제한 없이 신호가 올 때까지 유지됩니다.")]
        [SerializeField] private float maxDuration = 0f;

        [Header("─ 이동 방향 설정 ─")]
        [SerializeField] private DirectionMode directionMode = DirectionMode.CurrentPlayerForward;

        [Tooltip("directionMode가 TransformForward 또는 TargetPosition일 때 참조할 대상")]
        [SerializeField] private Transform targetTransform;

        [Tooltip("directionMode가 CustomWorldDirection일 때 이동할 방향")]
        [SerializeField] private Vector3 customDirection = Vector3.forward;

        [Header("─ 이동 속도 ─")]
        [Tooltip("강제로 걸어갈 속도 (PlayerLocomotion 기본 속도는 16)")]
        [SerializeField] private float walkSpeed = 8.0f;

        private bool _isWalking = false;
        private float _timer = 0f;
        private float _originalMoveSpeed = 16f;
        private Vector3 _lockedMoveDirection = Vector3.forward;

        // PlayerLocomotion 리플렉션 캐시
        private static readonly BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        private static FieldInfo _inputMoveDirField;
        private static FieldInfo _moveSpeedField;
        private static FieldInfo _visualsField;
        private static FieldInfo _isInputLockedField; // 플레이어 이동/점프/슬라이드 잠금 전용

        private PlayerVisuals _cachedVisuals;

        private void Awake()
        {
            CacheFieldInfos();
            EnsureTarget();
        }

        private static void CacheFieldInfos()
        {
            if (_inputMoveDirField == null)
                _inputMoveDirField = typeof(PlayerLocomotion).GetField("inputMoveDir", FieldFlags);

            if (_moveSpeedField == null)
                _moveSpeedField = typeof(PlayerLocomotion).GetField("moveSpeed", FieldFlags);

            if (_visualsField == null)
                _visualsField = typeof(PlayerLocomotion).GetField("visuals", FieldFlags);

            if (_isInputLockedField == null)
                _isInputLockedField = typeof(PlayerLocomotion).GetField("isInputLocked", FieldFlags);
        }

        public override void Execute()
        {
            if (_isWalking) return;

            EnsureTarget();

            if (targetLocomotion == null)
            {
                Debug.LogWarning($"[{gameObject.name}] PlayerLocomotion을 찾을 수 없어 강제 걷기를 실행할 수 없습니다.");
                IsOn = true;
                return;
            }

            CacheFieldInfos();

            IsOn = false;
            _isWalking = true;
            _timer = 0f;

            // 1. 원래 이동 속도 백업 및 새 걷기 속도 적용
            if (_moveSpeedField != null)
            {
                _originalMoveSpeed = (float)_moveSpeedField.GetValue(targetLocomotion);
                _moveSpeedField.SetValue(targetLocomotion, walkSpeed);
            }

            // 2. 이동 방향 결정
            _lockedMoveDirection = CalculateMoveDirection();

            // 3. ★ 핵심: InputManager 전체를 끄지 않고 PlayerLocomotion의 로코모션 조작만 잠금
            // 캐릭터의 WASD 이동, 점프, 슬라이드는 차단되지만 마우스 좌클릭(Mouse0)은 정상 입력됩니다.
            if (_isInputLockedField != null)
            {
                _isInputLockedField.SetValue(targetLocomotion, true);
            }
        }

        private void Update()
        {
            if (!_isWalking || targetLocomotion == null) return;

            _timer += Time.deltaTime;

            // 1. 종료 신호 도달 감지
            if (stopSignalCondition != null && stopSignalCondition.IsOn)
            {
                StopForcedWalk(success: true);
                return;
            }

            // 2. 시간 초과 감지
            if (maxDuration > 0f && _timer >= maxDuration)
            {
                StopForcedWalk(success: true);
                return;
            }

            // 3. 이동 방향 계산
            Vector3 currentMoveDir = CalculateMoveDirection();

            // 4. PlayerLocomotion에 강제 이동 방향 주입
            if (_inputMoveDirField != null)
            {
                _inputMoveDirField.SetValue(targetLocomotion, currentMoveDir);
            }

            // 5. 걷기 애니메이션 유지
            if (_cachedVisuals != null)
            {
                _cachedVisuals.SetWalkAnim(true);
            }
        }

        public void StopForcedWalk(bool success = true)
        {
            if (!_isWalking) return;

            _isWalking = false;

            if (targetLocomotion != null)
            {
                // 1. 플레이어 조작 잠금 해제 (점프/슬라이드/WASD 정상 복원)
                if (_isInputLockedField != null)
                {
                    _isInputLockedField.SetValue(targetLocomotion, false);
                }

                // 2. 속도 복구
                if (_moveSpeedField != null)
                {
                    _moveSpeedField.SetValue(targetLocomotion, _originalMoveSpeed);
                }

                // 3. 이동 입력 초기화
                if (_inputMoveDirField != null)
                {
                    _inputMoveDirField.SetValue(targetLocomotion, Vector3.zero);
                }

                // 4. 걷기 애니메이션 해제
                if (_cachedVisuals != null)
                {
                    _cachedVisuals.SetWalkAnim(false);
                }
            }

            IsOn = success;
        }

        private Vector3 CalculateMoveDirection()
        {
            Vector3 dir = Vector3.forward;

            switch (directionMode)
            {
                case DirectionMode.CurrentPlayerForward:
                    dir = _lockedMoveDirection;
                    break;

                case DirectionMode.TransformForward:
                    if (targetTransform != null) dir = targetTransform.forward;
                    break;

                case DirectionMode.TargetPosition:
                    if (targetTransform != null)
                    {
                        dir = targetTransform.position - targetLocomotion.transform.position;
                    }
                    break;

                case DirectionMode.CustomWorldDirection:
                    dir = customDirection;
                    break;
            }

            dir.y = 0f;
            return dir.sqrMagnitude > 0.001f ? dir.normalized : targetLocomotion.transform.forward;
        }

        private void OnDisable()
        {
            if (_isWalking)
            {
                StopForcedWalk(success: false);
            }
        }

        public override void Reset()
        {
            base.Reset();
            if (_isWalking)
            {
                StopForcedWalk(success: false);
            }
            IsOn = false;
        }

        private void EnsureTarget()
        {
            if (targetLocomotion == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    targetLocomotion = player.GetComponent<PlayerLocomotion>();
                }

                if (targetLocomotion == null)
                {
                    targetLocomotion = FindFirstObjectByType<PlayerLocomotion>();
                }
            }

            if (targetLocomotion != null && _cachedVisuals == null)
            {
                if (_visualsField != null)
                {
                    _cachedVisuals = _visualsField.GetValue(targetLocomotion) as PlayerVisuals;
                }

                if (_cachedVisuals == null)
                {
                    _cachedVisuals = targetLocomotion.GetComponentInChildren<PlayerVisuals>();
                }
            }
        }
    }
}