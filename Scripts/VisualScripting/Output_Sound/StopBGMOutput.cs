using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// SoundManager를 통해 메인 BGM, 서브 BGM(컷씬), 또는 환경음을 정지하는 ProcessBase 아웃풋입니다.
    /// </summary>
    public class StopBGMOutput : ProcessBase
    {
        public enum StopChannelType
        {
            MainBGM,          // 메인 배경음악 정지
            SubBGM,           // 서브 배경음악(컷씬용) 정지
            SpecificAmbience, // 특정 키의 환경음 정지
            AllAmbience,      // 모든 환경음 정지
            All               // 메인 BGM, 서브 BGM, 모든 환경음 일괄 정지
        }

        [Header("Stop Channel Settings")]
        [Tooltip("정지할 사운드 채널을 선택합니다.")]
        [SerializeField] private StopChannelType stopTarget = StopChannelType.MainBGM;

        [Tooltip("StopTarget이 'SpecificAmbience'일 때 정지할 환경음의 등록 키입니다.")]
        [SerializeField] private string ambienceKey;

        [Header("Fade Settings")]
        [Tooltip("체크 시 서서히 꺼지는 페이드 아웃 효과를 적용합니다.")]
        [SerializeField] private bool useFadeOut = true;

        [Tooltip("페이드 아웃 연출 시간(초 단위)입니다.")]
        [SerializeField] private float fadeDuration = 1.0f;

        [Header("Process Settings")]
        [Tooltip("True: 페이드 아웃 완료 후 다음 노드로 진행\nFalse: 정지 명령 즉시 다음 노드로 진행")]
        [SerializeField] private bool waitForFade = false;

        private Coroutine _fadeWaitCoroutine;

        public override void Execute()
        {
            IsOn = false;

            if (SoundManager.Instance == null)
            {
                Debug.LogWarning($"[{name}] SoundManager 인스턴스를 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            if (stopTarget == StopChannelType.SpecificAmbience && string.IsNullOrEmpty(ambienceKey))
            {
                Debug.LogWarning($"[{name}] 정지할 환경음 Key가 지정되지 않았습니다.");
                IsOn = true;
                return;
            }

            StartStopAudio();
        }

        private void StartStopAudio()
        {
            switch (stopTarget)
            {
                case StopChannelType.MainBGM:
                    SoundManager.Instance.StopBGM(useFadeOut, fadeDuration);
                    break;

                case StopChannelType.SubBGM:
                    SoundManager.Instance.StopSubBGM(useFadeOut, fadeDuration);
                    break;

                case StopChannelType.SpecificAmbience:
                    SoundManager.Instance.StopAmbience(ambienceKey, useFadeOut, fadeDuration);
                    break;

                case StopChannelType.AllAmbience:
                    SoundManager.Instance.StopAllAmbience(useFadeOut, fadeDuration);
                    break;

                case StopChannelType.All:
                    SoundManager.Instance.StopBGM(useFadeOut, fadeDuration);
                    SoundManager.Instance.StopSubBGM(useFadeOut, fadeDuration);
                    SoundManager.Instance.StopAllAmbience(useFadeOut, fadeDuration);
                    break;
            }

            if (waitForFade && useFadeOut && fadeDuration > 0f)
            {
                if (_fadeWaitCoroutine != null) StopCoroutine(_fadeWaitCoroutine);
                _fadeWaitCoroutine = StartCoroutine(WaitForFadeRoutine(fadeDuration));
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