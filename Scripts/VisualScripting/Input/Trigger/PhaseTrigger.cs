using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    // 트리거의 역할을 구분하기 위한 Enum
    public enum PhaseTriggerType
    {
        VentChase,      // 보스를 환풍구로 유인해서 날려보내는 트리거 (환풍구 앞 배치)
        NextPhaseJump   // 다리를 건넌 후 보스를 다음 배틀 위치로 점프시키는 트리거 (다리 건너편 배치)
    }

    public class PhaseTrigger : ProcessBase
    {
        [Header("Phase Trigger Settings")]
        [SerializeField] private string selectedTag = "Player";

        [Tooltip("이 트리거의 역할을 선택하세요.")]
        [SerializeField] private PhaseTriggerType triggerType = PhaseTriggerType.NextPhaseJump;

        [Tooltip("프리팹 소환 시 자동으로 NegativeDrone을 찾아 연결합니다.")]
        [SerializeField] private BossStateMachine bossScript;

        // 중복 실행을 막기 위한 방어용 플래그
        private bool _isTriggered = false;

        private void Start()
        {
            if (bossScript == null)
            {
                GameObject bossObj = GameObject.Find("NegativeDrone");

                if (bossObj != null)
                {
                    bossScript = bossObj.GetComponent<BossStateMachine>();
                }
                else
                {
                    bossScript = FindAnyObjectByType<BossStateMachine>();
                }

                if (bossScript == null)
                {
                    Debug.LogError("[PhaseTrigger] 씬에서 NegativeDrone 오브젝트 또는 BossStateMachine을 찾을 수 없습니다!");
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // 이미 트리거가 발동했다면 무시하고 바로 빠져나갑니다.
            if (_isTriggered) return;

            if (other.CompareTag(selectedTag))
            {
                _isTriggered = true; // 첫 번째 실행 시 true로 변경하여 다음 호출을 차단합니다.
                IsOn = true;
                Execute();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(selectedTag))
            {
                IsOn = false;
            }
        }

        public override void Execute()
        {
            IsOn = true;

            if (bossScript != null)
            {
                // 인스펙터에서 설정한 타입에 따라 BossStateMachine의 다른 연출 함수를 호출합니다.
                switch (triggerType)
                {
                    case PhaseTriggerType.VentChase:
                        bossScript.StartVentChaseAndBounce();
                        break;
                    case PhaseTriggerType.NextPhaseJump:
                        bossScript.StartNextPhaseJump();
                        break;
                }
            }

            // 트리거 비활성화
            gameObject.SetActive(false);
        }

        private void OnDrawGizmos()
        {
            // 레벨 디자인 시 씬 뷰에서 헷갈리지 않도록 트리거 타입에 따라 기즈모 색상을 다르게 표시합니다.
            Gizmos.color = triggerType == PhaseTriggerType.VentChase ? Color.cyan : Color.magenta;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}