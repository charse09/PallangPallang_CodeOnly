using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 양손(여러 개의 TelekinesisBeam)을 구분하여 특정 물체에 동적으로 연결하고 활성화하는 Output 노드.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Outputs/텔레키네시스 활성화 (Output)")]
    public class ActivateTelekinesisOutput : ProcessBase
    {
        [Header("Telekinesis Setup")]
        [Tooltip("제어할 TelekinesisBeam (오른손/왼손 중 선택하여 할당. 비워둘 경우 Player Hand로 씬에서 식별합니다)")]
        [SerializeField] private TelekinesisBeam telekinesisBeam;

        [Tooltip("텔레키네시스 빔을 연결할 목표 물체")]
        [SerializeField] private Transform targetObject;

        [Header("Hand Setup (양손 구분용)")]
        [Tooltip("특정 손(오른손/왼손 Transform)을 할당하면 해당 손의 TelekinesisBeam을 자동으로 찾아 연결합니다.")]
        [SerializeField] private Transform playerHand;

        public override void Execute()
        {
            IsOn = false;

            // 1. telekinesisBeam이 직접 지정되지 않았고, playerHand가 설정되어 있다면 해당 손의 Beam을 탐색
            if (telekinesisBeam == null && playerHand != null)
            {
                var allBeams = FindObjectsByType<TelekinesisBeam>(FindObjectsSortMode.None);
                foreach (var beam in allBeams)
                {
                    if (beam.playerHand == playerHand)
                    {
                        telekinesisBeam = beam;
                        break;
                    }
                }
            }

            // 2. 여전히 null이면 씬에서 아무 빔이나 기본 탐색
            if (telekinesisBeam == null)
            {
                telekinesisBeam = FindFirstObjectByType<TelekinesisBeam>();
            }

            if (telekinesisBeam == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 씬에서 조건에 맞는 TelekinesisBeam을 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            if (targetObject == null)
            {
                Debug.LogWarning($"[{gameObject.name}] targetObject가 지정되지 않았습니다.");
                IsOn = true;
                return;
            }

            // 필요 시 손 위치 재할당
            if (playerHand != null)
            {
                telekinesisBeam.playerHand = playerHand;
            }

            // 빔 활성화 및 타겟 연결[cite: 6]
            telekinesisBeam.SetBeamActive(true, targetObject);

            IsOn = true;
        }
    }
}