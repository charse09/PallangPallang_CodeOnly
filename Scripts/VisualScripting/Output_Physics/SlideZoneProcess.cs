using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class SlideZoneProcess : ProcessBase
    {
        [Header("Target Settings")]
        [SerializeField] private GameObject playerObject;

        [Header("Slide Zone Settings")]
        [Tooltip("강제로 진행하게 할 방향 벡터 (예: Z축 정면은 0, 0, 1)")]
        [SerializeField] private Vector3 slideDirection = Vector3.forward;

        [Tooltip("강제 슬라이드 속도")]
        [SerializeField] private float forwardSpeed = 18f;

        [Tooltip("미끌거리는 강도 (추천: 1.0 ~ 2.0 / 낮을수록 정지 제동이 풀려 미끄러짐)")]
        [SerializeField] private float slideSharpness = 1.5f;

        private PlayerLocomotion _playerLocomotion;

        private void Awake()
        {
            if (playerObject == null) playerObject = GameObject.FindGameObjectWithTag("Player");
        }

        public override void Execute()
        {
            IsOn = true;

            if (playerObject != null)
            {
                _playerLocomotion = playerObject.GetComponent<PlayerLocomotion>();
                if (_playerLocomotion != null)
                {
                    _playerLocomotion.EnterSlideZone(slideDirection, forwardSpeed, slideSharpness);
                    Debug.Log($"{name}: 강제 슬라이드 기믹 구역 진입.");
                }
            }
            else
            {
                Debug.LogWarning($"{name}: 플레이어 오브젝트를 맵핑할 수 없습니다.");
                IsOn = false;
            }
        }

        public void ExitZone()
        {
            if (_playerLocomotion != null)
            {
                _playerLocomotion.ExitSlideZone();
                Debug.Log($"{name}: 강제 슬라이드 기믹 구역 종료.");
            }
            IsOn = false;
        }
    }
}