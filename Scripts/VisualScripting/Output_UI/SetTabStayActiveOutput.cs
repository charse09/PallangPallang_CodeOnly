using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// UICanvas의 HUD 안에 있는 Tab[Stay]를 켜거나(true) 끌(false) 수 있는 Visual Scripting Output 노드입니다.
    /// </summary>
    [AddComponentMenu("Visual Scripting/Outputs/UI/Tab[Stay] On Off (Output)")]
    public class SetTabStayActiveOutput : ProcessBase
    {
        [Header("Tab[Stay] Settings")]
        [Tooltip("Tab[Stay]를 켤지(true), 끌지(false) 설정합니다.")]
        [SerializeField] private bool setActive = true;

        public override void Execute()
        {
            IsOn = false;

            UICanvas.SetTabStay(setActive);

            IsOn = true;
        }
    }
}
