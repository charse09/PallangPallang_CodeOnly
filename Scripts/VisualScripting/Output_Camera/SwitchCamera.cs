using UnityEngine;
using Unity.Cinemachine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 여러 대의 카메라 중 하나를 선택해 활성화하고 나머지는 자동으로 우선순위를 낮추는 Output
    /// </summary>
    public class SwitchCamera : ProcessBase
    {
        [Header("카메라 그룹 설정")]
        [Tooltip("서로 전환될 가능성이 있는 모든 카메라들을 이 리스트에 넣으세요.")]
        [SerializeField] private CinemachineCamera[] cameraGroup;

        [Header("타겟 설정")]
        [Tooltip("이 트리거가 실행될 때 '주인공'이 될 카메라입니다.")]
        [SerializeField] private CinemachineCamera targetCamera;

        [Header("우선순위 수치")]
        [SerializeField] private int activePriority = 20;
        [SerializeField] private int inactivePriority = 10;

        public override void Execute()
        {
            if (targetCamera == null || cameraGroup == null || cameraGroup.Length == 0)
            {
                Debug.LogWarning($"[{gameObject.name}] 카메라 그룹 또는 타겟 카메라가 설정되지 않았습니다.");
                return;
            }

            // 핵심 로직: 루프를 돌며 타겟만 높이고 나머지는 모두 낮춤
            foreach (var cam in cameraGroup)
            {
                if (cam == null) continue;

                if (cam == targetCamera)
                {
                    cam.Priority = activePriority;
                }
                else
                {
                    // 타겟이 아닌 모든 카메라는 낮은 우선순위로 강제 설정
                    cam.Priority = inactivePriority;
                }
            }

            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[Camera Group]</color> '{targetCamera.gameObject.name}' 활성화 (나머지 {cameraGroup.Length - 1}대 비활성화)");
#endif
        }

        public new void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}