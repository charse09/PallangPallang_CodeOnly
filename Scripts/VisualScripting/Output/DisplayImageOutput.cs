using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 모니터 등 게임 내 오브젝트의 Canvas에 2D 이미지를 출력하는 모듈
    /// </summary>
    public class DisplayImageOutput : ProcessBase
    {
        [Header("Display Target")]
        [Tooltip("이미지를 띄울 UI Image 컴포넌트 (모니터 화면용 Canvas 내부에 있어야 함)")]
        [SerializeField] private Image targetScreenImage;

        [Header("Image Settings")]
        [Tooltip("표시할 이미지 (Sprite)")]
        [SerializeField] private Sprite imageToShow;

        [Tooltip("이미지의 불투명도 (0~1)")]
        [Range(0f, 1f)]
        [SerializeField] private float targetAlpha = 1f;

        [Header("Effect Settings")]
        [Tooltip("체크 시: 이미지가 나타날 때 지직거리며 켜지는 연출 추가")]
        [SerializeField] private bool useFlickerEffect = true;

        [SerializeField] private float flickerDuration = 0.5f;

        public override void Execute()
        {
            // 1. 실행 시작 시 IsOn을 false로 강제 초기화 (부모 Reset 대신 직접 처리)
            IsOn = false;

            if (targetScreenImage == null)
            {
                IsOn = true; // 대상이 없으면 즉시 종료하여 시퀀스 멈춤 방지
                return;
            }

            // 2. 이미지 교체 및 활성화
            if (imageToShow != null)
            {
                targetScreenImage.sprite = imageToShow;
            }

            // 3. 연출 분기
            if (useFlickerEffect)
            {
                StartCoroutine(DisplayRoutine());
            }
            else
            {
                ApplyFinalLook();
                IsOn = true;
            }
        }

        private IEnumerator DisplayRoutine()
        {
            targetScreenImage.gameObject.SetActive(true);
            float elapsed = 0f;

            // 지직거리는 깜빡임 효과
            while (elapsed < flickerDuration)
            {
                elapsed += Time.deltaTime;
                targetScreenImage.enabled = Random.value > 0.4f;
                yield return new WaitForSeconds(0.04f);
            }

            ApplyFinalLook();

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> 화면 출력 완료.");
#endif
            // 모든 연출이 끝났으므로 다음 시퀀스 노드로 신호를 보냄
            IsOn = true;
        }

        private void ApplyFinalLook()
        {
            targetScreenImage.enabled = true;
            targetScreenImage.gameObject.SetActive(true);

            Color c = targetScreenImage.color;
            c.a = targetAlpha;
            targetScreenImage.color = c;
        }

        // 특정 상황에서 수동으로 화면을 끄고 싶을 때를 위한 메서드 (필요시 호출)
        public void ClearScreen()
        {
            if (targetScreenImage != null)
            {
                targetScreenImage.gameObject.SetActive(false);
                IsOn = false;
            }
        }
    }
}