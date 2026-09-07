using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 시퀀스 도중 지정된 시간만큼 아무것도 하지 않고 대기하는 모듈 (Time Delay)
    /// </summary>
    public class WaitTimeOutput : ProcessBase
    {
        [Tooltip("대기할 시간 (초)")]
        [SerializeField] private float waitSeconds = 1.0f;

        public override void Execute()
        {
            StartCoroutine(WaitRoutine());
        }

        private IEnumerator WaitRoutine()
        {
#if UNITY_EDITOR
            Debug.Log($"<color=grey>[{gameObject.name}]</color> {waitSeconds}초 대기 중...");
#endif
            yield return new WaitForSeconds(waitSeconds);

            // 대기가 끝나면 상태를 완료로 변경하여 시퀀스가 다음으로 넘어가게 함
            IsOn = true;
        }
    }
}