using UnityEngine;
using _Project.Scripts.Managers; // CounterManager를 부르기 위해 추가

namespace _Project.Scripts.VisualScripting
{
    public class IncreaseCounterOutput : ProcessBase
    {
        [Header("카운터 설정")]
        [Tooltip("증가시킬 카운터의 이름을 자유롭게 적으세요.\n(예: KillSlime, PushButtonA)")]
        [SerializeField] private string counterName = "MyCustomCounter";

        [Tooltip("한 번에 증가시킬 수치를 설정합니다. (기본값: 1)")]
        [SerializeField] private int incrementAmount = 1;

        public override void Execute()
        {
            // 방어 코드: 인스펙터에 이름을 안 적었을 경우
            if (string.IsNullOrEmpty(counterName))
            {
                Debug.LogWarning($"[{gameObject.name}] 카운터 이름(Counter Name)이 비어있습니다!");
                return;
            }

            // 매니저에게 해당 이름의 카운터를 증가시키라고 명령
            CounterManager.Increment(counterName, incrementAmount);

            // 콘솔에서 보기 좋게 현재 수치 출력
            int currentCount = CounterManager.GetCount(counterName);
            Debug.Log($"<color=lime>[Output 실행]</color> '{counterName}' 카운터가 {incrementAmount}만큼 증가했습니다. (현재 누적값: {currentCount})");
        }

        public new void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}