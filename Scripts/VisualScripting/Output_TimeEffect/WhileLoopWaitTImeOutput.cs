using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 시퀀스 도중 지정된 시간만큼 아무것도 하지 않고 대기하는 모듈 (Time Delay)
    /// 기존의 WaitTimeOutput은 WhileLoop에서 사용 시 처음 실행될 때 IsOn이 false로 초기화되지 않아서 최초 1회 실행 후 루프되지않음
    /// 그래서 새롭게 만든 WhileLoop에서 사용가능한 WaitTimeOutput을 만듬.
    /// 제미나이는 기존 WaitTimeOutput을 수정해도 별상관없다고 했지만 혹시 몰라서 새 스크립트 제작.
    /// 기존의 WaitTimeOutput과 큰차이 없고 WhileLoop에서 사용가능하다 정도의 차이.
    /// </summary>
    public class WhileLoopWaitTImeOutput : ProcessBase
    {
        [Tooltip("대기할 시간 (초)")]
        [SerializeField] private float waitSeconds = 1.0f;

        public override void Execute()
        {
            IsOn = false;
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