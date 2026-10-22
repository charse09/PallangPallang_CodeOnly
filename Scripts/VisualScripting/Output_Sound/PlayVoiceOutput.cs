using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    public class PlayVoiceOutput : ProcessBase
    {
        [Header("1. Voice Source Settings")]
        [Tooltip("SO_VoiceTable에 등록된 음성 키 (키 기반 호출 시 입력, 볼륨/3D/덕킹은 테이블 설정을 따름)")]
        [SerializeField] private string voiceKey = "";

        [Tooltip("직접 재생할 오디오 클립 (VoiceTable에 등록하지 않고 클립을 직접 넣을 때 사용)")]
        [SerializeField] private AudioClip voiceClip;

        [Header("2. Playback & 3D Settings (voiceClip 직접 사용 시 적용)")]
        [Tooltip("개별 음성 볼륨 (0.0 ~ 1.0, voiceClip 사용 시 적용)")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        [Tooltip("체크 시 지정한 위치(voiceLocation) 기반 3D 공간음으로 재생합니다.")]
        [SerializeField] private bool is3D = false;

        [Tooltip("3D 공간음 재생 위치로 사용할 Transform (비어있으면 본인 위치 사용)")]
        [SerializeField] private Transform voiceLocation;

        [Tooltip("체크 시 음성 재생 중 BGM 볼륨을 감쇄(더킹)합니다. (voiceClip 사용 시 적용)")]
        [SerializeField] private bool enableDucking = true;

        [Header("3. Process & Signal Settings")]
        [Tooltip("True: 음성 재생 완료(또는 패딩 시점) 후 다음 노드로 신호(IsOn = true) 전송\nFalse: 음성 시작 즉시 다음 노드로 신호 전송")]
        [SerializeField] private bool waitForCompletion = true;

        [Tooltip("신호 전송 전 대기 시간 보정값 (초)\n- 양수(+): 음성 종료 후 여운 대기\n- 음수(-): 음성이 완전히 끝나기 N초 전에 미리 다음 노드로 신호 전송")]
        [SerializeField] private float voiceEndPadding = 0.2f;

        [Tooltip("타임스케일(일시정지 등)의 영향을 받지 않고 실제 시간 기준으로 대기할지 여부")]
        [SerializeField] private bool useUnscaledTime = false;

        [Header("4. Skip Settings (스킵 설정)")]
        [Tooltip("체크 시 지정한 키 입력을 통해 음성을 즉시 중단하고 다음 노드로 진행합니다.")]
        [SerializeField] private bool allowSkip = true;

        [Tooltip("스킵을 트리거할 키 (기본: 마우스 우클릭)")]
        [SerializeField] private KeyCode skipKey = KeyCode.Mouse1;

        private Coroutine _voiceCoroutine;
        private bool _isPlaying = false;
        private bool _isSkipping = false;
        private int _executeFrame = -1;
        private string _activeVoiceKey = "";

        public override void Execute()
        {
            // 노드 실행 시작 시 신호를 끄고 초기화
            IsOn = false;
            _isPlaying = false;
            _isSkipping = false;
            _executeFrame = Time.frameCount;

            if (SoundManager.Instance == null)
            {
                Debug.LogWarning($"[{name}] SoundManager 인스턴스를 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            // 음성 소스 확인 (클립 또는 키 둘 다 없을 경우 즉시 패스)
            if (voiceClip == null && string.IsNullOrEmpty(voiceKey))
            {
                Debug.LogWarning($"[{name}] 재생할 AudioClip 또는 voiceKey가 지정되지 않았습니다.");
                IsOn = true;
                return;
            }

            StartPlayVoice();
        }

        private void StartPlayVoice()
        {
            Vector3 pos = (voiceLocation != null) ? voiceLocation.position : transform.position;
            ClearVoiceCoroutine();

            float duration = 0f;

            // 1. 직접 할당된 AudioClip 우선 재생
            if (voiceClip != null)
            {
                _activeVoiceKey = voiceClip.name;
                duration = SoundManager.Instance.PlayVoice(voiceClip, pos, volume, is3D, enableDucking);
            }
            // 2. 키값 기반 VoiceTable 재생
            else if (!string.IsNullOrEmpty(voiceKey))
            {
                _activeVoiceKey = voiceKey;
                duration = SoundManager.Instance.PlayVoice(voiceKey, pos);
            }

            // 음성 재생에 실패했거나 대기 옵션이 꺼져 있다면 키를 비우고 즉시 다음 노드로 신호 전송
            if (!waitForCompletion || duration <= 0f)
            {
                _activeVoiceKey = "";
                IsOn = true;
                return;
            }

            // 음성 완료 대기 루틴 시작 (음수 패딩도 전달 가능하도록 voiceEndPadding 그대로 전달)
            _isPlaying = true;
            _voiceCoroutine = StartCoroutine(WaitForVoiceRoutine(duration, voiceEndPadding));
        }

        private void Update()
        {
            HandleSkipInput();
        }

        private void HandleSkipInput()
        {
            if (!allowSkip || !_isPlaying || _isSkipping) return;

            // 노드가 활성화된 첫 프레임의 중복 입력 방지
            if (Time.frameCount == _executeFrame) return;

            if (Input.GetKeyDown(skipKey))
            {
                SkipVoice();
            }
        }

        public void SkipVoice()
        {
            if (_isSkipping || !_isPlaying) return;

            _isSkipping = true;
            _isPlaying = false;

            StopActiveVoice();
            ClearVoiceCoroutine();

            // 스킵 즉시 다음 노드로 완료 신호 전송
            IsOn = true;
        }

        private IEnumerator WaitForVoiceRoutine(float clipDuration, float padding)
        {
            // 1. 음수/양수 패딩이 반영된 신호 전달 시점까지의 대기 시간 계산 (최소 0초)
            float signalWaitTime = Mathf.Max(0f, clipDuration + padding);

            if (signalWaitTime > 0f)
            {
                if (useUnscaledTime)
                    yield return new WaitForSecondsRealtime(signalWaitTime);
                else
                    yield return new WaitForSeconds(signalWaitTime);
            }

            // 2. 다음 노드로 신호(IsOn = true) 우선 전송
            IsOn = true;

            // 3. 음수 패딩으로 인해 실제 오디오 클립이 아직 재생 중인 경우,
            // 남은 시간만큼 대기하여 음성 상태를 끝까지 안전하게 유지합니다.
            float remainingVoiceTime = clipDuration - signalWaitTime;
            if (remainingVoiceTime > 0f)
            {
                if (useUnscaledTime)
                    yield return new WaitForSecondsRealtime(remainingVoiceTime);
                else
                    yield return new WaitForSeconds(remainingVoiceTime);
            }

            _isPlaying = false;
            _voiceCoroutine = null;
            _activeVoiceKey = "";
        }

        /// <summary>
        /// 자신이 실행했던 음성 키와 일치할 때만 안전하게 정지합니다.
        /// (다른 노드나 외부에서 재생 중인 새 음성을 무차별 정지하지 않도록 방지)
        /// </summary>
        private void StopActiveVoice()
        {
            if (SoundManager.Instance == null) return;

            if (!string.IsNullOrEmpty(_activeVoiceKey))
            {
                SoundManager.Instance.StopVoice(_activeVoiceKey);
                _activeVoiceKey = "";
            }
        }

        private void ClearVoiceCoroutine()
        {
            if (_voiceCoroutine != null)
            {
                StopCoroutine(_voiceCoroutine);
                _voiceCoroutine = null;
            }
        }

        public override void Reset()
        {
            base.Reset();
            _isPlaying = false;
            _isSkipping = false;
            StopActiveVoice();
            ClearVoiceCoroutine();
            IsOn = false;
        }

        private void OnDisable()
        {
            if (_isPlaying && !_isSkipping)
            {
                StopActiveVoice();
            }

            _isPlaying = false;
            _isSkipping = false;
            ClearVoiceCoroutine();
            IsOn = false;
        }
    }
}