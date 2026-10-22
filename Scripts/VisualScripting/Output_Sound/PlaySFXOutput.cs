using System;
using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class PlaySFXOutput : ProcessBase
    {
        [Header("Sound Settings")]
        [Tooltip("SO_SoundTable에 등록된 사운드 키(soundKey)를 입력하세요.")]
        [SerializeField] private string soundKey;

        [Tooltip("3D 공간음 위치로 사용할 Transform (비어있으면 현재 오브젝트의 위치를 사용합니다.)")]
        [SerializeField] private Transform soundLocation;

        [Header("Process Settings")]
        [Tooltip("True: 사운드 재생이 완벽히 끝난 후 IsOn을 true로 변경\nFalse: 사운드 재생 시작 즉시 IsOn을 true로 변경")]
        [SerializeField] private bool waitForCompletion = false;

        private AudioSource _activeSource;
        private Coroutine _soundCoroutine;
        private Coroutine _fadeCoroutine;

        private void Awake()
        {
            if (soundLocation == null)
            {
                soundLocation = this.transform;
            }
        }

        public override void Execute()
        {
            IsOn = false;

            if (string.IsNullOrEmpty(soundKey))
            {
                Debug.LogWarning($"[{name}] Sound Key가 지정되지 않았습니다.");
                IsOn = true;
                return;
            }

            if (SoundManager.Instance == null)
            {
                Debug.LogWarning($"[{name}] 씬에 SoundManager 인스턴스가 존재하지 않습니다.");
                IsOn = true;
                return;
            }

            StartPlaySFX();
        }

        private void StartPlaySFX()
        {
            Vector3 targetPosition = (soundLocation != null) ? soundLocation.position : transform.position;

            // 재생 중인 기존 페이드나 대기 코루틴이 있다면 정리
            ClearRunningCoroutines();

            // SoundManager를 통해 사운드 재생 및 AudioSource 참조 캐싱
            _activeSource = SoundManager.Instance.PlaySFX(soundKey, targetPosition);

            if (_activeSource == null)
            {
                IsOn = true;
                return;
            }

            if (waitForCompletion && _activeSource.clip != null)
            {
                _soundCoroutine = StartCoroutine(WaitForSoundFinishRoutine(_activeSource.clip.length));
            }
            else
            {
                IsOn = true;
            }
        }

        /// <summary>
        /// StopSFXOutput 등 외부 노드에서 사운드를 강제 종료할 때 호출
        /// </summary>
        public void StopSound(bool fadeOut = false, float fadeDuration = 0.3f, Action onComplete = null)
        {
            ClearRunningCoroutines();

            if (_activeSource == null || !_activeSource.isPlaying)
            {
                _activeSource = null;
                onComplete?.Invoke();
                return;
            }

            if (fadeOut && fadeDuration > 0f)
            {
                _fadeCoroutine = StartCoroutine(FadeOutRoutine(_activeSource, fadeDuration, onComplete));
            }
            else
            {
                _activeSource.Stop();
                _activeSource = null;
                onComplete?.Invoke();
            }
        }

        private IEnumerator FadeOutRoutine(AudioSource source, float duration, Action onComplete)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration && source != null)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
                yield return null;
            }

            if (source != null)
            {
                source.Stop();
                // 풀링 재사용 시 볼륨이 0인 상태로 고정되는 것을 방지하기 위해 복원
                source.volume = startVolume;
            }

            _activeSource = null;
            _fadeCoroutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator WaitForSoundFinishRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);

            _soundCoroutine = null;
            _activeSource = null;
            IsOn = true;
        }

        private void ClearRunningCoroutines()
        {
            if (_soundCoroutine != null)
            {
                StopCoroutine(_soundCoroutine);
                _soundCoroutine = null;
            }

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        }

        public override void Reset()
        {
            base.Reset();
            ClearRunningCoroutines();
            if (_activeSource != null && _activeSource.isPlaying)
            {
                _activeSource.Stop();
                _activeSource = null;
            }
            IsOn = false;
        }
    }
}