using System.Collections;
using UnityEngine;

namespace _Project.Scripts.VisualScripting
{
    /// <summary>
    /// 지정한 오브젝트를 파괴(제거)하고, 파괴된 위치에 이펙트를 생성하는 Output 모듈
    /// </summary>
    public class DestroyObjectOutput : ProcessBase
    {
        [Header("Destroy Settings")]
        [Tooltip("파괴할 대상 오브젝트 (비워두면 이 스크립트가 붙은 오브젝트 자신을 파괴합니다)")]
        [SerializeField] private GameObject targetObject;

        [Tooltip("체크 시 즉시 파괴되며, 체크 해제 시 아래의 지연 시간 후에 파괴됩니다.")]
        [SerializeField] private bool destroyImmediately = true;

        [Tooltip("파괴되기 전까지 대기할 시간 (초)")]
        [SerializeField] private float delaySeconds = 1.0f;

        [Header("Effect Settings")]
        [Tooltip("파괴될 때 생성할 파티클/이펙트 프리팹 (없으면 비워두세요)")]
        [SerializeField] private GameObject destroyEffectPrefab;

        private bool isDestroying = false;

        private void Start()
        {
            // 타겟을 지정하지 않았다면 자기 자신을 타겟으로 설정
            if (targetObject == null)
            {
                targetObject = gameObject;
            }
        }

        public override void Execute()
        {
            // 이미 파괴가 진행 중이거나 타겟이 없다면 무시
            if (isDestroying || targetObject == null) return;

            if (destroyImmediately)
            {
                PerformDestruction();
            }
            else
            {
                StartCoroutine(DestroyRoutine());
            }
        }

        private IEnumerator DestroyRoutine()
        {
            isDestroying = true;

#if UNITY_EDITOR
            Debug.Log($"<color=yellow>[{gameObject.name}]</color> 파괴 타이머 시작... ({delaySeconds}초 후 파괴)");
#endif

            // 지정된 시간만큼 대기
            yield return new WaitForSeconds(delaySeconds);

            PerformDestruction();
        }

        private void PerformDestruction()
        {
            // 1. 파괴 이펙트가 설정되어 있다면, 타겟의 위치와 회전값을 그대로 가져와 생성
            if (destroyEffectPrefab != null)
            {
                Instantiate(destroyEffectPrefab, targetObject.transform.position, targetObject.transform.rotation);
            }

            // 2. 이 노드 다음에 연결된 다른 노드들이 작동할 수 있도록 상태를 먼저 켜줌
            // (오브젝트가 파괴되기 직전에 신호를 넘겨야 안전하게 전달됩니다)
            IsOn = true;

#if UNITY_EDITOR
            Debug.Log($"<color=green>[{gameObject.name}]</color> '{targetObject.name}' 오브젝트가 파괴되었습니다.");
#endif

            // 3. 실제 타겟 오브젝트 파괴 (유니티 메모리에서 제거)
            Destroy(targetObject);
        }
    }
}