using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TelekinesisBeam : MonoBehaviour
{
    // 씬 전체에서 활성화된 오디오 담당 빔들을 추적하는 정적 컬렉션
    private static readonly HashSet<TelekinesisBeam> activeAudioBeams = new HashSet<TelekinesisBeam>();

    [Header("연결 대상")]
    public Transform playerHand;   // 손 위치
    public Transform targetObject; // 잡고 있는 물체

    [Header("이펙트")]
    public ParticleSystem handParticle; // 손에서 터질 파티클
    public float uvScrollSpeed = 2.0f;

    [Header("Audio Settings")]
    [Tooltip("체크 해제 시 이 빔은 오디오 카운팅에서 완전히 제외됩니다.")]
    public bool enableAudio = true;

    [Header("SFX Table Sound Keys (SO_SFXTable 등록 키)")]
    [Tooltip("염력 시작 시 1회 재생할 SFX 키")]
    [SerializeField] private string startSoundKey = "Telekinesis_Start";

    [Tooltip("염력 유지 중 반복 재생할 SFX 키")]
    [SerializeField] private string loopSoundKey = "Telekinesis_Loop";

    [Tooltip("염력 해제 시 1회 재생할 SFX 키")]
    [SerializeField] private string endSoundKey = "Telekinesis_End";

    private LineRenderer lineRenderer;
    private Material beamMaterial;
    private float offset = 0f;

    private AudioSource loopAudioSource;
    private bool isBeamActive = false;
    private static bool isApplicationQuitting = false;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        beamMaterial = lineRenderer.material;

        if (enableAudio)
        {
            InitLoopAudioSource();
        }

        if (handParticle != null)
        {
            handParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            handParticle.Clear(true);
        }
    }

    private void InitLoopAudioSource()
    {
        loopAudioSource = GetComponent<AudioSource>();
        if (loopAudioSource == null)
        {
            loopAudioSource = gameObject.AddComponent<AudioSource>();
        }

        loopAudioSource.playOnAwake = false;
        loopAudioSource.loop = true;
        loopAudioSource.spatialBlend = 0f;

        if (AudioManager.Instance != null && AudioManager.Instance.SfxGroup != null)
        {
            loopAudioSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
        }
    }

    private void OnEnable()
    {
        if (targetObject != null && playerHand != null)
        {
            ActivateBeam();
        }
    }

    private void OnDisable()
    {
        if (isBeamActive)
        {
            DeactivateBeam();
        }
    }

    private void OnDestroy()
    {
        // 파괴 시 정적 컬렉션에서 안전하게 제거 및 잔여 빔 사운드 인계
        if (activeAudioBeams.Contains(this))
        {
            activeAudioBeams.Remove(this);
            PassLoopSoundToRemainingBeam();
        }
    }

    private void OnApplicationQuit()
    {
        isApplicationQuitting = true;
    }

    private void Update()
    {
        bool hasValidTarget = (playerHand != null && targetObject != null);

        if (isBeamActive && hasValidTarget)
        {
            if (!lineRenderer.enabled) lineRenderer.enabled = true;

            // 선 좌표 갱신 (월드 좌표)
            lineRenderer.useWorldSpace = true;
            lineRenderer.SetPosition(0, playerHand.position);
            lineRenderer.SetPosition(1, targetObject.position);

            // 파티클 위치 동기화
            if (handParticle != null)
            {
                handParticle.transform.position = playerHand.position;
            }

            // UV 애니메이션
            offset += Time.deltaTime * uvScrollSpeed;
            if (offset > 1.0f) offset -= 1.0f;
            beamMaterial.SetVector("_Offset", new Vector2(offset, 0));
        }
        else
        {
            if (lineRenderer != null && lineRenderer.enabled)
            {
                lineRenderer.enabled = false;
            }
        }
    }

    public void SetBeamActive(bool isActive, Transform target = null)
    {
        if (target != null)
        {
            targetObject = target;
        }

        if (isActive)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }
            ActivateBeam();
        }
        else
        {
            DeactivateBeam();
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void ActivateBeam()
    {
        if (isBeamActive || targetObject == null || playerHand == null) return;
        isBeamActive = true;

        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.useWorldSpace = true;
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, playerHand.position);
            lineRenderer.SetPosition(1, targetObject.position);
        }

        Vector3 handPos = playerHand.position;

        if (handParticle != null)
        {
            handParticle.transform.position = handPos;
            handParticle.Clear(true);
            handParticle.Play();
        }

        // --- 사운드 카운팅 처리 ---
        if (enableAudio)
        {
            // 씬 전체에서 첫 번째로 켜진 빔인 경우에만 시작음 및 루프 재생
            if (activeAudioBeams.Count == 0)
            {
                PlaySFX(startSoundKey, handPos);
                StartLoopSound();
            }

            activeAudioBeams.Add(this);
        }
    }

    private void DeactivateBeam()
    {
        if (!isBeamActive) return;
        isBeamActive = false;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }

        if (handParticle != null)
        {
            handParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            handParticle.Clear(true);
        }

        Vector3 handPos = (playerHand != null) ? playerHand.position : transform.position;

        // --- 사운드 카운팅 처리 ---
        if (enableAudio)
        {
            activeAudioBeams.Remove(this);
            StopLoopSound();

            if (!isApplicationQuitting)
            {
                if (activeAudioBeams.Count == 0)
                {
                    // 씬 내의 모든 빔이 꺼졌을 때만 종료음 1회 재생
                    PlaySFX(endSoundKey, handPos);
                }
                else
                {
                    // 내가 루프를 틀고 있었는데 꺼졌다면, 남아있는 다른 빔에게 루프 사운드를 인계
                    PassLoopSoundToRemainingBeam();
                }
            }
        }
    }

    /// <summary>
    /// 루프를 재생하던 빔이 꺼졌을 때, 남아있는 활성 빔 중 하나가 루프 사운드를 이어서 재생하도록 보장
    /// </summary>
    private static void PassLoopSoundToRemainingBeam()
    {
        foreach (var beam in activeAudioBeams)
        {
            if (beam != null && beam.gameObject.activeInHierarchy)
            {
                if (beam.loopAudioSource != null && !beam.loopAudioSource.isPlaying)
                {
                    beam.StartLoopSound();
                }
                break;
            }
        }
    }

    private void StartLoopSound()
    {
        if (string.IsNullOrEmpty(loopSoundKey) || SoundManager.Instance == null || loopAudioSource == null) return;

        if (SoundManager.Instance.TryGetSFXData(loopSoundKey, out var sfxData))
        {
            if (sfxData.clip != null)
            {
                loopAudioSource.clip = sfxData.clip;
                loopAudioSource.volume = sfxData.volume;
                loopAudioSource.Play();
            }
        }
    }

    private void StopLoopSound()
    {
        if (loopAudioSource != null && loopAudioSource.isPlaying)
        {
            loopAudioSource.Stop();
            loopAudioSource.clip = null;
        }
    }

    private void PlaySFX(string soundKey, Vector3 position)
    {
        if (!string.IsNullOrEmpty(soundKey) && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(soundKey, position);
        }
    }
}