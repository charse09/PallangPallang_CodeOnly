using System.Collections;
using UnityEngine;
using Unity.Cinemachine; // [중요] 시네머신 네임스페이스를 가져옵니다.

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// Cinemachine 환경에서 플레이어가 기계 위에 서 있을 때 
    /// CinemachineImpulseSource를 통해 화면 전체를 주기적으로 뒤흔드는 Output 모듈
    /// </summary>
    [RequireComponent(typeof(CinemachineImpulseSource))] // 이 컴포넌트가 필수적으로 필요합니다.
    public class CameraVibrationOutput : ProcessBase
    {
        [Header("Vibration Settings")]
        [Tooltip("기계 진동 연출이 유지될 총 시간 (초 단위)")]
        [SerializeField] private float duration = 3f;

        [Tooltip("충격을 발생시킬 주기 (초 단위, 낮을수록 촘촘하고 격렬하게 진동함)")]
        [SerializeField] private float pulseInterval = 0.05f;

        [Tooltip("기본 진동의 세기 가중치")]
        [SerializeField] private float shakeForceMultiplier = 1.0f;

        [Tooltip("플레이어가 충돌 중일 때만 흔들림을 적용할지 여부")]
        [SerializeField] private bool requirePlayerCollision = true;

        private CinemachineImpulseSource _impulseSource;
        private bool _isPlayerStanding = false;
        private bool _isShaking = false;

        private void Start()
        {
            _impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        public override void Execute()
        {
            // 실행 시작 시 상태 초기화
            IsOn = false;

            if (_impulseSource == null) _impulseSource = GetComponent<CinemachineImpulseSource>();

            if (_impulseSource == null)
            {
                Debug.LogWarning($"[{gameObject.name}] CinemachineImpulseSource 컴포넌트를 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            if (!_isShaking)
            {
                StartCoroutine(CinemachineShakeRoutine());
            }
        }

        private IEnumerator CinemachineShakeRoutine()
        {
            _isShaking = true;
            float elapsed = 0f;
            float nextPulseTime = 0f;

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> 시네머신 임펄스 진동 시퀀스 가동!");
#endif

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // 플레이어가 기계 위에 서 있거나 충돌 체크가 필요 없는 경우에만 주기적으로 임펄스 충격 신호 폭사
                if ((!requirePlayerCollision || _isPlayerStanding) && elapsed >= nextPulseTime)
                {
                    float progress = elapsed / duration;
                    // 시간이 끝날수록 서서히 진동이 잦아들도록 감쇄 설정
                    float fadeOut = Mathf.Lerp(1f, 0f, progress);

                    // 임펄스 소스에 설정된 프리셋 방향 벡터(기본적으로 무작위)로 신호 발생
                    // 원래 속도와 세기에 감쇄와 가중치를 곱해 튀겨줍니다.
                    _impulseSource.GenerateImpulse(Vector3.one * shakeForceMultiplier * fadeOut);

                    // 다음 충격을 주기 전 대기 시간 갱신
                    nextPulseTime = elapsed + pulseInterval;
                }

                yield return null;
            }

            _isShaking = false;

            // 프레임워크 규칙: 시퀀스 완료 알림
            IsOn = true;
        }

        // --- 물리 충돌 영역을 통한 플레이어 탑승 감지 ---
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("Player")) _isPlayerStanding = true;
        }

        private void OnCollisionStay(Collision collision)
        {
            if (collision.collider.CompareTag("Player")) _isPlayerStanding = true;
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.collider.CompareTag("Player")) _isPlayerStanding = false;
        }
    }
}