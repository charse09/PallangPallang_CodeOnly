using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 플레이어의 이동 키(W, A, S, D)를 방향별로 개별 허용/차단하는 Output 모듈
    /// </summary>
    public class PlayerDirectionalInputControlOutput : ProcessBase
    {
        [Header("Direction Settings")]
        [Tooltip("체크 시: W 키 (전진) 입력 허용")]
        [SerializeField] private bool allowW = false;

        [Tooltip("체크 시: A 키 (좌측) 입력 허용")]
        [SerializeField] private bool allowA = true;

        [Tooltip("체크 시: S 키 (후진) 입력 허용")]
        [SerializeField] private bool allowS = false;

        [Tooltip("체크 시: D 키 (우측) 입력 허용")]
        [SerializeField] private bool allowD = true;

        public override void Execute()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.SetDirectionalInput(allowW, allowA, allowS, allowD);
            }

#if UNITY_EDITOR
            Debug.Log($"<color=white>[{gameObject.name}]</color> 이동 키 상태 갱신 - " +
                      $"W: {(allowW ? "<color=green>O</color>" : "<color=red>X</color>")}, " +
                      $"A: {(allowA ? "<color=green>O</color>" : "<color=red>X</color>")}, " +
                      $"S: {(allowS ? "<color=green>O</color>" : "<color=red>X</color>")}, " +
                      $"D: {(allowD ? "<color=green>O</color>" : "<color=red>X</color>")}");
#endif

            IsOn = true;
        }
    }
}