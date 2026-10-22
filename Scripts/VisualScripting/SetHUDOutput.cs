using UnityEngine;

namespace _Project.Scripts.VisualScripting.UI
{
    /// <summary>
    /// 시퀀스 도중 특정 상황에서 HUD를 켜거나 끄는 Process 노드입니다.
    /// </summary>
    public class SetHUDOutput : ProcessBase
    {
        [Header("HUD Control")]
        [Tooltip("체크 시 HUD 활성화(ON), 체크 해제 시 HUD 비활성화(OFF)")]
        [SerializeField] private bool enableHUD = true;

        public override void Execute()
        {
            // UICanvas의 정적 메서드를 호출하여 즉시 On/Off
            UICanvas.SetHUD(enableHUD); //

            IsOn = true;
        }
    }
}