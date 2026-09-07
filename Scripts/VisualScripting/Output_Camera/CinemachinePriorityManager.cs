using UnityEngine;
using Unity.Cinemachine; // Unity 6 시네머신 네임스페이스

namespace _Project.Scripts.VisualScripting
{
    // 기획자가 고를 수 있는 정석 전환 스타일 정의
    public enum CameraBlendStyle
    {
        Cut,          // 띡! 하고 0초 만에 즉시 전환
        Linear,       // 일정한 속도로 정직하게 슥 이동
        EaseIn,       // 느리게 시작해서 빠르게 안착
        EaseOut,      // 빠르게 시작해서 부드럽게 감속하며 안착
        EaseInOut     // 부드럽게 시작해서 부드럽게 안착 (가장 영화적이고 기본적임)
    }

    public class CinemachinePriorityManager : MonoBehaviour
    {
        public static CinemachinePriorityManager Instance { get; private set; }

        private CinemachineBrain _brain;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            // 씬에서 메인 카메라에 붙어있는 시네머신 브레인을 자동으로 찾아둡니다.
            _brain = FindFirstObjectByType<CinemachineBrain>();
        }

        /// <summary>
        /// 씬의 카메라 점수를 정리하면서, 다음 카메라로 어떻게 넘어갈지(Blend) 스타일까지 실시간 주입합니다.
        /// </summary>
        public void SwitchActiveCamera(CinemachineCamera targetCamera, CameraBlendStyle blendStyle, float blendTime)
        {
            if (targetCamera == null) return;

            // 💡 [실시간 전환 스타일 해킹] 브레인의 기본 블렌딩 규칙을 노드가 원하는 대로 세팅합니다.
            if (_brain != null)
            {
                // 1. 스타일 변환
                switch (blendStyle)
                {
                    case CameraBlendStyle.Cut:
                        _brain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.Cut;
                        break;
                    case CameraBlendStyle.Linear:
                        _brain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.Linear;
                        break;
                    case CameraBlendStyle.EaseIn:
                        _brain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.EaseIn;
                        break;
                    case CameraBlendStyle.EaseOut:
                        _brain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.EaseOut;
                        break;
                    case CameraBlendStyle.EaseInOut:
                        _brain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.EaseInOut;
                        break;
                }

                // 2. 시간 변환 (Cut이면 자동으로 0초 처리)
                _brain.DefaultBlend.Time = (blendStyle == CameraBlendStyle.Cut) ? 0f : blendTime;
            }

            // 우선순위 정리는 기존 방식 그대로 안전하게 처리
            CinemachineCamera[] allCameras = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            foreach (var cam in allCameras)
            {
                cam.Priority = (cam == targetCamera) ? 10 : 0;
            }
        }
    }
}