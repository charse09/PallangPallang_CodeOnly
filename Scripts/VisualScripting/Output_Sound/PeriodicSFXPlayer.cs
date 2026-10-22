using System.Collections;
using UnityEngine;

/// <summary>
/// 이동체(기차, 롤러코스터 등)나 특정 환경 오브젝트에 부착하여
/// 지정된 주기(랜덤 또는 고정)마다 효과음을 자동으로 방출하는 범용 3D/2D 사운드 이미터
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PeriodicSFXPlayer : MonoBehaviour
{
    [Header("Sound Settings")]
    [Tooltip("SO_SFXTable에 등록된 사운드 키 (비워두면 아래 directClip 사용)")]
    [SerializeField] private string soundKey = "";

    [Tooltip("키를 사용하지 않고 직접 오디오 클립을 연결할 때 사용")]
    [SerializeField] private AudioClip directClip;

    [Range(0f, 1f)]
    [SerializeField] private float defaultVolume = 1f;

    [Tooltip("재생할 때마다 자연스럽게 적용할 랜덤 피치 편차 (0이면 원본 음정)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float randomPitchVariance = 0.05f;

    [Header("Interval Settings")]
    [Tooltip("활성화 시 자동으로 주기적 재생을 시작할지 여부")]
    [SerializeField] private bool autoPlay = true;

    [Tooltip("오브젝트가 켜지자마자 첫 소리를 즉시 출력할지 여부")]
    [SerializeField] private bool playImmediatelyOnEnable = false;

    [Tooltip("최소 대기 시간 (초)")]
    [SerializeField] private float minInterval = 10f;

    [Tooltip("최대 대기 시간 (초). minInterval과 같으면 고정 주기로 동작")]
    [SerializeField] private float maxInterval = 20f;

    [Header("3D Attenuation Settings")]
    [Tooltip("0 = 2D 사운드 (UI, 화면 전역) / 1 = 3D 사운드 (오브젝트 위치 추적 및 거리 감쇄)")]
    [Range(0f, 1f)]
    [SerializeField] private float spatialBlend = 1f;

    [SerializeField] private float minDistance = 5f;
    [SerializeField] private float maxDistance = 60f;

    private AudioSource audioSource;
    private Coroutine periodicCoroutine;
    private float basePitch = 1f;

    private void Awake()
    {
        InitAudioSource();
    }

    private void OnEnable()
    {
        SetupClipData();

        if (autoPlay)
        {
            StartPeriodicPlay();
        }
    }

    private void OnDisable()
    {
        StopPeriodicPlay();
    }

    private void InitAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.dopplerLevel = 0f;

        basePitch = audioSource.pitch;

        if (AudioManager.Instance != null && AudioManager.Instance.SfxGroup != null)
        {
            audioSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
        }
    }

    private void SetupClipData()
    {
        // 1. SoundManager의 SO_SFXTable에서 데이터 로드 시도
        if (!string.IsNullOrEmpty(soundKey) && SoundManager.Instance != null)
        {
            if (SoundManager.Instance.TryGetSFXData(soundKey, out var sfxData))
            {
                audioSource.clip = sfxData.clip;
                audioSource.volume = sfxData.volume;
                basePitch = sfxData.pitch > 0f ? sfxData.pitch : 1f;
                return;
            }
        }

        // 2. 직접 등록된 클립이 있을 경우 폴백
        if (directClip != null)
        {
            audioSource.clip = directClip;
            audioSource.volume = defaultVolume;
        }
    }

    /// <summary>
    /// 외부 이벤트(트리거 진입, 상호작용, 애니메이션 이벤트)에서 1회성으로 강제 출력
    /// </summary>
    public void PlayOnce()
    {
        if (audioSource == null) return;

        if (audioSource.clip == null)
        {
            SetupClipData();
        }

        if (audioSource.clip != null)
        {
            // 미세한 음정 변화를 주어 반복 재생 시 기계적인 느낌 완화
            if (randomPitchVariance > 0f)
            {
                audioSource.pitch = basePitch + Random.Range(-randomPitchVariance, randomPitchVariance);
            }
            else
            {
                audioSource.pitch = basePitch;
            }

            audioSource.PlayOneShot(audioSource.clip);
        }
    }

    /// <summary>
    /// 주기적 재생 시작
    /// </summary>
    public void StartPeriodicPlay()
    {
        StopPeriodicPlay();
        periodicCoroutine = StartCoroutine(PeriodicRoutine());
    }

    /// <summary>
    /// 주기적 재생 정지
    /// </summary>
    public void StopPeriodicPlay()
    {
        if (periodicCoroutine != null)
        {
            StopCoroutine(periodicCoroutine);
            periodicCoroutine = null;
        }
    }

    private IEnumerator PeriodicRoutine()
    {
        if (playImmediatelyOnEnable)
        {
            PlayOnce();
        }

        while (true)
        {
            float waitTime = (minInterval >= maxInterval)
                ? minInterval
                : Random.Range(minInterval, maxInterval);

            yield return new WaitForSeconds(waitTime);

            PlayOnce();
        }
    }
}