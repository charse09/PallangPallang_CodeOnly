using UnityEngine;
using UnityEngine.Audio;

namespace _Project.Scripts.VisualScripting
{
    public enum AudioCommand
    {
        PlayOneShot, // 소리를 중첩해서 재생 (효과음용)
        ChangeClip,  // 현재 재생 중인 소리를 멈추고 새 소리로 교체 (배경음/환경음용)
        Stop         // 현재 재생 중인 소리를 정지
    }

    public enum AudioCategory
    {
        SFX, // AudioManager의 SFX Group 적용
        BGM  // AudioManager의 BGM Group 적용
    }

    /// <summary>
    /// 사운드 재생, 교체, 정지 기능을 통합 관리하는 확장형 Output 모듈 (AudioManager Mixer 연동)
    /// </summary>
    public class PlaySoundOutput : ProcessBase
    {
        [Header("Command Settings")]
        [Tooltip("수행할 동작을 선택하세요.")]
        [SerializeField] private AudioCommand command = AudioCommand.PlayOneShot;

        [Header("Audio Category")]
        [Tooltip("AudioManager의 어떤 볼륨 그룹(SFX/BGM)에 적용할지 선택합니다.")]
        [SerializeField] private AudioCategory audioCategory = AudioCategory.SFX;

        [Header("Audio Resources")]
        [Tooltip("재생하거나 교체할 오디오 클립")]
        [SerializeField] private AudioClip audioClip;

        [Tooltip("소리를 출력할 오디오 소스 (비워두면 자동 생성)")]
        [SerializeField] private AudioSource targetSource;

        [Header("Sound Parameters")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1.0f;

        [Tooltip("기본 음높이 설정")]
        [Range(-3f, 3f)]
        [SerializeField] private float pitch = 1.0f;

        [Tooltip("재생 속도 배수 (값만큼 피치와 속도가 곱해집니다. 2.0이면 2배속)")]
        [Range(0.1f, 3f)]
        [SerializeField] private float speedMultiplier = 1.0f;

        private void Awake()
        {
            EnsureAudioSourceSetup();
        }

        private void OnEnable()
        {
            // 씬 전환 등으로 AudioManager가 뒤늦게 생성될 경우를 대비해 믹서 그룹 재연결
            ApplyMixerGroup();
        }

        /// <summary>
        /// AudioSource 기본 세팅 및 Mixer Group 할당
        /// </summary>
        private void EnsureAudioSourceSetup()
        {
            if (targetSource == null)
            {
                targetSource = GetComponent<AudioSource>();
                if (targetSource == null)
                {
                    targetSource = gameObject.AddComponent<AudioSource>();
                    targetSource.playOnAwake = false;
                }
            }

            ApplyMixerGroup();
        }

        /// <summary>
        /// AudioManager의 AudioMixerGroup을 AudioSource에 바인딩합니다.
        /// </summary>
        public void ApplyMixerGroup()
        {
            if (targetSource == null || AudioManager.Instance == null) return;

            AudioMixerGroup targetGroup = (audioCategory == AudioCategory.SFX)
                ? AudioManager.Instance.SfxGroup
                : AudioManager.Instance.BgmGroup;

            if (targetGroup != null)
            {
                targetSource.outputAudioMixerGroup = targetGroup;
            }
        }

        public override void Execute()
        {
            EnsureAudioSourceSetup();

            if (targetSource == null) return;

            // 최종 피치 계산
            float finalPitch = pitch * speedMultiplier;
            targetSource.pitch = finalPitch;

            switch (command)
            {
                case AudioCommand.PlayOneShot:
                    if (audioClip != null)
                    {
                        targetSource.PlayOneShot(audioClip, volume);
#if UNITY_EDITOR
                        Debug.Log($"<color=cyan>[{gameObject.name}]</color> 중첩 재생: {audioClip.name}");
#endif
                    }
                    break;

                case AudioCommand.ChangeClip:
                    if (audioClip != null)
                    {
                        targetSource.Stop();
                        targetSource.clip = audioClip;
                        targetSource.volume = volume;
                        targetSource.Play();
#if UNITY_EDITOR
                        Debug.Log($"<color=yellow>[{gameObject.name}]</color> 사운드 교체: {audioClip.name}");
#endif
                    }
                    break;

                case AudioCommand.Stop:
                    targetSource.Stop();
#if UNITY_EDITOR
                    Debug.Log($"<color=red>[{gameObject.name}]</color> 사운드 정지");
#endif
                    break;
            }

            IsOn = true;
        }
    }
}