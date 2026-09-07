

using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 카메라로 스위칭할 때, 어떻게(부드럽게, 즉시 등) 넘어갈지 스타일까지 제어하는 노드.
    /// </summary>
    public class CinemachineDirectorOutput : ProcessBase
    {
        [Header("Target Camera Setup")]
        [SerializeField] private CinemachineCamera targetVirtualCamera;

        [Header("★ Transition (넘어가는 연출 설정)")]
        [Tooltip("이 컷으로 전환될 때 어떻게 보간하며 넘어갈지 고르세요.")]
        [SerializeField] private CameraBlendStyle blendStyle = CameraBlendStyle.EaseInOut;
        
        [Tooltip("스르륵 넘어갈 시간(초) 입니다. (Cut 모드일 때는 무시됨)")]
        [SerializeField] private float blendTime = 1.5f;

        [Header("Timing (유지 시간)")]
        [Tooltip("화면이 완전히 전환된 후 이 구도를 유지할 시간 (-1이면 다음 카메라가 새치기할 때까지 무한 유지)")]
        [SerializeField] private float holdDuration = 2.0f;

        public override void Execute()
        {
            IsOn = false;

            if (targetVirtualCamera == null)
            {
                IsOn = true;
                return;
            }

            // 💡 사령탑에게 목표 카메라 정보와 함께 '전환 연출 스타일 및 시간'까지 세트로 던집니다.
            if (CinemachinePriorityManager.Instance != null)
            {
                CinemachinePriorityManager.Instance.SwitchActiveCamera(targetVirtualCamera, blendStyle, blendTime);
            }

            StartCoroutine(HoldRoutine());
        }

        private IEnumerator HoldRoutine()
        {
            // 화면이 넘어가는 시간(blendTime)만큼은 무조건 기다려주고
            float actualBlendTime = (blendStyle == CameraBlendStyle.Cut) ? 0f : blendTime;
            yield return new WaitForSeconds(actualBlendTime);

            // 그 이후 안착해서 연출을 보여주며 대기하는 흐름
            if (holdDuration < 0f)
            {
                while (true) yield return null;
            }
            else if (holdDuration > 0f)
            {
                yield return new WaitForSeconds(holdDuration);
            }

            IsOn = true; // 완료 후 다음 비주얼 노드로 바톤 터치
        }
    }
}