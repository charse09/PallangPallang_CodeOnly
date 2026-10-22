using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// UI 버튼 또는 클릭 가능한 UI 요소에 부착하여
/// 마우스 호버(커서 올림) 및 클릭 시 SoundManager를 통해 효과음을 재생하는 컴포넌트
/// </summary>
public class UIButtonSFX : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Hover Sound (마우스 올림)")]
    [Tooltip("체크 시 마우스 커서가 버튼 위로 올라왔을 때 효과음을 재생합니다.")]
    [SerializeField] private bool playHoverSound = true;

    [Tooltip("호버 시 재생할 SFX 키 (SO_SFXTable 등록 키)")]
    [SerializeField] private string hoverSoundKey = "UI_Hover";

    [Header("Click Sound (버튼 클릭)")]
    [Tooltip("체크 시 마우스로 버튼을 클릭했을 때 효과음을 재생합니다.")]
    [SerializeField] private bool playClickSound = true;

    [Tooltip("클릭 시 재생할 SFX 키 (SO_SFXTable 등록 키)")]
    [SerializeField] private string clickSoundKey = "UI_Click";

    private Button targetButton;

    private void Awake()
    {
        // 동일 오브젝트에 Button 컴포넌트가 있다면 캐싱
        targetButton = GetComponent<Button>();
    }

    /// <summary>
    /// 마우스 커서가 UI 영역 내부로 진입할 때 유니티 이벤트 시스템에 의해 자동 호출
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!playHoverSound) return;
        if (!IsInteractable()) return;

        PlaySFX(hoverSoundKey);
    }

    /// <summary>
    /// UI 영역 위에서 마우스 클릭이 발생했을 때 유니티 이벤트 시스템에 의해 자동 호출
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 마우스 좌클릭에만 반응
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (!playClickSound) return;
        if (!IsInteractable()) return;

        PlaySFX(clickSoundKey);
    }

    private bool IsInteractable()
    {
        // Button 컴포넌트가 붙어있는데 비활성화(interactable = false) 상태라면 사운드 차단
        if (targetButton != null && !targetButton.interactable)
        {
            return false;
        }
        return true;
    }

    private void PlaySFX(string soundKey)
    {
        if (!string.IsNullOrEmpty(soundKey) && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(soundKey);
        }
    }
}