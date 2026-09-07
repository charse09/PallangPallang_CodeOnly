using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 특정 키를 지정된 횟수만큼 연타해야 활성화되는 Input 트리거 모듈 (CS1540 protected 접근 제한자 에러 해결 버전)
    /// </summary>
    public class KeyMashTrigger : ProcessBase
    {
        [Header("Key Settings")]
        [Tooltip("연타해야 하는 입력 키")]
        [SerializeField] private KeyCode mashKey = KeyCode.Q;

        [Tooltip("탈출(성공)을 위해 필요한 총 연타 횟수")]
        [SerializeField] private int requiredMashCount = 10;

        [Header("Every Press Feedback (매 입력 시 연출)")]
        [Tooltip("키를 한 번 누를 때마다 즉각 실행할 연출 노드 (예: 플레이어가 꿈틀거리는 ScaleObjectOutput 등)")]
        [SerializeField] private ProcessBase perPressOutput;

        private int _currentMashCount = 0;
        private bool _isTriggered = false;

        public override void Execute()
        {
            // 시퀀스가 이 노드를 다시 호출하거나 초기화할 때 데이터 리셋
            _currentMashCount = 0;
            _isTriggered = false;
            IsOn = false;
        }

        private void Update()
        {
            // 이미 트리거가 성공했거나, 시퀀스가 작동 중이면 입력 무시
            if (_isTriggered || IsOn) return;

            // 플레이어가 지정된 키를 누르는 순간 감지
            if (Input.GetKeyDown(mashKey))
            {
                _currentMashCount++;

#if UNITY_EDITOR
                Debug.Log($"<color=yellow>[{gameObject.name}]</color> 연타 진행 중... ({_currentMashCount} / {requiredMashCount})");
#endif

                // ★ [핵심 해결] 외부 오브젝트의 protected 변수(IsOn)를 직접 건드리지 않고,
                // 프레임워크의 정석 규칙대로 Execute()만 안전하게 호출합니다.
                if (perPressOutput != null)
                {
                    perPressOutput.Execute();
                }

                // 목표 횟수 달성 여부 체크
                if (_currentMashCount >= requiredMashCount)
                {
                    _isTriggered = true;
                    IsOn = true; // 나 자신의 IsOn을 바꾸는 것은 protected 규칙상 완벽히 허용됩니다!

#if UNITY_EDITOR
                    Debug.Log($"<color=green>[{gameObject.name}]</color> 연타 성공! 다음 시퀀스를 실행합니다.");
#endif
                }
            }
        }
    }
}