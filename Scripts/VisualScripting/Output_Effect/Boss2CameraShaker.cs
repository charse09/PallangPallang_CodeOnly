using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

namespace _Project.Scripts.VisualScripting
{
    public enum CameraShakePreset
    {
        Low,      // 하: 약한 진동 (펀치, 가벼운 타격 등)
        Medium,   // 중: 중간 진동 (철퇴 휘두르기, 우퍼 충격파 등)
        High,     // 상: 강한 진동 (내려찍기, 돌진 충돌 등)
        Custom    // 커스텀: 진폭/주파수 직접 지정
    }

    [System.Serializable]
    public struct SkillShakeData
    {
        [Tooltip("연동할 보스 스킬 종류")]
        public BossSkillType skillType;

        [Tooltip("흔들림 강도 프리셋 (상/중/하/커스텀)")]
        public CameraShakePreset shakePreset;

        [Tooltip("카메라 흔들림 지속 시간 (초) - 꾸준한 느낌을 원하면 0.4초 이상 권장")]
        public float duration;

        [Header("Custom Preset Settings (shakePreset이 Custom일 때 적용)")]
        [Tooltip("커스텀 흔들림 강도 (진폭 / Force)")]
        public float customMagnitude;

        [Tooltip("커스텀 흔들림 진동수 (주파수/노이즈 속도)")]
        public float customFrequency;

        [Tooltip("최대 강도를 유지할 비율 (0.0: 즉시 감쇄, 0.7: 70% 동안 최대 강도 유지)")]
        [Range(0f, 0.9f)]
        public float sustainRatio;
    }

    public class Boss2CameraShaker : MonoBehaviour
    {
        [Header("Cinemachine 3.x Settings")]
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private CinemachineCamera virtualCamera;

        [Header("Fallback Settings")]
        [SerializeField] private Camera targetCamera;

        [Header("Skill Shake Mapping List")]
        public List<SkillShakeData> shakeDataList = new List<SkillShakeData>();

        private Dictionary<BossSkillType, SkillShakeData> _shakeDict = new Dictionary<BossSkillType, SkillShakeData>();

        private Transform _camTransform;
        private Vector3 _originalCamLocalPos;
        private Quaternion _originalCamLocalRot;
        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();
            if (targetCamera == null) targetCamera = Camera.main;

            if (targetCamera != null)
            {
                _camTransform = targetCamera.transform;
            }

            RebuildDictionary();
        }

        public void RebuildDictionary()
        {
            _shakeDict.Clear();
            foreach (var data in shakeDataList)
            {
                if (!_shakeDict.ContainsKey(data.skillType))
                {
                    _shakeDict.Add(data.skillType, data);
                }
            }
        }

        public void Shake(BossSkillType skillType)
        {
            if (_shakeDict.Count == 0 && shakeDataList.Count > 0)
            {
                RebuildDictionary();
            }

            if (!_shakeDict.TryGetValue(skillType, out SkillShakeData data))
            {
                return;
            }

            GetPresetValues(data, out float magnitude, out float frequency);
            float sustain = data.sustainRatio > 0 ? data.sustainRatio : 0.5f; // 기본 50% 지속

            // 1순위: Cinemachine 3.x ImpulseSource 사용
            if (impulseSource != null)
            {
                impulseSource.GenerateImpulseWithVelocity(Random.insideUnitSphere.normalized * magnitude);
                return;
            }

            // 2순위: Cinemachine 3.x Camera Perlin Noise 제어
            if (virtualCamera != null)
            {
                var perlin = virtualCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();
                if (perlin != null)
                {
                    if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
                    _shakeCoroutine = StartCoroutine(DoCinemachineNoiseRoutine(perlin, data.duration, magnitude, frequency, sustain));
                    return;
                }
            }

            // 3순위: 일반 Transform 카메라 위치/회전 기반 셰이크
            ShakeDirect(data.duration, magnitude, frequency, sustain);
        }

        public void ShakeDirect(float duration, float magnitude, float frequency = 35f, float sustainRatio = 0.5f)
        {
            if (_camTransform == null)
            {
                if (Camera.main != null)
                {
                    targetCamera = Camera.main;
                    _camTransform = targetCamera.transform;
                }
                else return;
            }

            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _camTransform.localPosition = _originalCamLocalPos;
                _camTransform.localRotation = _originalCamLocalRot;
            }

            _shakeCoroutine = StartCoroutine(DoTransformShakeRoutine(duration, magnitude, frequency, sustainRatio));
        }

        private IEnumerator DoCinemachineNoiseRoutine(CinemachineBasicMultiChannelPerlin perlin, float duration, float magnitude, float frequency, float sustainRatio)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // sustainRatio 구간 동안은 1.0 유지, 이후 smoothstep으로 감소
                float damping = progress <= sustainRatio
                    ? 1.0f
                    : Mathf.SmoothStep(1.0f, 0.0f, (progress - sustainRatio) / (1.0f - sustainRatio));

                perlin.AmplitudeGain = magnitude * damping;
                perlin.FrequencyGain = frequency;
                yield return null;
            }

            perlin.AmplitudeGain = 0f;
            perlin.FrequencyGain = 0f;
            _shakeCoroutine = null;
        }

        private IEnumerator DoTransformShakeRoutine(float duration, float magnitude, float frequency, float sustainRatio)
        {
            _originalCamLocalPos = _camTransform.localPosition;
            _originalCamLocalRot = _camTransform.localRotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;

                // 지속 구간 적용
                float damping = progress <= sustainRatio
                    ? 1.0f
                    : Mathf.SmoothStep(1.0f, 0.0f, (progress - sustainRatio) / (1.0f - sustainRatio));

                // 위치 노이즈 (Perlin Noise)
                float offsetX = (Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f) * 2f * magnitude * damping;
                float offsetY = (Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f) * 2f * magnitude * damping;

                // 회전 노이즈 (살짝 기울여 타격감 강화)
                float offsetRotZ = (Mathf.PerlinNoise(Time.time * frequency * 0.8f, 10f) - 0.5f) * 2f * (magnitude * 0.5f) * damping;

                _camTransform.localPosition = _originalCamLocalPos + new Vector3(offsetX, offsetY, 0f);
                _camTransform.localRotation = _originalCamLocalRot * Quaternion.Euler(0f, 0f, offsetRotZ);

                yield return null;
            }

            _camTransform.localPosition = _originalCamLocalPos;
            _camTransform.localRotation = _originalCamLocalRot;
            _shakeCoroutine = null;
        }

        private void GetPresetValues(SkillShakeData data, out float magnitude, out float frequency)
        {
            switch (data.shakePreset)
            {
                case CameraShakePreset.Low: // 약한 진동
                    magnitude = 1.0f;
                    frequency = 30f;
                    break;
                case CameraShakePreset.Medium: // 중간 진동
                    magnitude = 2.5f;
                    frequency = 40f;
                    break;
                case CameraShakePreset.High: // 강한 진동
                    magnitude = 5.0f;
                    frequency = 50f;
                    break;
                case CameraShakePreset.Custom: // 커스텀
                    magnitude = data.customMagnitude;
                    frequency = data.customFrequency > 0 ? data.customFrequency : 35f;
                    break;
                default:
                    magnitude = 2.0f;
                    frequency = 35f;
                    break;
            }
        }
    }
}