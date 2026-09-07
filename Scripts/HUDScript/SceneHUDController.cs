using UnityEngine;

/// <summary>
/// 특정 씬에 배치하여 해당 씬이 시작될 때 UICanvas의 HUD를 켜거나(true) 끌(false) 수 있는 컴포넌트입니다.
/// DontDestroyOnLoad로 전달된 UICanvas에 자동으로 접근하여 설정합니다.
/// </summary>
[AddComponentMenu("UI/Scene HUD Controller")]
public class SceneHUDController : MonoBehaviour
{
    [Header("Scene HUD Override")]
    [Tooltip("이 씬에서 HUD를 켤지(true), 끌지(false) 설정합니다.")]
    [SerializeField] private bool showHUD = false;

    [Tooltip("Start 시점에 HUD 상태를 강제 적용할지 여부")]
    [SerializeField] private bool applyOnStart = true;

    private void Start()
    {
        if (applyOnStart)
        {
            ApplyHUDState();
        }
    }

    public void ApplyHUDState()
    {
        UICanvas.SetHUD(showHUD);
    }
}