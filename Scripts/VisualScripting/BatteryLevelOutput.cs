using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    /// <summary>
    /// 하이어라키에 미리 배치된 배터리 단계별 게임오브젝트들을 SetActive로 켜고 끄며,
    /// 지정된 횟수와 시간 동안 지지직거리듯 깜빡이며 다음 단계로 닳게 만드는 Output 노드입니다.
    /// SoundManager와 연동하여 방전/글리치 효과음 및 배터리 부족 경고음을 함께 출력합니다.
    /// </summary>
    public class BatteryLevelOutput : ProcessBase
    {
        public enum BatteryLevel
        {
            Empty = 0,      // 0칸 (모두 OFF)
            Low = 1,        // 1칸 (battery_low)
            MiddleLow = 2,  // 2칸 (battery_middlelow)
            MiddleHigh = 3, // 3칸 (battery_middlehigh)
            High = 4        // 4칸 (battery_high)
        }

        public enum TransitionType
        {
            [Tooltip("출발 레벨에서 목표 레벨로 직접 지지직거리며 한 번에 전환")]
            Direct,

            [Tooltip("4 -> 3 -> 2 처럼 중간 단계들을 순차적으로 차례차례 깜빡이며 감소")]
            StepByStep
        }

        [Header("1. Pre-aligned Battery Objects (미리 배치한 오브젝트들)")]
        [Tooltip("4칸 완충 오브젝트 (battery_high)")]
        [SerializeField] private GameObject highObject;

        [Tooltip("3칸 오브젝트 (battery_middlehigh)")]
        [SerializeField] private GameObject middleHighObject;

        [Tooltip("2칸 오브젝트 (battery_middlelow)")]
        [SerializeField] private GameObject middleLowObject;

        [Tooltip("1칸 위험 오브젝트 (battery_low)")]
        [SerializeField] private GameObject lowObject;

        [Header("2. Level Range Settings (출발 ~ 도착 지점)")]
        [Tooltip("체크 시: 현재 게임에 남아있는 실제 배터리 잔량부터 시작\n해제 시: 아래 startLevel에 지정한 단계로 강제 시작")]
        [SerializeField] private bool useCurrentLevelAsStart = false;

        [Tooltip("출발 배터리 레벨 (어디서부터)")]
        [SerializeField] private BatteryLevel startLevel = BatteryLevel.High;

        [Tooltip("도착 배터리 레벨 (어디로)")]
        [SerializeField] private BatteryLevel targetLevel = BatteryLevel.MiddleLow;

        [Tooltip("전환 스타일: 한 번에 지지직 바꿀지 vs 한 단계씩 차례로 깎아 나갈지")]
        [SerializeField] private TransitionType transitionType = TransitionType.Direct;

        [Header("3. Flicker & Time Settings (깜빡임 횟수 및 시간)")]
        [Tooltip("★ 총 몇 초에 걸쳐 떨어질지 (전체 연출 시간)")]
        [Range(0.2f, 5.0f)]
        [SerializeField] private float transitionDuration = 1.0f;

        [Tooltip("★ 몇 번 깜빡거릴지 (깜빡임 횟수)")]
        [Range(2, 20)]
        [SerializeField] private int flickerCount = 6;

        [Tooltip("도착 후 1칸(Low) 상태일 때 경고용 무한 점멸을 계속 유지할지 여부")]
        [SerializeField] private bool keepWarningBlinkOnLow = false;

        [Header("4. Audio Settings (사운드 연동)")]
        [Tooltip("체크 시 배터리가 지지직거리며 닳을 때 효과음을 재생합니다.")]
        [SerializeField] private bool playFlickerSound = true;

        [Tooltip("방전/깜빡임 발생 시 재생할 SFX 키 (예: Battery_Discharge, Glitch_Spark 등)")]
        [SerializeField] private string flickerSoundKey = "Battery_Discharge";

        [Tooltip("체크 시 1칸(Low) 경고 점멸 루프 중 경고 비프음을 함께 반복 재생합니다.")]
        [SerializeField] private bool playWarningSoundOnLow = true;

        [Tooltip("배터리 부족 경고음 SFX 키 (예: Battery_Low_Warning, Warning_Beep 등)")]
        [SerializeField] private string lowWarningSoundKey = "Battery_Low_Warning";

        [Tooltip("효과음 재생 위치용 Transform (미할당 시 본인 오브젝트 위치)")]
        [SerializeField] private Transform soundTarget;

        // 전역 배터리 레벨 상태 기억 (기본값: 4 = High)
        public static BatteryLevel CurrentGlobalLevel { get; set; } = BatteryLevel.High;

        private Coroutine _transitionCoroutine;

        public override void Execute()
        {
            IsOn = false;

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }

            BatteryLevel from = useCurrentLevelAsStart ? CurrentGlobalLevel : startLevel;
            BatteryLevel to = targetLevel;

            ActivateOnly(from);

            _transitionCoroutine = StartCoroutine(MainTransitionRoutine(from, to));
        }

        private IEnumerator MainTransitionRoutine(BatteryLevel from, BatteryLevel to)
        {
            if (from == to)
            {
                ActivateOnly(to);
                CurrentGlobalLevel = to;
                IsOn = true;
                yield break;
            }

            // 방전 효과음 재생
            PlayFlickerSFX();

            if (transitionType == TransitionType.Direct)
            {
                yield return FlickerRoutine(from, to, flickerCount, transitionDuration);
            }
            else
            {
                int stepDirection = from > to ? -1 : 1;
                int totalSteps = Mathf.Abs((int)from - (int)to);

                float timePerStep = transitionDuration / totalSteps;
                int flickersPerStep = Mathf.Max(2, flickerCount / totalSteps);

                BatteryLevel currentStep = from;
                for (int i = 0; i < totalSteps; i++)
                {
                    // 단계별 전환 시에도 매 단계마다 사운드 재생
                    if (i > 0) PlayFlickerSFX();

                    BatteryLevel nextStep = currentStep + stepDirection;
                    yield return FlickerRoutine(currentStep, nextStep, flickersPerStep, timePerStep);
                    currentStep = nextStep;
                }
            }

            // 최종 완료: 목표 오브젝트만 활성화 확정
            ActivateOnly(to);
            CurrentGlobalLevel = to;

            // 1칸(Low) 도달 시 경고 루프 재생 여부
            if (to == BatteryLevel.Low && keepWarningBlinkOnLow)
            {
                _transitionCoroutine = StartCoroutine(LowBatteryWarningRoutine());
            }
            else
            {
                _transitionCoroutine = null;
            }

            // 시퀀스 완료 신호 출력
            IsOn = true;
        }

        private IEnumerator FlickerRoutine(BatteryLevel from, BatteryLevel to, int count, float duration)
        {
            float stepDelay = duration / (count * 2);

            for (int i = 0; i < count; i++)
            {
                ActivateOnly(to);
                yield return new WaitForSeconds(stepDelay);

                ActivateOnly(from);
                yield return new WaitForSeconds(stepDelay);
            }

            ActivateOnly(to);
        }

        private IEnumerator LowBatteryWarningRoutine()
        {
            while (true)
            {
                if (lowObject != null) lowObject.SetActive(false);
                yield return new WaitForSeconds(0.35f);

                if (lowObject != null) lowObject.SetActive(true);
                PlayWarningSFX(); // 점멸과 함께 경고 비프음 출력
                yield return new WaitForSeconds(0.35f);
            }
        }

        private void PlayFlickerSFX()
        {
            if (!playFlickerSound || string.IsNullOrEmpty(flickerSoundKey)) return;

            if (SoundManager.Instance != null)
            {
                Vector3 pos = (soundTarget != null) ? soundTarget.position : transform.position;
                SoundManager.Instance.PlaySFX(flickerSoundKey, pos);
            }
        }

        private void PlayWarningSFX()
        {
            if (!playWarningSoundOnLow || string.IsNullOrEmpty(lowWarningSoundKey)) return;

            if (SoundManager.Instance != null)
            {
                Vector3 pos = (soundTarget != null) ? soundTarget.position : transform.position;
                SoundManager.Instance.PlaySFX(lowWarningSoundKey, pos);
            }
        }

        /// <summary>
        /// 지정한 레벨의 오브젝트만 활성화(true)하고 나머지는 모두 비활성화(false)합니다.
        /// </summary>
        private void ActivateOnly(BatteryLevel level)
        {
            if (highObject != null) highObject.SetActive(level == BatteryLevel.High);
            if (middleHighObject != null) middleHighObject.SetActive(level == BatteryLevel.MiddleHigh);
            if (middleLowObject != null) middleLowObject.SetActive(level == BatteryLevel.MiddleLow);
            if (lowObject != null) lowObject.SetActive(level == BatteryLevel.Low);
        }

        public override void Reset()
        {
            base.Reset();
            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
                _transitionCoroutine = null;
            }
            IsOn = false;
        }

        public static void ResetBatteryToFull()
        {
            CurrentGlobalLevel = BatteryLevel.High;
        }
    }
}