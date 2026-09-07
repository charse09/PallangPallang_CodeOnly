using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MenuSelectionHandler : MonoBehaviour
{
    [Header("--- UI Elements ---")]
    [SerializeField] private RectTransform selectionRing; // 뒤따라올 테두리 이미지
    [SerializeField] private float smoothSpeed = 15f;    // 이동 부드러움 계수

    private RectTransform targetRect;
    private CanvasGroup ringCanvasGroup;

    void Awake()
    {
        // 링 이미지를 제어하기 위해 CanvasGroup을 가져오거나 추가
        ringCanvasGroup = selectionRing.GetComponent<CanvasGroup>();
        if (ringCanvasGroup == null) ringCanvasGroup = selectionRing.gameObject.AddComponent<CanvasGroup>();

        selectionRing.gameObject.SetActive(false);
    }

    void Update()
    {
        // 타겟 버튼 쪽으로 부드럽게 이동 (Time.unscaledDeltaTime 사용하여 게임 일시정지 중에도 정상 작동)
        if (targetRect != null)
        {
            selectionRing.position = Vector3.Lerp(selectionRing.position, targetRect.position, Time.unscaledDeltaTime * smoothSpeed);
            selectionRing.sizeDelta = Vector2.Lerp(selectionRing.sizeDelta, targetRect.sizeDelta * 1.2f, Time.unscaledDeltaTime * smoothSpeed);
        }
    }

    // 버튼에 PointerEnter 또는 Select 이벤트가 발생했을 때 호출할 함수
    public void OnButtonFocused(BaseEventData eventData)
    {
        GameObject selectedObj = eventData.selectedObject;
        if (selectedObj == null && eventData is PointerEventData pEvent) selectedObj = pEvent.pointerEnter;

        if (selectedObj != null)
        {
            targetRect = selectedObj.GetComponent<RectTransform>();
            selectionRing.gameObject.SetActive(true);
        }
    }

    // 마우스가 버튼을 벗어났을 때
    public void OnButtonUnfocused()
    {
    }
}