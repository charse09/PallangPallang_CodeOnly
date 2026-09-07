using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace _Project.Scripts.VisualScripting
{
    [RequireComponent(typeof(AudioSource))]
    public class GaugeQTEController : MonoBehaviour
    {
        [Header("UI Elements")]
        [Tooltip("키 안내 이미지 (예: 스페이스바 아이콘)")]
        [SerializeField] private Image keyHintImage;
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private Image resultImage;

        [Header("Gauge UI Elements (핵심)")]
        [Tooltip("초록색 안전 영역 UI (스크립트가 크기와 위치를 자동 조절함)")]
        [SerializeField] private RectTransform safeZoneRect;
        [Tooltip("움직이는 화살표(커서) UI")]
        [SerializeField] private RectTransform cursorRect;

        [Header("Juiciness Settings (뽀용 연출)")]
        [SerializeField] private RectTransform punchTargetRect;
        [Tooltip("게이지가 차오를 때 전체 UI 창이 커질 최대 배율")]
        [SerializeField] private float maxGrowthMultiplier = 1.15f;
        [Tooltip("버튼을 뗄 때 순간 뻥튀기 배율")]
        [SerializeField] private float releasePunchScale = 1.3f;
        [SerializeField] private float punchDuration = 0.15f;

        [Header("Result Visuals")]
        [SerializeField] private Sprite successSprite;
        [SerializeField] private Sprite failSprite;
        [SerializeField] private float resultDisplayTime = 0.8f;
        [SerializeField] private float resultPunchScaleAmount = 1.4f;
        [SerializeField] private float resultTargetScaleMultiplier = 1.15f;

        [Header("Audio Effects")]
        [SerializeField] private AudioClip holdingClip; // 누르고 있을 때 재생할 소리 (선택)
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip failClip;

        private KeyCode _targetKey;
        private bool _isActive = false;
        private bool _isHolding = false;

        private float _currentProgress = 0f;
        private float _fillSpeed = 0.5f;
        private float _timeLimit = 4.0f;
        private float _timer = 0f;

        private float _safeZoneMin;
        private float _safeZoneMax;

        private Action _onSuccess;
        private Action _onFail;
        private AudioSource _audioSource;

        private Vector3 _originalWindowScale;
        private Coroutine _punchCoroutine;
        private Vector3 _originalResultScale;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (punchTargetRect == null) punchTargetRect = GetComponent<RectTransform>();
            _originalWindowScale = punchTargetRect.localScale;
            if (resultImage != null) _originalResultScale = resultImage.rectTransform.localScale;

            gameObject.SetActive(false);
            if (resultImage != null) resultImage.gameObject.SetActive(false);
            SetUIVisible(false);
        }

        public void SetupAndStart(KeyCode key, Sprite hintSprite, float safeMin, float safeMax, float speed, float timeLimit, Action onSuccess, Action onFail)
        {
            _targetKey = key;
            if (keyText != null) keyText.text = key.ToString();

            if (keyHintImage != null)
            {
                keyHintImage.sprite = hintSprite;
                keyHintImage.gameObject.SetActive(hintSprite != null);
            }

            _safeZoneMin = Mathf.Clamp01(safeMin);
            _safeZoneMax = Mathf.Clamp01(safeMax);
            _fillSpeed = speed;
            _timeLimit = timeLimit;

            _currentProgress = 0f;
            _timer = 0f;
            _isHolding = false;

            _onSuccess = onSuccess;
            _onFail = onFail;

            // Safe Zone UI 시각적 셋업 (비율에 맞춰 Anchor 조정)
            if (safeZoneRect != null)
            {
                safeZoneRect.anchorMin = new Vector2(_safeZoneMin, 0f);
                safeZoneRect.anchorMax = new Vector2(_safeZoneMax, 1f);
                safeZoneRect.anchoredPosition = Vector2.zero;
                safeZoneRect.sizeDelta = Vector2.zero;
            }

            UpdateCursorPosition();

            if (resultImage != null)
            {
                resultImage.gameObject.SetActive(false);
                resultImage.rectTransform.localScale = _originalResultScale;
            }

            punchTargetRect.localScale = _originalWindowScale;

            SetUIVisible(true);
            _isActive = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isActive) return;

            _timer += Time.unscaledDeltaTime;
            if (_timer >= _timeLimit)
            {
                EndQTE(false); // 시간 초과 실패
                return;
            }

            // 키를 누르고 있는 동안 게이지 상승
            if (Input.GetKey(_targetKey))
            {
                if (!_isHolding)
                {
                    _isHolding = true;
                    if (holdingClip != null) PlaySound(holdingClip);
                }

                _currentProgress += _fillSpeed * Time.unscaledDeltaTime;
                UpdateCursorPosition();

                // 텐션을 위해 게이지가 찰수록 전체 창이 서서히 커짐
                if (_punchCoroutine == null)
                {
                    Vector3 baseProgressScale = Vector3.Lerp(_originalWindowScale, _originalWindowScale * maxGrowthMultiplier, _currentProgress);
                    punchTargetRect.localScale = baseProgressScale;
                }

                // 화살표가 끝에 도달하면 즉시 실패 처리 (원하지 않는다면 이 부분 삭제 가능)
                if (_currentProgress >= 1f)
                {
                    EndQTE(false);
                    return;
                }
            }
            // 키를 떼는 순간 판정!
            else if (Input.GetKeyUp(_targetKey) && _isHolding)
            {
                _isHolding = false;
                bool isSuccess = (_currentProgress >= _safeZoneMin && _currentProgress <= _safeZoneMax);

                if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
                _punchCoroutine = StartCoroutine(ReleasePunchRoutine(isSuccess));

                return; // 코루틴 내에서 EndQTE가 호출되도록 넘김
            }
        }

        private void UpdateCursorPosition()
        {
            if (cursorRect != null)
            {
                // 커서의 X축 위치를 진행도(0~1)에 맞춰 업데이트
                cursorRect.anchorMin = new Vector2(_currentProgress, 0f);
                cursorRect.anchorMax = new Vector2(_currentProgress, 1f);
                cursorRect.anchoredPosition = Vector2.zero;
            }
        }

        // 버튼을 뗐을 때 순간적으로 강하게 튕기는 연출 후 결과 반환
        private IEnumerator ReleasePunchRoutine(bool isSuccess)
        {
            float t = 0f;
            Vector3 currentScale = punchTargetRect.localScale;
            Vector3 targetPunchScale = currentScale * releasePunchScale;

            while (t < punchDuration)
            {
                t += Time.unscaledDeltaTime;
                float normalizedTime = t / punchDuration;
                // 커졌다가 원래 크기로 축소
                punchTargetRect.localScale = Vector3.Lerp(targetPunchScale, _originalWindowScale, normalizedTime);
                yield return null;
            }

            punchTargetRect.localScale = _originalWindowScale;
            EndQTE(isSuccess);
        }

        private void EndQTE(bool isSuccess)
        {
            _isActive = false;
            _isHolding = false;

            if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
            punchTargetRect.localScale = _originalWindowScale;

            SetUIVisible(false);
            PlaySound(isSuccess ? successClip : failClip);
            StartCoroutine(ResultDisplayRoutine(isSuccess));
        }

        private void SetUIVisible(bool active)
        {
            if (keyHintImage != null) keyHintImage.gameObject.SetActive(active);
            if (keyText != null) keyText.gameObject.SetActive(active);
            if (safeZoneRect != null) safeZoneRect.gameObject.SetActive(active);
            if (cursorRect != null) cursorRect.gameObject.SetActive(active);
        }

        private void PlaySound(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
                _audioSource.PlayOneShot(clip);
        }

        // 기존의 멋진 도장 쾅! 연출 재사용
        private IEnumerator ResultDisplayRoutine(bool isSuccess)
        {
            if (resultImage != null)
            {
                resultImage.sprite = isSuccess ? successSprite : failSprite;
                resultImage.gameObject.SetActive(true);

                Vector3 peakPunchScale = _originalResultScale * resultPunchScaleAmount;
                Vector3 finalFadeScale = _originalResultScale * resultTargetScaleMultiplier;

                float t = 0f;
                while (t < resultDisplayTime)
                {
                    t += Time.deltaTime;
                    float normalizedTime = t / resultDisplayTime;

                    if (normalizedTime < 0.2f)
                    {
                        float punchProgress = normalizedTime / 0.2f;
                        resultImage.rectTransform.localScale = Vector3.Lerp(_originalResultScale, peakPunchScale, punchProgress);
                    }
                    else
                    {
                        float fadeProgress = (normalizedTime - 0.2f) / 0.8f;
                        resultImage.rectTransform.localScale = Vector3.Lerp(peakPunchScale, finalFadeScale, fadeProgress);
                    }

                    Color c = resultImage.color;
                    if (t > resultDisplayTime * 0.5f)
                    {
                        float fadeAlphaProgress = (t - resultDisplayTime * 0.5f) / (resultDisplayTime * 0.5f);
                        c.a = Mathf.Lerp(1f, 0f, fadeAlphaProgress);
                    }
                    else c.a = 1f;

                    resultImage.color = c;
                    yield return null;
                }

                resultImage.gameObject.SetActive(false);
                resultImage.rectTransform.localScale = _originalResultScale;
            }

            gameObject.SetActive(false);

            if (isSuccess) _onSuccess?.Invoke();
            else _onFail?.Invoke();
        }
    }
}