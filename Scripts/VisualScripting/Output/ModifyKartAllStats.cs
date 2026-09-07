using System.Collections;
using UnityEngine;
using KartGame.KartSystems;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 특정 ArcadeKart의 일반 스탯뿐만 아니라 범퍼 물리 수치(Bumper Force 등)까지 모두 제어하는 프로세스
    /// </summary>
    public class ModifyKartAllStats : ProcessBase
    {
        [Header("[Target Kart Settings]")]
        [Tooltip("스탯을 변경할 대상 카트 (비워두면 이 오브젝트나 자식에게서 자동으로 찾음)")]
        [SerializeField] private ArcadeKart targetKart;

        [Header("[General Stat Settings]")]
        [Tooltip("이 효과의 고유 ID (예: GoldenBumper, MudSlow)")]
        [SerializeField] private string powerUpID = "CustomModifier";
        [Tooltip("이 효과가 유지될 시간 (초)")]
        [SerializeField] private float duration = 5f;
        [Tooltip("추가하거나 감소시킬 일반 스탯 수치 (마이너스 값 입력 시 디버프)")]
        [SerializeField] private ArcadeKart.Stats statModifiers;

        [Header("[Bumper Custom Settings]")]
        [Tooltip("체크하면 아래 설정한 범퍼 수치들로 카트의 범퍼 설정을 강제 교체합니다.")]
        [SerializeField] private bool changeBumperSettings = false;
        [Tooltip("변경할 범퍼 튕겨나가는 힘의 세기")]
        [SerializeField] private float customBumperForce = 25f;
        [Tooltip("변경할 위쪽으로 살짝 띄워주는 힘")]
        [SerializeField] private float customBumperUpwardBias = 0.5f;
        [Tooltip("변경할 범퍼 충돌 후 스턴 시간(초)")]
        [SerializeField] private float customBumperStunDuration = 0.5f;

        public override void Execute()
        {
            if (targetKart == null)
            {
                targetKart = GetComponentInChildren<ArcadeKart>();
                if (targetKart == null)
                {
                    Debug.LogError($"<{gameObject.name}> 대상을 찾을 수 없어 스탯을 변경하지 못했습니다.");
                    IsOn = true;
                    return;
                }
            }

            // 1. 일반 이동 스탯 적용 (기존 방식 활용)
            ArcadeKart.StatPowerup newPowerup = new ArcadeKart.StatPowerup
            {
                PowerUpID = this.powerUpID,
                MaxTime = this.duration,
                ElapsedTime = 0f,
                modifiers = this.statModifiers
            };
            targetKart.AddPowerup(newPowerup);

            // 2. 범퍼 수치 변경 옵션이 켜져 있다면 복구용 코루틴 실행
            if (changeBumperSettings)
            {
                StartCoroutine(BumperModifierRoutine());
            }
            else
            {
                // 범퍼 변경을 안 하면 바로 다음 노드로 신호 넘김
                IsOn = true;
            }
        }

        private IEnumerator BumperModifierRoutine()
        {
            // 변경 전 원래 카트의 범퍼 수치들을 백업해 둠
            float originalForce = targetKart.BumperForce;
            float originalUpwardBias = targetKart.BumperUpwardBias;
            float originalStunDuration = targetKart.BumperStunDuration;

            // 새로운 수치로 덮어쓰기
            targetKart.BumperForce = customBumperForce;
            targetKart.BumperUpwardBias = customBumperUpwardBias;
            targetKart.BumperStunDuration = customBumperStunDuration;

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> {targetKart.name}의 범퍼 설정을 변경했습니다. (Force: {customBumperForce})");
#endif

            // 소환 로직이나 즉시 발동형 노드의 흐름을 방해하지 않기 위해, 수치 변경 직후 다음 노드로 신호를 보냄
            IsOn = true;

            // 설정된 지속시간만큼 대기
            yield return new WaitForSeconds(duration);

            // 시간이 다 되면 원래 카트 수치로 안전하게 복구
            if (targetKart != null)
            {
                targetKart.BumperForce = originalForce;
                targetKart.BumperUpwardBias = originalUpwardBias;
                targetKart.BumperStunDuration = originalStunDuration;

#if UNITY_EDITOR
                Debug.Log($"<color=orange>[{gameObject.name}]</color> {targetKart.name}의 범퍼 설정이 원래대로 복구되었습니다.");
#endif
            }
        }
    }
}