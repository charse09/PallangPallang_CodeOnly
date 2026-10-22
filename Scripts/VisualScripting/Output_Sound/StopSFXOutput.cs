using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 PlaySFXOutput 노드에서 재생 중인 효과음을 즉시 또는 페이드아웃으로 정지시키는 Output 노드
    /// </summary>
    public class StopSFXOutput : ProcessBase
    {
        [Header("Target Sound to Stop")]
        [Tooltip("정지시킬 대상 PlaySFXOutput 컴포넌트")]
        [SerializeField] private PlaySFXOutput targetPlaySFX;

        [Header("Fade Settings")]
        [Tooltip("체크 시 서서히 볼륨을 줄이며 부드럽게 정지합니다.")]
        [SerializeField] private bool useFadeOut = false;

        [Tooltip("페이드아웃 지속 시간 (초)")]
        [SerializeField] private float fadeDuration = 0.3f;

        [Header("Progression Settings")]
        [Tooltip("체크 시 페이드아웃 완료를 기다리지 않고 즉시 다음 프로세스로 넘어갑니다.")]
        [SerializeField] private bool runInBackground = false;

        public override void Execute()
        {
            if (IsOn) return;
            IsOn = false;

            if (targetPlaySFX == null)
            {
                Debug.LogWarning($"[{name}] 정지할 대상 PlaySFXOutput이 지정되지 않았습니다.");
                IsOn = true;
                return;
            }

            // 대상 노드에 정지 요청 전달
            targetPlaySFX.StopSound(useFadeOut, fadeDuration, OnStopCompleted);

            if (runInBackground)
            {
                IsOn = true;
            }
        }

        private void OnStopCompleted()
        {
            if (!runInBackground)
            {
                IsOn = true;
            }
        }

        public override void Reset()
        {
            base.Reset();
            IsOn = false;
        }
    }
}