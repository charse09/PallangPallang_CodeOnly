using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundLauncher : MonoBehaviour
{
    [Header("Activation Conditions (작동 조건)")]
    [Tooltip("마이크가 90도 회전했는지 여부 (ActivateBooleanOutput 노드로 켜주세요)")]
    public bool isMicReady = false;

    [Header("Blast Settings")]
    public float launchForce = 15f;
    public bool useTransformUp = true;

    [Header("Juice (연출 설정)")]
    public Transform speakerVisual;
    public SpeakerCameraShake cameraShakeZone;
    public float visualMin = 0.85f;

    public ParticleSystem shockwaveParticle;
    public AudioSource boomSound;

    private List<Rigidbody> targetsInZone = new List<Rigidbody>();
    private Transform targetVisual;
    private Vector3 originalScale;

    // 중복 실행 방지 (연출이 겹치지 않게 보호하는 장치)
    private bool isBlasting = false;

    void Start()
    {
        targetVisual = (speakerVisual != null) ? speakerVisual : transform;
        originalScale = targetVisual.localScale;
    }

    // --- [새로 추가된 핵심 함수] ---
    // 전화기(Telephone)에서 소리가 날 때마다 이 함수를 호출합니다.
    public void TryTriggerBlast()
    {
        // 1. 마이크가 꺾여 있고(true), 
        // 2. 현재 스피커가 이미 폭발 연출 중이 아니라면 실행!
        if (isMicReady && !isBlasting)
        {
            StartCoroutine(SingleBlastSequence());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null && !targetsInZone.Contains(rb)) targetsInZone.Add(rb);
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb != null && targetsInZone.Contains(rb)) targetsInZone.Remove(rb);
    }

    // 무한 반복이 아니라 딱 한 번만 폭발하는 코루틴으로 변경되었습니다.
    private IEnumerator SingleBlastSequence()
    {
        isBlasting = true; // 실행 중 표시

        // [사전 연출] 움츠러듦
        Vector3 targetScale = originalScale * visualMin;
        float timeElapsed = 0;
        while (timeElapsed < 0.3f)
        {
            targetVisual.localScale = Vector3.Lerp(originalScale, targetScale, timeElapsed / 0.3f);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // [터짐 연출]
        targetVisual.localScale = originalScale;
        if (shockwaveParticle != null) shockwaveParticle.Play();
        if (boomSound != null) boomSound.Play();

        // 진동
        if (cameraShakeZone != null)
        {
            cameraShakeZone.TriggerShake();
        }

        // [물리 연출] 범위 안 객체 날리기
        Vector3 blastDirection = useTransformUp ? transform.up : transform.forward;
        targetsInZone.RemoveAll(item => item == null);

        foreach (Rigidbody rb in targetsInZone)
        {
            PlayerLocomotion player = rb.GetComponent<PlayerLocomotion>();
            if (player != null)
            {
                Vector3 launchVector = blastDirection * launchForce;
                player.ExternalLaunch(launchVector);
            }
            else
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                rb.AddForce(blastDirection * launchForce, ForceMode.Impulse);
            }
        }

        isBlasting = false; // 실행 완료, 다음 전화벨을 기다릴 준비
    }
}