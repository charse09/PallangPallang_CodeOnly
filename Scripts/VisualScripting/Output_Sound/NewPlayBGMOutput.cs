using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// BGM 테이블의 Key값을 바탕으로 SoundManager를 통해 메인 BGM, 서브 BGM(컷씬용), 환경음을 재생하는 아웃풋입니다.
    /// </summary>
    public class NewPlayBGMOutput : ProcessBase
    {
        public enum BGMChannelType
        {
            MainBGM,     // 일반 배경음악
            SubBGM,      // 컷씬/특수 연출용 서브 배경음악 (메인 BGM과 동시 재생)
            Ambience     // 환경음 및 배경 소음
        }

        [Header("Channel Settings")]
        [Tooltip("재생할 오디오 채널을 선택합니다.\n- MainBGM: 메인 배경음악\n- SubBGM: 컷씬/이벤트용 서브 배경음악\n- Ambience: 환경 배경음(비, 바람 등)")]
        [SerializeField] private BGMChannelType channelType = BGMChannelType.MainBGM;

        [Header("BGM Key Settings")]
        [Tooltip("SO_BGMTable에 등록된 사운드 식별 키(Key)입니다. (예: BGM_Lobby, BGM_Cutscene_01)")]
        [SerializeField] private string bgmKey;

        [Header("Playback Options")]
        [Tooltip("체크 시 0부터 목표 볼륨까지 서서히 커지는 페이드 인 효과를 적용합니다.")]
        [SerializeField] private bool useFadeIn = true;

        [Tooltip("체크 시 동일한 키의 BGM이 재생 중이더라도 곡을 처음부터 다시 재생합니다.")]
        [SerializeField] private bool restartIfPlaying = false;

        [Header("Fade Duration Override (선택 사항)")]
        [Tooltip("체크 시 테이블 기본 페이드 시간을 무시하고 아래의 커스텀 시간을 사용합니다.")]
        [SerializeField] private bool overrideFadeDuration = false;

        [Tooltip("overrideFadeDuration 활성화 시 적용할 페이드 인 시간(초)입니다.")]
        [SerializeField] private float customFadeDuration = 1.5f;

        [Header("Process Settings")]
        [Tooltip("True: 페이드 인이 완전히 끝날 때까지 대기 후 완료\nFalse: 재생 명령 즉시 다음 노드로 진행")]
        [SerializeField] private bool waitForFade = false;

        private Coroutine _fadeWaitCoroutine;

        public override void Execute()
        {
            IsOn = false;

            if (string.IsNullOrEmpty(bgmKey))
            {
                Debug.LogWarning($"[{name}] 재생할 BGM Key가 지정되지 않았습니다.");
                IsOn = true;
                return;
            }

            if (SoundManager.Instance == null)
            {
                Debug.LogWarning($"[{name}] SoundManager 인스턴스를 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            StartPlayAudio();
        }

        private void StartPlayAudio()
        {
            float? fadeOverride = overrideFadeDuration ? customFadeDuration : (float?)null;

            switch (channelType)
            {
                case BGMChannelType.MainBGM:
                    SoundManager.Instance.PlayBGM(bgmKey, useFadeIn, fadeOverride, restartIfPlaying);
                    break;
                case BGMChannelType.SubBGM:
                    SoundManager.Instance.PlaySubBGM(bgmKey, useFadeIn, fadeOverride, restartIfPlaying);
                    break;
                case BGMChannelType.Ambience:
                    SoundManager.Instance.PlayAmbience(bgmKey, useFadeIn, fadeOverride, restartIfPlaying);
                    break;
            }

            if (waitForFade && useFadeIn)
            {
                float waitTime = overrideFadeDuration ? customFadeDuration : 1.5f;
                if (_fadeWaitCoroutine != null) StopCoroutine(_fadeWaitCoroutine);
                _fadeWaitCoroutine = StartCoroutine(WaitForFadeRoutine(waitTime));
            }
            else
            {
                IsOn = true;
            }
        }

        private IEnumerator WaitForFadeRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            _fadeWaitCoroutine = null;
            IsOn = true;
        }

        public override void Reset()
        {
            base.Reset();
            if (_fadeWaitCoroutine != null)
            {
                StopCoroutine(_fadeWaitCoroutine);
                _fadeWaitCoroutine = null;
            }
        }
    }
}