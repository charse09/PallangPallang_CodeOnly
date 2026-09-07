using System;
using UnityEngine;

namespace _Project.Scripts.VisualScripting.Output_UI
{
    public enum CutInEffectType
    {
        Cut,       // 별도 효과 없음
        FadeIn,    // 불투명도 증가
        Shake,     // 화면 흔들림
        Slide      // 미끄러지듯 등장
    }

    public enum SlideDirection
    {
        Top,
        Bottom,
        Left,
        Right
    }

    public enum ProgressionMode
    {
        WaitInput,    // 정지형: 입력 대기
        Timer,        // 실시간형: 일정 시간(ms) 후 자동 넘어감
        TimerOrInput  // 복합형: 타이머 만료 또는 사용자 입력 중 먼저 발생한 것으로 넘어감
    }

    public enum TextPanelDirection
    {
        Top,
        Bottom,
        Left,
        Right
    }

    [Serializable]
    public class CutInStepData
    {
        [Header("Image Settings")]
        public Sprite image;
        [Tooltip("화면 중앙 기준 미세 위치 조정")]
        public Vector2 imageOffset = Vector2.zero;

        [Header("Effects (동시 적용 가능)")]
        [Tooltip("Cut 효과는 아래 다른 효과를 체크하지 않았을 때 기본 적용됩니다.")]
        public bool useFadeIn;
        public float fadeDuration = 1f;

        public bool useShake;
        public float shakeDuration = 0.5f;
        public float shakeIntensity = 20f;

        public bool useSlide;
        public SlideDirection slideDirection = SlideDirection.Right;
        public float slideDuration = 1f;
        public AnimationCurve slideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Audio")]
        [Tooltip("이미지 등장 시 출력할 효과음 오디오 에셋 ID (비워두면 재생안함)")]
        public string sfxID;

        [Header("Progression")]
        public ProgressionMode progressionMode = ProgressionMode.WaitInput;
        [Tooltip("타이머 모드일 때 대기 시간(밀리초)")]
        public float timerDurationMs = 1500f;

        [Header("Dialogue Text")]
        [Tooltip("String Table의 ID (비워두면 텍스트 출력 안함)")]
        public string stringID;
        [Tooltip("캐릭터 명/대사창 등 텍스트 UI의 화면 위치 오프셋")]
        public Vector2 textOffset = Vector2.zero;

        [Header("TextPanel Dissolve / Transition Effects")]
        [Tooltip("TextPanel에 등장 디졸브/슬라이드 효과를 적용할지 여부")]
        public bool useTextEnterEffect = false;
        [Tooltip("등장 방향 (Top: 위에서 아래로, Bottom: 아래에서 위로, Left: 왼쪽에서 오른쪽으로, Right: 오른쪽에서 왼쪽으로)")]
        public TextPanelDirection textEnterDirection = TextPanelDirection.Bottom;
        [Tooltip("등장 시 살짝 이동할 거리(오프셋) 정도 (픽셀 단위, 예: 50~150)")]
        public float textEnterOffset = 80f;
        [Tooltip("등장 효과 지속 시간 (초)")]
        public float textEnterDuration = 0.4f;
        [Tooltip("등장 애니메이션 가속도 커브")]
        public AnimationCurve textEnterCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Space(5)]
        [Tooltip("TextPanel에 퇴장 디졸브/슬라이드 효과를 적용할지 여부")]
        public bool useTextExitEffect = false;
        [Tooltip("퇴장 방향 (Top: 위로 빠짐, Bottom: 아래로 빠짐, Left: 왼쪽으로 빠짐, Right: 오른쪽으로 빠짐)")]
        public TextPanelDirection textExitDirection = TextPanelDirection.Right;
        [Tooltip("퇴장 시 살짝 이동할 거리(오프셋) 정도 (픽셀 단위, 예: 50~150)")]
        public float textExitOffset = 80f;
        [Tooltip("퇴장 효과 지속 시간 (초)")]
        public float textExitDuration = 0.3f;
        [Tooltip("퇴장 애니메이션 가속도 커브")]
        public AnimationCurve textExitCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    }
}
