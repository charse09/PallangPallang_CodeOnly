using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

namespace _Project.Scripts.VisualScripting
{
    [RequireComponent(typeof(AudioSource))]
    public class HoldQTEController : MonoBehaviour
    {
        [Header("UI Elements")]
        [Tooltip("Image Type이 'Filled'로 설정된 게이지 이미지를 넣으세요.")]
        [SerializeField] private Image fillGaugeImage;

        // A, D 등이 그려진 교체용 메인 이미지 (배경, 혹은 프레임 이미지 등)
        [Tooltip("노드에서 전달받은 이미지를 띄울 UI 컴포넌트입니다.")]
        [SerializeField] private Image qteVisualImage;

        [Tooltip("텍스트를 안 쓴다면 비워두세요(None).")]
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private Image resultImage;

        [Header("Result Visuals")]
        [SerializeField] private Sprite successSprite;
        [SerializeField] private Sprite failSprite;
        [SerializeField] private float resultDisplayTime = 0.8f;

        [Header("Audio Effects")]
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip failClip;

        [Header("Hold QTE Settings")]
        [SerializeField] private float fillSpeed = 0.5f;
        [SerializeField] private float dropSpeed = 0.5f;
        [SerializeField] private float timeLimit = 3.0f;

        public float ResultWeight { get; private set; }

        private List<KeyCode> _targetKeys;
        private bool _isActive = false;
        private float _currentFill = 0f;
        private float _timeElapsed = 0f;

        private Action _onSuccess;
        private Action _onFail;
        private AudioSource _audioSource;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            gameObject.SetActive(false);
            if (resultImage != null) resultImage.gameObject.SetActive(false);
        }

        // 매개변수에 Sprite(교체할 이미지) 추가
        public void SetupAndStart(List<KeyCode> keys, Sprite cueSprite, Action onSuccess, Action onFail)
        {
            _targetKeys = keys;

            // 전달받은 이미지가 있고, 할당된 Image 컴포넌트가 있다면 스프라이트 교체!
            if (qteVisualImage != null && cueSprite != null)
            {
                qteVisualImage.sprite = cueSprite;
            }

            if (keyText != null)
            {
                keyText.text = string.Join(" + ", _targetKeys);
            }

            _onSuccess = onSuccess;
            _onFail = onFail;

            _currentFill = 0f;
            _timeElapsed = 0f;
            ResultWeight = 0f;

            if (fillGaugeImage != null) fillGaugeImage.fillAmount = 0f;
            if (resultImage != null) resultImage.gameObject.SetActive(false);

            SetQTEBubblesActive(true);
            _isActive = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isActive) return;

            _timeElapsed += Time.unscaledDeltaTime;

            bool isHoldingAllKeys = true;
            foreach (var key in _targetKeys)
            {
                if (!Input.GetKey(key))
                {
                    isHoldingAllKeys = false;
                    break;
                }
            }

            if (isHoldingAllKeys)
            {
                _currentFill += fillSpeed * Time.unscaledDeltaTime;
            }
            else
            {
                _currentFill -= dropSpeed * Time.unscaledDeltaTime;
            }

            _currentFill = Mathf.Clamp01(_currentFill);
            if (fillGaugeImage != null) fillGaugeImage.fillAmount = _currentFill;

            if (_currentFill >= 1.0f)
            {
                EndQTE(true);
            }
            else if (_timeElapsed >= timeLimit)
            {
                EndQTE(false);
            }
        }

        private void SetQTEBubblesActive(bool active)
        {
            if (fillGaugeImage != null) fillGaugeImage.transform.parent.gameObject.SetActive(active);
            // 시각 이미지 끄기/켜기 동기화
            if (qteVisualImage != null) qteVisualImage.gameObject.SetActive(active);
            if (keyText != null) keyText.gameObject.SetActive(active);
        }

        private void EndQTE(bool isSuccess)
        {
            _isActive = false;
            SetQTEBubblesActive(false);
            ResultWeight = _currentFill;
            PlaySound(isSuccess ? successClip : failClip);
            StartCoroutine(ResultDisplayRoutine(isSuccess));
        }

        private void PlaySound(AudioClip clip)
        {
            if (_audioSource != null && clip != null)
            {
                _audioSource.PlayOneShot(clip);
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