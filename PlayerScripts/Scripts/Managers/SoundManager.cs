using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SFX, BGM, 환경음 및 Voice(더빙 음성) 스크립터블 오브젝트 테이블을 참조하여 
/// 오디오 재생, 풀링, 페이드 연출, A/B 듀얼 채널 크로스페이드 및 BGM 덕킹을 전담합니다.
/// </summary>
public class SoundManager : ManagerBase<SoundManager>
{
    public static SoundManager Instance => instance;

    [Header("Sound Data Tables")]
    [Tooltip("효과음 데이터를 관리하는 SFX 테이블 에셋")]
    [SerializeField] private SO_SFXTable sfxTable;

    [Tooltip("배경음악 및 환경음 데이터를 관리하는 BGM 테이블 에셋")]
    [SerializeField] private SO_BGMTable bgmTable;

    [Tooltip("대사 더빙 음성 데이터를 관리하는 Voice 테이블 에셋")]
    [SerializeField] private SO_VoiceTable voiceTable;

    [Header("Main BGM Dual Channel Settings (A/B Crossfade)")]
    private AudioSource bgmSourceA;
    private AudioSource bgmSourceB;
    private AudioSource activeBgmSource;
    private Coroutine bgmCrossfadeCoroutine;
    private Coroutine bgmDuckCoroutine;
    private Coroutine bgmFadeOutCoroutine; // ★ 추가: 페이드아웃 코루틴 참조 보관
    private float baseBgmVolume = 1f;
    private float externalBgmDuckRatio = 1f;

    // ★ 추가: 메뉴 일시정지 전 실제 재생 중이었는지 기록하는 플래그
    private bool isBgmAPausedByMenu = false;
    private bool isBgmBPausedByMenu = false;
    private bool isSubBgmPausedByMenu = false;

    [Header("Sub BGM Channel Settings (컷씬 / 이벤트 연출용)")]
    private AudioSource subBgmSource;
    private Coroutine subBgmFadeCoroutine;
    private Coroutine subBgmDuckCoroutine;
    private float baseSubBgmVolume = 1f;
    private float externalSubBgmDuckRatio = 1f;

    private float voiceDuckRatio = 1f;

    public bool IsMainBgmDucked => externalBgmDuckRatio < 0.999f || voiceDuckRatio < 0.999f;
    public bool IsSubBgmDucked => externalSubBgmDuckRatio < 0.999f || voiceDuckRatio < 0.999f;

    [Header("Ambience Multi-Channel Settings (다중 환경음 / 배경 소음)")]
    private Transform ambienceGroupTransform;
    private Dictionary<string, AudioSource> activeAmbienceDict = new Dictionary<string, AudioSource>();
    private Dictionary<string, Coroutine> ambienceFadeCoroutines = new Dictionary<string, Coroutine>();

    [Header("SFX Pooling Settings")]
    [Tooltip("미리 생성해 둘 SFX용 AudioSource 개수")]
    [SerializeField] private int sfxPoolSize = 20;
    private Queue<AudioSource> sfxPool = new Queue<AudioSource>();
    private Transform sfxGroupTransform;

    [Header("Voice Channel Settings (더빙 음성 관리)")]
    private AudioSource voiceSource;
    private Coroutine voiceDurationCoroutine;
    private bool isVoiceDuckingActive = false;
    private string currentPlayingVoiceKey = "";

    private bool voiceAffectedByTimeScale = true;
    private bool isVoicePausedByMenu = false;

    private Dictionary<string, SFXData> sfxDictionary = new Dictionary<string, SFXData>();
    private Dictionary<string, BGMData> bgmDictionary = new Dictionary<string, BGMData>();

    protected override void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        transform.SetParent(null);
        base.Awake();

        if (instance == this || instance == null)
        {
            DontDestroyOnLoad(gameObject);
        }

        InitDictionaries();
        InitDualBGM();
        InitSubBGM();
        InitAmbienceGroup();
        InitSFXPool();
        InitVoiceChannel();
    }

    private void Start()
    {
        if (AudioManager.Instance != null)
        {
            if (bgmSourceA != null) bgmSourceA.outputAudioMixerGroup = AudioManager.Instance.BgmGroup;
            if (bgmSourceB != null) bgmSourceB.outputAudioMixerGroup = AudioManager.Instance.BgmGroup;
            if (subBgmSource != null) subBgmSource.outputAudioMixerGroup = AudioManager.Instance.BgmGroup;

            foreach (var ambSource in activeAmbienceDict.Values)
            {
                ambSource.outputAudioMixerGroup = AudioManager.Instance.BgmGroup;
            }

            foreach (var source in sfxPool)
            {
                source.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
            }

            if (voiceSource != null)
            {
                voiceSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
            }
        }
    }

    #region --- 테이블 초기화 ---

    private void InitDictionaries()
    {
        sfxDictionary.Clear();
        if (sfxTable != null && sfxTable.SFXDataList != null)
        {
            foreach (var data in sfxTable.SFXDataList)
            {
                if (string.IsNullOrEmpty(data.soundKey)) continue;

                if (!sfxDictionary.ContainsKey(data.soundKey))
                    sfxDictionary.Add(data.soundKey, data);
                else
                    Debug.LogWarning($"[SoundManager] 중복된 SFX 키: {data.soundKey}");
            }
        }

        bgmDictionary.Clear();
        if (bgmTable != null && bgmTable.BGMDataList != null)
        {
            foreach (var data in bgmTable.BGMDataList)
            {
                if (string.IsNullOrEmpty(data.bgmKey)) continue;

                if (!bgmDictionary.ContainsKey(data.bgmKey))
                    bgmDictionary.Add(data.bgmKey, data);
                else
                    Debug.LogWarning($"[SoundManager] 중복된 BGM/환경음 키: {data.bgmKey}");
            }
        }

        if (voiceTable != null)
        {
            voiceTable.Initialize();
        }
    }

    #endregion

    #region --- SFX 풀링 및 재생 ---

    private void InitSFXPool()
    {
        GameObject groupObj = new GameObject("SFX_PoolGroup");
        groupObj.transform.SetParent(this.transform);
        sfxGroupTransform = groupObj.transform;

        for (int i = 0; i < sfxPoolSize; i++)
        {
            CreateNewSFXInstance();
        }
    }

    //private AudioSource CreateNewSFXInstance()
    //{
    //    GameObject obj = new GameObject("SFX_PooledSource");
    //    obj.transform.SetParent(sfxGroupTransform);

    //    AudioSource source = obj.AddComponent<AudioSource>();
    //    source.playOnAwake = false;
    //    source.rolloffMode = AudioRolloffMode.Linear;
    //    source.minDistance = 5.0f;
    //    source.maxDistance = 50.0f;
    //    source.dopplerLevel = 0f;

    //    if (AudioManager.Instance != null)
    //    {
    //        source.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
    //    }

    //    obj.SetActive(false);
    //    sfxPool.Enqueue(source);
    //    return source;
    //}
    private AudioSource CreateNewSFXInstance()
    {
        GameObject obj = new GameObject("SFX_PooledSource");
        obj.transform.SetParent(sfxGroupTransform);

        AudioSource source = obj.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.rolloffMode = AudioRolloffMode.Linear; // 이제 정상 인식됨
        source.minDistance = 5.0f;
        source.maxDistance = 50.0f;
        source.dopplerLevel = 0f;

        if (AudioManager.Instance != null)
        {
            source.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
        }

        obj.SetActive(false);
        sfxPool.Enqueue(source);
        return source;
    }

    // private AudioRolloffMode AudioRolloffMode() => AudioRolloffMode.Linear;

    public AudioSource PlaySFX(string soundKey, Vector3 position = default)
    {
        if (!sfxDictionary.TryGetValue(soundKey, out SFXData data))
        {
            Debug.LogWarning($"[SoundManager] SFX 키 '{soundKey}'를 찾을 수 없습니다.");
            return null;
        }

        if (data.clip == null) return null;

        if (sfxPool.Count == 0) CreateNewSFXInstance();

        AudioSource source = sfxPool.Dequeue();
        source.transform.position = position;
        source.clip = data.clip;
        source.volume = data.volume;
        source.pitch = (data.randomPitchVariance > 0f) ? data.pitch + Random.Range(-data.randomPitchVariance, data.randomPitchVariance) : data.pitch;
        source.spatialBlend = data.is3D ? 1f : 0f;

        source.gameObject.SetActive(true);
        source.Play();

        StartCoroutine(AutoReturnToPoolRoutine(source, source.clip.length));
        return source;
    }

    private IEnumerator AutoReturnToPoolRoutine(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool(source);
    }

    private void ReturnToPool(AudioSource source)
    {
        if (source == null) return;
        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        source.transform.SetParent(sfxGroupTransform);
        sfxPool.Enqueue(source);
    }

    public bool TryGetSFXData(string key, out SFXData data)
    {
        if (sfxDictionary != null && sfxDictionary.TryGetValue(key, out data))
        {
            return true;
        }
        data = default;
        return false;
    }

    /// <summary>
    /// 지속적으로 재생되어야 하는 루프 효과음을 풀에서 꺼내어 재생합니다.
    /// 반환된 AudioSource는 정지 시 StopLoopSFX로 회수해야 합니다.
    /// </summary>
    public AudioSource PlayLoopSFX(string soundKey, Vector3 position = default, Transform parent = null)
    {
        if (!sfxDictionary.TryGetValue(soundKey, out SFXData data))
        {
            Debug.LogWarning($"[SoundManager] SFX 키 '{soundKey}'를 찾을 수 없습니다.");
            return null;
        }

        if (data.clip == null) return null;

        if (sfxPool.Count == 0) CreateNewSFXInstance();

        AudioSource source = sfxPool.Dequeue();
        source.transform.position = position;
        if (parent != null) source.transform.SetParent(parent);

        source.clip = data.clip;
        source.volume = data.volume;
        source.pitch = data.pitch;
        source.spatialBlend = data.is3D ? 1f : 0f;
        source.loop = true;

        source.gameObject.SetActive(true);
        source.Play();

        return source;
    }

    /// <summary>
    /// 재생 중인 루프 효과음을 정지하고 풀로 반환합니다.
    /// </summary>
    public void StopLoopSFX(AudioSource source)
    {
        if (source == null) return;
        ReturnToPool(source);
    }

    #endregion

    #region --- Voice 제어 로직 ---

    public bool IsVoicePlaying => voiceSource != null && (voiceSource.isPlaying || isVoicePausedByMenu);

    private void InitVoiceChannel()
    {
        GameObject voiceObj = new GameObject("Voice_Source");
        voiceObj.transform.SetParent(this.transform);

        voiceSource = voiceObj.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.rolloffMode = AudioRolloffMode.Linear;
        voiceSource.minDistance = 3.0f;
        voiceSource.maxDistance = 30.0f;
        voiceSource.dopplerLevel = 0f;
    }

    //public void SetGamePauseState(bool isPaused)
    //{
    //    if (voiceSource == null || voiceSource.clip == null) return;

    //    if (voiceAffectedByTimeScale)
    //    {
    //        if (isPaused)
    //        {
    //            if (voiceSource.isPlaying)
    //            {
    //                voiceSource.Pause();
    //                isVoicePausedByMenu = true;
    //            }
    //        }
    //        else
    //        {
    //            if (isVoicePausedByMenu)
    //            {
    //                voiceSource.UnPause();
    //                isVoicePausedByMenu = false;
    //            }
    //            voiceSource.pitch = 1f;
    //        }
    //    }
    //}
    /// <summary>
    /// MenuManager 등에서 메뉴가 열리거나(일시정지) 닫힐 때(재개) 호출되어
    /// Voice, 메인 BGM, 서브 BGM의 일시정지 및 복원을 일괄 처리합니다.
    /// </summary>
    public void SetGamePauseState(bool isPaused)
    {
        // ==========================================
        // 1. Voice(대사 음성) 일시정지 및 복원
        // ==========================================
        if (voiceSource != null && voiceSource.clip != null && voiceAffectedByTimeScale)
        {
            if (isPaused)
            {
                if (voiceSource.isPlaying)
                {
                    voiceSource.Pause();
                    isVoicePausedByMenu = true;
                }
            }
            else
            {
                if (isVoicePausedByMenu)
                {
                    voiceSource.UnPause();
                    isVoicePausedByMenu = false;
                }
                voiceSource.pitch = 1f;
            }
        }

        // ==========================================
        // 2. Main BGM (A/B 듀얼 채널) 일시정지 및 복원
        // ==========================================
        if (isPaused)
        {
            // 메뉴가 열릴 때: 현재 재생 중이던 채널만 일시정지하고 플래그 기록
            if (bgmSourceA != null && bgmSourceA.isPlaying)
            {
                bgmSourceA.Pause();
                isBgmAPausedByMenu = true;
            }
            if (bgmSourceB != null && bgmSourceB.isPlaying)
            {
                bgmSourceB.Pause();
                isBgmBPausedByMenu = true;
            }
        }
        else
        {
            // 메뉴가 닫힐 때: 메뉴 때문에 멈췄던 채널만 원래 위치에서 재생 재개
            if (bgmSourceA != null && isBgmAPausedByMenu)
            {
                bgmSourceA.UnPause();
                isBgmAPausedByMenu = false;
            }
            if (bgmSourceB != null && isBgmBPausedByMenu)
            {
                bgmSourceB.UnPause();
                isBgmBPausedByMenu = false;
            }
        }

        // ==========================================
        // 3. Sub BGM (컷씬/이벤트 전용 채널) 일시정지 및 복원
        // ==========================================
        if (isPaused)
        {
            if (subBgmSource != null && subBgmSource.isPlaying)
            {
                subBgmSource.Pause();
                isSubBgmPausedByMenu = true;
            }
        }
        else
        {
            if (subBgmSource != null && isSubBgmPausedByMenu)
            {
                subBgmSource.UnPause();
                isSubBgmPausedByMenu = false;
            }
        }
    }

    public float PlayVoice(AudioClip clip, Vector3 position = default, float volume = 1f, bool is3D = false, bool enableDucking = true, bool affectedByTimeScale = true)
    {
        if (clip == null) return 0f;

        StopVoice(restoreDucking: !enableDucking);

        currentPlayingVoiceKey = clip.name;
        voiceAffectedByTimeScale = affectedByTimeScale;
        isVoicePausedByMenu = false;

        voiceSource.transform.position = position;
        voiceSource.clip = clip;
        voiceSource.volume = volume;
        voiceSource.spatialBlend = is3D ? 1f : 0f;
        voiceSource.pitch = 1f;

        if (enableDucking)
        {
            isVoiceDuckingActive = true;
            SetVoiceDucking(true, 0.25f, 0.4f);
        }

        voiceSource.Play();

        if (affectedByTimeScale && Time.timeScale <= 0f)
        {
            voiceSource.Pause();
            isVoicePausedByMenu = true;
        }

        voiceDurationCoroutine = StartCoroutine(Co_AutoRestoreVoiceDucking(clip.length, affectedByTimeScale));
        return clip.length;
    }

    public float PlayVoice(string voiceKey, Vector3 position = default, bool affectedByTimeScale = true)
    {
        if (voiceTable == null)
        {
            Debug.LogWarning("[SoundManager] VoiceTable이 할당되지 않았습니다.");
            return 0f;
        }

        VoiceData data = voiceTable.GetVoiceData(voiceKey);
        if (data == null || data.clip == null) return 0f;

        StopVoice(restoreDucking: !data.enableDucking);

        currentPlayingVoiceKey = voiceKey;
        voiceAffectedByTimeScale = affectedByTimeScale;
        isVoicePausedByMenu = false;

        voiceSource.transform.position = position;
        voiceSource.clip = data.clip;
        voiceSource.volume = data.volume;
        voiceSource.spatialBlend = data.is3D ? 1f : 0f;
        voiceSource.pitch = 1f;

        if (data.enableDucking)
        {
            isVoiceDuckingActive = true;
            SetVoiceDucking(true, 0.25f, 0.4f);
        }

        voiceSource.Play();

        if (affectedByTimeScale && Time.timeScale <= 0f)
        {
            voiceSource.Pause();
            isVoicePausedByMenu = true;
        }

        voiceDurationCoroutine = StartCoroutine(Co_AutoRestoreVoiceDucking(data.clip.length, affectedByTimeScale));
        return data.clip.length;
    }

    public void StopVoice(string voiceKey = null, bool restoreDucking = true)
    {
        if (!string.IsNullOrEmpty(voiceKey) && currentPlayingVoiceKey != voiceKey)
        {
            return;
        }

        currentPlayingVoiceKey = "";
        isVoicePausedByMenu = false;

        if (voiceDurationCoroutine != null)
        {
            StopCoroutine(voiceDurationCoroutine);
            voiceDurationCoroutine = null;
        }

        if (voiceSource != null && (voiceSource.isPlaying || isVoicePausedByMenu))
        {
            voiceSource.Stop();
            voiceSource.clip = null;
        }

        if (isVoiceDuckingActive)
        {
            isVoiceDuckingActive = false;
            if (restoreDucking)
            {
                SetVoiceDucking(false, 1.0f, 0.5f);
            }
        }
    }

    private IEnumerator Co_AutoRestoreVoiceDucking(float delay, bool affectedByTimeScale)
    {
        if (affectedByTimeScale)
            yield return new WaitForSeconds(delay);
        else
            yield return new WaitForSecondsRealtime(delay);

        if (isVoiceDuckingActive)
        {
            isVoiceDuckingActive = false;
            SetVoiceDucking(false, 1.0f, 0.5f);
        }
        voiceDurationCoroutine = null;
    }

    private void SetVoiceDucking(bool active, float duckRatio = 0.25f, float duration = 0.4f)
    {
        voiceDuckRatio = active ? Mathf.Clamp01(duckRatio) : 1.0f;
        ApplyMainBgmVolumeTransition(duration);
        ApplySubBgmVolumeTransition(duration);
    }

    #endregion

    #region --- Main BGM 제어 로직 ---

    private void InitDualBGM()
    {
        GameObject bgmRoot = new GameObject("BGM_DualChannels");
        bgmRoot.transform.SetParent(this.transform);

        GameObject objA = new GameObject("BGM_Source_A");
        objA.transform.SetParent(bgmRoot.transform);
        bgmSourceA = objA.AddComponent<AudioSource>();
        bgmSourceA.playOnAwake = false;

        GameObject objB = new GameObject("BGM_Source_B");
        objB.transform.SetParent(bgmRoot.transform);
        bgmSourceB = objB.AddComponent<AudioSource>();
        bgmSourceB.playOnAwake = false;

        activeBgmSource = bgmSourceA;
    }

    public float PlayBGM(string bgmKey, bool useFadeIn = true, float? fadeDurationOverride = null, bool restartIfPlaying = false)
    {
        if (!bgmDictionary.TryGetValue(bgmKey, out BGMData data))
        {
            Debug.LogWarning($"[SoundManager] BGM 키 '{bgmKey}'를 찾을 수 없습니다.");
            return 0f;
        }

        if (data.clip == null) return 0f;

        float fadeDuration = fadeDurationOverride ?? data.defaultFadeDuration;
        bool isFading = PlayBGMInternal(data.clip, data.volume, useFadeIn, fadeDuration, restartIfPlaying, data.loop);
        return isFading ? fadeDuration : 0f;
    }

    public void PlayBGM(AudioClip clip, float targetVolume = 1f, bool useFadeIn = false, float fadeDuration = 1.5f, bool restartIfPlaying = false, bool loop = true)
    {
        if (clip == null) return;
        PlayBGMInternal(clip, targetVolume, useFadeIn, fadeDuration, restartIfPlaying, loop);
    }

    private bool PlayBGMInternal(AudioClip clip, float targetVolume, bool useFadeIn, float fadeDuration, bool restartIfPlaying, bool loop)
    {
        baseBgmVolume = targetVolume;
        externalBgmDuckRatio = 1f;
        if (!isVoiceDuckingActive) voiceDuckRatio = 1f;

        // 진행 중이던 페이드아웃 및 덕킹 루틴 강제 취소
        StopBgmFadeOut();
        if (bgmDuckCoroutine != null)
        {
            StopCoroutine(bgmDuckCoroutine);
            bgmDuckCoroutine = null;
        }

        float calculatedTargetVolume = baseBgmVolume * externalBgmDuckRatio * voiceDuckRatio;

        // 동일한 곡이 이미 재생 중인 경우
        if (!restartIfPlaying && activeBgmSource != null && activeBgmSource.isPlaying && activeBgmSource.clip != null && activeBgmSource.clip == clip)
        {
            StopBgmCrossfade();
            activeBgmSource.volume = calculatedTargetVolume;
            return false;
        }

        AudioSource oldSource = activeBgmSource;
        AudioSource newSource = (activeBgmSource == bgmSourceA) ? bgmSourceB : bgmSourceA;

        StopBgmCrossfade();
        activeBgmSource = newSource;

        if (useFadeIn && fadeDuration > 0f)
        {
            bgmCrossfadeCoroutine = StartCoroutine(CrossfadeRoutine(oldSource, newSource, clip, calculatedTargetVolume, fadeDuration, loop));
            return true;
        }
        else
        {
            if (oldSource != null)
            {
                oldSource.Stop();
                oldSource.clip = null;
            }

            newSource.clip = clip;
            newSource.loop = loop;
            newSource.volume = calculatedTargetVolume;
            newSource.Play();
            return false;
        }
    }

    private IEnumerator CrossfadeRoutine(AudioSource fromSource, AudioSource toSource, AudioClip newClip, float targetVolume, float duration, bool loop)
    {
        toSource.clip = newClip;
        toSource.loop = loop;
        toSource.volume = 0f;
        toSource.Play();

        float fromStartVol = (fromSource != null && fromSource.isPlaying) ? fromSource.volume : 0f;
        float timer = 0f;

        while (timer < duration)
        {
            // 타임스케일 0에서도 페이드가 정상적으로 흐르도록 unscaledDeltaTime 사용
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);

            if (toSource != null)
                toSource.volume = Mathf.Lerp(0f, targetVolume, t);

            if (fromSource != null && fromSource.isPlaying)
                fromSource.volume = Mathf.Lerp(fromStartVol, 0f, t);

            yield return null;
        }

        if (toSource != null) toSource.volume = targetVolume;

        if (fromSource != null)
        {
            fromSource.Stop();
            fromSource.clip = null;
        }

        bgmCrossfadeCoroutine = null;
    }

    public void StopBGM(bool useFadeOut = true, float fadeDuration = 1.0f)
    {
        externalBgmDuckRatio = 1f;

        if (bgmDuckCoroutine != null)
        {
            StopCoroutine(bgmDuckCoroutine);
            bgmDuckCoroutine = null;
        }

        StopBgmCrossfade();
        StopBgmFadeOut();

        if (useFadeOut && fadeDuration > 0f)
        {
            bgmFadeOutCoroutine = StartCoroutine(FadeOutBothRoutine(fadeDuration));
        }
        else
        {
            StopBGMImmediately();
        }
    }

    /// <summary>
    /// 메인 BGM 채널 2개를 즉시 정지하고 클립 참조를 해제합니다.
    /// </summary>
    public void StopBGMImmediately()
    {
        StopBgmCrossfade();
        StopBgmFadeOut();

        if (bgmSourceA != null)
        {
            bgmSourceA.Stop();
            bgmSourceA.clip = null;
            bgmSourceA.volume = 0f;
        }

        if (bgmSourceB != null)
        {
            bgmSourceB.Stop();
            bgmSourceB.clip = null;
            bgmSourceB.volume = 0f;
        }

        activeBgmSource = bgmSourceA;
    }

    private void StopBgmFadeOut()
    {
        if (bgmFadeOutCoroutine != null)
        {
            StopCoroutine(bgmFadeOutCoroutine);
            bgmFadeOutCoroutine = null;
        }
    }

    private IEnumerator FadeOutBothRoutine(float duration)
    {
        float volA = (bgmSourceA != null) ? bgmSourceA.volume : 0f;
        float volB = (bgmSourceB != null) ? bgmSourceB.volume : 0f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);

            if (bgmSourceA != null && bgmSourceA.isPlaying) bgmSourceA.volume = Mathf.Lerp(volA, 0f, t);
            if (bgmSourceB != null && bgmSourceB.isPlaying) bgmSourceB.volume = Mathf.Lerp(volB, 0f, t);

            yield return null;
        }

        StopBGMImmediately();
        bgmFadeOutCoroutine = null;
    }

    private void StopBgmCrossfade()
    {
        if (bgmCrossfadeCoroutine != null)
        {
            StopCoroutine(bgmCrossfadeCoroutine);
            bgmCrossfadeCoroutine = null;
        }

        AudioSource inactiveSource = (activeBgmSource == bgmSourceA) ? bgmSourceB : bgmSourceA;
        if (inactiveSource != null && inactiveSource.isPlaying)
        {
            inactiveSource.Stop();
            inactiveSource.clip = null;
        }
    }

    public void DuckMainBGM(float duckRatio = 0.2f, float duration = 1.0f)
    {
        externalBgmDuckRatio = Mathf.Clamp01(duckRatio);
        ApplyMainBgmVolumeTransition(duration);
    }

    public void RestoreMainBGM(float duration = 1.0f)
    {
        externalBgmDuckRatio = 1.0f;
        ApplyMainBgmVolumeTransition(duration);
    }

    private void ApplyMainBgmVolumeTransition(float duration)
    {
        if (activeBgmSource == null || !activeBgmSource.isPlaying) return;

        float targetVolume = baseBgmVolume * externalBgmDuckRatio * voiceDuckRatio;

        if (bgmDuckCoroutine != null) StopCoroutine(bgmDuckCoroutine);
        bgmDuckCoroutine = StartCoroutine(MainBgmDuckTransitionRoutine(targetVolume, duration));
    }

    private IEnumerator MainBgmDuckTransitionRoutine(float targetVolume, float duration)
    {
        if (duration <= 0f)
        {
            if (activeBgmSource != null) activeBgmSource.volume = targetVolume;
            bgmDuckCoroutine = null;
            yield break;
        }

        float startVol = activeBgmSource != null ? activeBgmSource.volume : targetVolume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            if (activeBgmSource != null)
                activeBgmSource.volume = Mathf.Lerp(startVol, targetVolume, Mathf.Clamp01(timer / duration));
            yield return null;
        }

        if (activeBgmSource != null) activeBgmSource.volume = targetVolume;
        bgmDuckCoroutine = null;
    }

    #endregion

    #region --- Sub BGM 제어 로직 ---

    private void InitSubBGM()
    {
        GameObject subBgmObj = new GameObject("SubBGM_Source");
        subBgmObj.transform.SetParent(this.transform);

        subBgmSource = subBgmObj.AddComponent<AudioSource>();
        subBgmSource.playOnAwake = false;
    }

    public float PlaySubBGM(string bgmKey, bool useFadeIn = true, float? fadeDurationOverride = null, bool restartIfPlaying = false)
    {
        if (!bgmDictionary.TryGetValue(bgmKey, out BGMData data))
        {
            Debug.LogWarning($"[SoundManager] 서브 BGM 키 '{bgmKey}'를 찾을 수 없습니다.");
            return 0f;
        }

        if (data.clip == null) return 0f;

        float fadeDuration = fadeDurationOverride ?? data.defaultFadeDuration;
        subBgmSource.loop = data.loop;
        baseSubBgmVolume = data.volume;
        externalSubBgmDuckRatio = 1f;

        if (subBgmDuckCoroutine != null)
        {
            StopCoroutine(subBgmDuckCoroutine);
            subBgmDuckCoroutine = null;
        }

        float calculatedTargetVolume = baseSubBgmVolume * externalSubBgmDuckRatio * voiceDuckRatio;

        if (!restartIfPlaying && subBgmSource.isPlaying && subBgmSource.clip != null && subBgmSource.clip == data.clip)
        {
            StopSubBgmFade();
            subBgmSource.volume = calculatedTargetVolume;
            return 0f;
        }

        subBgmSource.clip = data.clip;

        if (useFadeIn && fadeDuration > 0f)
        {
            subBgmSource.Play();
            StartSubBgmFadeIn(calculatedTargetVolume, fadeDuration);
            return fadeDuration;
        }
        else
        {
            StopSubBgmFade();
            subBgmSource.volume = calculatedTargetVolume;
            subBgmSource.Play();
            return 0f;
        }
    }

    public void StopSubBGM(bool useFadeOut = true, float fadeDuration = 1.0f)
    {
        if (!subBgmSource.isPlaying) return;

        externalSubBgmDuckRatio = 1f;

        if (subBgmDuckCoroutine != null)
        {
            StopCoroutine(subBgmDuckCoroutine);
            subBgmDuckCoroutine = null;
        }

        if (useFadeOut)
        {
            StopSubBgmFade();
            subBgmFadeCoroutine = StartCoroutine(SubBgmFadeOutRoutine(fadeDuration));
        }
        else
        {
            StopSubBGMImmediately();
        }
    }

    public void StopSubBGMImmediately()
    {
        StopSubBgmFade();
        if (subBgmSource != null)
        {
            subBgmSource.Stop();
            subBgmSource.clip = null;
            subBgmSource.volume = 0f;
        }
    }

    public void DuckSubBGM(float duckRatio = 0.2f, float duration = 1.0f)
    {
        externalSubBgmDuckRatio = Mathf.Clamp01(duckRatio);
        ApplySubBgmVolumeTransition(duration);
    }

    public void RestoreSubBGM(float duration = 1.0f)
    {
        externalSubBgmDuckRatio = 1.0f;
        ApplySubBgmVolumeTransition(duration);
    }

    public void DuckAllBGM(float duckRatio = 0.2f, float duration = 1.0f)
    {
        DuckMainBGM(duckRatio, duration);
        DuckSubBGM(duckRatio, duration);
    }

    public void RestoreAllBGM(float duration = 1.0f)
    {
        RestoreMainBGM(duration);
        RestoreSubBGM(duration);
    }

    private void ApplySubBgmVolumeTransition(float duration)
    {
        if (subBgmSource == null || !subBgmSource.isPlaying) return;

        float targetVolume = baseSubBgmVolume * externalSubBgmDuckRatio * voiceDuckRatio;

        if (subBgmDuckCoroutine != null) StopCoroutine(subBgmDuckCoroutine);
        subBgmDuckCoroutine = StartCoroutine(SubBgmDuckTransitionRoutine(targetVolume, duration));
    }

    private IEnumerator SubBgmDuckTransitionRoutine(float targetVolume, float duration)
    {
        if (duration <= 0f)
        {
            if (subBgmSource != null) subBgmSource.volume = targetVolume;
            subBgmDuckCoroutine = null;
            yield break;
        }

        float startVol = subBgmSource != null ? subBgmSource.volume : targetVolume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            if (subBgmSource != null)
                subBgmSource.volume = Mathf.Lerp(startVol, targetVolume, Mathf.Clamp01(timer / duration));
            yield return null;
        }

        if (subBgmSource != null) subBgmSource.volume = targetVolume;
        subBgmDuckCoroutine = null;
    }

    private void StartSubBgmFadeIn(float targetVolume, float duration)
    {
        StopSubBgmFade();
        subBgmFadeCoroutine = StartCoroutine(SubBgmFadeInRoutine(targetVolume, duration));
    }

    private void StopSubBgmFade()
    {
        if (subBgmFadeCoroutine != null)
        {
            StopCoroutine(subBgmFadeCoroutine);
            subBgmFadeCoroutine = null;
        }
    }

    private IEnumerator SubBgmFadeInRoutine(float targetVolume, float duration)
    {
        subBgmSource.volume = 0f;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            subBgmSource.volume = Mathf.Lerp(0f, targetVolume, Mathf.Clamp01(timer / duration));
            yield return null;
        }
        subBgmSource.volume = targetVolume;
        subBgmFadeCoroutine = null;
    }

    private IEnumerator SubBgmFadeOutRoutine(float duration)
    {
        float startVolume = subBgmSource.volume;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            subBgmSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(timer / duration));
            yield return null;
        }
        StopSubBGMImmediately();
        subBgmFadeCoroutine = null;
    }

    #endregion

    #region --- Ambience 제어 로직 ---

    private void InitAmbienceGroup()
    {
        GameObject ambGroup = new GameObject("Ambience_Group");
        ambGroup.transform.SetParent(this.transform);
        ambienceGroupTransform = ambGroup.transform;
    }

    public float PlayAmbience(string bgmKey, bool useFadeIn = true, float? fadeDurationOverride = null, bool restartIfPlaying = false)
    {
        if (!bgmDictionary.TryGetValue(bgmKey, out BGMData data))
        {
            Debug.LogWarning($"[SoundManager] 환경음 키 '{bgmKey}'를 찾을 수 없습니다.");
            return 0f;
        }

        if (data.clip == null) return 0f;

        AudioSource source;
        if (activeAmbienceDict.TryGetValue(bgmKey, out source))
        {
            if (!restartIfPlaying && source.isPlaying && source.clip == data.clip)
            {
                StopAmbienceFade(bgmKey);
                source.volume = data.volume;
                return 0f;
            }
        }
        else
        {
            GameObject ambObj = new GameObject($"Ambience_{bgmKey}");
            ambObj.transform.SetParent(ambienceGroupTransform);
            source = ambObj.AddComponent<AudioSource>();
            source.playOnAwake = false;

            if (AudioManager.Instance != null)
                source.outputAudioMixerGroup = AudioManager.Instance.BgmGroup;

            activeAmbienceDict.Add(bgmKey, source);
        }

        source.clip = data.clip;
        source.loop = data.loop;
        float fadeDuration = fadeDurationOverride ?? data.defaultFadeDuration;

        if (useFadeIn && fadeDuration > 0f)
        {
            source.Play();
            StartAmbienceFadeIn(bgmKey, source, data.volume, fadeDuration);
            return fadeDuration;
        }
        else
        {
            StopAmbienceFade(bgmKey);
            source.volume = data.volume;
            source.Play();
            return 0f;
        }
    }

    public void StopAmbience(string bgmKey, bool useFadeOut = true, float fadeDuration = 1.0f)
    {
        if (!activeAmbienceDict.TryGetValue(bgmKey, out AudioSource source)) return;
        if (!source.isPlaying) return;

        if (useFadeOut)
        {
            StartAmbienceFadeOut(bgmKey, source, fadeDuration);
        }
        else
        {
            StopAmbienceFade(bgmKey);
            source.Stop();
            source.clip = null;
        }
    }

    public void StopAllAmbience(bool useFadeOut = true, float fadeDuration = 1.0f)
    {
        List<string> keys = new List<string>(activeAmbienceDict.Keys);
        foreach (var key in keys)
        {
            StopAmbience(key, useFadeOut, fadeDuration);
        }
    }

    private void StartAmbienceFadeIn(string key, AudioSource source, float targetVolume, float duration)
    {
        StopAmbienceFade(key);
        Coroutine co = StartCoroutine(AmbienceFadeInRoutine(key, source, targetVolume, duration));
        ambienceFadeCoroutines[key] = co;
    }

    private void StartAmbienceFadeOut(string key, AudioSource source, float duration)
    {
        StopAmbienceFade(key);
        Coroutine co = StartCoroutine(AmbienceFadeOutRoutine(key, source, duration));
        ambienceFadeCoroutines[key] = co;
    }

    private void StopAmbienceFade(string key)
    {
        if (ambienceFadeCoroutines.TryGetValue(key, out Coroutine co) && co != null)
        {
            StopCoroutine(co);
            ambienceFadeCoroutines.Remove(key);
        }
    }

    private IEnumerator AmbienceFadeInRoutine(string key, AudioSource source, float targetVolume, float duration)
    {
        source.volume = 0f;
        float timer = 0f;
        while (timer < duration)
        {
            if (source == null) yield break;
            timer += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(0f, targetVolume, Mathf.Clamp01(timer / duration));
            yield return null;
        }
        if (source != null) source.volume = targetVolume;
        ambienceFadeCoroutines.Remove(key);
    }

    private IEnumerator AmbienceFadeOutRoutine(string key, AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float timer = 0f;
        while (timer < duration)
        {
            if (source == null) yield break;
            timer += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(timer / duration));
            yield return null;
        }
        if (source != null)
        {
            source.Stop();
            source.clip = null;
            source.volume = startVolume;
        }
        ambienceFadeCoroutines.Remove(key);
    }

    #endregion

    #region --- ★ 씬 전환 전용 통합 초기화 함수 ---

    /// <summary>
    /// 로비 이동 등 씬 전환 시 남아있는 BGM, SubBGM, Ambience, Voice를 깨끗하게 비웁니다.
    /// </summary>
    public void ResetAllAudioForSceneTransition()
    {
        SetGamePauseState(false);
        isBgmAPausedByMenu = false;
        isBgmBPausedByMenu = false;
        isSubBgmPausedByMenu = false;

        StopVoice();
        StopBGMImmediately();
        StopSubBGMImmediately();
        StopAllAmbience(useFadeOut: false);
    } 
    //public void ResetAllAudioForSceneTransition()
    //{
    //    SetGamePauseState(false);
    //    StopVoice();
    //    StopBGMImmediately();
    //    StopSubBGMImmediately();
    //    StopAllAmbience(useFadeOut: false);
    //}

    #endregion
}