using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting.UI
{
    /// <summary>
    /// 화면을 돌려보라는 안내를 위해 마우스 아이콘이 와이퍼처럼 좌우로 둥글게 왕복하는 UI 가이드 노드입니다.
    /// 특정 ProcessBase 노드가 완료되거나 플레이어가 마우스를 조작하면 자동으로 비활성화(Deactivate)됩니다.
    /// </summary>
    public class MouseLookGuideOutput : ProcessBase
    {
        [Header("1. UI References")]
        [Tooltip("가이드 UI 전체 루트 오브젝트 (미할당 시 본인 오브젝트 사용)")]
        [SerializeField] private GameObject guideRoot;

        [Tooltip("와이퍼처럼 움직일 마우스 아이콘 RectTransform")]
        [SerializeField] private RectTransform mouseIcon;

        [Tooltip("부드러운 페이드 인/아웃을 위한 CanvasGroup (없으면 자동 추가)")]
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("2. Wiper Motion Settings (와이퍼 연출)")]
        [Tooltip("좌우 왕복 속도 (높을수록 빠르게 휙휙 움직임)")]
        [SerializeField] private float wiperSpeed = 3.2f;

        [Tooltip("좌우 왕복 이동 반경 (픽셀 단위)")]
        [SerializeField] private float sweepDistanceX = 75f;

        [Tooltip("와이퍼 회전 각도 (Z축 회전 배율)")]
        [SerializeField] private float swingAngleZ = 22f;

        [Tooltip("와이퍼가 호를 그리며 움직이는 궤적 높이 (원호 형태 보정)")]
        [SerializeField] private float arcHeight = 16f;

        [Tooltip("게임 일시정지 중에도 애니메이션이 작동할지 여부")]
        [SerializeField] private bool useUnscaledTime = true;

        [Header("3. Auto Deactivate Settings (자동 종료 조건)")]
        [Tooltip("★ 이 프로세스 노드의 IsOn이 true가 되면 가이드가 자동으로 사라집니다.")]
        [SerializeField] private ProcessBase dismissTriggerNode;

        [Tooltip("플레이어가 실제로 마우스를 일정량 이상 움직여 시점을 돌리면 가이드를 자동 종료할지 여부")]
        [SerializeField] private bool deactivateOnMouseInput = true;

        [Tooltip("마우스 입력 감지 민감도 (픽셀 이동량)")]
        [SerializeField] private float mouseMovementThreshold = 35f;

        [Tooltip("사라질 때의 페이드아웃 시간 (초)")]
        [SerializeField] private float fadeDuration = 0.35f;

        [Header("4. Sequence Flow")]
        [Tooltip("체크 시: 가이드가 화면에 뜬 채로 즉시 다음 시퀀스로 넘어감 (인게임 조작 노드와 병렬 실행 시 권장)\n해제 시: 가이드가 완전히 사라질 때까지 다음 시퀀스를 대기함")]
        [SerializeField] private bool completeSequenceImmediately = true;

        private Vector2 _originalMousePos;
        private Quaternion _originalMouseRot;
        private Coroutine _motionCoroutine;
        private Coroutine _fadeCoroutine;
        private bool _isShowing = false;
        private bool _isDismissing = false;
        private float _accumulatedMouseDelta = 0f;

        private void Awake()
        {
            if (guideRoot == null) guideRoot = gameObject;
            if (canvasGroup == null && guideRoot != null)
            {
                canvasGroup = guideRoot.GetComponent<CanvasGroup>();
                if (canvasGroup == null) canvasGroup = guideRoot.AddComponent<CanvasGroup>();
            }

            if (mouseIcon != null)
            {
                _originalMousePos = mouseIcon.anchoredPosition;
                _originalMouseRot = mouseIcon.localRotation;
            }

            // 시작 시 숨김
            if (guideRoot != null) guideRoot.SetActive(false);
            IsOn = false;
        }

        public override void Reset()
        {
            base.Reset();
            StopAllGuideCoroutines();
            _isShowing = false;
            _isDismissing = false;
            _accumulatedMouseDelta = 0f;

            RestoreMouseTransform();
            if (guideRoot != null) guideRoot.SetActive(false);
            IsOn = false;
        }

        public override void Execute()
        {
            IsOn = false;
            _isDismissing = false;
            _accumulatedMouseDelta = 0f;

            if (guideRoot != null)
            {
                guideRoot.SetActive(true);
            }

            StopAllGuideCoroutines();
            _fadeCoroutine = StartCoroutine(FadeRoutine(0f, 1f, fadeDuration));
            _motionCoroutine = StartCoroutine(WiperMotionRoutine());
            _isShowing = true;

            if (completeSequenceImmediately)
            {
                IsOn = true;
            }
        }

        private void Update()
        {
            if (!_isShowing || _isDismissing) return;

            // 1. 연동된 특정 Output 노드가 완료(IsOn == true)되었는지 감지
            if (dismissTriggerNode != null && dismissTriggerNode.IsOn)
            {
                DismissGuide();
                return;
            }

            // 2. 플레이어의 실제 마우스 이동 입력 감지
            if (deactivateOnMouseInput)
            {
                float deltaX = Input.GetAxisRaw("Mouse X");
                float deltaY = Input.GetAxisRaw("Mouse Y");
                _accumulatedMouseDelta += Mathf.Abs(deltaX) + Mathf.Abs(deltaY);

                if (_accumulatedMouseDelta >= mouseMovementThreshold)
                {
                    DismissGuide();
                }
            }
        }

        /// <summary>
        /// 외부 이벤트나 다른 스크립트에서 수동으로 가이드를 닫을 때 호출 가능합니다.
        /// </summary>
        public void DismissGuide()
        {
            if (!_isShowing || _isDismissing) return;
            _isDismissing = true;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(DismissRoutine());
        }

        private IEnumerator WiperMotionRoutine()
        {
            float timer = 0f;

            while (true)
            {
                float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                timer += delta * wiperSpeed;

                if (mouseIcon != null)
                {
                    // 좌우 스윙 (-1 ~ 1)
                    float sin = Mathf.Sin(timer);

                    // 1. 좌우 위치 왕복
                    float posX = _originalMousePos.x + (sin * sweepDistanceX);

                    // 2. 자동차 와이퍼처럼 끝으로 갈수록 살짝 위/아래로 호를 그리는 Y축 연산
                    float arcOffset = (1f - Mathf.Abs(sin)) * arcHeight;
                    float posY = _originalMousePos.y + arcOffset;

                    mouseIcon.anchoredPosition = new Vector2(posX, posY);

                    // 3. 진행 방향에 맞춰 살짝 기울어지는 와이퍼 회전각 (Z축 각도)
                    float angle = -sin * swingAngleZ;
                    mouseIcon.localRotation = _originalMouseRot * Quaternion.Euler(0f, 0f, angle);
                }

                yield return null;
            }
        }

        private IEnumerator FadeRoutine(float from, float to, float duration)
        {
            if (canvasGroup == null) yield break;

            float elapsed = 0f;
            canvasGroup.alpha = from;

            while (elapsed < duration)
            {
                float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                elapsed += delta;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = to;
            _fadeCoroutine = null;
        }

        private IEnumerator DismissRoutine()
        {
            yield return FadeRoutine(canvasGroup != null ? canvasGroup.alpha : 1f, 0f, fadeDuration);

            StopAllGuideCoroutines();
            RestoreMouseTransform();

            if (guideRoot != null)
            {
                guideRoot.SetActive(false);
            }

            _isShowing = false;
            _isDismissing = false;

            if (!completeSequenceImmediately)
            {
                IsOn = true;
            }
        }

        private void StopAllGuideCoroutines()
        {
            if (_motionCoroutine != null)
            {
                StopCoroutine(_motionCoroutine);
                _motionCoroutine = null;
            }
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        }

        private void RestoreMouseTransform()
        {
            if (mouseIcon != null)
            {
                mouseIcon.anchoredPosition = _originalMousePos;
                mouseIcon.localRotation = _originalMouseRot;
            }
        }

        private void OnDisable()
        {
            Reset();
        }
    }
}