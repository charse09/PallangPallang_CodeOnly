using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 세상을 멈추거나 다시 움직이게 하는 출력 노드입니다. (메뉴, 컷씬, 물리 버그 방지 등에 사용)
    /// </summary>
    public class TimePauseOutput : ProcessBase
    {
        [Tooltip("체크하면 시간을 멈추고(TimeScale=0), 체크를 해제하면 시간을 원래대로 돌립니다(TimeScale=1).")]
        [SerializeField] private bool isPause = true;

        public override void Execute()
        {
            IsOn = false;

            if (isPause)
            {
                Time.timeScale = 0f;
#if UNITY_EDITOR
                Debug.Log($"<color=red>[{gameObject.name}]</color> 세상을 멈췄습니다. (TimeScale = 0)");
#endif
            }
            else
            {
                Time.timeScale = 1f;
#if UNITY_EDITOR
                Debug.Log($"<color=green>[{gameObject.name}]</color> 세상을 다시 움직이게 했습니다. (TimeScale = 1)");
#endif
            }

            // 즉각적으로 다음 노드로 신호를 넘깁니다.
            IsOn = true;
        }
    }
}
