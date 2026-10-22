using UnityEngine;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    public class SideTalkboxOutput : ProcessBase
    {
        [Header("Global Settings")]
        [Tooltip("진행을 담당할 매니저 (비워둘 경우 씬 내에서 자동 탐색)")]
        [SerializeField] private SideTalkboxManager manager;

        [Header("Dialogue & UI Settings")]
        [Tooltip("출력할 말풍선 위치 선택")]
        [SerializeField] private TalkboxPosition position = TalkboxPosition.BottomLeft_Berry;

        [Tooltip("텍스트 데이터 테이블")]
        [SerializeField] private CutInStringTable stringTable;

        [Tooltip("StringTable에서 가져올 대사 ID")]
        [SerializeField] private string stringID;

        [Tooltip("변경할 캐릭터 이미지 (비워두면 기존 이미지가 유지됩니다)")]
        [SerializeField] private Sprite characterSprite;

        [Tooltip("대사창과 이미지가 화면에 출력되어 유지될 기본 시간 (초)")]
        [SerializeField] private float duration = 2.5f;

        [Header("Skip Settings (스킵 설정)")]
        [Tooltip("체크 시 지정한 키 입력을 통해 대사를 스킵할 수 있습니다.")]
        [SerializeField] private bool allowSkip = true;

        [Tooltip("스킵을 트리거할 특정 키 (마우스 우클릭을 원할 경우 Mouse1 선택)")]
        [SerializeField] private KeyCode skipKey = KeyCode.Mouse1;

        [Header("Audio Settings")]
        [Tooltip("체크 시 대사창 출력 효과음을 재생합니다.")]
        [SerializeField] private bool playTalkboxSound = true;

        [Tooltip("대사창 출력 시 재생할 SFX 키 (SO_SFXTable 등록 키, 비워둘 경우 무음)")]
        [SerializeField] private string soundKey = "";

        [Tooltip("체크 시 더빙 음성을 함께 출력합니다.")]
        [SerializeField] private bool playVoice = true;

        [Tooltip("음성 식별 키 (비워둘 경우 stringID를 기본 키로 사용합니다)")]
        [SerializeField] private string customVoiceKey = "";

        [Tooltip("체크 시 음성 길이에 맞춰 말풍선 유지 시간 및 다음 노드 신호 전달 타이밍을 자동 동기화합니다.")]
        [SerializeField] private bool syncDurationToVoice = true;

        [Tooltip("음성 종료 후 다음 노드로 신호를 넘기기 전 여운 시간 (초)")]
        [SerializeField] private float voiceEndPadding = 0.2f;

        [Header("Progression Settings")]
        [Tooltip("시퀀스 완료를 기다리지 않고 즉시 다음 노드를 실행할지 여부 (체크 해제되어 있어야 음성이 끝날 때까지 대기합니다)")]
        [SerializeField] private bool runInBackground = false;

        private bool _isPlaying = false;
        private bool _isSkipping = false;
        private int _executeFrame = -1;
        private float _currentVoiceDuration = 0f;

        private void Awake()
        {
            FindManagerIfNull();
        }

        public override void Reset()
        {
            base.Reset();
            _isPlaying = false;
            _isSkipping = false;
            _currentVoiceDuration = 0f;
            IsOn = false;
        }

        public override void Execute()
        {
            FindManagerIfNull();

            if (manager == null)
            {
                Debug.LogError($"[{gameObject.name}] SideTalkboxManager를 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            IsOn = false;
            _isPlaying = true;
            _isSkipping = false;
            _executeFrame = Time.frameCount;

            PlaySFX();
            float voiceLength = PlayDialogueVoice(); // 더빙 음성 재생 및 길이 반환

            // 음성 동기화가 활성화되어 있고 유효한 음성이 재생되었다면 음성 길이 + 여운 시간을 적용
            float finalDuration = duration;
            if (syncDurationToVoice && voiceLength > 0f)
            {
                finalDuration = voiceLength + voiceEndPadding;
            }

            // 계산된 최종 유지 시간을 매니저에 전달
            manager.ShowTalkbox(stringID, characterSprite, finalDuration, stringTable, position, allowSkip, OnSequenceCompleted);

            if (runInBackground)
            {
                IsOn = true;
            }
        }

        private void Update()
        {
            HandleSkipInput();
        }

        private void HandleSkipInput()
        {
            if (!allowSkip || !_isPlaying || _isSkipping) return;

            // 실행된 첫 프레임의 중복 입력 방지
            if (Time.frameCount == _executeFrame) return;

            // 지정한 키(키보드 또는 마우스 버튼) 입력 감지
            if (Input.GetKeyDown(skipKey))
            {
                SkipDialogue();
            }
        }

        public void SkipDialogue()
        {
            if (_isSkipping || !_isPlaying) return;

            _isSkipping = true;
            _isPlaying = false;

            StopCurrentVoice();

            if (manager != null)
            {
                manager.SkipTalkbox(position);
            }

            if (!runInBackground)
            {
                IsOn = true;
            }
        }

        private void PlaySFX()
        {
            if (!playTalkboxSound) return;

            if (!string.IsNullOrEmpty(soundKey) && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(soundKey);
            }
        }

        private float PlayDialogueVoice()
        {
            _currentVoiceDuration = 0f;

            if (!playVoice || SoundManager.Instance == null) return 0f;

            string vKey = !string.IsNullOrEmpty(customVoiceKey) ? customVoiceKey : stringID;
            if (string.IsNullOrEmpty(vKey)) return 0f;

            // SoundManager에서 반환하는 실제 클립 길이(초)를 저장
            _currentVoiceDuration = SoundManager.Instance.PlayVoice(vKey, Vector3.zero);
            return _currentVoiceDuration;
        }

        private void StopCurrentVoice()
        {
            if (SoundManager.Instance == null) return;

            string vKey = !string.IsNullOrEmpty(customVoiceKey) ? customVoiceKey : stringID;
            if (!string.IsNullOrEmpty(vKey))
            {
                SoundManager.Instance.StopVoice(vKey);
            }
            else
            {
                SoundManager.Instance.StopVoice();
            }
        }

        private void FindManagerIfNull()
        {
            if (manager == null)
            {
                manager = FindFirstObjectByType<SideTalkboxManager>(FindObjectsInactive.Include);
            }
        }

        private void OnSequenceCompleted()
        {
            _isPlaying = false;

            if (!_isSkipping)
            {
                StopCurrentVoice();
            }

            _isSkipping = false;

            if (!runInBackground)
            {
                IsOn = true;
            }
        }

        private void OnDisable()
        {
            if (!_isSkipping)
            {
                StopCurrentVoice();
            }

            _isPlaying = false;
            _isSkipping = false;
            _currentVoiceDuration = 0f;
            IsOn = false;
        }
    }
}