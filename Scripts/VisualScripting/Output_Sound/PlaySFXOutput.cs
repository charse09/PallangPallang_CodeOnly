using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class PlaySFXOutput : ProcessBase
    {
        [Header("Sound Settings")]
        [Tooltip("SO_SoundTable에 등록된 사운드 키(soundKey)를 입력하세요.")]
        [SerializeField] private string soundKey;

        [Tooltip("3D 공간음 위치로 사용할 Transform (비어있으면 현재 오브젝트의 위치를 사용합니다.)")]
        [SerializeField] private Transform soundLocation;

        [Header("Process Settings")]
        [Tooltip("True: 사운드 재생이 완벽히 끝난 후 IsOn을 true로 변경\nFalse: 사운드 재생 시작 즉시 IsOn을 true로 변경")]
        [SerializeField] private bool waitForCompletion = false;

        private Coroutine _soundCoroutine;

        private void Awake()
        {
            if (soundLocation == null)
            {
                soundLocation = this.transform;
            }
        }

        public override void Execute()
        {
            IsOn = false;

            // 예외 처리: soundKey 미설정 시 경고 후 즉시 완료 처리
            if (string.IsNullOrEmpty(soundKey))
            {
                Debug.LogWarning($"[{name}] Sound Key가 지정되지 않았습니다.");
                IsOn = true;
                return;
            }

            // 예외 처리: SoundManager 미존재 시 경고 후 즉시 완료 처리
            if (SoundManager.Instance == null)
            {
                Debug.LogWarning($"[{name}] 씬에 SoundManager 인스턴스가 존재하지 않습니다.");
                IsOn = true;
                return;
            }

            StartPlaySFX();
        }

        private void StartPlaySFX()
        {
            Vector3 targetPosition = (soundLocation != null) ? soundLocation.position : transform.position;

            // SoundManager를 통해 사운드 재생 및 AudioSource 참조 수신
            AudioSource source = SoundManager.Instance.PlaySFX(soundKey, targetPosition);

            if (source == null)
            {
                IsOn = true;
                return;
            }

            // 사운드가 끝날 때까지 대기해야 하는 연출인 경우 코루틴 실행
            if (waitForCompletion && source.clip != null)
            {
                if (_soundCoroutine != null) StopCoroutine(_soundCoroutine);
                _soundCoroutine = StartCoroutine(WaitForSoundFinishRoutine(source.clip.length));
            }
            else
            {
                // 재생 즉시 다음 프로세스로 넘어가는 경우
                IsOn = true;
            }
        }

        private IEnumerator WaitForSoundFinishRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);

            _soundCoroutine = null;
            IsOn = true;
        }
    }
}