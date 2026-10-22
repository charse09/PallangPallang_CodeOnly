using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public enum TargetAudioAction
    {
        Play,   // 사운드 생성 및 재생 (루프 또는 단발)
        Stop    // 재생 중인 사운드 정지 (즉시 정지 또는 페이드아웃)
    }

    /// <summary>
    /// 지정된 오브젝트의 자식으로 AudioSource를 찾아 연결하거나 자동 생성하여
    /// 3D 위치 추적 사운드를 재생/정지하는 프레임워크 전용 Output 노드
    /// </summary>
    public class AttachedAudioOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("소리가 출력될 대상 오브젝트 (비워둘 경우 이 스크립트가 붙은 오브젝트)")]
        [SerializeField] private Transform targetObject;

        [Tooltip("동작 유형 (Play: 재생, Stop: 정지)")]
        [SerializeField] private TargetAudioAction action = TargetAudioAction.Play;

        [Tooltip("자식으로 생성/탐색할 사운드 오브젝트의 이름 식별자")]
        [SerializeField] private string audioChildIdentifier = "Attached_AudioSource";

        [Header("Sound Settings (Play 선택 시)")]
        [Tooltip("SO_SFXTable에 등록된 사운드 키 (비워두면 아래 directClip 사용)")]
        [SerializeField] private string soundKey = "";

        [Tooltip("테이블 키 대신 직접 오디오 클립을 연결할 때 사용")]
        [SerializeField] private AudioClip directClip;

        [Tooltip("무한 반복 여부 (엔진음, 기믹 가동음 등은 체크)")]
        [SerializeField] private bool loop = false;

        [Range(0f, 1f)]
        [SerializeField] private float volume = 1.0f;

        [Header("3D Spatial Settings (Play 선택 시)")]
        [Tooltip("체크 시 거리에 따라 소리가 작아지는 3D 사운드로 설정")]
        [SerializeField] private bool use3D = true;

        [Tooltip("소리가 최대 크기로 들리는 최소 거리 (미터)")]
        [SerializeField] private float minDistance = 2f;

        [Tooltip("소리가 완전히 사라지는 최대 거리 (미터)")]
        [SerializeField] private float maxDistance = 25f;

        [Header("Stop Settings (Stop 선택 시)")]
        [Tooltip("체크 시 서서히 볼륨이 줄어들며 자연스럽게 멈춥니다.")]
        [SerializeField] private bool useFadeOut = true;

        [Tooltip("페이드아웃 시간 (초)")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Tooltip("정지 후 생성되었던 자식 사운드 오브젝트를 완전히 파괴할지 여부")]
        [SerializeField] private bool destroyOnStop = false;

        [Header("Progression Settings")]
        [Tooltip("체크 시 사운드 재생 완료/페이드아웃을 기다리지 않고 즉시 다음 노드로 진행")]
        [SerializeField] private bool runInBackground = true;

        private Coroutine _fadeCoroutine;

        public override void Execute()
        {
            if (IsOn) return;
            IsOn = false;

            Transform target = targetObject != null ? targetObject : transform;

            if (action == TargetAudioAction.Play)
            {
                ExecutePlay(target);
            }
            else
            {
                ExecuteStop(target);
            }
        }

        private void ExecutePlay(Transform target)
        {
            AudioSource source = GetOrCreateAudioSource(target);
            if (source == null)
            {
                IsOn = true;
                return;
            }

            // 1. 클립 세팅 (테이블 키 우선 -> 직접 등록 클립)
            AudioClip clipToPlay = directClip;
            float finalVolume = volume;

            if (!string.IsNullOrEmpty(soundKey) && SoundManager.Instance != null)
            {
                if (SoundManager.Instance.TryGetSFXData(soundKey, out var sfxData))
                {
                    clipToPlay = sfxData.clip;
                    finalVolume = sfxData.volume * volume;
                }
            }

            if (clipToPlay == null)
            {
                Debug.LogWarning($"[{gameObject.name}] 재생할 오디오 클립을 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            // 2. AudioSource 파라미터 세팅
            source.clip = clipToPlay;
            source.volume = finalVolume;
            source.loop = loop;
            source.spatialBlend = use3D ? 1.0f : 0.0f;
            source.minDistance = minDistance;
            source.maxDistance = maxDistance;
            source.rolloffMode = AudioRolloffMode.Linear;

            if (AudioManager.Instance != null && AudioManager.Instance.SfxGroup != null)
            {
                source.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
            }

            // 3. 재생
            source.Play();

            if (runInBackground || loop)
            {
                IsOn = true;
            }
            else
            {
                StartCoroutine(WaitForClipEndRoutine(clipToPlay.length));
            }
        }

        private void ExecuteStop(Transform target)
        {
            AudioSource source = FindExistingAudioSource(target);
            if (source == null || !source.isPlaying)
            {
                IsOn = true;
                return;
            }

            if (useFadeOut && fadeDuration > 0f)
            {
                if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = StartCoroutine(FadeOutRoutine(source));
            }
            else
            {
                source.Stop();
                if (destroyOnStop)
                {
                    Destroy(source.gameObject);
                }
                IsOn = true;
            }
        }

        private AudioSource GetOrCreateAudioSource(Transform target)
        {
            AudioSource existing = FindExistingAudioSource(target);
            if (existing != null) return existing;

            // 자식 오브젝트 생성 및 AudioSource 부착
            GameObject audioObj = new GameObject(audioChildIdentifier);
            audioObj.transform.SetParent(target, false);
            audioObj.transform.localPosition = Vector3.zero;

            return audioObj.AddComponent<AudioSource>();
        }

        private AudioSource FindExistingAudioSource(Transform target)
        {
            Transform child = target.Find(audioChildIdentifier);
            if (child != null)
            {
                return child.GetComponent<AudioSource>();
            }

            // 동일한 컴포넌트가 직접 붙어있는지 확인
            return target.GetComponent<AudioSource>();
        }

        private IEnumerator WaitForClipEndRoutine(float clipLength)
        {
            yield return new WaitForSeconds(clipLength);
            IsOn = true;
        }

        private IEnumerator FadeOutRoutine(AudioSource source)
        {
            float startVol = source.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVol, 0f, elapsed / fadeDuration);
                yield return null;
            }

            source.Stop();
            source.volume = startVol;

            if (destroyOnStop)
            {
                Destroy(source.gameObject);
            }

            _fadeCoroutine = null;
            IsOn = true;
        }

        public override void Reset()
        {
            base.Reset();
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
            IsOn = false;
        }
    }
}