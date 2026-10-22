using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 평상시에는 항상 활성화(IsOn = true) 상태를 유지하다가,
    /// 지정한 태그의 대상이 트리거 영역 안에 들어오면 꺼지고(IsOn = false),
    /// 영역을 벗어나면 다시 켜지는(IsOn = true) 반전 트리거 노드입니다.
    /// (예: 안전 구역 진입 시 추적/경보 OFF 연동용)
    /// </summary>
    public class TriggerEnterInvert : ProcessBase
    {
        [Tooltip("감지할 대상 오브젝트의 태그 (예: Player)")]
        [SerializeField] private string selectedTag = "Player";

        private void Awake()
        {
            // 기본 상태: 항상 켜짐
            IsOn = true;
        }

        private void OnEnable()
        {
            // 오브젝트 활성화 시 기본 상태 복구
            IsOn = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(selectedTag))
            {
                // 지정 대상이 들어오면 비활성화 (OFF)
                IsOn = false;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(selectedTag))
            {
                // 지정 대상이 밖으로 나가면 다시 활성화 (ON)
                IsOn = true;
            }
        }

        public override void Execute()
        {
            IsOn = true;
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = true;
        }

        private void OnDrawGizmos()
        {
            // 기존 파란색 TriggerEnter와 씬 뷰에서 구분되도록 노란색/주황색 표시
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}