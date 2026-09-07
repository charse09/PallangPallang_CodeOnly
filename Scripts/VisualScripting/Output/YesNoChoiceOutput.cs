using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace _Project.Scripts.VisualScripting
{
    [Serializable]
    public class YesNoStep
    {
        [Tooltip("인스펙터 식별용 이름")]
        public string stepName = "Step 1";

        [TextArea(2, 4)]
        [Tooltip("화면에 표시할 질문 텍스트")]
        public string questionText = "정말 동의하십니까?";

        [Header("--- [No Button Behavior For This Step] ---")]
        [Tooltip("이 단계에서 '아니오' 버튼이 도망칠지 여부 (1단계: false / 2단계 이후: true 추천)")]
        public bool noButtonRunsAway = false;

        [Tooltip("이 단계에서의 '아니오' 버튼 크기 배율 (1.0 = 100%, 0.75 = 75%, 0.5 = 50%)")]
        [Range(0.1f, 1.5f)]
        public float buttonScale = 1.0f;

        [Tooltip("이 단계에서의 도망 속도 (15 = 보통, 25 = 빠름, 40 = 극도로 빠름)")]
        public float runSpeed = 16f;

        [Header("--- [Branch Processes] ---")]
        [Tooltip("'예'를 눌렀을 때 실행할 분기 프로세스 (실행 후 즉시 선택창이 꺼집니다)")]
        public ProcessData onYesProcess;

        [Tooltip("'아니오'를 눌렀을 때 실행할 분기 프로세스 (다음 단계로 진행하기 전에 실행)")]
        public ProcessData onNoProcess;
    }

    /// <summary>
    /// 플레이어에게 단계별 '예/아니오' 질문을 제시하며,
    /// '예'를 누르면 즉시 해당 Output을 실행하고 UI 전체를 비활성화하는 노드입니다.
    /// </summary>
    public class YesNoChoiceOutput : ProcessBase
    {
        [Header("1. UI & Canvas References")]
        [Tooltip("선택창 Canvas 전체 (할당 시 UI 전체가 완벽하게 꺼집니다)")]
        [SerializeField] private Canvas choiceCanvas;

        [Tooltip("선택지 UI 전체를 담고 있는 부모 패널")]
        [SerializeField] private GameObject choicePanel;

        [Tooltip("질문 문구를 띄울 TextMeshPro")]
        [SerializeField] private TextMeshProUGUI questionText;

        [Tooltip("버튼들이 돌아다닐 수 있는 활동 반경 (미지정 시 버튼의 부모 RectTransform 자동 사용)")]
        [SerializeField] private RectTransform buttonBoundaryArea;

        [Header("2. Yes Button Settings (빛나는 예 버튼)")]
        [SerializeField] private Button yesButton;
        [Tooltip("마우스를 올렸을 때 반짝이며 커지는 펄스 배율")]
        [SerializeField] private float yesHoverScale = 1.18f;
        [Tooltip("마우스를 올렸을 때 빛나는 틴트 색상")]
        [SerializeField] private Color yesGlowColor = new Color(1f, 0.92f, 0.4f, 1f);

        [Header("3. No Button Global Settings")]
        [SerializeField] private Button noButton;
        [Tooltip("도망칠 때 구역 가장자리 안쪽으로 남겨둘 패딩 여백")]
        [SerializeField] private float boundaryPadding = 40f;

        [Tooltip("마우스가 버튼 중심으로부터 이 거리(px) 안으로 들어오면 회피 시작 (60~100 권장)")]
        [SerializeField] private float detectionRadius = 80f;

        [Tooltip("한 번 도망친 후 멈칫하는 쿨타임 (초) - 이 틈을 노려 잡을 수 있음 (0.15~0.25 추천)")]
        [SerializeField] private float fleeCooldown = 0.2f;

        [Header("4. Sound Effects (Optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip yesHoverSound;
        [SerializeField] private AudioClip yesClickSound;
        [SerializeField] private AudioClip noEscapeSound;
        [SerializeField] private AudioClip noClickSound;

        [Header("5. Multi-Step Timeline (단계별 질문 & 난이도 설정)")]
        [SerializeField]
        private List<YesNoStep> questionSteps = new List<YesNoStep>()
        {
            new YesNoStep
            {
                stepName = "1차 확인 (가만히 있음)",
                questionText = "정말 이 제안을 수락하시겠습니까?",
                noButtonRunsAway = false,
                buttonScale = 1.0f,
                runSpeed = 0f
            },
            new YesNoStep
            {
                stepName = "2차 확인 (도망 시작 + 75% 크기)",
                questionText = "다시 한 번 여쭙겠습니다. 정말 아니오인가요?",
                noButtonRunsAway = true,
                buttonScale = 0.75f,
                runSpeed = 16f
            },
            new YesNoStep
            {
                stepName = "3차 확인 (초고속 도망 + 50% 콩알탄)",
                questionText = "잠깐만요, 진짜로 거절하실 생각인가요?!",
                noButtonRunsAway = true,
                buttonScale = 0.5f,
                runSpeed = 32f
            }
        };

        private int _currentStepIndex = 0;
        private ShinyHoverButton _shinyYes;
        private StepProgressiveRunawayButton _runawayNo;
        private Vector2 _originalNoButtonPos;
        private float _lastClickTime = -1f;

        private void Awake()
        {
            if (audioSource == null)
                audioSource = GetComponent<AudioSource>();

            if (buttonBoundaryArea == null && noButton != null)
                buttonBoundaryArea = noButton.transform.parent as RectTransform;

            SetupButtonComponents();

            // 시작 시 UI 전체 비활성화
            SetUIActive(false);

            IsOn = false;
        }

        private void SetupButtonComponents()
        {
            if (yesButton != null)
            {
                _shinyYes = yesButton.gameObject.GetComponent<ShinyHoverButton>();
                if (_shinyYes == null)
                    _shinyYes = yesButton.gameObject.AddComponent<ShinyHoverButton>();

                _shinyYes.Setup(yesHoverScale, yesGlowColor, () => PlaySound(yesHoverSound));
                yesButton.onClick.RemoveAllListeners();
                yesButton.onClick.AddListener(OnYesClicked);
            }

            if (noButton != null)
            {
                _originalNoButtonPos = (noButton.transform as RectTransform).anchoredPosition;

                _runawayNo = noButton.gameObject.GetComponent<StepProgressiveRunawayButton>();
                if (_runawayNo == null)
                    _runawayNo = noButton.gameObject.AddComponent<StepProgressiveRunawayButton>();

                _runawayNo.Setup(
                    noButton,
                    buttonBoundaryArea,
                    boundaryPadding,
                    detectionRadius,
                    fleeCooldown,
                    () => PlaySound(noEscapeSound),
                    OnNoClicked
                );

                noButton.onClick.RemoveAllListeners();
                noButton.onClick.AddListener(OnNoClicked);
            }
        }

        public override void Reset()
        {
            base.Reset();
            _currentStepIndex = 0;

            SetUIActive(false);
            ResetButtonPositions();
            IsOn = false;
        }

        public override void Execute()
        {
            if (questionSteps == null || questionSteps.Count == 0)
            {
                IsOn = true;
                return;
            }

            IsOn = false;
            _currentStepIndex = 0;

            SetUIActive(true);
            ApplyStep(_currentStepIndex);
        }

        private void ApplyStep(int index)
        {
            if (index >= questionSteps.Count)
            {
                FinishSequence();
                return;
            }

            YesNoStep step = questionSteps[index];

            if (questionText != null)
                questionText.text = step.questionText;

            ResetButtonPositions();

            if (_runawayNo != null)
            {
                _runawayNo.ApplyStepConfiguration(
                    step.noButtonRunsAway,
                    step.buttonScale,
                    step.runSpeed
                );
            }
        }

        // ★ 언제든 '예'를 누르면 즉시 해당 Output을 쏘고 UI를 모두 끄며 시퀀스 종료
        private void OnYesClicked()
        {
            PlaySound(yesClickSound);

            YesNoStep currentStep = (_currentStepIndex >= 0 && _currentStepIndex < questionSteps.Count)
                ? questionSteps[_currentStepIndex]
                : null;

            // 1. 해당 스텝에 등록된 onYesProcess 실행
            if (currentStep != null && currentStep.onYesProcess.process != null)
            {
                currentStep.onYesProcess.process.Reset();
                currentStep.onYesProcess.process.Execute();
            }

            // 2. 다음 질문으로 넘어가지 않고 즉시 패널/캔버스 비활성화 및 프로세스 완료 처리
            FinishSequence();
        }

        // '아니오'를 누르면 해당 스텝의 onNoProcess를 실행하고 다음 단계로 진행
        public void OnNoClicked()
        {
            if (Time.unscaledTime - _lastClickTime < 0.25f) return;
            _lastClickTime = Time.unscaledTime;

            PlaySound(noClickSound);

            YesNoStep currentStep = questionSteps[_currentStepIndex];

            if (currentStep.onNoProcess.process != null)
            {
                currentStep.onNoProcess.process.Reset();
                currentStep.onNoProcess.process.Execute();
            }

            _currentStepIndex++;
            if (_currentStepIndex < questionSteps.Count)
            {
                ApplyStep(_currentStepIndex);
            }
            else
            {
                FinishSequence();
            }
        }

        private void SetUIActive(bool active)
        {
            if (choiceCanvas != null)
                choiceCanvas.gameObject.SetActive(active);

            if (choicePanel != null)
                choicePanel.SetActive(active);
        }

        private void ResetButtonPositions()
        {
            if (noButton != null)
            {
                RectTransform rt = noButton.transform as RectTransform;
                rt.anchoredPosition = _originalNoButtonPos;
                if (_runawayNo != null) _runawayNo.ResetPosition(_originalNoButtonPos);
            }
        }

        private void FinishSequence()
        {
            // UI 캔버스 및 패널 모두 비활성화
            SetUIActive(false);

            // IsOn을 켜서 상위 시퀀스가 다음 노드로 진행할 수 있도록 플래그 설정
            IsOn = true;
        }

        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
        }
    }

    // =========================================================================
    // 마우스를 올리면 두근거리며 빛나는 '예' 버튼 헬퍼
    // =========================================================================
    public class ShinyHoverButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private Graphic _targetGraphic;
        private Color _originalColor;
        private Color _glowColor;
        private float _hoverScale;
        private Vector3 _originalScale;
        private Action _onHoverSound;
        private bool _isHovered = false;

        public void Setup(float hoverScale, Color glowColor, Action onHoverSound)
        {
            _hoverScale = hoverScale;
            _glowColor = glowColor;
            _onHoverSound = onHoverSound;
            _originalScale = transform.localScale;

            _targetGraphic = GetComponent<Graphic>();
            if (_targetGraphic != null)
                _originalColor = _targetGraphic.color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            _onHoverSound?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            transform.localScale = _originalScale;
            if (_targetGraphic != null) _targetGraphic.color = _originalColor;
        }

        private void Update()
        {
            if (!_isHovered) return;

            float pulse = Mathf.Sin(Time.unscaledTime * 8f) * 0.5f + 0.5f;
            transform.localScale = _originalScale * Mathf.Lerp(1.05f, _hoverScale, pulse);

            if (_targetGraphic != null)
            {
                _targetGraphic.color = Color.Lerp(_originalColor, _glowColor, pulse);
            }
        }
    }

    // =========================================================================
    // 질문 단계에 맞춰 크기와 속도가 조절되며, 마우스 누름 즉시 클릭 판정을 처리하는 아니오 버튼
    // =========================================================================
    public class StepProgressiveRunawayButton : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
    {
        private Button _button;
        private RectTransform _rect;
        private RectTransform _boundaryArea;
        private float _padding;
        private float _detectionRadius;
        private float _fleeCooldown;
        private Action _onEscapeSound;
        private Action _onDirectClick;

        private bool _runsAway = false;
        private float _speed = 15f;
        private float _targetScaleRatio = 1.0f;
        private Vector3 _initialBaseScale;
        private Vector2 _targetAnchoredPos;
        private float _cooldownTimer = 0f;

        public void Setup(
            Button button,
            RectTransform boundaryArea,
            float padding,
            float detectionRadius,
            float fleeCooldown,
            Action onEscapeSound,
            Action onDirectClick)
        {
            _button = button;
            _rect = transform as RectTransform;
            _boundaryArea = boundaryArea;
            _padding = padding;
            _detectionRadius = detectionRadius;
            _fleeCooldown = fleeCooldown;
            _onEscapeSound = onEscapeSound;
            _onDirectClick = onDirectClick;

            _initialBaseScale = _rect.localScale;
            _targetAnchoredPos = _rect.anchoredPosition;
        }

        public void ApplyStepConfiguration(bool runsAway, float scaleRatio, float speed)
        {
            _runsAway = runsAway;
            _targetScaleRatio = scaleRatio;
            _speed = speed;
            _cooldownTimer = 0f;
        }

        public void ResetPosition(Vector2 defaultPos)
        {
            _targetAnchoredPos = defaultPos;
            _cooldownTimer = 0f;
            if (_rect != null)
            {
                _rect.anchoredPosition = defaultPos;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button != null && _button.interactable)
            {
                _onDirectClick?.Invoke();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_runsAway || _cooldownTimer > 0f) return;
            FleeToFurthestPoint();
        }

        private void Update()
        {
            if (_rect == null) return;

            _rect.localScale = Vector3.Lerp(
                _rect.localScale,
                _initialBaseScale * _targetScaleRatio,
                Time.unscaledDeltaTime * 10f
            );

            if (_cooldownTimer > 0f)
            {
                _cooldownTimer -= Time.unscaledDeltaTime;
            }

            if (!_runsAway || _boundaryArea == null) return;

            Vector2 mouseScreenPos = Input.mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _boundaryArea,
                mouseScreenPos,
                null,
                out Vector2 mouseLocalPos
            );

            float distToMouse = Vector2.Distance(_rect.anchoredPosition, mouseLocalPos);
            if (_cooldownTimer <= 0f && distToMouse < _detectionRadius * _targetScaleRatio)
            {
                FleeToFurthestPoint();
            }

            _rect.anchoredPosition = Vector2.Lerp(
                _rect.anchoredPosition,
                _targetAnchoredPos,
                Time.unscaledDeltaTime * _speed
            );
        }

        private void FleeToFurthestPoint()
        {
            if (_boundaryArea == null || _rect == null) return;

            Rect boundary = _boundaryArea.rect;
            float currentWidth = _rect.rect.width * _targetScaleRatio;
            float currentHeight = _rect.rect.height * _targetScaleRatio;

            float halfW = currentWidth * 0.5f + _padding;
            float halfH = currentHeight * 0.5f + _padding;

            float minX = boundary.xMin + halfW;
            float maxX = boundary.xMax - halfW;
            float minY = boundary.yMin + halfH;
            float maxY = boundary.yMax - halfH;

            Vector2 mouseScreenPos = Input.mousePosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _boundaryArea,
                mouseScreenPos,
                null,
                out Vector2 mouseLocalPos
            );

            Vector2[] cornerCandidates = new Vector2[]
            {
                new Vector2(minX, minY),
                new Vector2(maxX, minY),
                new Vector2(minX, maxY),
                new Vector2(maxX, maxY),
                new Vector2(0f, minY),
                new Vector2(0f, maxY),
                new Vector2(minX, 0f),
                new Vector2(maxX, 0f)
            };

            Vector2 bestCandidate = cornerCandidates[0];
            float maxDistance = -1f;

            foreach (var pos in cornerCandidates)
            {
                float dist = Vector2.Distance(pos, mouseLocalPos);
                if (dist > maxDistance)
                {
                    maxDistance = dist;
                    bestCandidate = pos;
                }
            }

            _targetAnchoredPos = bestCandidate;
            _cooldownTimer = _fleeCooldown;
            _onEscapeSound?.Invoke();
        }
    }
}