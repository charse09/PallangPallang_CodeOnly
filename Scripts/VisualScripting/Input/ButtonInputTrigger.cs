using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// UI Button 클릭 시 탄력 있는 팝(Punch Scale) 애니메이션을 재생한 뒤
    /// 시퀀스를 진행시키는 Input Trigger 노드입니다.
    /// </summary>
    public class ButtonInputTrigger : ProcessBase
    {
        [Header("1. Target Reference")]
        [Tooltip("클릭을 감지할 UI Button (미할당 시 본인 오브젝트에서 자동 탐색)")]
        [SerializeField] private Button targetButton;

        [Header("2. Bounce Animation (뽀용 연출)")]
        [Tooltip("클릭 시 탄력 있게 튀어 오르는 스케일 연출 활성화 여부")]
        [SerializeField] private bool usePunchAnimation = true;

        [Tooltip("최대 확대 배율 (기본 1.15 ~ 1.25 권장)")]
        [SerializeField] private float punchScale = 1.2f;

        [Tooltip("뽀용하고 튕겨 돌아오는 총 시간 (초)")]
        [SerializeField] private float punchDuration = 0.22f;

        [Tooltip("탄력 반동 커브 (살짝 커졌다가 0.95로 줄어든 뒤 원래 크기로 안착)")]
        [SerializeField] private AnimationCurve punchCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.4f, 1.2f),
            new Keyframe(0.75f, 0.95f),
            new Keyframe(1f, 1f)
        );

        [Tooltip("뽀용 튀는 애니메이션이 완전히 끝난 뒤 다음 시퀀스로 넘어갈지 여부")]
        [SerializeField] private bool waitForAnimationToFinish = true;

        [Tooltip("게임 일시정지(TimeScale = 0) 중에도 튀는 애니메이션이 작동할지 여부")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("3. Trigger Mode")]
        [Tooltip("체크 시: Execute()가 호출되기 전까지는 입력을 대기하지 않음.\n해제 시: 씬 시작부터 버튼 클릭 상시 대기.")]
        [SerializeField] private bool waitForExecute = true;

        [Tooltip("1회 클릭 후 입력을 닫고 완료할지 여부")]
        [SerializeField] private bool oneShot = true;

        [Header("4. Button UI Control")]
        [Tooltip("대기 상태에 맞춰 Button의 interactable을 자동 On/Off할지 여부")]
        [SerializeField] private bool autoManageInteractable = true;

        [Tooltip("트리거 완료 시 버튼 게임오브젝트를 비활성화(SetActive false)할지 여부")]
        [SerializeField] private bool deactivateOnTriggered = false;

        [Header("5. Custom Callbacks")]
        [Tooltip("버튼이 클릭되었을 때 추가로 호출할 인스펙터 이벤트")]
        [SerializeField] private UnityEvent onClickTriggered;

        private bool _isListening = false;
        private Vector3 _originalScale = Vector3.one;
        private Coroutine _punchCoroutine;
        private Transform _targetTransform;

        private void Awake()
        {
            if (targetButton == null) targetButton = GetComponent<Button>();
            if (targetButton != null)
            {
                _targetTransform = targetButton.transform;
                _originalScale = _targetTransform.localScale;
            }

            IsOn = false;
        }

        private void Start()
        {
            if (!waitForExecute)
            {
                StartListening();
            }
            else if (autoManageInteractable && targetButton != null)
            {
                targetButton.interactable = false;
            }
        }

        public override void Reset()
        {
            base.Reset();

            StopListening();
            RestoreScale();
            IsOn = false;

            if (waitForExecute && autoManageInteractable && targetButton != null)
            {
                targetButton.interactable = false;
            }
        }

        public override void Execute()
        {
            IsOn = false;
            StartListening();
        }

        private void StartListening()
        {
            if (_isListening || targetButton == null) return;

            targetButton.onClick.AddListener(OnButtonClicked);
            _isListening = true;

            if (autoManageInteractable)
            {
                targetButton.interactable = true;
            }
        }

        private void StopListening()
        {
            if (!_isListening || targetButton == null) return;

            targetButton.onClick.RemoveListener(OnButtonClicked);
            _isListening = false;
        }

        private void OnButtonClicked()
        {
            if (oneShot)
            {
                StopListening();

                if (autoManageInteractable && targetButton != null)
                {
                    targetButton.interactable = false;
                }
            }

            onClickTriggered?.Invoke();

            if (usePunchAnimation && _targetTransform != null)
            {
                if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
                _punchCoroutine = StartCoroutine(PunchScaleRoutine());
            }
            else
            {
                FinishTrigger();
            }
        }

        private IEnumerator PunchScaleRoutine()
        {
            float elapsed = 0f;

            if (!waitForAnimationToFinish)
            {
                IsOn = true;
            }

            while (elapsed < punchDuration)
            {
                float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += delta;

                float progress = Mathf.Clamp01(elapsed / punchDuration);
                
                // 커브 평가 및 스케일 배율 적용
                float curveVal = punchCurve.Evaluate(progress);
                // punchScale 인스펙터 값을 반영하여 정규화된 커브 진폭 보정
                float scaleMultiplier = Mathf.LerpUnclamped(1f, punchScale, (curveVal - 1f) / 0.2f + 1f);

                _targetTransform.localScale = _originalScale * scaleMultiplier;

                yield return null;
            }

            RestoreScale();
            _punchCoroutine = null;

            if (waitForAnimationToFinish)
            {
                FinishTrigger();
            }
        }

        private void FinishTrigger()
        {
            if (deactivateOnTriggered && targetButton != null)
            {
                targetButton.gameObject.SetActive(false);
            }

            IsOn = true;
        }

        private void RestoreScale()
        {
            if (_punchCoroutine != null)
            {
                StopCoroutine(_punchCoroutine);
                _punchCoroutine = null;
            }

            if (_targetTransform != null)
            {
                _targetTransform.localScale = _originalScale;
            }
        }

        private void OnDisable() => RestoreScale();
        private void OnDestroy() => StopListening();
    }
}