using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class KeyboardKeyAudio : MonoBehaviour
{
    public AudioClip pressSound;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // --- 귀여운 효과음 연출을 위한 오디오 소스 기본 세팅 ---
        audioSource.playOnAwake = false;

        // 3D 사운드 설정을 1로 하면 플레이어와 키의 거리에 따라 소리 방향/크기가 달라집니다.
        // 일반 2D 사운드로 고르게 듣고 싶다면 이 값을 0f로 바꾸세요.
        audioSource.spatialBlend = 1f;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 오직 스탬프 손(Enemy)이 칠 때만 소리 나도록 설정
        if (other.CompareTag("Human"))
        {
            if (pressSound != null && audioSource != null)
            {
                // [꿀팁] 소리가 날 때마다 피치(높낮이)를 미세하게 무작위로 조절하면
                // 똑같은 소리라도 통통 튀는 기계식 토이 키보드처럼 훨씬 귀엽게 들립니다!
                audioSource.pitch = Random.Range(0.85f, 1.15f);

                // 소리가 겹치더라도 끊기지 않고 예쁘게 나도록 PlayOneShot 사용
                audioSource.PlayOneShot(pressSound);
            }
        }
    }
}