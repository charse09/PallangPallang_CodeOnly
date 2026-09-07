using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class SetSlideZone : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("슬라이드 구역 효과를 받을 플레이어 (비워두면 씬에서 'Player' 태그를 자동으로 찾음)")]
        [SerializeField] private GameObject targetPlayer;

        [Header("Slide Zone Settings")]
        [Tooltip("True면 진입(Enter), False면 퇴출(Exit)")]
        [SerializeField] private bool isEntering = true;

        [Tooltip("미끄러질 방향을 지정할 트랜스폼 (진입 시에만 사용. 비워두면 이 스크립트가 달린 객체의 앞방향)")]
        [SerializeField] private Transform directionReference;

        [Tooltip("슬라이드 구역에서의 전진 속도")]
        [SerializeField] private float slideSpeed = 25f;

        [Tooltip("좌우 회피 기동의 민첩성 (수치가 클수록 빠릿하게 움직임)")]
        [SerializeField] private float slideSharpness = 3f;

        private void Awake()
        {
            // 인스펙터에서 타겟을 비워뒀다면, 시작할 때 플레이어 태그로 자동 할당
            if (targetPlayer == null)
            {
                targetPlayer = GameObject.FindWithTag("Player");
            }
        }

        public override void Execute()
        {
            // 실행 신호 ON
            IsOn = true;

            if (targetPlayer != null && targetPlayer.TryGetComponent(out PlayerLocomotion player))
            {
                if (isEntering)
                {
                    // 방향 기준점이 없으면 자기 자신의 앞방향(Z축)을 사용
                    Vector3 slideDir = directionReference != null ? directionReference.forward : transform.forward;

                    // PlayerLocomotion에 이미 만들어져 있던 함수 호출
                    player.EnterSlideZone(slideDir, slideSpeed, slideSharpness);
                    Debug.Log($"{name}: {targetPlayer.name} 슬라이드 구역 진입 완료");
                }
                else
                {
                    // 구역 퇴출 시 호출
                    player.ExitSlideZone();
                    Debug.Log($"{name}: {targetPlayer.name} 슬라이드 구역 퇴출 완료");
                }
            }
            else
            {
                Debug.LogWarning($"{name}: 타겟 플레이어를 찾을 수 없거나 PlayerLocomotion 컴포넌트가 없습니다.");
            }
        }

        // 에디터에서 미끄러지는 방향을 직관적으로 볼 수 있게 기즈모(화살표) 렌더링
        private void OnDrawGizmos()
        {
            if (!isEntering) return;

            Vector3 center = transform.position;
            Vector3 direction = directionReference != null ? directionReference.forward : transform.forward;

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(center, direction * 3f);

            // 화살표 머리 모양 그리기
            Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + 20, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - 20, 0) * Vector3.forward;
            Gizmos.DrawRay(center + direction * 3f, right * 1f);
            Gizmos.DrawRay(center + direction * 3f, left * 1f);
        }
    }
}