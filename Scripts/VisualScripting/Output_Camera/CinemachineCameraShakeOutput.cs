using System.Collections;
using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using Unity.Cinemachine;
#else
using Cinemachine;       
#endif

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 트리거 신호를 받아 Cinemachine의 Noise(Perlin) 강도를 조절하여 
    /// 카메라 진동 효과(Camera Shake)를 주는 노코드 아웃풋 모듈
    /// </summary>
    public class CinemachineCameraShakeOutput : ProcessBase
    {
        [Header("Cinemachine Target")]
        [Tooltip("진동 효과를 줄 Cinemachine Virtual Camera 오브젝트")]
#if UNITY_6000_0_OR_NEWER
        [SerializeField] private CinemachineCamera virtualCamera;
#else
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
#endif

        [Header("Shake Settings")]
        [Tooltip("진동이 유지될 시간 (초)")]
        [SerializeField] private float duration = 0.5f;

        [Tooltip("진동 세기 (Amplitude Gain)")]
        [SerializeField] private float amplitude = 1.5f;

        [Tooltip("진동 속도 / 빈도 (Frequency Gain)")]
        [SerializeField] private float frequency = 20f;

        [Tooltip("체크 시 시간이 지남에 따라 진동이 부드럽게 감소(Fade Out)합니다.")]
        [SerializeField] private bool fadeOut = true;

        private CinemachineBasicMultiChannelPerlin _noise;
        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            if (virtualCamera != null)
            {
#if UNITY_6000_0_OR_NEWER
                _noise = virtualCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
#else
                _noise = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
#endif

                // [핵심 추가 포인트] 
                // 에디터에서 실수로 값을 켜두더라도, 게임 시작 시 무조건 진동을 멈춤(0) 상태로 둡니다.
                if (_noise != null)
                {
                    _noise.AmplitudeGain = 0f;
                    _noise.FrequencyGain = 0f;
                }
            }
        }


        public override void Execute()
        {
            if (_noise == null)
            {
                Debug.LogWarning($"<color=yellow>[{gameObject.name}]</color> CinemachineBasicMultiChannelPerlin component not found! 카메라에 Noise를 추가해주세요.");
                IsOn = true; // Visual Scripting flow continues
                return;
            }

            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }

            _shakeCoroutine = StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            float elapsedTime = 0f;

            // 진동 시작 시 파라미터 적용
            _noise.AmplitudeGain = amplitude;
            _noise.FrequencyGain = frequency;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / duration;

                if (fadeOut)
                {
                    // 선형 보간(Lerp)을 사용하여 강도를 0으로 부드럽게 감소
                    _noise.AmplitudeGain = Mathf.Lerp(amplitude, 0f, progress);
                }

                yield return null;
            }

            // 진동 종료 후 완벽히 초기화
            _noise.AmplitudeGain = 0f;
            _noise.FrequencyGain = 0f;
            _shakeCoroutine = null;

            IsOn = true;
        }

        private void OnDisable()
        {
            // 컴포넌트가 비활성화될 때 진동이 무한히 남는 것을 방지
            if (_noise != null)
            {
                _noise.AmplitudeGain = 0f;
                _noise.FrequencyGain = 0f;
            }
        }
    }
}