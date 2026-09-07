using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정된 자식 오브젝트를 부모로부터 분리(독립)시키고, 특정 스크립트들을 다시 활성화하는 Output 모듈
    /// </summary>
    public class DetachFromParentOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("부모로부터 분리할 타겟 오브젝트 (아래 'Find Player Automatically'가 꺼져있고 비워두면 이 스크립트가 붙은 오브젝트)")]
        [SerializeField] private Transform targetObject;

        [Tooltip("체크 시: 씬 안의 PlayerLocomotion을 찾아 자동으로 targetObject로 설정합니다. (플레이어를 분리할 때 유용)")]
        [SerializeField] private bool findPlayerAutomatically = false;

        [Header("Position Settings")]
        [Tooltip("체크 시: 현재 월드 좌표와 스케일(World Scale)을 유지한 채 분리됩니다.")]
        [SerializeField] private bool keepWorldPosition = true;

        [Header("Enable Settings")]
        [Tooltip("분리될 때 다시 활성화(Enable) 처리할 스크립트 목록입니다.")]
        [SerializeField] private List<MonoBehaviour> scriptsToEnable = new List<MonoBehaviour>();

        [Header("Rigidbody Settings")]
        [Tooltip("체크 시: 타겟에 Rigidbody가 있다면 다시 물리 연산을 받도록 isKinematic을 false로 돌려놓습니다.")]
        [SerializeField] private bool disableKinematicOnDetach = false;

        public override void Execute()
        {
            if (findPlayerAutomatically)
            {
                PlayerLocomotion player = FindFirstObjectByType<PlayerLocomotion>();
                if (player != null)
                {
                    targetObject = player.transform;
                }
            }

            if (targetObject == null)
            {
                targetObject = transform;
            }

            // 1. 부모로부터 분리 (SetParent의 두 번째 인자가 true면 유니티가 알아서 스케일을 원상복구 해줍니다)
            targetObject.SetParent(null, keepWorldPosition);

            // 2. 지정된 스크립트들을 다시 활성화
            if (scriptsToEnable != null && scriptsToEnable.Count > 0)
            {
                foreach (var script in scriptsToEnable)
                {
                    if (script != null)
                    {
                        script.enabled = true;
#if UNITY_EDITOR
                        Debug.Log($"<color=orange>[{gameObject.name}]</color> '{script.GetType().Name}' 스크립트가 다시 활성화되었습니다.");
#endif
                    }
                }
            }

            // 3. Rigidbody isKinematic 복구 및 PlayerLocomotion 복구
            if (disableKinematicOnDetach)
            {
                Rigidbody rb = targetObject.GetComponentInChildren<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
#if UNITY_EDITOR
                    Debug.Log($"<color=orange>[{gameObject.name}]</color> '{targetObject.name}'의 Rigidbody가 다시 물리 연산(isKinematic=false)을 받습니다.");
#endif
                }

                // 타겟이 플레이어일 경우 PlayerLocomotion 복구
                PlayerLocomotion playerLoco = targetObject.GetComponentInChildren<PlayerLocomotion>();
                if (playerLoco != null)
                {
                    playerLoco.enabled = true;
#if UNITY_EDITOR
                    Debug.Log($"<color=orange>[{gameObject.name}]</color> '{targetObject.name}'의 PlayerLocomotion이 복구되었습니다.");
#endif
                }
            }

#if UNITY_EDITOR
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> '{targetObject.name}'이(가) 부모로부터 분리되어 최상단에 배치되었습니다.");
#endif

            // 프레임워크 룰: 즉시 완료
            IsOn = true;
        }
    }
}