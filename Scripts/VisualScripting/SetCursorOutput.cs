using UnityEngine;

namespace _Project.Scripts.VisualScripting.UI
{
    /// <summary>
    /// HUD나 메뉴를 건드리지 않고 마우스 커서 표시/잠금 상태만 독립적으로 제어하는 노드입니다.
    /// 화면 클릭 시 커서가 다시 잠기며 숨겨지는 현상을 방지하도록 UpdateCursorState와 연동됩니다.
    /// </summary>
    public class SetCursorOutput : ProcessBase
    {
        [Header("Cursor Control")]
        [Tooltip("체크 시 마우스 커서 표시(해제), 체크 해제 시 마우스 커서 숨김(잠금)")]
        [SerializeField] private bool showCursor = true;

        public override void Execute()
        {
            ApplyCursorState(showCursor);
            IsOn = true;
        }

        /// <summary>
        /// 씬 어디서든 마우스 커서 표시 상태를 안전하게 제어하는 정적 함수입니다.
        /// </summary>
        public static void ApplyCursorState(bool visible)
        {
            // 1. 화면 클릭 시 UpdateCursorState가 마우스를 다시 잠그지 못하도록 플래그 동기화
            UpdateCursorState.isMenuOpen = visible; //

            // 2. 씬에 배치된 UpdateCursorState 탐색 및 커서 상태 적용
            UpdateCursorState cursorState = Object.FindFirstObjectByType<UpdateCursorState>(); //[cite: 12]
            if (cursorState != null)
            {
                cursorState.SetCursorLock(!visible); //[cite: 12]
            }
            else
            {
                Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = visible;
            }
        }
    }
}