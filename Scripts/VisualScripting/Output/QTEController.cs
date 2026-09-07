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
        // 싱글톤 제거로 씬에 다중 배치 가능

        [Header("UI Elements")]
        [SerializeField] private RectTransform targetImage; 
        [SerializeField] private RectTransform ringImage;   
        [SerializeField] private TextMeshProUGUI keyText;   
        [SerializeField] private Image resultImage; 

        [Header("Result Visuals")]
        [SerializeField] private Sprite successSprite; 
        [SerializeField] private Sprite failSprite;    
        [SerializeField] private float resultDisplayTime = 0.8f; 

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

            // 부모의 RectTransform 크기를 건드리지 않고 배율(Scale)만 늘려줍니다.
            ringImage.localScale = new Vector3(startScale, startScale, 1f);
            
            if (resultImage != null) resultImage.gameObject.SetActive(false);
            SetQTEBubblesActive(true);
            
            _isActive = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isActive) return;

            // 테두리 스케일 축소
            ringImage.localScale -= new Vector3(shrinkSpeed, shrinkSpeed, 0f) * Time.deltaTime;

            // 🆕 [비율 기반 변경] 테두리 배율(x)이 목표 배율(1.0)에서 허용 오차를 지나쳐 너무 작아지면 자동 실패
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
            // 🆕 [비율 기반 변경] 현재 테두리의 스케일 값 자체가 곧 목표(1.0) 대비 비율이 됩니다.
            float currentScaleRatio = ringImage.localScale.x;

            // 1.0f(원래 딱 맞는 크기)와의 절대적인 비율 차이(오차) 계산
            float diff = Mathf.Abs(currentScaleRatio - 1.0f);

            // 지정된 successWindow 비율 이내로 들어왔는지 판정
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
            StartCoroutine(ResultDisplayRoutine(isSuccess));
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
                    timer += Time.deltaTime;
                    
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
        [Tooltip("QTE 성공 시 재생할 SFX 키")]
        [SerializeField] private string successSoundKey = "QTE_Success";

        [Tooltip("QTE 실패 시 재생할 SFX 키")]
        [SerializeField] private string failSoundKey = "QTE_Fail";

        [Header("QTE Timing Settings")]
        [SerializeField] private float shrinkSpeed = 2.0f;
        [SerializeField] private float startScale = 3.0f;

        [Tooltip("원래 크기(1.0) 기준 허용 오차 비율 (0.15 = 15% 이내일 때 성공)")]
        [SerializeField] private float successWindow = 0.15f;

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

            // 1. 시간 초과 자동 실패
            if (ringImage.localScale.x < (1.0f - successWindow))
            {
                EndQTE(false);
                return;
            }

            // 2. 지정된 키 입력 시 판정
            if (Input.GetKeyDown(_targetKey))
            {
                CheckTiming();
            }
            // 3. 엉뚱한 다른 키를 눌렀을 때 즉시 실패
            else if (Input.anyKeyDown && !Input.GetMouseButtonDown(0) && !Input.GetMouseButtonDown(1))
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

            // SoundManager 풀링을 통해 전역에서 사운드 재생 (QTE 창이 꺼져도 소리는 끝까지 완주)
            string keyToPlay = isSuccess ? successSoundKey : failSoundKey;
            if (!string.IsNullOrEmpty(keyToPlay) && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySFX(keyToPlay);
            }

            StartCoroutine(ResultDisplayRoutine(isSuccess));
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

            // SoundManager가 소리를 쥐고 있으므로 UI 오브젝트를 바로 비활성화해도 무방
            gameObject.SetActive(false);

            if (isSuccess) _onSuccess?.Invoke();
            else _onFail?.Invoke();
        }
    }
}