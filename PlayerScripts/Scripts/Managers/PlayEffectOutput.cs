using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// EffectManager와 SoundManager를 통해 이펙트와 SFX를 동시에 제어하는 Output 노드
    /// </summary>
    [AddComponentMenu("Visual Scripting/Output/Play Effect Output")]
    public class PlayEffectOutput : ProcessBase
    {
        [Header("Effect Settings")]
        [Tooltip("재생할 이펙트 타입 (Custom Effect Key가 비어있을 때 사용)")]
        [SerializeField] private EffectType effectType = EffectType.BumperAntennaSpark;

        [Tooltip("문자열 키로 직접 이펙트를 재생할 경우 입력 (입력 시 effectType보다 우선 적용)")]
        [SerializeField] private string customEffectKey = "";

        [Tooltip("이펙트 및 사운드 스폰 기준 Transform (비워두면 현재 노드 오브젝트 위치)")]
        [SerializeField] private Transform spawnTarget;

        [Tooltip("체크 시 생성된 이펙트를 spawnTarget의 자식으로 부착합니다.")]
        [SerializeField] private bool attachToTarget = true;

        [Header("SFX Settings")]
        [Tooltip("SoundManager의 SO_SFXTable에 등록된 사운드 키 (비워두면 사운드 미재생)")]
        [SerializeField] private string sfxKey = "";

        [Header("Duration Controls")]
        [Tooltip("체크 시 노드가 Reset되거나 외부에서 끌 때까지 이펙트 및 루프 사운드를 영구 유지합니다.")]
        [SerializeField] private bool isInfinite = false;

        [Tooltip("유지 시간(초). 0 이하면 프리팹(PooledEffect)의 기본 수명을 따릅니다.")]
        [SerializeField] private float duration = 2.0f;

        private GameObject _spawnedEffectInstance;
        private AudioSource _activeLoopAudioSource;

        public override void Execute()
        {
            Transform target = spawnTarget != null ? spawnTarget : transform;
            Transform parentToAttach = attachToTarget ? target : null;
            float effectiveDuration = isInfinite ? 0f : duration;

            // 1. 이펙트 재생 (EffectManager 위임)
            if (EffectManager.Instance != null)
            {
                CleanupEffect();

                if (!string.IsNullOrEmpty(customEffectKey))
                {
                    _spawnedEffectInstance = EffectManager.Instance.PlayEffect(
                        customEffectKey, target.position, target.rotation, parentToAttach, effectiveDuration);
                }
                else
                {
                    _spawnedEffectInstance = EffectManager.Instance.PlayEffect(
                        effectType, target.position, target.rotation, parentToAttach, effectiveDuration);
                }

                if (isInfinite && _spawnedEffectInstance != null)
                {
                    PooledEffect pooled = _spawnedEffectInstance.GetComponent<PooledEffect>();
                    if (pooled != null) pooled.SetDurationAndStart(0f, true);
                }
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}] PlayEffectOutput: EffectManager.Instance가 존재하지 않습니다.");
            }

            // 2. 사운드 재생 (SoundManager 위임)
            if (!string.IsNullOrEmpty(sfxKey))
            {
                if (SoundManager.Instance != null)
                {
                    if (isInfinite)
                    {
                        // 지속형: 루프 재생 요청 후 정지를 위해 AudioSource 참조 보관
                        _activeLoopAudioSource = SoundManager.Instance.PlayLoopSFX(sfxKey, target.position, parentToAttach);
                    }
                    else
                    {
                        // 단발성: 위치 기반 3D 풀링 재생 (시간 경과 시 SoundManager가 자동 회수)
                        SoundManager.Instance.PlaySFX(sfxKey, target.position);
                    }
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] PlayEffectOutput: SoundManager.Instance가 존재하지 않습니다.");
                }
            }

            IsOn = true;
        }

        private void CleanupEffect()
        {
            // 이펙트 회수
            if (_spawnedEffectInstance != null)
            {
                if (EffectManager.Instance != null)
                {
                    EffectManager.Instance.StopEffect(_spawnedEffectInstance);
                }
                _spawnedEffectInstance = null;
            }

            // 루프 사운드 회수
            if (_activeLoopAudioSource != null)
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.StopLoopSFX(_activeLoopAudioSource);
                }
                else
                {
                    _activeLoopAudioSource.Stop();
                }
                _activeLoopAudioSource = null;
            }
        }

        public override void Reset()
        {
            base.Reset();
            CleanupEffect();
            IsOn = false;
        }

        private void OnDisable()
        {
            CleanupEffect();
        }

        private void OnDestroy()
        {
            CleanupEffect();
        }
    }
}