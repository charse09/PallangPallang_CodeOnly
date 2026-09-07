using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 오브젝트를 현재 부모로부터 분리하고, 스크립트를 활성화하는 Output 모듈
    /// (CharacterController 바닥 뚫림 버그 방지 패치 적용)
    /// </summary>
    public class DetachAndEnableOutput : ProcessBase
    {
        [Header("Detach Settings")]
        [SerializeField] private Transform targetToDetach;
        [SerializeField] private bool keepWorldPosition = true;

        [Header("Enable Settings")]
        [SerializeField] private MonoBehaviour scriptToEnable;

        public override void Execute()
        {
            if (targetToDetach == null) targetToDetach = transform;

            // ★ 버그 픽스 핵심: 분리하기 전에 CharacterController가 있다면 먼저 끕니다.
            CharacterController cc = targetToDetach.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            // 부모로부터 독립
            if (targetToDetach.parent != null)
            {
                targetToDetach.SetParent(null, keepWorldPosition);
#if UNITY_EDITOR
                Debug.Log($"<color=cyan>[{gameObject.name}]</color> '{targetToDetach.name}' 독립 완료.");
#endif
            }

            // ★ 버그 픽스 핵심: 독립이 끝난 후 CharacterController를 안전하게 다시 켭니다.
            if (cc != null)
            {
                cc.enabled = true;
            }

            // 지정된 스크립트 활성화 (PlayerLocomotion 등)
            if (scriptToEnable != null && !scriptToEnable.enabled)
            {
                scriptToEnable.enabled = true;
#if UNITY_EDITOR
                Debug.Log($"<color=green>[{gameObject.name}]</color> '{scriptToEnable.GetType().Name}' 스크립트 활성화됨.");
#endif
            }

            IsOn = true;
        }
    }
}