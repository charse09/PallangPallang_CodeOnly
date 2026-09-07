using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class CinemachineFaceCameraOutput : ProcessBase
    {
        [Header("Target Setup")]
        [Tooltip("카메라를 바라보게 할 대상 (비어있으면 Tag가 Player인 오브젝트 자동 탐색)")]
        [SerializeField] private Transform targetTransform;

        [Header("★ Toggle Control")]
        [Tooltip("체크 시: 카메라 방향을 보게 설정\n체크 해제 시: 원래 연출 상태로 복구")]
        [SerializeField] private bool toggleFaceCamera = true;

        [Header("Timing")]
        [Tooltip("노드 진행 전 대기할 시간 (0이면 즉시 완료)")]
        [SerializeField] private float holdDuration = 0.0f;

        public override void Execute()
        {
            IsOn = false;

            // Target 탐색
            if (targetTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) targetTransform = player.transform;
            }

            if (targetTransform != null)
            {
                var playerVisuals = targetTransform.GetComponentInParent<PlayerVisuals>();
                if (playerVisuals == null) playerVisuals = targetTransform.GetComponentInChildren<PlayerVisuals>();

                if (playerVisuals != null)
                {
                    // PlayerVisuals의 변수 제어
                    playerVisuals.SetFacingCamera(toggleFaceCamera);
                }
            }

            StartCoroutine(HoldRoutine());
        }

        private IEnumerator HoldRoutine()
        {
            if (holdDuration > 0f)
            {
                yield return new WaitForSeconds(holdDuration);
            }

            IsOn = true; // 다음 VisualScripting 노드로 진행
        }
    }
}