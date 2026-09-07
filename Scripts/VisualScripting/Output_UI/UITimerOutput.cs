using System.Collections;
using UnityEngine;
using TMPro; // TextMeshPro 라이브러리를 사용합니다.

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// UI 상에서 실시간으로 카운트다운을 시각화하고, 시간이 다 되면 완료 신호를 보내는 Output 모듈
    /// </summary>
    public class UITimerOutput : ProcessBase
    {
        [Header("UI References")]
        [Tooltip("시간을 표시할 TextMeshPro-UGUI 컴포넌트를 할당하세요.")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Timer Settings")]
        [Tooltip("카운트다운할 총 시간 (초 단위)")]
        [SerializeField] private float targetTime = 30f;

        [Tooltip("소수점 아래 몇 자리까지 표시할지 결정합니다. (0: 정수만, 1: 0.0초, 2: 0.00초)")]
        [Range(0, 2)]
        [SerializeField] private int decimalPlaces = 1;

        [Header("Urgent Juice Settings (긴박한 연출)")]
        [Tooltip("경고 상태로 전환될 남은 시간 (초)")]
        [SerializeField] private float dangerThreshold = 10f;

        [Tooltip("평상시 타이머 글자 색상")]
        [SerializeField] private Color normalColor = Color.white;

        [Tooltip("시간이 얼마 안 남았을 때의 경고 글자 색상")]
        [SerializeField] private Color dangerColor = Color.red;

        [Tooltip("체크 시: 경고 상태일 때 글자가 붉은색으로 깜빡거립니다.")]
        [SerializeField] private bool useUrgentBlink = true;

        [Tooltip("경고 상태일 때 글자가 깜빡이는 속도 (높을수록 빠름)")]
        [SerializeField] private float blinkSpeed = 5f;

        private float _currentTime;
        private bool _isTiming = false;
        private Coroutine _timerCoroutine;

        public override void Execute()
        {
            // 실행 시작 시 상태 초기화 (프레임워크 규칙)
            IsOn = false;

            if (timerText == null)
            {
                Debug.LogWarning($"[{gameObject.name}] TimerText 컴포넌트가 할당되지 않았습니다.");
                IsOn = true;
                return;
            }

            // 이미 타이머가 돌고 있다면 중복 방지를 위해 기존 코루틴 정지
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
            }

            _timerCoroutine = StartCoroutine(TimerRoutine());
        }

        private IEnumerator TimerRoutine()
        {
            _isTiming = true;
            _currentTime = targetTime;
            timerText.gameObject.SetActive(true);
            timerText.color = normalColor;

#if UNITY_EDITOR
            Debug.Log($"<color=orange>[{gameObject.name}]</color> 타이머 카운트다운 시작: {targetTime}초");
#endif

            while (_currentTime > 0f)
            {
                _currentTime -= Time.deltaTime;

                // 음수 방지 및 최종 고정
                if (_currentTime < 0f) _currentTime = 0f;

                // 1. 소수점 설정에 따른 시간 텍스트 포맷팅 (0 -> "0", 1 -> "0.0", 2 -> "0.00")
                timerText.text = _currentTime.ToString($"F{decimalPlaces}");

                // 2. 긴박한 상황 연출 (Danger Threshold 진입 시)
                if (_currentTime <= dangerThreshold)
                {
                    if (useUrgentBlink)
                    {
                        // 사인파(Sin)를 이용해 투명도(Alpha)를 조절하며 깜빡임 재현
                        float alpha = Mathf.Lerp(0.3f, 1f, (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f);
                        Color c = dangerColor;
                        c.a = alpha;
                        timerText.color = c;
                    }
                    else
                    {
                        timerText.color = dangerColor;
                    }
                }
                else
                {
                    timerText.color = normalColor;
                }

                yield return null;
            }

            // 타이머 종료 처리 (0.0 또는 0 표시 고정)
            timerText.text = (0f).ToString($"F{decimalPlaces}");
            _isTiming = false;
            _timerCoroutine = null;

            // =========================================================
            // ★ [프레임워크 규칙] 시간 초과! 나 다 끝났어 신호 발동
            // 이 신호가 켜져야 SequenceConditional이 다음 노드로 넘어갑니다.
            // =========================================================
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=red>[{gameObject.name}]</color> 타임아웃! 지정된 시간이 만료되었습니다.");
#endif
        }

        // 수동으로 타이머를 숨기거나 끄고 싶을 때 외부에 노출할 수 있는 함수 (선택 사항)
        public void StopAndHideTimer()
        {
            if (_timerCoroutine != null)
            {
                StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
            if (timerText != null)
            {
                timerText.gameObject.SetActive(false);
            }
            _isTiming = false;
            IsOn = false;
        }
    }
}