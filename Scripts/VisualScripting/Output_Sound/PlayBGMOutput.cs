using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 씬(Scene)이 전환되어도 끊기지 않고 계속 재생되는 배경음악(BGM) Output 모듈
    /// NewBGMOutput 사용바람.
    /// </summary>
    public class PlayBGMOutput : ProcessBase
    {
        [Header("BGM Settings")]
        [Tooltip("재생할 배경음악 오디오 클립을 넣으세요.")]
        [SerializeField] private AudioClip bgmClip;

        [Tooltip("재생 볼륨 (0.0 ~ 1.0)")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1.0f;

        [Tooltip("체크 해제: 이미 같은 BGM이 재생 중이면 그대로 이어서 재생합니다. (자연스러운 씬 이동에 필수)\n체크: 같은 BGM이더라도 무조건 처음부터 다시 재생합니다.")]
        [SerializeField] private bool restartIfPlaying = false;

        [Header("Fade In Settings")]
        [Tooltip("체크 시 BGM이 시작될 때 서서히 볼륨이 커집니다.")]
        [SerializeField] private bool useFadeIn = false;

        [Tooltip("서서히 커지는 시간 (초 단위)")]
        [SerializeField] private float fadeDuration = 1.5f;

        public override void Execute()
        {
            if (bgmClip == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> BGM 클립이 할당되지 않았습니다!");
#endif
                return;
            }

            PlayPersistentBGM();

            // 액션 실행 후 프레임워크 룰에 따라 즉시 다음 노드로 신호 전달
            IsOn = true;
        }

        private void PlayPersistentBGM()
        {
            // BGM을 전담할 영구적인(Persistent) 게임오브젝트의 이름
            string bgmPlayerName = "GOS_PersistentBGMPlayer";

            // 씬에 이미 BGM 플레이어가 존재하는지 검색
            GameObject bgmObj = GameObject.Find(bgmPlayerName);
            AudioSource audioSource;
            PersistentBGMController bgmController;

            if (bgmObj == null)
            {
                // 없으면 새로 생성하고 오디오 소스 및 헬퍼 컴포넌트 추가
                bgmObj = new GameObject(bgmPlayerName);
                audioSource = bgmObj.AddComponent<AudioSource>();
                bgmController = bgmObj.AddComponent<PersistentBGMController>();

                // ★ 핵심: 이 오브젝트는 씬이 변경되어도 파괴되지 않도록 설정
                DontDestroyOnLoad(bgmObj);
            }
            else
            {
                audioSource = bgmObj.GetComponent<AudioSource>();
                bgmController = bgmObj.GetComponent<PersistentBGMController>();
                if (bgmController == null)
                {
                    bgmController = bgmObj.AddComponent<PersistentBGMController>();
                }
            }

            // AudioMixer의 BGM 그룹에 출력 연결
            if (AudioManager.Instance != null && AudioManager.Instance.BgmGroup != null)
            {
                audioSource.outputAudioMixerGroup = AudioManager.Instance.BgmGroup;
            }

            // [자연스러운 이어짐 방지] 
            // 현재 플레이어에 세팅된 음악이 내가 틀려는 음악과 같고, 재생 중이라면?
            if (!restartIfPlaying && audioSource.isPlaying && audioSource.clip == bgmClip)
            {
                // 이미 동일한 음악이 잘 나오는 중이므로 기존 페이드를 중단하고 해당 볼륨으로 고정
                bgmController.StopFade();
                audioSource.volume = volume;
#if UNITY_EDITOR
                Debug.Log($"<color=yellow>[{gameObject.name}]</color> 동일한 BGM이 이미 재생 중이므로 이어서 재생합니다: {bgmClip.name}");
#endif
                return;
            }

            // 새로운 음악 세팅
            audioSource.clip = bgmClip;
            audioSource.loop = true; // BGM이므로 무한 반복 활성화

            if (useFadeIn)
            {
                audioSource.Play();
                // 영구 플레이어 오브젝트에 구현된 페이드 인 코루틴 호출 (씬 전환 시에도 끊기지 않음)
                bgmController.StartFadeIn(audioSource, volume, fadeDuration);
            }
            else
            {
                bgmController.StopFade();
                audioSource.volume = volume;
                audioSource.Play();
            }

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> 새로운 BGM 재생 시작 (FadeIn: {useFadeIn}): {bgmClip.name}");
#endif
        }
    }

    /// <summary>
    /// 영구(Persistent) BGM 오브젝트에 붙어 페이드 인 코루틴을 관리하는 클래스
    /// </summary>
    public class PersistentBGMController : MonoBehaviour
    {
        private Coroutine fadeCoroutine;

        public void StartFadeIn(AudioSource source, float targetVolume, float duration)
        {
            StopFade();
            fadeCoroutine = StartCoroutine(FadeInRoutine(source, targetVolume, duration));
        }

        public void StopFade()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
        }

        private IEnumerator FadeInRoutine(AudioSource source, float targetVolume, float duration)
        {
            if (source == null) yield break;

            source.volume = 0f; // 볼륨 0에서 시작
            float timer = 0f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                if (source == null) yield break;

                // 0에서 목표 볼륨(targetVolume)까지 보평하여 증가
                source.volume = Mathf.Lerp(0f, targetVolume, timer / duration);
                yield return null;
            }

            if (source != null)
            {
                source.volume = targetVolume; // 최종 목표 볼륨 수치 맞춤
            }
        }
    }
}