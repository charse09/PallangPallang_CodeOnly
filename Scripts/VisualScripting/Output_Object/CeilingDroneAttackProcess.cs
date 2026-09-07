using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    // 공격 종류를 선택할 수 있는 Enum
    public enum DroneAttackType
    {
        HandAttack,
        GroundAttack
    }

    public class CeilingDroneAttackProcess : ProcessBase
    {
        [Header("Drone Settings")]
        [SerializeField] private CeilingNegativeDrone targetDrone;

        [Header("Command Settings")]
        [Tooltip("실행할 공격 애니메이션 타입")]
        [SerializeField] private DroneAttackType attackType = DroneAttackType.HandAttack;

        [Header("Flow Settings")]
        [SerializeField] private float waitDuration = 1.0f;

        private Coroutine _processCoroutine;

        private void Awake()
        {
            // 할당되지 않았다면 자기 자신에서 찾기
            if (targetDrone == null) targetDrone = GetComponent<CeilingNegativeDrone>();
        }

        public override void Execute()
        {
            IsOn = false;

            if (targetDrone == null)
            {
                Debug.LogWarning("CeilingDroneAttackProcess: 제어할 CeilingNegativeDrone이 없습니다!");
                IsOn = true;
                return;
            }

            // 설정된 공격 실행
            ExecuteAttackCommand();

            // 대기 시간이 설정되어 있다면 코루틴 실행, 아니면 바로 완료 처리
            if (waitDuration > 0f)
            {
                if (_processCoroutine != null) StopCoroutine(_processCoroutine);
                _processCoroutine = StartCoroutine(WaitRoutine());
            }
            else
            {
                IsOn = true;
            }
        }

        private void ExecuteAttackCommand()
        {
            switch (attackType)
            {
                case DroneAttackType.HandAttack:
                    targetDrone.TriggerHandAttack();
                    break;
                case DroneAttackType.GroundAttack:
                    targetDrone.TriggerGroundAttack();
                    break;
            }
        }

        private IEnumerator WaitRoutine()
        {
            yield return new WaitForSeconds(waitDuration);
            _processCoroutine = null;
            IsOn = true; // 다음 Process로 넘어가기 위한 플래그
        }
    }
}