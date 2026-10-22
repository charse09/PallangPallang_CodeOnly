using UnityEngine;

/// <summary>
/// 개별 단발 사운드 설정 구조체
/// </summary>
[System.Serializable]
public struct SoundAction
{
    [Tooltip("재생할 오디오 클립")]
    public AudioClip clip;

    [Tooltip("이 사운드의 개별 볼륨 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float volume;

    [Tooltip("재생 시 무작위 피치 변화폭 (0 = 기본 피치 고정, 0.05 = 자연스러운 미세 편차)")]
    [Range(0f, 0.2f)]
    public float pitchVariance;

    public SoundAction(AudioClip clip, float volume = 1f, float pitchVariance = 0.05f)
    {
        this.clip = clip;
        this.volume = volume;
        this.pitchVariance = pitchVariance;
    }
}

/// <summary>
/// 발소리 등 여러 클립 중 하나를 무작위로 선택해 재생하는 다중 사운드 구조체
/// </summary>
[System.Serializable]
public struct SoundActionGroup
{
    [Tooltip("재생할 오디오 클립 목록 (목록 중 1개를 무작위 선택)")]
    public AudioClip[] clips;

    [Tooltip("이 사운드 그룹의 개별 볼륨 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float volume;

    [Tooltip("재생 시 무작위 피치 변화폭")]
    [Range(0f, 0.2f)]
    public float pitchVariance;

    public SoundActionGroup(AudioClip[] clips, float volume = 0.8f, float pitchVariance = 0.08f)
    {
        this.clips = clips;
        this.volume = volume;
        this.pitchVariance = pitchVariance;
    }
}

/// <summary>
/// 지속 재생되는 루프 사운드 설정 구조체
/// </summary>
[System.Serializable]
public struct LoopSoundAction
{
    [Tooltip("루프 재생할 오디오 클립")]
    public AudioClip clip;

    [Tooltip("루프 재생 볼륨 (0.0 ~ 1.0)")]
    [Range(0f, 1f)]
    public float volume;

    public LoopSoundAction(AudioClip clip, float volume = 1f)
    {
        this.clip = clip;
        this.volume = volume;
    }
}

/// <summary>
/// 플레이어의 모든 캐릭터 사운드를 구조체 기반으로 자체 AudioSource를 통해 독립 제어하는 컴포넌트입니다.
/// </summary>
[RequireComponent(typeof(PlayerLocomotion))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Audio Sources (미할당 시 런타임에 자동 생성)")]
    [Tooltip("단발성 효과음(점프, 착지, 변신 등)을 재생할 AudioSource")]
    [SerializeField] private AudioSource sfxSource;

    [Tooltip("발소리를 전용으로 재생할 AudioSource")]
    [SerializeField] private AudioSource footstepSource;

    [Tooltip("지속되는 효과음(바람 구르기, 줄타기, 종이 휘날림 등)을 재생할 루프 전용 AudioSource")]
    [SerializeField] private AudioSource loopSource;

    [Header("Audio Output Settings")]
    [Tooltip("0 = 완전한 2D 사운드 (카메라 거리와 무관하게 원본 크기로 선명하게 출력)\n1 = 완전한 3D 사운드 (거리 감쇄 적용)")]
    [Range(0f, 1f)]
    [SerializeField] private float spatialBlend = 0f;

    [Header("Movement Actions")]
    [Tooltip("발소리 클립 목록 및 설정")]
    [SerializeField] private SoundActionGroup footstepSounds = new SoundActionGroup(null, 0.75f, 0.08f);

    [Tooltip("기본 발소리 발생 주기 (초 단위, 속도가 빠를수록 자동 단축)")]
    [SerializeField] private float baseStepInterval = 0.35f;

    [Tooltip("착지 사운드")]
    [SerializeField] private SoundAction landSound = new SoundAction(null, 0.85f, 0.08f);

    [Tooltip("점프 도약 사운드")]
    [SerializeField] private SoundAction jumpSound = new SoundAction(null, 0.9f, 0.05f);

    [Tooltip("슬라이딩 시작 사운드")]
    [SerializeField] private SoundAction slideSound = new SoundAction(null, 0.8f, 0.05f);

    [Tooltip("누운 상태/종이 모드에서 일어설 때 사운드")]
    [SerializeField] private SoundAction standUpSound = new SoundAction(null, 0.7f, 0.05f);

    [Header("State Transformations")]
    [Tooltip("종이 상태로 변신할 때 사운드")]
    [SerializeField] private SoundAction paperEnterSound = new SoundAction(null, 1.0f, 0.05f);

    [Tooltip("종이 상태가 해제될 때 사운드")]
    [SerializeField] private SoundAction paperExitSound = new SoundAction(null, 1.0f, 0.05f);

    [Tooltip("슬림 상태로 변신할 때 사운드")]
    [SerializeField] private SoundAction slimEnterSound = new SoundAction(null, 1.0f, 0.05f);

    [Tooltip("슬림 상태가 해제될 때 사운드")]
    [SerializeField] private SoundAction slimExitSound = new SoundAction(null, 1.0f, 0.05f);

    [Tooltip("버티기(Brace) 동작 사운드")]
    [SerializeField] private SoundAction braceSound = new SoundAction(null, 0.85f, 0.05f);

    [Header("Gimmicks & Looping Sounds")]
    [Tooltip("종이 상태 공중 활강/휘날림 루프 사운드 (바닥에 닿으면 자동 정지)")]
    [SerializeField] private LoopSoundAction paperFlutterLoop = new LoopSoundAction(null, 0.85f);

    [Tooltip("바람 구르기 기믹 루프 사운드")]
    [SerializeField] private LoopSoundAction windRollLoop = new LoopSoundAction(null, 0.9f);

    [Tooltip("줄타기 기믹 루프 사운드")]
    [SerializeField] private LoopSoundAction ropeWalkLoop = new LoopSoundAction(null, 0.8f);

    [Tooltip("발사대 사출 시 사운드")]
    [SerializeField] private SoundAction launchSound = new SoundAction(null, 1.0f, 0.05f);

    [Tooltip("리스폰 사운드")]
    [SerializeField] private SoundAction respawnSound = new SoundAction(null, 1.0f, 0.05f);

    private PlayerLocomotion locomotion;
    private Rigidbody rb;

    // 상태 추적 플래그
    private bool prevGrounded;
    private bool prevPaperState;
    private bool prevSlimState;
    private bool prevBraceState;
    private bool prevSliding;
    private bool prevWindRolling;
    private bool prevRidingRope;
    private bool prevRespawning;

    private float footstepTimer;

    private void Awake()
    {
        locomotion = GetComponent<PlayerLocomotion>();
        rb = GetComponent<Rigidbody>();

        InitAudioSources();
    }

    private void Start()
    {
        SetupMixerGroup();

        // 초기 상태 백업
        prevGrounded = locomotion.IsGrounded;
        prevPaperState = locomotion.GetPaperState();
        prevSlimState = locomotion.GetSlimState();
        prevBraceState = locomotion.IsBracing;
        prevSliding = locomotion.IsSliding;
        prevWindRolling = locomotion.isWindRolling;
        prevRidingRope = locomotion.isRidingRope;
        prevRespawning = locomotion.isRespawning;
    }

    private void Update()
    {
        if (locomotion == null) return;

        HandleFootsteps();
        HandleStateChanges();
        HandleLoopingSounds();
    }

    private void InitAudioSources()
    {
        if (sfxSource == null) sfxSource = CreateChildAudioSource("SFX_Source");
        if (footstepSource == null) footstepSource = CreateChildAudioSource("Footstep_Source");
        if (loopSource == null) loopSource = CreateChildAudioSource("Loop_Source");

        ConfigureSource(sfxSource);
        ConfigureSource(footstepSource);
        ConfigureSource(loopSource);

        loopSource.loop = true;
    }

    private AudioSource CreateChildAudioSource(string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(transform);
        child.transform.localPosition = Vector3.zero;
        return child.AddComponent<AudioSource>();
    }

    private void ConfigureSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2.0f;
        source.maxDistance = 40.0f;
        source.dopplerLevel = 0f;
    }

    private void SetupMixerGroup()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.SfxGroup != null)
        {
            sfxSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
            footstepSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
            loopSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
        }
    }

    #region --- 발소리 처리 ---

    private void HandleFootsteps()
    {
        bool isMovingOnGround = locomotion.IsGrounded &&
                                !locomotion.IsSliding &&
                                !locomotion.isWindRolling &&
                                !locomotion.isRidingRope &&
                                locomotion.HasMoveInput &&
                                rb.linearVelocity.sqrMagnitude > 0.5f;

        if (isMovingOnGround)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f)
            {
                PlayGroupAction(footstepSounds, footstepSource);

                float currentSpeed = rb.linearVelocity.magnitude;
                float speedRatio = Mathf.Max(currentSpeed / 16f, 0.5f);
                footstepTimer = baseStepInterval / speedRatio;
            }
        }
        else
        {
            footstepTimer = 0f;
        }
    }

    #endregion

    #region --- 단발성 상태 변화 사운드 ---

    private void HandleStateChanges()
    {
        bool currentGrounded = locomotion.IsGrounded;
        bool currentPaper = locomotion.GetPaperState();
        bool currentSlim = locomotion.GetSlimState();
        bool currentBrace = locomotion.IsBracing;
        bool currentSliding = locomotion.IsSliding;
        bool currentRespawning = locomotion.isRespawning;

        // 1. 착지
        if (!prevGrounded && currentGrounded)
        {
            PlayAction(landSound, footstepSource);
        }

        // 2. 점프
        if (prevGrounded && !currentGrounded && rb.linearVelocity.y > 1f)
        {
            PlayAction(jumpSound, sfxSource);
        }

        // 3. 일어서기 (누운 종이 상태에서 복귀할 때)
        if (prevPaperState && !currentPaper)
        {
            PlayAction(standUpSound, sfxSource);
        }

        // 4. 종이 변신 진입
        if (!prevPaperState && currentPaper)
        {
            PlayAction(paperEnterSound, sfxSource);
        }

        // 5. 슬라이딩 시작
        if (!prevSliding && currentSliding)
        {
            PlayAction(slideSound, sfxSource);
        }

        // 6. 슬림 상태 진입 / 해제
        if (prevSlimState != currentSlim)
        {
            PlayAction(currentSlim ? slimEnterSound : slimExitSound, sfxSource);
        }

        // 7. 버티기
        if (!prevBraceState && currentBrace && !currentSliding)
        {
            PlayAction(braceSound, sfxSource);
        }

        // 8. 리스폰
        if (!prevRespawning && currentRespawning)
        {
            PlayAction(respawnSound, sfxSource);
        }

        prevGrounded = currentGrounded;
        prevPaperState = currentPaper;
        prevSlimState = currentSlim;
        prevBraceState = currentBrace;
        prevSliding = currentSliding;
        prevRespawning = currentRespawning;
    }

    public void PlayAction(SoundAction action, AudioSource targetSource = null)
    {
        if (action.clip == null) return;

        AudioSource source = targetSource != null ? targetSource : sfxSource;
        if (source == null) return;

        source.pitch = 1.0f + Random.Range(-action.pitchVariance, action.pitchVariance);
        source.PlayOneShot(action.clip, action.volume);
    }

    public void PlayGroupAction(SoundActionGroup group, AudioSource targetSource = null)
    {
        if (group.clips == null || group.clips.Length == 0) return;

        AudioClip clip = group.clips[Random.Range(0, group.clips.Length)];
        if (clip == null) return;

        AudioSource source = targetSource != null ? targetSource : footstepSource;
        if (source == null) return;

        source.pitch = 1.0f + Random.Range(-group.pitchVariance, group.pitchVariance);
        source.PlayOneShot(clip, group.volume);
    }

    public void PlayLaunchSound()
    {
        PlayAction(launchSound, sfxSource);
    }

    #endregion

    #region --- 루프 지속 사운드 ---

    private void HandleLoopingSounds()
    {
        bool currentWindRolling = locomotion.isWindRolling;
        bool currentRidingRope = locomotion.isRidingRope;

        // 종이 상태이면서 공중에 떠 있을 때만 활성화 (바닥에 닿으면 false)
        bool currentPaperFloating = locomotion.GetPaperState() && !locomotion.IsGrounded;

        if (currentWindRolling)
        {
            PlayLoopSound(windRollLoop);
        }
        else if (currentRidingRope)
        {
            PlayLoopSound(ropeWalkLoop);
        }
        else if (currentPaperFloating)
        {
            PlayLoopSound(paperFlutterLoop);
        }
        else
        {
            StopLoopSound();
        }

        prevWindRolling = currentWindRolling;
        prevRidingRope = currentRidingRope;
    }

    private void PlayLoopSound(LoopSoundAction action)
    {
        if (action.clip == null || loopSource == null) return;

        if (loopSource.clip != action.clip || !loopSource.isPlaying)
        {
            loopSource.clip = action.clip;
            loopSource.volume = action.volume;
            loopSource.Play();
        }
    }

    private void StopLoopSound()
    {
        if (loopSource != null && loopSource.isPlaying)
        {
            loopSource.Stop();
            loopSource.clip = null;
        }
    }

    #endregion
}