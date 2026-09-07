using UnityEngine;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    public enum GuideActionType
    {
        Show, // 퀘스트 가이드 화면 표시 및 유지 (Activate)
        Hide  // 퀘스트 가이드 퇴장 및 비활성화 (Deactivate)
    }

    /// <summary>
    /// 프레임워크 신호에 맞춰 퀘스트 가이드를 화면에 띄워 유지하거나(Show),
    /// 원하는 특정 시점에 페이드아웃과 함께 닫는(Hide) 전용 Output 모듈
    /// </summary>
    public class QuestGuideOutput : ProcessBase
    {
        [Header("Guide Action")]
        [Tooltip("Show: 가이드를 화면에 띄우고 유지합니다.\nHide: 화면에 떠 있는 가이드를 닫습니다.")]
        [SerializeField] private GuideActionType actionType = GuideActionType.Show;

        [Header("Manager Settings")]
        [Tooltip("비워둘 경우 씬 내 비활성화된 매니저까지 자동 탐색합니다.")]
        [SerializeField] private SimpleCutInManager manager;

        [Header("Show Settings (Show 선택 시 적용)")]
        [SerializeField] private CutInStringTable stringTable;
        [SerializeField] private QuestGuideData guideData = new QuestGuideData();

        [Header("Hide Settings (Hide 선택 시 적용)")]
        [Tooltip("체크 시 가이드가 사라질 때 서서히 투명해지며 닫힙니다.")]
        [SerializeField] private bool useFadeOut = true;

        [Tooltip("서서히 사라지는 페이드아웃 시간(초)")]
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("Progression Settings")]
        [Tooltip("체크 시 등장/퇴장 페이드 연출 완료를 기다리지 않고 즉시 다음 노드로 진행합니다.")]
        [SerializeField] private bool runInBackground = false;

        private void Awake()
        {
            FindManagerIfNull();
        }

        public override void Execute()
        {
            if (IsOn) return;

            FindManagerIfNull();

            if (manager == null)
            {
                Debug.LogError($"[{gameObject.name}] SimpleCutInManager를 찾을 수 없습니다.");
                IsOn = true;
                return;
            }

            IsOn = false;

            if (actionType == GuideActionType.Show)
            {
                // 불필요한 Progression을 제외한 데이터를 매니저로 전달
                manager.ShowGuide(guideData.ToCutInStepData(), stringTable, OnCompleted);
            }
            else
            {
                // 지정된 FadeOut 설정으로 가이드 닫기
                manager.HideGuide(useFadeOut, fadeOutDuration, OnCompleted);
            }

            if (runInBackground)
            {
                IsOn = true;
            }
        }

        private void FindManagerIfNull()
        {
            if (manager == null)
            {
                manager = FindFirstObjectByType<SimpleCutInManager>(FindObjectsInactive.Include);
            }
        }

        private void OnCompleted()
        {
            if (!runInBackground)
            {
                IsOn = true;
            }
        }
    }

    /// <summary>
    /// Progression(타이머/입력 대기) 필드를 제거한 퀘스트 가이드 전용 인스펙터 데이터 구조
    /// </summary>
    [System.Serializable]
    public class QuestGuideData
    {
        [Header("Image Settings")]
        public Sprite image;
        public Vector2 imageOffset = Vector2.zero;

        [Header("Effects")]
        public bool useFadeIn = true;
        public float fadeInDuration = 0.5f;

        public bool useSlide = false;
        public SlideDirection slideDirection = SlideDirection.Right;
        public float slideDuration = 0.5f;
        public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public bool useShake = false;
        public float shakeDuration = 0.5f;
        public float shakeIntensity = 20f;

        [Header("Audio")]
        [Tooltip("등장 시 출력할 효과음 오디오 에셋 ID")]
        public string sfxID;

        [Header("Dialogue Text")]
        public string stringID;
        public Vector2 textOffset = Vector2.zero;

        [Header("Text Enter Effects")]
        public bool useTextEnterEffect = false;
        public TextPanelDirection textEnterDirection = TextPanelDirection.Bottom;
        public float textEnterOffset = 80f;
        public float textEnterDuration = 0.4f;
        public AnimationCurve textEnterCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public CutInStepData ToCutInStepData()
        {
            return new CutInStepData
            {
                image = this.image,
                imageOffset = this.imageOffset,
                useFadeIn = this.useFadeIn,
                fadeDuration = this.fadeInDuration,
                useSlide = this.useSlide,
                slideDirection = this.slideDirection,
                slideDuration = this.slideDuration,
                slideCurve = this.slideCurve,
                useShake = this.useShake,
                shakeDuration = this.shakeDuration,
                shakeIntensity = this.shakeIntensity,
                sfxID = this.sfxID,
                stringID = this.stringID,
                textOffset = this.textOffset,
                useTextEnterEffect = this.useTextEnterEffect,
                textEnterDirection = this.textEnterDirection,
                textEnterOffset = this.textEnterOffset,
                textEnterDuration = this.textEnterDuration,
                textEnterCurve = this.textEnterCurve,
                // 프레임워크 유지형이므로 스텝 내부 대기 비활성화
                progressionMode = ProgressionMode.WaitInput
            };
        }
    }
}