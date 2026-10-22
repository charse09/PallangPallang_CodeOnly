/*
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace _Project.Scripts.VisualScripting
{
    public class QTEController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private RectTransform targetImage;
        [SerializeField] private RectTransform ringImage;
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private Image resultImage;

        [Header("Result Visuals")]
        [SerializeField] private Sprite successSprite;
        [SerializeField] private Sprite failSprite;
        [SerializeField] private float resultDisplayTime = 0.8f;

        [Header("SFX Table Sound Keys (SO_SFXTable의 soundKey와 일치)")]
        [Tooltip("QTE 키를 눌렀을 때(입력/타격 피드백) 1회 재생할 SFX 키")]
        [SerializeField] private string pressSoundKey = "QTE_Press";

        [Tooltip("QTE 성공 시 재생할 SFX 키")]
        [SerializeField] private string successSoundKey = "QTE_Success";

        [Tooltip("QTE 실패 시 재생할 SFX 키")]
        [SerializeField] private string failSoundKey = "QTE_Fail";

        [Header("QTE Timing Settings")]
        [SerializeField] private float shrinkSpeed = 2.0f;
        [SerializeField] private float startScale = 3.0f;

        [Tooltip("원래 크기(1.0) 기준 허용 오차 비율 (0.15 = 15% 이내일 때 성공)")]
        [SerializeField] private float successWindow = 0.15f;

        [Tooltip("체크 시 지정된 키 이외의 다른 키(W, A, S, D 등) 입력을 완전히 무시합니다.")]
        [SerializeField] private bool ignoreOtherKeys = true;

        private KeyCode _targetKey;
        private bool _isActive = false;

        private Action _onSuccess;
        private Action _onFail;

        private void Awake()
        {
            if (resultImage != null) resultImage.gameObject.SetActive(false);
            SetQTEBubblesActive(false);
            gameObject.SetActive(false);
        }

        public void SetupAndStart(KeyCode key, Action onSuccess, Action onFail)
        {
            _targetKey = key;
            if (keyText != null) keyText.text = key.ToString();

            _onSuccess = onSuccess;
            _onFail = onFail;

            ringImage.localScale = new Vector3(startScale, startScale, 1f);

            if (resultImage != null) resultImage.gameObject.SetActive(false);
            SetQTEBubblesActive(true);

            _isActive = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isActive) return;

            ringImage.localScale -= new Vector3(shrinkSpeed, shrinkSpeed, 0f) * Time.unscaledDeltaTime;

            // 1. 시간 초과 시 자동 실패
            if (ringImage.localScale.x < (1.0f - successWindow))
            {
                EndQTE(false);
                return;
            }

            // 2. 지정된 키 입력 시 피드백 사운드 및 타이밍 판정
            if (Input.GetKeyDown(_targetKey))
            {
                PlaySFX(pressSoundKey);
                CheckTiming();
            }
            // 3. 다른 키 입력 처리 (ignoreOtherKeys가 false일 때만 다른 키 입력 시 실패 처리)
            else if (!ignoreOtherKeys && Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
            {
                EndQTE(false);
            }
        }

        private void CheckTiming()
        {
            float currentScaleRatio = ringImage.localScale.x;
            float diff = Mathf.Abs(currentScaleRatio - 1.0f);

            bool isSuccess = diff <= successWindow;
            EndQTE(isSuccess);
        }

        private void SetQTEBubblesActive(bool active)
        {
            if (targetImage != null) targetImage.gameObject.SetActive(active);
            if (ringImage != null) ringImage.gameObject.SetActive(active);
            if (keyText != null) keyText.gameObject.SetActive(active);
        }

        private void EndQTE(bool isSuccess)
        {
            _isActive = false;
            SetQTEBubblesActive(false);

            // 결과 사운드 재생
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

        private IEnumerator ResultDisplayRoutine(bool isSuccess)
        {
            if (resultImage != null)
            {
                resultImage.sprite = isSuccess ? successSprite : failSprite;
                resultImage.gameObject.SetActive(true);

                Color c = resultImage.color;
                c.a = 1f;
                resultImage.color = c;

                float timer = 0f;
                while (timer < resultDisplayTime)
                {
                    timer += Time.unscaledDeltaTime;

                    if (timer > resultDisplayTime * 0.5f)
                    {
                        c.a = Mathf.Lerp(1f, 0f, (timer - resultDisplayTime * 0.5f) / (resultDisplayTime * 0.5f));
                        resultImage.color = c;
                    }
                    yield return null;
                }

                resultImage.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);

            if (isSuccess) _onSuccess?.Invoke();
            else _onFail?.Invoke();
        }
    }
}

*/

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace _Project.Scripts.VisualScripting
{
    public class QTEController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private RectTransform targetImage;
        [SerializeField] private RectTransform ringImage;
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private Image resultImage;

        [Header("Result Visuals")]
        [SerializeField] private Sprite successSprite;
        [SerializeField] private Sprite failSprite;
        [SerializeField] private float resultDisplayTime = 0.8f;

        [Header("SFX Table Sound Keys (SO_SFXTable의 soundKey와 일치)")]
        [Tooltip("QTE 키를 눌렀을 때(입력/타격 피드백) 1회 재생할 SFX 키")]
        [SerializeField] private string pressSoundKey = "QTE_Press";

        [Tooltip("QTE 성공 시 재생할 SFX 키")]
        [SerializeField] private string successSoundKey = "QTE_Success";

        [Tooltip("QTE 실패 시 재생할 SFX 키")]
        [SerializeField] private string failSoundKey = "QTE_Fail";

        [Header("QTE Timing Settings")]
        [SerializeField] private float shrinkSpeed = 2.0f;
        [SerializeField] private float startScale = 3.0f;

        [Tooltip("원래 크기(1.0) 기준 허용 오차 비율 (0.15 = 15% 이내일 때 성공)")]
        [SerializeField] private float successWindow = 0.15f;

        [Tooltip("체크 시 지정된 키 이외의 다른 키(W, A, S, D 등) 입력을 완전히 무시합니다.")]
        [SerializeField] private bool ignoreOtherKeys = true;

        [Header("Grace Period Settings (초반 입력 유예/무적)")]
        [Tooltip("체크 시 QTE 시작 직후 지정된 시간 동안 잘못 누르거나 너무 일찍 눌러도 실패 처리되지 않습니다.")]
        [SerializeField] private bool useGracePeriod = false;

        [Tooltip("초반 입력 유예 시간 (초). 기본값은 0초이며, 이 시간 동안 링은 줄어들지만 premature 입력으로 인한 실패가 무시됩니다.")]
        [Min(0f)]
        [SerializeField] private float gracePeriodDuration = 0f;

        private KeyCode _targetKey;
        private bool _isActive = false;
        private float _elapsedTimer = 0f;
        private bool _currentUseGrace = false;
        private float _currentGraceDuration = 0f;

        private Action _onSuccess;
        private Action _onFail;

        private void Awake()
        {
            if (resultImage != null) resultImage.gameObject.SetActive(false);
            SetQTEBubblesActive(false);
            gameObject.SetActive(false);
        }

        public void SetupAndStart(
            KeyCode key, 
            Action onSuccess, 
            Action onFail, 
            bool? overrideUseGrace = null, 
            float? overrideGraceDuration = null)
        {
            _targetKey = key;
            if (keyText != null) keyText.text = key.ToString();

            _onSuccess = onSuccess;
            _onFail = onFail;

            // QTEProcess에서 오버라이드한 값이 있으면 사용, 없으면 컨트롤러 인스펙터 설정 사용
            _currentUseGrace = overrideUseGrace ?? useGracePeriod;
            _currentGraceDuration = overrideGraceDuration ?? gracePeriodDuration;
            _elapsedTimer = 0f;

            ringImage.localScale = new Vector3(startScale, startScale, 1f);

            if (resultImage != null) resultImage.gameObject.SetActive(false);
            SetQTEBubblesActive(true);

            _isActive = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isActive) return;

            _elapsedTimer += Time.unscaledDeltaTime;
            ringImage.localScale -= new Vector3(shrinkSpeed, shrinkSpeed, 0f) * Time.unscaledDeltaTime;

            // 1. 시간 초과 시 자동 실패 (링이 타겟 원보다 너무 작아짐)
            if (ringImage.localScale.x < (1.0f - successWindow))
            {
                EndQTE(false);
                return;
            }

            // 현재 인식 유예(무적) 시간 상태인지 체크
            bool inGracePeriod = _currentUseGrace && (_elapsedTimer < _currentGraceDuration);

            // 2. 지정된 키 입력 시 피드백 사운드 및 타이밍 판정
            if (Input.GetKeyDown(_targetKey))
            {
                CheckTiming(inGracePeriod);
            }
            // 3. 다른 키 입력 처리 (ignoreOtherKeys가 false일 때)
            else if (!ignoreOtherKeys && Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
            {
                // 유예 시간 중이 아닐 때만 다른 키 입력으로 실패 처리
                if (!inGracePeriod)
                {
                    EndQTE(false);
                }
            }
        }

        private void CheckTiming(bool inGracePeriod)
        {
            float currentScaleRatio = ringImage.localScale.x;
            float diff = Mathf.Abs(currentScaleRatio - 1.0f);

            bool isSuccess = diff <= successWindow;

            if (isSuccess)
            {
                // 성공 타이밍이면 유예 시간과 상관없이 즉시 성공 인정
                PlaySFX(pressSoundKey);
                EndQTE(true);
            }
            else
            {
                // 유예 시간 중 너무 일찍 눌렀다면 실패 처리하지 않고 입력을 부드럽게 무시
                if (inGracePeriod)
                {
                    return;
                }

                // 유예 시간이 지난 상태에서 빗맞추면 실패
                PlaySFX(pressSoundKey);
                EndQTE(false);
            }
        }

        private void SetQTEBubblesActive(bool active)
        {
            if (targetImage != null) targetImage.gameObject.SetActive(active);
            if (ringImage != null) ringImage.gameObject.SetActive(active);
            if (keyText != null) keyText.gameObject.SetActive(active);
        }

        private void EndQTE(bool isSuccess)
        {
            _isActive = false;
            SetQTEBubblesActive(false);

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

        private IEnumerator ResultDisplayRoutine(bool isSuccess)
        {
            if (resultImage != null)
            {
                resultImage.sprite = isSuccess ? successSprite : failSprite;
                resultImage.gameObject.SetActive(true);

                Color c = resultImage.color;
                c.a = 1f;
                resultImage.color = c;

                float timer = 0f;
                while (timer < resultDisplayTime)
                {
                    timer += Time.unscaledDeltaTime;

                    if (timer > resultDisplayTime * 0.5f)
                    {
                        c.a = Mathf.Lerp(1f, 0f, (timer - resultDisplayTime * 0.5f) / (resultDisplayTime * 0.5f));
                        resultImage.color = c;
                    }
                    yield return null;
                }

                resultImage.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);

            if (isSuccess) _onSuccess?.Invoke();
            else _onFail?.Invoke();
        }
    }
}