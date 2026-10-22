using System.Reflection;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// PlayerLocomotion 스크립트를 직접 수정하지 않고,
    /// 이동 속도, 점프력, 슬라이드 속도, 지속 시간, 쿨타임을 외부에서 조절하는 Output 노드입니다.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Output/Player Locomotion Stats Output")]
    public class PlayerLocomotionStatsOutput : ProcessBase
    {
        [Header("─ 대상 플레이어 ─")]
        [Tooltip("조절할 PlayerLocomotion. 비워두면 'Player' 태그를 가진 오브젝트에서 자동으로 찾습니다.")]
        [SerializeField] private PlayerLocomotion targetLocomotion;

        [Header("─ 이동 속도 (Move Speed) ─")]
        [Tooltip("체크 시 이동 속도(기본값 16)를 변경합니다.")]
        [SerializeField] private bool changeMoveSpeed = true;
        [SerializeField] private float newMoveSpeed = 24f;

        [Header("─ 점프 정도 (Jump Speed) ─")]
        [Tooltip("체크 시 점프력/도약 속도(기본값 15)를 변경합니다.")]
        [SerializeField] private bool changeJumpSpeed = true;
        [SerializeField] private float newJumpSpeed = 18f;

        [Header("─ 슬라이드 속도 (Slide Speed) ─")]
        [Tooltip("체크 시 슬라이드 속도(기본값 35)를 변경합니다.")]
        [SerializeField] private bool changeSlideSpeed = true;
        [SerializeField] private float newSlideSpeed = 50f;

        [Header("─ 슬라이드 지속 시간 (Slide Duration) ─")]
        [Tooltip("체크 시 슬라이드 유지 시간(초 단위, 기본값 1.0)을 변경합니다.")]
        [SerializeField] private bool changeSlideDuration = true;
        [SerializeField] private float newSlideDuration = 1.5f;

        [Header("─ 슬라이드 쿨타임 (Slide Cooldown) ─")]
        [Tooltip("체크 시 슬라이드 쿨타임(초 단위, 기본값 3.0)을 변경합니다.")]
        [SerializeField] private bool changeSlideCooldown = true;
        [SerializeField] private float newSlideCooldown = 1.0f;

        private static readonly BindingFlags FieldFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        private static FieldInfo _moveSpeedField;
        private static FieldInfo _jumpSpeedField;
        private static FieldInfo _slideSpeedField;
        private static FieldInfo _slideDurationField;
        private static FieldInfo _slideCooldownField;

        private void Awake()
        {
            CacheFieldInfos();
            EnsureTarget();
        }

        private static void CacheFieldInfos()
        {
            if (_moveSpeedField == null)
                _moveSpeedField = typeof(PlayerLocomotion).GetField("moveSpeed", FieldFlags);

            if (_jumpSpeedField == null)
                _jumpSpeedField = typeof(PlayerLocomotion).GetField("jumpSpeed", FieldFlags);

            if (_slideSpeedField == null)
                _slideSpeedField = typeof(PlayerLocomotion).GetField("slideSpeed", FieldFlags);

            if (_slideDurationField == null)
                _slideDurationField = typeof(PlayerLocomotion).GetField("slideDuration", FieldFlags);

            if (_slideCooldownField == null)
                _slideCooldownField = typeof(PlayerLocomotion).GetField("slideCooldown", FieldFlags);
        }

        public override void Execute()
        {
            EnsureTarget();

            if (targetLocomotion == null)
            {
                Debug.LogWarning($"[{gameObject.name}] PlayerLocomotion 대상을 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            CacheFieldInfos();

            // 1. 이동 속도 변경
            if (changeMoveSpeed && _moveSpeedField != null)
            {
                _moveSpeedField.SetValue(targetLocomotion, newMoveSpeed);
            }

            // 2. 점프력(점프 속도) 변경
            if (changeJumpSpeed && _jumpSpeedField != null)
            {
                _jumpSpeedField.SetValue(targetLocomotion, newJumpSpeed);
            }

            // 3. 슬라이드 속도 변경
            if (changeSlideSpeed && _slideSpeedField != null)
            {
                _slideSpeedField.SetValue(targetLocomotion, newSlideSpeed);
            }

            // 4. 슬라이드 지속 시간 변경
            if (changeSlideDuration && _slideDurationField != null)
            {
                _slideDurationField.SetValue(targetLocomotion, newSlideDuration);
            }

            // 5. 슬라이드 쿨타임 변경
            if (changeSlideCooldown && _slideCooldownField != null)
            {
                _slideCooldownField.SetValue(targetLocomotion, newSlideCooldown);
            }

            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[PlayerLocomotionStatsOutput]</color> 스탯 변경 적용됨 " +
                      $"(이동: {(changeMoveSpeed ? newMoveSpeed.ToString() : "미변경")}, " +
                      $"점프: {(changeJumpSpeed ? newJumpSpeed.ToString() : "미변경")}, " +
                      $"슬라이드 속도: {(changeSlideSpeed ? newSlideSpeed.ToString() : "미변경")}, " +
                      $"슬라이드 유지: {(changeSlideDuration ? newSlideDuration.ToString() + "s" : "미변경")}, " +
                      $"슬라이드 쿨: {(changeSlideCooldown ? newSlideCooldown.ToString() + "s" : "미변경")})");
#endif
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