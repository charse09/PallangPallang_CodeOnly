using System.Collections;
using UnityEngine;

public class TelephoneRinger : MonoBehaviour
{
    [Header("Telephone Settings")]
    [Tooltip("전화벨이 울리는 주기 (초 단위)")]
    public float ringInterval = 3f;

    [Tooltip("전화벨 소리를 재생할 오디오 소스")]
    public AudioSource ringSound;

    [Header("Speaker Connection")]
    [Tooltip("신호를 전달할 대상 스피커의 SoundLauncher 컴포넌트를 연결하세요.")]
    public SoundLauncher targetSpeaker;

    private void Awake()
    {
        // 게임 시작과 동시에 주기적으로 전화벨을 울리는 코루틴 시작
        StartCoroutine(PeriodicRingSequence());
    }

    private IEnumerator PeriodicRingSequence()
    {
        while (true)
        {
            // 설정된 주기만큼 대기
            yield return new WaitForSeconds(ringInterval);

            // 1. 전화벨 소리 재생
            if (ringSound != null)
            {
                ringSound.Play();
            }
            // Debug.Log($"[{gameObject.name}] 따르릉! 전화기에서 소리가 납니다.");

            // 2. 연결된 스피커에 "음파 발사 시도" 명령 전달
            if (targetSpeaker != null)
            {
                targetSpeaker.TryTriggerBlast();
            }
        }
    }
}