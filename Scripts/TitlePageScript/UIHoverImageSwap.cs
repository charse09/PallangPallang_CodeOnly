using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class UIHoverImageSwap : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("타겟 이미지 설정")]
    [Tooltip("스프라이트를 변경할 타겟 Image (비워둘 경우 자기 자신 또는 자식 오브젝트의 Image를 자동으로 찾습니다)")]
    public Image targetImage;

    [Header("호버 감지 트리거 설정")]
    [Tooltip("부모 오브젝트(예: GameStartButton) 위에 마우스를 올려도 호버가 발동할지 여부")]
    public bool triggerOnParentHover = true;

    [Tooltip("호버 이벤트를 감지할 특정 오브젝트 (비워둘 경우 자기 자신 및 부모를 감지)")]
    public GameObject customTriggerTarget;

    private RectTransform targetRectTransform;
    
    private Sprite defaultSprite;
    private Vector2 defaultSize;
    private Vector2 defaultPosition;
    private Color defaultColor;
    private Coroutine blinkCoroutine;

    private int hoverCount = 0;
    private bool isHovered = false;
    
    [Header("호버 설정")]
    [Tooltip("마우스 호버 시 보여줄 스프라이트")]
    public Sprite hoverSprite;
    
    [Tooltip("호버 시 새로운 이미지의 원본 크기(Native Size)로 자동 변경할지 여부")]
    public bool useNativeSizeOnHover = true;

    [Tooltip("호버 시 위치(Pos X, Pos Y)를 직접 지정할지 여부")]
    public bool useCustomHoverPosition = false;
    
    [Tooltip("호버 시 변경할 위치 (useCustomHoverPosition이 체크되어야 적용됨)")]
    public Vector2 hoverPosition;

    [Header("깜빡임(Blink) 설정")]
    [Tooltip("호버 시 이미지가 깜빡일지 여부")]
    public bool useBlinkEffect = true;
    
    [Tooltip("깜빡이는 간격 (초)")]
    public float blinkInterval = 0.5f;
    
    [Tooltip("깜빡일 때 변할 투명도 (0 = 완전 투명, 1 = 불투명)")]
    [Range(0f, 1f)]
    public float blinkAlpha = 0f;

    void Awake()
    {
        InitializeTargetImage();
        SetupTriggerForwarders();
    }

    void OnEnable()
    {
        hoverCount = 0;
        if (isHovered)
        {
            RevertToDefault();
        }
    }

    void OnDisable()
    {
        hoverCount = 0;
        if (isHovered)
        {
            RevertToDefault();
        }
    }

    void OnDestroy()
    {
        CleanupForwarders();
    }

    private void InitializeTargetImage()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
            if (targetImage == null)
            {
                targetImage = GetComponentInChildren<Image>();
            }
        }

        if (targetImage != null)
        {
            targetRectTransform = targetImage.rectTransform;
            defaultSprite = targetImage.sprite;
            defaultColor = targetImage.color;

            if (targetRectTransform != null)
            {
                defaultSize = targetRectTransform.sizeDelta;
                defaultPosition = targetRectTransform.anchoredPosition;
            }
        }
    }

    private void SetupTriggerForwarders()
    {
        // 커스텀 트리거 대상이 지정되어 있는 경우
        if (customTriggerTarget != null && customTriggerTarget != gameObject)
        {
            AttachForwarder(customTriggerTarget);
        }

        // 부모 오브젝트 호버 감지 설정이 켜져 있고 부모가 있는 경우
        if (triggerOnParentHover && transform.parent != null)
        {
            AttachForwarder(transform.parent.gameObject);
        }
    }

    private void AttachForwarder(GameObject targetGo)
    {
        if (targetGo == null) return;

        UIHoverEventForwarder forwarder = targetGo.GetComponent<UIHoverEventForwarder>();
        if (forwarder == null)
        {
            forwarder = targetGo.AddComponent<UIHoverEventForwarder>();
        }
        forwarder.RegisterTarget(this);
    }

    private void CleanupForwarders()
    {
        if (customTriggerTarget != null)
        {
            UIHoverEventForwarder forwarder = customTriggerTarget.GetComponent<UIHoverEventForwarder>();
            if (forwarder != null)
            {
                forwarder.UnregisterTarget(this);
            }
        }

        if (transform.parent != null)
        {
            UIHoverEventForwarder forwarder = transform.parent.GetComponent<UIHoverEventForwarder>();
            if (forwarder != null)
            {
                forwarder.UnregisterTarget(this);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverCount++;
        if (hoverCount == 1 && !isHovered)
        {
            ApplyHover();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoverCount = Mathf.Max(0, hoverCount - 1);
        if (hoverCount == 0 && isHovered)
        {
            RevertToDefault();
        }
    }

    private void ApplyHover()
    {
        isHovered = true;
        if (targetImage != null && hoverSprite != null)
        {
            targetImage.sprite = hoverSprite;
            if (useNativeSizeOnHover)
            {
                targetImage.SetNativeSize();
            }
            if (useCustomHoverPosition && targetRectTransform != null)
            {
                targetRectTransform.anchoredPosition = hoverPosition;
            }

            if (useBlinkEffect)
            {
                if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
                blinkCoroutine = StartCoroutine(BlinkRoutine());
            }
        }
    }

    private void RevertToDefault()
    {
        isHovered = false;
        if (targetImage != null)
        {
            if (defaultSprite != null)
            {
                targetImage.sprite = defaultSprite;
            }

            if (targetRectTransform != null)
            {
                targetRectTransform.sizeDelta = defaultSize;
                targetRectTransform.anchoredPosition = defaultPosition;
            }
            
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            targetImage.color = defaultColor;
        }
    }

    private IEnumerator BlinkRoutine()
    {
        bool isAlphaLow = false;
        
        while (true)
        {
            yield return new WaitForSeconds(blinkInterval);

            isAlphaLow = !isAlphaLow;
            
            Color newColor = defaultColor;
            newColor.a = isAlphaLow ? blinkAlpha : defaultColor.a;
            
            if (targetImage != null)
            {
                targetImage.color = newColor;
            }
        }
    }
}

/// <summary>
/// 부모 오브젝트 또는 지정된 트리거 오브젝트의 PointerEnter/PointerExit 이벤트를 UIHoverImageSwap으로 전달하는 헬퍼 컴포넌트
/// </summary>
public class UIHoverEventForwarder : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private readonly List<UIHoverImageSwap> targets = new List<UIHoverImageSwap>();

    public void RegisterTarget(UIHoverImageSwap target)
    {
        if (target != null && !targets.Contains(target))
        {
            targets.Add(target);
        }
    }

    public void UnregisterTarget(UIHoverImageSwap target)
    {
        if (target != null)
        {
            targets.Remove(target);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (targets[i] != null && targets[i].isActiveAndEnabled)
            {
                targets[i].OnPointerEnter(eventData);
            }
            else if (targets[i] == null)
            {
                targets.RemoveAt(i);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            if (targets[i] != null && targets[i].isActiveAndEnabled)
            {
                targets[i].OnPointerExit(eventData);
            }
            else if (targets[i] == null)
            {
                targets.RemoveAt(i);
            }
        }
    }
}
