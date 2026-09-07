using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 캔버스 UI(Image/Graphic)의 색상을 지정된 목표 색상으로 부드럽게 전환하는 아웃풋 구문입니다.
    /// </summary>
    public class CanvasImageColorChangeOutput : ProcessBase
    {
        [Header("References")]
        [Tooltip("색상을 변경할 UI Image 요소")]
        [SerializeField] private Image targetImage;

        [Header("Color Settings")]
        [Tooltip("최종 변경하고자 하는 목표 색상 (알파값 포함)")]
        [SerializeField] private Color targetColor = Color.white;

        [Tooltip("시작할 때 현재 UI의 색상부터 시작할지 여부.\n체크 해제 시 Custom Start Color에서 설정한 색상부터 시작합니다.")]
        [SerializeField] private bool useCurrentColorAsStart = true;

        [Tooltip("useCurrentColorAsStart가 false일 때 사용할 시작 색상")]
        [SerializeField] private Color customStartColor = Color.white;

        [Header("Transition Settings")]
        [Tooltip("색상 전환에 걸리는 시간 (초). 0 이하로 설정 시 즉시 변경됩니다.")]
        [SerializeField] private float duration = 1f;

        public override void Execute()
        {
            if (targetImage == null)
            {
                Debug.LogError($"<color=red>[{gameObject.name}] CanvasImageColorChangeOutput: " +
                               "TargetImage가 지정되지 않았습니다!</color>");
                IsOn = true;
                return;
            }

            StopAllCoroutines();

            if (duration <= 0f)
            {
                // 시간 설정이 0 이하이면 즉시 변경
                targetImage.color = targetColor;
                IsOn = true;
            }
            else
            {
                StartCoroutine(ChangeColorRoutine());
            }
        }

        private IEnumerator ChangeColorRoutine()
        {
            IsOn = false;

            float elapsed = 0f;
            Color startColor = useCurrentColorAsStart ? targetImage.color : customStartColor;

#if UNITY_EDITOR
            Debug.Log($"<color=lime>[{gameObject.name}]</color> [CanvasImageColorChangeOutput] " +
                      $"UI 색상 변경 시작. ({startColor} -> {targetColor})");
#endif

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 스무스 스텝을 사용하여 부드럽게 전환 (필요에 따라 Color.Lerp로 변경 가능)
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                targetImage.color = Color.Lerp(startColor, targetColor, smoothT);

                yield return null;
            }

            // 최종 색상 확정
            targetImage.color = targetColor;
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> [CanvasImageColorChangeOutput] " +
                      "UI 색상 변경 완료!");
#endif
        }
    }
}