using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;

namespace _Project.Scripts.VisualScripting
{
    [RequireComponent(typeof(AudioSource))]
    public class TimingHoldQTEController : MonoBehaviour
    {
        private enum QTEState
        {
            Inactive,
            WaitingForInput,
            Holding
        }

        [Header("UI Elements")]
        [Tooltip("QTE 진행 중 켜질 전체 부모 오브젝트 (결과 이미지는 가급적 이 밖에 두거나 켜져 있도록 설정하세요)")]
        [SerializeField] private GameObject qteContainer; 
        [SerializeField] private Image keyHintImage;
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private RectTransform barBackground;
        [SerializeField] private RectTransform targetZoneImage;
        [SerializeField] private RectTransform arrowImage;
        [SerializeField] private Image resultImage;

        [Header("Result Visuals")]
        [SerializeField] private Sprite successSprite;
        [SerializeField] private Sprite failSprite;
        [SerializeField] private float resultDisplayTime = 0.8f;
        [SerializeField] private float resultPunchScaleAmount = 1.4f;
        [SerializeField] private float resultTargetScaleMultiplier = 1.15f;

        [Header("Audio Effects")]
        [SerializeField] private AudioClip pressClip;
        [SerializeField] private AudioClip successClip;
        [SerializeField] private AudioClip failClip;

        private AudioSource _audioSource;
        private QTEState _currentState = QTEState.Inactive;

        private KeyCode _targetKey;
        private Action _onSuccess;
        private Action _onFail;

        private bool _hasWaitLimit;
        private float _waitLimitTime;
        private float _arrowMoveDuration;

        private float _idleTimer;
        private float _arrowProgress;

        private Vector3 _originalResultScale;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();

            if (resultImage != null) _originalResultScale = resultImage.rectTransform.localScale;

            gameObject.SetActive(false);
            if (resultImage != null) resultImage.gameObject.SetActive(false);
            SetQTEElementsActive(false);
        }

        public void SetupAndStart(
            KeyCode key, 
            Sprite inputSprite, 
            bool hasWaitLimit, 
            float waitLimitTime, 
            float arrowMoveDuration,
            Action onSuccess, 
            Action onFail)
        {
            _targetKey = key;
            if (keyText != null) keyText.text = key.ToString();

            if (keyHintImage != null)
            {
                if (inputSprite != null)
                {
                    keyHintImage.sprite = inputSprite;
                    keyHintImage.gameObject.SetActive(true);
                }
                else
                {
                    keyHintImage.gameObject.SetActive(false);
                }
            }

            _hasWaitLimit = hasWaitLimit;
            _waitLimitTime = waitLimitTime;
            _arrowMoveDuration = arrowMoveDuration;

            _onSuccess = onSuccess;
            _onFail = onFail;

            _idleTimer = 0f;
            _arrowProgress = 0f;

            UpdateArrowPosition(0f);

            if (resultImage != null)
            {
                resultImage.gameObject.SetActive(false);
                resultImage.rectTransform.localScale = _originalResultScale;
            }

            SetQTEElementsActive(true);
            
            _currentState = QTEState.WaitingForInput;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_currentState == QTEState.Inactive) return;

            if (_currentState == QTEState.WaitingForInput)
            {
                // 입력 대기 시간 체크
                if (_hasWaitLimit)
                {
                    _idleTimer += Time.deltaTime;
                    if (_idleTimer >= _waitLimitTime)
                    {
                        EndQTE(false); // 시간 초과 실패
                        return;
                    }
                }

                // 키 누름 감지
                if (Input.GetKeyDown(_targetKey))
                {
                    PlaySound(pressClip);
                    _currentState = QTEState.Holding;
                    _arrowProgress = 0f;
                }
            }
            else if (_currentState == QTEState.Holding)
            {
                // 키를 누르고 있는 동안 화살표 이동
                _arrowProgress += Time.deltaTime / _arrowMoveDuration;
                UpdateArrowPosition(_arrowProgress);

                // 화살표가 끝에 도달하면 실패 (시간 초과)
                if (_arrowProgress >= 1f)
                {
                    EndQTE(false);
                    return;
                }

                // 키 떼는 시점에 판정
                if (Input.GetKeyUp(_targetKey))
                {
                    CheckTiming();
                }
            }
        }

        private void UpdateArrowPosition(float progress)
        {
            if (arrowImage != null && barBackground != null)
            {
                // BarBackground의 로컬 좌표를 기준으로 왼쪽 끝에서 오른쪽 끝을 계산합니다.
                // 이 방식을 사용하면 에디터에서 Arrow를 어디에 자식으로 두든(단 Canvas 내부),
                // 실제 Bar의 왼쪽~오른쪽 경계를 정확히 따라갑니다.
                Vector3 barLeftWorld = barBackground.TransformPoint(new Vector3(barBackground.rect.xMin, 0, 0));
                Vector3 barRightWorld = barBackground.TransformPoint(new Vector3(barBackground.rect.xMax, 0, 0));
                
                // Y값과 Z값은 arrowImage 본래의 위치를 유지하도록 둡니다. (사용자가 Bar 밑에 배치하면 그 높이 그대로 이동)
                float currentWorldX = Mathf.Lerp(barLeftWorld.x, barRightWorld.x, progress);
                arrowImage.position = new Vector3(currentWorldX, arrowImage.position.y, arrowImage.position.z);
            }
        }

        private void CheckTiming()
        {
            if (targetZoneImage == null || arrowImage == null)
            {
                EndQTE(false);
                return;
            }

            // Arrow의 중심점(월드)이 TargetZone 범위(로컬 Rect 기준) 안에 들어왔는지 검사합니다.
            // 이렇게 하면 TargetZone을 Canvas 아무 곳에 배치하고 크기를 마음대로 바꿔도 정확히 판정합니다.
            Vector3 arrowWorldPos = arrowImage.position;
            Vector3 localPosInTargetZone = targetZoneImage.InverseTransformPoint(arrowWorldPos);
            
            // X축 기준 안에 들어왔는지 확인
            bool isInsideX = localPosInTargetZone.x >= targetZoneImage.rect.xMin && 
                             localPosInTargetZone.x <= targetZoneImage.rect.xMax;

            if (isInsideX)
            {
                EndQTE(true);
            }
            else
            {
                EndQTE(false);
            }
        }

        private void SetQTEElementsActive(bool active)
        {
            // qteContainer를 통째로 끄면 ResultImage까지 꺼질 수 있어서 
            // BarBackground와 Arrow, KeyHint 등 개별 UI만 꺼줍니다.
            if (barBackground != null) barBackground.gameObject.SetActive(active);
            if (targetZoneImage != null) targetZoneImage.gameObject.SetActive(active);
            if (arrowImage != null) arrowImage.gameObject.SetActive(active);
            if (keyHintImage != null) keyHintImage.gameObject.SetActive(active);
            if (keyText != null) keyText.gameObject.SetActive(active);
        }

        private void EndQTE(bool isSuccess)
        {
            _currentState = QTEState.Inactive;
            
            SetQTEElementsActive(false);
            
            PlaySound(isSuccess ? successClip : failClip);
            
            // 즉각적인 시퀀스 진행을 위해 애니메이션 코루틴 시작 전에 콜백을 먼저 쏴줍니다.
            if (isSuccess) _onSuccess?.Invoke();
            else _onFail?.Invoke();

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

            gameObject.SetActive(false);
        }
    }
}
