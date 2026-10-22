//using System.Collections;
//using UnityEngine;

//namespace _Project.Scripts.VisualScripting
//{
//    /// <summary>
//    /// 시퀀스 도중 지정된 시간만큼 아무것도 하지 않고 대기하는 모듈 (Time Delay)
//    /// </summary>
//    public class WaitTimeOutput : ProcessBase
//    {
//        [Tooltip("대기할 시간 (초)")]
//        [SerializeField] private float waitSeconds = 1.0f;

//        public override void Execute()
//        {
//            StartCoroutine(WaitRoutine());
//        }

//        private IEnumerator WaitRoutine()
//        {
//#if UNITY_EDITOR
//            Debug.Log($"<color=grey>[{gameObject.name}]</color> {waitSeconds}초 대기 중...");
//#endif
//            yield return new WaitForSeconds(waitSeconds);

//            // 대기가 끝나면 상태를 완료로 변경하여 시퀀스가 다음으로 넘어가게 함
//            IsOn = true;
//        }
//    }
//}

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

        private Coroutine _waitCoroutine;

        public override void Execute()
        {
            // 중복 실행 방지 및 대기 시작 시 완료 플래그 초기화
            IsOn = false;

            if (_waitCoroutine != null) StopCoroutine(_waitCoroutine);
            _waitCoroutine = StartCoroutine(WaitRoutine());
        }

        private IEnumerator WaitRoutine()
        {
#if UNITY_EDITOR
            Debug.Log($"<color=grey>[{gameObject.name}]</color> {waitSeconds}초 대기 시작...");
#endif
            yield return new WaitForSeconds(waitSeconds);

            // 대기 완료 후 다음 프로세스로 진행
            IsOn = true;
            _waitCoroutine = null;
        }

        public override void Reset()
        {
            base.Reset();
            if (_waitCoroutine != null)
            {
                StopCoroutine(_waitCoroutine);
                _waitCoroutine = null;
            }
            IsOn = false;
        }
    }
}