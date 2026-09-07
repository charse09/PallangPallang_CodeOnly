using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 오브젝트를 부모 자식 종속시키고, 특정 스크립트를 비활성화하는 Output 노드
    /// </summary>
    public class AttachToParentOutput : ProcessBase
    {
        [Header("Target Settings")]
        [Tooltip("자식으로 들어갈 타겟 오브젝트 (아래 'Find Player Automatically'가 꺼져있고 비워두면 이 스크립트가 붙은 오브젝트)")]
        [SerializeField] private Transform targetObject;

        [Tooltip("체크 시: 씬 안의 PlayerLocomotion을 찾아 자동으로 targetObject로 설정합니다. (플레이어를 붙일 때 유용)")]
        [SerializeField] private bool findPlayerAutomatically = false;

        [Tooltip("새로운 부모가 될 오브젝트")]
        [SerializeField] private Transform newParent;

        [Header("Position Settings")]
        [Tooltip("체크 시: 부모의 위치(0,0,0)와 회전으로 자식을 이동시킵니다.")]
        [SerializeField] private bool snapToParent = true;

        [Tooltip("체크 시: 자식 오브젝트의 크기(Scale)가 부모의 크기에 늘어나거나 찌그러지지 않고 독립적으로 유지됩니다.")]
        [SerializeField] private bool keepIndependentScale = true;

        [Header("Disable Settings")]
        [Tooltip("자식으로 들어갈 때 비활성화(Disable) 처리할 스크립트 목록입니다.")]
        [SerializeField] private List<MonoBehaviour> scriptsToDisable = new List<MonoBehaviour>();

        [Header("Rigidbody Settings")]
        [Tooltip("체크 시: 타겟에 Rigidbody가 있다면 isKinematic을 true로 만들어 물리(중력 등)의 영향을 받지 않고 부모를 완벽히 따라가게 합니다.")]
        [SerializeField] private bool setKinematic = false;

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

            // 스케일을 독립적으로 유지하기 위해 부모 설정 전의 원래 월드 스케일을 기억합니다.
            Vector3 originalWorldScale = targetObject.lossyScale;

            // 1. 부모 설정
            // SetParent의 두 번째 인자(worldPositionStays)를 항상 true로 넘겨서 Unity가 자동으로 스케일을 계산하도록 합니다.
            targetObject.SetParent(newParent, true);

            // 2. Snap 처리 (위치와 회전만 부모에게 맞춤)
            if (snapToParent && newParent != null)
            {
                targetObject.localPosition = Vector3.zero;
                targetObject.localRotation = Quaternion.identity;
            }

            // 3. 스케일 독립성 보장
            if (newParent != null)
            {
                if (keepIndependentScale)
                {
                    // 부모의 크기(lossyScale)와 무관하게 원래의 크기를 가지도록 역계산하여 적용합니다.
                    Vector3 parentScale = newParent.lossyScale;
                    targetObject.localScale = new Vector3(
                        originalWorldScale.x / parentScale.x,
                        originalWorldScale.y / parentScale.y,
                        originalWorldScale.z / parentScale.z
                    );
                }
                else if (snapToParent)
                {
                    // 예전 방식처럼 부모의 스케일에 완전히 종속되기를 원할 경우 (늘어남 발생)
                    targetObject.localScale = Vector3.one;
                }
            }

            // 4. 지정된 스크립트들을 비활성화
            if (scriptsToDisable != null && scriptsToDisable.Count > 0)
            {
                foreach (var script in scriptsToDisable)
                {
                    if (script != null)
                    {
                        script.enabled = false;
#if UNITY_EDITOR
                        Debug.Log($"<color=orange>[{gameObject.name}]</color> '{script.GetType().Name}' 스크립트가 비활성화되었습니다.");
#endif
                    }
                }
            }

            // 5. Rigidbody isKinematic 처리 및 PlayerLocomotion 강제 제어
            if (setKinematic)
            {
                Rigidbody rb = targetObject.GetComponentInChildren<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = true;
#if UNITY_EDITOR
                    Debug.Log($"<color=orange>[{gameObject.name}]</color> '{targetObject.name}'의 Rigidbody가 Kinematic으로 변경되었습니다.");
#endif
                }

                // 타겟이 플레이어일 경우 자체 중력 로직을 확실하게 끄기 위해 PlayerLocomotion을 찾아 비활성화
                PlayerLocomotion playerLoco = targetObject.GetComponentInChildren<PlayerLocomotion>();
                if (playerLoco != null)
                {
                    playerLoco.enabled = false;
                    playerLoco.ResetAllVelocities();
#if UNITY_EDITOR
                    Debug.Log($"<color=orange>[{gameObject.name}]</color> '{targetObject.name}'의 PlayerLocomotion을 강제로 비활성화하고 속도를 초기화했습니다.");
#endif
                }
            }

#if UNITY_EDITOR
            string parentName = newParent != null ? newParent.name : "최상단(Root)";
            Debug.Log($"<color=cyan>[{gameObject.name}]</color> '{targetObject.name}'이(가) '{parentName}'의 자식이 되었습니다.");
#endif

            // 다음 노드로 실행을 넘김
            IsOn = true;
        }
    }
}