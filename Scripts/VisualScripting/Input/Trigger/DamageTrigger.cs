using UnityEngine;
using _Project.Scripts.VisualScripting;

namespace _Project.Scripts.VisualScripting
{
    public class DamageTrigger : ProcessBase
    {
        private EntryCloner _cloner;
        private bool _isInitialized = false;

        [Header("Debug Settings")]
        [SerializeField] private bool useDebugKey = true;
        [SerializeField] private KeyCode debugDamageKey = KeyCode.K; // K키를 누르면 데미지
        [SerializeField] private float debugDamageAmount = 2f;

        private void Start()
        {
            _cloner = GetComponent<EntryCloner>();
        }

        private void Update()
        {
            if (useDebugKey && Input.GetKeyDown(debugDamageKey))
            {
                TakeDamage(debugDamageAmount);
            }
        }

        // 외부(적, 함정 등)에서 호출할 함수
        public void TakeDamage(float damage)
        {
            // 1. 기초 검사 (데미지가 1 미만이거나 클로너가 없으면 무시)
            if (damage < 1f || _cloner == null) return;

            // 2. 현재 HP 데이터 가져오기 (EntryCloner의 Dictionary에서 "HP" 키값 읽기)
            // TestData - Class.csv에 정의된 컬럼명 "HP"를 사용합니다.
            float currentHp = _cloner.GetStat<float>("HP");

            if (currentHp > 0)
            {
                // 3. 데미지 계산 및 데이터 갱신
                float newHp = Mathf.Max(0, currentHp - damage);
                _cloner.SetStat("HP", newHp);

                Debug.Log($"[{gameObject.name}] 피격! 데미지: {damage}, 남은 HP: {newHp}");

                // 4. 노코드 트리거 활성화 (ProcessBase 상속 기능)
                IsOn = true;
                Execute(); // 연결된 후속 노드 실행

                // 5. 트리거 상태 리셋 (연속 피격 감지를 위해 짧은 시간 뒤 초기화)
                Invoke(nameof(ResetTrigger), 0.1f);
            }
        }

        private void ResetTrigger()
        {
            IsOn = false;
        }

        public override void Execute()
        {
            // 부모 클래스의 추상 메서드 구현 (필수)
            // 실제 로직은 유니티 인스펙터의 Conditional 노드 등에 연결하여 사용합니다.
        }
    }
}