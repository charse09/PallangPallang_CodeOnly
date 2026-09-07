using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 아이콘이 표시될 화면 상의 기준 위치를 정의합니다.
    /// </summary>
    public enum IconScreenPosition
    {
        Center,         // 화면 중앙
        TopLeft,        // 좌측 상단
        TopRight,       // 우측 상단
        BottomLeft,     // 좌측 하단
        BottomRight,    // 우측 하단
        TopCenter       // 상단 중앙 (경고문 등에 자주 쓰임)
    }

    /// <summary>
    /// 화면의 지정된 위치에 원하는 아이콘(Sprite)을 일정 시간 동안 출력하는 Output 모듈
    /// </summary>
    public class ShowIconOutput : ProcessBase
    {
        [Header("Icon Settings")]
        [Tooltip("화면에 띄울 아이콘 이미지를 넣으세요. (Sprite 타입이어야 합니다)")]
        [SerializeField] private Sprite iconSprite;

        [Tooltip("아이콘의 가로/세로 크기")]
        [SerializeField] private Vector2 iconSize = new Vector2(100f, 100f);

        [Tooltip("아이콘을 화면에 띄워둘 시간 (초)")]
        [SerializeField] private float duration = 2.0f;

        [Header("Position Settings")]
        [Tooltip("아이콘이 나타날 화면 기준 위치를 선택하세요.")]
        [SerializeField] private IconScreenPosition screenPosition = IconScreenPosition.Center;

        [Tooltip("기준 위치로부터의 미세 조정 값 (예: 우측 상단에서 X를 -50 하면 왼쪽으로 50만큼 이동)")]
        [SerializeField] private Vector2 positionOffset = Vector2.zero;

        [Header("Flow Settings")]
        [Tooltip("체크 시: 아이콘이 사라질 때까지 기다린 후 다음 노드를 실행합니다.\n체크 해제 시: 아이콘을 띄우자마자 즉시 다음 노드를 실행합니다.")]
        [SerializeField] private bool waitForFinish = false;

        private const string CanvasName = "GOS_GlobalIconCanvas";

        public override void Execute()
        {
            if (iconSprite == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"<color=red>[{gameObject.name}]</color> 출력할 아이콘(Sprite)이 할당되지 않았습니다!");
#endif
                return;
            }

            StartCoroutine(ShowIconRoutine());
        }

        private IEnumerator ShowIconRoutine()
        {
            // 아이콘 UI 컴포넌트 가져오기 (없으면 자동 생성)
            Image uiIcon = SetupIconUI();

            // 아이콘 이미지 및 크기 세팅
            uiIcon.sprite = iconSprite;
            uiIcon.rectTransform.sizeDelta = iconSize;

            // 앵커(위치) 세팅 적용
            ApplyScreenPosition(uiIcon.rectTransform);

            uiIcon.gameObject.SetActive(true);

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 화면 아이콘 출력: '{iconSprite.name}' ({duration}초)");
#endif

            // 대기하지 않는 모드라면 즉시 다음 노드로 신호 전달
            if (!waitForFinish)
            {
                IsOn = true;
            }

            // 지정된 시간만큼 대기
            yield return new WaitForSeconds(duration);

            // 다른 노드가 이 이미지를 덮어쓰지 않았을 때만 화면에서 지움
            if (uiIcon.sprite == iconSprite)
            {
                uiIcon.sprite = null;
                uiIcon.gameObject.SetActive(false);
            }

            // 대기 모드라면 아이콘이 지워진 후 다음 노드로 신호 전달
            if (waitForFinish)
            {
                IsOn = true;
#if UNITY_EDITOR
                Debug.Log($"<color=green>[{gameObject.name}]</color> 아이콘 출력 완료.");
#endif
            }
        }

        /// <summary>
        /// 씬에 글로벌 캔버스가 없으면 생성하고, Image 컴포넌트를 반환합니다.
        /// </summary>
        private Image SetupIconUI()
        {
            GameObject canvasObj = GameObject.Find(CanvasName);

            if (canvasObj == null)
            {
                canvasObj = new GameObject(CanvasName);
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 950; // 텍스트보다는 살짝 아래, 페이드보다는 위에 배치

                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                DontDestroyOnLoad(canvasObj);
            }

            Transform iconTransform = canvasObj.transform.Find("DisplayIcon");
            Image uiIcon = null;

            if (iconTransform != null)
            {
                uiIcon = iconTransform.GetComponent<Image>();
            }
            else
            {
                GameObject iconObj = new GameObject("DisplayIcon");
                iconObj.transform.SetParent(canvasObj.transform, false);

                uiIcon = iconObj.AddComponent<Image>();
                uiIcon.preserveAspect = true; // 이미지 비율이 찌그러지지 않도록 보호

                // 광클 시 클릭 방해를 막기 위해 레이캐스트 타겟 해제
                uiIcon.raycastTarget = false;
            }

            return uiIcon;
        }

        /// <summary>
        /// 선택한 Enum 값에 따라 RectTransform의 앵커와 피벗을 자동으로 설정합니다.
        /// </summary>
        private void ApplyScreenPosition(RectTransform rect)
        {
            Vector2 anchorValue = new Vector2(0.5f, 0.5f);

            switch (screenPosition)
            {
                case IconScreenPosition.Center:
                    anchorValue = new Vector2(0.5f, 0.5f);
                    break;
                case IconScreenPosition.TopLeft:
                    anchorValue = new Vector2(0f, 1f);
                    break;
                case IconScreenPosition.TopRight:
                    anchorValue = new Vector2(1f, 1f);
                    break;
                case IconScreenPosition.BottomLeft:
                    anchorValue = new Vector2(0f, 0f);
                    break;
                case IconScreenPosition.BottomRight:
                    anchorValue = new Vector2(1f, 0f);
                    break;
                case IconScreenPosition.TopCenter:
                    anchorValue = new Vector2(0.5f, 1f);
                    break;
            }

            // 앵커(기준점)와 피벗(중심점)을 동일하게 맞추어 위치 계산을 편하게 만듦
            rect.anchorMin = anchorValue;
            rect.anchorMax = anchorValue;
            rect.pivot = anchorValue;

            // 최종 위치 = 기준점 + 사용자가 입력한 오프셋
            rect.anchoredPosition = positionOffset;
        }
    }
}