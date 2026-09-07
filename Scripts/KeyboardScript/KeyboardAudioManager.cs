using UnityEngine;

public class KeyboardAudioManager : MonoBehaviour
{
    [Header("Sound Settings")]
    [Tooltip("키보드가 눌릴 때 재생할 귀여운 효과음을 넣어주세요.")]
    public AudioClip globalCuteSound;

    [ContextMenu("Setup All Key Sounds")]
    public void SetupKeySounds()
    {
        if (globalCuteSound == null)
        {
            Debug.LogWarning("[KeyboardAudioManager] 재생할 오디오 클립(Sound)이 지정되지 않았습니다! 효과음을 먼저 넣어주세요.");
            return;
        }

        // 키보드 바디의 자식(키캡)들을 순회하며 사운드 컴포넌트 일괄 추가
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            // 1. 소리를 내기 위한 AudioSource가 없다면 추가
            AudioSource audioSource = child.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = child.gameObject.AddComponent<AudioSource>();
            }

            // 2. 방금 만든 사운드 감지 스크립트가 없다면 추가
            KeyboardKeyAudio keyAudio = child.GetComponent<KeyboardKeyAudio>();
            if (keyAudio == null)
            {
                keyAudio = child.gameObject.AddComponent<KeyboardKeyAudio>();
            }

            // 3. 매니저에 등록한 귀여운 소리를 모든 키에 복사
            keyAudio.pressSound = globalCuteSound;
        }

        Debug.Log($"[KeyboardAudioManager] 총 {transform.childCount}개의 키캡에 귀여운 사운드 일괄 세팅이 완료되었습니다!");
    }
}