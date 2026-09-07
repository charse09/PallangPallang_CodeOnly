using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 줄타기 균형 UI 컴포넌트
/// Canvas 위에 배치하여 현재 균형 상태를 시각적으로 표시합니다.
/// 
/// [선택 사항] - 없어도 RopeBalanceGimmick은 정상 동작합니다.
/// 
/// [세팅 방법]
/// 1. Canvas 하위에 빈 GameObject를 만들고 RopeBalanceUI 컴포넌트 부착
/// 2. BalanceBar (Image), BalanceIndicator (Image) 연결
/// 3. BalanceBar: 가로로 긴 슬라이더 배경 이미지
/// 4. BalanceIndicator: 현재 균형 위치를 나타내는 작은 원 이미지
/// </summary>
public class RopeBalanceUI : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("균형 바 배경 Image (RectTransform 기준)")]
    [SerializeField] private RectTransform balanceBar;

    [Tooltip("균형 위치 인디케이터 (작은 원)")]
    [SerializeField] private RectTransform balanceIndicator;

    [Tooltip("인디케이터 색 - 안전 구간")]
    [SerializeField] private Color safeColor = new Color(0.3f, 0.9f, 0.4f);

    [Tooltip("인디케이터 색 - 위험 구간")]
    [SerializeField] private Color dangerColor = new Color(0.95f, 0.25f, 0.2f);

    [Tooltip("인디케이터 Image 컴포넌트 (색 변경용)")]
    [SerializeField] private Image indicatorImage;

    private CanvasGroup canvasGroup;
    private float barHalfWidth;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;

        if (balanceBar != null)
            barHalfWidth = balanceBar.rect.width * 0.5f;
    }

    public void Show()
    {
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    /// <summary>balance: -1(왼쪽)~0(중앙)~+1(오른쪽)</summary>
    public void UpdateBalance(float balance)
    {
        if (balanceIndicator == null || balanceBar == null) return;

        // 인디케이터 X 위치 갱신
        float xPos = balance * barHalfWidth;
        balanceIndicator.anchoredPosition = new Vector2(xPos, balanceIndicator.anchoredPosition.y);

        // 위험도에 따라 색 변경
        if (indicatorImage != null)
        {
            float danger = Mathf.Abs(balance); // 0=안전, 1=위험
            indicatorImage.color = Color.Lerp(safeColor, dangerColor, danger);
        }
    }
}
