using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace _Project.Scripts.VisualScripting
{
    public class MTEController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image targetImage;
        [Tooltip("체크 해제(false) 시 MTE의 시작, 진행, 종료 등 모든 과정에서 targetImage가 비활성화되어 사라지지 않고 계속 켜져 있습니다.\n체크(true) 시 MTE 비활성화 및 종료 시 targetImage가 함께 꺼집니다.")]
        [SerializeField] private bool disableTargetImageOnEnd = true;

        [SerializeField] private Image gaugeFillImage;
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private Image resultImage;

        [Tooltip("게이지 채워지는 끝 지점에 표시할 핸들 이미지.\ngaugeFillImage와 같은 Canvas 안에 배치하고, Pivot을 (0.5, 0.5)로 설정하세요.")]
        [SerializeField] private RectTransform gaugeHandleImage;

        [Header("Juiciness Settings (뽀용 연출)")]
        [Tooltip("연타 시 팅겨 나갈 UI의 RectTransform을 넣으세요. (비워두면 이 스크립트가 붙은 오브젝트)")]
        [SerializeField] private RectTransform punchTargetRect;
        [Tooltip("버튼을 누를 때 순간적으로 뻥튀기될 크기 배율 (1.15면 15% 커짐)")]
        [SerializeField] private float punchScaleAmount = 1.15f;
        [Tooltip("원래 크기로 돌아오는 데 걸리는 시간 (작을수록 탱글탱글함)")]
        [SerializeField] private float punchDuration = 0.1f;

        [Tooltip("게이지가 100%에 도달했을 때 전체 UI 창이 최종적으로 몇 배까지 커질지 설정 (1.2면 원래 크기보다 20% 점점 커짐)")]
        [SerializeField] private float maxGrowthMultiplier = 1.25f;

        [Header("Result Visuals")]
        [SerializeField] private Sprite successSprite;
        [SerializeField] private Sprite failSprite;
        [SerializeField] private float resultDisplayTime = 0.8f;

        [Tooltip("결과 도장이 찍힐 때 순간 최대 뻥튀기될 크기 배율 (1.4면 순간 40% 커졌다가 팅기며 줄어듬)")]
        [SerializeField] private float resultPunchScaleAmount = 1.4f;
        [Tooltip("결과창이 팅기며 부드럽게 사라지는 동안 최종적으로 도달할 크기 배율 (1.15면 살짝 커진 상태로 소멸)")]
        [SerializeField] private float resultTargetScaleMultiplier = 1.15f;

        [Header("SFX Table Sound Keys (SO_SFXTable의 soundKey와 일치)")]
        [Tooltip("버튼 연타 시 재생할 SFX 키")]
        [SerializeField] private string mashSoundKey = "QTE_Mash";

        [Tooltip("MTE 성공 시 재생할 SFX 키")]
        [SerializeField] private string successSoundKey = "QTE_Success";

        [Tooltip("MTE 실패 시 재생할 SFX 키")]
        [SerializeField] private string failSoundKey = "QTE_Fail";

        private KeyCode _targetKey;
        private bool _isActive = false;

        private float _currentProgress = 0f;
        private float _mashPower = 0.08f;
        private float _drainSpeed = 0.2f;
        private float _timeLimit = 4.0f;
        private float _timer = 0f;

        private Action _onSuccess;
        private Action _onFail;

        private Vector3 _originalWindowScale;
        private Coroutine _punchCoroutine;
        private Vector3 _originalResultScale;

        // 핸들 위치 계산용 캐시
        private float _gaugeRadius;

        private void Awake()
        {
            if (punchTargetRect == null) punchTargetRect = GetComponent<RectTransform>();
            _originalWindowScale = punchTargetRect.localScale;

            if (resultImage != null) _originalResultScale = resultImage.rectTransform.localScale;

            if (gaugeFillImage != null)
                _gaugeRadius = gaugeFillImage.rectTransform.rect.width * 0.5f;

            gameObject.SetActive(false);
            if (resultImage != null) resultImage.gameObject.SetActive(false);
            SetMTEBubblesActive(false);
        }

        public void SetupAndStart(KeyCode key, Sprite inputSprite, float mashPower, float drainSpeed, float timeLimit, Action onSuccess, Action onFail)
        {
            _targetKey = key;
            if (keyText != null) keyText.text = key.ToString();

            if (targetImage != null)
            {
                if (inputSprite != null)
                {
                    targetImage.sprite = inputSprite;
                    targetImage.gameObject.SetActive(true);
                }
                else if (disableTargetImageOnEnd)
                {
                    targetImage.gameObject.SetActive(false);
                }
            }

            _mashPower = mashPower;
            _drainSpeed = drainSpeed;
            _timeLimit = timeLimit;

            _currentProgress = 0f;
            _timer = 0f;

            if (gaugeFillImage != null) gaugeFillImage.fillAmount = 0f;

            UpdateHandlePosition(0f);

            _onSuccess = onSuccess;
            _onFail = onFail;

            if (resultImage != null)
            {
                resultImage.gameObject.SetActive(false);
                resultImage.rectTransform.localScale = _originalResultScale;
            }

            punchTargetRect.localScale = _originalWindowScale;

            SetMTEBubblesActive(true);
            _isActive = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isActive) return;

            _timer += Time.unscaledDeltaTime;
            if (_timer >= _timeLimit)
            {
                EndMTE(false);
                return;
            }

            _currentProgress -= _drainSpeed * Time.unscaledDeltaTime;
            _currentProgress = Mathf.Clamp01(_currentProgress);

            if (Input.GetKeyDown(_targetKey))
            {
                PlaySFX(mashSoundKey);

                _currentProgress += _mashPower;
                _currentProgress = Mathf.Clamp01(_currentProgress);

                if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
                _punchCoroutine = StartCoroutine(PunchScaleRoutine());

                if (_currentProgress >= 1f)
                {
                    EndMTE(true);
                    return;
                }
            }

            if (gaugeFillImage != null)
            {
                gaugeFillImage.fillAmount = _currentProgress;
            }

            UpdateHandlePosition(_currentProgress);

            if (_punchCoroutine == null)
            {
                Vector3 baseProgressScale = Vector3.Lerp(_originalWindowScale, _originalWindowScale * maxGrowthMultiplier, _currentProgress);
                punchTargetRect.localScale = baseProgressScale;
            }
        }

        private IEnumerator PunchScaleRoutine()
        {
            float t = 0f;

            Vector3 currentBaseScale = Vector3.Lerp(_originalWindowScale, _originalWindowScale * maxGrowthMultiplier, _currentProgress);
            Vector3 targetPunchScale = currentBaseScale * punchScaleAmount;
            punchTargetRect.localScale = targetPunchScale;

            while (t < punchDuration)
            {
                t += Time.unscaledDeltaTime;
                float normalizedTime = t / punchDuration;
                punchTargetRect.localScale = Vector3.Lerp(targetPunchScale, currentBaseScale, normalizedTime);
                yield return null;
            }

            punchTargetRect.localScale = currentBaseScale;
            _punchCoroutine = null;
        }

        private void EndMTE(bool isSuccess)
        {
            _isActive = false;

            if (_punchCoroutine != null) StopCoroutine(_punchCoroutine);
            punchTargetRect.localScale = _originalWindowScale;

            SetMTEBubblesActive(false);

            // 성공/실패에 맞는 키를 사운드 매니저에 전달
            string keyToPlay = isSuccess ? successSoundKey : failSoundKey;
            PlaySFX(keyToPlay);

            StartCoroutine(ResultDisplayRoutine(isSuccess));
        }

        private void PlaySFX(string soundKey)
        {
            if (!string.IsNullOrEmpty(soundKey) && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(soundKey);
            }
        }

        private void UpdateHandlePosition(float fillAmount)
        {
            if (gaugeHandleImage == null || gaugeFillImage == null) return;

            float originAngle;
            switch ((int)gaugeFillImage.fillOrigin)
            {
                case 0: originAngle = -90f; break;
                case 1: originAngle = 0f; break;
                case 2: originAngle = 90f; break;
                case 3: originAngle = 180f; break;
                default: originAngle = 90f; break;
            }

            float direction = gaugeFillImage.fillClockwise ? -1f : 1f;
            float angleDeg = originAngle + direction * fillAmount * 360f;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector2 handleLocalPos = new Vector2(
                Mathf.Cos(angleRad) * _gaugeRadius,
                Mathf.Sin(angleRad) * _gaugeRadius
            );

            RectTransform gaugeRect = gaugeFillImage.rectTransform;
            Vector3 worldPos = gaugeRect.TransformPoint(handleLocalPos);
            gaugeHandleImage.position = worldPos;

            gaugeHandleImage.rotation = Quaternion.Euler(0f, 0f, angleDeg - 90f);
        }

        private void SetMTEBubblesActive(bool active)
        {
            if (targetImage != null)
            {
                if (active || !disableTargetImageOnEnd)
                {
                    targetImage.gameObject.SetActive(true);
                }
                else
                {
                    targetImage.gameObject.SetActive(false);
                }
            }

            if (gaugeFillImage != null) gaugeFillImage.gameObject.SetActive(active);
            if (keyText != null) keyText.gameObject.SetActive(active);
            if (gaugeHandleImage != null) gaugeHandleImage.gameObject.SetActive(active);
        }

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
                    t += Time.unscaledDeltaTime;
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
                    else
                    {
                        c.a = 1f;
                    }
                    resultImage.color = c;

                    yield return null;
                }

                resultImage.gameObject.SetActive(false);
                resultImage.rectTransform.localScale = _originalResultScale;
            }

            if (targetImage != null && disableTargetImageOnEnd)
            {
                targetImage.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);

            if (isSuccess) _onSuccess?.Invoke();
            else _onFail?.Invoke();
        }
    }
}