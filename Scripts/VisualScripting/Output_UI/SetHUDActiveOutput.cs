using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// UICanvas 하위의 HUD를 켜거나(true) 끄는(false) 비주얼 스크립팅 Output 노드입니다.
    /// DontDestroyOnLoad로 전달된 UICanvas를 자동으로 찾아 HUD를 제어합니다.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Outputs/UI/HUD On Off (Output)")]
    public class SetHUDActiveOutput : ProcessBase
    {
        [Header("HUD Settings")]
        [Tooltip("HUD를 켤지(true), 끌지(false) 설정합니다.")]
        [SerializeField] private bool setActive = true;

        public override void Execute()
        {
            IsOn = false;

            // DontDestroyOnLoad로 넘어온 UICanvas의 HUD를 제어
            UICanvas.SetHUD(setActive);

            // 노드 완료 처리
            IsOn = true;
        }
    }
}