using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class InteractionTrigger : ProcessBase
    {
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float interactDistance = 3.0f;
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private GameObject _player;

        private void Start()
        {
            _player = GameObject.FindGameObjectWithTag(playerTag);
        }

        private void Update()
        {
            // 1. 플레이어 참조 유실 방지
            if (_player == null) return;

            float distance = Vector3.Distance(transform.position, _player.transform.position);

            // 2. 상호작용 조건 체크
            if (distance <= interactDistance)
            {
                if (Input.GetKeyDown(interactKey))
                {
                    IsOn = true;
                    Execute();

#if UNITY_EDITOR
                    this.Log($"{interactKey} 키 입력으로 상호작용 활성화");
#endif  
                }
            }
        }

        private void ResetTrigger()
        {
            IsOn = false;
        }

        public override void Execute()
        {
            // 후속 노드 실행
        }

        public new void Reset()
        {
            base.Reset();
            IsOn = false;
        }

        // [수정/추가된 기즈모 연출 부분]

        /// <summary>
        /// 오브젝트를 선택하지 않아도 씬 뷰에서 항상 범위를 확인할 수 있게 그려줍니다.
        /// </summary>
        private void OnDrawGizmos()
        {
            // 반투명한 노란색 선 구체
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, interactDistance);
        }

        /// <summary>
        /// 오브젝트를 에디터에서 마우스로 선택했을 때 선명하고 선명한 채움 구체로 강조해 줍니다.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 선택 시 진한 노란색 테두리
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactDistance);

            // 범위 내부를 약간 반투명한 노란색으로 채워 감지 구역을 입체적으로 표시
            Gizmos.color = new Color(1f, 0.92f, 0.016f, 0.15f);
            Gizmos.DrawSphere(transform.position, interactDistance);
        }
    }
}