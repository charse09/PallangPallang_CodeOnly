using System.Collections;
using System.Reflection;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// PlayerLocomotion의 4가지 주요 키(F, Shift, E, Space) 입력을
    /// 외부 Visual Scripting 시퀀스에서 강제로 실행해 주는 Output 노드입니다.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Output/Player Locomotion Action Output")]
    public class PlayerLocomotionActionOutput : ProcessBase
    {
        public enum ActionType
        {
            [Tooltip("Space키: 점프 액션 강제 실행")]
            Space_Jump,

            [Tooltip("Shift키: 슬라이드 액션 강제 실행 (종이 상태 진입 및 슬라이드)")]
            Shift_Slide,

            [Tooltip("F키: 종이 상태(Paper State) 전환")]
            F_PaperState,

            [Tooltip("E키: 슬림 상태(Slim State) 전환")]
            E_SlimState
        }

        public enum StateTriggerMode
        {
            [Tooltip("현재 상태를 반대로 뒤집음 (ON <-> OFF)")]
            Toggle,

            [Tooltip("무조건 켜기 (ON)")]
            ForceOn,

            [Tooltip("무조건 끄기 (OFF)")]
            ForceOff
        }

        public enum SlideDirectionMode
        {
            [Tooltip("현재 플레이어가 누르고 있는 이동 입력 방향 우선, 멈춰있으면 캐릭터 정면")]
            InputOrCharacterForward,

            [Tooltip("입력과 무관하게 항상 캐릭터가 바라보는 정면 방향으로 슬라이드")]
            AlwaysCharacterForward
        }

        [Header("─ 대상 플레이어 ─")]
        [Tooltip("비워두면 'Player' 태그를 가진 오브젝트를 자동으로 탐색합니다.")]
        [SerializeField] private PlayerLocomotion targetLocomotion;

        [Header("─ 강제할 키 액션 ─")]
        [SerializeField] private ActionType forcedAction = ActionType.Space_Jump;

        [Header("─ [Space / 점프] 설정 ─")]
        [Tooltip("체크 시 기본 jumpSpeed 대신 아래 커스텀 점프력을 사용합니다.")]
        [SerializeField] private bool overrideJumpPower = false;
        [SerializeField] private float customJumpPower = 15f;

        [Header("─ [Shift / 슬라이드] 설정 ─")]
        [SerializeField] private SlideDirectionMode slideDirectionMode = SlideDirectionMode.InputOrCharacterForward;
        [Tooltip("이미 슬라이드/종이 상태일 때 Shift를 누르면 해제할지 여부 (기본 키 동작과 동일)")]
        [SerializeField] private bool cancelIfAlreadyPaper = false;

        [Header("─ [F / E 상태 토글] 설정 ─")]
        [Tooltip("F키(Paper) 또는 E키(Slim)를 누를 때의 동작 방식")]
        [SerializeField] private StateTriggerMode stateMode = StateTriggerMode.Toggle;

        // PlayerLocomotion 리플렉션 캐시
        private static readonly BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        private static MethodInfo _jumpRoutineMethod;
        private static FieldInfo _jumpSpeedField;
        private static FieldInfo _jumpModifierField;
        private static FieldInfo _isSlidingField;
        private static FieldInfo _slideDirectionField;
        private static FieldInfo _currentSlideTimerField;
        private static FieldInfo _slideCooldownTimerField;
        private static FieldInfo _slideDurationField;
        private static FieldInfo _slideCooldownField;
        private static FieldInfo _inputMoveDirField;

        private void Awake()
        {
            CacheReflection();
            EnsureTarget();
        }

        private static void CacheReflection()
        {
            if (_jumpRoutineMethod == null)
            {
                _jumpRoutineMethod = typeof(PlayerLocomotion).GetMethod("JumpRoutine", FieldFlags);
                _jumpSpeedField = typeof(PlayerLocomotion).GetField("jumpSpeed", FieldFlags);
                _jumpModifierField = typeof(PlayerLocomotion).GetField("jumpModifier", BindingFlags.Public | BindingFlags.Instance);

                _isSlidingField = typeof(PlayerLocomotion).GetField("isSliding", FieldFlags);
                _slideDirectionField = typeof(PlayerLocomotion).GetField("slideDirection", FieldFlags);
                _currentSlideTimerField = typeof(PlayerLocomotion).GetField("currentSlideTimer", FieldFlags);
                _slideCooldownTimerField = typeof(PlayerLocomotion).GetField("slideCooldownTimer", FieldFlags);
                _slideDurationField = typeof(PlayerLocomotion).GetField("slideDuration", FieldFlags);
                _slideCooldownField = typeof(PlayerLocomotion).GetField("slideCooldown", FieldFlags);
                _inputMoveDirField = typeof(PlayerLocomotion).GetField("inputMoveDir", FieldFlags);
            }
        }

        public override void Execute()
        {
            EnsureTarget();

            if (targetLocomotion == null)
            {
                Debug.LogWarning($"[{gameObject.name}] PlayerLocomotion을 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            CacheReflection();

            switch (forcedAction)
            {
                case ActionType.Space_Jump:
                    ExecuteSpaceJump();
                    break;

                case ActionType.Shift_Slide:
                    ExecuteShiftSlide();
                    break;

                case ActionType.F_PaperState:
                    ExecuteFPaperState();
                    break;

                case ActionType.E_SlimState:
                    ExecuteESlimState();
                    break;
            }

            IsOn = true;
        }

        /// <summary>
        /// [Space] 점프 강제 실행
        /// </summary>
        private void ExecuteSpaceJump()
        {
            float power = customJumpPower;
            if (!overrideJumpPower && _jumpSpeedField != null)
            {
                float baseSpeed = (float)_jumpSpeedField.GetValue(targetLocomotion);
                float modifier = targetLocomotion.jumpModifier;
                power = baseSpeed * modifier;
            }

            if (_jumpRoutineMethod != null)
            {
                IEnumerator routine = (IEnumerator)_jumpRoutineMethod.Invoke(targetLocomotion, new object[] { power });
                targetLocomotion.StartCoroutine(routine);
            }
        }

        /// <summary>
        /// [Shift] 슬라이드 강제 실행
        /// </summary>
        private void ExecuteShiftSlide()
        {
            bool currentPaper = targetLocomotion.GetPaperState();

            // 이미 종이/슬라이드 상태인 경우의 해제 처리
            if (currentPaper && cancelIfAlreadyPaper)
            {
                targetLocomotion.SetAbsolutePaperState(false);
                if (_isSlidingField != null) _isSlidingField.SetValue(targetLocomotion, false);
                return;
            }

            // 1. 종이 상태 진입
            targetLocomotion.SetAbsolutePaperState(true);

            // 2. 방향 계산
            Vector3 dir = targetLocomotion.transform.forward;
            if (slideDirectionMode == SlideDirectionMode.InputOrCharacterForward && _inputMoveDirField != null)
            {
                Vector3 inputDir = (Vector3)_inputMoveDirField.GetValue(targetLocomotion);
                if (inputDir.sqrMagnitude > 0.01f)
                {
                    dir = inputDir.normalized;
                }
            }

            // 3. 슬라이드 상태 및 타이머 주입
            if (_isSlidingField != null) _isSlidingField.SetValue(targetLocomotion, true);
            if (_slideDirectionField != null) _slideDirectionField.SetValue(targetLocomotion, dir);

            float duration = 1.0f;
            if (_slideDurationField != null) duration = (float)_slideDurationField.GetValue(targetLocomotion);
            float durationMod = targetLocomotion.slideDurationModifier;
            if (_currentSlideTimerField != null) _currentSlideTimerField.SetValue(targetLocomotion, duration * durationMod);

            float cooldown = 3.0f;
            if (_slideCooldownField != null) cooldown = (float)_slideCooldownField.GetValue(targetLocomotion);
            if (_slideCooldownTimerField != null) _slideCooldownTimerField.SetValue(targetLocomotion, cooldown);
        }

        /// <summary>
        /// [F] 종이 상태 강제 전환
        /// </summary>
        private void ExecuteFPaperState()
        {
            bool current = targetLocomotion.GetPaperState();
            bool targetState = (stateMode == StateTriggerMode.Toggle) ? !current : (stateMode == StateTriggerMode.ForceOn);

            targetLocomotion.SetAbsolutePaperState(targetState);
        }

        /// <summary>
        /// [E] 슬림 상태 강제 전환
        /// </summary>
        private void ExecuteESlimState()
        {
            bool current = targetLocomotion.GetSlimState();
            bool targetState = (stateMode == StateTriggerMode.Toggle) ? !current : (stateMode == StateTriggerMode.ForceOn);

            targetLocomotion.SetSlimState(targetState);
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;
        }

        private void EnsureTarget()
        {
            if (targetLocomotion != null) return;

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
    }
}