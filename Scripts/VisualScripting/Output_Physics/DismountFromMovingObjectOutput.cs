using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// AttachToMovingObject에 탑승 중인 플레이어를 강제로 하차시키고,
    /// 하차 완료(또는 착지) 시점에 지정한 Output들을 순차적으로 실행하는 노드입니다.
    /// </summary>
    public class DismountFromMovingObjectOutput : ProcessBase
    {
        [Header("1. Target Attachment")]
        [Tooltip("하차시킬 대상 AttachToMovingObject (비워둘 시 씬에서 자동 탐색)")]
        [SerializeField] private AttachToMovingObject targetAttach;

        [Header("2. Dismount Mode Override (하차 방식 오버라이드)")]
        [Tooltip("체크 시 AttachToMovingObject에 기존 설정된 방식 대신 아래 설정으로 강제 하차합니다.")]
        [SerializeField] private bool overrideSettings = false;

        [Tooltip("하차 탈출 방식")]
        [SerializeField] private AttachToMovingObject.DismountType dismountMode = AttachToMovingObject.DismountType.Momentum;

        [Header("Jump To Target Override (점프 모드 전용)")]
        [Tooltip("점프해 도착할 목표 Transform")]
        [SerializeField] private Transform jumpTargetTransform;

        [Tooltip("점프 포탄 궤적 최고 높이")]
        [SerializeField] private float jumpHeight = 3.0f;

        [Tooltip("목표 지점까지 날아가는 데 걸리는 총 시간 (초)")]
        [SerializeField] private float jumpDuration = 1.0f;

        [Header("3. Execution Timing")]
        [Tooltip("체크 시 포탄 궤적 점프가 끝나고 목표 지점에 착지한 후 Output 실행\n체크 해제 시 발판에서 떨어지자마자 즉시 Output 실행")]
        [SerializeField] private bool waitForLanding = true;

        [Header("4. Outputs On Dismount (하차 시 실행할 대상)")]
        [Tooltip("하차 완료 시 순차적으로 실행할 ProcessBase Output 목록")]
        [SerializeField] private List<ProcessBase> outputsOnDismount = new List<ProcessBase>();

        [Tooltip("추가로 호출할 인스펙터 이벤트")]
        [SerializeField] private UnityEvent onDismountEvent;

        private Coroutine _dismountCoroutine;

        public override void Reset()
        {
            base.Reset();
            if (_dismountCoroutine != null)
            {
                StopCoroutine(_dismountCoroutine);
                _dismountCoroutine = null;
            }
            IsOn = false;
        }

        public override void Execute()
        {
            IsOn = false;

            if (targetAttach == null)
            {
                targetAttach = FindFirstObjectByType<AttachToMovingObject>();
            }

            if (targetAttach == null)
            {
                Debug.LogWarning($"[{gameObject.name}] AttachToMovingObject 컴포넌트를 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            _dismountCoroutine = StartCoroutine(DismountSequence());
        }

        private IEnumerator DismountSequence()
        {
            float waitDuration = 0f;

            // 1. 하차 명령 전달
            if (overrideSettings && dismountMode == AttachToMovingObject.DismountType.JumpToTarget && jumpTargetTransform != null)
            {
                waitDuration = jumpDuration;
                targetAttach.DismountToTarget(jumpTargetTransform, jumpHeight, jumpDuration);
            }
            else
            {
                targetAttach.Detach();
            }

            // 2. 착지 대기 타이밍 처리
            if (waitForLanding && waitDuration > 0f)
            {
                yield return new WaitForSeconds(waitDuration);
            }
            else
            {
                yield return null; // 발판 분리 프레임 보장
            }

            // 3. 하차 완료 후 지정된 후속 Output들 실행
            foreach (var output in outputsOnDismount)
            {
                if (output != null)
                {
                    output.Reset();
                    output.Execute();
                }
            }

            onDismountEvent?.Invoke();

            // 4. 노드 완료 플래그 설정
            IsOn = true;
            _dismountCoroutine = null;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> Dismount 완료. 후속 Output {outputsOnDismount.Count}개 실행됨.");
#endif
        }
    }
}