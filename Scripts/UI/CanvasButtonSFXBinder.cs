using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Canvas 프리팹 최상위에 부착하여, 하위의 모든 Button 컴포넌트에
/// 클릭 및 마우스 호버 사운드를 자동으로 감지하고 바인딩하는 스크립트
/// </summary>
public class CanvasButtonSFXBinder : MonoBehaviour
{
    [Header("Default Sound Keys")]
    [Tooltip("기본 마우스 호버 효과음 키 (SO_SFXTable 등록 키, 비워두면 무음)")]
    [SerializeField] private string defaultHoverSoundKey = "UI_Hover";

    [Tooltip("기본 버튼 클릭 효과음 키 (SO_SFXTable 등록 키, 비워두면 무음)")]
    [SerializeField] private string defaultClickSoundKey = "UI_Click";

    [Header("Options")]
    [Tooltip("비활성화(Inactive)되어 있는 하위 버튼까지 탐색할지 여부")]
    [SerializeField] private bool includeInactiveButtons = true;

    [Tooltip("체크 시 호버 사운드를 재생합니다.")]
    [SerializeField] private bool enableHoverSound = true;

    [Tooltip("체크 시 클릭 사운드를 재생합니다.")]
    [SerializeField] private bool enableClickSound = true;

    // 마우스를 빠르게 훑을 때 사운드가 겹치는 현상 방지용 전역 쿨다운
    private static float _lastGlobalHoverTime;
    private const float GlobalHoverCooldown = 0.05f;

    private readonly HashSet<Button> _boundButtons = new HashSet<Button>();

    private void Awake()
    {
        BindAllButtons();
    }

    /// <summary>
    /// 하위의 모든 버튼을 탐색하여 사운드 리스너를 바인딩합니다.
    /// (런타임에 동적으로 새 버튼/슬롯이 추가된 후 외부에서 호출 가능)
    /// </summary>
    public void BindAllButtons()
    {
        Button[] buttons = GetComponentsInChildren<Button>(includeInactiveButtons);

        foreach (var button in buttons)
        {
            // 이미 바인딩된 버튼이거나, 개별 커스텀 사운드 컴포넌트(UIButtonSFX)가 이미 붙어있다면 건너뜀
            if (_boundButtons.Contains(button) || button.GetComponent<UIButtonSFX>() != null)
            {
                continue;
            }

            BindSingleButton(button);
            _boundButtons.Add(button);
        }
    }

    private void BindSingleButton(Button button)
    {
        // 1. 클릭 사운드 등록
        if (enableClickSound && !string.IsNullOrEmpty(defaultClickSoundKey))
        {
            button.onClick.AddListener(() =>
            {
                if (button.interactable)
                {
                    PlaySFX(defaultClickSoundKey);
                }
            });
        }

        // 2. 호버 사운드 등록 (EventTrigger 활용)
        if (enableHoverSound && !string.IsNullOrEmpty(defaultHoverSoundKey))
        {
            EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry enterEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };

            enterEntry.callback.AddListener((data) =>
            {
                if (!button.interactable) return;

                // 연속 훑기 시 쿨다운 검사
                if (Time.unscaledTime - _lastGlobalHoverTime < GlobalHoverCooldown) return;
                _lastGlobalHoverTime = Time.unscaledTime;

                PlaySFX(defaultHoverSoundKey);
            });

            trigger.triggers.Add(enterEntry);
        }
    }

    private void PlaySFX(string soundKey)
    {
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(soundKey))
        {
            SoundManager.Instance.PlaySFX(soundKey);
        }
    }
}