using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class TimeController : ProcessBase
    {
        [Header("시간 제어 설정")]
        [Tooltip("체크(True)하면 시간을 멈춥니다.\n체크 해제(False)하면 시간을 다시 정상 속도로 돌립니다.")]
        [SerializeField] private bool stopTime = true;

        public override void Execute()
        {
            if (stopTime)
            {
                // 시간 정지 기능
                Time.timeScale = 0f;
                Debug.Log("<color=orange>[Output 실행]</color> 게임 시간이 멈췄습니다. (TimeScale = 0)");
            }
            else
            {
                // 시간 복구 기능
                Time.timeScale = 1f;
                Debug.Log("<color=orange>[Output 실행]</color> 게임 시간이 다시 흐릅니다. (TimeScale = 1)");
            }
        }

        public new void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}