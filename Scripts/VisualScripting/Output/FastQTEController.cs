using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace _Project.Scripts.VisualScripting
{
    public class FastQTEController : MonoBehaviour
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
        [Tooltip("Fast QTE 성공 시 재생할 SFX 키")]
        [SerializeField] private string successSoundKey = "QTE_Success";

        [Tooltip("Fast QTE 실패 시 재생할 SFX 키")]
        [SerializeField] private string failSoundKey = "QTE_Fail";

        [Header("QTE Settings")]
        [SerializeField] private float shrinkSpeed = 2.0f;
        [SerializeField] private float startScale = 3.0f;

        [Tooltip("원래 크기(1.0) 기준 허용할 오차 비율 (0.15 = 원래 크기의 전후 15% 이내일 때 성공)")]
        [SerializeField] private float successWindow = 0.15f;

        private KeyCode _targetKey;
        private bool _isActive = false;

        private Action _onSuccess;
        private Action _onFail;

        private void Awake()
        {
            gameObject.SetActive(false);
            if (resultImage != null) resultImage.gameObject.SetActive(false);
            SetQTEBubblesActive(false);
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

            if (ringImage.localScale.x < (1.0f - successWindow))
            {
                EndQTE(false);
            }

            if (Input.GetKeyDown(_targetKey))
            {
                CheckTiming();
            }
        }

        private void CheckTiming()
        {
            float currentScaleRatio = ringImage.localScale.x;
            float diff = Mathf.Abs(currentScaleRatio - 1.0f);

            if (diff <= successWindow)
            {
                EndQTE(true);
            }
            else
            {
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

            // SoundManager를 통해 전역 풀링 사운드 재생
            string keyToPlay = isSuccess ? successSoundKey : failSoundKey;
            PlaySFX(keyToPlay);

            // 빠른 시퀀스 전환을 위해 콜백 즉시 호출 (기존 로직 보존)
            if (isSuccess) _onSuccess?.Invoke();
            else _onFail?.Invoke();

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
        }
    }
}