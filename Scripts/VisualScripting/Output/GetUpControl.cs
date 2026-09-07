using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// [Output 노드] 플레이어가 엎드린 자세에서 키를 연타해 힘겹게 일어나는 동작을 처리합니다.
    ///
    /// [동작 방식]
    ///   - Execute() 호출 시 PlayerLocomotions 물리를 차단하고 일어나기 모드 시작.
    ///   - 연타 키(기본: JumpPressed)를 누를 때마다 Progress가 증가하고,
    ///     누르지 않으면 일정 대기 후 Progress가 서서히 감소합니다.
    ///   - 키를 누를 때마다 캐릭터가 "솟구쳤다가 내려오는" 흔들림(Wobble)이 추가되어
    ///     힘겨운 느낌을 표현합니다.
    ///   - Progress가 1.0에 도달하면 완전히 일어나고 IsOn = true.
    ///   - Reset() 또는 완료 시 PlayerLocomotions 물리를 복원합니다.
    /// </summary>
    public class GetUpControl : ProcessBase
    {
        // ─── References ────────────────────────────────────────────
        [Header("References")]
        [Tooltip("일어날 오브젝트 Transform. 비워두면 이 컴포넌트가 붙은 오브젝트 자신.")]
        [SerializeField] private Transform targetObject;

        // ─── Rotation ──────────────────────────────────────────────
        [Header("Rotation Settings")]
        [Tooltip("엎드린 자세의 X 회전각 (도).\nFaceDownFallControl의 faceDownEulerAngles.x와 일치시킬 것.\n예) 90 = 얼굴을 아래로 향한 상태")]
        [SerializeField] private float prostratAngle = 90f;

        [Tooltip("완전히 일어선 자세의 X 회전각 (도). 일반적으로 0.")]
        [SerializeField] private float standingAngle = 0f;

        [Tooltip("회전이 목표치를 따라오는 보간 속도. 클수록 더 빠르게 반응.")]
        [SerializeField] private float rotationSmoothSpeed = 7f;

        // ─── Key Mash ──────────────────────────────────────────────
        [Header("Key Mash Settings")]
        [Tooltip("true면 InputManager.Instance.JumpTriggered(WasPressedThisFrame)를 연타 키로 사용합니다.\nfalse면 아래 FallbackMashKey를 사용합니다.")]
        [SerializeField] private bool useInputManagerJump = true;

        [Tooltip("useInputManagerJump가 false일 때 사용할 키.")]
        [SerializeField] private KeyCode fallbackMashKey = KeyCode.Space;

        // ─── Progress ──────────────────────────────────────────────
        [Header("Progress Settings")]
        [Tooltip("키를 한 번 누를 때 Progress가 증가하는 양 (0~1 범위).\n0.10~0.15 권장. 클수록 적게 눌러도 일어남.")]
        [SerializeField][Range(0.01f, 0.5f)] private float progressPerPress = 0.12f;

        [Tooltip("마지막 키 입력 후 Progress 감소가 시작되기까지의 대기 시간 (초).\n너무 짧으면 유저가 따라잡기 힘듦.")]
        [SerializeField] private float decayDelay = 0.38f;

        [Tooltip("Progress 감소 속도 (초당).\n클수록 빠르게 내려앉아 연타를 더 많이 해야 함.")]
        [SerializeField] private float decayRate = 0.20f;

        // ─── Wobble ────────────────────────────────────────────────
        [Header("Wobble Settings (흔들림)")]
        [Tooltip("키를 누를 때 추가되는 흔들림의 최대 각도 (도).\n클수록 더 격하게 들썩임. 15~30 권장.")]
        [SerializeField] private float wobbleAmplitude = 22f;

        [Tooltip("흔들림 진동 주파수 (rad/s).\n클수록 더 빠르게 떨림. 8~14 권장.")]
        [SerializeField] private float wobbleFrequency = 11f;

        [Tooltip("키를 누른 후 흔들림이 사라지기까지의 시간 (초).")]
        [SerializeField] private float wobbleFadeDuration = 0.50f;

        // ─── Audio ─────────────────────────────────────────────────
        [Header("Audio (선택)")]
        [Tooltip("키를 누를 때마다 재생할 효과음. 없으면 무시.")]
        [SerializeField] private AudioClip pressClip;

        [Tooltip("완전히 일어날 때 재생할 효과음. 없으면 무시.")]
        [SerializeField] private AudioClip standUpClip;

        private AudioSource _audioSource;

        // ─── 내부 상태 ─────────────────────────────────────────────
        private bool  _isActive        = false;
        private float _progress        = 0f;   // 실제 진행도 (0 = 엎드림, 1 = 완전히 일어남)
        private float _smoothProgress  = 0f;   // 회전 기저에 쓰이는 smoothed 값
        private float _wobbleTimer     = 0f;   // 흔들림 sin파 시간축
        private float _wobbleWeight    = 0f;   // 현재 흔들림 강도 (1 → 0 으로 감쇠)
        private float _decayDelayTimer = 0f;   // 마지막 입력 후 decay 대기 타이머
        private float _startY          = 0f;   // 진입 시 Y 회전 보존
        private int   _wobbleDirSign   = -1;   // 흔들림 방향: 일어나는 방향(+) vs 눕는 방향(-)

        private PlayerLocomotions _locomotion;

        // ───────────────────────────────────────────────────────────

        private void Awake()
        {
            if (targetObject == null)
                targetObject = transform;

            _locomotion  = targetObject.GetComponentInParent<PlayerLocomotions>();
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null && (pressClip != null || standUpClip != null))
                _audioSource = gameObject.AddComponent<AudioSource>();
        }

        // ─── ProcessBase 구현 ──────────────────────────────────────

        public override void Execute()
        {
            if (targetObject == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> [GetUpControl] " +
                                 "targetObject가 없습니다!");
#endif
                IsOn = true;
                return;
            }

            // 흔들림 방향: 각도가 작아지는 쪽이 "일어나는" 방향이면 -1, 커지는 쪽이면 +1
            // 예) prostratAngle=90, standingAngle=0 → 일어나는 방향 = X 감소 → _wobbleDirSign = -1
            _wobbleDirSign = (prostratAngle > standingAngle) ? -1 : 1;

            // PlayerLocomotions 물리 차단 (FaceDownFallControl과 동일한 방식)
            if (_locomotion != null)
                _locomotion.isFallingControl = true;

            // 현재 Y 회전 보존 (일어나는 도중 캐릭터가 바라보던 방향을 유지)
            _startY          = targetObject.eulerAngles.y;

            // 상태 초기화
            _progress        = 0f;
            _smoothProgress  = 0f;
            _wobbleTimer     = 0f;
            _wobbleWeight    = 0f;
            _decayDelayTimer = 0f;
            _isActive        = true;

            // 정확한 엎드린 자세로 스냅 (FaceDownFallControl이 이미 적용했을 것이나 안전하게)
            targetObject.rotation = Quaternion.Euler(prostratAngle, _startY, 0f);

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> [GetUpControl] " +
                      "일어나기 시작! 키를 연타하세요.");
#endif
        }

        public override void Reset()
        {
            base.Reset();

            if (_isActive)
            {
                // 강제 중단 시에도 PlayerLocomotions 물리 복원
                if (_locomotion != null)
                    _locomotion.isFallingControl = false;
            }

            _isActive     = false;
            _progress     = 0f;
            _wobbleWeight = 0f;
        }

        // ─── Update ────────────────────────────────────────────────

        private void Update()
        {
            if (!_isActive || targetObject == null) return;

            // ── 1. 연타 입력 감지 ─────────────────────────────────
            // JumpTriggered = WasPressedThisFrame() → 누른 첫 프레임에만 true.
            // JumpPressed(IsPressed)를 사용하면 누르는 동안 매 프레임 true가 되어
            // 한 번 눌렀을 때 즉시 완료되는 버그가 생기므로 반드시 Triggered를 사용.
            bool pressed = useInputManagerJump && InputManager.Instance != null
                ? InputManager.Instance.JumpTriggered
                : Input.GetKeyDown(fallbackMashKey);

            if (pressed)
            {
                OnKeyPressed();
            }

            // ── 2. Progress decay ─────────────────────────────────
            // 마지막 입력 후 decayDelay 초 뒤부터 progress가 서서히 감소
            if (_decayDelayTimer > 0f)
            {
                _decayDelayTimer -= Time.deltaTime;
            }
            else
            {
                _progress = Mathf.Max(0f, _progress - decayRate * Time.deltaTime);
            }

            // ── 3. Wobble 감쇠 ────────────────────────────────────
            if (_wobbleWeight > 0f)
            {
                _wobbleWeight -= Time.deltaTime / wobbleFadeDuration;
                _wobbleWeight  = Mathf.Max(0f, _wobbleWeight);
            }

            // ── 4. Smooth progress ────────────────────────────────
            // 실제 progress를 부드럽게 추적하여 회전의 기저로 사용
            _smoothProgress = Mathf.Lerp(_smoothProgress, _progress,
                                          Time.deltaTime * rotationSmoothSpeed);

            // ── 5. 회전 각도 계산 ─────────────────────────────────
            // [기저] progress에 따른 엎드림 → 일어섬 선형 보간
            float baseAngle = Mathf.Lerp(prostratAngle, standingAngle, _smoothProgress);

            // [흔들림] 키 누를 때마다 솟구쳤다가 내려오는 sin파 오실레이션
            // π/2에서 시작 → sin(π/2)=1 (즉시 최대 surge) → 0 → -1 (뒤로 처짐) → 반복
            _wobbleTimer += Time.deltaTime * wobbleFrequency;
            float wobble   = Mathf.Sin(_wobbleTimer) * wobbleAmplitude * _wobbleWeight;

            // _wobbleDirSign: 일어나는 방향으로 첫 번째 surge가 가도록 부호 결정
            // prostratAngle=90 → standingAngle=0 : 작아지는 방향이 "일어남" → sin 양수면 빼야 함 → sign=-1
            float finalAngle = baseAngle + wobble * _wobbleDirSign;

            // ── 6. 회전 적용 ─────────────────────────────────────
            // 흔들림이 강할 때(wobbleWeight 높을 때)는 더 빠르게 반응 (surge 느낌 강화)
            float dynamicSmooth = rotationSmoothSpeed + _wobbleWeight * 6f;

            targetObject.rotation = Quaternion.Slerp(
                targetObject.rotation,
                Quaternion.Euler(finalAngle, _startY, 0f),
                Time.deltaTime * dynamicSmooth
            );

            // ── 7. 완료 체크 ─────────────────────────────────────
            if (_progress >= 1f)
            {
                StandUp();
            }
        }

        // ─── 내부 메서드 ───────────────────────────────────────────

        /// <summary>
        /// 연타 키가 눌렸을 때 호출
        /// </summary>
        private void OnKeyPressed()
        {
            // Progress 증가 (1 초과 방지)
            _progress        = Mathf.Clamp01(_progress + progressPerPress);
            _decayDelayTimer = decayDelay;

            // 흔들림 강도를 최대치로 리셋하고 timer를 π/2에서 시작
            // → sin(π/2)=1 이므로 누른 즉시 최대 surge 연출
            _wobbleWeight = 1f;
            _wobbleTimer  = Mathf.PI * 0.5f / wobbleFrequency;

            // 효과음 재생
            PlayClip(pressClip);
        }

        /// <summary>
        /// Progress 1 도달 시 완전히 일어나는 처리
        /// </summary>
        private void StandUp()
        {
            _isActive = false;

            // PlayerLocomotions 물리 복원
            if (_locomotion != null)
                _locomotion.isFallingControl = false;

            // 일어선 자세로 확정 스냅
            targetObject.rotation = Quaternion.Euler(standingAngle, _startY, 0f);

            PlayClip(standUpClip);
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> [GetUpControl] 일어나기 완료!");
#endif
        }

        private void PlayClip(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
                _audioSource.PlayOneShot(clip);
        }

        // ─── Gizmos (에디터 전용) ──────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || targetObject == null) return;

            // 진행도 텍스트를 씬 뷰에 표시
            UnityEditor.Handles.color = Color.green;
            UnityEditor.Handles.Label(
                targetObject.position + Vector3.up * 2.5f,
                $"GetUp: {_progress * 100f:F0}%  Wobble: {_wobbleWeight:F2}"
            );

            // 현재 "일어서는" 방향 시각화
            Gizmos.color = Color.yellow;
            Vector3 standDir = targetObject.up * _wobbleDirSign;
            Gizmos.DrawRay(targetObject.position, standDir * 1.5f);
        }
#endif
    }
}
